using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
    [HideInInspector]
    public string originalName = ""; // Оригинальное название без префиксов
    public int id = -1;
    public Sprite icon;
    [SerializeField] private GameObject dropModelPrefab;
    
    public GameObject DropModelPrefab 
    { 
        get => dropModelPrefab; 
        set => dropModelPrefab = value; 
    }
    public ItemType itemType = ItemType.Consumable;
    public EquipmentSlot equipmentSlot = EquipmentSlot.None;
    public EquipmentSlot alternativeSlot = EquipmentSlot.None;
    public EquipmentSlot primaryDisplaySlot = EquipmentSlot.None;
    [Header("Flags")]
    public int maxStack = 1;
    public bool canDrop = true;
    public bool canSell = true;
    public bool canUse = false;
    public bool canHotbar = false;
    public bool isTwoHanded = false;
    public bool preferRightHand = true; // true = предпочитает правую руку, false = только левую
    
    [Header("Chest Settings")]
    public ChestItemData chestData; // Данные сундука (используется только для itemType.Chest)
    [Header("Character Stat Bonuses")]
    public int strengthBonus;
    public int agilityBonus;
    public int spiritBonus;
    public int constitutionBonus;
    public int accuracyBonus;
    public float attackRangeBonus = 0f; // Бонус к дальности атаки
    
    [Header("Recovery & Special Stats")]
    public int hpRecoveryBonus;
    public int spRecoveryBonus;
    public int dodgeBonus;
    public int damageChanceBonus; // Шанс урона (%)
    
    [Header("Dynamic Stat Ranges")]
    public bool useDynamicStats = false;
    
    [Header("Damage Ranges")]
    public StatRange minDamageRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange maxDamageRange = new StatRange { minValue = 0, maxValue = 0 };
    
    [Header("Stat Ranges")]
    public StatRange strengthRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange agilityRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange spiritRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange constitutionRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange accuracyRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange healthRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange manaRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange defenseRange = new StatRange { minValue = 0, maxValue = 0 }; // УСТАРЕЛО: используйте armorRange и physicalResistRange
    public StatRange armorRange = new StatRange { minValue = 0, maxValue = 0 }; // Плоская броня (вычитается из урона)
    public StatRange physicalResistRange = new StatRange { minValue = 0, maxValue = 0 }; // Процентное сопротивление (0-100%)
    public StatRange criticalRange = new StatRange { minValue = 0, maxValue = 0 };
    public FloatStatRange movementSpeedRange = new FloatStatRange { minValue = 0, maxValue = 0 };
    public StatRange hpRecoveryRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange spRecoveryRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange dodgeRange = new StatRange { minValue = 0, maxValue = 0 };
    public StatRange damageChanceRange = new StatRange { minValue = 0, maxValue = 0 }; // Шанс урона
    
    [System.Serializable]
    public class StatRange
    {
        public int minValue;
        public int maxValue;
        public float chance = 1.0f; // Вероятность появления этого стата
    }
    
    [System.Serializable]
    public class FloatStatRange
    {
        public float minValue;
        public float maxValue;
        public float chance = 1.0f; // Вероятность появления этого стата
    }
    [Header("MMO Properties")]
    public Rarity rarity = Rarity.Common;
    public int requiredLevel = 1;
    public CharacterClass characterClass = CharacterClass.Warrior;
    [Header("Skill Effect (Optional)")]
    public SkillBase skillEffect;
    public float castRange = 5f;
    [Header("Visuals")]
    public string model1;
    public string boneName;
    public string alternativeBoneName;
    public Quaternion modelRotation = Quaternion.identity;
    public Vector3 modelScale = Vector3.one;
    [Header("Additional Item Properties")]
    public string model2;
    public string model3;
    public string model4;
    public string model5;
    public string shipSymbol;
    public int shipSize;
    public int number;
    public string obtain;
    public string prefix;
    public float rate;
    public int setId;
    public int forgingLevel;
    public int stableValue;
    public bool onlyId;
    public bool trade;
    public bool picked;
    public bool discard;
    public bool confirmToDelete;
    public bool stackable;
    public bool isInstantiation;
    public int price;
    public int size;
    public int characterLevel;
    public string characterNick;
    public int characterReputation;
    public bool itemCanEquip = true;
    public string location;
    public string itemSwitchLocation;
    public string itemObtainIntoLocation;
    // Используемые статы
    public int minAttackConstantBonus;
    public int maxAttackConstantBonus;
    public int maxHpConstantBonus;
    public int maxSpConstantBonus;
    public int crtConstantBonus;
    public float mspdConstantBonus;
    public int physicalResist; // УСТАРЕЛО: используйте armorBonus и physicalResistBonus
    public int armorBonus; // Плоская броня (прямое вычитание из урона)
    public int physicalResistBonus; // Процентное сопротивление физическому урону (0-100%)
    
    /// <summary>
    /// Генерирует предмет с случайными статами на основе диапазонов (сохраняет оригинальный ID)
    /// </summary>
    public Item GenerateDynamicItem()
    {
        Debug.Log($"[Item] GenerateDynamicItem called for: {itemName} (ID: {id}, UseDynamicStats: {useDynamicStats})");
        
        if (!useDynamicStats)
        {
            Debug.LogWarning($"[Item] {itemName} does not use dynamic stats - returning original");
            return this;
        }
        
        Debug.Log($"[Item] Creating dynamic copy of SO: {itemName}");
        
        // КРИТИЧЕСКАЯ ЗАЩИТА: Никогда не модифицируем оригинальный SO
        // Создаем копию предмета с сохранением типа (Item, SwordItem, etc.)
        Item generatedItem = ScriptableObject.CreateInstance(this.GetType()) as Item;
        CopyItemProperties(this, generatedItem);
        
        // НИКОГДА НЕ МОДИФИЦИРУЕМ ОРИГИНАЛЬНЫЙ SO!
        
        // Сохраняем оригинальный ID
        generatedItem.id = this.id;
        Debug.Log($"[Item] Preserved original ID: {this.id}");
        
        // Убеждаемся, что originalName установлено правильно
        if (string.IsNullOrEmpty(generatedItem.originalName))
        {
            generatedItem.originalName = this.itemName;
        }
        
        // Генерируем случайные статы
        GenerateRandomStats(generatedItem);
        
        // Обновляем имя с префиксами/суффиксами
        string originalName = generatedItem.itemName;
        UpdateItemNameWithStats(generatedItem);
        Debug.Log($"[Item] Name updated in SO: '{originalName}' -> '{generatedItem.itemName}'");
        
        // НЕ добавляем в базу данных - используем оригинальный ID
        Debug.Log($"[Item] Dynamic item generated successfully: {generatedItem.itemName} (ID: {generatedItem.id}, Rarity: {generatedItem.rarity})");
        
        return generatedItem;
    }
    
    private void CopyItemProperties(Item source, Item target)
    {
        Debug.Log($"[Item] Copying properties from SO: {source.itemName} to new instance");
        
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
        target.requiredLevel = source.requiredLevel;
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
        
        // Копируем dropModelPrefab для правильного отображения модели
        target.DropModelPrefab = source.DropModelPrefab;
        
        // Копируем диапазоны статов (создаем новые экземпляры)
        target.useDynamicStats = source.useDynamicStats;
        target.minDamageRange = new StatRange { minValue = source.minDamageRange.minValue, maxValue = source.minDamageRange.maxValue, chance = source.minDamageRange.chance };
        target.maxDamageRange = new StatRange { minValue = source.maxDamageRange.minValue, maxValue = source.maxDamageRange.maxValue, chance = source.maxDamageRange.chance };
        target.strengthRange = new StatRange { minValue = source.strengthRange.minValue, maxValue = source.strengthRange.maxValue, chance = source.strengthRange.chance };
        target.agilityRange = new StatRange { minValue = source.agilityRange.minValue, maxValue = source.agilityRange.maxValue, chance = source.agilityRange.chance };
        target.spiritRange = new StatRange { minValue = source.spiritRange.minValue, maxValue = source.spiritRange.maxValue, chance = source.spiritRange.chance };
        target.constitutionRange = new StatRange { minValue = source.constitutionRange.minValue, maxValue = source.constitutionRange.maxValue, chance = source.constitutionRange.chance };
        target.accuracyRange = new StatRange { minValue = source.accuracyRange.minValue, maxValue = source.accuracyRange.maxValue, chance = source.accuracyRange.chance };
        target.healthRange = new StatRange { minValue = source.healthRange.minValue, maxValue = source.healthRange.maxValue, chance = source.healthRange.chance };
        target.manaRange = new StatRange { minValue = source.manaRange.minValue, maxValue = source.manaRange.maxValue, chance = source.manaRange.chance };
        target.defenseRange = new StatRange { minValue = source.defenseRange.minValue, maxValue = source.defenseRange.maxValue, chance = source.defenseRange.chance };
        target.armorRange = new StatRange { minValue = source.armorRange.minValue, maxValue = source.armorRange.maxValue, chance = source.armorRange.chance };
        target.physicalResistRange = new StatRange { minValue = source.physicalResistRange.minValue, maxValue = source.physicalResistRange.maxValue, chance = source.physicalResistRange.chance };
        target.criticalRange = new StatRange { minValue = source.criticalRange.minValue, maxValue = source.criticalRange.maxValue, chance = source.criticalRange.chance };
        target.movementSpeedRange = new FloatStatRange { minValue = source.movementSpeedRange.minValue, maxValue = source.movementSpeedRange.maxValue, chance = source.movementSpeedRange.chance };
        target.hpRecoveryRange = new StatRange { minValue = source.hpRecoveryRange.minValue, maxValue = source.hpRecoveryRange.maxValue, chance = source.hpRecoveryRange.chance };
        target.spRecoveryRange = new StatRange { minValue = source.spRecoveryRange.minValue, maxValue = source.spRecoveryRange.maxValue, chance = source.spRecoveryRange.chance };
        target.dodgeRange = new StatRange { minValue = source.dodgeRange.minValue, maxValue = source.dodgeRange.maxValue, chance = source.dodgeRange.chance };
        target.damageChanceRange = new StatRange { minValue = source.damageChanceRange.minValue, maxValue = source.damageChanceRange.maxValue, chance = source.damageChanceRange.chance };
        
        // Сбрасываем бонусные статы (они будут сгенерированы)
        target.strengthBonus = 0;
        target.agilityBonus = 0;
        target.spiritBonus = 0;
        target.constitutionBonus = 0;
        target.accuracyBonus = 0;
        target.hpRecoveryBonus = 0;
        target.spRecoveryBonus = 0;
        target.dodgeBonus = 0;
        target.damageChanceBonus = 0;
        
        // Сбрасываем бонусы урона и других статов (они будут сгенерированы)
        target.minAttackConstantBonus = 0;
        target.maxAttackConstantBonus = 0;
        target.maxHpConstantBonus = 0;
        target.maxSpConstantBonus = 0;
        target.crtConstantBonus = 0;
        target.mspdConstantBonus = 0.0f;
        target.physicalResist = 0;
        target.armorBonus = 0;
        target.physicalResistBonus = 0;
    }
    
    private void GenerateRandomStats(Item item)
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
        
        // УСТАРЕЛО: defenseRange - используйте armorRange и physicalResistRange
        if (item.defenseRange.maxValue > 0 && Random.Range(0f, 1f) <= item.defenseRange.chance)
        {
            item.physicalResist = Random.Range(item.defenseRange.minValue, item.defenseRange.maxValue + 1);
        }
        
        // НОВЫЕ: отдельные диапазоны для брони и сопротивления
        if (item.armorRange.maxValue > 0 && Random.Range(0f, 1f) <= item.armorRange.chance)
        {
            item.armorBonus = Random.Range(item.armorRange.minValue, item.armorRange.maxValue + 1);
        }
        
        if (item.physicalResistRange.maxValue > 0 && Random.Range(0f, 1f) <= item.physicalResistRange.chance)
        {
            item.physicalResistBonus = Random.Range(item.physicalResistRange.minValue, item.physicalResistRange.maxValue + 1);
        }
        
        if (item.criticalRange.maxValue > 0 && Random.Range(0f, 1f) <= item.criticalRange.chance)
        {
            item.crtConstantBonus = Random.Range(item.criticalRange.minValue, item.criticalRange.maxValue + 1);
        }
        
        if (item.movementSpeedRange.maxValue > 0 && Random.Range(0f, 1f) <= item.movementSpeedRange.chance)
        {
            item.mspdConstantBonus = Random.Range(item.movementSpeedRange.minValue, item.movementSpeedRange.maxValue);
        }
        
        if (item.damageChanceRange.maxValue > 0 && Random.Range(0f, 1f) <= item.damageChanceRange.chance)
        {
            item.damageChanceBonus = Random.Range(item.damageChanceRange.minValue, item.damageChanceRange.maxValue + 1);
        }
    }
    
    /// <summary>
    /// Получает оригинальное имя предмета, извлекая его из возможно поврежденного имени
    /// </summary>
    private string GetOriginalItemName(Item item)
    {
        // Если originalName уже установлено и не пусто, используем его
        if (!string.IsNullOrEmpty(item.originalName))
        {
            return item.originalName;
        }
        
        // Если originalName пусто, пытаемся извлечь оригинальное имя из текущего
        string currentName = item.itemName;
        
        // Список всех возможных префиксов, которые мы добавляем
        string[] prefixes = {
            "Eternal Phoenix", "Celestial Serpent", "Primordial Wolf", "Arcane Eagle",
            "Sacred Dragon", "Ancient Titan", "Divine Beast", "Mystic Guardian",
            "Mammoth", "Colossus", "Giant", "Strong", "Enhanced", "Blessed", "Enchanted"
        };
        
        // Удаляем все префиксы из начала имени
        string extractedName = currentName;
        bool foundPrefix = true;
        
        while (foundPrefix)
        {
            foundPrefix = false;
            foreach (string prefix in prefixes)
            {
                if (extractedName.StartsWith(prefix + " "))
                {
                    extractedName = extractedName.Substring(prefix.Length + 1).Trim();
                    foundPrefix = true;
                    break;
                }
            }
        }
        
        // БЕЗОПАСНО устанавливаем originalName только для сгенерированных копий
        // Проверяем, является ли это сгенерированной копией (не оригинальным SO из Resources)
        bool isGeneratedCopy = item.name == "Item" || item.name.Contains("(Clone)");
        if (isGeneratedCopy)
        {
            item.originalName = extractedName;
            Debug.Log($"[Item] Set originalName for generated copy: '{extractedName}'");
        }
        
        Debug.Log($"[Item] Extracted original name: '{currentName}' -> '{extractedName}' (isGeneratedCopy: {isGeneratedCopy})");
        return extractedName;
    }
    
    private void UpdateItemNameWithStats(Item item)
    {
        // Используем оригинальное имя как базовое, если оно есть, иначе извлекаем из текущего имени
        string baseName = GetOriginalItemName(item);
        
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
    
    private int GetNextAvailableId()
    {
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogWarning("[Item] ItemDatabase not found, using ID 1000");
            return 1000;
        }
        
        Item[] existingItems = database.GetAllItems();
        int maxId = 999;
        
        foreach (Item item in existingItems)
        {
            if (item != null && item.id > maxId)
            {
                maxId = item.id;
            }
        }
        
        return maxId + 1;
    }
    
    private void AddToDatabase(Item item)
    {
        #if UNITY_EDITOR
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogError("[Item] Cannot add item to database: ItemDatabase not found in Resources!");
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
        
        Debug.Log($"[Item] Added item to database: {item.itemName} (ID: {item.id})");
        #endif
    }
    
    public string itemLeftHandExertIdentifier;
    public int itemEnergy;
    public int durability;
    public int maxInstantiation;
    public int holeValue;
    public int shipDurabilityRecovered;
    public int canContainCannonQuantity;
    public int shipMemberCount;
    public string memberLabel;
    public int cargoCapacity;
    public int fuelConsumption;
    public int cannonballPathOfFlightSpeed;
    public int shipMovementSpeed;
    public string usageEffect;
    public string displayEffect;
    public string itemBindEffect;
    public string itemBindEffectDummy;
    public string displayItemEffect;
    public string itemDropModelEffect;
    public string itemUsageEffect;
    public string description;
    public int itemLevel;
    public string remark;
    public enum WeaponType { None, OneHandedSword, TwoHandedSword, Bow, Staff, Dagger, Axe, DualWeapons }
    public WeaponType weaponType = WeaponType.None;
    public void OnEnable()
    {
        if (id < 0)
        {
            Debug.LogWarning($"[Item] ID not set for {itemName}, defaulting to -1");
        }
        // ����� �����, ���� ������� �� �����������
        if (equipmentSlot == EquipmentSlot.None && alternativeSlot == EquipmentSlot.None)
        {
            boneName = string.Empty;
            alternativeBoneName = string.Empty;
            primaryDisplaySlot = EquipmentSlot.None;
            isTwoHanded = false;
            // Reset equipment fields
        }
        // �������� ���������� ������
        if (isTwoHanded && primaryDisplaySlot == EquipmentSlot.None)
        {
            // Двуручное оружие всегда отображается на левой руке
            primaryDisplaySlot = EquipmentSlot.LeftHand;
            Debug.Log($"[Item] Set primaryDisplaySlot to LeftHand for two-handed item {itemName}");
        }
    }
    public virtual void Use(PlayerCore player)
    {
        if (canUse)
        {
            Debug.Log($"Used {itemName}");
            
            // Обработка сундуков
            if (itemType == ItemType.Chest && chestData != null)
            {
                OpenChest(player);
                return;
            }
            
            if (skillEffect == null)
            {
                Debug.Log($"[Item] No skill effect for {itemName}, no default action");
            }
            else
            {
                skillEffect.Init(player);
                PlayerSkills skills = player.GetComponent<PlayerSkills>();
                if (skills != null)
                {
                    if (skillEffect.CastTime > 0)
                    {
                        skills.SelectSkill(skillEffect);
                        Debug.Log($"[Item] Selected skill {skillEffect.SkillName} for casting from item {itemName}");
                    }
                    else
                    {
                        Ray ray = player.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
                        Vector3? targetPos = null;
                        if (Physics.Raycast(ray, out RaycastHit hit, castRange, LayerMask.GetMask("Ground")))
                        {
                            targetPos = hit.point;
                        }
                        else
                        {
                            targetPos = player.transform.position + player.transform.forward * castRange;
                        }
                        skills.CmdExecuteSkill(player, targetPos, 0, skillEffect.SkillName, 0);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Открывает сундук и выдает награды игроку
    /// </summary>
    private void OpenChest(PlayerCore player)
    {
        if (chestData == null)
        {
            Debug.LogError($"[Item] ChestData is null for chest item {itemName}!");
            return;
        }
        
        // Генерируем награды
        List<ItemInfo> rewards = chestData.GenerateRewards();
        int goldReward = chestData.GetGoldReward();
        
        // Выдаем предметы игроку
        bool allItemsAdded = true;
        foreach (var reward in rewards)
        {
            if (!player.Inventory.AddItemInfo(reward))
            {
                allItemsAdded = false;
                Debug.LogWarning($"[Item] Failed to add item {reward.id} to player {player.playerName}'s inventory");
            }
        }
        
        // Выдаем золото
        if (goldReward > 0)
        {
            player.Inventory.AddGold(goldReward);
        }
        
        // Показываем уведомление игроку
        ShowChestRewardNotification(player, rewards, goldReward, allItemsAdded);
        
        Debug.Log($"[Item] Player {player.playerName} opened chest {itemName}, received {rewards.Count} items and {goldReward} gold");
    }
    
    /// <summary>
    /// Показывает уведомление о наградах из сундука
    /// </summary>
    private void ShowChestRewardNotification(PlayerCore player, List<ItemInfo> rewards, int goldReward, bool allItemsAdded)
    {
        string message = $"Получены награды из {itemName}:\n";
        
        foreach (var reward in rewards)
        {
            Item item = reward.GetItem();
            if (item != null)
            {
                string itemName = reward.hasDynamicStats ? reward.dynamicItemName : item.itemName;
                message += $"• {itemName} x{reward.quantity}\n";
            }
        }
        
        if (goldReward > 0)
        {
            message += $"• Золото: {goldReward}\n";
        }
        
        if (!allItemsAdded)
        {
            message += "\n⚠️ Инвентарь полон! Некоторые предметы не были получены.";
        }
        
        // Показываем уведомление (можно интегрировать с системой уведомлений)
        Debug.Log($"[Item] {message}");
        
        // Здесь можно добавить UI уведомление
        // NotificationSystem.ShowNotification(message);
    }
    public bool IsEquipable(int playerLevel, CharacterClass playerClass)
    {
        bool classMatch = characterClass == playerClass;
        return (equipmentSlot != EquipmentSlot.None || alternativeSlot != EquipmentSlot.None) && playerLevel >= requiredLevel && itemCanEquip && classMatch;
    }
    public bool CanEquipToSlot(EquipmentSlot slot)
    {
        if (isTwoHanded)
        {
            // Двуручное оружие можно экипировать только в левую руку
            bool twoHandedCanEquip = slot == EquipmentSlot.LeftHand;
            Debug.Log($"[Item] CanEquipToSlot: {itemName} (isTwoHanded={isTwoHanded}) to {slot}: {twoHandedCanEquip}");
            return twoHandedCanEquip;
        }
        bool oneHandedCanEquip = slot == equipmentSlot || slot == alternativeSlot;
        Debug.Log($"[Item] CanEquipToSlot: {itemName} (equipmentSlot={equipmentSlot}, alternativeSlot={alternativeSlot}) to {slot}: {oneHandedCanEquip}");
        return oneHandedCanEquip;
    }
    public string GetBoneNameForSlot(EquipmentSlot slot)
    {
        if (isTwoHanded && primaryDisplaySlot != EquipmentSlot.None)
        {
            // ��� ���������� ������ ���������� boneName, ���� primaryDisplaySlot �����
            return boneName;
        }
        return slot == alternativeSlot && !string.IsNullOrEmpty(alternativeBoneName) ? alternativeBoneName : boneName;
    }
    public GameObject GetDropModelPrefab()
    {
        Debug.Log($"[Item] GetDropModelPrefab called for: {itemName} (ID: {id}), Prefab: {(dropModelPrefab != null ? dropModelPrefab.name : "NULL")}");
        return dropModelPrefab;
    }
    public GameObject GetEquipModelPrefab()
    {
        if (!string.IsNullOrEmpty(model1))
        {
            GameObject prefab = Resources.Load<GameObject>(model1);
            if (prefab == null)
            {
                Debug.LogWarning($"[Item] Equip model prefab not found at path: {model1} for {itemName}");
            }
            return prefab;
        }
        return null;
    }
}
public enum ItemType { Normal, Consumable, Weapon, Armor, Accessory, QuestItem, Material, Chest }
public enum EquipmentSlot { None, Head, Body, Legs, RightHand, LeftHand, Ring, Necklace, Boots, Gloves, Weapon, OffHand }
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }