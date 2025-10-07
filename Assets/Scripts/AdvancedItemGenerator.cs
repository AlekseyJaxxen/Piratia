using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AdvancedItemGenerator", menuName = "Item Generator/Smart Generator")]
public class AdvancedItemGenerator : ScriptableObject
{
    [Header("Generation Settings")]
    [Tooltip("Базовая область предмета (Item Obtain Into Location)")]
    public Vector2 itemObtainIntoLocation = new Vector2(0, 0);
    [Tooltip("Генерировать предметы в Resources папку")]
    public bool generateToResources = true;
    [Tooltip("Добавлять сгенерированные предметы в ItemDatabase")]
    public bool addToItemDatabase = true;
    [Tooltip("Начальный ID для сгенерированных предметов")]
    public int startId = 1000;
    [Tooltip("Путь для сохранения сгенерированных предметов")]
    public string outputPath = "Resources/Items/Generated/";
    
    [Header("Template System")]
    [Tooltip("Доступные шаблоны предметов")]
    public List<ItemTemplate> availableTemplates = new List<ItemTemplate>();
    
    [Header("Level-Based Generation")]
    [Tooltip("Список уровней для генерации предметов")]
    public List<LevelConfig> levelConfigs = new List<LevelConfig>();
    
    [System.Serializable]
    public class StatTemplate
    {
        [Tooltip("Название характеристики")]
        public string displayName;
        [Tooltip("Диапазон значений (мин-макс)")]
        public Vector2Int range = new Vector2Int(0, 0);
        [Tooltip("Множитель роста максимального значения с уровнем")]
        public float levelMultiplier = 1.0f;
        [Tooltip("Включена ли эта характеристика")]
        public bool enabled = true;
        [Tooltip("Имя поля в Item классе")]
        public string itemPropertyName;
        [Tooltip("Чанс получения этой характеристики")]
        [Range(0f, 1f)] public float chance = 1.0f;
        
        public StatTemplate(string name, string propertyName, Vector2Int initialRange, float multiplier = 1.0f)
        {
            displayName = name;
            itemPropertyName = propertyName;
            range = initialRange;
            levelMultiplier = multiplier;
        }
        
        public StatTemplate(string name, string propertyName) : this(name, propertyName, new Vector2Int(0, 0), 1.0f) {}
        
        public StatTemplate(string name) : this(name, "", new Vector2Int(0, 0), 1.0f) {}
        
        /// <summary>
        /// Получает масштабированный диапазон для заданного уровня
        /// </summary>
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
        [Tooltip("Название шаблона")]
        public string templateName;
        [Tooltip("Базовый предмет для шаблона")]
        public Item baseItemTemplate;
        [Tooltip("Список характеристик для этого типа предмета")]
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
        [Tooltip("Выбранный шаблон предмета")]
        public ItemTemplate selectedTemplate;
        [Tooltip("Дополнительные пользовательские характеристики")]
        public List<StatTemplate> customStats = new List<StatTemplate>();
        [Tooltip("Базовое имя предмета")]
        public string baseItemName = "Generated Item";
        [Tooltip("Количество предметов для генерации")]
        public int itemWeight = 1;
    }
    
    #region Generation Methods
    
