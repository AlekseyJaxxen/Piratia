using UnityEngine;

/// <summary>
/// Скрипт для тестирования сундуков
/// Добавьте этот компонент на любой GameObject и используйте кнопки для тестирования
/// </summary>
public class ChestTester : MonoBehaviour
{
    [Header("Тестирование сундуков")]
    [SerializeField] private int chestItemId = 100; // ID сундука в ItemDatabase
    [SerializeField] private int testPlayerIndex = 0; // Индекс игрока для тестирования
    
    [Header("Кнопки тестирования")]
    [SerializeField] private bool addChestToInventory = false;
    [SerializeField] private bool openChestDirectly = false;
    
    private void Update()
    {
        if (addChestToInventory)
        {
            addChestToInventory = false;
            AddChestToInventory();
        }
        
        if (openChestDirectly)
        {
            openChestDirectly = false;
            OpenChestDirectly();
        }
    }
    
    /// <summary>
    /// Добавляет сундук в инвентарь игрока
    /// </summary>
    [ContextMenu("Add Chest to Inventory")]
    public void AddChestToInventory()
    {
        PlayerCore[] players = FindObjectsOfType<PlayerCore>();
        if (players.Length == 0)
        {
            Debug.LogError("[ChestTester] No players found!");
            return;
        }
        
        PlayerCore player = players[testPlayerIndex % players.Length];
        Item chestItem = ItemDatabase.Instance?.GetItem(chestItemId);
        
        if (chestItem == null)
        {
            Debug.LogError($"[ChestTester] Chest item with ID {chestItemId} not found in ItemDatabase!");
            return;
        }
        
        if (chestItem.itemType != ItemType.Chest)
        {
            Debug.LogError($"[ChestTester] Item {chestItem.itemName} is not a chest item!");
            return;
        }
        
        bool success = player.Inventory.AddItem(chestItem, 1);
        if (success)
        {
            Debug.Log($"[ChestTester] Successfully added chest '{chestItem.itemName}' to player {player.playerName}'s inventory");
        }
        else
        {
            Debug.LogError($"[ChestTester] Failed to add chest to inventory (inventory might be full)");
        }
    }
    
    /// <summary>
    /// Открывает сундук напрямую (для тестирования логики)
    /// </summary>
    [ContextMenu("Open Chest Directly")]
    public void OpenChestDirectly()
    {
        PlayerCore[] players = FindObjectsOfType<PlayerCore>();
        if (players.Length == 0)
        {
            Debug.LogError("[ChestTester] No players found!");
            return;
        }
        
        PlayerCore player = players[testPlayerIndex % players.Length];
        Item chestItem = ItemDatabase.Instance?.GetItem(chestItemId);
        
        if (chestItem == null)
        {
            Debug.LogError($"[ChestTester] Chest item with ID {chestItemId} not found in ItemDatabase!");
            return;
        }
        
        if (chestItem.itemType != ItemType.Chest)
        {
            Debug.LogError($"[ChestTester] Item {chestItem.itemName} is not a chest item!");
            return;
        }
        
        if (chestItem.chestData == null)
        {
            Debug.LogError($"[ChestTester] Chest item {chestItem.itemName} has no ChestData assigned!");
            return;
        }
        
        // Открываем сундук напрямую
        chestItem.Use(player);
        Debug.Log($"[ChestTester] Opened chest '{chestItem.itemName}' directly for player {player.playerName}");
    }
    
    /// <summary>
    /// Создает тестовый сундук новичка
    /// </summary>
    [ContextMenu("Create Test Starter Chest")]
    public void CreateTestStarterChest()
    {
        // Создаем ChestItemData
        ChestItemData chestData = ScriptableObject.CreateInstance<ChestItemData>();
        chestData.chestName = "Сундук новичка";
        chestData.description = "Сундук с полезными предметами для начинающих";
        chestData.goldReward = 100;
        chestData.goldChance = 1.0f;
        
        // Добавляем награды
        chestData.rewards.Add(new ChestReward
        {
            itemId = 5, // Blade of Enigma
            quantity = 1,
            dropChance = 1.0f,
            isGuaranteed = true
        });
        
        chestData.rewards.Add(new ChestReward
        {
            itemId = 6, // Другой предмет
            quantity = 1,
            dropChance = 0.8f,
            isGuaranteed = false
        });
        
        // Создаем Item
        Item chestItem = ScriptableObject.CreateInstance<Item>();
        chestItem.itemName = "Сундук новичка";
        chestItem.id = chestItemId;
        chestItem.itemType = ItemType.Chest;
        chestItem.canUse = true;
        chestItem.canDrop = true;
        chestItem.canSell = true;
        chestItem.maxStack = 1;
        chestItem.chestData = chestData;
        
        // Сохраняем в папку Resources для доступа через ItemDatabase
        #if UNITY_EDITOR
        string path = "Assets/Resources/Items/";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        UnityEditor.AssetDatabase.CreateAsset(chestData, path + "StarterChestData.asset");
        UnityEditor.AssetDatabase.CreateAsset(chestItem, path + "StarterChestItem.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log($"[ChestTester] Created test starter chest at {path}");
        Debug.Log($"[ChestTester] Don't forget to add the item to ItemDatabase with ID {chestItemId}");
        #else
        Debug.LogWarning("[ChestTester] CreateTestStarterChest can only be used in Editor mode");
        #endif
    }
}
