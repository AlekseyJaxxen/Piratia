# 🎬 Настройка DoTween эффектов удара для монстров

## ✅ **Что создано:**

Создана система DoTween эффектов получения удара для не-гуманоидных монстров (использующих Animation компонент).

## 🛠️ **Как настроить для вашего монстра:**

### **1️⃣ Добавьте компонент MonsterHitEffects:**

1. **Откройте префаб монстра** в Unity
2. **Найдите child объект с MeshRenderer** (модель монстра)
3. **Добавьте компонент `MonsterHitEffects`** на этот объект
4. **Настройте параметры** в Inspector

### **2️⃣ Структура префаба должна быть:**

```
MushroomMonsterPrefab
├── 🧠 Monster.cs
├── 🎬 Animation Component
├── ❤️ HealthMonster.cs
└── 📦 MushroomModel (child)
    ├── 🎨 MeshRenderer
    └── ✨ MonsterHitEffects.cs ← ДОБАВИТЬ СЮДА
```

### **3️⃣ Настройки компонента MonsterHitEffects:**

#### **🎨 Hit Effect Settings:**
- **Hit Flash Duration**: `0.3` - длительность вспышки цвета
- **Hit Flash Color**: `Red` - цвет вспышки при ударе
- **Hit Flash Curve**: `EaseInOut` - кривая анимации цвета

#### **💥 Shake Effect Settings:**
- **Shake Duration**: `0.4` - длительность тряски
- **Shake Strength**: `0.15` - сила тряски
- **Shake Vibrato**: `10` - частота вибрации
- **Shake Randomness**: `90` - случайность направления

#### **📐 Scale Effect Settings:**
- **Scale Hit Multiplier**: `0.9` - насколько сжимается при ударе
- **Scale Duration**: `0.2` - длительность эффекта масштаба
- **Scale Curve**: `EaseInOut` - кривая анимации масштаба

#### **🚀 Knockback Effect Settings:**
- **Enable Knockback**: `✓ true` - включить отталкивание
- **Knockback Force**: `0.3` - сила отталкивания
- **Knockback Duration**: `0.2` - длительность отталкивания

## 🎯 **Как это работает:**

### **Автоматически:**
```csharp
// При получении урона монстром автоматически вызывается:
monster.PlaySimpleHitEffect();

// Что запускает последовательность:
1. 🎨 Вспышка красного цвета (0.3 сек)
2. 📐 Сжатие масштаба и возврат (0.2 сек)
3. 💥 Тряска позиции (0.4 сек)
4. 🚀 Отталкивание и возврат (0.2 сек)
```

### **Программно:**
```csharp
// В коде можно вызвать эффект с направлением удара:
Vector3 hitDirection = (target.position - attacker.position).normalized;
monster.PlayHitEffect(hitDirection);

// Или простой эффект без направления:
monster.PlaySimpleHitEffect();
```

## 🎭 **Примеры настроек для разных монстров:**

### **🍄 Гриб (мягкий эффект):**
```
Hit Flash Duration: 0.4
Hit Flash Color: Orange
Shake Strength: 0.1
Scale Hit Multiplier: 0.95
Knockback Force: 0.2
```

### **🗿 Каменный голем (жесткий эффект):**
```
Hit Flash Duration: 0.2
Hit Flash Color: Yellow
Shake Strength: 0.3
Scale Hit Multiplier: 0.98
Knockback Force: 0.1
```

### **👻 Призрак (мистический эффект):**
```
Hit Flash Duration: 0.5
Hit Flash Color: Purple
Shake Strength: 0.05
Scale Hit Multiplier: 0.85
Knockback Force: 0.4
```

## 🔧 **Отладка и тестирование:**

### **В редакторе:**
1. **Выберите объект** с MonsterHitEffects
2. **В контекстном меню** нажмите `Test Hit Effect`
3. **Наблюдайте** за эффектом в Scene view

### **В игре:**
```csharp
// Логи для отладки:
[MonsterHitEffects] Initialized for monster: MushroomMonster
[Monster] MonsterHitEffects initialized for MushroomMonster
[Monster] Playing hit effect for non-humanoid monster MushroomMonster
[MonsterHitEffects] Playing hit effect for MushroomMonster
[MonsterHitEffects] Hit effect completed for MushroomMonster
```

## 🎨 **Визуальные эффекты:**

### **🎬 Последовательность анимации:**
```
Время: 0.0s ────────────────────────────────── 1.0s
       │                                        │
Цвет:  │██████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│ (красная вспышка)
       │                                        │
Масштаб:│▼▼▼▲▲▲▲▲▲░░░░░░░░░░░░░░░░░░░░░░░░░░░│ (сжатие → возврат)
       │                                        │
Тряска: │░░░░██████████████████░░░░░░░░░░░░░░░░│ (вибрация)
       │                                        │
Отталк.:│░░░░▲▲▲▼▼▼░░░░░░░░░░░░░░░░░░░░░░░░░░░│ (отлет → возврат)
```

### **🌟 Комбинированный эффект:**
- **Одновременно**: цвет + масштаб + тряска + отталкивание
- **Результат**: живой, реалистичный эффект получения удара
- **Синхронизация**: все эффекты работают по сети

## 🎯 **Совместимость:**

### **✅ Работает с:**
- **Animation компонентом** (не-гуманоидные монстры)
- **Любыми MeshRenderer** моделями
- **Mirror Networking** (синхронизировано)
- **Существующими эффектами** MonsterAnimation

### **❌ Не подходит для:**
- **Animator контроллеров** (гуманоидные монстры)
- **UI элементов**
- **Статичных объектов**

## 🎉 **Результат:**

После настройки ваши не-гуманоидные монстры будут:
- ✅ **Реалистично реагировать** на получение урона
- ✅ **Показывать направление** удара через отталкивание
- ✅ **Иметь плавные анимации** через DoTween
- ✅ **Синхронизироваться** по сети
- ✅ **Сочетаться** с основными анимациями Animation компонента

**Ваши монстры станут более живыми и отзывчивыми!** 🎭
