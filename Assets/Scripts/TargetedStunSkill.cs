using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewTargetedStunSkill", menuName = "Skills/TargetedStunSkill")]
public class TargetedStunSkill : SkillBase
{
    [Header("Stun Skill Specifics")]
    public int baseDamage = 0; // ���� ����� �������� ����, ����� 0
    public float damageMultiplier = 1f;
    public float stunDuration = 2f;
    public GameObject effectPrefab;
    
    // Для lag compensation
    public float GetSkillRangeOrDefault()
    {
        return 20f; // Дефолтный радиус
    }

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[TargetedStunSkill] Target object is null");
            return;
        }
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if ((targetCore == null || IsAlly(targetCore, caster)) && targetMonster == null)
        {
            Debug.LogWarning("[TargetedStunSkill] Invalid target: not enemy");
            return;
        }
        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[TargetedStunSkill] Target {targetObject.name} has no NetworkIdentity");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[TargetedStunSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        
        // CLIENT-SIDE PREDICTION: Показываем мгновенный эффект
        if (caster.isLocalPlayer)
        {
            var predictionManager = caster.GetComponent<SkillPredictionManager>();
            if (predictionManager != null)
            {
                predictionManager.PredictSkillExecution(_skillName, targetIdentity.netId, Weight);
            }
        }
        
        // LAG-COMPENSATED SKILL EXECUTION: Отправляем с временной меткой
        skills.CmdExecuteSkillWithCompensation(caster, null, targetIdentity.netId, _skillName, Weight, NetworkTime.time);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (targetObject == null) return;
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats == null) return;

        int finalDamage = 0;
        if (baseDamage > 0)
        {
            if (SkillDamageType == DamageType.Physical)
            {
                int randomAttack = Random.Range(stats.minAttack, stats.maxAttack + 1);
                finalDamage = Mathf.RoundToInt((baseDamage + randomAttack) * damageMultiplier);
            }
            else
            {
                finalDamage = baseDamage + Mathf.RoundToInt(stats.spirit * damageMultiplier);
            }
        }

        // Try HealthMonster first for monsters, then fall back to Health
        HealthMonster targetHealthMonster = targetObject.GetComponent<HealthMonster>();
        if (targetHealthMonster != null && finalDamage > 0)
        {
            targetHealthMonster.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
        }
        else
        {
            Health targetHealth = targetObject.GetComponent<Health>();
            if (targetHealth != null && finalDamage > 0)
            {
                targetHealth.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
            }
        }

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if (targetCore != null && IsEnemy(targetCore, caster))
        {
            targetCore.ApplyControlEffect(ControlEffectType.Stun, stunDuration, weight);
        }
        else if (targetMonster != null)
        {
            targetMonster.ReceiveControlEffect(ControlEffectType.Stun, stunDuration, weight);
        }
        caster.GetComponent<PlayerSkills>().RpcPlayTargetedStun(targetObject.GetComponent<NetworkIdentity>().netId, _skillName);
    }

    public void PlayEffect(GameObject target, PlayerSkills playerSkills)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
            playerSkills.StartCoroutine(DestroyEffectAfterDelay(effect, 2f));
        }
    }

    private IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            Object.Destroy(effect);
        }
    }
    
    /// <summary>
    /// Checks if target player is an enemy
    /// Solo players are enemies to each other
    /// </summary>
    private bool IsEnemy(PlayerCore target, PlayerCore caster)
    {
        if (target == null) return false;
        
        // Solo players are enemies to each other
        if (caster.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return true; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return caster.team != target.team;
    }
    
    /// <summary>
    /// Checks if target player is an ally
    /// Solo players are never allies to each other (except themselves)
    /// </summary>
    private bool IsAlly(PlayerCore target, PlayerCore caster)
    {
        if (target == null) return false;
        
        // Solo players are never allies to each other
        if (caster.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return false; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return caster.team == target.team;
    }
}