using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class AreaOfEffectHealSkill : SkillBase
{
    [Header("Heal Settings")]
    public int healAmount = 20;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue || !isOwned)
        {
            Debug.Log("Target position is null or not owned");
            return;
        }

        Debug.Log($"Attempting to AOE heal at position: {targetPosition.Value}");

        CmdHealArea(targetPosition.Value, healAmount);
    }

    [Command]
    private void CmdHealArea(Vector3 position, int amount)
    {
        Debug.Log($"Server received AOE heal command at position: {position}");

        Collider[] hitColliders = Physics.OverlapSphere(position, Range);
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        foreach (var hitCollider in hitColliders)
        {
            Health targetHealth = hitCollider.GetComponent<Health>();
            PlayerCore targetCore = hitCollider.GetComponent<PlayerCore>();

            if (targetHealth != null && targetCore != null && casterCore != null)
            {
                if (casterCore.team == targetCore.team)
                {
                    targetHealth.Heal(amount);
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