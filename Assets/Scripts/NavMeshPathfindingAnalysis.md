# 🚨 Проблема: Скилл на недоступную цель без NavMesh пути

## 🎯 **Сценарий проблемы**

**Что происходит сейчас:**
1. Игрок кликает на цель на другом острове (без NavMesh пути)
2. Система вызывает `_core.Movement.MoveTo(target.transform.position)`
3. NavMeshAgent не может найти путь
4. **Игрок застревает в бесконечном цикле!**

## 🔍 **Анализ текущего кода**

### ❌ **AttackAction() - ПРОБЛЕМА:**
```csharp
while (target != null && targetHealth.CurrentHealth > 0)  // ❌ БЕСКОНЕЧНЫЙ ЦИКЛ!
{
    float distance = Vector3.Distance(transform.position, target.transform.position);
    
    if (distance > attackRange)
    {
        // ❌ ПРОБЛЕМА: Нет проверки успешности пути!
        _core.Movement.MoveTo(target.transform.position);
        Debug.Log($"[PlayerActionSystem] Moving to target {target.name} at distance {distance}");
    }
    
    yield return null;  // ❌ Бесконечное повторение!
}
```

### ❌ **CastSkillAction() - ТА ЖЕ ПРОБЛЕМА:**
```csharp
while (true)  // ❌ БЕСКОНЕЧНЫЙ ЦИКЛ!
{
    float distance = Vector3.Distance(transform.position, targetObject.transform.position);
    
    if (distance > effectiveRange)
    {
        // ❌ ПРОБЛЕМА: Нет проверки успешности пути!
        _core.Movement.MoveTo(targetObject.transform.position);
        Debug.Log($"[PlayerActionSystem] Moving to cast target {targetObject.name} at distance {distance}");
    }
    
    yield return null;  // ❌ Бесконечное повторение!
}
```

### ❌ **MoveTo() - НЕТ ОБРАТНОЙ СВЯЗИ:**
```csharp
public void MoveTo(Vector3 destination)
{
    NavMeshHit hit;
    if (NavMesh.SamplePosition(destination, out hit, 1f, NavMesh.AllAreas))
    {
        destination = hit.position;  // ✅ Корректирует позицию
    }
    
    _agent.isStopped = false;
    _agent.SetDestination(destination);  // ❌ НЕТ ПРОВЕРКИ УСПЕШНОСТИ!
    
    // ❌ НЕТ ВОЗВРАТА bool - успешен ли путь!
}
```

## 🚨 **Что произойдет:**

### **Сценарий 1: Цель на другом острове**
1. `MoveTo()` вызывается с координатами цели
2. `NavMesh.SamplePosition()` находит ближайшую точку NavMesh (на текущем острове)
3. `SetDestination()` устанавливает путь к краю текущего острова
4. Игрок доходит до края острова и останавливается
5. `distance > attackRange` все еще `true` (цель далеко)
6. **БЕСКОНЕЧНЫЙ ЦИКЛ**: каждый кадр вызывается `MoveTo()` снова!

### **Сценарий 2: Цель в недоступном месте**
1. `NavMesh.SamplePosition()` не находит NavMesh рядом с целью
2. `SetDestination()` получает исходную позицию цели (вне NavMesh)
3. `NavMeshAgent.pathStatus` становится `NavMeshPathStatus.PathPartial` или `PathInvalid`
4. Игрок не двигается или двигается к ближайшей доступной точке
5. **БЕСКОНЕЧНЫЙ ЦИКЛ**: расстояние не уменьшается!

### **Сценарий 3: Препятствие между игроком и целью**
1. NavMesh путь существует, но очень длинный (обход препятствий)
2. Игрок начинает движение по длинному пути
3. Пока игрок идет, `distance > attackRange` остается `true`
4. **Каждый кадр** вызывается новый `MoveTo()` - путь пересчитывается!
5. Производительность падает, движение становится "дерганым"

## 🛠️ **Необходимые исправления**

### 🔧 **Исправление 1: Добавить проверку pathStatus**

```csharp
// В AttackAction() и CastSkillAction()
if (distance > attackRange)
{
    _core.Movement.MoveTo(target.transform.position);
    
    // Ждем расчета пути
    yield return new WaitUntil(() => !_core.Movement.Agent.pathPending);
    
    // Проверяем успешность пути
    if (_core.Movement.Agent.pathStatus != NavMeshPathStatus.PathComplete)
    {
        Debug.LogWarning($"[PlayerActionSystem] Cannot reach target {target.name}. Path status: {_core.Movement.Agent.pathStatus}");
        CompleteAction();
        yield break;
    }
}
```

