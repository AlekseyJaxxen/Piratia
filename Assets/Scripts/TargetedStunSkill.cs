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
            if (targetCore != null)
            {
                targetCore.StartCoroutine(StunRoutine(targetCore, stunDuration));
            }
        }
        RpcPlayEffect(targetNetId);
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

    [Server]
    private IEnumerator StunRoutine(PlayerCore core, float duration)
    {
        core.SetStunState(true);
        core.Movement.StopMovement();
        yield return new WaitForSeconds(duration);
        core.SetStunState(false);
    }
}