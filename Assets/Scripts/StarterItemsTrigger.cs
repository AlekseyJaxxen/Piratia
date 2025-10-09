using UnityEngine;
using Mirror;

/// <summary>
/// Компонент для автоматической выдачи стартовых предметов при спавне игрока
/// </summary>
public class StarterItemsTrigger : NetworkBehaviour
{
    [Header("Starter Items System")]
    [SerializeField] private StarterItemsSystem starterItemsSystem;
    
    [Header("Trigger Settings")]
    [SerializeField] private bool giveItemsOnPlayerSpawn = true;
    [SerializeField] private bool giveItemsOnTriggerEnter = false;
    [SerializeField] private bool giveItemsOnStart = false;
    
    [Header("Trigger Area")]
    [SerializeField] private bool useTriggerCollider = true;
    [SerializeField] private float triggerRadius = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Start()
    {
        // Если система стартовых предметов не назначена, ищем её в сцене
        if (starterItemsSystem == null)
        {
            starterItemsSystem = FindObjectOfType<StarterItemsSystem>();
            if (starterItemsSystem == null)
            {
                Debug.LogError("[StarterItemsTrigger] StarterItemsSystem not found in scene!");
            }
        }
        
        if (giveItemsOnStart && isServer)
        {
            GiveItemsToAllPlayersInScene();
        }
    }
    
    /// <summary>
    /// Вызывается при спавне игрока
    /// </summary>
    public void OnPlayerSpawned(PlayerCore player)
    {
        if (!isServer) return;
        
        if (giveItemsOnPlayerSpawn && starterItemsSystem != null)
        {
            if (showDebugInfo)
                Debug.Log($"[StarterItemsTrigger] Player {player.playerName} spawned, giving starter items");
            
            starterItemsSystem.GiveStarterItemsToPlayer(player);
        }
    }
    
    /// <summary>
    /// Выдает предметы всем игрокам в сцене
    /// </summary>
    [Server]
    public void GiveItemsToAllPlayersInScene()
    {
        if (starterItemsSystem == null) return;
        
        var allPlayers = FindObjectsOfType<PlayerCore>();
        foreach (var player in allPlayers)
        {
            if (showDebugInfo)
                Debug.Log($"[StarterItemsTrigger] Giving starter items to existing player: {player.playerName}");
            
            starterItemsSystem.GiveStarterItemsToPlayer(player);
        }
        
        if (showDebugInfo)
            Debug.Log($"[StarterItemsTrigger] Attempted to give items to {allPlayers.Length} existing players");
    }
    
    /// <summary>
    /// Обработчик входа в триггер
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!isServer || !giveItemsOnTriggerEnter) return;
        
        PlayerCore player = other.GetComponent<PlayerCore>();
        if (player != null && starterItemsSystem != null)
        {
            if (showDebugInfo)
                Debug.Log($"[StarterItemsTrigger] Player {player.playerName} entered trigger, giving starter items");
            
            starterItemsSystem.GiveStarterItemsToPlayer(player);
        }
    }
    
    /// <summary>
    /// Ручная выдача предметов игроку
    /// </summary>
    [Server]
    public void GiveItemsToPlayer(PlayerCore player)
    {
        if (starterItemsSystem != null)
        {
            starterItemsSystem.GiveStarterItemsToPlayer(player);
        }
    }
    
    /// <summary>
    /// Устанавливает систему стартовых предметов
    /// </summary>
    public void SetStarterItemsSystem(StarterItemsSystem system)
    {
        starterItemsSystem = system;
    }
    
    /// <summary>
    /// Включает/выключает выдачу предметов при спавне
    /// </summary>
    public void SetGiveItemsOnSpawn(bool enabled)
    {
        giveItemsOnPlayerSpawn = enabled;
    }
    
    /// <summary>
    /// Включает/выключает выдачу предметов при входе в триггер
    /// </summary>
    public void SetGiveItemsOnTriggerEnter(bool enabled)
    {
        giveItemsOnTriggerEnter = enabled;
    }
    
    void OnDrawGizmosSelected()
    {
        if (useTriggerCollider)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
    
    void OnValidate()
    {
        // Автоматически добавляем коллайдер-триггер если нужно
        if (useTriggerCollider && giveItemsOnTriggerEnter)
        {
            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            
            collider.isTrigger = true;
            collider.radius = triggerRadius;
        }
    }
}
