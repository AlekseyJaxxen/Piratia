using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class AreaOfEffectHealSkill : SkillBase
{
    [Header("Heal Settings")]
    public int healAmount = 20;

    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, Range);
        foreach (var hitCollider in hitColliders)
        {
            Health targetHealth = hitCollider.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.Heal(healAmount);
            }
        }
    }
}