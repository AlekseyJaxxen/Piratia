using UnityEngine;
using Mirror;
using System.Collections;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public int damageAmount = 10;
    public GameObject vfxPrefab;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;

    [Header("Critical Hit Settings")]
    public GameObject criticalHitVfxPrefab;
    public GameObject impactEffectPrefab;
    public Color criticalHitColor = Color.yellow;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned)
        {
            Debug.Log("Target is null or not owned");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.Log("Target has no NetworkIdentity");
            return;
        }

        // Клиентская проверка маны для UI
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.Log("Not enough mana to cast skill!");
            return;
        }

        Debug.Log($"Attempting to attack target: {targetObject.name}, netId: {targetIdentity.netId}");

        CmdPerformAttack(caster.transform.position, caster.transform.rotation, targetIdentity.netId);
    }

    [Command]
    private void CmdPerformAttack(Vector3 casterPosition, Quaternion casterRotation, uint targetNetId)
    {
        Debug.Log($"Server received attack command for target netId: {targetNetId}");

        // Серверная проверка маны
        CharacterStats stats = connectionToClient.identity.GetComponent<CharacterStats>();
        if (stats != null && !stats.ConsumeMana(ManaCost))
        {
            Debug.Log("Not enough mana on server!");
            return;
        }

        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore attackerCore = connectionToClient.identity.GetComponent<PlayerCore>();

            if (targetHealth != null && targetCore != null && attackerCore != null)
            {
                if (attackerCore.team != targetCore.team)
                {
                    // Добавляем логику критического удара
                    CharacterStats attackerStats = attackerCore.GetComponent<CharacterStats>();
                    bool isCritical = false;
                    int finalDamage = damageAmount;

                    if (attackerStats != null)
                    {
                        // Проверяем шанс крита
                        float randomValue = Random.Range(0f, 100f);
                        isCritical = randomValue <= attackerStats.criticalHitChance;

                        if (isCritical)
                        {
                            finalDamage = Mathf.RoundToInt(damageAmount * attackerStats.criticalHitMultiplier);
                            Debug.Log($"CRITICAL HIT! Damage: {finalDamage} (base: {damageAmount}, multiplier: {attackerStats.criticalHitMultiplier}x)");
                        }
                    }

                    Debug.Log($"Server: Applying {(isCritical ? "CRITICAL " : "")}damage {finalDamage} to {targetCore.playerName}");
                    targetHealth.TakeDamage(finalDamage, SkillDamageType, isCritical);
                    RpcPlayVFX(casterPosition, casterRotation, targetIdentity.transform.position, isCritical);
                }
                else
                {
                    Debug.Log("Cannot attack a teammate!");
                }
            }
            else
            {
                Debug.Log("Missing components on target");
            }
        }
        else
        {
            Debug.Log($"Target with netId {targetNetId} not found on server");
        }
    }

    [ClientRpc]
    private void RpcPlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical)
    {
        // Обычный VFX эффект
        if (vfxPrefab != null)
        {
            Quaternion xRotation = Quaternion.Euler(90, 0, 0);
            Quaternion finalRotation = startRotation * xRotation;
            GameObject vfxInstance = Instantiate(vfxPrefab, startPosition, finalRotation);

            // Если это крит, меняем цвет эффекта
            if (isCritical && vfxInstance.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = criticalHitColor;
            }

            Destroy(vfxInstance, 0.2f);
        }

        // Дополнительный VFX для критов
        if (isCritical && criticalHitVfxPrefab != null)
        {
            GameObject critVfx = Instantiate(criticalHitVfxPrefab, startPosition, startRotation);
            Destroy(critVfx, 1f);
        }

        // Снаряд
        if (projectilePrefab != null)
        {
            GameObject projectileInstance = Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));

            // Если это крит, меняем внешний вид снаряда
            if (isCritical && projectileInstance.TryGetComponent<Renderer>(out var projectileRenderer))
            {
                projectileRenderer.material.color = criticalHitColor;
                // Увеличиваем размер снаряда для критов
                projectileInstance.transform.localScale *= 1.3f;
            }

            StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition, isCritical));
        }
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 start, Vector3 end, bool isCritical)
    {
        // Увеличиваем скорость снаряда для критов
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
            // Эффект при попадании
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, projectile.transform.position, Quaternion.identity);

                // Если это крит, меняем цвет эффекта попадания
                if (isCritical && impact.TryGetComponent<Renderer>(out var impactRenderer))
                {
                    impactRenderer.material.color = criticalHitColor;
                    // Увеличиваем размер эффекта для критов
                    impact.transform.localScale *= 1.5f;
                }

                Destroy(impact, isCritical ? 2f : 1f); // Более долгий эффект для критов
            }

            Destroy(projectile);
        }
    }
}