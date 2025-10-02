# 🎬 Комбинирование Animation с DoTween для монстров

## ✅ **Да, можно комбинировать!**

**Вы можете использовать DoTween анимации на объекте с MeshRenderer, даже если родительский объект использует Animation компонент.**

## 🏗️ **Структура вашего монстра**

```
MonsterPrefab (корневой объект)
├── 🧠 Monster Script
├── 🎬 Animation Component (для основных анимаций)
└── 📦 Model (префаб)
    ├── 🎨 MeshRenderer (child) ← ЗДЕСЬ DoTween
    └── 🎭 Animation Clips (Mushroom_attack, Mushroom_idle, etc.)
```

## 🎯 **Как это работает**

### **Animation компонент:**
- Управляет **основными анимациями** (атака, покой, движение)
- Работает с **Transform** компонентами (позиция, поворот, масштаб)
- Анимирует **весь объект** или его части

### **DoTween:**
- Управляет **дополнительными эффектами** 
- Может анимировать **любые свойства** (цвет, прозрачность, UI, etc.)
- Работает **независимо** от Animation компонента

## 🔧 **Практическая реализация**

### **1️⃣ Создайте компонент для DoTween эффектов:**

```csharp
using UnityEngine;
using DG.Tweening;
using Mirror;

public class MonsterDoTweenEffects : NetworkBehaviour
{
    [Header("References")]
    private Monster _monster;
    private MeshRenderer _meshRenderer;
    private Material _originalMaterial;
    private Color _originalColor;
    
    [Header("Effect Settings")]
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 0.2f;
    
    private Sequence _damageSequence;
    private Tweener _shakeTween;
    
    private void Awake()
    {
        _monster = GetComponentInParent<Monster>();
        
        // Находим MeshRenderer в child объектах
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        
        if (_meshRenderer != null)
        {
            _originalMaterial = _meshRenderer.material;
            _originalColor = _originalMaterial.color;
            
            Debug.Log($"[MonsterDoTweenEffects] Found MeshRenderer on {_monster.monsterName}");
        }
        else
        {
            Debug.LogWarning($"[MonsterDoTweenEffects] No MeshRenderer found for {_monster.monsterName}");
        }
    }
    
    /// <summary>
    /// Эффект вспышки при получении урона
    /// </summary>
    public void PlayDamageFlash()
    {
        if (_meshRenderer == null) return;
        
        // Останавливаем предыдущую анимацию
        _damageSequence?.Kill();
        
        _damageSequence = DOTween.Sequence();
        _damageSequence.Append(_meshRenderer.material.DOColor(damageFlashColor, damageFlashDuration * 0.5f));
        _damageSequence.Append(_meshRenderer.material.DOColor(_originalColor, damageFlashDuration * 0.5f));
        _damageSequence.SetAutoKill(true);
        
        Debug.Log($"[MonsterDoTweenEffects] Playing damage flash on {_monster.monsterName}");
    }
    
    /// <summary>
    /// Эффект тряски при получении урона
    /// </summary>
    public void PlayDamageShake()
    {
        if (transform == null) return;
        
        // Останавливаем предыдущую тряску
        _shakeTween?.Kill();
        
        Vector3 originalPosition = transform.localPosition;
        _shakeTween = transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90f, false, true)
            .OnComplete(() => transform.localPosition = originalPosition);
        
        Debug.Log($"[MonsterDoTweenEffects] Playing damage shake on {_monster.monsterName}");
    }
    
    /// <summary>
    /// Эффект появления монстра
    /// </summary>
    public void PlaySpawnEffect()
    {
        if (transform == null) return;
        
        // Начинаем с нулевого масштаба
        transform.localScale = Vector3.zero;
        
        // Плавно увеличиваем до нормального размера
        transform.DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBounce);
        
        Debug.Log($"[MonsterDoTweenEffects] Playing spawn effect on {_monster.monsterName}");
    }
    
    /// <summary>
    /// Эффект смерти монстра
    /// </summary>
    public void PlayDeathEffect()
    {
        if (transform == null || _meshRenderer == null) return;
        
        Sequence deathSequence = DOTween.Sequence();
        
        // Тряска
        deathSequence.Append(transform.DOShakePosition(0.3f, 0.3f, 10, 90f, false, true));
        
        // Исчезновение
        deathSequence.Append(_meshRenderer.material.DOFade(0f, 0.5f));
        deathSequence.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
        
        deathSequence.SetAutoKill(true);
        
        Debug.Log($"[MonsterDoTweenEffects] Playing death effect on {_monster.monsterName}");
    }
    
    /// <summary>
    /// Эффект свечения при особых атаках
    /// </summary>
    public void PlayGlowEffect(Color glowColor, float duration = 1f)
    {
        if (_meshRenderer == null) return;
        
        Sequence glowSequence = DOTween.Sequence();
        glowSequence.Append(_meshRenderer.material.DOColor(glowColor, duration * 0.3f));
        glowSequence.Append(_meshRenderer.material.DOColor(_originalColor, duration * 0.7f));
        glowSequence.SetAutoKill(true);
        
        Debug.Log($"[MonsterDoTweenEffects] Playing glow effect on {_monster.monsterName}");
    }
    
    private void OnDestroy()
    {
        // Очищаем все DoTween анимации
        _damageSequence?.Kill();
        _shakeTween?.Kill();
        DOTween.Kill(transform);
        DOTween.Kill(_meshRenderer);
    }
}
```

