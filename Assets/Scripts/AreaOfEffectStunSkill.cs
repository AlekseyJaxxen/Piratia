using UnityEngine;
using Mirror;
using System.Collections;

public class AreaOfEffectStunSkill : SkillBase
{
    [Header("Area Stun Skill Specifics")]
    public float stunDuration = 2f;
    public float effectRadius = 2f;
    public GameObject effectPrefab;

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (!isLocalPlayer || !targetPosition.HasValue) return;

        CmdStunArea(targetPosition.Value);
    }

    [Command]
    private void CmdStunArea(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, effectRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Player"))
            {
                NetworkIdentity targetIdentity = hit.GetComponent<NetworkIdentity>();
                if (targetIdentity != null && targetIdentity.netId != netId)
                {
                    PlayerCore targetCore = hit.GetComponent<PlayerCore>();
                    if (targetCore != null)
                    {
                        targetCore.StartCoroutine(StunRoutine(targetCore, stunDuration));
                    }
                }
            }
        }
        RpcPlayEffect(position);
    }

    [ClientRpc]
    private void RpcPlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            // ������� ������ � ����� ����������� ��� �������
            GameObject effect = Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 3f; // ����������� � 1.5 ����

            // ��������� �������� ��� �������� ����������
            StartCoroutine(DecreaseScaleOverTime(effect.transform, 1f, 2f));
        }
    }

    private IEnumerator DecreaseScaleOverTime(Transform targetTransform, float duration, float destroyTime)
    {
        float elapsed = 0f;
        Vector3 originalScale = targetTransform.localScale;
        Vector3 targetScale = Vector3.zero;

        // �������� ���������� ��������
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            targetTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // ��������, ��� ������� ����� ���� � ����� ��������
        targetTransform.localScale = Vector3.zero;

        // ���� ���������� ����� ����� ������������, ����� ������ �����
        yield return new WaitForSeconds(destroyTime - duration);

        // ���������� ������
        Destroy(targetTransform.gameObject);
    }

    [Server]
    private System.Collections.IEnumerator StunRoutine(PlayerCore core, float duration)
    {
        core.SetStunState(true);
        core.Movement.StopMovement();
        yield return new WaitForSeconds(duration);
        core.SetStunState(false);
    }
}