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
        // Attempting AOE damage
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats == null) return;
        int totalBaseDamage;
        if (SkillDamageType == DamageType.Physical)
        {
            int randomAttack = Random.Range(stats.minAttack, stats.maxAttack + 1);
            totalBaseDamage = baseDamage + randomAttack; // ��������� ��� ���������
        }
        else
        {
            totalBaseDamage = baseDamage + Mathf.RoundToInt(stats.spirit);
        }
        // ��������� ����� �����, ����� �������� "Player" � "Ignore Raycast"
        int aoeLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Monster", "Enemy");
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, aoeRadius, aoeLayerMask);
        foreach (Collider col in hitColliders)
        {
            // Try HealthMonster first for monsters, then fall back to Health
            HealthMonster targetHealthMonster = col.GetComponent<HealthMonster>();
            Health targetHealth = col.GetComponent<Health>();
            
            if (targetHealthMonster != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                Monster targetMonster = col.GetComponent<Monster>();
                if (targetCore != null && targetCore.team != caster.team)
                {
                    targetHealthMonster.TakeDamage(totalBaseDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
                }
                else if (targetMonster != null)
                {
                    targetHealthMonster.TakeDamage(totalBaseDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
                }
            }
            else if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                Monster targetMonster = col.GetComponent<Monster>();
                if (targetCore != null && targetCore.team != caster.team)
                {
                    targetHealth.TakeDamage(totalBaseDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
                }
                else if (targetMonster != null)
                {
                    targetHealth.TakeDamage(totalBaseDamage, SkillDamageType, false, caster.netIdentity, damageMultiplier);
                }
            }
        }
        caster.GetComponent<PlayerSkills>().RpcPlayAoeDamage(targetPosition.Value, _skillName);
    }

    public void PlayEffect(Vector3 position, PlayerCore caster)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, position, Quaternion.identity);
            Object.Destroy(effect, 2f);
            // Effect spawned at cast point
        }
    }
}