### **2️⃣ Интегрируйте с системой монстров:**

```csharp
// В Monster.cs добавьте:
public class Monster : NetworkBehaviour
{
    // ... существующий код ...
    
    private MonsterDoTweenEffects _doTweenEffects;
    
    private void Awake()
    {
        // ... существующий код ...
        
        // Находим компонент DoTween эффектов
        _doTweenEffects = GetComponentInChildren<MonsterDoTweenEffects>();
        if (_doTweenEffects == null)
        {
            Debug.LogWarning($"[Monster] No MonsterDoTweenEffects found for {monsterName}");
        }
    }
    
    /// <summary>
    /// Вызывается при получении урона
    /// </summary>
    public void OnDamageTaken()
    {
        // Основная анимация через Animation компонент
        if (HasUniversalAnimation(UniversalAnimationId.Hit))
        {
            PlayUniversalAnimation(UniversalAnimationId.Hit);
        }
        
        // Дополнительные DoTween эффекты
        if (_doTweenEffects != null)
        {
            _doTweenEffects.PlayDamageFlash();
            _doTweenEffects.PlayDamageShake();
        }
    }
    
    /// <summary>
    /// Вызывается при смерти монстра
    /// </summary>
    public void OnDeath()
    {
        // Основная анимация смерти
        if (HasUniversalAnimation(UniversalAnimationId.Death))
        {
            PlayUniversalAnimation(UniversalAnimationId.Death);
        }
        
        // Дополнительные DoTween эффекты
        if (_doTweenEffects != null)
        {
            _doTweenEffects.PlayDeathEffect();
        }
    }
    
    /// <summary>
    /// Вызывается при появлении монстра
    /// </summary>
    public void OnSpawn()
    {
        // Основная анимация появления
        if (HasUniversalAnimation(UniversalAnimationId.Spawn))
        {
            PlayUniversalAnimation(UniversalAnimationId.Spawn);
        }
        
        // Дополнительные DoTween эффекты
        if (_doTweenEffects != null)
        {
            _doTweenEffects.PlaySpawnEffect();
        }
    }
}
```

### **3️⃣ Интегрируйте с системой здоровья:**

```csharp
// В Health.cs или HealthMonster.cs:
public void TakeDamage(int damage)
{
    // ... существующий код нанесения урона ...
    
    // Получаем Monster компонент
    Monster monster = GetComponent<Monster>();
    if (monster != null)
    {
        monster.OnDamageTaken(); // Запускаем эффекты урона
    }
    
    // Проверяем смерть
    if (CurrentHealth <= 0)
    {
        if (monster != null)
        {
            monster.OnDeath(); // Запускаем эффекты смерти
        }
    }
}
```

