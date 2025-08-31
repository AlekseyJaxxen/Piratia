using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewSlowSkill", menuName = "Skills/SlowSkill")]
public class SlowSkill : SkillBase
{
    [Header("Slow Skill Specifics")]
    public float slowPercentage = 0.5f;
    public float slowDuration = 3.0f;
    public int baseDamage = 10;
    public const float DAMAGE_MULTIPLIER = 1.5f;
    public GameObject projectilePrefab;
    public GameObject impactEffectPrefab;
    public GameObject slowEffectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[SlowSkill] Target object is null");
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
        Debug.Log($"[SlowSkill] Attempting to slow target: {targetObject.name}, netId: {targetIdentity.netId}, weight: {Weight}");
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public void SpawnProjectile(Vector3 startPos, Vector3 targetPos, PlayerSkills playerSkills)
    {
        if (projectilePrefab != null)
        {
            GameObject projectile = Object.Instantiate(projectilePrefab, startPos + Vector3.up * 1f, Quaternion.identity);
            playerSkills.StartCoroutine(MoveProjectile(projectile, targetPos + Vector3.up * 1f));
        }
    }

    public void ApplySlowEffect(GameObject target, float duration, PlayerSkills playerSkills)
    {
        if (slowEffectPrefab != null)
        {
            playerSkills.StartCoroutine(ManageSlowEffect(target, duration));
        }
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 targetPos)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = projectile.transform.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            projectile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        if (impactEffectPrefab != null)
        {
            GameObject impact = Object.Instantiate(impactEffectPrefab, projectile.transform.position, Quaternion.identity);
            Object.Destroy(impact, 2f);
        }
        Object.Destroy(projectile);
    }

    private IEnumerator ManageSlowEffect(GameObject target, float duration)
    {
        GameObject effectInstance = Object.Instantiate(slowEffectPrefab, target.transform);
        effectInstance.transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(duration);
        if (effectInstance != null)
        {
            Object.Destroy(effectInstance);
        }
    }
}