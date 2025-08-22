
using UnityEngine;
using Mirror;
using System.Collections;

public class ProjectileDamageSkill : SkillBase
{
    [Header("Projectile Damage Skill Specifics")]
    public int damageAmount = 15;
    public GameObject projectilePrefab;
    public GameObject impactEffectPrefab;

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;

        CmdSpawnProjectileAndDamage(player.transform.position, targetObject.transform.position, targetObject.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    private void CmdSpawnProjectileAndDamage(Vector3 startPos, Vector3 targetPos, uint targetNetId)
    {
        RpcSpawnProjectile(startPos, targetPos);

        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

            // ѕровер€ем, что цель не принадлежит нашей команде.
            if (targetHealth != null && targetCore != null && casterCore != null)
            {
                if (casterCore.team != targetCore.team)
                {
                    targetHealth.TakeDamage(damageAmount);
                }
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