using UnityEngine;
using DG.Tweening;

/// <summary>
/// DoTween эффекты получения удара для не-гуманоидных монстров (с Animation компонентом)
/// </summary>
public class MonsterHitEffects : MonoBehaviour
{
    [Header("References")]
    private Monster _monster;
    private MeshRenderer _meshRenderer;
    private Material _originalMaterial;
    private Color _originalColor;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    
    [Header("Hit Effect Settings")]
    [SerializeField] private float hitFlashDuration = 0.3f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private AnimationCurve hitFlashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Shake Effect Settings")]
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float shakeStrength = 0.15f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;
    
    [Header("Scale Effect Settings")]
    [SerializeField] private float scaleHitMultiplier = 0.9f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Knockback Effect Settings")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float knockbackForce = 0.3f;
    [SerializeField] private float knockbackDuration = 0.2f;
    
    [Header("Death Animation Settings")]
    [SerializeField] private float deathFallDuration = 1.5f;
    [SerializeField] private Vector3 deathFallAngle = new Vector3(0, 0, 90f); // Поворот на бок
    [SerializeField] private Vector3 deathFallPositionOffset = new Vector3(0, -0.3f, 0); // Немного опускается
    [SerializeField] private float deathColorFadeDuration = 2f;
    [SerializeField] private Color deathTintColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    private Sequence _hitEffectSequence;
    private Sequence _deathEffectSequence;
    private Tweener _shakeTween;
    private bool _isPlayingHitEffect = false;
    private bool _isPlayingDeathAnimation = false;
    
    private void Awake()
    {
        InitializeComponents();
        CacheOriginalValues();
    }
    
    private void InitializeComponents()
    {
        // Получаем Monster компонент
        _monster = GetComponentInParent<Monster>();
        if (_monster == null)
        {
            Debug.LogError($"[MonsterHitEffects] No Monster component found in parent of {gameObject.name}");
            return;
        }
        
        // Ищем MeshRenderer в child объектах
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (_meshRenderer == null)
        {
            Debug.LogWarning($"[MonsterHitEffects] No MeshRenderer found for monster {_monster.monsterName}");
            return;
        }
        
        // Получаем материал
        if (_meshRenderer.material != null)
        {
            _originalMaterial = _meshRenderer.material;
            _originalColor = _originalMaterial.color;
        }
        
        Debug.Log($"[MonsterHitEffects] Initialized for monster: {_monster.monsterName}");
    }
    
    private void CacheOriginalValues()
    {
        _originalPosition = transform.localPosition;
        _originalScale = transform.localScale;
    }
    
    /// <summary>
    /// Основной метод для проигрывания эффекта получения удара
    /// Убираем RPC - будет вызываться через Monster.cs
    /// </summary>
    public void PlayHitEffect(Vector3 hitDirection)
    {
        if (_isPlayingHitEffect || _meshRenderer == null) return;
        
        PlayHitEffectLocal(hitDirection);
    }
    
    /// <summary>
    /// Локальное проигрывание эффекта удара
    /// </summary>
    public void PlayHitEffectLocal(Vector3 hitDirection)
    {
        if (_isPlayingHitEffect || _meshRenderer == null) return;
        
        _isPlayingHitEffect = true;
        
        // Останавливаем предыдущие эффекты
        StopAllEffects();
        
        // Создаем последовательность эффектов
        _hitEffectSequence = DOTween.Sequence();
        
        // 1️⃣ Добавляем эффект вспышки цвета
        AddColorFlashEffect();
        
        // 2️⃣ Добавляем эффект сжатия и возврата масштаба
        AddScaleHitEffect();
        
        // 3️⃣ Добавляем эффект тряски
        AddShakeEffect();
        
        // 4️⃣ Добавляем эффект отталкивания
        if (enableKnockback)
        {
            AddKnockbackEffect(hitDirection);
        }
        
        // Завершение последовательности
        _hitEffectSequence.OnComplete(() => {
            _isPlayingHitEffect = false;
            Debug.Log($"[MonsterHitEffects] Hit effect completed for {_monster.monsterName}");
        });
        
        _hitEffectSequence.Play();
        
        Debug.Log($"[MonsterHitEffects] Playing hit effect for {_monster.monsterName}");
    }
    
