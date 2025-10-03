using UnityEngine;

[System.Serializable]
public struct ItemInfo
{
    public int id;
    public int quantity;
    
    // Динамические статы (если предмет использует динамическую генерацию)
    public bool hasDynamicStats;
    public string dynamicItemName;
    public int strengthBonus;
    public int agilityBonus;
    public int spiritBonus;
    public int constitutionBonus;
    public int accuracyBonus;
    public int minAttackConstantBonus;
    public int maxAttackConstantBonus;
    public int maxHpConstantBonus;
    public int maxSpConstantBonus;
    public int crtConstantBonus;
    public float mspdConstantBonus;
    public int physicalResist; // УСТАРЕЛО: используйте armorBonus и physicalResistBonus
    public int armorBonus; // Плоская броня
    public int physicalResistBonus; // Процентное сопротивление
    public int hpRecoveryBonus;
    public int spRecoveryBonus;
    public int dodgeBonus;
    public Rarity dynamicRarity;
    
    private static Item cachedItem = null;
    private static int cachedItemId = -1;
    
    public Item GetItem()
    {
        if (id <= 0) return null;
        
        // Кэшируем последний запрошенный предмет
        if (cachedItemId == id && cachedItem != null)
        {
            return cachedItem;
        }
        
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogError("[ItemInfo] ItemDatabase not found in Resources!");
            return null;
        }
        Item item = database.GetItem(id);
        if (item == null)
        {
            Debug.LogError($"[ItemInfo] Failed to load Item with ID: {id}");
            return null;
        }
        
        // Кэшируем для следующего запроса
        cachedItem = item;
        cachedItemId = id;
        
        return item;
    }
    
    /// <summary>
    /// Получает имя предмета с учетом динамических статов
    /// </summary>
    public string GetItemName()
    {
        if (hasDynamicStats && !string.IsNullOrEmpty(dynamicItemName))
        {
            return dynamicItemName;
        }
        
        Item item = GetItem();
        return item?.itemName ?? "Unknown Item";
    }
    
    /// <summary>
    /// Получает редкость предмета с учетом динамических статов
    /// </summary>
    public Rarity GetItemRarity()
    {
        if (hasDynamicStats)
        {
            return dynamicRarity;
        }
        
        Item item = GetItem();
        return item?.rarity ?? Rarity.Common;
    }
    
    /// <summary>
    /// Получает итоговые статы предмета (базовые + динамические)
    /// </summary>
    public float GetTotalStatBonus(StatType statType)
    {
        Item item = GetItem();
        if (item == null) return 0;
        
        float baseStat = 0;
        float dynamicStat = 0;
        
        switch (statType)
        {
            case StatType.Strength:
                baseStat = item.strengthBonus;
                dynamicStat = hasDynamicStats ? strengthBonus : 0;
                break;
            case StatType.Agility:
                baseStat = item.agilityBonus;
                dynamicStat = hasDynamicStats ? agilityBonus : 0;
                break;
            case StatType.Spirit:
                baseStat = item.spiritBonus;
                dynamicStat = hasDynamicStats ? spiritBonus : 0;
                break;
            case StatType.Constitution:
                baseStat = item.constitutionBonus;
                dynamicStat = hasDynamicStats ? constitutionBonus : 0;
                break;
            case StatType.Accuracy:
                baseStat = item.accuracyBonus;
                dynamicStat = hasDynamicStats ? accuracyBonus : 0;
                break;
            case StatType.MinAttack:
                baseStat = item.minAttackConstantBonus;
                dynamicStat = hasDynamicStats ? minAttackConstantBonus : 0;
                break;
            case StatType.MaxAttack:
                baseStat = item.maxAttackConstantBonus;
                dynamicStat = hasDynamicStats ? maxAttackConstantBonus : 0;
                break;
            case StatType.MaxHP:
                baseStat = item.maxHpConstantBonus;
                dynamicStat = hasDynamicStats ? maxHpConstantBonus : 0;
                break;
            case StatType.MaxMP:
                baseStat = item.maxSpConstantBonus;
                dynamicStat = hasDynamicStats ? maxSpConstantBonus : 0;
                break;
            case StatType.Critical:
                baseStat = item.crtConstantBonus;
                dynamicStat = hasDynamicStats ? crtConstantBonus : 0;
                break;
            case StatType.MovementSpeed:
                baseStat = item.mspdConstantBonus;
                dynamicStat = hasDynamicStats ? mspdConstantBonus : 0;
                break;
            case StatType.PhysicalResist: // УСТАРЕЛО
                baseStat = item.physicalResist;
                dynamicStat = hasDynamicStats ? physicalResist : 0;
                break;
            case StatType.Armor:
                baseStat = item.armorBonus;
                dynamicStat = hasDynamicStats ? armorBonus : 0;
                break;
            case StatType.PhysicalResistance:
                baseStat = item.physicalResistBonus;
                dynamicStat = hasDynamicStats ? physicalResistBonus : 0;
                break;
            case StatType.HPRecovery:
                baseStat = item.hpRecoveryBonus;
                dynamicStat = hasDynamicStats ? hpRecoveryBonus : 0;
                break;
            case StatType.SPRecovery:
                baseStat = item.spRecoveryBonus;
                dynamicStat = hasDynamicStats ? spRecoveryBonus : 0;
                break;
            case StatType.Dodge:
                baseStat = item.dodgeBonus;
                dynamicStat = hasDynamicStats ? dodgeBonus : 0;
                break;
        }
        
        return baseStat + dynamicStat;
    }
    
    /// <summary>
    /// Получает визуальное значение для отображения в UI (MovementSpeed умножается на 100 для отображения)
    /// </summary>
    public int GetDisplayValue(StatType statType)
    {
        Item item = GetItem();
        if (item == null) return 0;
        
        if (statType == StatType.MovementSpeed)
        {
            int baseValue = Mathf.RoundToInt(item.mspdConstantBonus * 100);
            int dynamicValue = hasDynamicStats ? Mathf.RoundToInt(mspdConstantBonus * 100) : 0;
            return baseValue + dynamicValue;
        }
        
        // Для всех других статов возвращаем обычное значение
        return Mathf.RoundToInt(GetTotalStatBonus(statType));
    }
    
    public enum StatType
    {
        Strength,
        Agility,
        AttackSpeed,
        Spirit,
        Constitution,
        Accuracy,
        MinAttack,
        MaxAttack,
        MaxHP,
        MaxMP,
        Critical,
        MovementSpeed,
        PhysicalResist, // УСТАРЕЛО: используйте Armor и PhysicalResistance
        Armor, // Плоская броня
        PhysicalResistance, // Процентное сопротивление
        HPRecovery,
        SPRecovery,
        Dodge
    }
    
}