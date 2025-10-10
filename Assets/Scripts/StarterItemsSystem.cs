using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Система выдачи стартовых предметов игрокам при входе в игру
/// </summary>
public class StarterItemsSystem : NetworkBehaviour
{
    [Header("Starter Items Configuration")]
    [SerializeField] private List<StarterItemData> starterItems = new List<StarterItemData>();
    
    [Header("Settings")]
    [SerializeField] private bool giveItemsOnSpawn = true;
    [SerializeField] private bool giveItemsOnlyOnce = true;
    [SerializeField] private bool logItemGiving = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Список игроков, которые уже получили стартовые предметы
    private static HashSet<uint> playersWhoReceivedItems = new HashSet<uint>();
    
    [System.Serializable]
    public class StarterItemData
    {
        [Header("Item Settings")]
        public Item item;
        public int quantity = 1;
        public bool useDynamicStats = false;
        
        
        [Header("Class Restrictions")]
        public CharacterClass requiredClass = CharacterClass.Warrior;
        public bool giveToAllClasses = true; // Если true, игнорирует requiredClass
        
        [Header("Level Requirements")]
        public int minLevel = 1;
        public int maxLevel = 100;
        
        [Header("Chance")]
        [Range(0f, 1f)]
        public float chance = 1.0f; // Шанс получить предмет (0-1)
        
        public bool ShouldGiveToPlayer(CharacterStats playerStats)
        {
            string itemName = item?.itemName ?? "Unknown Item";
            
            // Проверяем уровень
            if (playerStats.level < minLevel || playerStats.level > maxLevel)
            {
                Debug.Log($"[StarterItemsSystem] Skipping {itemName} for player {playerStats.name}: level {playerStats.level} not in range {minLevel}-{maxLevel}");
                return false;
            }
            
            // Проверяем класс (если не для всех классов)
            if (!giveToAllClasses && !playerStats.HasClass(requiredClass))
            {
                Debug.Log($"[StarterItemsSystem] Skipping {itemName} for player {playerStats.name}: class {playerStats.characterClass} doesn't match required {requiredClass}");
                return false;
            }
            
            // Проверяем шанс
            if (UnityEngine.Random.Range(0f, 1f) > chance)
            {
                Debug.Log($"[StarterItemsSystem] Skipping {itemName} for player {playerStats.name}: failed chance roll ({chance})");
                return false;
            }
            
            Debug.Log($"[StarterItemsSystem] Giving {itemName} to player {playerStats.name} (class: {playerStats.characterClass}, level: {playerStats.level})");
            return true;
        }
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[StarterItemsSystem] OnStartServer() called");
        
        // Очищаем список игроков при старте сервера для корректной работы при перезапуске
        if (playersWhoReceivedItems != null)
        {
            ClearReceivedItemsList();
            Debug.Log("[StarterItemsSystem] Cleared received items list on server start");
        }
        else
        {
            Debug.LogError("[StarterItemsSystem] playersWhoReceivedItems is null in OnStartServer!");
        }
    }

