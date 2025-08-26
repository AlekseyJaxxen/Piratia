using UnityEngine;
using System.Collections;
using Mirror;

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
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName);
    }

    public void SpawnProjectile(Vector3 startPos, Vector3 targetPos)
    {
        if (projectilePrefab != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, startPos + Vector3.up * 1f, Quaternion.identity);
            StartCoroutine(MoveProjectile(projectile, targetPos + Vector3.up * 1f));
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
            GameObject impact = Instantiate(impactEffectPrefab, projectile.transform.position, Quaternion.identity);
            Destroy(impact, 2f);
        }
        Destroy(projectile);
    }
}