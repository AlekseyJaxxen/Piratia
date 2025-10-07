using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewSlowSkill", menuName = "Skills/SlowSkill")]
public class SlowSkill : SkillBase
{
    [Header("Slow Skill Specifics")]
    public int baseDamage = 10;
    public float damageMultiplier = 1.5f;
    public float slowPercentage = 0.5f;
    public float slowDuration = 5f;
    public GameObject projectilePrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[SlowSkill] Target object is null");
            return;
        }
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if ((targetCore == null || IsAlly(targetCore, caster)) && (targetMonster == null || !targetObject.CompareTag("Enemy")))
        {
            Debug.LogWarning("[SlowSkill] Invalid target: not enemy or same team");
            return;
        }
        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[SlowSkill] Target {targetObject.name} has no NetworkIdentity");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[SlowSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        // Attempting to slow target
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats == null) return;
        int finalDamage;
        if (SkillDamageType == DamageType.Physical)
        {
            int randomAttack = Random.Range(stats.minAttack, stats.maxAttack + 1);
            finalDamage = Mathf.RoundToInt((baseDamage + randomAttack) * damageMultiplier);
        }
        else
        {
            finalDamage = baseDamage + Mathf.RoundToInt(stats.spirit * damageMultiplier);
        }
        // Try HealthMonster first for monsters, then fall back to Health
        HealthMonster targetHealthMonster = targetObject.GetComponent<HealthMonster>();
        if (targetHealthMonster != null)
        {
            targetHealthMonster.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
        }
        else
        {
            Health targetHealth = targetObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
            }
        }
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if (targetCore != null)
        {
            CharacterStats targetStats = targetCore.GetComponent<CharacterStats>();
            if (targetStats != null)
            {
                targetStats.ApplySlow(slowPercentage, slowDuration);
                // Applied slow to player
            }
        }
        else if (targetMonster != null)
        {
            targetMonster.ReceiveControlEffect(ControlEffectType.Slow, slowDuration, Mathf.RoundToInt(slowPercentage * 100));
            // Applied slow to monster
        }
        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        caster.GetComponent<PlayerSkills>().RpcApplySlowEffect(targetNetId, slowDuration, _skillName);
    }

    public void SpawnProjectile(Vector3 startPos, Vector3 targetPos, PlayerSkills playerSkills)
    {
        if (projectilePrefab != null)
        {
            GameObject projectile = Object.Instantiate(projectilePrefab, startPos + Vector3.up * 1f, Quaternion.identity);
            // ������ ��������
        }
    }

    public void ApplySlowEffect(GameObject target, float duration, PlayerSkills playerSkills)
    {
        // Applying VFX for slow
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