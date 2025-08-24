using UnityEngine;
using Mirror;
using System.Collections;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public int damageAmount = 10;

    // ���������: ������ ����������� �������
    public GameObject vfxPrefab;

    // ���������: ������ ������� � ��� ��������
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;

    // ���� ����� ���������� �� �������
    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned) return;

        // ���������� ������� �� ������ ��� ���������� �����. �������� ������� ������.
        CmdPerformAttack(caster.transform.position, caster.transform.rotation, targetObject.GetComponent<NetworkIdentity>().netId);
    }

    // [Command]-�����, ������� ����������� �� �������
    [Command(requiresAuthority = false)]
    private void CmdPerformAttack(Vector3 casterPosition, Quaternion casterRotation, uint targetNetId)
    {
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore attackerCore = connectionToClient.identity.GetComponent<PlayerCore>();

            if (targetHealth != null && targetCore != null && attackerCore != null)
            {
                if (attackerCore.team != targetCore.team)
                {
                    targetHealth.TakeDamage(damageAmount);
                    // �������� Rpc ����� ��� ��������������� VFX �� ���� ��������, ��������� �������.
                    RpcPlayVFX(casterPosition, casterRotation, targetIdentity.transform.position);
                }
                else
                {
                    Debug.Log("Cannot attack a teammate!");
                }
            }
        }
    }

    // [ClientRpc]-�����, ������� ����������� �� ���� ��������
    [ClientRpc]
    private void RpcPlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition)
    {
        if (vfxPrefab != null)
        {
            // ������� �������������� ������� �� 90 �������� �� ��� X
            Quaternion xRotation = Quaternion.Euler(90, 0, 0);

            // ���������� ������� ������ � ����� ���������
            Quaternion finalRotation = startRotation * xRotation;

            // ������� ������ � �������� ���������
            GameObject vfxInstance = Instantiate(vfxPrefab, startPosition, finalRotation);

            Destroy(vfxInstance, 0.2f);
        }

        if (projectilePrefab != null)
        {
            // ������� ������ � ��������� ��� ��������
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