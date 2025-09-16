using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class RevealGroundEffect : NetworkBehaviour
{
    private float dur;
    private float rad;
    private PlayerTeam team;
    private int layerMask;
    private HashSet<uint> revealedPlayers = new HashSet<uint>();
    public void Init(float duration, float radius, PlayerTeam ownerTeam, int layerMask)
    {
        dur = duration;
        rad = radius;
        team = ownerTeam;
        this.layerMask = layerMask;
        StartCoroutine(DestroyAfter(duration));
    }
    private void Update()
    {
        if (!isServer) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, rad, layerMask);
        HashSet<uint> currentPlayers = new HashSet<uint>();
        foreach (Collider col in hits)
        {
            PlayerCore player = col.GetComponent<PlayerCore>();
            if (player != null && player.team != team && player.Skills._isInvisible)
            {
                currentPlayers.Add(player.netId);
                if (!revealedPlayers.Contains(player.netId))
                {
                    player.Skills.RpcRevealPlayer(true, LayerMask.NameToLayer("Player"));
                    player.Skills.RpcSetInvisibilityState(false);  // Добавлено: временно видим для атаки
                    player.Skills.SetPlayerLayer(LayerMask.NameToLayer("Player"));
                    revealedPlayers.Add(player.netId);
                }
            }
        }
        List<uint> playersToHide = new List<uint>(revealedPlayers);
        foreach (uint playerId in playersToHide)
        {
            if (!currentPlayers.Contains(playerId))
            {
                if (NetworkServer.spawned.ContainsKey(playerId))
                {
                    PlayerCore player = NetworkServer.spawned[playerId].GetComponent<PlayerCore>();
                    if (player != null && player.Skills._isInvisible)
                    {
                        player.Skills.RpcSetInvisibilityVisibility(true, player.team, player.Skills._originalLayer);
                        player.Skills.RpcSetInvisibilityState(true);  // Добавлено: возвращаем невидимость
                        player.Skills.SetPlayerLayer(LayerMask.NameToLayer("Ignore Raycast"));
                        player.Skills.RpcRevealPlayer(false, LayerMask.NameToLayer("Ignore Raycast"));
                    }
                }
                revealedPlayers.Remove(playerId);
            }
        }
    }
    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (uint playerId in revealedPlayers)
        {
            if (NetworkServer.spawned.ContainsKey(playerId))
            {
                PlayerCore player = NetworkServer.spawned[playerId].GetComponent<PlayerCore>();
                if (player != null && player.Skills._isInvisible)
                {
                    player.Skills.RpcSetInvisibilityVisibility(true, player.team, player.Skills._originalLayer);
                    player.Skills.RpcSetInvisibilityState(true);  // Добавлено: возвращаем невидимость
                    player.Skills.SetPlayerLayer(LayerMask.NameToLayer("Ignore Raycast"));
                    player.Skills.RpcRevealPlayer(false, LayerMask.NameToLayer("Ignore Raycast"));
                }
            }
        }
        NetworkServer.Destroy(gameObject);
    }
}