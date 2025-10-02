# 🎯 Различия в движении игрока: Обычное vs Атака

## 🔍 **Проблема**

**Игрок двигается по-разному когда:**
1. **Обычное движение**: Клик левой кнопкой по terrain → плавное движение
2. **Движение к цели**: Есть цель для атаки → прерывистое, "дерганое" движение

## 📋 **Анализ кода**

### 🟢 **Обычное движение (клик по terrain)**

#### **В `PlayerMovement.cs`:**
```csharp
// Клик по Ground
else if (hit.collider.CompareTag("Ground"))
{
    _core.Combat.ClearTarget();                    // ✅ Очищаем цель
    _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);  // ✅ Обычное движение
}
```

#### **В `PlayerActionSystem.cs` → `MoveAction()`:**
```csharp
private IEnumerator MoveAction(Vector3 destination)
{
    _core.Movement.MoveTo(destination);            // ✅ Одна команда движения
    
    while (_core.Movement.Agent.pathPending || 
           _core.Movement.Agent.remainingDistance > _core.Movement.stoppingDistance)
    {
        yield return null;                         // ✅ Ждем завершения
    }
    
    CompleteAction();                              // ✅ Завершаем
}
```

#### **В `PlayerMovement.cs` → `MoveTo()`:**
```csharp
public void MoveTo(Vector3 destination)
{
    _agent.isStopped = false;
    _agent.SetDestination(destination);            // ✅ Прямой путь к цели
}
```

### 🔴 **Движение к цели атаки**

#### **В `PlayerMovement.cs`:**
```csharp
// Клик по Enemy
else if (hit.collider.CompareTag("Enemy"))
{
    _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);  // ❌ Атака
}
```

#### **В `PlayerActionSystem.cs` → `AttackAction()`:**
```csharp
while (target != null && targetHealth.CurrentHealth > 0)  // ❌ Бесконечный цикл!
{
    float distance = Vector3.Distance(transform.position, target.transform.position);
    
    if (distance > attackRange)
    {
        // ❌ ПРОБЛЕМА: Пошаговое движение!
        Vector3 direction = (target.transform.position - transform.position).normalized;
        Vector3 tempPos = transform.position + direction * 1f;  // ❌ Шаг 1 метр
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(tempPos, out hit, 1f, NavMesh.AllAreas))
        {
            _core.Movement.MoveTo(hit.position);               // ❌ Движение по 1 метру!
        }
    }
    
    yield return null;  // ❌ Каждый кадр новая команда движения!
}
```

## 🚨 **Основные проблемы**

### ❌ **Проблема 1: Пошаговое движение**
```csharp
// Вместо прямого пути к цели:
_agent.SetDestination(target.transform.position);

// Используется пошаговое движение:
Vector3 tempPos = transform.position + direction * 1f;  // Шаг 1 метр
_core.Movement.MoveTo(hit.position);
```

### ❌ **Проблема 2: Постоянные команды движения**
```csharp
while (target != null)  // Каждый кадр
{
    _core.Movement.MoveTo(hit.position);  // Новая команда движения!
    yield return null;                    // Следующий кадр
}
```

### ❌ **Проблема 3: Нет учета текущего пути**
- Обычное движение: ждет завершения пути
- Движение к цели: прерывает путь каждый кадр

## 🛠️ **Решения**

### 🔧 **Решение 1: Использовать прямое движение к цели**

```csharp
// В AttackAction() ЗАМЕНИТЬ:
Vector3 direction = (target.transform.position - transform.position).normalized;
Vector3 tempPos = transform.position + direction * 1f;
_core.Movement.MoveTo(hit.position);

// НА:
_core.Movement.MoveTo(target.transform.position);  // Прямой путь!
```

### 🔧 **Решение 2: Добавить проверку текущего пути**

```csharp
if (distance > attackRange)
{
    // Проверяем, не движемся ли мы уже к цели
    if (!_core.Movement.Agent.pathPending && 
        _core.Movement.Agent.remainingDistance <= _core.Movement.Agent.stoppingDistance)
    {
        // Только тогда задаем новый путь
        _core.Movement.MoveTo(target.transform.position);
    }
}
```

### 🔧 **Решение 3: Унифицировать логику движения**

```csharp
private IEnumerator AttackAction(GameObject target, ISkill skill = null)
{
    // ... инициализация ...
    
    // Сначала двигаемся к цели (как обычное движение)
    _core.Movement.MoveTo(target.transform.position);
    
    // Ждем приближения к цели
    while (Vector3.Distance(transform.position, target.transform.position) > attackRange)
    {
        // Обновляем путь только если цель сильно сместилась
        if (Vector3.Distance(_core.Movement.Agent.destination, target.transform.position) > 2f)
        {
            _core.Movement.MoveTo(target.transform.position);
        }
        
        yield return new WaitForSeconds(0.1f);  // Проверяем реже
    }
    
    // Теперь атакуем
    // ... логика атаки ...
}
```

### 🔧 **Решение 4: Добавить параметр качества движения**

```csharp
public void MoveTo(Vector3 destination, bool smoothMovement = true)
{
    if (smoothMovement)
    {
        // Обычное плавное движение
        _agent.SetDestination(destination);
    }
    else
    {
        // Пошаговое движение (для особых случаев)
        // ... текущая логика ...
    }
}
```

## 📊 **Сравнение поведения**

### ✅ **Обычное движение (клик по terrain)**:
- ✅ **Одна команда** `SetDestination()`
- ✅ **Плавный путь** по NavMesh
- ✅ **Ждет завершения** движения
- ✅ **Нет прерываний** пути

### ❌ **Движение к цели атаки**:
- ❌ **Множество команд** каждый кадр
- ❌ **Пошаговое движение** по 1 метру
- ❌ **Постоянные прерывания** пути
- ❌ **"Дерганое" поведение**

## 🎯 **Рекомендуемое исправление**

### **Изменить в `PlayerActionSystem.cs`:**

```csharp
private IEnumerator AttackAction(GameObject target, ISkill skill = null)
{
    // ... инициализация ...
    
    float attackRange = skill.Range;
    bool isMovingToTarget = false;
    
    while (target != null && targetHealth.CurrentHealth > 0)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        if (distance > attackRange)
        {
            // Начинаем движение к цели (только один раз)
            if (!isMovingToTarget)
            {
                _core.Movement.MoveTo(target.transform.position);
                isMovingToTarget = true;
            }
            
            // Обновляем путь только если цель сильно сместилась
            float destinationDistance = Vector3.Distance(_core.Movement.Agent.destination, target.transform.position);
            if (destinationDistance > 3f)  // Цель сместилась больше чем на 3 метра
            {
                _core.Movement.MoveTo(target.transform.position);
            }
        }
        else
        {
            // Достигли цели
            isMovingToTarget = false;
            _core.Movement.StopMovement();
            
            // ... логика атаки ...
        }
        
        yield return new WaitForSeconds(0.1f);  // Проверяем реже, не каждый кадр
    }
}
```

## 🎉 **Ожидаемый результат**

После исправления:
- ✅ **Плавное движение** к цели атаки
- ✅ **Одинаковое поведение** для обычного движения и движения к цели
- ✅ **Нет "дерганого" движения**
- ✅ **Оптимизированная производительность** (меньше команд NavMesh)
