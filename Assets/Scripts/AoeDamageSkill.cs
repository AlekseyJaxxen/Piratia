using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewAoeDamageSkill", menuName = "Skills/AoeDamageSkill")]
public class AoeDamageSkill : SkillBase
{
    [Header("AOE Damage Skill Specifics")]
    public int damage = 50;
    public float aoeRadius = 5f;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("[AoeDamageSkill] Target position is null");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[AoeDamageSkill] PlayerSkills component missing on caster");
            return;
        }

        Debug.Log($"[AoeDamageSkill] Attempting to AOE damage at position: {targetPosition.Value}");

        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, aoeRadius, caster.interactableLayers);
        foreach (Collider col in hitColliders)
        {
            Health targetHealth = col.GetComponent<Health>();
            if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                if (targetCore != null && targetCore.team != caster.team)
                {
                    targetHealth.TakeDamage(damage, SkillDamageType, false, caster.netIdentity);
                }
            }
        }
        caster.GetComponent<PlayerSkills>().RpcPlayAoeDamage(targetPosition.Value, _skillName);
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