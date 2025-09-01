using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewAreaOfEffectHealSkill", menuName = "Skills/AreaOfEffectHealSkill")]
public class AreaOfEffectHealSkill : SkillBase
{
    [Header("AOE Heal Skill Specifics")]
    public int healAmount = 100;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("[AreaOfEffectHealSkill] Target position is null");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[AreaOfEffectHealSkill] PlayerSkills component missing on caster");
            return;
        }

        Debug.Log($"[AreaOfEffectHealSkill] Attempting to AOE heal at position: {targetPosition.Value}");

        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, EffectRadius, caster.interactableLayers);
        foreach (Collider col in hitColliders)
        {
            Health targetHealth = col.GetComponent<Health>();
            if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                if (targetCore != null && targetCore.team == caster.team)
                {
                    targetHealth.Heal(healAmount);
                }
            }
        }

        caster.GetComponent<PlayerSkills>().RpcPlayAoeHeal(targetPosition.Value, _skillName);
    }

    public void PlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Object.Destroy(effect, 2f);
        }
    }
}