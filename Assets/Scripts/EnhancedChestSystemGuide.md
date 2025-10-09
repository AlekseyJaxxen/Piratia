# 🎁 Улучшенная система сундуков

## 🎯 **Обзор улучшений**

Система сундуков была значительно улучшена:
- ✅ **Выбор предметов через SO** вместо ввода ID
- ✅ **Разный дроп в зависимости от класса** игрока
- ✅ **Класс-специфичные награды** и золото
- ✅ **Обратная совместимость** со старыми сундуками

## 🛠️ **Новые возможности:**

### **1️⃣ Выбор предметов через SO**

**БЫЛО:**
```csharp
public int itemId = -1; // Нужно было помнить ID
```

**СТАЛО:**
```csharp
[SerializeField] private Item itemSO; // Выбор предмета через SO
[SerializeField] private int itemId = -1; // Fallback для старых сундуков
```

### **2️⃣ Ограничения по классам**

```csharp
[Header("Ограничения по классу")]
public CharacterClass requiredClass = CharacterClass.Newbie; // Требуемый класс
public bool giveToAllClasses = true; // Давать всем классам
```

### **3️⃣ Класс-специфичные награды**

```csharp
[Header("Класс-специфичные награды")]
public bool useClassSpecificRewards = false; // Использовать разный дроп
public List<ClassSpecificRewards> classSpecificRewards = new List<ClassSpecificRewards>();
```

## 🎮 **Как использовать:**

### **Способ 1: Обычные сундуки (как раньше)**

1. **Создайте ChestItemData** в Unity
2. **Настройте предметы** в разделе "Предметы в сундуке"
3. **Выберите предметы через SO** в Inspector
4. **Настройте шансы и количество**

### **Способ 2: Класс-специфичные сундуки**

1. **Создайте ChestItemData** в Unity
2. **Включите "Use Class Specific Rewards"**
3. **Настройте награды для каждого класса** в "Class Specific Rewards"
4. **Каждый класс получит свои предметы**

## ⚙️ **Настройка в Inspector:**

### **Обычный сундук:**
```
ChestItemData:
├── Chest Name: "Сундук новичка"
├── Description: "Полезные предметы для начинающих"
├── Use Class Specific Rewards: ❌
├── Rewards:
│   ├── Item SO: [BasicSword] (выбор через SO!)
│   ├── Quantity: 1
│   ├── Drop Chance: 1.0
│   ├── Required Class: Newbie
│   ├── Give To All Classes: ✅
│   └── Is Guaranteed: ✅
└── Gold Reward: 100
```

### **Класс-специфичный сундук:**
```
ChestItemData:
├── Chest Name: "Сундук воина"
├── Use Class Specific Rewards: ✅
├── Class Specific Rewards:
│   ├── [0] Warrior:
│   │   ├── Target Class: Warrior
│   │   ├── Class Rewards:
│   │   │   ├── Item SO: [WarriorSword]
│   │   │   ├── Quantity: 1
│   │   │   └── Is Guaranteed: ✅
│   │   └── Class Gold Reward: 200
│   ├── [1] Mage:
│   │   ├── Target Class: Mage
│   │   ├── Class Rewards:
│   │   │   ├── Item SO: [MageStaff]
│   │   │   ├── Quantity: 1
│   │   │   └── Is Guaranteed: ✅
│   │   └── Class Gold Reward: 150
│   └── [2] Archer:
│       ├── Target Class: Archer
│       ├── Class Rewards:
│       │   ├── Item SO: [ArcherBow]
│       │   ├── Quantity: 1
│       │   └── Is Guaranteed: ✅
│       └── Class Gold Reward: 180
└── Gold Reward: 50 (fallback)
```

## 🎯 **Примеры использования:**

### **Пример 1: Сундук для всех классов**
```csharp
// Настройки:
useClassSpecificRewards = false
rewards = [
    { itemSO: BasicSword, quantity: 1, giveToAllClasses: true, isGuaranteed: true },
    { itemSO: HealthPotion, quantity: 3, giveToAllClasses: true, dropChance: 0.8 }
]

// Результат:
// Все игроки получат: BasicSword + 80% шанс на HealthPotion x3
```

