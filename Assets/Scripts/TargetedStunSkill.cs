using UnityEngine;
using Mirror;
using System.Collections;

public class TargetedStunSkill : SkillBase
{
    [Header("Targeted Stun Skill Specifics")]
    public float stunDuration = 3f;
    public GameObject effectPrefab;

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;
        CmdApplyStun(targetObject.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    private void CmdApplyStun(uint targetNetId)
    {
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null)
            {
                if (casterCore.team != targetCore.team)
                {
                    targetCore.TryApplyStun(2, stunDuration);
                    RpcPlayEffect(targetNetId);
                }
                else
                {
                    Debug.Log("Cannot stun a teammate!");
                }
            }
        }
    }

    [ClientRpc]
    private void RpcPlayEffect(uint targetNetId)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab, targetIdentity.transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
    }
}