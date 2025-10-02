# 🎯 Умное движение игрока: Подход как можно ближе к цели

## ✅ **Реализованное решение**

**Теперь игрок умно подходит как можно ближе к цели и пытается атаковать/кастовать с максимально возможного расстояния!**

## 🔧 **Новые возможности**

### 🎯 **1. Умный поиск ближайшей точки**

#### **В `PlayerMovement.cs`:**
```csharp
public Vector3 GetClosestReachablePoint(Vector3 targetPosition, float maxSearchDistance = 10f)
{
    // 1️⃣ Пробуем прямой путь
    NavMeshPath directPath = new NavMeshPath();
    if (NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, directPath))
    {
        if (directPath.status == NavMeshPathStatus.PathComplete)
        {
            return targetPosition; // ✅ Прямой путь доступен
        }
        
        // 2️⃣ Если путь частичный, берем последнюю доступную точку
        if (directPath.status == NavMeshPathStatus.PathPartial && directPath.corners.Length > 1)
        {
            return directPath.corners[directPath.corners.Length - 1]; // ✅ Максимально близко
        }
    }
    
    // 3️⃣ Ищем ближайшую точку на NavMesh
    NavMeshHit hit;
    if (NavMesh.SamplePosition(targetPosition, out hit, maxSearchDistance, NavMesh.AllAreas))
    {
        // Проверяем, можем ли мы дойти до этой точки
        NavMeshPath pathToClosest = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, pathToClosest))
        {
            if (pathToClosest.status == NavMeshPathStatus.PathComplete)
            {
                return hit.position; // ✅ Ближайшая доступная точка
            }
        }
    }
    
    return transform.position; // ❌ Никуда не можем дойти
}
```

### 🎯 **2. Проверка достижимости цели**

```csharp
public bool CanReachTarget(Vector3 targetPosition, out Vector3 closestPoint, out float distanceToTarget)
{
    closestPoint = GetClosestReachablePoint(targetPosition);
    distanceToTarget = Vector3.Distance(closestPoint, targetPosition);
    
    // Считаем цель достижимой, если можем подойти ближе чем на 15 метров
    return distanceToTarget <= 15f;
}
```

### 🎯 **3. Улучшенное движение к цели**

```csharp
public bool MoveToTarget(Vector3 targetPosition, out Vector3 actualDestination)
{
    // Находим ближайшую доступную точку
    actualDestination = GetClosestReachablePoint(targetPosition);
    
    // Если уже на месте, не двигаемся
    if (Vector3.Distance(transform.position, actualDestination) < 0.5f)
    {
        return true; // ✅ Уже на месте
    }
    
    _agent.isStopped = false;
    bool pathSet = _agent.SetDestination(actualDestination);
    
    Debug.Log($"[PlayerMovement] Moving to closest reachable point: {actualDestination} (target was: {targetPosition})");
    return pathSet;
}
```

## 🚀 **Умная логика атаки**

### **В `AttackAction()`:**

#### **1️⃣ Предварительная проверка:**
```csharp
// Проверяем, можем ли мы достичь цель
Vector3 closestPoint;
float distanceToTarget;
bool canReach = _core.Movement.CanReachTarget(target.transform.position, out closestPoint, out distanceToTarget);

if (!canReach)
{
    Debug.LogWarning($"[PlayerActionSystem] Target {target.name} is not reachable (closest distance: {distanceToTarget:F1}m)");
    CompleteAction(); // ❌ Цель недостижима
    yield break;
}
```

#### **2️⃣ Динамическое увеличение дальности:**
```csharp
// Определяем эффективную дальность атаки с учетом недоступности цели
float effectiveAttackRange = attackRange;
if (distanceToTarget > 0.5f) // Если цель не полностью доступна
{
    effectiveAttackRange = Mathf.Max(attackRange, distanceToTarget + 1f); // ✅ Увеличиваем дальность
    Debug.Log($"[PlayerActionSystem] Target not fully reachable. Extended attack range to {effectiveAttackRange:F1}m");
}
```

