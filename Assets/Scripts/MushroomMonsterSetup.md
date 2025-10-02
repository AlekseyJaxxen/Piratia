# 🍄 Настройка анимации атаки для монстра-гриба

## 📋 Что у вас есть:
- **Модель монстра**: Гриб
- **Компонент**: `Animation` (не-гуманоидный монстр)
- **Анимация**: `Mushroom_attack` с ID 0

## 🎯 Цель:
Воспроизводить анимацию `Mushroom_attack` при атаке basic skill

## ⚙️ Способы настройки:

### 🔧 **Способ 1: Автоматический (рекомендуется)**

Система автоматически найдет и воспроизведет анимацию `Mushroom_attack` по следующему алгоритму:

1. **Поиск по паттерну**: `mushroom_attack`, `Mushroom_attack`
2. **Fallback на стандартные**: `attack`, `Attack`
3. **Последний fallback**: Первая доступная анимация (ID 0)

**Что делать**: Ничего! Система автоматически найдет `Mushroom_attack`.

### 🎛️ **Способ 2: Ручная настройка (точный контроль)**

В ScriptableObject `MonsterBasicAttackSkill` для вашего гриба:

#### **Вариант A: По имени**
```
Animation Settings:
├── Custom Attack Animation Name: "Mushroom_attack"
└── Custom Attack Animation Id: -1
```

#### **Вариант B: По ID (быстрее)**
```
Animation Settings:
├── Custom Attack Animation Name: ""
└── Custom Attack Animation Id: 0
```

## 🔍 **Проверка работы:**

### Логи при инициализации:
```
[Monster] Found MeshRenderer on MushroomMonster (non-humanoid)
[Monster] Found Animation component on MushroomMonster
[Monster] Initialized animation cache for non-humanoid MushroomMonster: 1 animations
[Monster] Available animations for MushroomMonster: 0:Mushroom_attack
```

### Логи при атаке:
```
[MonsterBasicAttackSkill] Playing non-humanoid attack animation 'Mushroom_attack' for MushroomMonster
```

Или (если используете ID):
```
[MonsterBasicAttackSkill] Playing custom animation by ID 0: 'Mushroom_attack' for MushroomMonster
```

## 🚀 **Результат:**

Когда ваш гриб атакует игрока:
1. **MonsterAI2** вызывает `basicAttackSkill.Execute()`
2. **MonsterBasicAttackSkill** воспроизводит анимацию `Mushroom_attack`
3. **Monster** наносит урон цели
4. **Анимация** проигрывается синхронно на всех клиентах

## 🐛 **Отладка:**

Если анимация не проигрывается:

1. **Проверьте логи инициализации** - должна быть найдена анимация
2. **Проверьте имя анимации** - должно точно совпадать с `Mushroom_attack`
3. **Проверьте Animation компонент** - анимация должна быть добавлена
4. **Проверьте MonsterInfo** - должен быть назначен правильный `basicAttackSkill`

## 📝 **Пример настройки:**

```csharp
// В коде (для отладки):
Monster mushroom = GetComponent<Monster>();
Debug.Log($"Animation count: {mushroom.GetAnimationCount()}");
Debug.Log($"Animation 0: {mushroom.GetAnimationName(0)}");
Debug.Log($"Has Mushroom_attack: {mushroom.HasAnimation("Mushroom_attack")}");

// Ручное воспроизведение:
mushroom.PlayAnimation("Mushroom_attack");
// или
mushroom.PlayAnimationById(0);
```

**Готово! Ваш гриб теперь будет воспроизводить анимацию `Mushroom_attack` при каждой атаке!** 🍄⚔️
