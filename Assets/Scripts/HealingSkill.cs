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
        if (targetObject == null)
        {
            Debug.LogWarning("[HealingSkill] Target object is null");
            return;
        }

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        if (targetCore == null || targetCore.team != caster.team)
        {
            Debug.LogWarning("[HealingSkill] Invalid target: not ally or self");
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
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
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
           // Object.Destroy(effectPrefab, 2f);
        }
    }
}