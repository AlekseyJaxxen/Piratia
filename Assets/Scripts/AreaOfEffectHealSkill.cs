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
        foreach (var hitCollider in hitColliders)
        {
            Health targetHealth = hitCollider.GetComponent<Health>();
            if (targetHealth != null)
            {
                // Вызываем метод Heal на сервере, как и положено
                targetHealth.Heal(amount);
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