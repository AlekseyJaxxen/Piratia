# 🎁 Система стартовых предметов

## 🎯 **Обзор системы**

Система автоматически выдает стартовые предметы игрокам при входе в игру. Поддерживает:
- ✅ **Автоматическую выдачу** при спавне игрока
- ✅ **Ограничения по классам** и уровням
- ✅ **Шанс получения** предметов
- ✅ **Динамические статы** для предметов
- ✅ **Однократную выдачу** (игрок получает предметы только один раз)

## 🛠️ **Компоненты системы:**

### **1️⃣ StarterItemsSystem.cs**
Основной компонент системы, управляет списком стартовых предметов и их выдачей.

### **2️⃣ StarterItemsTrigger.cs**
Компонент для автоматической выдачи предметов при спавне игрока.

### **3️⃣ StarterItemsManager.cs**
Менеджер для создания и настройки системы в сцене.

## 🎮 **Как использовать:**

### **Способ 1: Автоматическая настройка**

1. **Создайте пустой GameObject** в сцене
2. **Добавьте компонент `StarterItemsManager`**
3. **Настройте дефолтные предметы** в Inspector
4. **Система автоматически создаст** все необходимые компоненты

### **Способ 2: Ручная настройка**

1. **Создайте GameObject** с компонентом `StarterItemsSystem`
2. **Настройте стартовые предметы** в Inspector
3. **Создайте GameObject** с компонентом `StarterItemsTrigger`
4. **Свяжите компоненты** между собой

## ⚙️ **Настройка стартовых предметов:**

### **В Inspector StarterItemsSystem:**

```csharp
[Header("Starter Items Configuration")]
public List<StarterItemData> starterItems;

[Header("Settings")]
public bool giveItemsOnSpawn = true;        // Выдавать при спавне
public bool giveItemsOnlyOnce = true;       // Только один раз
public bool logItemGiving = true;           // Логирование
```

### **Настройка каждого предмета:**

```csharp
[System.Serializable]
public class StarterItemData
{
    public Item item;                        // Сам предмет
    public int quantity = 1;                 // Количество
    public bool useDynamicStats = false;     // Динамические статы
    
    public CharacterClass requiredClass = CharacterClass.Newbie;  // Требуемый класс
    public bool giveToAllClasses = true;     // Для всех классов
    
    public int minLevel = 1;                 // Минимальный уровень
    public int maxLevel = 100;               // Максимальный уровень
    
    [Range(0f, 1f)]
    public float chance = 1.0f;              // Шанс получения (0-1)
}
```

## 🎯 **Примеры настройки:**

### **Пример 1: Базовые предметы для всех**
```csharp
// Стартовый меч для всех игроков
item = BasicSword
quantity = 1
requiredClass = Newbie
giveToAllClasses = true
chance = 1.0f
```

### **Пример 2: Предмет только для воинов**
```csharp
// Щит только для воинов
item = WarriorShield
quantity = 1
requiredClass = Warrior
giveToAllClasses = false
chance = 1.0f
```

### **Пример 3: Редкий предмет с шансом**
```csharp
// Редкий предмет с 50% шансом
item = RarePotion
quantity = 1
requiredClass = Newbie
giveToAllClasses = true
chance = 0.5f
```

### **Пример 4: Предмет с ограничением по уровню**
```csharp
// Предмет только для игроков 5-10 уровня
item = Level5Armor
quantity = 1
requiredClass = Newbie
giveToAllClasses = true
minLevel = 5
maxLevel = 10
chance = 1.0f
```

## 🔧 **API для работы с системой:**

### **Добавление предмета:**
```csharp
StarterItemsSystem system = FindObjectOfType<StarterItemsSystem>();
system.AddStarterItem(swordItem, 1, CharacterClass.Newbie);
```

### **Удаление предмета:**
```csharp
system.RemoveStarterItem(swordItem);
```

### **Ручная выдача предметов:**
```csharp
PlayerCore player = FindObjectOfType<PlayerCore>();
system.GiveStarterItemsToPlayer(player);
```

### **Выдача всем игрокам:**
```csharp
system.GiveItemsToAllPlayers();
```

### **Очистка списка получивших:**
```csharp
system.ClearReceivedItemsList(); // Для тестирования
```

## 🎮 **Интеграция с игрой:**

### **Автоматическая выдача при спавне:**
Система автоматически интегрирована с `MyNetworkManager`:
- При спавне игрока автоматически вызывается `GiveStarterItemsToPlayer()`
- Игрок получает предметы сразу после создания персонажа

### **Логирование:**
```
[StarterItemsSystem] Giving starter items to player: PlayerName
[StarterItemsSystem] Gave 1x Basic Sword to PlayerName
[StarterItemsSystem] Completed giving items to PlayerName: 3 given, 0 failed
```

## 🎨 **Создание объекта в сцене:**

### **Быстрый способ:**
1. **Создайте пустой GameObject** в сцене
2. **Назовите его "StarterItemsManager"**
3. **Добавьте компонент `StarterItemsManager`**
4. **Настройте дефолтные предметы** в Inspector
5. **Готово!** ✅

### **Настройка в Inspector:**
```
StarterItemsManager:
├── Auto Setup On Start: ✅
├── Create Starter Items System: ✅
├── Create Starter Items Trigger: ✅
├── Default Starter Items: [Sword, Armor, Potion, ...]
└── Default Quantities: [1, 1, 1, ...]
```

## 🧪 **Тестирование:**

### **Контекстное меню (Context Menu):**
- **"Give Items to All Players"** - выдать предметы всем игрокам
- **"Clear Received Items List"** - очистить список получивших
- **"Setup Starter Items System"** - настроить систему

### **Проверка в игре:**
1. **Запустите игру**
2. **Создайте игрока**
3. **Проверьте инвентарь** - должны появиться стартовые предметы
4. **Проверьте логи** в консоли

## 🔍 **Отладка:**

### **Включить отладку:**
```csharp
[Header("Debug")]
public bool showDebugInfo = true;
public bool logItemGiving = true;
```

### **Проверить настройки:**
```csharp
// Количество стартовых предметов
int count = system.GetStarterItemsCount();

// Список предметов
var items = system.GetStarterItems();
```

### **Частые проблемы:**
1. **Предметы не выдаются** - проверьте, что `StarterItemsSystem` есть в сцене
2. **Предметы выдаются повторно** - включите `giveItemsOnlyOnce = true`
3. **Не все предметы выдаются** - проверьте ограничения по классу/уровню
4. **Инвентарь полон** - проверьте свободные слоты

## 🎯 **Итог:**

### **✅ Что получили:**
1. **Автоматическая выдача** стартовых предметов при входе в игру
2. **Гибкая настройка** ограничений и условий
3. **Поддержка всех типов предметов** включая динамические
4. **Простая интеграция** с существующей системой
5. **Удобное тестирование** и отладка

### **🚀 Готово к использованию:**
- Создайте объект в сцене с `StarterItemsManager`
- Настройте нужные предметы в Inspector
- Игроки будут автоматически получать предметы при входе в игру!

**Система готова!** 🎉
