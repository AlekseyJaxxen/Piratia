using UnityEngine;
using Mirror;

public class HealingSkill : SkillBase
{
    [Header("Healing Skill Settings")]
    public int healAmount = 20;

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

        Debug.Log($"Attempting to heal target: {targetObject.name}, netId: {targetIdentity.netId}");

        CmdPerformHeal(targetIdentity.netId);
    }

    [Command]
    private void CmdPerformHeal(uint targetNetId)
    {
        Debug.Log($"Server received heal command for target netId: {targetNetId}");

        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

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