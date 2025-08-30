using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewMonsterBasicAttackSkill", menuName = "Skills/MonsterBasicAttackSkill")]
public class MonsterBasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public GameObject vfxPrefab;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    [Header("Critical Hit Settings")]
    public GameObject criticalHitVfxPrefab;
    public GameObject impactEffectPrefab;
    public Color criticalHitColor = Color.yellow;
    [Header("Monster Attack Settings")]
    public int baseDamage = 10;
    public float criticalChance = 0.1f; // 10% chance for critical hit
    public float criticalMultiplier = 1.5f;

    public void Execute(Monster caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (caster == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Monster component missing on caster for skill {_skillName}");
            return;
        }

        if (targetObject == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Target object is null for skill {_skillName}");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Target {targetObject.name} has no NetworkIdentity for skill {_skillName}");
            return;
        }

        bool isCritical = Random.value < criticalChance;
        int damage = isCritical ? Mathf.RoundToInt(baseDamage * criticalMultiplier) : baseDamage;

        Debug.Log($"[MonsterBasicAttackSkill] Monster requesting attack for skill {_skillName} on target: {targetObject.name}, netId: {targetIdentity.netId}, damage: {damage}, isCritical: {isCritical}");

        // Вызываем метод в Monster для обработки сетевой атаки
        caster.ExecuteAttack(targetIdentity.netId, _skillName, damage, isCritical);
    }

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        // Этот метод оставлен для совместимости с SkillBase, но не используется напрямую
        Debug.LogWarning($"[MonsterBasicAttackSkill] ExecuteSkillImplementation called with PlayerCore, redirecting to Monster logic");
        Monster monster = caster.GetComponent<Monster>();
        if (monster != null)
        {
            Execute(monster, targetPosition, targetObject);
        }
    }

    public void PlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical, Monster monster)
    {
        if (vfxPrefab != null)
        {
            Quaternion xRotation = Quaternion.Euler(0, 0, 0);
            Quaternion finalRotation = startRotation * xRotation;
            GameObject vfxInstance = Object.Instantiate(vfxPrefab, startPosition, finalRotation);
            if (isCritical && vfxInstance.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = criticalHitColor;
            }
            Object.Destroy(vfxInstance, 0.2f);
        }
        if (isCritical && criticalHitVfxPrefab != null)
        {
            GameObject critVfx = Object.Instantiate(criticalHitVfxPrefab, startPosition, startRotation);
            Object.Destroy(critVfx, 1f);
        }
        if (projectilePrefab != null)
        {
            GameObject projectileInstance = Object.Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            if (isCritical && projectileInstance.TryGetComponent<Renderer>(out var projectileRenderer))
            {
                projectileRenderer.material.color = criticalHitColor;
                projectileInstance.transform.localScale *= 1.3f;
            }
            monster.StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition, isCritical));
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
                GameObject impact = Object.Instantiate(impactEffectPrefab, projectile.transform.position, Quaternion.identity);
                if (isCritical && impact.TryGetComponent<Renderer>(out var impactRenderer))
                {
                    impactRenderer.material.color = criticalHitColor;
                    impact.transform.localScale *= 1.5f;
                }
                Object.Destroy(impact, isCritical ? 2f : 1f);
            }
            Object.Destroy(projectile);
        }
    }
}