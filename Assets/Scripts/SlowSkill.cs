using UnityEngine;
using Mirror;
using System.Collections;

public class SlowSkill : SkillBase
{
    [Header("Slow Skill Specifics")]
    public float slowPercentage = 0.5f; // Процент замедления (0.5 = 50%)
    public float slowDuration = 3.0f; // Длительность замедления в секундах

    public int baseDamage = 10;
    private const float DAMAGE_MULTIPLIER = 1.5f;

    public GameObject projectilePrefab;
    public GameObject impactEffectPrefab;
    public GameObject slowEffectPrefab; // Добавлено: Префаб визуального эффекта замедления

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;

        CmdApplySlowAndDamage(player.transform.position, targetObject.transform.position, targetObject.GetComponent<NetworkIdentity>().netId, baseDamage, slowPercentage, slowDuration);
    }

    [Command]
    private void CmdApplySlowAndDamage(Vector3 startPos, Vector3 targetPos, uint targetNetId, int damage, float slowPercent, float slowDuration)
    {
        RpcSpawnProjectile(startPos, targetPos);

        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null && casterCore.team != targetCore.team)
            {
                Health targetHealth = targetIdentity.GetComponent<Health>();
                if (targetHealth != null)
                {
                    int finalDamage = Mathf.RoundToInt(damage * DAMAGE_MULTIPLIER);
                    targetHealth.TakeDamage(finalDamage);
                    Debug.Log($"Нанесено {finalDamage} урона. Базовый урон: {damage}");
                }

                targetCore.ApplySlow(slowPercent, slowDuration);

                // Добавлено: Вызываем RPC для отображения VFX на клиентах
                RpcApplySlowEffect(targetNetId, slowDuration);
            }
        }
    }

    [ClientRpc]
    private void RpcSpawnProjectile(Vector3 startPos, Vector3 targetPos)
    {
        if (projectilePrefab != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, startPos + Vector3.up * 1f, Quaternion.identity);
            StartCoroutine(MoveProjectile(projectile, targetPos + Vector3.up * 1f));
        }
    }

    // Добавлено: RPC метод для отображения VFX
    [ClientRpc]
    private void RpcApplySlowEffect(uint targetNetId, float duration)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId) && slowEffectPrefab != null)
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            // Запускаем корутину для управления эффектом
            StartCoroutine(ManageSlowEffect(targetIdentity.gameObject, duration));
        }
    }

    // Добавлено: Корутина для управления жизненным циклом VFX
    private IEnumerator ManageSlowEffect(GameObject target, float duration)
    {
        GameObject effectInstance = Instantiate(slowEffectPrefab, target.transform);
        effectInstance.transform.localPosition = Vector3.zero; // Размещаем эффект относительно цели

        yield return new WaitForSeconds(duration);

        if (effectInstance != null)
        {
            Destroy(effectInstance);
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