using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class RevealGroundEffect : NetworkBehaviour
{
    private float dur;
    private float rad;
    private PlayerCore ownerPlayerCore;
    private int layerMask;
    private HashSet<uint> revealedPlayers = new HashSet<uint>();
    public void Init(float duration, float radius, PlayerCore ownerPlayerCore, int layerMask)
    {
        dur = duration;
        rad = radius;
        this.ownerPlayerCore = ownerPlayerCore;
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
            if (player != null && IsEnemy(player, ownerPlayerCore) && player.Skills._isInvisible)
            {
                currentPlayers.Add(player.netId);
                if (!revealedPlayers.Contains(player.netId))
                {
                    player.Skills.RpcRevealPlayer(true, LayerMask.NameToLayer("Player"));
                    player.Skills.RpcSetInvisibilityState(false);  // ���������: �������� ����� ��� �����
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
                        player.Skills.RpcSetInvisibilityState(true);  // ���������: ���������� �����������
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
                    player.Skills.RpcSetInvisibilityState(true);  // ���������: ���������� �����������
                    player.Skills.SetPlayerLayer(LayerMask.NameToLayer("Ignore Raycast"));
                    player.Skills.RpcRevealPlayer(false, LayerMask.NameToLayer("Ignore Raycast"));
                }
            }
        }
        NetworkServer.Destroy(gameObject);
    }
    
    /// <summary>
    /// Checks if player is an enemy to the ground effect owner
    /// Supports dynamic teams: guild, party, faction, and basic teams
    /// </summary>
    private bool IsEnemy(PlayerCore player, PlayerCore ownerPlayerCore)
    {
        if (player == null || ownerPlayerCore == null) return false;
        
        // A player is never an enemy to themselves
        if (ownerPlayerCore == player)
        {
            return false;
        }
        
        // Check party membership first (highest priority)
        if (!string.IsNullOrEmpty(ownerPlayerCore.partyId) && !string.IsNullOrEmpty(player.partyId) && 
            ownerPlayerCore.partyId == player.partyId)
        {
            return false; // Party members are never enemies
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(ownerPlayerCore.guildId) && !string.IsNullOrEmpty(player.guildId) && 
            ownerPlayerCore.guildId == player.guildId)
        {
            return false; // Guild members are never enemies
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(ownerPlayerCore.factionId) && !string.IsNullOrEmpty(player.factionId) && 
            ownerPlayerCore.factionId == player.factionId)
        {
            return false; // Faction members are never enemies
        }
        
        // Check basic team logic
        if (player.team == ownerPlayerCore.team && player.team != PlayerTeam.Solo)
        {
            return false; // Same team, not enemy
        }
        
        // For solo players, they are enemies to each other
        if (player.team == PlayerTeam.Solo && ownerPlayerCore.team == PlayerTeam.Solo)
        {
            return true; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return player.team != ownerPlayerCore.team;
    }
}