## 🎭 **Примеры эффектов**

### **✅ Что можно анимировать DoTween:**

#### **🎨 Материалы и цвета:**
```csharp
// Смена цвета при получении урона
_meshRenderer.material.DOColor(Color.red, 0.2f);

// Прозрачность при смерти
_meshRenderer.material.DOFade(0f, 1f);

// Свечение при особой атаке
_meshRenderer.material.DOColor(Color.yellow, 0.5f);
```

#### **📐 Transform анимации:**
```csharp
// Тряска при уроне
transform.DOShakePosition(0.3f, 0.2f);

// Масштабирование при появлении
transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce);

// Вращение вокруг оси
transform.DORotate(new Vector3(0, 360, 0), 2f, RotateMode.FastBeyond360);
```

#### **🌟 Специальные эффекты:**
```csharp
// Пульсация
transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);

// Левитация
transform.DOMoveY(transform.position.y + 0.5f, 1f).SetLoops(-1, LoopType.Yoyo);

// Мерцание
_meshRenderer.material.DOFade(0.5f, 0.3f).SetLoops(-1, LoopType.Yoyo);
```

## ⚠️ **Важные моменты**

### **🔄 Совместимость систем:**

#### **✅ НЕ конфликтуют:**
- **Animation** анимирует Transform (позиция, поворот, масштаб основного объекта)
- **DoTween** анимирует материалы, цвета, UI, или другие Transform

#### **❌ МОГУТ конфликтовать:**
- Если Animation и DoTween анимируют **одни и те же свойства** одновременно
- Например: Animation двигает объект, а DoTween тоже пытается его двигать

### **🛡️ Как избежать конфликтов:**

#### **1️⃣ Разделение ответственности:**
```csharp
// Animation компонент - основные анимации
- Mushroom_attack (движение всего монстра)
- Mushroom_idle (базовая поза)
- Mushroom_move (перемещение)

// DoTween - дополнительные эффекты
- Смена цвета материала
- Тряска при уроне
- Эффекты появления/исчезновения
```

#### **2️⃣ Использование разных объектов:**
```csharp
MonsterPrefab
├── Animation Component (анимирует весь объект)
└── Model
    ├── MeshRenderer ← DoTween анимирует ЭТОТ объект
    └── EffectPoint ← DoTween может анимировать отдельные части
```

#### **3️⃣ Временное разделение:**
```csharp
// Сначала Animation анимация
PlayUniversalAnimation(UniversalAnimationId.Attack);

// Затем DoTween эффекты
DOVirtual.DelayedCall(0.2f, () => {
    _doTweenEffects.PlayGlowEffect(Color.red);
});
```

## 🎯 **Рекомендуемая архитектура**

### **Для вашего гриба-монстра:**

```
MushroomMonsterPrefab
├── 🧠 Monster.cs
├── 🎬 Animation (Mushroom_attack, Mushroom_idle)
├── 🎭 MonsterDoTweenEffects.cs
└── 📦 MushroomModel
    ├── 🎨 MeshRenderer ← DoTween эффекты здесь
    └── 🍄 Mushroom mesh
```

### **Распределение анимаций:**
- **Animation**: Основные движения (атака, покой, движение)
- **DoTween**: Визуальные эффекты (вспышки, тряска, свечение)

## 🎉 **Результат**

**Теперь ваш монстр может:**
- ✅ Играть **основные анимации** через Animation компонент
- ✅ Одновременно показывать **визуальные эффекты** через DoTween
- ✅ **Не конфликтовать** между системами
- ✅ Иметь **богатую анимацию** с минимальными усилиями

**Пример в действии:**
1. Монстр атакует → Animation играет `Mushroom_attack`
2. Одновременно → DoTween делает красную вспышку материала
3. При получении урона → DoTween трясет модель + меняет цвет
4. При смерти → Animation играет `Mushroom_death` + DoTween исчезновение

**Это создает более живых и выразительных монстров!** 🎭
