# 🛠️ Доработка ItemGenerator - Генерация по типу слота

## 🎯 **Что сделано:**

Доработал ItemGenerator для **умной генерации предметов** в зависимости от типа слота и ItemType. Теперь генератор показывает только релевантные статы и создает правильные конфигурации.

## 🔧 **Основные изменения:**

### **1️⃣ Новая структура LevelConfig:**

```csharp
[System.Serializable]
public class LevelConfig
{
    [Header("Weapon Stats (Weapon/OffHand only)")]
    public Vector2Int minDamageRange;
    public Vector2Int maxDamageRange;
    public Vector2Int attackSpeedRange; // ✅ НОВОЕ: может быть отрицательным

    [Header("Armor Stats (All armor pieces)")]
    public Vector2Int armorRange;           // ✅ НОВОЕ: плоская броня
    public Vector2Int physicalResistRange; // ✅ НОВОЕ: процентное сопротивление

    [Header("Slot-Specific Stats")]
    public Vector2Int dodgeRange;           // Legs/Boots only
    public Vector2Int movementSpeedRange;  // Legs/Boots only
    public Vector2Int hpRecoveryRange;     // Body only
    public Vector2Int spRecoveryRange;     // Body only

    [Header("Core Stats (All equipment)")]
    public Vector2Int strengthRange;
    public Vector2Int agilityRange;
    // ... остальные базовые статы
}
```

### **2️⃣ Логика генерации по типу слота:**

#### **🗡️ Оружие (Weapon/RightHand/LeftHand):**
- ✅ **Урон**: Min/Max Damage
- ✅ **Скорость атаки**: Attack Speed (может быть отрицательной)
- ✅ **Базовые статы**: STR, AGI, CON, ACC, SPI
- ✅ **Критический удар**: Critical

#### **🛡️ Броня - общие статы:**
- ✅ **Защита**: Armor (плоская) + Physical Resistance (%)
- ✅ **Базовые статы**: STR, AGI, CON, ACC, SPI

#### **🪖 Шлем (Head):**
- ✅ **Защита** + **Max HP/MP** + **Critical**
- ✅ **Базовые статы**

#### **👕 Доспех (Body):**
- ✅ **Защита** + **Max HP/MP** + **HP/SP Recovery**
- ✅ **Базовые статы**

#### **👖 Штаны/Ботинки (Legs/Boots):**
- ✅ **Защита** + **Dodge** + **Movement Speed** + **Max HP**
- ✅ **Базовые статы**

#### **🧤 Перчатки (Gloves):**
- ✅ **Защита** + **Accuracy** + **Critical** + **Attack Speed**
- ✅ **Базовые статы**

#### **💍 Аксессуары (Ring/Necklace):**
- ✅ **Max HP/MP** + **Critical** + **HP/SP Recovery**
- ✅ **Базовые статы**

### **3️⃣ Умная генерация Sample Configuration:**

```csharp
public void GenerateSampleConfiguration()
{
    if (baseItem == null) return;
    
    EquipmentSlot slot = baseItem.equipmentSlot;
    ItemType itemType = baseItem.itemType;
    
    if (itemType == ItemType.Weapon)
        GenerateWeaponConfigs();      // ✅ Конфиги для оружия
    else if (itemType == ItemType.Armor)
        GenerateArmorConfigs(slot);   // ✅ Конфиги для конкретного слота брони
    else if (itemType == ItemType.Accessory)
        GenerateAccessoryConfigs();   // ✅ Конфиги для аксессуаров
}
```

#### **Примеры конфигураций:**

**Шлем (Level 15):**
```csharp
new LevelConfig
{
    level = 15,
    armorRange = new Vector2Int(20, 30),           // Защита
    physicalResistRange = new Vector2Int(5, 10),   // Сопротивление
    strengthRange = new Vector2Int(0, 3),          // Базовые статы
    constitutionRange = new Vector2Int(1, 4),
    healthRange = new Vector2Int(50, 100),         // HP
    manaRange = new Vector2Int(25, 60),            // MP
    criticalRange = new Vector2Int(0, 2),          // Критический удар
    statChance = 0.4f
}
```

**Ботинки (Level 30):**
```csharp
new LevelConfig
{
    level = 30,
    armorRange = new Vector2Int(35, 55),           // Защита
    physicalResistRange = new Vector2Int(8, 15),   // Сопротивление
    agilityRange = new Vector2Int(2, 8),           // Ловкость (важно для ботинок)
    healthRange = new Vector2Int(150, 280),        // HP
    dodgeRange = new Vector2Int(4, 12),            // ✅ Уворот (специфично)
    movementSpeedRange = new Vector2Int(10, 25),   // ✅ Скорость (специфично)
    statChance = 0.5f
}
```

**Меч (Level 50):**
```csharp
new LevelConfig
{
    level = 50,
    minDamageRange = new Vector2Int(180, 220),     // ✅ Урон
    maxDamageRange = new Vector2Int(220, 260),
    attackSpeedRange = new Vector2Int(-8, 12),     // ✅ Скорость атаки
    strengthRange = new Vector2Int(4, 15),         // Сила (важно для оружия)
    criticalRange = new Vector2Int(3, 12),         // Критический удар
    statChance = 0.6f
}
```

### **4️⃣ Обновленный Editor Window:**

