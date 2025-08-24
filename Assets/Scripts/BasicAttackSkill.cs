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
                    Debug.Log($"Server: Applying damage to {targetCore.playerName}");
                    targetHealth.TakeDamage(damageAmount, SkillDamageType);
                    RpcPlayVFX(casterPosition, casterRotation, targetIdentity.transform.position);
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
    private void RpcPlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition)
    {
        if (vfxPrefab != null)
        {
            Quaternion xRotation = Quaternion.Euler(90, 0, 0);
            Quaternion finalRotation = startRotation * xRotation;
            GameObject vfxInstance = Instantiate(vfxPrefab, startPosition, finalRotation);
            Destroy(vfxInstance, 0.2f);
        }

        if (projectilePrefab != null)
        {
            GameObject projectileInstance = Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition));
        }
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 start, Vector3 end)
    {
        while (projectile != null && Vector3.Distance(projectile.transform.position, end) > 0.1f)
        {
            projectile.transform.position = Vector3.MoveTowards(
                projectile.transform.position,
                end,
                projectileSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }
}