using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewTargetedRecoverySkill", menuName = "Skills/TargetedRecoverySkill")]
public class TargetedRecoverySkill : SkillBase
{
    [Header("Recovery Skill Specifics")]
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[TargetedRecoverySkill] Target object is null");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[TargetedRecoverySkill] PlayerSkills component missing on caster");
            return;
        }
        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning("[TargetedRecoverySkill] Target object has no NetworkIdentity");
            return;
        }
        Debug.Log($"[TargetedRecoverySkill] Attempting to recover target: {targetObject.name}, weight: {Weight}");
        skills.CmdExecuteSkill(caster, null, targetIdentity.netId, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[TargetedRecoverySkill] Target object is null on server");
            return;
        }
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if (targetCore != null && IsAlly(targetCore, caster))
        {
            targetCore.ClearNegativeEffectsExceptStun();
        }
        else if (targetMonster != null)
        {
            // targetMonster.ClearNegativeEffectsExceptStun();
        }
        caster.GetComponent<PlayerSkills>().RpcPlayTargetedRecovery(targetObject.GetComponent<NetworkIdentity>().netId, _skillName);
    }

    public void PlayEffect(GameObject target, PlayerSkills skills)
    {
        if (effectPrefab != null && target != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
            Object.Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// Checks if target player is an ally
    /// Supports dynamic teams: guild, party, faction, and basic teams
    /// </summary>
    private bool IsAlly(PlayerCore target, PlayerCore caster)
    {
        if (target == null) return false;
        
        // A player is always an ally to themselves
        if (caster == target)
        {
            return true;
        }
        
        // Check basic team logic first
        if (caster.team == target.team && caster.team != PlayerTeam.Solo)
        {
            return true;
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(caster.guildId) && caster.guildId == target.guildId)
        {
            return true;
        }
        
        // Check party membership
        if (!string.IsNullOrEmpty(caster.partyId) && caster.partyId == target.partyId)
        {
            return true;
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(caster.factionId) && caster.factionId == target.factionId)
        {
            return true;
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (caster.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return false;
        }
        
        return false;
    }
}