using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class AreaOfEffectHealSkill : SkillBase
{
    [Header("Heal Settings")]
    public int healAmount = 20;
    public GameObject effectPrefab;

    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!isLocalPlayer) return;

        CmdHealArea(targetPosition.Value, healAmount);
    }

    [Command]
    private void CmdHealArea(Vector3 position, int amount)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, Range);
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        foreach (var hitCollider in hitColliders)
        {
            Health targetHealth = hitCollider.GetComponent<Health>();
            PlayerCore targetCore = hitCollider.GetComponent<PlayerCore>();

            if (targetHealth != null && targetCore != null && casterCore != null)
            {
                // Проверяем, что цель находится в той же команде, что и кастующий
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