### **Пример 2: Класс-специфичный сундук**
```csharp
// Настройки:
useClassSpecificRewards = true
classSpecificRewards = [
    {
        targetClass: Warrior,
        classRewards: [
            { itemSO: WarriorSword, quantity: 1, isGuaranteed: true },
            { itemSO: WarriorArmor, quantity: 1, dropChance: 0.7 }
        ],
        classGoldReward: 200
    },
    {
        targetClass: Mage,
        classRewards: [
            { itemSO: MageStaff, quantity: 1, isGuaranteed: true },
            { itemSO: ManaPotion, quantity: 5, dropChance: 0.9 }
        ],
        classGoldReward: 150
    }
]

// Результат:
// Warrior получит: WarriorSword + 70% шанс на WarriorArmor + 200 золота
// Mage получит: MageStaff + 90% шанс на ManaPotion x5 + 150 золота
```

### **Пример 3: Смешанный сундук**
```csharp
// Настройки:
useClassSpecificRewards = true
rewards = [
    { itemSO: HealthPotion, quantity: 2, giveToAllClasses: true, isGuaranteed: true }
]
classSpecificRewards = [
    {
        targetClass: Warrior,
        classRewards: [
            { itemSO: WarriorSword, quantity: 1, isGuaranteed: true }
        ]
    }
]

// Результат:
// Все игроки получат: HealthPotion x2
// Warrior дополнительно получит: WarriorSword
```

## 🔧 **API для работы:**

### **Генерация наград:**
```csharp
// Для конкретного игрока (с учетом класса)
List<ItemInfo> rewards = chestData.GenerateRewards(playerStats);

// Без учета класса (fallback)
List<ItemInfo> rewards = chestData.GenerateRewards();
```

### **Получение золота:**
```csharp
// Для конкретного игрока (с учетом класса)
int gold = chestData.GetGoldReward(playerStats);

// Без учета класса (fallback)
int gold = chestData.GetGoldReward();
```

### **Проверка предмета:**
```csharp
// Получение ID предмета
int itemId = reward.GetItemId();

// Получение самого предмета
Item item = reward.GetItem();

// Проверка, должен ли игрок получить предмет
bool shouldGive = reward.ShouldGiveToPlayer(playerStats);
```

## 🎨 **Создание сундуков в Unity:**

### **Шаг 1: Создание ChestItemData**
1. **Правый клик** в Project → Create → Items → Chest Item
2. **Назовите файл** (например, "WarriorChest")

### **Шаг 2: Настройка обычного сундука**
1. **Установите название** и описание
2. **Добавьте предметы** в "Rewards"
3. **Выберите предметы через SO** (перетащите из Project)
4. **Настройте количество и шансы**

### **Шаг 3: Настройка класс-специфичного сундука**
1. **Включите "Use Class Specific Rewards"**
2. **Добавьте классы** в "Class Specific Rewards"
3. **Для каждого класса настройте:**
   - Target Class (Warrior, Mage, etc.)
   - Class Name (для отображения)
   - Class Rewards (предметы для этого класса)
   - Class Gold Reward (золото для этого класса)

## 🧪 **Тестирование:**

### **Тест 1: Обычный сундук**
1. **Создайте сундук** с обычными наградами
2. **Откройте сундук** любым игроком
3. **Проверьте**, что все игроки получают одинаковые предметы

### **Тест 2: Класс-специфичный сундук**
1. **Создайте сундук** с класс-специфичными наградами
2. **Откройте сундук** игроком-воином
3. **Проверьте**, что получены предметы для воина
4. **Откройте сундук** игроком-магом
5. **Проверьте**, что получены предметы для мага

### **Тест 3: Проверка логов**
```
[ChestItemData] Player Warrior opened chest, received 2 items and 200 gold
[ChestItemData] Player Mage opened chest, received 3 items and 150 gold
```

## 🔍 **Отладка:**

### **Частые проблемы:**
1. **Предметы не выдаются** - проверьте, что Item SO назначен
2. **Неправильный класс** - проверьте настройки requiredClass
3. **Нет класс-специфичных наград** - проверьте useClassSpecificRewards

### **Логи для отладки:**
```
[ChestItemData] Item not found! ID: 123, SO: null
[ChestItemData] No class-specific rewards found for player class. Using default rewards.
```

## 🎯 **Итог:**

### **✅ Что получили:**
1. **Удобный выбор предметов** через SO вместо ID
2. **Разный дроп для разных классов** игроков
3. **Класс-специфичное золото** и награды
4. **Обратную совместимость** со старыми сундуками
5. **Гибкую систему** для создания разнообразных сундуков

### **🚀 Готово к использованию:**
- Создавайте сундуки через Create → Items → Chest Item
- Выбирайте предметы через SO в Inspector
- Настраивайте класс-специфичные награды
- Игроки будут получать подходящие предметы для их класса!

**Система сундуков значительно улучшена!** 🎉
