using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemGenerator", menuName = "Tools/Item Generator")]
public class ItemGenerator : ScriptableObject
{
    [Header("Base Item Template")]
    [Tooltip("Базовый предмет, на основе которого будут генерироваться новые предметы")]
    public Item baseItem;
    
    [Header("Generation Settings")]
    [Tooltip("Генерировать предметы в Resources папку")]
    public bool generateToResources = true;
    [Tooltip("Путь для сохранения сгенерированных предметов")]
    public string outputPath = "Resources/Items/Generated/";
    [Tooltip("Добавлять сгенерированные предметы в ItemDatabase")]
    public bool addToItemDatabase = true;
    [Tooltip("Начальный ID для сгенерированных предметов")]
    public int startId = 1000;
    
    [Header("Template System")]
    [Tooltip("Доступные шаблоны предметов")]
    public List<ItemTemplate> availableTemplates = new List<ItemTemplate>();
    
    [Header("Level-Based Generation")]
    [Tooltip("Список уровней для генерации предметов")]
    public List<LevelConfig> levelConfigs = new List<LevelConfig>();
    
    [System.Serializable]
    public class StatTemplate
    {
        public string displayName;
        public Vector2Int range = new Vector2Int(0, 0);
        public float levelMultiplier = 1.0f; // Множитель для увеличения max значения с уровнем
        public bool enabled = true;
        public string itemPropertyName; // Имя поля в Item классе
        
        public StatTemplate(string name, string propertyName, Vector2Int initialRange, float multiplier = 1.0f)
        {
            displayName = name;
            itemPropertyName = propertyName;
            range = initialRange;
            levelMultiplier = multiplier;
        }
        
        public StatTemplate(string name) : this(name, "", new Vector2Int(0, 0), 1.0f) {}
        
        public Vector2Int GetScaledRange(int level)
        {
            if (level <= 1) return range;
            
            int scaledMax = Mathf.RoundToInt(range.y + (level - 1) * levelMultiplier);
            return new Vector2Int(range.x, scaledMax);
        }
    }
    
    [System.Serializable]
    public class ItemTemplate
    {
        public string templateName;
        public Item baseItemTemplate; // Ссылка на предмет как шаблон
        public List<StatTemplate> defaultStats = new List<StatTemplate>();
        
        public ItemTemplate(string name, Item itemTemplate)
        {
            templateName = name;
            baseItemTemplate = itemTemplate;
        }
    }
    
    [System.Serializable]
    public class LevelConfig
    {
        [Tooltip("Уровень предмета")]
        public int level;
        
        [Header("Universal Damage Ranges")]
        [Tooltip("Минимальный урон (мин-макс)")]
        public Vector2Int minDamageRange = new Vector2Int(0, 0);
        [Tooltip("Максимальный урон (мин-макс)")]
        public Vector2Int maxDamageRange = new Vector2Int(0, 0);
        
        [Header("Universal Defense Ranges")]
        [Tooltip("Базовая защита (мин-макс)")]
        public Vector2Int defenseRange = new Vector2Int(0, 0);
        [Tooltip("Физическое сопротивление % (мин-макс)")]
        public Vector2Int physicalResistRange = new Vector2Int(0, 0);
        
        [Header("Universal Special Stats")]
        [Tooltip("Критический удар (мин-макс)")]
        public Vector2Int criticalRange = new Vector2Int(0, 0);
        [Tooltip("Шанс урона % (мин-макс)")]
        public Vector2Int damageChanceRange = new Vector2Int(0, 0);
        [Tooltip("Скорость движения (мин-макс)")]
        public Vector2Int movementSpeedRange = new Vector2Int(0, 0);
        [Tooltip("Уклонение (мин-макс)")]
        public Vector2Int dodgeRange = new Vector2Int(0, 0);
        [Tooltip("Восстановление HP (мин-макс)")]
        public Vector2Int hpRecoveryRange = new Vector2Int(0, 0);
        [Tooltip("Восстановление SP (мин-макс)")]
        public Vector2Int spRecoveryRange = new Vector2Int(0, 0);
        
