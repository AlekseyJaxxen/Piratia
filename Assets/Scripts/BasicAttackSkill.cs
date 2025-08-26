using UnityEngine;
using System.Collections;
using Mirror;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public GameObject vfxPrefab;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;

    [Header("Critical Hit Settings")]
    public GameObject criticalHitVfxPrefab;
    public GameObject impactEffectPrefab;
    public Color criticalHitColor = Color.yellow;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] Target object is null for skill {_skillName}");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] Target {targetObject.name} has no NetworkIdentity for skill {_skillName}");
            return;
        }

        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[BasicAttackSkill] Not enough mana for skill {_skillName}: {stats.currentMana}/{ManaCost}");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] PlayerSkills component missing on caster for skill {_skillName}");
            return;
        }

        Debug.Log($"[BasicAttackSkill] Client requesting attack for skill {_skillName} on target: {targetObject.name}, netId: {targetIdentity.netId}, strength: {stats.strength}, minAttack: {stats.minAttack}, maxAttack: {stats.maxAttack}");
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, 0); // Некотрольный скилл, weight = 0
    }

    public void PlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical)
    {
        if (vfxPrefab != null)
        {
            Quaternion xRotation = Quaternion.Euler(90, 0, 0);
            Quaternion finalRotation = startRotation * xRotation;
            GameObject vfxInstance = Instantiate(vfxPrefab, startPosition, finalRotation);
            if (isCritical && vfxInstance.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = criticalHitColor;
            }
            Destroy(vfxInstance, 0.2f);
        }

        if (isCritical && criticalHitVfxPrefab != null)
        {
            GameObject critVfx = Instantiate(criticalHitVfxPrefab, startPosition, startRotation);
            Destroy(critVfx, 1f);
        }

        if (projectilePrefab != null)
        {
            GameObject projectileInstance = Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            if (isCritical && projectileInstance.TryGetComponent<Renderer>(out var projectileRenderer))
            {
                projectileRenderer.material.color = criticalHitColor;
                projectileInstance.transform.localScale *= 1.3f;
            }
            StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition, isCritical));
        }
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 start, Vector3 end, bool isCritical)
    {
        float actualSpeed = isCritical ? projectileSpeed * 1.5f : projectileSpeed;
        while (projectile != null && Vector3.Distance(projectile.transform.position, end) > 0.1f)
        {
            projectile.transform.position = Vector3.MoveTowards(
                projectile.transform.position,
                end,
                actualSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (projectile != null)
        {
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, projectile.transform.position, Quaternion.identity);
                if (isCritical && impact.TryGetComponent<Renderer>(out var impactRenderer))
                {
                    impactRenderer.material.color = criticalHitColor;
                    impact.transform.localScale *= 1.5f;
                }
                Destroy(impact, isCritical ? 2f : 1f);
            }
            Destroy(projectile);
        }
    }
}