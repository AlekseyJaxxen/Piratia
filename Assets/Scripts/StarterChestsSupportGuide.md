# 🎁 Поддержка сундуков в системе стартовых предметов

## 🎯 **Проблема решена!**

Теперь в StarterItemsSystem можно добавлять сундуки как стартовые предметы. Система была расширена для поддержки как обычных предметов, так и сундуков.

## 🛠️ **Что добавлено:**

### **1️⃣ Поддержка сундуков в StarterItemData**

```csharp
[System.Serializable]
public class StarterItemData
{
    [Header("Item Settings")]
    public Item item;                    // Обычный предмет
    public int quantity = 1;
    public bool useDynamicStats = false;
    
    [Header("Chest Settings")]
    public bool isChest = false;         // Является ли предмет сундуком
    public ChestItemData chestData;      // Данные сундука (если isChest = true)
    
    [Header("Class Restrictions")]
    public CharacterClass requiredClass = CharacterClass.Newbie;
    public bool giveToAllClasses = true;
    
    // ... остальные настройки
}
```

### **2️⃣ Новые методы для работы с сундуками**

```csharp
// Добавление сундука
public void AddStarterChest(ChestItemData chestData, int quantity = 1, CharacterClass requiredClass = CharacterClass.Newbie)

// Удаление сундука
public void RemoveStarterChest(ChestItemData chestData)
```

### **3️⃣ Обновленный StarterItemsManager**

```csharp
[Header("Default Starter Chests")]
[SerializeField] private ChestItemData[] defaultStarterChests;
[SerializeField] private int[] defaultChestQuantities = { 1, 1 };
```

## 🎮 **Как использовать:**

### **Способ 1: Через Inspector в StarterItemsManager**

1. **Создайте StarterItemsManager** в сцене
2. **В разделе "Default Starter Chests":**
   - Добавьте сундуки в массив `Default Starter Chests`
   - Настройте количества в `Default Chest Quantities`
3. **Система автоматически добавит сундуки** при старте

### **Способ 2: Через код**

```csharp
// Получить систему стартовых предметов
StarterItemsSystem system = FindObjectOfType<StarterItemsSystem>();

// Добавить сундук для всех классов
system.AddStarterChest(warriorChestData, 1, CharacterClass.Newbie);

// Добавить сундук только для воинов
system.AddStarterChest(warriorChestData, 1, CharacterClass.Warrior);
```

### **Способ 3: Через StarterItemsManager**

```csharp
// Получить менеджер
StarterItemsManager manager = FindObjectOfType<StarterItemsManager>();

// Добавить сундук
manager.AddStarterChest(warriorChestData, 1, CharacterClass.Warrior);
```

## ⚙️ **Настройка в Inspector:**

### **StarterItemsManager:**
```
StarterItemsManager:
├── Default Starter Items: [Sword, Armor, Potion]
├── Default Quantities: [1, 1, 3]
├── Default Starter Chests: [WarriorChest, MageChest]  ← НОВОЕ!
├── Default Chest Quantities: [1, 1]                   ← НОВОЕ!
└── Auto Setup On Start: ✅
```

### **StarterItemsSystem (продвинутая настройка):**
```
StarterItemsSystem:
├── Starter Items:
│   ├── [0] Item: BasicSword
│   │   ├── Is Chest: ❌
│   │   └── Chest Data: None
│   ├── [1] Item: None
│   │   ├── Is Chest: ✅                    ← НОВОЕ!
│   │   └── Chest Data: WarriorChest        ← НОВОЕ!
│   └── [2] Item: None
│       ├── Is Chest: ✅                    ← НОВОЕ!
│       └── Chest Data: MageChest           ← НОВОЕ!
└── Give Items On Spawn: ✅
```

## 🎯 **Примеры использования:**

### **Пример 1: Сундук для всех новичков**
```csharp
// Настройки:
StarterItemData starterChest = {
    isChest: true,
    chestData: newbieChestData,
    quantity: 1,
    requiredClass: Newbie,
    giveToAllClasses: true,
    chance: 1.0f
}

// Результат:
// Все игроки получат: 1x NewbieChest при входе в игру
```