        [Header("Universal Character Stats")]
        [Tooltip("Сила (мин-макс)")]
        public Vector2Int strengthRange = new Vector2Int(0, 0);
        [Tooltip("Ловкость (мин-макс)")]
        public Vector2Int agilityRange = new Vector2Int(0, 0);
        [Tooltip("Дух (мин-макс)")]
        public Vector2Int spiritRange = new Vector2Int(0, 0);
        [Tooltip("Телосложение (мин-макс)")]
        public Vector2Int constitutionRange = new Vector2Int(0, 0);
        [Tooltip("Точность (мин-макс)")]
        public Vector2Int accuracyRange = new Vector2Int(0, 0);
        [Tooltip("Здоровье (мин-макс)")]
        public Vector2Int healthRange = new Vector2Int(0, 0);
        [Tooltip("Мана (мин-макс)")]
        public Vector2Int manaRange = new Vector2Int(0, 0);
        
        [Header("Item Type Restrictions")]
        [Tooltip("Какие типы предметов могут получать урон (Weapon = true, Armor = false)")]
        public bool canHaveDamage = true;
        [Tooltip("Какие типы предметов могут получать защиту (Armor = true, Weapon = false)")]
        public bool canHaveDefense = true;
        [Tooltip("Какие типы предметов могут получать крит. урон (Helmet = true, other = varies)")]
        public bool canHaveCritical = true;
        [Tooltip("Какие типы предметов могут получать шанс урона (Gloves = true, other = varies)")]
        public bool canHaveDamageChance = true;
        [Tooltip("Какие типы предметов могут получать перемещение (Boots/Necklace = true, other = varies)")]
        public bool canHaveMovementSpeed = true;
        [Tooltip("Какие типы предметов могут получать уклонение (Boots = true, other = varies)")]
        public bool canHaveDodge = true;
        
        [Header("Generation Chances")]
        [Tooltip("Шанс появления каждого стата (0-1)")]
        [Range(0f, 1f)] public float statChance = 0.5f;
    }
    
