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
    public int itemId = -1;
    public int quantity = 1;
    public float dropChance = 1.0f; // Шанс выпадения (0.0 - 1.0)
    
    [Header("Роллинг статов")]
    public bool rollStats = false; // Роллить ли статы как при дропе с монстра
    public int level = 1; // Уровень для роллинга статов
    
    [Header("Дополнительные настройки")]
    public bool isGuaranteed = false; // Гарантированный предмет (игнорирует dropChance)
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
    
    [Header("Дополнительные награды")]
    public int goldReward = 0; // Золото
    public float goldChance = 1.0f; // Шанс получения золота
    
    /// <summary>
    /// Генерирует награды из сундука
    /// </summary>
    public List<ItemInfo> GenerateRewards()
    {
        List<ItemInfo> generatedRewards = new List<ItemInfo>();
        
        // Обрабатываем гарантированные предметы
        foreach (var reward in rewards.Where(r => r.isGuaranteed))
        {
            ItemInfo itemInfo = CreateItemInfo(reward);
            if (itemInfo.id > 0)
            {
                generatedRewards.Add(itemInfo);
            }
        }
        
        // Обрабатываем случайные предметы
        foreach (var reward in rewards.Where(r => !r.isGuaranteed))
        {
            if (Random.value <= reward.dropChance)
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
        Item baseItem = ItemDatabase.Instance?.GetItem(reward.itemId);
        if (baseItem == null)
        {
            Debug.LogError($"[ChestItemData] Item with ID {reward.itemId} not found!");
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
    /// Получает золото из сундука
    /// </summary>
    public int GetGoldReward()
    {
        if (Random.value <= goldChance)
        {
            return goldReward;
        }
        return 0;
    }
}
