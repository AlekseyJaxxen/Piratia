# 🎯 Простое умное движение: Подойти как можно ближе

## ✅ **Обновленная логика**

**Теперь игрок просто подходит как можно ближе к цели, и если не может атаковать/кастовать - завершает действие.**

## 🔧 **Как это работает**

### 🎯 **Логика атаки:**

#### **1️⃣ Проверка достижимости:**
```csharp
// Проверяем, можем ли мы достичь цель
Vector3 closestPoint;
float distanceToTarget;
bool canReach = _core.Movement.CanReachTarget(target.transform.position, out closestPoint, out distanceToTarget);

if (!canReach)
{
    Debug.LogWarning($"[PlayerActionSystem] Target {target.name} is not reachable (closest distance: {distanceToTarget:F1}m)");
    CompleteAction(); // ❌ Цель недостижима - завершаем
    yield break;
}
```

#### **2️⃣ Движение к ближайшей точке:**
```csharp
if (distanceToActualTarget > attackRange) // Стандартная дальность атаки
{
    // Начинаем движение к ближайшей доступной точке
    if (!isMovingToTarget)
    {
        if (_core.Movement.MoveToTarget(target.transform.position, out actualDestination))
        {
            isMovingToTarget = true;
            Debug.Log($"[PlayerActionSystem] Moving to closest reachable point: {actualDestination}");
        }
        else
        {
            CompleteAction(); // ❌ Не удалось начать движение
            yield break;
        }
    }
}
```

#### **3️⃣ Проверка после достижения ближайшей точки:**
```csharp
// Проверяем, достигли ли мы ближайшей точки
if (Vector3.Distance(transform.position, actualDestination) <= _core.Movement.Agent.stoppingDistance + 0.5f)
{
    // Мы дошли как можно ближе, но цель все еще далеко
    if (distanceToActualTarget > attackRange)
    {
        Debug.Log($"[PlayerActionSystem] Reached closest point but target still out of attack range ({distanceToActualTarget:F1}m > {attackRange:F1}m). Stopping attack.");
        CompleteAction(); // ❌ Не можем атаковать - завершаем
        yield break;
    }
}
```

#### **4️⃣ Успешная атака:**
```csharp
else
{
    isMovingToTarget = false;
    _core.Movement.StopMovement();
    _core.Movement.RotateTo(target.transform.position - transform.position);
    Debug.Log($"[PlayerActionSystem] Target in range. Stopping to attack. Distance: {distanceToActualTarget:F1}m");
    
    // ✅ Выполняем атаку
    _core.Skills.CmdExecuteSkill(_core, null, target.GetComponent<NetworkIdentity>().netId, skill.SkillName, ((SkillBase)skill).Weight);
}
```

### 🎭 **Аналогичная логика для каста заклинаний:**

```csharp
// Используем стандартную дальность каста без увеличения
float effectiveRange = skillToCast.Range - castRangeOffset;

// Если дошли как можно ближе, но все еще далеко:
if (distanceToActualTarget > effectiveRange)
{
    Debug.Log($"[PlayerActionSystem] Reached closest point but cast target still out of range ({distanceToActualTarget:F1}m > {effectiveRange:F1}m). Stopping cast.");
    _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
    CompleteAction(); // ❌ Не можем кастовать - завершаем
    yield break;
}
```

## 📊 **Сценарии использования**

### ✅ **Сценарий 1: Цель в пределах досягаемости**
```
[PlayerMovement] Moving to closest reachable point: (target position)
[PlayerActionSystem] Target in range. Stopping to attack. Distance: 2.3m
✅ Атака выполняется успешно
```

### ✅ **Сценарий 2: Цель за препятствием (достижима)**
```
[PlayerMovement] Moving to closest reachable point: (around obstacle)
[PlayerActionSystem] Target in range. Stopping to attack. Distance: 2.8m
✅ Игрок обходит препятствие и атакует
```

### ❌ **Сценарий 3: Цель на другом острове (недостижима)**
```
[PlayerMovement] Moving to closest reachable point: (edge of island)
[PlayerActionSystem] Reached closest point but target still out of attack range (15.2m > 3.0m). Stopping attack.
❌ Действие завершается - цель слишком далеко
```

### ❌ **Сценарий 4: Полностью недоступная цель**
```
[PlayerActionSystem] Target is not reachable (closest distance: 25.3m)
❌ Действие завершается сразу - цель недостижима
```

## 🎯 **Преимущества простой логики**

### ✅ **Предсказуемость:**
- **Четкие правила**: можешь атаковать - атакуешь, не можешь - не атакуешь
- **Нет "магии"** с увеличением дальности
- **Понятное поведение** для игрока

### ✅ **Честность:**
- **Соблюдение** заявленной дальности атаки/каста
- **Нет обмана** игрока относительно возможностей скиллов
- **Реалистичное поведение** персонажа

### ✅ **Производительность:**
- **Простая логика** без сложных вычислений
- **Быстрое завершение** при недостижимых целях
- **Меньше проверок** и условий

### ✅ **Отладка:**
- **Понятные сообщения** о причинах завершения
- **Четкие логи** расстояний и дальностей
- **Простая диагностика** проблем

## 🎮 **Поведение в игре**

### **Для игрока:**
1. **Кликает на цель** для атаки/каста
2. **Персонаж движется** к ближайшей доступной точке
3. **Если цель в радиусе действия** - выполняет действие
4. **Если цель слишком далеко** - останавливается и ничего не делает
5. **Получает понятную обратную связь** через логи/UI

### **Для разработчика:**
```
✅ Успешная атака:
[PlayerActionSystem] Target in range. Stopping to attack. Distance: 2.8m

❌ Неудачная попытка:
[PlayerActionSystem] Reached closest point but target still out of attack range (8.5m > 3.0m). Stopping attack.

❌ Недостижимая цель:
[PlayerActionSystem] Target is not reachable (closest distance: 20.1m)
```

## 🔧 **Настройки системы**

### **В `PlayerMovement.cs`:**
```csharp
// Максимальное расстояние для поиска ближайшей точки
public Vector3 GetClosestReachablePoint(Vector3 targetPosition, float maxSearchDistance = 10f)

// Максимальное расстояние до цели для считания её "достижимой"
return distanceToTarget <= 15f; // 15 метров
```

### **В `PlayerActionSystem.cs`:**
```csharp
// Дополнительное расстояние для определения "достижения" ближайшей точки
if (Vector3.Distance(transform.position, actualDestination) <= _core.Movement.Agent.stoppingDistance + 0.5f)
```

## 🎉 **Результат**

**Теперь система работает просто и честно:**
- ✅ **Подходит как можно ближе** к цели
- ✅ **Атакует/кастует**, если цель в радиусе действия
- ✅ **Завершает действие**, если цель недостижима
- ✅ **Никаких "магических" увеличений** дальности
- ✅ **Предсказуемое поведение** для игрока

**Игрок всегда знает, что если его персонаж не атакует - значит цель действительно слишком далеко!** 🎯
