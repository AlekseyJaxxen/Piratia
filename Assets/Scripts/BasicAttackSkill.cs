using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewBasicAttackSkill", menuName = "Skills/BasicAttackSkill")]
public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public GameObject vfxPrefab;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    [Header("VFX Position Settings")]
    public Vector3 vfxStartOffset = new Vector3(0f, 1.5f, 0f); // —мещение начальной позиции дл€ снар€да
    public Vector3 vfxTargetOffset = new Vector3(0f, 1.0f, 0f); // —мещение позиции VFX на цели
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
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if ((targetCore == null || targetCore.team == caster.team) && targetMonster == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] Invalid target for {_skillName}");
            return;
        }
        float distance = Vector3.Distance(caster.transform.position, targetObject.transform.position);
        if (distance > Range)
        {
            Debug.LogWarning($"[BasicAttackSkill] Target {targetObject.name} is out of range: {distance} > {Range}");
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
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, 0);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
        caster.GetComponent<PlayerAnimation>().PlayAttackAnimation();
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        int damage = Random.Range(stats.minAttack, stats.maxAttack + 1);
        bool isCritical = stats.TryCriticalHit();
        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, SkillDamageType, isCritical, caster.netIdentity);
        }
        Vector3 startPos = caster.transform.position + vfxStartOffset;
        Quaternion startRot = caster.transform.rotation;
        Vector3 targetPos = targetObject.transform.position + vfxTargetOffset;
        caster.GetComponent<PlayerSkills>().RpcPlayBasicAttackVFX(startPos, startRot, targetPos, isCritical, _skillName);
    }

    public void PlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical, PlayerSkills playerSkills)
    {
        if (vfxPrefab != null)
        {
            // VFX по€вл€етс€ на цели
            GameObject vfxInstance = Object.Instantiate(vfxPrefab, endPosition, Quaternion.identity);
            if (isCritical && vfxInstance.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = criticalHitColor;
            }
            Object.Destroy(vfxInstance, 0.2f);
            Debug.Log($"[BasicAttackSkill] Spawned VFX at target position: {endPosition}");
        }
        if (isCritical && criticalHitVfxPrefab != null)
        {
            //  ритический VFX также на цели
            GameObject critVfx = Object.Instantiate(criticalHitVfxPrefab, endPosition, Quaternion.identity);
            Object.Destroy(critVfx, 1f);
            Debug.Log($"[BasicAttackSkill] Spawned critical VFX at target position: {endPosition}");
        }
        if (projectilePrefab != null)
        {
            // —нар€д движетс€ от персонажа к цели
            GameObject projectileInstance = Object.Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            if (isCritical && projectileInstance.TryGetComponent<Renderer>(out var projectileRenderer))
            {
                projectileRenderer.material.color = criticalHitColor;
                projectileInstance.transform.localScale *= 1.3f;
            }
            playerSkills.StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition, isCritical));
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
                // Ёффект попадани€ на цели
                GameObject impact = Object.Instantiate(impactEffectPrefab, end, Quaternion.identity);
                if (isCritical && impact.TryGetComponent<Renderer>(out var impactRenderer))
                {
                    impactRenderer.material.color = criticalHitColor;
                    impact.transform.localScale *= 1.5f;
                }
                Object.Destroy(impact, isCritical ? 2f : 1f);
                Debug.Log($"[BasicAttackSkill] Spawned impact effect at target position: {end}");
            }
            Object.Destroy(projectile);
        }
    }
}