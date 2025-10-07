using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewAreaOfEffectStunSkill", menuName = "Skills/AreaOfEffectStunSkill")]
public class AreaOfEffectStunSkill : SkillBase
{
    [Header("AOE Stun Skill Specifics")]
    public float stunDuration = 2f;
    public GameObject effectPrefab;
    public float aoeRadius = 5f;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("[AreaOfEffectStunSkill] Target position is null");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[AreaOfEffectStunSkill] PlayerSkills component missing on caster");
            return;
        }
        
        Debug.Log($"[AreaOfEffectStunSkill] Attempting to AOE stun at position: {targetPosition.Value}, weight: {Weight}");
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        int aoeLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Enemy");
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, aoeRadius, aoeLayerMask);
        foreach (Collider col in hitColliders)
        {
            PlayerCore targetCore = col.GetComponent<PlayerCore>();
            Monster targetMonster = col.GetComponent<Monster>();
            if (targetCore != null && IsEnemy(targetCore, caster) && !targetCore.isDead)
            {
                targetCore.ApplyControlEffect(ControlEffectType.Stun, stunDuration, weight);
                Debug.Log($"[AreaOfEffectStunSkill] Applied stun to player {targetCore.gameObject.name}, duration={stunDuration}, weight={weight}");
            }
            else if (targetMonster != null && targetMonster.gameObject.CompareTag("Enemy"))
            {
                targetMonster.ReceiveControlEffect(ControlEffectType.Stun, stunDuration, weight);
                Debug.Log($"[AreaOfEffectStunSkill] Applied stun to monster {targetMonster.monsterName}, duration={stunDuration}, weight={weight}");
            }
        }
        caster.GetComponent<PlayerSkills>().RpcPlayAoeStun(targetPosition.Value, _skillName);
    }

    public void PlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Object.Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// Checks if target player is an enemy
    /// Supports dynamic teams: guild, party, faction, and basic teams
    /// </summary>
    private bool IsEnemy(PlayerCore target, PlayerCore caster)
    {
        if (target == null) return false;
        
        // A player is never an enemy to themselves
        if (caster == target)
        {
            return false;
        }
        
        // Check party membership first (highest priority)
        if (!string.IsNullOrEmpty(caster.partyId) && !string.IsNullOrEmpty(target.partyId) && 
            caster.partyId == target.partyId)
        {
            return false; // Party members are never enemies
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(caster.guildId) && !string.IsNullOrEmpty(target.guildId) && 
            caster.guildId == target.guildId)
        {
            return false; // Guild members are never enemies
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(caster.factionId) && !string.IsNullOrEmpty(target.factionId) && 
            caster.factionId == target.factionId)
        {
            return false; // Faction members are never enemies
        }
        
        // Check basic team logic
        if (caster.team == target.team && caster.team != PlayerTeam.Solo)
        {
            return false; // Same team members are not enemies
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (caster.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return true; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return caster.team != target.team;
    }
}