# 🎯 Универсальная система ID анимаций

## 📋 Концепция

**Проблема**: Разные монстры имеют разные названия анимаций:
- Гриб: `"Mushroom_attack"`
- Дракон: `"Dragon_bite"`  
- Скелет: `"Attack"`

**Решение**: Универсальные ID, где позиция анимации определяет её назначение:

```csharp
public enum UniversalAnimationId
{
    Attack = 0,     // ID 0 - всегда атака (любое название)
    Idle = 1,       // ID 1 - всегда покой
    Move = 2,       // ID 2 - всегда движение
    Death = 3,      // ID 3 - всегда смерть
    Hit = 4,        // ID 4 - всегда получение урона
    Spawn = 5       // ID 5 - всегда появление
}
```

## 🎮 Как это работает

### Для не-гуманоидных монстров (Animation):
```
Animation Component:
├── [0] Mushroom_attack  ← UniversalAnimationId.Attack
├── [1] Mushroom_idle    ← UniversalAnimationId.Idle
├── [2] Mushroom_move    ← UniversalAnimationId.Move
└── [3] Mushroom_death   ← UniversalAnimationId.Death
```

### Для гуманоидных монстров (Animator):
```
AnimatorController:
├── [0] Attack    ← UniversalAnimationId.Attack
├── [1] Idle      ← UniversalAnimationId.Idle  
├── [2] Walk      ← UniversalAnimationId.Move
└── [3] Death     ← UniversalAnimationId.Death
```

## ⚙️ Настройка MonsterBasicAttackSkill

### ✅ Рекомендуемая настройка (универсальная):
```
Animation Settings:
├── Use Universal Animation Ids: ✓ true
├── Custom Attack Animation Name: ""
└── Custom Attack Animation Id: -1
```

### 🔧 Альтернативная настройка (старая система):
```
Animation Settings:
├── Use Universal Animation Ids: ✗ false
├── Custom Attack Animation Name: "Attack"
└── Custom Attack Animation Id: -1
```

## 🎯 Преимущества

### ✅ **Универсальность**:
- Не нужно знать точные названия анимаций
- ID 0 всегда означает атаку для любого монстра

### ⚡ **Производительность**:
- Прямой доступ по индексу
- Нет поиска по строкам

### 🔄 **Совместимость**:
- Работает с Animator и Animation
- Старые монстры продолжают работать

### 🛠️ **Простота**:
- Один параметр `useUniversalAnimationIds = true`
- Автоматическое определение типа монстра

## 📝 Примеры использования

### В коде:
```csharp
// Универсальная система
monster.PlayUniversalAnimation(UniversalAnimationId.Attack);

// Проверка доступности
if (monster.HasUniversalAnimation(UniversalAnimationId.Attack))
{
    monster.PlayUniversalAnimation(UniversalAnimationId.Attack);
}

// Получение имени
string attackName = monster.GetUniversalAnimationName(UniversalAnimationId.Attack);
// Результат: "Mushroom_attack" для гриба, "Attack" для скелета
```

### Автоматически в MonsterBasicAttackSkill:
```csharp
// При атаке автоматически вызывается:
PlayUniversalAnimation(caster, UniversalAnimationId.Attack);
// Это играет ID 0, независимо от названия анимации
```

## 🐛 Отладка

### Логи при инициализации:
```
[Monster] Available animations for MushroomMonster: 0:Mushroom_attack, 1:Mushroom_idle
[Monster] Available animations for SkeletonMonster: 0:Attack, 1:Idle, 2:Walk, 3:Death
```

### Логи при атаке:
```
[MonsterBasicAttackSkill] Playing universal Attack animation (ID: 0): 'Mushroom_attack' for MushroomMonster
[MonsterBasicAttackSkill] Playing universal Attack animation (ID: 0): 'Attack' for SkeletonMonster
```

### Если ID недоступен:
```
[MonsterBasicAttackSkill] Universal Attack ID 0 not available for Monster (only 0 animations). Using first animation: 'OnlyAnimation' (ID: 0)
```

## 📋 Рекомендации по настройке анимаций

### Для новых монстров:
1. **Расположите анимации в правильном порядке**:
   - Позиция 0: Анимация атаки
   - Позиция 1: Анимация покоя
   - Позиция 2: Анимация движения
   - Позиция 3: Анимация смерти

2. **Включите универсальную систему**:
   - `Use Universal Animation Ids = true`

### Для существующих монстров:
1. **Если порядок анимаций правильный** - включите универсальную систему
2. **Если порядок неправильный** - оставьте старую систему или переупорядочите анимации

## 🎉 Результат

**Теперь все монстры используют единую систему ID, независимо от названий анимаций!**

- ✅ Гриб с `"Mushroom_attack"` → ID 0
- ✅ Дракон с `"Dragon_bite"` → ID 0  
- ✅ Скелет с `"Attack"` → ID 0
- ✅ Все играют атаку через `UniversalAnimationId.Attack`
