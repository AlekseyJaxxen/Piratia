using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class AreaOfEffectStunSkill : SkillBase
{
    [Header("AOE Stun Skill Specifics")]
    public float stunDuration = 2f;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue || !isOwned)
        {
            Debug.Log("Target position is null or not owned");
            return;
        }

        Debug.Log($"Attempting to AOE stun at position: {targetPosition.Value}");

        CmdStunArea(targetPosition.Value, stunDuration);
    }

    [Command]
    private void CmdStunArea(Vector3 position, float duration)
    {
        Debug.Log($"Server received AOE stun command at position: {position}");

        Collider[] hitColliders = Physics.OverlapSphere(position, Range);
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        foreach (var hitCollider in hitColliders)
        {
            PlayerCore targetCore = hitCollider.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null)
            {
                if (casterCore.team != targetCore.team)
                {
                    targetCore.ApplyControlEffect(ControlEffectType.Stun, duration);
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
            GameObject effect = Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
}