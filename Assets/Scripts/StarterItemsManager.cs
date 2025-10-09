using UnityEngine;
using Mirror;

/// <summary>
/// Менеджер для создания и настройки системы стартовых предметов в сцене
/// </summary>
public class StarterItemsManager : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createStarterItemsSystem = true;
    [SerializeField] private bool createStarterItemsTrigger = true;
    
    [Header("Default Starter Items")]
    [SerializeField] private Item[] defaultStarterItems;
    [SerializeField] private int[] defaultQuantities = { 1, 1, 1, 1, 1 };
    
    
    [Header("References")]
    [SerializeField] private StarterItemsSystem starterItemsSystem;
    [SerializeField] private StarterItemsTrigger starterItemsTrigger;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupStarterItemsSystem();
        }
    }
    
    /// <summary>
    /// Настраивает систему стартовых предметов
    /// </summary>
    [ContextMenu("Setup Starter Items System")]
    public void SetupStarterItemsSystem()
    {
        // Создаем StarterItemsSystem если нужно
        if (createStarterItemsSystem && starterItemsSystem == null)
        {
            GameObject systemObject = new GameObject("StarterItemsSystem");
            systemObject.transform.SetParent(transform);
            starterItemsSystem = systemObject.AddComponent<StarterItemsSystem>();
            
            Debug.Log("[StarterItemsManager] Created StarterItemsSystem");
        }
        
        // Создаем StarterItemsTrigger если нужно
        if (createStarterItemsTrigger && starterItemsTrigger == null)
        {
            GameObject triggerObject = new GameObject("StarterItemsTrigger");
            triggerObject.transform.SetParent(transform);
            starterItemsTrigger = triggerObject.AddComponent<StarterItemsTrigger>();
            
            // Настраиваем триггер
            if (starterItemsSystem != null)
            {
                starterItemsTrigger.SetStarterItemsSystem(starterItemsSystem);
            }
            
            Debug.Log("[StarterItemsManager] Created StarterItemsTrigger");
        }
        
        // Добавляем дефолтные предметы
        if (defaultStarterItems != null && defaultStarterItems.Length > 0)
        {
            AddDefaultStarterItems();
        }
        
    }
    
    /// <summary>
    /// Добавляет дефолтные стартовые предметы
    /// </summary>
    private void AddDefaultStarterItems()
    {
        if (starterItemsSystem == null) return;
        
        for (int i = 0; i < defaultStarterItems.Length && i < defaultQuantities.Length; i++)
        {
            if (defaultStarterItems[i] != null)
            {
                starterItemsSystem.AddStarterItem(
                    defaultStarterItems[i], 
                    defaultQuantities[i], 
                    CharacterClass.Warrior
                );
            }
        }
        
        Debug.Log($"[StarterItemsManager] Added {defaultStarterItems.Length} default starter items");
    }
    
    
    /// <summary>
    /// Добавляет предмет в список стартовых
    /// </summary>
    public void AddStarterItem(Item item, int quantity = 1, CharacterClass requiredClass = CharacterClass.Warrior)
    {
        if (starterItemsSystem != null)
        {
            starterItemsSystem.AddStarterItem(item, quantity, requiredClass);
        }
    }
    
    
    /// <summary>
    /// Удаляет предмет из списка стартовых
    /// </summary>
    public void RemoveStarterItem(Item item)
    {
        if (starterItemsSystem != null)
        {
            starterItemsSystem.RemoveStarterItem(item);
        }
    }
    
    
    /// <summary>
    /// Выдает предметы всем игрокам в сцене
    /// </summary>
    [ContextMenu("Give Items to All Players")]
    public void GiveItemsToAllPlayers()
    {
        if (starterItemsSystem != null)
        {
            starterItemsSystem.GiveItemsToAllPlayers();
        }
    }
    
    /// <summary>
    /// Очищает список игроков, получивших предметы
    /// </summary>
    [ContextMenu("Clear Received Items List")]
    public void ClearReceivedItemsList()
    {
        if (starterItemsSystem != null)
        {
            starterItemsSystem.ClearReceivedItemsList();
        }
    }
    
    /// <summary>
    /// Получает количество стартовых предметов
    /// </summary>
    public int GetStarterItemsCount()
    {
        return starterItemsSystem != null ? starterItemsSystem.GetStarterItemsCount() : 0;
    }
    
    void OnValidate()
    {
        // Валидация в редакторе
        if (defaultStarterItems != null && defaultQuantities != null)
        {
            if (defaultStarterItems.Length != defaultQuantities.Length)
            {
                Debug.LogWarning("[StarterItemsManager] defaultStarterItems and defaultQuantities arrays should have the same length");
            }
        }
    }
}
