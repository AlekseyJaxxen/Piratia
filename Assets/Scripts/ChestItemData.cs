using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Данные сундука как предмета в инвентаре
/// </summary>
[System.Serializable]
public class ChestReward
{
    [Header("Основные настройки")]
    [SerializeField] private Item itemSO; // Выбор предмета через SO
    public int itemId = -1; // Fallback для старых сундуков (публичное для совместимости)
    
    /// <summary>
    /// Публичное свойство для доступа к itemSO
    /// </summary>
    public Item ItemSO 
    { 
        get => itemSO; 
        set => itemSO = value; 
    }
    public int quantity = 1;
    public float dropChance = 1.0f; // Шанс выпадения (0.0 - 1.0)
    
    [Header("Ограничения по классу")]
    public CharacterClass requiredClass = CharacterClass.Warrior; // Требуемый класс
    public bool giveToAllClasses = true; // Давать всем классам (игнорирует requiredClass)
    
    [Header("Роллинг статов")]
    public bool rollStats = false; // Роллить ли статы как при дропе с монстра
    public int level = 1; // Уровень для роллинга статов
    
    [Header("Дополнительные настройки")]
    public bool isGuaranteed = false; // Гарантированный предмет (игнорирует dropChance)
    
    /// <summary>
    /// Получает ID предмета (из SO или fallback)
    /// </summary>
    public int GetItemId()
    {
        if (itemSO != null)
        {
            return itemSO.id;
        }
        return itemId;
    }
    
    /// <summary>
    /// Получает предмет
    /// </summary>
    public Item GetItem()
    {
        if (itemSO != null)
        {
            return itemSO;
        }
        
        if (itemId > 0)
        {
            return ItemDatabase.Instance?.GetItem(itemId);
        }
        
        return null;
    }
    
    /// <summary>
    /// Проверяет, должен ли игрок получить этот предмет
    /// </summary>
    public bool ShouldGiveToPlayer(CharacterStats playerStats)
    {
        // Проверяем класс (если не для всех классов)
        if (!giveToAllClasses && !playerStats.HasClass(requiredClass))
        {
            return false;
        }
        
        return true;
    }
}

/// <summary>
/// Класс-специфичные награды для сундука
/// </summary>
[System.Serializable]
public class ClassSpecificRewards
{
    [Header("Настройки класса")]
    public CharacterClass targetClass = CharacterClass.Warrior;
    public string className = "Warrior"; // Для отображения в Inspector
    
    [Header("Предметы для этого класса")]
    public List<ChestReward> classRewards = new List<ChestReward>();
    
    [Header("Дополнительные награды")]
    public int classGoldReward = 0;
    public float classGoldChance = 1.0f;
    
    /// <summary>
    /// Проверяет, подходит ли этот набор наград для игрока
    /// </summary>
    public bool IsForPlayer(CharacterStats playerStats)
    {
        return playerStats.HasClass(targetClass);
    }
}

[CreateAssetMenu(fileName = "NewChestItem", menuName = "Items/Chest Item")]
public class ChestItemData : ScriptableObject
{
    [Header("Основные настройки")]
    public string chestName = "Сундук новичка";
    public string description = "Сундук с полезными предметами для начинающих";
    public Sprite icon;
    
    [Header("Предметы в сундуке")]
    public List<ChestReward> rewards = new List<ChestReward>();
    
    [Header("Класс-специфичные награды")]
    public bool useClassSpecificRewards = false; // Использовать разный дроп для разных классов
    public List<ClassSpecificRewards> classSpecificRewards = new List<ClassSpecificRewards>();
    
    [Header("Дополнительные награды")]
    public int goldReward = 0; // Золото
    public float goldChance = 1.0f; // Шанс получения золота
    
    /// <summary>
    /// Генерирует награды из сундука для конкретного игрока
    /// </summary>
    public List<ItemInfo> GenerateRewards(CharacterStats playerStats = null)
    {
        List<ItemInfo> generatedRewards = new List<ItemInfo>();
        
        if (useClassSpecificRewards && playerStats != null)
        {
            // Используем класс-специфичные награды
            generatedRewards.AddRange(GenerateClassSpecificRewards(playerStats));
        }
        else
        {
            // Используем обычные награды
            generatedRewards.AddRange(GenerateDefaultRewards(playerStats));
        }
        
        return generatedRewards;
    }
    
