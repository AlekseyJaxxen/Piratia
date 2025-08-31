using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewProjectileDamageSkill", menuName = "Skills/ProjectileDamageSkill")]
public class ProjectileDamageSkill : SkillBase
{
    [Header("Projectile Damage Skill Specifics")]
    public int damageAmount = 15;
    public GameObject projectilePrefab;
    public GameObject impactEffectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[ProjectileDamageSkill] Target object is null");
            return;
        }
        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[ProjectileDamageSkill] Target {targetObject.name} has no NetworkIdentity");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[ProjectileDamageSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        Debug.Log($"[ProjectileDamageSkill] Attempting to projectile attack target: {targetObject.name}, netId: {targetIdentity.netId}");
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, 0); // Некотрольный скилл, weight = 0
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
}