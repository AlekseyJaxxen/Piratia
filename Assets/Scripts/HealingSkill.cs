using UnityEngine;
using Mirror;

public class HealingSkill : SkillBase
{
    [Header("Healing Skill Settings")]
    public int healAmount = 20;

    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned) return;

        // ќтправл€ем команду на сервер дл€ выполнени€ лечени€.
        CmdPerformHeal(targetObject.GetComponent<NetworkIdentity>().netId);
    }

    [Command(requiresAuthority = false)]
    private void CmdPerformHeal(uint targetNetId)
    {
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

            // ѕровер€ем, что цель находитс€ в той же команде, что и кастующий
            if (targetHealth != null && targetCore != null && casterCore != null)
            {
                if (casterCore.team == targetCore.team)
                {
                    targetHealth.Heal(healAmount);
                }
                else
                {
                    Debug.Log("Cannot heal an enemy!");
                }
            }
        }
    }
}