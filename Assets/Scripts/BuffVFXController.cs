using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

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
        UpdateVFX("Stun", _playerCore.isStunned, stunVFXPrefab, _playerCore.stunEffectEndTime, stunVFXOffset);
        UpdateVFX("Silence", _playerCore.isSilenced, silenceVFXPrefab, _playerCore.silenceEffectEndTime, silenceVFXOffset);
        UpdateVFX("Invisibility", _playerSkills._isInvisible, invisibilityVFXPrefab, -1f, invisibilityVFXOffset);
        bool isSlowed = _stats.activeSlowEffects.Any(e => e.EndTime > Time.time);
        float slowEndTime = isSlowed ? _stats.activeSlowEffects.Max(e => e.EndTime) : 0f;
        UpdateVFX("Slow", isSlowed, slowVFXPrefab, slowEndTime, slowVFXOffset);

        foreach (var effect in _stats.activeStatEffects)
        {
            string key = effect.IsToggle ? $"ToggleBuff_{effect.Stat}" : $"Stat{(effect.Value >= effect.OriginalValue ? "Buff" : "Debuff")}_{effect.Stat}";
            GameObject vfxPrefab = effect.VFXPrefab != null ? effect.VFXPrefab : (effect.IsToggle ? toggleBuffVFXPrefab : (effect.Value >= effect.OriginalValue ? statBuffVFXPrefab : statDebuffVFXPrefab));
            Vector3 offset = effect.VFXPrefab != null ? effect.VFXOffset : (effect.IsToggle ? toggleBuffVFXOffset : (effect.Value >= effect.OriginalValue ? statBuffVFXOffset : statDebuffVFXOffset));
            UpdateVFX(key, effect.IsActive, vfxPrefab, effect.EndTime, offset);
        }
    }

    private void UpdateVFX(string key, bool isActive, GameObject vfxPrefab, float endTime, Vector3 offset)
    {
        if (isActive && vfxPrefab != null && !_activeVFX.ContainsKey(key))
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position + offset, Quaternion.identity, transform);
            _activeVFX[key] = vfx;
            Debug.Log($"[BuffVFXController] Activated VFX for {key} on {gameObject.name} at offset {offset}");
        }
        else if (!isActive && _activeVFX.ContainsKey(key))
        {
            if (_activeVFX[key] != null)
            {
                Destroy(_activeVFX[key]);
            }
            _activeVFX.Remove(key);
            Debug.Log($"[BuffVFXController] Deactivated VFX for {key} on {gameObject.name}");
        }
        else if (isActive && endTime > Time.time && _activeVFX.ContainsKey(key) && _activeVFX[key] == null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position + offset, Quaternion.identity, transform);
            _activeVFX[key] = vfx;
            Debug.Log($"[BuffVFXController] Restored VFX for {key} on {gameObject.name} at offset {offset}");
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