### 🔧 **Исправление 2: Добавить timeout**

```csharp
private IEnumerator AttackAction(GameObject target, ISkill skill = null)
{
    float startTime = Time.time;
    const float maxPathfindingTime = 10f;  // 10 секунд максимум
    
    while (target != null && targetHealth.CurrentHealth > 0)
    {
        // Проверка timeout
        if (Time.time - startTime > maxPathfindingTime)
        {
            Debug.LogWarning($"[PlayerActionSystem] Attack timeout: cannot reach {target.name} within {maxPathfindingTime}s");
            CompleteAction();
            yield break;
        }
        
        // ... остальная логика ...
    }
}
```

### 🔧 **Исправление 3: Улучшить MoveTo() с возвратом результата**

```csharp
public bool MoveTo(Vector3 destination)
{
    if (_agent == null) return false;
    
    NavMeshHit hit;
    if (NavMesh.SamplePosition(destination, out hit, 1f, NavMesh.AllAreas))
    {
        destination = hit.position;
    }
    else
    {
        Debug.LogWarning($"[PlayerMovement] No NavMesh found near destination {destination}");
        return false;  // ❌ Нет NavMesh
    }
    
    _agent.isStopped = false;
    bool pathSet = _agent.SetDestination(destination);
    
    if (!pathSet)
    {
        Debug.LogWarning($"[PlayerMovement] Failed to set destination {destination}");
        return false;  // ❌ Не удалось установить путь
    }
    
    return true;  // ✅ Путь установлен успешно
}
```

### 🔧 **Исправление 4: Добавить проверку достижимости цели**

```csharp
private bool IsTargetReachable(Vector3 targetPosition, float maxDistance)
{
    NavMeshPath path = new NavMeshPath();
    bool hasPath = NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);
    
    if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
    {
        return false;
    }
    
    // Проверяем длину пути (не слишком ли длинный обход)
    float pathLength = 0f;
    for (int i = 1; i < path.corners.Length; i++)
    {
        pathLength += Vector3.Distance(path.corners[i-1], path.corners[i]);
    }
    
    return pathLength <= maxDistance * 2f;  // Максимум в 2 раза длиннее прямого расстояния
}
```

## 🎯 **Рекомендуемое решение**

### **Полное исправление AttackAction():**

```csharp
private IEnumerator AttackAction(GameObject target, ISkill skill = null)
{
    // ... инициализация ...
    
    float startTime = Time.time;
    const float maxPathfindingTime = 10f;
    bool isMovingToTarget = false;
    
    while (target != null && targetHealth.CurrentHealth > 0)
    {
        // Timeout проверка
        if (Time.time - startTime > maxPathfindingTime)
        {
            Debug.LogWarning($"[PlayerActionSystem] Attack timeout: cannot reach {target.name}");
            CompleteAction();
            yield break;
        }
        
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        if (distance > attackRange)
        {
            // Начинаем движение только один раз
            if (!isMovingToTarget)
            {
                // Проверяем достижимость цели
                if (!IsTargetReachable(target.transform.position, attackRange * 3f))
                {
                    Debug.LogWarning($"[PlayerActionSystem] Target {target.name} is not reachable");
                    CompleteAction();
                    yield break;
                }
                
                if (!_core.Movement.MoveTo(target.transform.position))
                {
                    Debug.LogWarning($"[PlayerActionSystem] Failed to set path to {target.name}");
                    CompleteAction();
                    yield break;
                }
                
                isMovingToTarget = true;
            }
            
            // Проверяем статус пути
            if (!_core.Movement.Agent.pathPending)
            {
                if (_core.Movement.Agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    Debug.LogWarning($"[PlayerActionSystem] Path to {target.name} failed: {_core.Movement.Agent.pathStatus}");
                    CompleteAction();
                    yield break;
                }
            }
        }
        else
        {
            isMovingToTarget = false;
            // ... логика атаки ...
        }
        
        yield return new WaitForSeconds(0.1f);  // Проверяем реже
    }
}
```

## 🎉 **Ожидаемый результат**

После исправлений:
- ✅ **Нет бесконечных циклов** при недоступных целях
- ✅ **Корректное сообщение** игроку о недоступности цели  
- ✅ **Timeout защита** от зависания системы
- ✅ **Оптимизированная производительность** (меньше вызовов NavMesh)
- ✅ **Плавное движение** без постоянных пересчетов пути