    /// <summary>
    /// Добавляет эффект вспышки цвета
    /// </summary>
    private void AddColorFlashEffect()
    {
        if (_meshRenderer == null || _meshRenderer.material == null) return;
        
        // Плавная смена цвета на красный и обратно
        var colorTween = _meshRenderer.material.DOColor(hitFlashColor, hitFlashDuration * 0.3f)
            .SetEase(hitFlashCurve);
            
        var colorBackTween = _meshRenderer.material.DOColor(_originalColor, hitFlashDuration * 0.7f)
            .SetEase(hitFlashCurve);
        
        _hitEffectSequence.Append(colorTween);
        _hitEffectSequence.Append(colorBackTween);
    }
    
    /// <summary>
    /// Добавляет эффект сжатия масштаба при ударе
    /// </summary>
    private void AddScaleHitEffect()
    {
        Vector3 hitScale = _originalScale * scaleHitMultiplier;
        
        var scaleDownTween = transform.DOScale(hitScale, scaleDuration * 0.4f)
            .SetEase(scaleCurve);
            
        var scaleUpTween = transform.DOScale(_originalScale, scaleDuration * 0.6f)
            .SetEase(Ease.OutBounce);
        
        _hitEffectSequence.Join(scaleDownTween); // Join - выполняется параллельно с цветом
        _hitEffectSequence.Append(scaleUpTween);
    }
    
    /// <summary>
    /// Добавляет эффект тряски
    /// </summary>
    private void AddShakeEffect()
    {
        _shakeTween = transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false, true)
            .OnComplete(() => {
                // Возвращаем в исходную позицию
                transform.localPosition = _originalPosition;
            });
        
