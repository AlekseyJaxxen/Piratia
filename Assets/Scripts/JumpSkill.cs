using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "JumpSkill", menuName = "Skills/JumpSkill")]
public class JumpSkill : SkillBase
{
    //[SerializeField] protected CastType castType = CastType.GroundAoEInstant;

    [Header("Jump Settings")]
    [SerializeField] private float baseJumpDistance = 10f;
    [SerializeField] private float jumpDuration = 1f;
    [SerializeField] private float maxHeightDifference = 5f;
    [SerializeField] private float heightMultiplier = 2f;

    private LineRenderer trajectoryIndicator;
    private Vector3? currentTargetPos;

    public override void Init(PlayerCore core)
    {
        base.Init(core);
        _castTime = 0f; // JumpSkill не требует времени каста
        _range = CalculateMaxJumpDistance();
    }

    private float CalculateMaxJumpDistance()
    {
        return baseJumpDistance + (_player.Stats.agility * 0.5f);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (!targetPosition.HasValue) return;

        Vector3 startPos = caster.transform.position;
        Vector3 endPos = targetPosition.Value;

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(endPos, out hit, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[JumpSkill] Invalid target: {endPos}");
            return;
        }
        endPos = hit.position;

        float distance = Vector3.Distance(startPos, endPos);
        float heightDiff = Mathf.Abs(endPos.y - startPos.y);
        float effectiveMax = _range - (heightDiff / maxHeightDifference) * _range * 0.5f;
        
        // Если цель слишком далеко, прыгаем к ближайшей точке в радиусе
        if (distance > effectiveMax)
        {
            Vector3 direction = (endPos - startPos).normalized;
            endPos = startPos + direction * effectiveMax;
            
            // Проверяем, что новая позиция на NavMesh
            if (NavMesh.SamplePosition(endPos, out hit, 1f, NavMesh.AllAreas))
            {
                endPos = hit.position;
            }
        }
        
        if (heightDiff > maxHeightDifference)
        {
            Debug.LogWarning($"[JumpSkill] Jump invalid: height={heightDiff} > max={maxHeightDifference}");
            return;
        }

        caster.ApplyControlEffect(ControlEffectType.Stun, jumpDuration, weight);
        caster.Skills.StartJumpCoroutine(startPos, endPos, weight, jumpDuration, heightMultiplier);
    }

    private IEnumerator PerformJump(PlayerCore caster, Vector3 start, Vector3 end, int weight)
    {
        caster.Movement.Agent.enabled = false;
        caster.GetComponent<NetworkTransformHybrid>().enabled = false;
        caster.RpcDisableNT();

        float elapsed = 0f;
        float distance = Vector3.Distance(start, end);
        float heightDiff = end.y - start.y;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y = start.y + t * heightDiff + Mathf.Sin(t * Mathf.PI) * (distance / 2f) * heightMultiplier;

            caster.transform.position = pos;
            RpcSetPosition(caster.netId, pos);

            yield return null;
        }
        caster.transform.position = end;
        RpcSetPosition(caster.netId, end);

        caster.Movement.Agent.enabled = true;
        caster.Movement.Agent.Warp(end);
        caster.GetComponent<NetworkTransformHybrid>().enabled = true;
        caster.RpcEnableNT();
        caster.ClearStunEffect();
    }

    [ClientRpc]
    private void RpcSetPosition(uint netId, Vector3 pos)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out var identity) && identity.gameObject != null)
        {
            identity.gameObject.transform.position = pos;
        }
    }

    protected override void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        // ���������� ����� (VFX, ����������), ���� �����; ��������� � ExecuteOnServer
    }

    public override void SetIndicatorVisibility(bool visible)
    {
        base.SetIndicatorVisibility(visible);
        if (visible)
        {
            if (trajectoryIndicator == null)
            {
                GameObject trajObj = new GameObject("TrajectoryIndicator");
                trajectoryIndicator = trajObj.AddComponent<LineRenderer>();
                trajectoryIndicator.positionCount = 20;
                trajectoryIndicator.startWidth = 0.1f;
                trajectoryIndicator.endWidth = 0.1f;
                trajectoryIndicator.material = new Material(Shader.Find("Sprites/Default")); // ������� ��������
            }
            trajectoryIndicator.gameObject.SetActive(true);
        }
        else
        {
            if (trajectoryIndicator != null)
            {
                Destroy(trajectoryIndicator.gameObject);
                trajectoryIndicator = null;
            }
            currentTargetPos = null;
        }
    }

    public override void SetEffectRadiusPosition(Vector3 position)
    {
        base.SetEffectRadiusPosition(position);
        currentTargetPos = position;
        UpdateTrajectory();
    }

    private void UpdateTrajectory()
    {
        if (trajectoryIndicator == null || !currentTargetPos.HasValue) return;

        Vector3 start = _player.transform.position;
        Vector3 end = currentTargetPos.Value;
        float distance = Vector3.Distance(start, end);
        float heightDiff = end.y - start.y;
        float effectiveMax = _range - (Mathf.Abs(heightDiff) / maxHeightDifference) * _range * 0.5f;

        bool canJump = distance <= effectiveMax && Mathf.Abs(heightDiff) <= maxHeightDifference;

        trajectoryIndicator.startColor = canJump ? Color.green : Color.red;
        trajectoryIndicator.endColor = canJump ? Color.green : Color.red;

        trajectoryIndicator.SetPosition(0, start);
        for (int i = 1; i < trajectoryIndicator.positionCount - 1; i++)
        {
            float t = (float)i / (trajectoryIndicator.positionCount - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y = start.y + t * heightDiff + Mathf.Sin(t * Mathf.PI) * (distance / 2f) * heightMultiplier;
            trajectoryIndicator.SetPosition(i, pos);
        }
        trajectoryIndicator.SetPosition(trajectoryIndicator.positionCount - 1, end);
    }

    public override void CleanupIndicators()
    {
        base.CleanupIndicators();
        if (trajectoryIndicator != null)
        {
            Destroy(trajectoryIndicator.gameObject);
            trajectoryIndicator = null;
        }
    }
}