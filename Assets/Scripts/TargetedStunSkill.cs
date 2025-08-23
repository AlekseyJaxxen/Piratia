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
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        // ÄÎÁÀÂËÅÍÎ: Ïğîâåğêà, íàõîäèòñÿ ëè êàñòåğ ïîä ñòàíîì
        if (casterCore.isStunned)
        {
            Debug.Log("Caster is stunned and cannot use this skill.");
            return;
        }

        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null)
            {
                if (casterCore.team != targetCore.team)
                {
                    // ÈÑÏÎËÜÇÓÅÌ ÍÎÂÛÉ ÌÅÒÎÄ ÄËß ÏĞÈÌÅÍÅÍÈß İÔÔÅÊÒÀ ÊÎÍÒĞÎËß
                    targetCore.ApplyControlEffect(ControlEffectType.Stun, stunDuration);
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