    [ContextMenu("Generate All Items")]
    public void GenerateItems()
    {
        List<Item> generatedItems = new List<Item>();
        
        foreach (var config in levelConfigs)
        {
            for (int i = 0; i < config.itemWeight; i++)
            {
                Item item = GenerateItemForLevel(config.level);
                if (item != null)
                {
                    generatedItems.Add(item);
                }
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
        
        Debug.Log($"[AdvancedItemGenerator] Generated {generatedItems.Count} items from {levelConfigs.Count} level configs");
    }
    
    [ContextMenu("Generate Single Item")]
    public void GenerateSingleItem()
    {
        if (levelConfigs.Count == 0)
        {
            Debug.LogError("[AdvancedItemGenerator] No level configs!");
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
            
            Debug.Log($"[AdvancedItemGenerator] Generated single item: {item.itemName} (Level {item.requiredLevel}, ID: {item.id})");
        }
    }
    
    private Item GenerateItemForLevel(int level)
    {
        var config = GetConfigForLevel(level);
        if (config?.selectedTemplate?.baseItemTemplate == null)
        {
            Debug.LogError($"[AdvancedItemGenerator] No valid config/template for level {level}");
            return null;
        }
        
        // Создаем предмет того же типа что и шаблон
        Item baseItem = ScriptableObject.CreateInstance(config.selectedTemplate.baseItemTemplate.GetType()) as Item;
        
        // Копируем свойства базового предмета
        CopyItemProperties(config.selectedTemplate.baseItemTemplate, baseItem);
        
        baseItem.id = GetNextAvailableId();
        baseItem.requiredLevel = level;
        baseItem.itemName = config.baseItemName;
        
        GenerateStatsFromTemplate(baseItem, config);
        
        return baseItem;
    }
    
    private void GenerateStatsFromTemplate(Item item, LevelConfig config)
    {
        ItemTemplate template = config.selectedTemplate;
        List<StatTemplate> allStats = new List<StatTemplate>();
        
        // Добавляем характеристики из шаблона
        foreach (var stat in template.defaultStats)
        {
            if (stat.enabled && Random.Range(0f, 1f) <= stat.chance)
            {
                GenerateSingleStatFromTemplate(item, stat, config.level);
            }
        }
        
        // Добавляем пользовательские характеристики
        foreach (var stat in config.customStats)
        {
            if (stat.enabled && Random.Range(0f, 1f) <= stat.chance)
            {
                GenerateSingleStatFromTemplate(item, stat, config.level);
            }
        }
    }
    
    private void GenerateSingleStatFromTemplate(Item item, StatTemplate statTemplate, int level)
    {
        Vector2Int scaledRange = statTemplate.GetScaledRange(level);
        
        if (scaledRange.y > scaledRange.x)
        {
            int randomValue = Random.Range(scaledRange.x, scaledRange.y + 1);
            SetItemProperty(item, statTemplate.itemPropertyName, randomValue);
            
            Debug.Log($"[AdvancedItemGenerator] Generated {statTemplate.displayName}: {randomValue} (range {scaledRange}, level {level})");
        }
    }
    
    private void SetItemProperty(Item item, string propertyName, int value)
    {
        try
        {
            var field = typeof(Item).GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(int))
            {
                field.SetValue(item, value);
            }
            else
            {
                Debug.LogWarning($"[AdvancedItemGenerator] Field '{propertyName}' not found or not int type");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AdvancedItemGenerator] Error setting property '{propertyName}': {e.Message}");
        }
    }
    
    #endregion
    
    #region Template Creation
    
    [ContextMenu("Create Sword Template")]
    public void CreateSwordTemplate()
    {
        // Найдем или создадим SwordItem как базовый шаблон
        Item swordBase = null;
        
        if (availableTemplates.Count > 0 && availableTemplates[0].baseItemTemplate != null)
        {
            swordBase = availableTemplates[0].baseItemTemplate;
        }
        
        if (swordBase == null)
        {
            Debug.LogError("[AdvancedItemGenerator] No base item template set! Create SwordItem first.");
            return;
        }
        
        ItemTemplate swordTemplate = new ItemTemplate("Sword", swordBase);
        
        // Основные характеристики меча
        swordTemplate.defaultStats.Add(new StatTemplate("Min Attack", "minAttackConstantBonus", new Vector2Int(0, 15), 3.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Max Attack", "maxAttackConstantBonus", new Vector2Int(0, 25), 5.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Damage Chance %", "damageChanceBonus", new Vector2Int(0, 8), 1.5f));
        
        // Базовые характеристики игрока
        swordTemplate.defaultStats.Add(new StatTemplate("Strength", "strengthBonus", new Vector2Int(0, 3), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("Agility", "agilityBonus", new Vector2Int(0, 3), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("Spirit", "spiritBonus", new Vector2Int(0, 2), 1.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Constitution", "constitutionBonus", new Vector2Int(0, 3), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("Accuracy", "accuracyBonus", new Vector2Int(0, 2), 1.0f));
        
        // Боевые характеристики
        swordTemplate.defaultStats.Add(new StatTemplate("Critical Damage", "criticalBonus", new Vector2Int(0, 8), 2.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("Dodge", "dodgeBonus", new Vector2Int(0, 5), 1.5f));
        swordTemplate.defaultStats.Add(new StatTemplate("HP Recovery", "hpRecoveryBonus", new Vector2Int(0, 3), 1.0f));
        swordTemplate.defaultStats.Add(new StatTemplate("SP Recovery", "spRecoveryBonus", new Vector2Int(0, 2), 0.8f));
        
        // Сохраняем шаблон
        availableTemplates.Add(swordTemplate);
        Debug.Log($"[AdvancedItemGenerator] Created Sword Template with {swordTemplate.defaultStats.Count} stats");
        
        // Автоматически создаем конфигурацию уровня
        CreateLevelConfigWithTemplate(swordTemplate, 5);
        CreateLevelConfigWithTemplate(swordTemplate, 10);
        CreateLevelConfigWithTemplate(swordTemplate, 15);
    }
    
    [ContextMenu("Create Armor Template")]
    public void CreateArmorTemplate()
    {
        // Найдем ArmorItem как базовый шаблон
        Item armorBase = null;
        
        if (availableTemplates.Count > 0 && availableTemplates[0].baseItemTemplate != null)
        {
            armorBase = availableTemplates[0].baseItemTemplate;
        }
        
        if (armorBase == null)
        {
            Debug.LogError("[AdvancedItemGenerator] No base item template set! Create ArmorItem first.");
            return;
        }
        
        ItemTemplate armorTemplate = new ItemTemplate("Armor", armorBase);
        
        // Основные характеристики брони (на основе "Darkness's Shadow" Lv 75)
        // Адаптируем значения под наш уровень 15 (75/5 = 15)
        
        // Фиксированная защита и статы (основные характеристики)
        armorTemplate.defaultStats.Add(new StatTemplate("Armor Defense", "armorBonus", new Vector2Int(0, 20), 2.5f));
        armorTemplate.defaultStats.Add(new StatTemplate("Physical Resist %", "physicalResistBonus", new Vector2Int(0, 6), 0.8f));
        armorTemplate.defaultStats.Add(new StatTemplate("Maximum HP", "maxHpConstantBonus", new Vector2Int(0, 60), 6.0f));
        
        // Базовые характеристики игрока
        armorTemplate.defaultStats.Add(new StatTemplate("Strength", "strengthBonus", new Vector2Int(0, 2), 0.2f));
        armorTemplate.defaultStats.Add(new StatTemplate("Agility", "agilityBonus", new Vector2Int(0, 2), 0.2f));
        armorTemplate.defaultStats.Add(new StatTemplate("Constitution", "constitutionBonus", new Vector2Int(0, 2), 0.2f));
        
        // Дополнительные боевые характеристики
        armorTemplate.defaultStats.Add(new StatTemplate("Dodge", "dodgeBonus", new Vector2Int(0, 2), 0.3f));
        armorTemplate.defaultStats.Add(new StatTemplate("HP Recovery", "hpRecoveryBonus", new Vector2Int(0, 1), 0.1f));
        armorTemplate.defaultStats.Add(new StatTemplate("SP Recovery", "spRecoveryBonus", new Vector2Int(0, 1), 0.05f));
        
        // Сохраняем шаблон
        availableTemplates.Add(armorTemplate);
        Debug.Log($"[AdvancedItemGenerator] Created Armor Template with {armorTemplate.defaultStats.Count} stats");
        
        // Автоматически создаем конфигурации уровней
        CreateLevelConfigWithTemplate(armorTemplate, 15);
        CreateLevelConfigWithTemplate(armorTemplate, 30);
        CreateLevelConfigWithTemplate(armorTemplate, 50);
        CreateLevelConfigWithTemplate(armorTemplate, 75);
    }
    
    [ContextMenu("Create All Templates")]
    public void CreateAllTemplates()
    {
        CreateSwordTemplate();
        CreateArmorTemplate();
        CreateGlovesTemplate();
        CreateBootsTemplate();
        
        Debug.Log("[AdvancedItemGenerator] Created all available templates (Sword, Armor, Gloves, Boots)!");
    }
    
    [ContextMenu("Create Gloves Template")]
    public void CreateGlovesTemplate()
    {
        // Найдем GlovesItem как базовый шаблон
        Item glovesBase = null;
        
        if (availableTemplates.Count > 0 && availableTemplates[0].baseItemTemplate != null)
        {
            glovesBase = availableTemplates[0].baseItemTemplate;
        }
        
        if (glovesBase == null)
        {
            Debug.LogError("[AdvancedItemGenerator] No base item template set! Create GlovesItem first.");
            return;
        }
        
        ItemTemplate glovesTemplate = new ItemTemplate("Gloves", glovesBase);
        
        // Основные характеристики перчаток (на основе "Darkness's Touch" Lv 75)
        // Адаптируем значения под наш уровень 15 (75/5 = 15)
        // Base stats: STR/AGI/ACC/CON/SPR могут быть у любых предметов
        
        // Защитные характеристики
        glovesTemplate.defaultStats.Add(new StatTemplate("Armor Defense", "armorBonus", new Vector2Int(0, 13), 1.8f));
        glovesTemplate.defaultStats.Add(new StatTemplate("Maximum HP", "maxHpConstantBonus", new Vector2Int(0, 18), 1.8f));
        glovesTemplate.defaultStats.Add(new StatTemplate("Dodge", "dodgeBonus", new Vector2Int(0, 1), 0.15f));
        
        // Базовые характеристики (все могут быть у любых предметов)
        glovesTemplate.defaultStats.Add(new StatTemplate("Strength", "strengthBonus", new Vector2Int(0, 1), 0.1f));
        glovesTemplate.defaultStats.Add(new StatTemplate("Agility", "agilityBonus", new Vector2Int(0, 1), 0.1f));
        glovesTemplate.defaultStats.Add(new StatTemplate("Constitution", "constitutionBonus", new Vector2Int(0, 1), 0.1f));
        glovesTemplate.defaultStats.Add(new StatTemplate("Accuracy", "accuracyBonus", new Vector2Int(0, 1), 0.1f));
        glovesTemplate.defaultStats.Add(new StatTemplate("Spirit", "spiritBonus", new Vector2Int(0, 1), 0.1f));
        
        // Боевые характеристики (специфично для перчаток)
        glovesTemplate.defaultStats.Add(new StatTemplate("Hit Rate", "accuracyBonus", new Vector2Int(0, 15), 2.0f));
        
        // Вспомогательные характеристики
        glovesTemplate.defaultStats.Add(new StatTemplate("HP Recovery", "hpRecoveryBonus", new Vector2Int(0, 1), 0.1f));
        glovesTemplate.defaultStats.Add(new StatTemplate("SP Recovery", "spRecoveryBonus", new Vector2Int(0, 1), 0.05f));
        
        // Сохраняем шаблон
        availableTemplates.Add(glovesTemplate);
        Debug.Log($"[AdvancedItemGenerator] Created Gloves Template with {glovesTemplate.defaultStats.Count} stats");
        
        // Автоматически создаем конфигурации уровней
        CreateLevelConfigWithTemplate(glovesTemplate, 15);
        CreateLevelConfigWithTemplate(glovesTemplate, 30);
        CreateLevelConfigWithTemplate(glovesTemplate, 50);
        CreateLevelConfigWithTemplate(glovesTemplate, 75);
    }
    
    [ContextMenu("Create Boots Template")]
    public void CreateBootsTemplate()
    {
        // Найдем BootsItem как базовый шаблон
        Item bootsBase = null;
        
        if (availableTemplates.Count > 0 && availableTemplates[0].baseItemTemplate != null)
        {
            bootsBase = availableTemplates[0].baseItemTemplate;
        }
        
        if (bootsBase == null)
        {
            Debug.LogError("[AdvancedItemGenerator] No base item template set! Create BootsItem first.");
            return;
        }
        
        ItemTemplate bootsTemplate = new ItemTemplate("Boots", bootsBase);
        
        // Основные характеристики ботинок (на основе "Darkness's Trace" Lv 75)
        // Адаптируем значения под наш уровень 15 (75/5 = 15)
        // Base stats: STR/AGI/ACC/CON/SPR могут быть у любых предметов
        
        // Защитные характеристики (раздельно)
        bootsTemplate.defaultStats.Add(new StatTemplate("Armor Defense", "armorBonus", new Vector2Int(0, 6), 1.0f));
        bootsTemplate.defaultStats.Add(new StatTemplate("Physical Resist %", "physicalResistBonus", new Vector2Int(0, 3), 0.6f));
        bootsTemplate.defaultStats.Add(new StatTemplate("Maximum HP", "maxHpConstantBonus", new Vector2Int(0, 18), 1.8f));
        
        // Уворот (важная характеристика ботинок)
        bootsTemplate.defaultStats.Add(new StatTemplate("Dodge", "dodgeBonus", new Vector2Int(0, 17), 1.8f));
        
        // Базовые характеристики (все могут быть у любых предметов)
        bootsTemplate.defaultStats.Add(new StatTemplate("Strength", "strengthBonus", new Vector2Int(0, 1), 0.1f));
        bootsTemplate.defaultStats.Add(new StatTemplate("Agility", "agilityBonus", new Vector2Int(0, 1), 0.1f));
        bootsTemplate.defaultStats.Add(new StatTemplate("Constitution", "constitutionBonus", new Vector2Int(0, 1), 0.1f));
        bootsTemplate.defaultStats.Add(new StatTemplate("Accuracy", "accuracyBonus", new Vector2Int(0, 1), 0.1f));
        bootsTemplate.defaultStats.Add(new StatTemplate("Spirit", "spiritBonus", new Vector2Int(0, 1), 0.1f));
        
        // Скорость движения (специфично для ботинок) - хранится 0.8 максимум на 75 уровне, отображается +80
        // Формула: база + (range.max * levelMultiplier * уровень) = 0 + (2.67 * 0.004f * 75) = 0.8
        bootsTemplate.defaultStats.Add(new StatTemplate("Movement Speed", "mspdConstantBonus", new Vector2Int(0, 2), 0.00533f));
        
        // Вспомогательные характеристики
        bootsTemplate.defaultStats.Add(new StatTemplate("HP Recovery", "hpRecoveryBonus", new Vector2Int(0, 1), 0.1f));
        bootsTemplate.defaultStats.Add(new StatTemplate("SP Recovery", "spRecoveryBonus", new Vector2Int(0, 1), 0.05f));
        
        // Сохраняем шаблон
        availableTemplates.Add(bootsTemplate);
        Debug.Log($"[AdvancedItemGenerator] Created Boots Template with {bootsTemplate.defaultStats.Count} stats");
        
        // Автоматически создаем конфигурации уровней
        CreateLevelConfigWithTemplate(bootsTemplate, 15);
        CreateLevelConfigWithTemplate(bootsTemplate, 30);
        CreateLevelConfigWithTemplate(bootsTemplate, 50);
        CreateLevelConfigWithTemplate(bootsTemplate, 75);
    }
    
    private void CreateLevelConfigWithTemplate(ItemTemplate template, int level)
    {
        LevelConfig config = new LevelConfig();
        config.level = level;
        config.selectedTemplate = template;
        config.baseItemName = $"{template.templateName} Lv.{level}";
        config.customStats = new List<StatTemplate>(); // Можно добавлять дополнительные статы
        config.itemWeight = 1;
        
        levelConfigs.Add(config);
        Debug.Log($"[AdvancedItemGenerator] Created Level Config for {template.templateName} at level {level}");
    }
    
    #endregion
    
    #region Utility Methods
    
    private LevelConfig GetConfigForLevel(int level)
    {
        LevelConfig exactMatch = levelConfigs.FirstOrDefault(config => config.level == level);
        if (exactMatch != null) return exactMatch;
        
        return levelConfigs.OrderBy(config => Mathf.Abs(config.level - level)).FirstOrDefault();
    }
    
    private void CopyItemProperties(Item source, Item destination)
    {
        if (source == null || destination == null) return;
        
        // Простое копирование основных свойств
        destination.itemName = source.itemName;
        destination.itemType = source.itemType;
        destination.equipmentSlot = source.equipmentSlot;
        destination.stackable = source.stackable;
        destination.maxStack = source.maxStack;
        destination.icon = source.icon;
        destination.description = source.description;
        destination.price = source.price;
        destination.canSell = source.canSell;
        destination.canDrop = source.canDrop;
    }
    
    private int GetNextAvailableId()
    {
        return startId + Random.Range(1, 1000); // Простая генерация ID
    }
    
    #endregion
    
    #region File Operations
    
    private void SaveItemsToResources(List<Item> items)
    {
        #if UNITY_EDITOR
        string fullPath = "Assets/" + outputPath;
        if (!System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.CreateDirectory(fullPath);
            Debug.Log($"[AdvancedItemGenerator] Created directory: {fullPath}");
        }
        
        foreach (Item item in items)
        {
            string fileName = $"{item.itemName.Replace(" ", "_")}_ID{item.id}.asset";
            string assetPath = fullPath + fileName;
            
            AssetDatabase.CreateAsset(item, assetPath);
            Debug.Log($"[AdvancedItemGenerator] Saved item: {assetPath}");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[AdvancedItemGenerator] Saved {items.Count} items to {fullPath}");
        #endif
    }
    
    private void AddItemsToDatabase(List<Item> items)
    {
        #if UNITY_EDITOR
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogError("[AdvancedItemGenerator] Cannot add items to database: ItemDatabase not found in Resources!");
            return;
        }
        
        Item[] existingItems = database.GetAllItems();
        List<Item> allItems = new List<Item>(existingItems);
        allItems.AddRange(items);
        
        var serializedObject = new SerializedObject(database);
        var itemsProperty = serializedObject.FindProperty("items");
        itemsProperty.arraySize = allItems.Count;
        
        for (int i = 0; i < allItems.Count; i++)
        {
            itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = allItems[i];
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AdvancedItemGenerator] Added {items.Count} items to database");
        #endif
    }
    
    #endregion
}
