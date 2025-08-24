using UnityEngine;
using Mirror;
using System.Collections;

public class PoisonSkill : SkillBase
{
    [Header("Poison Skill Specifics")]
    public float poisonDuration = 4f;
    public GameObject effectPrefab;

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;
        CmdApplyPoison(targetObject.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    private void CmdApplyPoison(uint targetNetId)
    {
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
        if (casterCore.GetComponent<ControlEffectManager>().IsStunned)
        {
            Debug.Log("Caster is stunned and cannot use this skill.");
            return;
        }

        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null && casterCore.team != targetCore.team)
            {
                targetCore.GetComponent<ControlEffectManager>().ApplyControlEffect(ControlEffectType.Poison, poisonDuration);
                RpcPlayEffect(targetNetId);
            }
            else
            {
                Debug.Log("Cannot poison a teammate!");
            }
        }
    }

    [ClientRpc]
    private void RpcPlayEffect(uint targetNetId)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId) && effectPrefab != null)
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            GameObject effect = Instantiate(effectPrefab, targetIdentity.transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, poisonDuration);
        }
    }
}