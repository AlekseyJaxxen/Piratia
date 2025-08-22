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

            if (targetHealth != null)
            {
                // ������� ���� ����.
                targetHealth.TakeDamage(damageAmount);
            }
        }
    }
}