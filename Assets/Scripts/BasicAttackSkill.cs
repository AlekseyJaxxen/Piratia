using UnityEngine;
using Mirror;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public int damageAmount = 10;

    // ���������: ������ ����������� �������
    public GameObject vfxPrefab;

    // ���� ����� ���������� �� �������
    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned) return;

        // ���������� ������� �� ������ ��� ���������� �����
        CmdPerformAttack(caster.transform.position, targetObject.GetComponent<NetworkIdentity>().netId);
    }

    // [Command]-�����, ������� ����������� �� �������
    [Command(requiresAuthority = false)]
    private void CmdPerformAttack(Vector3 casterPosition, uint targetNetId)
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
                    // ���������: �������� Rpc ����� ��� ��������������� VFX �� ���� ��������
                    RpcPlayVFX(casterPosition, targetIdentity.transform.position);
                }
                else
                {
                    Debug.Log("Cannot attack a teammate!");
                }
            }
        }
    }

    // ���������: [ClientRpc]-�����, ������� ����������� �� ���� ��������
    [ClientRpc]
    private void RpcPlayVFX(Vector3 startPosition, Vector3 endPosition)
    {
        if (vfxPrefab != null)
        {
            // ����� �� ������ ������� ������. 
            // ��� �������, ������� ��� �� ��������� ������� � ���������� ����� 2 �������.
            GameObject vfxInstance = Instantiate(vfxPrefab, startPosition, Quaternion.identity);

            // ���� VFX ������ ������������ � ����, ����� ����� �������� ������ ��������.
            // ��������, ����� �������� ���������, ������� ����� ���������� ������ �� startPosition � endPosition.

            Destroy(vfxInstance, 0.2f);
        }
    }
}