```csharp
private void DrawRelevantStatRanges(ItemGenerator.LevelConfig config)
{
    if (generator.baseItem == null)
    {
        EditorGUILayout.HelpBox("Select a base item to see relevant stat ranges", MessageType.Info);
        return;
    }
    
    EquipmentSlot slot = generator.baseItem.equipmentSlot;
    ItemType itemType = generator.baseItem.itemType;
    
    // ✅ Показываем только релевантные поля в зависимости от типа предмета
    if (itemType == ItemType.Weapon)
    {
        EditorGUILayout.LabelField("Weapon Stats", EditorStyles.boldLabel);
        DrawStatRange("Min Damage", ref config.minDamageRange);
        DrawStatRange("Max Damage", ref config.maxDamageRange);
        DrawStatRange("Attack Speed", ref config.attackSpeedRange);
    }
    
    if (itemType == ItemType.Armor)
    {
        EditorGUILayout.LabelField("Armor Stats", EditorStyles.boldLabel);
        DrawStatRange("Armor (Flat)", ref config.armorRange);
        DrawStatRange("Physical Resist (%)", ref config.physicalResistRange);
        
        switch (slot)
        {
            case EquipmentSlot.Head:
                EditorGUILayout.LabelField("Helmet Specific", EditorStyles.boldLabel);
                DrawStatRange("Max Health", ref config.healthRange);
                DrawStatRange("Critical", ref config.criticalRange);
                break;
                
            case EquipmentSlot.Legs:
            case EquipmentSlot.Boots:
                EditorGUILayout.LabelField("Legs/Boots Specific", EditorStyles.boldLabel);
                DrawStatRange("Dodge", ref config.dodgeRange);           // ✅ Только для ног
                DrawStatRange("Movement Speed", ref config.movementSpeedRange); // ✅ Только для ног
                break;
        }
    }
    
    // Базовые статы для всех предметов
    EditorGUILayout.LabelField("Core Stats (All Equipment)", EditorStyles.boldLabel);
    DrawStatRange("Strength", ref config.strengthRange);
    // ...
}
```

### **5️⃣ Обновленная генерация статов:**

```csharp
private void GenerateStatsForItem(Item item, LevelConfig config)
{
    // ✅ Новые статы защиты
    if (config.armorRange.y > 0)
    {
        item.armorBonus = Random.Range(config.armorRange.x, config.armorRange.y + 1);
        Debug.Log($"Generated armor (flat): {item.armorBonus}");
    }
    
    if (config.physicalResistRange.y > 0)
    {
        item.physicalResistBonus = Random.Range(config.physicalResistRange.x, config.physicalResistRange.y + 1);
        Debug.Log($"Generated physical resistance: {item.physicalResistBonus}%");
    }
    
    // ✅ Скорость атаки (может быть отрицательной)
    if (config.attackSpeedRange.x != 0 || config.attackSpeedRange.y != 0)
    {
        item.attackSpeedConstantBonus = Random.Range(config.attackSpeedRange.x, config.attackSpeedRange.y + 1);
        Debug.Log($"Generated attack speed: {item.attackSpeedConstantBonus}");
    }
    
    // ✅ Legacy defense (для совместимости)
    if (config.defenseRange.y > 0)
    {
        item.physicalResist = Random.Range(config.defenseRange.x, config.defenseRange.y + 1);
        Debug.Log($"Generated defense (LEGACY): {item.physicalResist}");
    }
}
```

## 🎮 **Как использовать:**

### **1️⃣ Для шлема:**
1. **Выберите** `HelmetItem` как Base Item
2. **Нажмите** "Generate Sample Config"
3. **Результат**: Конфигурации с защитой, HP, MP, критическим ударом
4. **В Editor Window**: Показываются только релевантные поля

### **2️⃣ Для ботинок:**
1. **Выберите** `BootsItem` как Base Item
2. **Нажмите** "Generate Sample Config"
3. **Результат**: Конфигурации с защитой, уворотом, скоростью движения
4. **В Editor Window**: Видны поля Dodge и Movement Speed

### **3️⃣ Для меча:**
1. **Выберите** `SwordItem` как Base Item
2. **Нажмите** "Generate Sample Config"
3. **Результат**: Конфигурации с уроном, скоростью атаки
4. **В Editor Window**: Показываются Weapon Stats

## 🔍 **Примеры генерируемых предметов:**

### **Шлем Level 30:**
```
Enhanced Iron Helmet
- Armor: 55 (flat reduction)
- Physical Resistance: 15% (percentage reduction)
- Strength: +6
- Constitution: +8
- Max Health: +200
- Max Mana: +120
- Critical: +4
```

### **Ботинки Level 30:**
```
Swift Leather Boots
- Armor: 45 (flat reduction)
- Physical Resistance: 12% (percentage reduction)
- Agility: +8 (важно для ботинок)
- Constitution: +6
- Max Health: +280
- Dodge: +12 (специфично для ног)
- Movement Speed: +25 (специфично для ног)
```

### **Меч Level 50:**
```
Colossus Blade of Power
- Min Damage: 200
- Max Damage: 240
- Attack Speed: +10 (может быть отрицательным)
- Strength: +15 (важно для оружия)
- Agility: +12
- Accuracy: +8
- Critical: +12
```

## 🎯 **Итог:**

✅ **ItemGenerator теперь умный** - показывает только релевантные статы
✅ **Конфигурации генерируются автоматически** по типу предмета
✅ **Editor Window адаптивный** - разные поля для разных слотов
✅ **Логика MMO** - каждый слот дает подходящие статы
✅ **Обратная совместимость** - старые поля работают

**Теперь если вы выберете шлем - увидите только статы шлема, а если меч - только статы оружия!** ⚔️🛡️
