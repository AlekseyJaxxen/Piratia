using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class BuffVFXController : NetworkBehaviour
{
    private PlayerCore _playerCore;
    private PlayerSkills _playerSkills;
    private CharacterStats _stats;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject stunVFXPrefab;
    [SerializeField] private Vector3 stunVFXOffset = Vector3.up;
    [SerializeField] private GameObject silenceVFXPrefab;
    [SerializeField] private Vector3 silenceVFXOffset = Vector3.up;
    [SerializeField] private GameObject slowVFXPrefab;
    [SerializeField] private Vector3 slowVFXOffset = Vector3.up;
    [SerializeField] private GameObject invisibilityVFXPrefab;
    [SerializeField] private Vector3 invisibilityVFXOffset = Vector3.up;
    [SerializeField] private GameObject statBuffVFXPrefab;
    [SerializeField] private Vector3 statBuffVFXOffset = Vector3.up;
    [SerializeField] private GameObject statDebuffVFXPrefab;
    [SerializeField] private Vector3 statDebuffVFXOffset = Vector3.up;
    [SerializeField] private GameObject toggleBuffVFXPrefab;
    [SerializeField] private Vector3 toggleBuffVFXOffset = Vector3.up;
    
    [Header("Buff Container")]
    [SerializeField] private Transform buffContainer; // Контейнер для размещения VFX
    [SerializeField] private float buffSpacing = 1.5f; // Расстояние между баффами

    private Dictionary<string, GameObject> _activeVFX = new Dictionary<string, GameObject>();
    
    // Событийная модель вместо периодических проверок
    private bool _useEventBasedUpdates = true;
    private bool _isInitialized = false;
    
    // Для совместимости со старым методом
    private float _lastVFXUpdate = 0f;
    private const float VFX_UPDATE_INTERVAL = 0.5f;

    private void Awake()
    {
        _playerCore = GetComponent<PlayerCore>();
        _playerSkills = GetComponent<PlayerSkills>();
        _stats = GetComponent<CharacterStats>();
    }

    private void Start()
    {
        if (_useEventBasedUpdates)
        {
            InitializeEventBasedUpdates();
        }
    }

    private void InitializeEventBasedUpdates()
    {
        if (_isInitialized) return;
        
        // Подписываемся на события изменения эффектов
        if (_stats != null)
        {
            // Подписываемся на события добавления/удаления эффектов
            _stats.OnStatEffectAdded.AddListener(OnStatEffectAdded);
            _stats.OnStatEffectRemoved.AddListener(OnStatEffectRemoved);
        }
        
        if (_playerCore != null)
        {
            // Подписываемся на события изменения контроля
            _playerCore.OnStunChanged.AddListener(OnStunChanged);
            _playerCore.OnSilenceChanged.AddListener(OnSilenceChanged);
        }
        
        if (_playerSkills != null)
        {
            // Подписываемся на события изменения невидимости
            _playerSkills.OnInvisibilityStateChanged.AddListener(OnInvisibilityChanged);
        }
        
        _isInitialized = true;
        
        // Первоначальное обновление
        ForceUpdateVFX();
    }

    private void Update()
    {
        if (!isClient) return;
        
        // Если не используем событийную модель, обновляем с интервалом
        if (!_useEventBasedUpdates)
        {
            // VFX обновление с интервалом (старый метод)
            if (Time.time - _lastVFXUpdate >= VFX_UPDATE_INTERVAL)
            {
                UpdateBuffVFX();
                CleanupExpiredVFX();
                _lastVFXUpdate = Time.time;
            }
        }
        else
        {
            // При событийной модели только очищаем истекшие эффекты
            CleanupExpiredVFX();
        }
    }
    
    // Принудительное обновление при изменении эффектов
    public void ForceUpdateVFX()
    {
        if (!isClient) return;
        UpdateBuffVFX();
        CleanupExpiredVFX();
        _lastVFXUpdate = Time.time;
    }
    
    private void CleanupExpiredVFX()
    {
        var expiredKeys = new List<string>();
        foreach (var kvp in _activeVFX)
        {
            // Очищаем записи где объект стал null
            if (kvp.Value == null)
            {
                expiredKeys.Add(kvp.Key);
                continue;
            }
            
            // Проверяем, истек ли эффект
            bool isExpired = true;
            foreach (var effect in _stats.activeStatEffects)
            {
                string key = effect.IsToggle ? $"ToggleBuff_{effect.Stat}" : $"Stat{(effect.Value >= effect.OriginalValue ? "Buff" : "Debuff")}_{effect.Stat}";
                if (key == kvp.Key && effect.IsActive)
                {
                    isExpired = false;
                    break;
                }
            }
            
            if (isExpired)
            {
                expiredKeys.Add(kvp.Key);
            }
        }
        
        foreach (var key in expiredKeys)
        {
            if (_activeVFX[key] != null)
            {
                Destroy(_activeVFX[key]);
            }
            _activeVFX.Remove(key);
            // Debug.Log($"[BuffVFXController] Cleaned up expired VFX {key} on {gameObject.name} at NetworkTime={(float)NetworkTime.time}");
        }
    }

    private void UpdateBuffVFX()
    {
        UpdateVFX("Stun", _playerCore.isStunned, stunVFXPrefab, _playerCore.stunEffectEndTime, stunVFXOffset);
        UpdateVFX("Silence", _playerCore.isSilenced, silenceVFXPrefab, _playerCore.silenceEffectEndTime, silenceVFXOffset);
        UpdateVFX("Invisibility", _playerSkills._isInvisible, invisibilityVFXPrefab, -1f, invisibilityVFXOffset);
        bool isSlowed = _stats.activeSlowEffects.Any(e => e.EndTime > (float)NetworkTime.time);
        float slowEndTime = isSlowed ? _stats.activeSlowEffects.Max(e => e.EndTime) : 0f;
        UpdateVFX("Slow", isSlowed, slowVFXPrefab, slowEndTime, slowVFXOffset);

        foreach (var effect in _stats.activeStatEffects)
        {
            string key = effect.IsToggle ? $"ToggleBuff_{effect.Stat}" : $"Stat{(effect.Value >= effect.OriginalValue ? "Buff" : "Debuff")}_{effect.Stat}";
            
            // Получаем VFX префаб
            GameObject vfxPrefab = null;
            if (effect.VFXPrefab != null)
            {
                vfxPrefab = effect.VFXPrefab;
            }
            else if (!string.IsNullOrEmpty(effect.VFXPrefabName))
            {
                // Загружаем префаб по имени
                vfxPrefab = Resources.Load<GameObject>($"VFX/{effect.VFXPrefabName}");
                if (vfxPrefab == null)
                {
                    Debug.LogWarning($"[BuffVFXController] Failed to load VFX prefab: VFX/{effect.VFXPrefabName}");
                }
            }
            else
            {
                // Используем дефолтные префабы
                vfxPrefab = effect.IsToggle ? toggleBuffVFXPrefab : (effect.Value >= effect.OriginalValue ? statBuffVFXPrefab : statDebuffVFXPrefab);
            }
            
            Vector3 offset = effect.VFXOffset != Vector3.zero ? effect.VFXOffset : (effect.IsToggle ? toggleBuffVFXOffset : (effect.Value >= effect.OriginalValue ? statBuffVFXOffset : statDebuffVFXOffset));
            
            // Логирование для диагностики (отключено для production)
            // if (effect.IsActive)
            // {
            //     Debug.Log($"[BuffVFXController] {gameObject.name}: {key} is active, EndTime={effect.EndTime}, NetworkTime={(float)NetworkTime.time}");
            // }
            
            UpdateVFX(key, effect.IsActive, vfxPrefab, effect.EndTime, offset);
        }
    }

    private void UpdateVFX(string key, bool isActive, GameObject vfxPrefab, float endTime, Vector3 offset)
    {
        if (isActive && vfxPrefab != null && !_activeVFX.ContainsKey(key))
        {
            // Создаем VFX в контейнере или на персонаже
            Transform parent = buffContainer != null ? buffContainer : transform;
            Vector3 position = buffContainer != null ? Vector3.zero : transform.position + offset;
            GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity, parent);
            _activeVFX[key] = vfx;
            
            // Обновляем позиции всех баффов
            UpdateBuffPositions();
            
            // Debug.Log($"[BuffVFXController] Activated VFX for {key} on {gameObject.name} in {(buffContainer != null ? "container" : "character")}");
        }
        else if (!isActive && _activeVFX.ContainsKey(key))
        {
            if (_activeVFX[key] != null)
            {
                Destroy(_activeVFX[key]);
            }
            _activeVFX.Remove(key);
            
            // Обновляем позиции оставшихся баффов
            UpdateBuffPositions();
            
            // Debug.Log($"[BuffVFXController] Deactivated VFX for {key} on {gameObject.name}");
        }
        else if (isActive && endTime > (float)NetworkTime.time && _activeVFX.ContainsKey(key) && _activeVFX[key] == null)
        {
            // Восстанавливаем VFX в контейнере или на персонаже
            Transform parent = buffContainer != null ? buffContainer : transform;
            Vector3 position = buffContainer != null ? Vector3.zero : transform.position + offset;
            GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity, parent);
            _activeVFX[key] = vfx;
            
            // Обновляем позиции всех баффов
            UpdateBuffPositions();
            
            // Debug.Log($"[BuffVFXController] Restored VFX for {key} on {gameObject.name} in {(buffContainer != null ? "container" : "character")}");
        }
        
        // ИСПРАВЛЕНИЕ ПРОБЛЕМЫ: Дополнительная проверка для объектов которые стали null
        else if (isActive && vfxPrefab != null && _activeVFX.ContainsKey(key) && 
                 (_activeVFX[key] == null || _activeVFX[key].Equals(null)))
        {
            // VFX должен быть активен, но объект был уничтожен - восстанавливаем его
            Transform parent = buffContainer != null ? buffContainer : transform;
            Vector3 position = buffContainer != null ? Vector3.zero : transform.position + offset;
            GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity, parent);
            _activeVFX[key] = vfx;
            
            // Обновляем позиции всех баффов
            UpdateBuffPositions();
            
            // Debug.Log($"[BuffVFXController] Fixed missing VFX for {key} on {gameObject.name}");
        }
    }
    
    private void UpdateBuffPositions()
    {
        if (buffContainer == null) return;
        
        var activeBuffs = _activeVFX.Values.Where(vfx => vfx != null).ToList();
        
        if (activeBuffs.Count == 0) return;
        
        // Вычисляем общую ширину
        float totalWidth = (activeBuffs.Count - 1) * buffSpacing;
        float startX = -totalWidth * 0.5f; // Начинаем с левого края
        
        // Размещаем баффы по горизонтали
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i] != null)
            {
                float x = startX + i * buffSpacing;
                activeBuffs[i].transform.localPosition = new Vector3(x, 0, 0);
            }
        }
    }

    // Обработчики событий для событийной модели
    private void OnStatEffectAdded(CharacterStats.StatEffect effect)
    {
        if (!_useEventBasedUpdates) return;
        
        string key = effect.IsToggle ? $"ToggleBuff_{effect.Stat}" : $"Stat{(effect.Value >= effect.OriginalValue ? "Buff" : "Debuff")}_{effect.Stat}";
        GameObject vfxPrefab = GetVFXPrefabForEffect(effect);
        Vector3 offset = GetVFXOffsetForEffect(effect);
        
        UpdateVFX(key, true, vfxPrefab, effect.EndTime, offset);
    }
    
    private void OnStatEffectRemoved(CharacterStats.StatEffect effect)
    {
        if (!_useEventBasedUpdates) return;
        
        string key = effect.IsToggle ? $"ToggleBuff_{effect.Stat}" : $"Stat{(effect.Value >= effect.OriginalValue ? "Buff" : "Debuff")}_{effect.Stat}";
        
        if (_activeVFX.ContainsKey(key))
        {
            if (_activeVFX[key] != null)
            {
                Destroy(_activeVFX[key]);
            }
            _activeVFX.Remove(key);
            UpdateBuffPositions();
        }
    }
    
    private void OnStunChanged(bool isStunned)
    {
        if (!_useEventBasedUpdates) return;
        
        UpdateVFX("Stun", isStunned, stunVFXPrefab, _playerCore.stunEffectEndTime, stunVFXOffset);
    }
    
    private void OnSilenceChanged(bool isSilenced)
    {
        if (!_useEventBasedUpdates) return;
        
        UpdateVFX("Silence", isSilenced, silenceVFXPrefab, _playerCore.silenceEffectEndTime, silenceVFXOffset);
    }
    
    private void OnInvisibilityChanged(bool isInvisible)
    {
        if (!_useEventBasedUpdates) return;
        
        UpdateVFX("Invisibility", isInvisible, invisibilityVFXPrefab, -1f, invisibilityVFXOffset);
    }
    
    // Вспомогательные методы для получения VFX префабов и смещений
    private GameObject GetVFXPrefabForEffect(CharacterStats.StatEffect effect)
    {
        if (effect.VFXPrefab != null)
        {
            return effect.VFXPrefab;
        }
        else if (!string.IsNullOrEmpty(effect.VFXPrefabName))
        {
            GameObject vfxPrefab = Resources.Load<GameObject>($"VFX/{effect.VFXPrefabName}");
            if (vfxPrefab == null)
            {
                Debug.LogWarning($"[BuffVFXController] Failed to load VFX prefab: VFX/{effect.VFXPrefabName}");
            }
            return vfxPrefab;
        }
        else
        {
            return effect.IsToggle ? toggleBuffVFXPrefab : (effect.Value >= effect.OriginalValue ? statBuffVFXPrefab : statDebuffVFXPrefab);
        }
    }
    
    private Vector3 GetVFXOffsetForEffect(CharacterStats.StatEffect effect)
    {
        if (effect.VFXOffset != Vector3.zero)
        {
            return effect.VFXOffset;
        }
        else
        {
            return effect.IsToggle ? toggleBuffVFXOffset : (effect.Value >= effect.OriginalValue ? statBuffVFXOffset : statDebuffVFXOffset);
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (_stats != null)
        {
            _stats.OnStatEffectAdded.RemoveListener(OnStatEffectAdded);
            _stats.OnStatEffectRemoved.RemoveListener(OnStatEffectRemoved);
        }
        
        if (_playerCore != null)
        {
            _playerCore.OnStunChanged.RemoveListener(OnStunChanged);
            _playerCore.OnSilenceChanged.RemoveListener(OnSilenceChanged);
        }
        
        if (_playerSkills != null)
        {
            _playerSkills.OnInvisibilityStateChanged.RemoveListener(OnInvisibilityChanged);
        }
        
        // Очищаем VFX
        foreach (var vfx in _activeVFX.Values)
        {
            if (vfx != null) Destroy(vfx);
        }
        _activeVFX.Clear();
    }
}