using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewHealingSkill", menuName = "Skills/HealingSkill")]
public class HealingSkill : SkillBase
{
    [Header("Healing Skill Specifics")]
    public int healAmount = 50;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        // ��������, ���� ��� ����� SelfBuff
        if (SkillCastType == CastType.SelfBuff)
        {
            CharacterStats casterStats = caster.GetComponent<CharacterStats>();
            if (casterStats != null && !casterStats.HasEnoughMana(ManaCost))
            {
                Debug.LogWarning($"[HealingSkill] Not enough mana: {casterStats.currentMana}/{ManaCost}");
                return;
            }

            PlayerSkills casterSkills = caster.GetComponent<PlayerSkills>();
            Debug.Log($"[HealingSkill] Healing self: {caster.name}, netId: {caster.netId}");
            casterSkills.CmdExecuteSkill(caster, null, caster.netId, _skillName, 0);
            // Кулдаун уже устанавливается в CmdExecuteSkill, дублировать не нужно
            return;
        }

        // ������������ ������ ��� ������ ����� �����
        if (targetObject == null)
        {
            Debug.LogWarning("[HealingSkill] Target object is null");
            return;
        }

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        if (targetCore == null)
        {
            // Try to get PlayerCore from parent (for reviveCollider)
            targetCore = targetObject.GetComponentInParent<PlayerCore>();
        }
        if (targetCore == null || !IsAlly(targetCore, caster))
        {
            Debug.LogWarning("[HealingSkill] Invalid target: not ally or self");
            return;
        }
        // Don't allow healing dead players
        if (targetCore.isDead)
        {
            Debug.LogWarning("[HealingSkill] Cannot heal dead player");
            return;
        }

        float distance = Vector3.Distance(caster.transform.position, targetObject.transform.position);
        if (distance > Range)
        {
            Debug.LogWarning($"[HealingSkill] Target {targetObject.name} is out of range: {distance} > {Range}");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            // Try to get NetworkIdentity from parent (for reviveCollider)
            targetIdentity = targetObject.GetComponentInParent<NetworkIdentity>();
        }
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[HealingSkill] Target {targetObject.name} has no NetworkIdentity");
            return;
        }

        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[HealingSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        Debug.Log($"[HealingSkill] Attempting to heal target: {targetObject.name}, netId: {targetIdentity.netId}");
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, 0);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (targetObject == null) return;

        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.Heal(healAmount);
        }

        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        caster.GetComponent<PlayerSkills>().RpcPlayHealingSkill(targetNetId, _skillName);
    }

    public void PlayEffect(GameObject target)
    {
        if (effectPrefab != null)
        {
            Object.Instantiate(effectPrefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
        }
    }
    
    /// <summary>
    /// Checks if target player is an ally
    /// Solo players are never allies to each other (except themselves)
    /// </summary>
    private bool IsAlly(PlayerCore target, PlayerCore caster)
    {
        if (target == null) return false;
        
        // A player is always an ally to themselves
        if (caster == target)
        {
            return true;
        }
        
        // Solo players are never allies to each other
        if (caster.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return false; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return caster.team == target.team;
    }
}