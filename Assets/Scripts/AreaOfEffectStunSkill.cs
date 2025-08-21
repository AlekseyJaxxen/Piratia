using UnityEngine;
using Mirror;

public class AreaOfEffectStunSkill : SkillBase
{
    [Header("Area Stun Skill Specifics")]
    public float stunDuration = 2f;
    public float effectRadius = 2f;
    public GameObject effectPrefab;

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (!isLocalPlayer || !targetPosition.HasValue) return;

        CmdStunArea(targetPosition.Value);
    }

    [Command]
    private void CmdStunArea(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, effectRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Player"))
            {
                NetworkIdentity targetIdentity = hit.GetComponent<NetworkIdentity>();
                if (targetIdentity != null && targetIdentity.netId != netId)
                {
                    PlayerCore targetCore = hit.GetComponent<PlayerCore>();
                    if (targetCore != null)
                    {
                        targetCore.StartCoroutine(StunRoutine(targetCore, stunDuration));
                    }
                }
            }
        }
        RpcPlayEffect(position);
    }

    [ClientRpc]
    private void RpcPlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    [Server]
    private System.Collections.IEnumerator StunRoutine(PlayerCore core, float duration)
    {
        core.SetStunState(true);
        core.Movement.StopMovement();
        yield return new WaitForSeconds(duration);
        core.SetStunState(false);
    }
}