    /// <summary>
    /// Генерирует класс-специфичные награды
    /// </summary>
    private List<ItemInfo> GenerateClassSpecificRewards(CharacterStats playerStats)
    {
        List<ItemInfo> generatedRewards = new List<ItemInfo>();
        
        // Ищем подходящий набор наград для класса игрока
        var classRewards = classSpecificRewards.FirstOrDefault(cr => cr.IsForPlayer(playerStats));
        if (classRewards == null)
        {
            Debug.LogWarning($"[ChestItemData] No class-specific rewards found for player class. Using default rewards.");
            return GenerateDefaultRewards(playerStats);
        }
        
        // Обрабатываем гарантированные предметы
        foreach (var reward in classRewards.classRewards.Where(r => r.isGuaranteed))
        {
            if (reward.ShouldGiveToPlayer(playerStats))
            {
                ItemInfo itemInfo = CreateItemInfo(reward);
                if (itemInfo.id > 0)
                {
                    generatedRewards.Add(itemInfo);
                }
            }
        }
        
        // Обрабатываем случайные предметы
        foreach (var reward in classRewards.classRewards.Where(r => !r.isGuaranteed))
        {
            if (reward.ShouldGiveToPlayer(playerStats) && Random.value <= reward.dropChance)
            {
                ItemInfo itemInfo = CreateItemInfo(reward);
                if (itemInfo.id > 0)
                {
                    generatedRewards.Add(itemInfo);
                }
            }
        }
        
        return generatedRewards;
    }
    
    /// <summary>
    /// Генерирует обычные награды
    /// </summary>
    private List<ItemInfo> GenerateDefaultRewards(CharacterStats playerStats)
    {
        List<ItemInfo> generatedRewards = new List<ItemInfo>();
        
        // Обрабатываем гарантированные предметы
        foreach (var reward in rewards.Where(r => r.isGuaranteed))
        {
            if (playerStats == null || reward.ShouldGiveToPlayer(playerStats))
            {
                ItemInfo itemInfo = CreateItemInfo(reward);
                if (itemInfo.id > 0)
                {
                    generatedRewards.Add(itemInfo);
                }
            }
        }
        
        // Обрабатываем случайные предметы
        foreach (var reward in rewards.Where(r => !r.isGuaranteed))
        {
            if ((playerStats == null || reward.ShouldGiveToPlayer(playerStats)) && Random.value <= reward.dropChance)
            {
                ItemInfo itemInfo = CreateItemInfo(reward);
                if (itemInfo.id > 0)
                {
                    generatedRewards.Add(itemInfo);
                }
            }
        }
        
        return generatedRewards;
    }
    
    /// <summary>
    /// Создает ItemInfo для награды
    /// </summary>
    private ItemInfo CreateItemInfo(ChestReward reward)
    {
        Item baseItem = reward.GetItem();
        if (baseItem == null)
        {
            Debug.LogError($"[ChestItemData] Item not found! ID: {reward.GetItemId()}, SO: {(reward.GetItem() != null ? reward.GetItem().name : "null")}");
            return new ItemInfo();
        }
        
        if (reward.rollStats && baseItem.useDynamicStats)
        {
            // Генерируем предмет с динамическими статами
            Item generatedItem = baseItem.GenerateDynamicItem();
            if (generatedItem != null)
            {
                return new ItemInfo
                {
                    id = reward.itemId,
                    quantity = reward.quantity,
                    hasDynamicStats = true,
                    dynamicItemName = generatedItem.itemName,
                    strengthBonus = generatedItem.strengthBonus,
                    agilityBonus = generatedItem.agilityBonus,
                    spiritBonus = generatedItem.spiritBonus,
                    constitutionBonus = generatedItem.constitutionBonus,
                    accuracyBonus = generatedItem.accuracyBonus,
                    minAttackConstantBonus = generatedItem.minAttackConstantBonus,
                    maxAttackConstantBonus = generatedItem.maxAttackConstantBonus,
                    maxHpConstantBonus = generatedItem.maxHpConstantBonus,
                    maxSpConstantBonus = generatedItem.maxSpConstantBonus,
                    crtConstantBonus = generatedItem.crtConstantBonus,
                    mspdConstantBonus = generatedItem.mspdConstantBonus,
                    physicalResist = generatedItem.physicalResist,
                    dynamicRarity = generatedItem.rarity
                };
            }
        }
        
        // Обычный предмет без роллинга статов
        return new ItemInfo
        {
            id = reward.itemId,
            quantity = reward.quantity,
            hasDynamicStats = false
        };
    }
    
    /// <summary>
    /// Получает золото из сундука для конкретного игрока
    /// </summary>
    public int GetGoldReward(CharacterStats playerStats = null)
    {
        if (useClassSpecificRewards && playerStats != null)
        {
            // Используем класс-специфичное золото
            var classRewards = classSpecificRewards.FirstOrDefault(cr => cr.IsForPlayer(playerStats));
            if (classRewards != null)
            {
                if (Random.value <= classRewards.classGoldChance)
                {
                    return classRewards.classGoldReward;
                }
                return 0;
            }
        }
        
        // Используем обычное золото
        if (Random.value <= goldChance)
        {
            return goldReward;
        }
        return 0;
    }
}