    void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[StarterItemsSystem] System initialized with {starterItems.Count} starter items");
        }
        
        Debug.Log($"[StarterItemsSystem] Start() called - isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}");
    }
    
    /// <summary>
    /// Выдает стартовые предметы игроку
    /// </summary>
    [Server]
    public void GiveStarterItemsToPlayer(PlayerCore player)
    {
        if (player == null)
        {
            Debug.LogError("[StarterItemsSystem] Player is null!");
            return;
        }
        
        if (player.Inventory == null)
        {
            Debug.LogError("[StarterItemsSystem] Player inventory is null!");
            return;
        }
        
        if (player.Stats == null)
        {
            Debug.LogError("[StarterItemsSystem] Player stats is null!");
            return;
        }
        
        // Проверяем, получал ли игрок уже предметы (если включена опция)
        if (giveItemsOnlyOnce && playersWhoReceivedItems.Contains(player.netId))
        {
            if (logItemGiving)
                Debug.Log($"[StarterItemsSystem] Player {player.playerName} already received starter items");
            return;
        }
        
        if (logItemGiving)
        {
            Debug.Log($"[StarterItemsSystem] Giving starter items to player: {player.playerName}");
            Debug.Log($"[StarterItemsSystem] Player class: {player.Stats?.characterClass}");
            Debug.Log($"[StarterItemsSystem] Player level: {player.Stats?.level}");
        }
        
        // Проверяем, есть ли вообще стартовые предметы
        if (starterItems == null || starterItems.Count == 0)
        {
            Debug.LogWarning("[StarterItemsSystem] No starter items configured! Adding basic starter items...");
            AddBasicStarterItems();
            
            // Проверяем еще раз после добавления
            if (starterItems == null || starterItems.Count == 0)
            {
                Debug.LogError("[StarterItemsSystem] Failed to add basic starter items!");
                return;
            }
        }
        
        if (logItemGiving)
            Debug.Log($"[StarterItemsSystem] Processing {starterItems.Count} starter items for player {player.playerName}");
        
        int itemsGiven = 0;
        int itemsFailed = 0;
        
        foreach (var starterItem in starterItems)
        {
            if (logItemGiving)
            {
                Debug.Log($"[StarterItemsSystem] Processing starter item: {starterItem.item?.itemName}");
            }
            
            if (starterItem.item == null)
            {
                Debug.LogWarning("[StarterItemsSystem] Starter item is null, skipping");
                continue;
            }
            
            // Проверяем, должен ли игрок получить этот предмет
            if (!starterItem.ShouldGiveToPlayer(player.Stats))
            {
                if (showDebugInfo)
                    Debug.Log($"[StarterItemsSystem] Skipping {starterItem.item.itemName} for player {player.playerName} (requirements not met)");
                continue;
            }
            
            // Создаем ItemInfo для предмета
            ItemInfo? itemInfoNullable = CreateItemInfo(starterItem);
            
            // Проверяем, что ItemInfo создан корректно
            if (!itemInfoNullable.HasValue || itemInfoNullable.Value.id <= 0)
            {
                itemsFailed++;
                string itemName = starterItem.item?.itemName ?? "Unknown Item";
                Debug.LogError($"[StarterItemsSystem] Failed to create ItemInfo for {itemName}");
                continue;
            }
            
            ItemInfo itemInfo = itemInfoNullable.Value;
            
            // Пытаемся добавить предмет в инвентарь
            if (player.Inventory.AddItemInfo(itemInfo))
            {
                itemsGiven++;
                if (logItemGiving)
                {
                    string itemName = starterItem.item?.itemName ?? "Unknown Item";
                    Debug.Log($"[StarterItemsSystem] Gave {starterItem.quantity}x {itemName} to {player.playerName}");
                }
            }
            else
            {
                itemsFailed++;
                string itemName = starterItem.item?.itemName ?? "Unknown Item";
                Debug.LogWarning($"[StarterItemsSystem] Failed to give {starterItem.quantity}x {itemName} to {player.playerName} (inventory full?)");
            }
        }
        
        // Добавляем игрока в список получивших предметы
        if (giveItemsOnlyOnce)
        {
            playersWhoReceivedItems.Add(player.netId);
        }
        
        if (logItemGiving)
        {
            Debug.Log($"[StarterItemsSystem] Completed giving items to {player.playerName}: {itemsGiven} given, {itemsFailed} failed");
        }
        
        // Уведомляем клиент о получении предметов (только если есть соединение и есть что уведомлять)
        if (itemsGiven > 0 || itemsFailed > 0)
        {
            if (player != null && player.connectionToClient != null && player.connectionToClient.isReady)
            {
                try
                {
                    // Дополнительная проверка перед отправкой RPC
                    if (player.connectionToClient != null && player.connectionToClient.isReady)
                    {
                        RpcNotifyPlayerAboutStarterItems(player.connectionToClient, itemsGiven, itemsFailed);
                        if (logItemGiving)
                            Debug.Log($"[StarterItemsSystem] Successfully sent RPC to player {player.playerName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[StarterItemsSystem] Connection became invalid before sending RPC to {player.playerName}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[StarterItemsSystem] Failed to send RPC to player {player.playerName}: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[StarterItemsSystem] Cannot notify player {player?.playerName ?? "null"} about starter items: connectionToClient is null or not ready");
            }
        }
        else if (logItemGiving)
        {
            Debug.Log($"[StarterItemsSystem] No items to notify about for player {player.playerName}");
        }
    }
    
    /// <summary>
    /// Создает ItemInfo для стартового предмета
    /// </summary>
    private ItemInfo? CreateItemInfo(StarterItemData starterItem)
    {
        if (starterItem.item == null)
        {
            Debug.LogError("[StarterItemsSystem] StarterItem has no item!");
            return null;
        }
        
        ItemInfo itemInfo = new ItemInfo();
        
        // Создаем ItemInfo для предмета
        itemInfo.id = starterItem.item.id;
        itemInfo.quantity = starterItem.quantity;
        itemInfo.hasDynamicStats = starterItem.useDynamicStats;
        
        if (logItemGiving)
            Debug.Log($"[StarterItemsSystem] Created item info: {starterItem.item.itemName} (ID: {itemInfo.id}, Quantity: {itemInfo.quantity})");
        
        // Если предмет использует динамические статы, генерируем их
        if (starterItem.useDynamicStats && starterItem.item != null && starterItem.item.useDynamicStats)
        {
            Item dynamicItem = starterItem.item.GenerateDynamicItem();
            itemInfo.dynamicItemName = dynamicItem.itemName;
            itemInfo.dynamicRarity = dynamicItem.rarity;
            
            // Копируем динамические статы из сгенерированного предмета
            itemInfo.strengthBonus = dynamicItem.strengthBonus;
            itemInfo.agilityBonus = dynamicItem.agilityBonus;
            itemInfo.spiritBonus = dynamicItem.spiritBonus;
            itemInfo.constitutionBonus = dynamicItem.constitutionBonus;
            itemInfo.accuracyBonus = dynamicItem.accuracyBonus;
            itemInfo.minAttackConstantBonus = dynamicItem.minAttackConstantBonus;
            itemInfo.maxAttackConstantBonus = dynamicItem.maxAttackConstantBonus;
            itemInfo.maxHpConstantBonus = dynamicItem.maxHpConstantBonus;
            itemInfo.maxSpConstantBonus = dynamicItem.maxSpConstantBonus;
            itemInfo.crtConstantBonus = dynamicItem.crtConstantBonus;
            itemInfo.mspdConstantBonus = dynamicItem.mspdConstantBonus;
            itemInfo.physicalResist = dynamicItem.physicalResist;
            itemInfo.constantDefence = dynamicItem.constantDefence;
            itemInfo.physicalResistBonus = dynamicItem.physicalResistBonus;
            itemInfo.hpRecoveryBonus = dynamicItem.hpRecoveryBonus;
            itemInfo.spRecoveryBonus = dynamicItem.spRecoveryBonus;
            itemInfo.dodgeBonus = dynamicItem.dodgeBonus;
            itemInfo.attackSpeedBonus = dynamicItem.attackSpeedBonus;
            itemInfo.attackSpeedPercentBonus = dynamicItem.attackSpeedPercentBonus;
        }
        
        return itemInfo;
    }
    
    /// <summary>
    /// Уведомляет игрока о получении стартовых предметов
    /// </summary>
    [TargetRpc]
    private void RpcNotifyPlayerAboutStarterItems(NetworkConnectionToClient conn, int itemsGiven, int itemsFailed)
    {
        if (itemsGiven > 0)
        {
            string message = $"Received starter items: {itemsGiven} items";
            if (itemsFailed > 0)
            {
                message += $"\nFailed to receive: {itemsFailed} items (inventory full?)";
            }
            
            Debug.Log($"[StarterItemsSystem] {message}");
            
            // Здесь можно добавить UI уведомление
            // NotificationSystem.ShowNotification(message);
        }
    }
    
    /// <summary>
    /// Добавляет стартовый предмет в список
    /// </summary>
    public void AddStarterItem(Item item, int quantity = 1, CharacterClass requiredClass = CharacterClass.Warrior)
    {
        if (item == null)
        {
            Debug.LogError("[StarterItemsSystem] Cannot add null item to starter items");
            return;
        }
        
        var newStarterItem = new StarterItemData
        {
            item = item,
            quantity = quantity,
            requiredClass = requiredClass,
            giveToAllClasses = (requiredClass == CharacterClass.Warrior)
        };
        
        starterItems.Add(newStarterItem);
        
        if (showDebugInfo)
            Debug.Log($"[StarterItemsSystem] Added starter item: {quantity}x {item.itemName}");
    }
    
    
    /// <summary>
    /// Удаляет стартовый предмет из списка
    /// </summary>
    public void RemoveStarterItem(Item item)
    {
        if (item == null) return;
        
        starterItems.RemoveAll(x => x.item == item);
        
        if (showDebugInfo)
            Debug.Log($"[StarterItemsSystem] Removed starter item: {item.itemName}");
    }
    
    
    
    /// <summary>
    /// Добавляет базовые стартовые предметы из ItemDatabase
    /// </summary>
    private void AddBasicStarterItems()
    {
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[StarterItemsSystem] ItemDatabase.Instance is null! Cannot add basic starter items.");
            return;
        }
        
        Item[] allItems = ItemDatabase.Instance.GetAllItems();
        if (allItems == null || allItems.Length == 0)
        {
            Debug.LogError("[StarterItemsSystem] No items found in ItemDatabase! Cannot add basic starter items.");
            return;
        }
        
        Debug.Log($"[StarterItemsSystem] ItemDatabase has {allItems.Length} items available");
        
        // Ищем базовые предметы для воина (ID 1-10 обычно базовые предметы)
        for (int i = 1; i <= 10; i++)
        {
            Item item = ItemDatabase.Instance.GetItem(i);
            if (item != null && item.requiredLevel <= 5 && item.id > 0) // Только предметы для низкого уровня с валидным ID
            {
                AddStarterItem(item, 1, CharacterClass.Warrior);
                Debug.Log($"[StarterItemsSystem] Added basic starter item: {item.itemName} (ID: {item.id})");
            }
            else if (item != null)
            {
                Debug.LogWarning($"[StarterItemsSystem] Skipping item {item.itemName} - invalid ID: {item.id} or level: {item.requiredLevel}");
            }
            else
            {
                Debug.LogWarning($"[StarterItemsSystem] No item found with ID: {i}");
            }
        }
        
        Debug.Log($"[StarterItemsSystem] Added {starterItems.Count} basic starter items");
    }
    
    /// <summary>
    /// Очищает список игроков, получивших предметы (для тестирования и перезапуска сервера)
    /// </summary>
    [ContextMenu("Clear Received Items List")]
    public void ClearReceivedItemsList()
    {
        playersWhoReceivedItems.Clear();
        Debug.Log("[StarterItemsSystem] Cleared received items list");
    }
    
    /// <summary>
    /// Статический метод для очистки списка игроков (можно вызывать из других скриптов)
    /// </summary>
    public static void ClearAllReceivedItems()
    {
        playersWhoReceivedItems.Clear();
        Debug.Log("[StarterItemsSystem] Cleared all received items list (static method)");
    }
    
    /// <summary>
    /// Выдает предметы всем игрокам в игре (для тестирования)
    /// </summary>
    [ContextMenu("Give Items to All Players")]
    public void GiveItemsToAllPlayers()
    {
        if (!isServer)
        {
            Debug.LogWarning("[StarterItemsSystem] Can only give items on server");
            return;
        }
        
        var allPlayers = FindObjectsOfType<PlayerCore>();
        foreach (var player in allPlayers)
        {
            GiveStarterItemsToPlayer(player);
        }
        
        Debug.Log($"[StarterItemsSystem] Attempted to give items to {allPlayers.Length} players");
    }
    
    /// <summary>
    /// Получает количество стартовых предметов
    /// </summary>
    public int GetStarterItemsCount()
    {
        return starterItems.Count;
    }
    
    /// <summary>
    /// Получает список стартовых предметов
    /// </summary>
    public List<StarterItemData> GetStarterItems()
    {
        return new List<StarterItemData>(starterItems);
    }
    
    void OnDestroy()
    {
        // Очищаем список при уничтожении объекта (например, при остановке сервера)
        if (isServer)
        {
            ClearReceivedItemsList();
            Debug.Log("[StarterItemsSystem] Cleared received items list on server stop");
        }
    }
    
    void OnValidate()
    {
        // Валидация в редакторе
        if (starterItems != null)
        {
            foreach (var item in starterItems)
            {
                if (item.item == null)
                {
                    Debug.LogWarning("[StarterItemsSystem] Some starter items have null Item reference");
                    break;
                }
            }
        }
    }
}
