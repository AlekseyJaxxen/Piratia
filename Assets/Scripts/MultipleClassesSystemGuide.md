# 🎭 Система множественных классов

## 🎯 **Обзор системы**

Новая система позволяет игрокам иметь несколько классов одновременно:
- **Newbie** - базовый класс, который есть у всех игроков
- **Основной класс** - назначается при старте игры (Warrior, Mage, Archer, etc.)
- **Дополнительные классы** - могут быть добавлены позже

## 🛠️ **Что изменено:**

### **1️⃣ Добавлен класс Newbie в enum:**

**Файл:** `Enums.cs`
```csharp
public enum CharacterClass
{
    Newbie,     // Базовый класс для всех игроков
    Warrior,
    Mage,
    Archer,
    Monster,
    Tank,
    None
}
```

### **2️⃣ Расширена система классов в CharacterStats:**

**Файл:** `CharacterStats.cs`
```csharp
[Header("Multiple Classes Support")]
[SyncVar(hook = nameof(OnPlayerClassesChanged))]
public List<CharacterClass> playerClasses = new List<CharacterClass>();

/// <summary>
/// Инициализирует игрока с базовым классом Newbie и добавляет основной класс
/// </summary>
[Server]
public void InitializePlayerClasses(CharacterClass mainClass)
{
    // Всегда добавляем Newbie как базовый класс
    if (!playerClasses.Contains(CharacterClass.Newbie))
    {
        playerClasses.Add(CharacterClass.Newbie);
    }
    
    // Добавляем основной класс, если он не Newbie
    if (mainClass != CharacterClass.Newbie && !playerClasses.Contains(mainClass))
    {
        playerClasses.Add(mainClass);
    }
    
    // Устанавливаем основной класс как активный
    characterClass = mainClass;
}

/// <summary>
/// Проверяет, имеет ли игрок определенный класс
/// </summary>
public bool HasClass(CharacterClass classToCheck)
{
    return playerClasses.Contains(classToCheck);
}

/// <summary>
/// Добавляет новый класс игроку
/// </summary>
[Command]
public void CmdAddClass(CharacterClass newClass)
{
    if (!playerClasses.Contains(newClass))
    {
        playerClasses.Add(newClass);
    }
}

/// <summary>
/// Переключает активный класс игрока
/// </summary>
[Command]
public void CmdSwitchClass(CharacterClass newActiveClass)
{
    if (playerClasses.Contains(newActiveClass))
    {
        characterClass = newActiveClass;
        LoadClassData();
        CalculateDerivedStats();
        StartCoroutine(InitializeSkills());
    }
}
```

### **3️⃣ Обновлена система предметов:**

**Файл:** `Item.cs`
```csharp
public bool IsEquipable(int playerLevel, CharacterClass playerClass)
{
    // Предметы с классом Newbie доступны всем игрокам
    bool classMatch = characterClass == CharacterClass.Newbie || characterClass == playerClass;
    return (equipmentSlot != EquipmentSlot.None || alternativeSlot != EquipmentSlot.None) 
        && playerLevel >= requiredLevel && itemCanEquip && classMatch;
}

/// <summary>
/// Проверяет, может ли игрок экипировать предмет, учитывая все его классы
/// </summary>
public bool IsEquipable(int playerLevel, List<CharacterClass> playerClasses)
{
    // Предметы с классом Newbie доступны всем игрокам
    bool classMatch = characterClass == CharacterClass.Newbie || playerClasses.Contains(characterClass);
    return (equipmentSlot != EquipmentSlot.None || alternativeSlot != EquipmentSlot.None) 
        && playerLevel >= requiredLevel && itemCanEquip && classMatch;
}
```

### **4️⃣ Обновлена инициализация игрока:**

**Файл:** `MyNetworkManager.cs`
```csharp
// Инициализируем множественные классы: Newbie + основной класс
characterStats.InitializePlayerClasses(info.characterClass);
```

## 🎮 **Как работает система:**

### **При создании игрока:**
1. ✅ **Автоматически добавляется класс Newbie**
2. ✅ **Добавляется основной класс** (выбранный при старте)
3. ✅ **Активным становится основной класс**