    public Item GenerateItemForDrop(int level)
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return null;
        }
        
        LevelConfig config = GetConfigForLevel(level);
        if (config == null)
        {
            Debug.LogWarning($"[ItemGenerator] No config found for level {level}, using base item");
            return baseItem;
        }
        
        Item generatedItem = ScriptableObject.CreateInstance(baseItem.GetType()) as Item;
        CopyItemProperties(baseItem, generatedItem);
        
        generatedItem.id = GetNextAvailableId();
        generatedItem.requiredLevel = level;
        
        GenerateUniversalStats(generatedItem, config);
        SetupDynamicRanges(generatedItem, config);
        UpdateItemNameWithStats(generatedItem);
        
        return generatedItem;
    }
    
    /// <summary>
    /// Генерирует предмет с динамическими статами на основе базового предмета (для дропа с монстров)
    /// Использует оригинальный ID предмета вместо нового
    /// </summary>
    public Item GenerateDynamicItemForDrop(int level)
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return null;
        }
        
        LevelConfig config = GetConfigForLevel(level);
        if (config == null)
        {
            Debug.LogWarning($"[ItemGenerator] No config found for level {level}, using base item");
            return baseItem;
        }
        
        Item generatedItem = ScriptableObject.CreateInstance(baseItem.GetType()) as Item;
        CopyItemProperties(baseItem, generatedItem);
        
        // Используем оригинальный ID для динамических предметов
        generatedItem.id = baseItem.id;
        generatedItem.requiredLevel = level;
        
        // Включаем динамические статы
        generatedItem.useDynamicStats = true;
        
        GenerateUniversalStats(generatedItem, config);
        SetupDynamicRanges(generatedItem, config);
        UpdateItemNameWithStats(generatedItem);
        
        return generatedItem;
    }
    
    [ContextMenu("Generate Items")]
    public void GenerateItems()
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return;
        }
        
        List<Item> generatedItems = new List<Item>();
        
        foreach (var config in levelConfigs)
        {
            Item item = GenerateItemForLevel(config.level);
            if (item != null)
            {
                generatedItems.Add(item);
            }
        }
        
        if (generateToResources)
        {
            SaveItemsToResources(generatedItems);
        }
        
        if (addToItemDatabase)
        {
            AddItemsToDatabase(generatedItems);
        }
        
        Debug.Log($"[ItemGenerator] Generated {generatedItems.Count} items from {levelConfigs.Count} level configs");
    }
    
    [ContextMenu("Generate Single Item")]
    public void GenerateSingleItem()
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return;
        }
        
        if (levelConfigs.Count == 0)
        {
            Debug.LogError("[ItemGenerator] No level configs defined!");
            return;
        }
        
        // Генерируем предмет для первого уровня в списке
        var config = levelConfigs[0];
        Item item = GenerateItemForLevel(config.level);
        
        if (item != null)
        {
            if (generateToResources)
            {
                List<Item> singleItem = new List<Item> { item };
                SaveItemsToResources(singleItem);
            }
            
            if (addToItemDatabase)
            {
                List<Item> singleItem = new List<Item> { item };
                AddItemsToDatabase(singleItem);
            }
            
            Debug.Log($"[ItemGenerator] Generated single item: {item.itemName} (Level {item.requiredLevel}, ID: {item.id})");
        }
    }
    
    private Item GenerateItemForLevel(int level)
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return null;
        }
        
        LevelConfig config = GetConfigForLevel(level);
        if (config == null)
        {
            Debug.LogWarning($"[ItemGenerator] No config found for level {level}");
            return null;
        }
        
        Item item = ScriptableObject.CreateInstance(baseItem.GetType()) as Item;
        CopyItemProperties(baseItem, item);
        
        item.id = GetNextAvailableId();
        item.requiredLevel = level;
        
        GenerateUniversalStats(item, config);
        SetupDynamicRanges(item, config);
        UpdateItemNameWithStats(item);
        
        return item;
    }
    
    private LevelConfig GetConfigForLevel(int level)
    {
        LevelConfig exactMatch = levelConfigs.FirstOrDefault(config => config.level == level);
        if (exactMatch != null) return exactMatch;
        
        return levelConfigs.OrderBy(config => Mathf.Abs(config.level - level)).FirstOrDefault();
    }
    
    private void GenerateUniversalStats(Item item, LevelConfig config)
    {
        int statsCount = 0;
        
        // Урон - только если можно
        if (config.canHaveDamage && config.minDamageRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
        {
            item.minAttackConstantBonus = Random.Range(config.minDamageRange.x, config.minDamageRange.y + 1);
            statsCount++;
        }
        
        if (config.canHaveDamage && config.maxDamageRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
        {
            item.maxAttackConstantBonus = Random.Range(config.maxDamageRange.x, config.maxDamageRange.y + 1);
            statsCount++;
        }
        
        // Защита - только если можно
        if (config.canHaveDefense && config.defenseRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
        {
            item.armorBonus = Random.Range(config.defenseRange.x, config.defenseRange.y + 1);
                statsCount++;
            }
            
        if (config.canHaveDefense && config.physicalResistRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
            {
            item.physicalResistBonus = Random.Range(config.physicalResistRange.x, config.physicalResistRange.y + 1);
                statsCount++;
            }
            
        // Остальные универсальные статы
        GenerateRandomStat(item, config.strengthRange, () => item.strengthBonus = Random.Range(config.strengthRange.x, config.strengthRange.y + 1), ref statsCount);
        GenerateRandomStat(item, config.agilityRange, () => item.agilityBonus = Random.Range(config.agilityRange.x, config.agilityRange.y + 1), ref statsCount);
        GenerateRandomStat(item, config.spiritRange, () => item.spiritBonus = Random.Range(config.spiritRange.x, config.spiritRange.y + 1), ref statsCount);
        GenerateRandomStat(item, config.constitutionRange, () => item.constitutionBonus = Random.Range(config.constitutionRange.x, config.constitutionRange.y + 1), ref statsCount);
        GenerateRandomStat(item, config.accuracyRange, () => item.accuracyBonus = Random.Range(config.accuracyRange.x, config.accuracyRange.y + 1), ref statsCount);
        
        if (config.canHaveCritical && config.criticalRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
        {
            item.crtConstantBonus = Random.Range(config.criticalRange.x, config.criticalRange.y + 1);
                statsCount++;
            }
            
        if (config.canHaveDamageChance && config.damageChanceRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
            {
            item.damageChanceBonus = Random.Range(config.damageChanceRange.x, config.damageChanceRange.y + 1);
                statsCount++;
            }
            
        if (config.canHaveMovementSpeed && config.movementSpeedRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
            {
            item.mspdConstantBonus = Random.Range(config.movementSpeedRange.x, config.movementSpeedRange.y + 1);
                statsCount++;
            }
            
        if (config.canHaveDodge && config.dodgeRange.y > 0 && Random.Range(0f, 1f) <= config.statChance)
            {
            item.dodgeBonus = Random.Range(config.dodgeRange.x, config.dodgeRange.y + 1);
                statsCount++;
            }
            
        Debug.Log($"[ItemGenerator] Generated {statsCount} stats total");
    }
    
    private void SetupDynamicRanges(Item item, LevelConfig config)
    {
        // Настраиваем диапазоны для динамической генерации при выпадении с монстров
        if (config.canHaveDamage)
        {
            if (config.minDamageRange.y > 0)
            {
                item.minDamageRange = new Item.StatRange 
                { 
                    minValue = config.minDamageRange.x, 
                    maxValue = config.minDamageRange.y, 
                    chance = config.statChance 
                };
            }
            
            if (config.maxDamageRange.y > 0)
            {
                item.maxDamageRange = new Item.StatRange 
                { 
                    minValue = config.maxDamageRange.x, 
                    maxValue = config.maxDamageRange.y, 
                    chance = config.statChance 
                };
            }
        }
        
        if (config.canHaveDefense)
        {
            if (config.defenseRange.y > 0)
            {
                item.armorRange = new Item.StatRange 
                { 
                    minValue = config.defenseRange.x, 
                    maxValue = config.defenseRange.y, 
                    chance = config.statChance 
                };
            }
            
            if (config.physicalResistRange.y > 0)
            {
                item.physicalResistRange = new Item.StatRange 
                { 
                    minValue = config.physicalResistRange.x, 
                    maxValue = config.physicalResistRange.y, 
                    chance = config.statChance 
                };
            }
        }
        
        // Настройка основных характеристик
        if (config.strengthRange.y > 0)
        {
        item.strengthRange = new Item.StatRange 
        { 
            minValue = config.strengthRange.x, 
            maxValue = config.strengthRange.y, 
            chance = config.statChance 
        };
        }
        
        if (config.agilityRange.y > 0)
        {
        item.agilityRange = new Item.StatRange 
        { 
            minValue = config.agilityRange.x, 
            maxValue = config.agilityRange.y, 
            chance = config.statChance 
        };
        }
        
        if (config.spiritRange.y > 0)
        {
        item.spiritRange = new Item.StatRange 
        { 
            minValue = config.spiritRange.x, 
            maxValue = config.spiritRange.y, 
            chance = config.statChance 
        };
        }
        
        if (config.constitutionRange.y > 0)
        {
        item.constitutionRange = new Item.StatRange 
        { 
            minValue = config.constitutionRange.x, 
            maxValue = config.constitutionRange.y, 
            chance = config.statChance 
        };
        }
        
        if (config.accuracyRange.y > 0)
        {
        item.accuracyRange = new Item.StatRange 
        { 
            minValue = config.accuracyRange.x, 
            maxValue = config.accuracyRange.y, 
            chance = config.statChance 
        };
        }
        
        // Специальные статы
        if (config.canHaveCritical && config.criticalRange.y > 0)
        {
        item.criticalRange = new Item.StatRange 
        { 
            minValue = config.criticalRange.x, 
            maxValue = config.criticalRange.y, 
            chance = config.statChance 
        };
        }
        
        if (config.canHaveDamageChance && config.damageChanceRange.y > 0)
        {
            item.damageChanceRange = new Item.StatRange 
            { 
                minValue = config.damageChanceRange.x, 
                maxValue = config.damageChanceRange.y, 
                chance = config.statChance 
            };
        }
        
        if (config.canHaveMovementSpeed && config.movementSpeedRange.y > 0)
        {
        item.movementSpeedRange = new Item.StatRange 
        { 
            minValue = config.movementSpeedRange.x, 
            maxValue = config.movementSpeedRange.y, 
            chance = config.statChance 
        };
        }
        
        if (config.canHaveDodge && config.dodgeRange.y > 0)
        {
        item.dodgeRange = new Item.StatRange 
        { 
            minValue = config.dodgeRange.x, 
            maxValue = config.dodgeRange.y, 
            chance = config.statChance 
        };
        }
        
        // Включаем использование динамических статов
        item.useDynamicStats = true;
        
        Debug.Log($"[ItemGenerator] Setup dynamic ranges for {item.itemName}");
    }
    
    private void GenerateRandomStat(Item item, Vector2Int range, System.Action action, ref int count)
    {
        if (range.y > 0 && Random.Range(0f, 1f) <= 0.5f) // вероятность для универсальных статов
        {
            action();
            count++;
        }
    }
    
    private void CopyItemProperties(Item source, Item target)
    {
        target.itemName = source.itemName;
        target.originalName = !string.IsNullOrEmpty(source.originalName) ? source.originalName : source.itemName;
        target.itemType = source.itemType;
        target.equipmentSlot = source.equipmentSlot;
        target.maxStack = source.maxStack;
        target.canDrop = source.canDrop;
        target.canSell = source.canSell;
        target.canUse = source.canUse;
        target.icon = source.icon;
        target.price = source.price;
        
        // Обнуляем статы для новой генерации
        target.minAttackConstantBonus = 0;
        target.maxAttackConstantBonus = 0;
        target.armorBonus = 0;
        target.physicalResistBonus = 0;
        target.strengthBonus = 0;
        target.agilityBonus = 0;
        target.spiritBonus = 0;
        target.constitutionBonus = 0;
        target.accuracyBonus = 0;
        target.crtConstantBonus = 0;
        target.damageChanceBonus = 0;
        target.mspdConstantBonus = 0;
        target.dodgeBonus = 0;
    }
    
    private void UpdateItemNameWithStats(Item item)
    {
        string baseName = !string.IsNullOrEmpty(item.originalName) ? item.originalName : item.itemName;
        item.itemName = baseName; // Простая реализация без префиксов для начала
    }
    
    private int GetNextAvailableId()
    {
        return startId + Random.Range(1, 1000); // Простая генерация ID
    }
    
    [ContextMenu("Create Sword Template")]
    public void CreateSwordTemplate()
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Set base item first!");
            return;
        }
        
        // Создаем шаблон меча
        ItemTemplate swordTemplate = new ItemTemplate("Sword", baseItem);
        
        // Добавляем базовые характеристики меча
        swordTemplate.defaultStats.Add(new StatTemplate("Min Damage", "minAttackConstantBonus", new Vector2Int(0, 15), 3.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Max Damage", "maxAttackConstantBonus", new Vector2Int(0, 25), 5.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Attack Speed", "attackSpeedBonus", new Vector2Int(0, 5), 0.5f));
        
        // Базовые характеристики игрока
        swordTemplate.defaultStats.Add(new StatTemplate("Strength", "strengthBonus", new Vector2Int(0, 3), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("Agility", "agilityBonus", new Vector2Int(0, 3), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("Spirit", "spiritBonus", new Vector2Int(0, 2), 1.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Constitution", "constitutionBonus", new Vector2Int(0, 3), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("Accuracy", "accuracyBonus", new Vector2Int(0, 2), 1.0f));
        
        // Боевые характеристики
        swordTemplate.defaultStats.Add(new StatTemplate("Critical Damage", "criticalBonus", new Vector2Int(0, 5), 2.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Hit Rate", "hitRateBonus", new Vector2Int(0, 8), 2.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Dodge", "dodgeBonus", new Vector2Int(0, 3), 1.0f));
        
        Debug.Log($"[ItemGenerator] Created Sword Template with {swordTemplate.defaultStats.Count} stats");
    }
    
    [ContextMenu("Generate Sample Configuration")]
    public void GenerateSampleConfiguration()
    {
        levelConfigs.Clear();
        Debug.Log("[ItemGenerator] Generated loot items configuration for monster drops");
        
        // Пример конфигурации для уровня 5 - предметы для лута с монстров
        levelConfigs.Add(new LevelConfig
        {
            level = 5,
            minDamageRange = new Vector2Int(8, 25),      // Минимальный урон: 8-25
            maxDamageRange = new Vector2Int(25, 45),     // Максимальный урон: 25-45  
            defenseRange = new Vector2Int(5, 15),        // Защита: 5-15
            physicalResistRange = new Vector2Int(2, 8),  // Физическое сопротивление: 2-8
            strengthRange = new Vector2Int(1, 5),        // Сила: 1-5
            agilityRange = new Vector2Int(1, 5),          // Ловкость: 1-5
            spiritRange = new Vector2Int(1, 4),           // Дух: 1-4
            constitutionRange = new Vector2Int(1, 6),    // Телосложение: 1-6
            accuracyRange = new Vector2Int(1, 4),         // Точность: 1-4
            criticalRange = new Vector2Int(1, 8),         // Критический урон: 1-8%
            damageChanceRange = new Vector2Int(1, 6),     // Шанс урона: 1-6%
            movementSpeedRange = new Vector2Int(1, 3),    // Скорость движения: 1-3
            dodgeRange = new Vector2Int(1, 5),            // Уворот: 1-5%
            statChance = 0.7f                             // 70% шанс получить каждый стат
        });
        
        // Конфигурация для уровня 10 - более сильные предметы
        levelConfigs.Add(new LevelConfig
        {
            level = 10,
            minDamageRange = new Vector2Int(15, 40),     // Минимальный урон: 15-40
            maxDamageRange = new Vector2Int(40, 70),     // Максимальный урон: 40-70  
            defenseRange = new Vector2Int(10, 25),        // Защита: 10-25
            physicalResistRange = new Vector2Int(5, 15),  // Физическое сопротивление: 5-15
            strengthRange = new Vector2Int(2, 8),         // Сила: 2-8
            agilityRange = new Vector2Int(2, 8),          // Ловкость: 2-8
            spiritRange = new Vector2Int(2, 6),           // Дух: 2-6
            constitutionRange = new Vector2Int(2, 10),    // Телосложение: 2-10
            accuracyRange = new Vector2Int(2, 6),         // Точность: 2-6
            criticalRange = new Vector2Int(2, 12),        // Критический урон: 2-12%
            damageChanceRange = new Vector2Int(2, 10),    // Шанс урона: 2-10%
            movementSpeedRange = new Vector2Int(2, 5),    // Скорость движения: 2-5
            dodgeRange = new Vector2Int(2, 8),             // Уворот: 2-8%
            statChance = 0.75f                            // 75% шанс получить каждый стат
        });
        
        // Конфигурация для уровня 15 - самые крутые предметы
        levelConfigs.Add(new LevelConfig
        {
            level = 15,
            minDamageRange = new Vector2Int(25, 60),     // Минимальный урон: 25-60
            maxDamageRange = new Vector2Int(60, 100),    // Максимальный урон: 60-100  
            defenseRange = new Vector2Int(20, 40),        // Защита: 20-40
            physicalResistRange = new Vector2Int(8, 20),  // Физическое сопротивление: 8-20
            strengthRange = new Vector2Int(3, 12),        // Сила: 3-12
            agilityRange = new Vector2Int(3, 12),         // Ловкость: 3-12
            spiritRange = new Vector2Int(3, 10),           // Дух: 3-10
            constitutionRange = new Vector2Int(3, 15),    // Телосложение: 3-15
            accuracyRange = new Vector2Int(3, 10),         // Точность: 3-10
            criticalRange = new Vector2Int(3, 18),        // Критический урон: 3-18%
            damageChanceRange = new Vector2Int(3, 15),     // Шанс урона: 3-15%
            movementSpeedRange = new Vector2Int(3, 8),    // Скорость движения: 3-8
            dodgeRange = new Vector2Int(3, 12),            // Уворот: 3-12%
            statChance = 0.8f                             // 80% шанс получить каждый стат
        });
    }
    
    private void SaveItemsToResources(List<Item> items)
    {
        #if UNITY_EDITOR
        string fullPath = "Assets/" + outputPath;
        if (!System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.CreateDirectory(fullPath);
            Debug.Log($"[ItemGenerator] Created directory: {fullPath}");
        }
        
        foreach (Item item in items)
        {
            string fileName = $"{item.itemName.Replace(" ", "_")}_Lv{item.requiredLevel}.asset";
            string assetPath = fullPath + fileName;
            
            UnityEditor.AssetDatabase.CreateAsset(item, assetPath);
            Debug.Log($"[ItemGenerator] Saved item: {assetPath}");
        }
        
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log($"[ItemGenerator] Saved {items.Count} items to {fullPath}");
        #endif
    }
    
    private void AddItemsToDatabase(List<Item> items)
    {
        #if UNITY_EDITOR
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogError("[ItemGenerator] Cannot add items to database: ItemDatabase not found in Resources!");
            return;
        }
        
        Item[] existingItems = database.GetAllItems();
        List<Item> allItems = new List<Item>(existingItems);
        allItems.AddRange(items);
        
        var serializedObject = new UnityEditor.SerializedObject(database);
        var itemsProperty = serializedObject.FindProperty("items");
        itemsProperty.arraySize = allItems.Count;
        
        for (int i = 0; i < allItems.Count; i++)
        {
            itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = allItems[i];
        }
        
        serializedObject.ApplyModifiedProperties();
        UnityEditor.EditorUtility.SetDirty(database);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log($"[ItemGenerator] Added {items.Count} items to database");
        #endif
    }
}
