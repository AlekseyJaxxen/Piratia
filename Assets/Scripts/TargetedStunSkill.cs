using UnityEngine;
using Mirror;
using System.Collections;

public class TargetedStunSkill : SkillBase
{
    [Header("Targeted Stun Skill Specifics")]
    public float stunDuration = 3f;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
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

        // Клиентская проверка маны
        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.Log("Not enough mana to cast skill!");
            return;
        }

        Debug.Log($"Attempting to stun target: {targetObject.name}, netId: {targetIdentity.netId}");

        CmdApplyStun(targetIdentity.netId);
    }

    [Command]
    private void CmdApplyStun(uint targetNetId)
    {
        Debug.Log($"Server received stun command for target netId: {targetNetId}");

        // Серверная проверка маны
        CharacterStats stats = connectionToClient.identity.GetComponent<CharacterStats>();
        if (stats != null && !stats.ConsumeMana(ManaCost))
        {
            Debug.Log("Not enough mana on server!");
            return;
        }

        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        if (casterCore.isStunned)
        {
            Debug.Log("Caster is stunned and cannot use this skill.");
            return;
        }

        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null)
            {
                if (casterCore.team != targetCore.team)
                {
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