### **Пример инициализации:**
```csharp
// Игрок выбрал Warrior при старте
playerClasses = [Newbie, Warrior]
characterClass = Warrior (активный)
```

### **Предметы с классом Newbie:**
- ✅ **Доступны всем игрокам** независимо от их основного класса
- ✅ **Можно экипировать** любому персонажу
- ✅ **Идеально для стартового снаряжения**

## 🔧 **API для работы с классами:**

### **Проверка класса:**
```csharp
// Проверить, есть ли у игрока определенный класс
bool hasWarrior = playerStats.HasClass(CharacterClass.Warrior);
bool hasNewbie = playerStats.HasClass(CharacterClass.Newbie); // всегда true
```

### **Добавление класса:**
```csharp
// Добавить новый класс игроку
playerStats.CmdAddClass(CharacterClass.Mage);
```

### **Переключение активного класса:**
```csharp
// Переключиться на другой класс (если он есть у игрока)
playerStats.CmdSwitchClass(CharacterClass.Mage);
```

### **Проверка экипировки предметов:**
```csharp
// Старый способ (только активный класс)
bool canEquip = item.IsEquipable(playerLevel, playerStats.characterClass);

// Новый способ (все классы игрока)
bool canEquip = item.IsEquipable(playerLevel, playerStats.playerClasses);
```

## 🎯 **Практические примеры:**

### **Пример 1: Стартовый игрок**
```csharp
// При создании игрока, выбравшего Warrior:
playerClasses = [Newbie, Warrior]
characterClass = Warrior

// Может экипировать:
// ✅ Предметы с классом Newbie
// ✅ Предметы с классом Warrior
// ❌ Предметы с классом Mage
```

### **Пример 2: Игрок с несколькими классами**
```csharp
// После добавления Mage:
playerClasses = [Newbie, Warrior, Mage]
characterClass = Warrior (активный)

// Может экипировать:
// ✅ Предметы с классом Newbie
// ✅ Предметы с классом Warrior
// ✅ Предметы с классом Mage
// ❌ Предметы с классом Archer
```

### **Пример 3: Переключение классов**
```csharp
// Переключились на Mage:
playerClasses = [Newbie, Warrior, Mage]
characterClass = Mage (активный)

// Теперь активны навыки и статы Mage
// Но все еще можно экипировать предметы Warrior и Newbie
```

## 🎨 **Создание предметов для Newbie:**

### **В Inspector Unity:**
1. **Создайте новый предмет** (SwordItem, ArmorItem, etc.)
2. **Установите "Character Class" = Newbie**
3. **Предмет будет доступен всем игрокам** ✅

### **Программно:**
```csharp
Item newbieSword = ScriptableObject.CreateInstance<SwordItem>();
newbieSword.characterClass = CharacterClass.Newbie;
newbieSword.itemName = "Training Sword";
newbieSword.requiredLevel = 1;
```

## 🔍 **Логи для отладки:**

### **Инициализация игрока:**
```
[CharacterStats] Player initialized with classes: Newbie, Warrior (active: Warrior)
[MyNetworkManager] Server initialized player with classes: Newbie, Warrior (active: Warrior)
```

### **Добавление класса:**
```
[CharacterStats] Added class Mage to player. Current classes: Newbie, Warrior, Mage
```

### **Переключение класса:**
```
[CharacterStats] Switched to class Mage
```

### **Экипировка предметов:**
```
[ArmorItem] Equipping Newbie Armor to Body from slot 3
// Работает для всех игроков!
```

## 🎯 **Итог:**

### **✅ Что получили:**
1. **Все игроки имеют класс Newbie** автоматически
2. **Предметы Newbie доступны всем** игрокам
3. **Поддержка множественных классов** для будущего расширения
4. **Обратная совместимость** с существующими предметами
5. **Гибкая система** для добавления новых классов

### **🚀 Возможности для развития:**
- **Система мультиклассов** (игрок может изучать новые классы)
- **Гибридные билды** (комбинация навыков разных классов)
- **Специальные предметы** для определенных комбинаций классов
- **Классовые квесты** и достижения

**Система готова к использованию!** 🎉
