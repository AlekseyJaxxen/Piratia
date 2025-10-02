# 🎭 Система анимаций монстров

## 📋 Обзор системы

Система поддерживает **два типа монстров**:

### 🧙‍♂️ **Гуманоидные монстры**
- **Рендерер**: `SkinnedMeshRenderer`
- **Анимации**: `Animator` + `AnimatorController`
- **Примеры**: Люди, эльфы, орки, скелеты

### 🐉 **Не-гуманоидные монстры**
- **Рендерер**: `MeshRenderer`
- **Анимации**: `Animation` + `AnimationClip[]`
- **Примеры**: Драконы, слизни, кристаллы, механизмы

## 🔧 Настройка монстров

### Гуманоидный монстр (SkinnedMeshRenderer + Animator)

```
MonsterPrefab/
├── Monster.cs
├── SkinnedMeshRenderer (на дочернем объекте)
├── Animator (на дочернем объекте)
└── AnimatorController (назначен в Animator)
    ├── States: "Idle", "Walk", "Attack", "Death"
    └── Transitions между состояниями
```

### Не-гуманоидный монстр (MeshRenderer + Animation)

```
MonsterPrefab/
├── Monster.cs
├── MeshRenderer (на дочернем объекте)
├── Animation (на дочернем объекте)
└── AnimationClips (назначены в Animation)
    ├── "Idle" - AnimationClip
    ├── "Move" - AnimationClip
    ├── "Attack" - AnimationClip
    └── "Death" - AnimationClip
```

## 🎮 API для работы с анимациями

### Основные методы

```csharp
// Воспроизведение анимации по имени (работает для обеих систем)
monster.PlayAnimation("Attack");

// Воспроизведение анимации по ID (НОВОЕ!)
monster.PlayAnimationById(2); // Играет анимацию с ID 2

// Проверка типа монстра
bool isHumanoid = monster.IsHumanoidMonster();
bool isNonHumanoid = monster.IsNonHumanoidMonster();

// Получение доступных анимаций
string[] animations = monster.GetAvailableAnimations();

// Работа с ID анимаций (НОВОЕ!)
int attackId = monster.GetAnimationId("Attack");        // Получить ID по имени
string animName = monster.GetAnimationName(2);          // Получить имя по ID
int totalAnims = monster.GetAnimationCount();           // Количество анимаций
Dictionary<string, int> idMap = monster.GetAnimationIdMap(); // Полный словарь

// Проверка наличия анимации
bool hasAttack = monster.HasAnimation("Attack");

// Остановка всех анимаций
monster.StopAllAnimations();
```

### Стандартные имена анимаций

| Анимация | Описание | Когда используется |
|----------|----------|-------------------|
| `"Idle"` | Состояние покоя | Когда монстр не двигается |
| `"Walk"` / `"Move"` | Движение | Во время перемещения |
| `"Attack"` | Атака | При выполнении атаки |
| `"Death"` | Смерть | При смерти монстра |
| `"Hit"` | Получение урона | При получении урона |
| `"Spawn"` | Появление | При спавне монстра |

## 🆔 Работа с ID анимаций

### Преимущества использования ID:
- **Производительность**: Быстрее чем поиск по строке
- **Сетевой трафик**: Меньше данных передается (int vs string)
- **Безопасность**: Защита от опечаток в именах

### Автоматическое назначение ID:
```csharp
// ID назначаются автоматически при инициализации:
// 0: "Idle"
// 1: "Walk" 
// 2: "Attack"
// 3: "Death"
```

### Примеры использования ID:

```csharp
// Получение информации об анимациях
Debug.Log($"Monster has {monster.GetAnimationCount()} animations");

// Получение ID конкретной анимации
int idleId = monster.GetAnimationId("Idle");
if (idleId != -1)
{
    monster.PlayAnimationById(idleId); // Быстрое воспроизведение
}

// Перебор всех анимаций
for (int i = 0; i < monster.GetAnimationCount(); i++)
{
    string animName = monster.GetAnimationName(i);
    Debug.Log($"Animation {i}: {animName}");
}

// Получение полного словаря
var animMap = monster.GetAnimationIdMap();
foreach (var kvp in animMap)
{
    Debug.Log($"{kvp.Key} -> ID {kvp.Value}");
}
```

### Сетевая синхронизация по ID:
```csharp
// На сервере
monster.RpcPlayAnimationById(2); // Отправляет только int вместо string

// Автоматически работает для combined монстров
```

## 🔄 Автоматическое определение типа

Система автоматически определяет тип монстра при инициализации:

```csharp
// В Monster.cs - LoadAndInitializeClient()
_skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
if (_skinnedRenderer == null)
{
    // Не-гуманоидный монстр
    _meshRenderer = model.GetComponentInChildren<MeshRenderer>();
    _animation = model.GetComponentInChildren<Animation>();
}
else
{
    // Гуманоидный монстр
    _animator = model.GetComponentInChildren<Animator>();
}
```

## 🌐 Сетевая синхронизация

Анимации автоматически синхронизируются по сети:

```csharp
// На сервере
monster.PlayAnimation("Attack"); // Отправляет RPC клиентам

// На клиенте
// Анимация воспроизводится автоматически через RpcPlayAnimation
```

## 🔗 Combined монстры

Для составных монстров (голова + ноги):
- **Legs** управляет анимациями **Head**
- Специальная анимация `"LegsKick"` для атак ног
- Автоматическая передача управления между частями

## 📝 Примеры использования

### Создание не-гуманоидного монстра

1. **Создайте префаб** с `MeshRenderer`
2. **Добавьте компонент `Animation`**
3. **Назначьте `AnimationClip`-ы** в компонент `Animation`
4. **Установите один клип как `Default`** (обычно "Idle")
5. **Настройте `MonsterInfo`** с этим префабом

### Добавление новой анимации

```csharp
// Для гуманоидов: добавьте состояние в AnimatorController
// Для не-гуманоидов: добавьте AnimationClip в компонент Animation

// Использование
monster.PlayAnimation("NewAnimation");
```

## ⚠️ Важные замечания

1. **Имена анимаций** должны точно совпадать в коде и в AnimatorController/Animation
2. **Animation компонент** должен содержать все необходимые клипы
3. **Default анимация** в Animation должна быть установлена (обычно "Idle")
4. **Animator Controller** должен содержать все состояния для гуманоидов

## 🐛 Отладка

Включите логирование для диагностики:

```csharp
// Логи автоматически выводятся при инициализации:
// "Found SkinnedMeshRenderer on MonsterName (humanoid)"
// "Found MeshRenderer on MonsterName (non-humanoid)"
// "Found Animator on MonsterName"
// "Found Animation component on MonsterName"
// "Initialized animation cache for humanoid MonsterName: 4 animations"
// "Available animations for MonsterName: 0:Idle, 1:Walk, 2:Attack, 3:Death"
```

Проверьте доступные анимации:

```csharp
// По именам (старый способ)
string[] anims = monster.GetAvailableAnimations();
Debug.Log($"Available animations: {string.Join(", ", anims)}");

// По ID (новый способ)
Debug.Log($"Total animations: {monster.GetAnimationCount()}");
for (int i = 0; i < monster.GetAnimationCount(); i++)
{
    Debug.Log($"ID {i}: {monster.GetAnimationName(i)}");
}

// Проверка конкретной анимации
int attackId = monster.GetAnimationId("Attack");
if (attackId != -1)
{
    Debug.Log($"Attack animation has ID: {attackId}");
    monster.PlayAnimationById(attackId);
}
else
{
    Debug.LogError("Attack animation not found!");
}
```
