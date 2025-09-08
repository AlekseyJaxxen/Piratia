using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BuffVFXController : NetworkBehaviour
{
    private PlayerCore _playerCore;
    private PlayerSkills _playerSkills;
    private CharacterStats _stats;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject stunVFXPrefab; // Префаб VFX для оглушения
    [SerializeField] private GameObject silenceVFXPrefab; // Префаб VFX для молчания
    [SerializeField] private GameObject slowVFXPrefab; // Префаб VFX для замедления
    [SerializeField] private GameObject invisibilityVFXPrefab; // Префаб VFX для невидимости
    [SerializeField] private GameObject statBuffVFXPrefab; // Префаб VFX для баффов характеристик
    [SerializeField] private GameObject statDebuffVFXPrefab; // Префаб VFX для дебаффов характеристик
    [SerializeField] private GameObject toggleBuffVFXPrefab; // Префаб VFX для переключаемых баффов

    private Dictionary<string, GameObject> _activeVFX = new Dictionary<string, GameObject>();

    private void Awake()
    {
        _playerCore = GetComponent<PlayerCore>();
        _playerSkills = GetComponent<PlayerSkills>();
        _stats = GetComponent<CharacterStats>();
    }

    private void Update()
    {
        if (!isClient) return;
        UpdateBuffVFX();
    }

    private void UpdateBuffVFX()
    {
        // Оглушение
        UpdateVFX("Stun", _playerCore.isStunned, stunVFXPrefab, _playerCore.stunEffectEndTime);

        // Молчание
        UpdateVFX("Silence", _playerCore.isSilenced, silenceVFXPrefab, _playerCore.silenceEffectEndTime);

        // Невидимость
        UpdateVFX("Invisibility", _playerSkills._isInvisible, invisibilityVFXPrefab, -1f); // -1f для неопределенной длительности

        // Замедление
        bool isSlowed = _stats.activeSlowEffects.Any(e => e.EndTime > Time.time);
        UpdateVFX("Slow", isSlowed, slowVFXPrefab, isSlowed ? _stats.activeSlowEffects.Max(e => e.EndTime) : 0f);

        // Баффы/дебаффы характеристик (из ApplyBuff/ApplyDebuff/ToggleBuff)
        // Предполагаем, что баффы/дебаффы хранятся временно в CharacterStats
        // Для простоты проверяем наличие активных корутин в CharacterStats
        // Требуется доработка, если есть список активных баффов
        // Для примера используем заглушки (нужны реальные данные о баффах)
        bool hasStatBuff = false; // Заменить на реальную проверку
        bool hasStatDebuff = false; // Заменить на реальную проверку
        bool hasToggleBuff = false; // Заменить на реальную проверку
        UpdateVFX("StatBuff", hasStatBuff, statBuffVFXPrefab, -1f);
        UpdateVFX("StatDebuff", hasStatDebuff, statDebuffVFXPrefab, -1f);
        UpdateVFX("ToggleBuff", hasToggleBuff, toggleBuffVFXPrefab, -1f);
    }

    private void UpdateVFX(string key, bool isActive, GameObject vfxPrefab, float endTime)
    {
        if (isActive && vfxPrefab != null && !_activeVFX.ContainsKey(key))
        {
            // Активируем VFX
            GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity, transform);
            _activeVFX[key] = vfx;
            Debug.Log($"[BuffVFXController] Activated VFX for {key} on {gameObject.name}");
        }
        else if (!isActive && _activeVFX.ContainsKey(key))
        {
            // Деактивируем VFX
            if (_activeVFX[key] != null)
            {
                Destroy(_activeVFX[key]);
            }
            _activeVFX.Remove(key);
            Debug.Log($"[BuffVFXController] Deactivated VFX for {key} on {gameObject.name}");
        }
        else if (isActive && endTime > Time.time && _activeVFX.ContainsKey(key) && _activeVFX[key] == null)
        {
            // Восстанавливаем VFX, если он был уничтожен, но эффект еще активен
            GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity, transform);
            _activeVFX[key] = vfx;
            Debug.Log($"[BuffVFXController] Restored VFX for {key} on {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        foreach (var vfx in _activeVFX.Values)
        {
            if (vfx != null) Destroy(vfx);
        }
        _activeVFX.Clear();
    }
}