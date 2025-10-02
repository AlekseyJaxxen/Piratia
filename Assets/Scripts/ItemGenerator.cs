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
    
    [Header("Level-Based Generation")]
    [Tooltip("Список уровней для генерации предметов")]
    public List<LevelConfig> levelConfigs = new List<LevelConfig>();
    
    [System.Serializable]
    public class LevelConfig
    {
        [Tooltip("Уровень предмета")]
        public int level;
        
        [Header("Damage Ranges")]
        [Tooltip("Минимальный урон (мин-макс)")]
        public Vector2Int minDamageRange = new Vector2Int(0, 0);
        [Tooltip("Максимальный урон (мин-макс)")]
        public Vector2Int maxDamageRange = new Vector2Int(0, 0);
        
        [Header("Stat Ranges")]
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
        [Tooltip("Защита (мин-макс)")]
        public Vector2Int defenseRange = new Vector2Int(0, 0);
        [Tooltip("Критический удар (мин-макс)")]
        public Vector2Int criticalRange = new Vector2Int(0, 0);
        [Tooltip("Скорость движения (мин-макс)")]
        public Vector2Int movementSpeedRange = new Vector2Int(0, 0);
        [Tooltip("Восстановление HP (мин-макс)")]
        public Vector2Int hpRecoveryRange = new Vector2Int(0, 0);
        [Tooltip("Восстановление SP (мин-макс)")]
        public Vector2Int spRecoveryRange = new Vector2Int(0, 0);
        [Tooltip("Уклонение (мин-макс)")]
        public Vector2Int dodgeRange = new Vector2Int(0, 0);
        
        [Header("Chances")]
        [Tooltip("Шанс появления каждого стата (0-1)")]
        [Range(0f, 1f)] public float statChance = 0.5f;
    }
    
    /// <summary>
    /// Генерирует предмет для дропа с монстра на основе базового предмета и уровня
    /// </summary>
    public Item GenerateItemForDrop(int level)
    {
        Debug.Log($"[ItemGenerator] Starting item generation for level {level}");
        
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return null;
        }
        
        Debug.Log($"[ItemGenerator] Using base item: {baseItem.itemName} (ID: {baseItem.id})");
        
        // Находим конфигурацию для указанного уровня
        LevelConfig config = GetConfigForLevel(level);
        if (config == null)
        {
            Debug.LogWarning($"[ItemGenerator] No config found for level {level}, using base item");
            return baseItem;
        }
        
        Debug.Log($"[ItemGenerator] Found config for level {config.level} with stat chance: {config.statChance}");
        
        // Создаем копию базового предмета с сохранением типа (Item, SwordItem, etc.)
        Item generatedItem = ScriptableObject.CreateInstance(baseItem.GetType()) as Item;
        CopyItemProperties(baseItem, generatedItem);
        
        // Присваиваем уникальный ID
        int newId = GetNextAvailableId();
        generatedItem.id = newId;
        Debug.Log($"[ItemGenerator] Assigned new ID: {newId}");
        
        // Убеждаемся, что originalName установлено правильно
        if (string.IsNullOrEmpty(generatedItem.originalName))
        {
            generatedItem.originalName = baseItem.itemName;
        }
        
        // Устанавливаем уровень
        generatedItem.requiredLevel = level;
        
        // Генерируем статы на основе конфигурации
        GenerateStatsForItem(generatedItem, config);
        
        // Обновляем имя с префиксами
        string originalName = generatedItem.itemName;
        UpdateItemNameWithStats(generatedItem);
        Debug.Log($"[ItemGenerator] Name updated: '{originalName}' -> '{generatedItem.itemName}'");
        
        // Добавляем в базу данных если нужно
        if (addToItemDatabase)
        {
            AddSingleItemToDatabase(generatedItem);
        }
        
        Debug.Log($"[ItemGenerator] Successfully generated item: {generatedItem.itemName} (ID: {generatedItem.id}, Level: {generatedItem.requiredLevel}, Rarity: {generatedItem.rarity})");
        return generatedItem;
    }
    
    /// <summary>
    /// Генерирует предмет с динамическими статами на основе базового предмета (для дропа с монстров)
    /// </summary>
    public Item GenerateDynamicItemForDrop(int level)
    {
        Debug.Log($"[ItemGenerator] Starting dynamic item generation for level {level}");
        
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return null;
        }
        
        Debug.Log($"[ItemGenerator] Using base item: {baseItem.itemName} (ID: {baseItem.id})");
        
        // Находим конфигурацию для указанного уровня
        LevelConfig config = GetConfigForLevel(level);
        if (config == null)
        {
            Debug.LogWarning($"[ItemGenerator] No config found for level {level}, using base item");
            return baseItem;
        }
        
        Debug.Log($"[ItemGenerator] Found config for level {config.level} with stat chance: {config.statChance}");
        
        // Создаем копию базового предмета с сохранением типа (Item, SwordItem, etc.)
        Item generatedItem = ScriptableObject.CreateInstance(baseItem.GetType()) as Item;
        CopyItemProperties(baseItem, generatedItem);
        
        // Сохраняем оригинальный ID
        generatedItem.id = baseItem.id;
        Debug.Log($"[ItemGenerator] Using original ID: {baseItem.id}");
        
        // Убеждаемся, что originalName установлено правильно
        if (string.IsNullOrEmpty(generatedItem.originalName))
        {
            generatedItem.originalName = baseItem.itemName;
        }
        
        // Устанавливаем уровень
        generatedItem.requiredLevel = level;
        
        // Включаем динамические статы
        generatedItem.useDynamicStats = true;
        Debug.Log($"[ItemGenerator] Enabled dynamic stats");
        
        // Настраиваем диапазоны статов на основе конфигурации
        SetupStatRanges(generatedItem, config);
        Debug.Log($"[ItemGenerator] Configured stat ranges");
        
        // Генерируем случайные статы
        GenerateRandomStats(generatedItem);
        Debug.Log($"[ItemGenerator] Generated random stats");
        
        // Обновляем имя с префиксами
        string originalName = generatedItem.itemName;
        UpdateItemNameWithStats(generatedItem);
        Debug.Log($"[ItemGenerator] Name updated: '{originalName}' -> '{generatedItem.itemName}'");
        
        Debug.Log($"[ItemGenerator] Successfully generated dynamic item: {generatedItem.itemName} (ID: {generatedItem.id}, Level: {generatedItem.requiredLevel}, Rarity: {generatedItem.rarity})");
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
            Debug.Log($"[ItemGenerator] Generated single item: {item.itemName} (Level {item.requiredLevel}, ID: {item.id})");
        }
    }
    
    [ContextMenu("Generate Sample Configuration")]
    public void GenerateSampleConfiguration()
    {
        levelConfigs.Clear();
        
        // Добавляем примеры конфигураций для разных уровней
        levelConfigs.Add(new LevelConfig
        {
            level = 5,
            minDamageRange = new Vector2Int(15, 20),
            maxDamageRange = new Vector2Int(20, 23),
            strengthRange = new Vector2Int(0, 1),
            agilityRange = new Vector2Int(0, 1),
            statChance = 0.3f
        });
        
        levelConfigs.Add(new LevelConfig
        {
            level = 10,
            minDamageRange = new Vector2Int(25, 35),
            maxDamageRange = new Vector2Int(35, 45),
            strengthRange = new Vector2Int(0, 2),
            agilityRange = new Vector2Int(0, 2),
            constitutionRange = new Vector2Int(0, 1),
            statChance = 0.4f
        });
        
        levelConfigs.Add(new LevelConfig
        {
            level = 20,
            minDamageRange = new Vector2Int(50, 70),
            maxDamageRange = new Vector2Int(70, 90),
            strengthRange = new Vector2Int(0, 4),
            agilityRange = new Vector2Int(0, 4),
            constitutionRange = new Vector2Int(0, 2),
            accuracyRange = new Vector2Int(0, 2),
            statChance = 0.5f
        });
        
        levelConfigs.Add(new LevelConfig
        {
            level = 50,
            minDamageRange = new Vector2Int(150, 160),
            maxDamageRange = new Vector2Int(170, 190),
            strengthRange = new Vector2Int(0, 12),
            agilityRange = new Vector2Int(0, 12),
            constitutionRange = new Vector2Int(0, 8),
            accuracyRange = new Vector2Int(0, 8),
            spiritRange = new Vector2Int(0, 6),
            statChance = 0.6f
        });
        
        Debug.Log("[ItemGenerator] Generated sample configuration with 4 level configs");
    }
    
    private Item GenerateItemForLevel(int level)
    {
        if (baseItem == null)
        {
            Debug.LogError("[ItemGenerator] Base item is not set!");
            return null;
        }
        
        // Находим конфигурацию для указанного уровня
        LevelConfig config = GetConfigForLevel(level);
        if (config == null)
        {
            Debug.LogWarning($"[ItemGenerator] No config found for level {level}");
            return null;
        }
        
        // Создаем копию базового предмета с сохранением типа (Item, SwordItem, etc.)
        Item item = ScriptableObject.CreateInstance(baseItem.GetType()) as Item;
        CopyItemProperties(baseItem, item);
        
        // Присваиваем уникальный ID
        item.id = GetNextAvailableId();
        
        // Устанавливаем уровень
        item.requiredLevel = level;
        
        // Генерируем статы на основе конфигурации
        GenerateStatsForItem(item, config);
        
        // Обновляем имя с префиксами
        UpdateItemNameWithStats(item);
        
        return item;
    }
    
    private LevelConfig GetConfigForLevel(int level)
    {
        // Ищем точное совпадение уровня
        LevelConfig exactMatch = levelConfigs.FirstOrDefault(config => config.level == level);
        if (exactMatch != null) return exactMatch;
        
        // Если точного совпадения нет, ищем ближайший уровень
        return levelConfigs.OrderBy(config => Mathf.Abs(config.level - level)).FirstOrDefault();
    }
    
    private void GenerateStatsForItem(Item item, LevelConfig config)
    {
        Debug.Log($"[ItemGenerator] Generating stats for item with stat chance: {config.statChance}");
        
        // Генерируем урон
        if (config.minDamageRange.y > 0)
        {
            item.minAttackConstantBonus = Random.Range(config.minDamageRange.x, config.minDamageRange.y + 1);
            Debug.Log($"[ItemGenerator] Generated min damage: {item.minAttackConstantBonus} (range: {config.minDamageRange.x}-{config.minDamageRange.y})");
        }
        
        if (config.maxDamageRange.y > 0)
        {
            item.maxAttackConstantBonus = Random.Range(config.maxDamageRange.x, config.maxDamageRange.y + 1);
            Debug.Log($"[ItemGenerator] Generated max damage: {item.maxAttackConstantBonus} (range: {config.maxDamageRange.x}-{config.maxDamageRange.y})");
        }
        
        // Генерируем статы с учетом шанса
        float roll = Random.Range(0f, 1f);
        bool statsGenerated = roll <= config.statChance;
        Debug.Log($"[ItemGenerator] Stat generation roll: {roll:F3} <= {config.statChance:F3} = {statsGenerated}");
        
        if (statsGenerated)
        {
            int statsCount = 0;
            
            if (config.strengthRange.y > 0)
            {
                item.strengthBonus = Random.Range(config.strengthRange.x, config.strengthRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated strength: {item.strengthBonus} (range: {config.strengthRange.x}-{config.strengthRange.y})");
                statsCount++;
            }
            
            if (config.agilityRange.y > 0)
            {
                item.agilityBonus = Random.Range(config.agilityRange.x, config.agilityRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated agility: {item.agilityBonus} (range: {config.agilityRange.x}-{config.agilityRange.y})");
                statsCount++;
            }
            
            if (config.spiritRange.y > 0)
            {
                item.spiritBonus = Random.Range(config.spiritRange.x, config.spiritRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated spirit: {item.spiritBonus} (range: {config.spiritRange.x}-{config.spiritRange.y})");
                statsCount++;
            }
            
            if (config.constitutionRange.y > 0)
            {
                item.constitutionBonus = Random.Range(config.constitutionRange.x, config.constitutionRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated constitution: {item.constitutionBonus} (range: {config.constitutionRange.x}-{config.constitutionRange.y})");
                statsCount++;
            }
            
            if (config.accuracyRange.y > 0)
            {
                item.accuracyBonus = Random.Range(config.accuracyRange.x, config.accuracyRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated accuracy: {item.accuracyBonus} (range: {config.accuracyRange.x}-{config.accuracyRange.y})");
                statsCount++;
            }
            
            if (config.healthRange.y > 0)
            {
                int healthBonus = Random.Range(config.healthRange.x, config.healthRange.y + 1);
                item.maxHpConstantBonus = healthBonus; // Прямое значение HP
                Debug.Log($"[ItemGenerator] Generated health: {healthBonus} HP (range: {config.healthRange.x}-{config.healthRange.y})");
                statsCount++;
            }
            
            if (config.manaRange.y > 0)
            {
                int manaBonus = Random.Range(config.manaRange.x, config.manaRange.y + 1);
                item.maxSpConstantBonus = manaBonus * 5; // 1 очко = 5 MP
                Debug.Log($"[ItemGenerator] Generated mana: {manaBonus} -> {item.maxSpConstantBonus} MP (range: {config.manaRange.x}-{config.manaRange.y})");
                statsCount++;
            }
            
            if (config.defenseRange.y > 0)
            {
                item.physicalResist = Random.Range(config.defenseRange.x, config.defenseRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated defense: {item.physicalResist} (range: {config.defenseRange.x}-{config.defenseRange.y})");
                statsCount++;
            }
            
            if (config.criticalRange.y > 0)
            {
                item.crtConstantBonus = Random.Range(config.criticalRange.x, config.criticalRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated critical: {item.crtConstantBonus} (range: {config.criticalRange.x}-{config.criticalRange.y})");
                statsCount++;
            }
            
            if (config.movementSpeedRange.y > 0)
            {
                item.mspdConstantBonus = Random.Range(config.movementSpeedRange.x, config.movementSpeedRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated movement speed: {item.mspdConstantBonus} (range: {config.movementSpeedRange.x}-{config.movementSpeedRange.y})");
                statsCount++;
            }
            
            if (config.hpRecoveryRange.y > 0)
            {
                item.hpRecoveryBonus = Random.Range(config.hpRecoveryRange.x, config.hpRecoveryRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated HP recovery: {item.hpRecoveryBonus} (range: {config.hpRecoveryRange.x}-{config.hpRecoveryRange.y})");
                statsCount++;
            }
            
            if (config.spRecoveryRange.y > 0)
            {
                item.spRecoveryBonus = Random.Range(config.spRecoveryRange.x, config.spRecoveryRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated SP recovery: {item.spRecoveryBonus} (range: {config.spRecoveryRange.x}-{config.spRecoveryRange.y})");
                statsCount++;
            }
            
            if (config.dodgeRange.y > 0)
            {
                item.dodgeBonus = Random.Range(config.dodgeRange.x, config.dodgeRange.y + 1);
                Debug.Log($"[ItemGenerator] Generated dodge: {item.dodgeBonus} (range: {config.dodgeRange.x}-{config.dodgeRange.y})");
                statsCount++;
            }
            
            Debug.Log($"[ItemGenerator] Generated {statsCount} stats total");
        }
        else
        {
            Debug.Log($"[ItemGenerator] No stats generated due to chance roll");
        }
    }
    
    private void SetupStatRanges(Item item, LevelConfig config)
    {
        // Настраиваем диапазоны статов для динамической генерации
        item.minDamageRange = new Item.StatRange 
        { 
            minValue = config.minDamageRange.x, 
            maxValue = config.minDamageRange.y, 
            chance = 1.0f 
        };
        
        item.maxDamageRange = new Item.StatRange 
        { 
            minValue = config.maxDamageRange.x, 
            maxValue = config.maxDamageRange.y, 
            chance = 1.0f 
        };
        
        item.strengthRange = new Item.StatRange 
        { 
            minValue = config.strengthRange.x, 
            maxValue = config.strengthRange.y, 
            chance = config.statChance 
        };
        
        item.agilityRange = new Item.StatRange 
        { 
            minValue = config.agilityRange.x, 
            maxValue = config.agilityRange.y, 
            chance = config.statChance 
        };
        
        item.spiritRange = new Item.StatRange 
        { 
            minValue = config.spiritRange.x, 
            maxValue = config.spiritRange.y, 
            chance = config.statChance 
        };
        
        item.constitutionRange = new Item.StatRange 
        { 
            minValue = config.constitutionRange.x, 
            maxValue = config.constitutionRange.y, 
            chance = config.statChance 
        };
        
        item.accuracyRange = new Item.StatRange 
        { 
            minValue = config.accuracyRange.x, 
            maxValue = config.accuracyRange.y, 
            chance = config.statChance 
        };
        
        item.healthRange = new Item.StatRange 
        { 
            minValue = config.healthRange.x, 
            maxValue = config.healthRange.y, 
            chance = config.statChance 
        };
        
        item.manaRange = new Item.StatRange 
        { 
            minValue = config.manaRange.x, 
            maxValue = config.manaRange.y, 
            chance = config.statChance 
        };
        
        item.defenseRange = new Item.StatRange 
        { 
            minValue = config.defenseRange.x, 
            maxValue = config.defenseRange.y, 
            chance = config.statChance 
        };
        
        item.criticalRange = new Item.StatRange 
        { 
            minValue = config.criticalRange.x, 
            maxValue = config.criticalRange.y, 
            chance = config.statChance 
        };
        
        item.movementSpeedRange = new Item.StatRange 
        { 
            minValue = config.movementSpeedRange.x, 
            maxValue = config.movementSpeedRange.y, 
            chance = config.statChance 
        };
        
        item.hpRecoveryRange = new Item.StatRange 
        { 
            minValue = config.hpRecoveryRange.x, 
            maxValue = config.hpRecoveryRange.y, 
            chance = config.statChance 
        };
        
        item.spRecoveryRange = new Item.StatRange 
        { 
            minValue = config.spRecoveryRange.x, 
            maxValue = config.spRecoveryRange.y, 
            chance = config.statChance 
        };
        
        item.dodgeRange = new Item.StatRange 
        { 
            minValue = config.dodgeRange.x, 
            maxValue = config.dodgeRange.y, 
            chance = config.statChance 
        };
    }
    
    private void CopyItemProperties(Item source, Item target)
    {
        // Копируем все свойства базового предмета
        target.itemName = source.itemName;
        // Устанавливаем оригинальное имя - если у источника есть originalName, используем его, иначе текущее itemName
        target.originalName = !string.IsNullOrEmpty(source.originalName) ? source.originalName : source.itemName;
        target.itemType = source.itemType;
        target.equipmentSlot = source.equipmentSlot;
        target.alternativeSlot = source.alternativeSlot;
        target.primaryDisplaySlot = source.primaryDisplaySlot;
        target.maxStack = source.maxStack;
        target.canDrop = source.canDrop;
        target.canSell = source.canSell;
        target.canUse = source.canUse;
        target.canHotbar = source.canHotbar;
        target.isTwoHanded = source.isTwoHanded;
        target.preferRightHand = source.preferRightHand;
        target.rarity = source.rarity;
        target.characterClass = source.characterClass;
        target.skillEffect = source.skillEffect;
        target.castRange = source.castRange;
        target.model1 = source.model1;
        target.boneName = source.boneName;
        target.alternativeBoneName = source.alternativeBoneName;
        target.modelRotation = source.modelRotation;
        target.modelScale = source.modelScale;
        target.icon = source.icon;
        target.price = source.price;
        target.durability = source.durability;
        target.description = source.description;
        
        // Копируем dropModelPrefab для правильного отображения модели
        target.DropModelPrefab = source.DropModelPrefab;
        
        // Сбрасываем все бонусные статы (они будут сгенерированы заново)
        target.minAttackConstantBonus = 0;
        target.maxAttackConstantBonus = 0;
        target.maxHpConstantBonus = 0;
        target.maxSpConstantBonus = 0;
        target.crtConstantBonus = 0;
        target.mspdConstantBonus = 0;
        target.physicalResist = 0;
        target.strengthBonus = 0;
        target.agilityBonus = 0;
        target.spiritBonus = 0;
        target.constitutionBonus = 0;
        target.accuracyBonus = 0;
        target.hpRecoveryBonus = 0;
        target.spRecoveryBonus = 0;
        target.dodgeBonus = 0;
        
        // Копируем настройки динамических статов
        target.useDynamicStats = source.useDynamicStats;
        target.minDamageRange = source.minDamageRange;
        target.maxDamageRange = source.maxDamageRange;
        target.strengthRange = source.strengthRange;
        target.agilityRange = source.agilityRange;
        target.spiritRange = source.spiritRange;
        target.constitutionRange = source.constitutionRange;
        target.accuracyRange = source.accuracyRange;
        target.healthRange = source.healthRange;
        target.manaRange = source.manaRange;
        target.defenseRange = source.defenseRange;
        target.criticalRange = source.criticalRange;
        target.movementSpeedRange = source.movementSpeedRange;
    }
    
    private void GenerateRandomStats(Item item)
    {
        // Используем метод из Item.cs для генерации случайных статов
        if (item.useDynamicStats)
        {
            // Генерируем урон на основе диапазонов
            if (item.minDamageRange.maxValue > 0 && Random.Range(0f, 1f) <= item.minDamageRange.chance)
            {
                item.minAttackConstantBonus = Random.Range(item.minDamageRange.minValue, item.minDamageRange.maxValue + 1);
            }
            
            if (item.maxDamageRange.maxValue > 0 && Random.Range(0f, 1f) <= item.maxDamageRange.chance)
            {
                item.maxAttackConstantBonus = Random.Range(item.maxDamageRange.minValue, item.maxDamageRange.maxValue + 1);
            }
            
            // Генерируем статы на основе диапазонов
            if (item.strengthRange.maxValue > 0 && Random.Range(0f, 1f) <= item.strengthRange.chance)
            {
                item.strengthBonus = Random.Range(item.strengthRange.minValue, item.strengthRange.maxValue + 1);
            }
            
            if (item.agilityRange.maxValue > 0 && Random.Range(0f, 1f) <= item.agilityRange.chance)
            {
                item.agilityBonus = Random.Range(item.agilityRange.minValue, item.agilityRange.maxValue + 1);
            }
            
            if (item.spiritRange.maxValue > 0 && Random.Range(0f, 1f) <= item.spiritRange.chance)
            {
                item.spiritBonus = Random.Range(item.spiritRange.minValue, item.spiritRange.maxValue + 1);
            }
            
            if (item.constitutionRange.maxValue > 0 && Random.Range(0f, 1f) <= item.constitutionRange.chance)
            {
                item.constitutionBonus = Random.Range(item.constitutionRange.minValue, item.constitutionRange.maxValue + 1);
            }
            
            if (item.accuracyRange.maxValue > 0 && Random.Range(0f, 1f) <= item.accuracyRange.chance)
            {
                item.accuracyBonus = Random.Range(item.accuracyRange.minValue, item.accuracyRange.maxValue + 1);
            }
            
            if (item.healthRange.maxValue > 0 && Random.Range(0f, 1f) <= item.healthRange.chance)
            {
                int healthBonus = Random.Range(item.healthRange.minValue, item.healthRange.maxValue + 1);
                item.maxHpConstantBonus = healthBonus; // Прямое значение HP
            }
            
            if (item.manaRange.maxValue > 0 && Random.Range(0f, 1f) <= item.manaRange.chance)
            {
                int manaBonus = Random.Range(item.manaRange.minValue, item.manaRange.maxValue + 1);
                item.maxSpConstantBonus = manaBonus * 5; // 1 очко = 5 MP
            }
            
            if (item.defenseRange.maxValue > 0 && Random.Range(0f, 1f) <= item.defenseRange.chance)
            {
                item.physicalResist = Random.Range(item.defenseRange.minValue, item.defenseRange.maxValue + 1);
            }
            
            if (item.criticalRange.maxValue > 0 && Random.Range(0f, 1f) <= item.criticalRange.chance)
            {
                item.crtConstantBonus = Random.Range(item.criticalRange.minValue, item.criticalRange.maxValue + 1);
            }
            
            if (item.movementSpeedRange.maxValue > 0 && Random.Range(0f, 1f) <= item.movementSpeedRange.chance)
            {
                item.mspdConstantBonus = Random.Range(item.movementSpeedRange.minValue, item.movementSpeedRange.maxValue + 1);
            }
        }
    }
    
    private void UpdateItemNameWithStats(Item item)
    {
        // Используем оригинальное имя как базовое, если оно есть, иначе текущее имя
        string baseName = !string.IsNullOrEmpty(item.originalName) ? item.originalName : item.itemName;
        
        // Подсчитываем количество статов и их общую силу
        int statCount = 0;
        int totalStatPower = 0;
        int maxPossiblePower = 0;
        
        if (item.strengthBonus > 0)
        {
            statCount++;
            totalStatPower += item.strengthBonus;
            maxPossiblePower += item.strengthRange.maxValue;
        }
        if (item.agilityBonus > 0)
        {
            statCount++;
            totalStatPower += item.agilityBonus;
            maxPossiblePower += item.agilityRange.maxValue;
        }
        if (item.spiritBonus > 0)
        {
            statCount++;
            totalStatPower += item.spiritBonus;
            maxPossiblePower += item.spiritRange.maxValue;
        }
        if (item.constitutionBonus > 0)
        {
            statCount++;
            totalStatPower += item.constitutionBonus;
            maxPossiblePower += item.constitutionRange.maxValue;
        }
        if (item.accuracyBonus > 0)
        {
            statCount++;
            totalStatPower += item.accuracyBonus;
            maxPossiblePower += item.accuracyRange.maxValue;
        }
        
        // Если нет статов, возвращаем базовое имя
        if (statCount == 0)
        {
            item.itemName = baseName;
            item.rarity = Rarity.Common;
            return;
        }
        
        // Вычисляем процент от максимально возможной силы
        float powerPercentage = maxPossiblePower > 0 ? (float)totalStatPower / maxPossiblePower : 0f;
        
        // Выбираем префикс на основе количества статов и их силы
        string prefix = GetPrefixByStats(statCount, powerPercentage);
        
        // Определяем редкость на основе силы статов
        item.rarity = GetRarityByPower(powerPercentage);
        
        item.itemName = prefix + " " + baseName;
    }
    
    private string GetPrefixByStats(int statCount, float powerPercentage)
    {
        // Один стат
        if (statCount == 1)
        {
            if (powerPercentage >= 0.9f) return "Mammoth";
            if (powerPercentage >= 0.7f) return "Colossus";
            if (powerPercentage >= 0.5f) return "Giant";
            if (powerPercentage >= 0.3f) return "Strong";
            return "Enhanced";
        }
        
        // Два стата
        if (statCount == 2)
        {
            if (powerPercentage >= 0.9f) return "Sacred Dragon";
            if (powerPercentage >= 0.7f) return "Ancient Titan";
            if (powerPercentage >= 0.5f) return "Divine Beast";
            if (powerPercentage >= 0.3f) return "Mystic Guardian";
            return "Blessed";
        }
        
        // Три или больше статов
        if (statCount >= 3)
        {
            if (powerPercentage >= 0.9f) return "Eternal Phoenix";
            if (powerPercentage >= 0.7f) return "Celestial Serpent";
            if (powerPercentage >= 0.5f) return "Primordial Wolf";
            if (powerPercentage >= 0.3f) return "Arcane Eagle";
            return "Enchanted";
        }
        
        return "Enhanced";
    }
    
    private Rarity GetRarityByPower(float powerPercentage)
    {
        if (powerPercentage >= 0.9f) return Rarity.Legendary;
        if (powerPercentage >= 0.7f) return Rarity.Epic;
        if (powerPercentage >= 0.5f) return Rarity.Rare;
        if (powerPercentage >= 0.3f) return Rarity.Uncommon;
        return Rarity.Common;
    }
    
    private void SaveItemsToResources(List<Item> items)
    {
        #if UNITY_EDITOR
        string fullPath = "Assets/" + outputPath;
        if (!System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.CreateDirectory(fullPath);
        }
        
        foreach (Item item in items)
        {
            string fileName = $"{item.itemName.Replace(" ", "_")}_Lv{item.requiredLevel}.asset";
            string assetPath = fullPath + fileName;
            
            UnityEditor.AssetDatabase.CreateAsset(item, assetPath);
        }
        
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log($"[ItemGenerator] Saved {items.Count} items to {fullPath}");
        #endif
    }
    
    private int GetNextAvailableId()
    {
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogWarning("[ItemGenerator] ItemDatabase not found, using start ID");
            return startId;
        }
        
        Item[] existingItems = database.GetAllItems();
        int maxId = startId - 1;
        
        foreach (Item item in existingItems)
        {
            if (item != null && item.id > maxId)
            {
                maxId = item.id;
            }
        }
        
        return maxId + 1;
    }
    
    private void AddSingleItemToDatabase(Item item)
    {
        #if UNITY_EDITOR
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogError("[ItemGenerator] Cannot add item to database: ItemDatabase not found in Resources!");
            return;
        }
        
        Item[] existingItems = database.GetAllItems();
        List<Item> allItems = new List<Item>(existingItems);
        allItems.Add(item);
        
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
        
        Debug.Log($"[ItemGenerator] Added item to database: {item.itemName} (ID: {item.id})");
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