        _hitEffectSequence.Join(_shakeTween); // Тряска параллельно с другими эффектами
    }
    
    /// <summary>
    /// Добавляет эффект отталкивания
    /// </summary>
    private void AddKnockbackEffect(Vector3 hitDirection)
    {
        if (hitDirection == Vector3.zero)
        {
            // Если направление не задано, отталкиваем назад
            hitDirection = -transform.forward;
        }
        
        // Нормализуем направление и применяем силу
        Vector3 knockbackVector = hitDirection.normalized * knockbackForce;
        Vector3 targetPosition = _originalPosition + knockbackVector;
        
        var knockbackTween = transform.DOLocalMove(targetPosition, knockbackDuration * 0.4f)
            .SetEase(Ease.OutQuad);
            
        var returnTween = transform.DOLocalMove(_originalPosition, knockbackDuration * 0.6f)
            .SetEase(Ease.InOutQuad);
        
        _hitEffectSequence.Join(knockbackTween);
        _hitEffectSequence.Append(returnTween);
    }
    
    /// <summary>
    /// Проигрывает упрощенный эффект удара без направления
    /// </summary>
    public void PlaySimpleHitEffect()
    {
        PlayHitEffectLocal(Vector3.zero);
    }
    
    /// <summary>
    /// Проигрывает анимацию смерти - падение на бок
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (_isPlayingDeathAnimation || _meshRenderer == null) return;
        
        _isPlayingDeathAnimation = true;
        Debug.Log($"[MonsterHitEffects] Playing death animation for {_monster.monsterName}");
        
        // Останавливаем все предыдущие эффекты
        StopAllEffects();
        
        // Создаем последовательность для анимации смерти
        _deathEffectSequence = DOTween.Sequence();
        
        // 1️⃣ Добавляем эффект падения на бок
        AddDeathFallEffect();
        
        // 2️⃣ Добавляем эффект постепенного затемнения
        AddDeathColorFadeEffect();
        
        // Завершение анимации смерти
        _deathEffectSequence.OnComplete(() => {
            _isPlayingDeathAnimation = false;
            Debug.Log($"[MonsterHitEffects] Death animation completed for {_monster.monsterName}");
        });
        
        _deathEffectSequence.Play();
    }
    
    /// <summary>
    /// Добавляет эффект падения на бок
    /// </summary>
    private void AddDeathFallEffect()
    {
        // Поворачиваем монстра на бок (вращение вокруг оси Z)
        Vector3 targetRotation = transform.rotation.eulerAngles + deathFallAngle;
        
        var rotationTween = transform.DORotate(targetRotation, deathFallDuration * 0.7f)
            .SetEase(Ease.InOutBack);
        
        // Одновременно опускаем монстра немного
        Vector3 targetPosition = _originalPosition + deathFallPositionOffset;
        var positionTween = transform.DOLocalMove(targetPosition, deathFallDuration * 0.7f)
            .SetEase(Ease.InOutBack);
        
        // Объединяем поворот и движение
        _deathEffectSequence.Join(rotationTween);
        _deathEffectSequence.Join(positionTween);
    }
    
    /// <summary>
    /// Добавляет эффект постепенного затемнения материала
    /// </summary>
    private void AddDeathColorFadeEffect()
    {
        if (_meshRenderer == null || _meshRenderer.material == null) return;
        
        // Сначала затемняем цвет до death tint
        Color targetColor = _originalColor * deathTintColor;
        var colorTween = _meshRenderer.material.DOColor(targetColor, deathColorFadeDuration * 0.5f)
            .SetEase(Ease.InOutQuad);
        
        // Затем постепенно уменьшаем альфу до 0 (исчезновение)
        var alphaTween = _meshRenderer.material.DOFade(0f, deathColorFadeDuration * 0.5f)
            .SetDelay(deathColorFadeDuration * 0.5f)
            .SetEase(Ease.InQuad);
        
        _deathEffectSequence.Append(colorTween);
        _deathEffectSequence.Append(alphaTween);
    }
    
    /// <summary>
    /// Останавливает все эффекты
    /// </summary>
    public void StopAllEffects()
    {
        _hitEffectSequence?.Kill();
        _deathEffectSequence?.Kill();
        _shakeTween?.Kill();
        
        // Возвращаем исходные значения только если НЕ играет анимация смерти
        if (!_isPlayingDeathAnimation)
        {
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _meshRenderer.material.color = _originalColor;
            }
            
            transform.localPosition = _originalPosition;
            transform.localScale = _originalScale;
        }
        
        _isPlayingHitEffect = false;
    }
    
    /// <summary>
    /// Останавливает только эффекты удара, сохраняя анимацию смерти
    /// </summary>
    public void StopHitEffectsOnly()
    {
        _hitEffectSequence?.Kill();
        _shakeTween?.Kill();
        _isPlayingHitEffect = false;
        
        Debug.Log($"[MonsterHitEffects] Stopped hit effects only, death animation continues");
    }
    
    /// <summary>
    /// Настройка эффектов в рантайме
    /// </summary>
    public void SetHitEffectSettings(float flashDuration, Color flashColor, float shakeStrength, float scaleMult)
    {
        hitFlashDuration = flashDuration;
        hitFlashColor = flashColor;
        this.shakeStrength = shakeStrength;
        scaleHitMultiplier = scaleMult;
        
        Debug.Log($"[MonsterHitEffects] Updated settings for {_monster.monsterName}");
    }
    
    /// <summary>
    /// Проверка, проигрывается ли эффект в данный момент
    /// </summary>
    public bool IsPlayingHitEffect()
    {
        return _isPlayingHitEffect;
    }
    
    /// <summary>
    /// Проверка, проигрывается ли анимация смерти
    /// </summary>
    public bool IsPlayingDeathAnimation()
    {
        return _isPlayingDeathAnimation;
    }
    
    private void OnDestroy()
    {
        // Очищаем все DoTween анимации при уничтожении
        StopAllEffects();
        DOTween.Kill(transform);
        DOTween.Kill(_meshRenderer);
    }
    
    private void OnDisable()
    {
        // Останавливаем эффекты при деактивации
        StopAllEffects();
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Тестирование эффекта в редакторе
    /// </summary>
    [ContextMenu("Test Hit Effect")]
    private void TestHitEffect()
    {
        if (Application.isPlaying)
        {
            PlayHitEffectLocal(Vector3.back);
        }
    }
    
    /// <summary>
    /// Тестирование анимации смерти в редакторе
    /// </summary>
    [ContextMenu("Test Death Animation")]
    private void TestDeathAnimation()
    {
        if (Application.isPlaying)
        {
            PlayDeathAnimation();
        }
    }
    #endif
}
