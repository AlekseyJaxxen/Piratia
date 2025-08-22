using UnityEngine;
using Mirror;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public int damageAmount = 10;

    // ���� ����� ���������� �� �������, ����� ����� ����� ���������.
    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned) return;

        // ���������� ������� �� ������, ����� ��������� �����.
        // ���������� NetworkIdentity.netId ��� ������������� ���� �� ����.
        CmdPerformAttack(targetObject.GetComponent<NetworkIdentity>().netId);
    }

    // [Command]-�����, ������� ����������� �� �������.
    // requiresAuthority = false, ��� ��� ����� ����� ���� �� ������ ���������.

    [Command(requiresAuthority = false)]
    private void CmdPerformAttack(uint targetNetId)
    {
        // ���������, ���������� �� ���� �� �������.
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore attackerCore = connectionToClient.identity.GetComponent<PlayerCore>();

            // �������� �� ��, ��� ���� �� ����������� ����� �������
            if (targetHealth != null && targetCore != null && attackerCore != null)
            {
                if (attackerCore.team != targetCore.team)
                {
                    // ������� ���� ����.
                    targetHealth.TakeDamage(damageAmount);
                }
                else
                {
                    Debug.Log("Cannot attack a teammate!");
                }
            }
        }
    }
}