#### **3️⃣ Умное движение:**
```csharp
if (distanceToActualTarget > effectiveAttackRange)
{
    // Начинаем движение к ближайшей доступной точке
    if (!isMovingToTarget)
    {
        if (_core.Movement.MoveToTarget(target.transform.position, out actualDestination))
        {
            isMovingToTarget = true;
            Debug.Log($"[PlayerActionSystem] Moving to closest reachable point: {actualDestination}");
        }
    }
    
    // Проверяем, достигли ли мы ближайшей точки
    if (Vector3.Distance(transform.position, actualDestination) <= _core.Movement.Agent.stoppingDistance + 0.5f)
    {
        if (distanceToActualTarget > effectiveAttackRange)
        {
            Debug.Log($"[PlayerActionSystem] Reached closest point but target still out of range. Attempting attack anyway.");
            // ✅ Пытаемся атаковать с максимально возможного расстояния
        }
    }
}
```

## 🎭 **Умная логика каста заклинаний**

### **Аналогичная логика в `CastSkillAction()`:**

```csharp
// Определяем эффективную дальность каста с учетом недоступности цели
float effectiveRange = skillToCast.Range - castRangeOffset;
if (distanceToTarget > 0.5f) // Если цель не полностью доступна
{
    effectiveRange = Mathf.Max(effectiveRange, distanceToTarget + 1f); // ✅ Увеличиваем дальность каста
    Debug.Log($"[PlayerActionSystem] Cast target not fully reachable. Extended cast range to {effectiveRange:F1}m");
}
```

## 📊 **Сценарии использования**

### ✅ **Сценарий 1: Цель на другом острове**
1. **Проверка**: `CanReachTarget()` определяет ближайшую доступную точку на краю острова
2. **Движение**: Игрок идет к краю своего острова
3. **Увеличение дальности**: Система увеличивает `effectiveAttackRange` до расстояния до цели + 1м
4. **Атака**: Игрок атакует с максимально возможного расстояния

### ✅ **Сценарий 2: Цель за препятствием**
1. **Поиск пути**: `GetClosestReachablePoint()` находит путь в обход препятствия
2. **Движение**: Игрок идет по оптимальному пути к ближайшей точке
3. **Атака**: Как только цель в радиусе действия - атакует

### ✅ **Сценарий 3: Цель в недоступном месте**
1. **Проверка**: Если ближайшая точка дальше 15м от цели - отказ
2. **Сообщение**: "Target is not reachable"
3. **Завершение**: Действие прекращается без зависания

### ✅ **Сценарий 4: Прямой путь доступен**
1. **Оптимизация**: Система сразу определяет прямой путь
2. **Движение**: Обычное движение напрямую к цели
3. **Атака**: Стандартная логика атаки

## 🎯 **Преимущества нового решения**

### ✅ **Умность:**
- **Автоматический поиск** оптимального пути
- **Динамическое увеличение** дальности атаки/каста
- **Попытка атаки** с максимально возможного расстояния

### ✅ **Надежность:**
- **Нет бесконечных циклов** при недоступных целях
- **Корректная обработка** частичных путей NavMesh
- **Защита от зависания** системы

### ✅ **Производительность:**
- **Однократный расчет** пути вместо постоянных пересчетов
- **Оптимизированные проверки** NavMesh
- **Меньше вызовов** SetDestination()

### ✅ **Пользовательский опыт:**
- **Плавное движение** без "дерганий"
- **Логичное поведение** при недоступных целях
- **Информативные сообщения** в логах

## 🎮 **Как это работает в игре**

### **Для игрока:**
1. **Кликает на цель** (враг/союзник для каста)
2. **Система автоматически** находит лучший путь
3. **Игрок плавно движется** к ближайшей доступной точке
4. **Атакует/кастует** как только возможно
5. **Если цель недоступна** - получает понятное сообщение

### **Для разработчика:**
1. **Подробные логи** всех этапов процесса
2. **Четкие сообщения** о причинах неудач
3. **Визуальная отладка** через Debug.Log
4. **Настраиваемые параметры** (максимальное расстояние поиска, etc.)

## 🎉 **Результат**

**Теперь игрок может атаковать и кастовать заклинания на цели даже через препятствия, на других островах, или в труднодоступных местах - система автоматически найдет оптимальный путь и попытается выполнить действие с максимально возможного расстояния!** 🎯
