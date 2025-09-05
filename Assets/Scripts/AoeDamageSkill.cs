using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewAoeDamageSkill", menuName = "Skills/AoeDamageSkill")]
public class AoeDamageSkill : SkillBase
{
    [Header("AOE Damage Skill Specifics")]
    public int baseDamage = 50;
    public float damageMultiplier = 1f;
    public float aoeRadius = 5f;
    public GameObject effectPrefab;
    [Header("VFX Settings")]
    public float forwardOffset = 2f;

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

        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, aoeRadius, caster.interactableLayers);
        foreach (Collider col in hitColliders)
        {
            Health targetHealth = col.GetComponent<Health>();
            if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                Monster targetMonster = col.GetComponent<Monster>();
                if (targetCore != null && targetCore.team != caster.team)
                {
                    targetHealth.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity);
                }
                else if (targetMonster != null)
                {
                    targetHealth.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity);
                }
            }
        }
        caster.GetComponent<PlayerSkills>().RpcPlayAoeDamage(targetPosition.Value, _skillName);
    }

    public void PlayEffect(Vector3 position, PlayerCore caster)
    {
        if (effectPrefab != null)
        {
            Vector3 effectPosition = caster.transform.position + caster.transform.forward * forwardOffset;
            Quaternion effectRotation = Quaternion.LookRotation(caster.transform.forward);
            GameObject effect = Object.Instantiate(effectPrefab, effectPosition, effectRotation);
            Object.Destroy(effect, 2f);
            Debug.Log($"[AoeDamageSkill] Effect spawned at {effectPosition}, facing {caster.transform.forward}");
        }
    }
}