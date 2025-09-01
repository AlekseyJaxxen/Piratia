using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewSlowSkill", menuName = "Skills/SlowSkill")]
public class SlowSkill : SkillBase
{
    [Header("Slow Skill Specifics")]
    public int baseDamage = 10;
    public float slowPercentage = 0.5f;
    public float slowDuration = 5f;
    public const float DAMAGE_MULTIPLIER = 1.5f;
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
        if ((targetCore == null || targetCore.team == caster.team) && targetMonster == null)
        {
            Debug.LogWarning("[SlowSkill] Invalid target: not enemy");
            return;
        }

        float distance = Vector3.Distance(caster.transform.position, targetObject.transform.position);
        if (distance > Range)
        {
            Debug.LogWarning($"[SlowSkill] Target {targetObject.name} is out of range: {distance} > {Range}");
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
        Debug.Log($"[SlowSkill] Attempting to slow target: {targetObject.name}, netId: {targetIdentity.netId}");
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        int damage = Mathf.RoundToInt(baseDamage * DAMAGE_MULTIPLIER);
        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, SkillDamageType, false, caster.netIdentity);
        }

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        if (targetCore != null)
        {
            targetCore.ApplySlow(slowPercentage, slowDuration, weight);
        }

        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        caster.GetComponent<PlayerSkills>().RpcApplySlowEffect(targetNetId, slowDuration, _skillName);
    }

    public void SpawnProjectile(Vector3 startPos, Vector3 targetPos, PlayerSkills playerSkills)
    {
        if (projectilePrefab != null)
        {
            GameObject projectile = Object.Instantiate(projectilePrefab, startPos + Vector3.up * 1f, Quaternion.identity);
            // Логика движения
        }
    }

    public void ApplySlowEffect(GameObject target, float duration, PlayerSkills playerSkills)
    {
        // VFX для slow
    }
}