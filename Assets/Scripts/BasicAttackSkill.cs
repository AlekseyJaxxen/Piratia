using UnityEngine;
using Mirror;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public int damageAmount = 10;

    // Ётот метод вызываетс€ на клиенте, когда игрок хочет атаковать.
    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned) return;

        // ќтправл€ем команду на сервер, чтобы выполнить атаку.
        // »спользуем NetworkIdentity.netId дл€ идентификации цели по сети.
        CmdPerformAttack(targetObject.GetComponent<NetworkIdentity>().netId);
    }

    // [Command]-метод, который выполн€етс€ на сервере.
    // requiresAuthority = false, так как атака может быть на чужого персонажа.
    [Command(requiresAuthority = false)]
    private void CmdPerformAttack(uint targetNetId)
    {
        // ѕровер€ем, существует ли цель на сервере.
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            Health targetHealth = targetIdentity.GetComponent<Health>();

            if (targetHealth != null)
            {
                // Ќаносим урон цели.
                targetHealth.TakeDamage(damageAmount);
            }
        }
    }
}