### **Пример 2: Класс-специфичные сундуки**
```csharp
// Настройки:
StarterItemData warriorChest = {
    isChest: true,
    chestData: warriorChestData,
    quantity: 1,
    requiredClass: Warrior,
    giveToAllClasses: false,
    chance: 1.0f
}

StarterItemData mageChest = {
    isChest: true,
    chestData: mageChestData,
    quantity: 1,
    requiredClass: Mage,
    giveToAllClasses: false,
    chance: 1.0f
}

// Результат:
// Warrior получит: 1x WarriorChest
// Mage получит: 1x MageChest
// Archer получит: ничего (нет сундука для Archer)
```

### **Пример 3: Смешанные стартовые предметы**
```csharp
// Настройки:
StarterItems = [
    { item: BasicSword, isChest: false, quantity: 1 },      // Обычный предмет
    { chestData: NewbieChest, isChest: true, quantity: 1 }, // Сундук
    { item: HealthPotion, isChest: false, quantity: 3 }     // Обычный предмет
]

// Результат:
// Все игроки получат: BasicSword + NewbieChest + HealthPotion x3
```

## 🔧 **API для работы:**

### **Добавление сундука:**
```csharp
// Через StarterItemsSystem
system.AddStarterChest(chestData, 1, CharacterClass.Newbie);

// Через StarterItemsManager
manager.AddStarterChest(chestData, 1, CharacterClass.Warrior);
```

### **Удаление сундука:**
```csharp
// Через StarterItemsSystem
system.RemoveStarterChest(chestData);

// Через StarterItemsManager
manager.RemoveStarterChest(chestData);
```

### **Проверка сундука:**
```csharp
// Проверить, является ли стартовый предмет сундуком
bool isChest = starterItem.isChest;

// Получить данные сундука
ChestItemData chestData = starterItem.chestData;
```

## 🎨 **Создание стартовых сундуков в Unity:**

### **Шаг 1: Создание сундука**
1. **Создайте ChestItemData** (Create → Items → Chest Item)
2. **Настройте награды** в сундуке
3. **Сохраните** как "NewbieChest"

### **Шаг 2: Добавление в стартовые предметы**
1. **Найдите StarterItemsManager** в сцене
2. **В "Default Starter Chests"** добавьте созданный сундук
3. **Настройте количество** в "Default Chest Quantities"

### **Шаг 3: Тестирование**
1. **Запустите игру**
2. **Создайте игрока**
3. **Проверьте инвентарь** - должен появиться сундук
4. **Откройте сундук** - должны появиться награды

## 🧪 **Тестирование:**

### **Тест 1: Обычный стартовый сундук**
1. **Добавьте сундук** в Default Starter Chests
2. **Запустите игру** и создайте игрока
3. **Проверьте инвентарь** - должен быть сундук
4. **Откройте сундук** - должны появиться награды

### **Тест 2: Класс-специфичный сундук**
1. **Создайте сундук** с наградами для воинов
2. **Добавьте его** как стартовый предмет для Warrior
3. **Создайте игрока-воина** - должен получить сундук
4. **Создайте игрока-мага** - НЕ должен получить сундук

### **Тест 3: Проверка логов**
```
[StarterItemsSystem] Created chest item: NewbieChest
[StarterItemsSystem] Added starter chest: 1x NewbieChest
[StarterItemsSystem] Gave 1x NewbieChest to PlayerName
```

## 🔍 **Отладка:**

### **Частые проблемы:**
1. **Сундук не появляется** - проверьте, что `isChest = true` и `chestData` назначен
2. **Сундук не открывается** - проверьте, что у сундука есть награды
3. **Неправильный класс** - проверьте настройки `requiredClass` и `giveToAllClasses`

### **Логи для отладки:**
```
[StarterItemsSystem] StarterItem has no item or chestData!
[StarterItemsSystem] Created chest item: WarriorChest
[StarterItemsSystem] Added starter chest: 1x WarriorChest
```

## 🎯 **Итог:**

### **✅ Что получили:**
1. **Полную поддержку сундуков** в системе стартовых предметов
2. **Удобную настройку** через Inspector
3. **Класс-специфичные сундуки** для разных игроков
4. **Обратную совместимость** с обычными предметами
5. **Гибкую систему** для создания разнообразных стартовых наборов

### **🚀 Готово к использованию:**
- Добавляйте сундуки в Default Starter Chests
- Настраивайте класс-специфичные сундуки
- Игроки будут получать сундуки при входе в игру
- Сундуки будут содержать подходящие награды для их класса!

**Теперь можно добавлять сундуки как стартовые предметы!** 🎉
