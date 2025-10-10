using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

/// <summary>
/// Централизованный менеджер для управления игроками
/// Заменяет использование FindObjectOfType для поиска игроков
/// </summary>
public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    [Header("Player Tracking")]
    private Dictionary<uint, PlayerCore> playersByNetId = new Dictionary<uint, PlayerCore>();
    private Dictionary<string, List<PlayerCore>> playersByPartyId = new Dictionary<string, List<PlayerCore>>();
    private Dictionary<string, List<PlayerCore>> playersByGuildId = new Dictionary<string, List<PlayerCore>>();
    
    [Header("Events")]
    public System.Action<PlayerCore> OnPlayerJoined;
    public System.Action<PlayerCore> OnPlayerLeft;
    public System.Action<PlayerCore, string> OnPlayerJoinedParty;
    public System.Action<PlayerCore, string> OnPlayerLeftParty;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Регистрирует игрока в менеджере
    /// </summary>
    [Server]
    public void RegisterPlayer(PlayerCore player)
    {
        if (player == null) return;
        
        uint netId = player.netId;
        playersByNetId[netId] = player;
        
        // Добавляем в группы
        if (!string.IsNullOrEmpty(player.partyId))
        {
            AddPlayerToParty(player, player.partyId);
        }
        
        if (!string.IsNullOrEmpty(player.guildId))
        {
            AddPlayerToGuild(player, player.guildId);
        }
        
        OnPlayerJoined?.Invoke(player);
        Debug.Log($"[PlayerManager] Registered player: {player.playerName} (NetId: {netId})");
    }
    
    /// <summary>
    /// Удаляет игрока из менеджера
    /// </summary>
    [Server]
    public void UnregisterPlayer(PlayerCore player)
    {
        if (player == null) return;
        
        uint netId = player.netId;
        playersByNetId.Remove(netId);
        
        // Удаляем из групп
        if (!string.IsNullOrEmpty(player.partyId))
        {
            RemovePlayerFromParty(player, player.partyId);
        }
        
        if (!string.IsNullOrEmpty(player.guildId))
        {
            RemovePlayerFromGuild(player, player.guildId);
        }
        
        OnPlayerLeft?.Invoke(player);
        Debug.Log($"[PlayerManager] Unregistered player: {player.playerName} (NetId: {netId})");
    }
    
    /// <summary>
    /// Получает игрока по NetId
    /// </summary>
    public PlayerCore GetPlayerByNetId(uint netId)
    {
        return playersByNetId.TryGetValue(netId, out var player) ? player : null;
    }
    
    /// <summary>
    /// Получает всех игроков
    /// </summary>
    public List<PlayerCore> GetAllPlayers()
    {
        return playersByNetId.Values.ToList();
    }
    
    /// <summary>
    /// Получает игроков в группе
    /// </summary>
    public List<PlayerCore> GetPlayersInParty(string partyId)
    {
        if (string.IsNullOrEmpty(partyId)) return new List<PlayerCore>();
        return playersByPartyId.TryGetValue(partyId, out var players) ? players : new List<PlayerCore>();
    }
    
    /// <summary>
    /// Получает игроков в гильдии
    /// </summary>
    public List<PlayerCore> GetPlayersInGuild(string guildId)
    {
        if (string.IsNullOrEmpty(guildId)) return new List<PlayerCore>();
        return playersByGuildId.TryGetValue(guildId, out var players) ? players : new List<PlayerCore>();
    }
    
    /// <summary>
    /// Получает локального игрока
    /// </summary>
    public PlayerCore GetLocalPlayer()
    {
        return playersByNetId.Values.FirstOrDefault(p => p.isLocalPlayer);
    }
    
    /// <summary>
    /// Добавляет игрока в группу
    /// </summary>
    [Server]
    public void AddPlayerToParty(PlayerCore player, string partyId)
    {
        if (player == null || string.IsNullOrEmpty(partyId)) return;
        
        if (!playersByPartyId.ContainsKey(partyId))
        {
            playersByPartyId[partyId] = new List<PlayerCore>();
        }
        
        if (!playersByPartyId[partyId].Contains(player))
        {
            playersByPartyId[partyId].Add(player);
            OnPlayerJoinedParty?.Invoke(player, partyId);
        }
    }
    
    /// <summary>
    /// Удаляет игрока из группы
    /// </summary>
    [Server]
    public void RemovePlayerFromParty(PlayerCore player, string partyId)
    {
        if (player == null || string.IsNullOrEmpty(partyId)) return;
        
        if (playersByPartyId.TryGetValue(partyId, out var players))
        {
            players.Remove(player);
            OnPlayerLeftParty?.Invoke(player, partyId);
            
            if (players.Count == 0)
            {
                playersByPartyId.Remove(partyId);
            }
        }
    }
    
    /// <summary>
    /// Добавляет игрока в гильдию
    /// </summary>
    [Server]
    public void AddPlayerToGuild(PlayerCore player, string guildId)
    {
        if (player == null || string.IsNullOrEmpty(guildId)) return;
        
        if (!playersByGuildId.ContainsKey(guildId))
        {
            playersByGuildId[guildId] = new List<PlayerCore>();
        }
        
        if (!playersByGuildId[guildId].Contains(player))
        {
            playersByGuildId[guildId].Add(player);
        }
    }
    
    /// <summary>
    /// Удаляет игрока из гильдии
    /// </summary>
    [Server]
    public void RemovePlayerFromGuild(PlayerCore player, string guildId)
    {
        if (player == null || string.IsNullOrEmpty(guildId)) return;
        
        if (playersByGuildId.TryGetValue(guildId, out var players))
        {
            players.Remove(player);
            
            if (players.Count == 0)
            {
                playersByGuildId.Remove(guildId);
            }
        }
    }
    
    /// <summary>
    /// Получает количество игроков в группе
    /// </summary>
    public int GetPartySize(string partyId)
    {
        return GetPlayersInParty(partyId).Count;
    }
    
    /// <summary>
    /// Проверяет, является ли игрок лидером группы
    /// </summary>
    public bool IsPartyLeader(PlayerCore player)
    {
        if (player == null || string.IsNullOrEmpty(player.partyId)) return false;
        
        var partyMembers = GetPlayersInParty(player.partyId);
        return partyMembers.Any(p => p.isPartyLeader);
    }
    
    /// <summary>
    /// Получает лидера группы
    /// </summary>
    public PlayerCore GetPartyLeader(string partyId)
    {
        if (string.IsNullOrEmpty(partyId)) return null;
        
        var partyMembers = GetPlayersInParty(partyId);
        return partyMembers.FirstOrDefault(p => p.isPartyLeader);
    }
    
    /// <summary>
    /// Обновляет информацию о группе игрока
    /// </summary>
    [Server]
    public void UpdatePlayerParty(PlayerCore player, string oldPartyId, string newPartyId)
    {
        if (player == null) return;
        
        // Удаляем из старой группы
        if (!string.IsNullOrEmpty(oldPartyId))
        {
            RemovePlayerFromParty(player, oldPartyId);
        }
        
        // Добавляем в новую группу
        if (!string.IsNullOrEmpty(newPartyId))
        {
            AddPlayerToParty(player, newPartyId);
        }
    }
    
    /// <summary>
    /// Получает статистику менеджера
    /// </summary>
    public (int totalPlayers, int totalParties, int totalGuilds) GetStats()
    {
        return (playersByNetId.Count, playersByPartyId.Count, playersByGuildId.Count);
    }
}
