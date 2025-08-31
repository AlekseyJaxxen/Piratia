using UnityEngine;
using Mirror;
using UnityEditor;

[CreateAssetMenu(fileName = "NewSkillBase", menuName = "Skills/SkillBase")]
public abstract class SkillBase : ScriptableObject, ISkill
{
    public float Cooldown => _cooldown;
    public float Range => _range;
    public float CastTime => _castTime;
    public KeyCode Hotkey { get => _hotkey; set => _hotkey = value; }
    public Texture2D CastCursor => _castCursor;
    public int ManaCost => _manaCost;
    public DamageType SkillDamageType => _damageType;
    public float RemainingCooldown => _playerSkills != null ? _playerSkills.GetRemainingCooldown(SkillName) : 0f;
    public float CooldownProgressNormalized => Cooldown > 0 ? 1f - (RemainingCooldown / Cooldown) : 1f;
    public string SkillName => _skillName;
    public int Weight => _weight;
    public float EffectRadius => _effectRadius;
    public bool ignoreGlobalCooldown = false;
    protected PlayerCore _player;
    protected PlayerSkills _playerSkills;
    [SerializeField] protected Sprite _icon; public Sprite Icon => _icon;
    [Header("Base Skill Settings")]
    [SerializeField] protected string _skillName;
    [SerializeField] protected KeyCode _hotkey;
    [SerializeField] protected float _range;
    [SerializeField] protected float _cooldown;
    [SerializeField] protected float _castTime;
    [SerializeField] protected Texture2D _castCursor;
    [Header("Mana Cost")]
    [SerializeField] protected int _manaCost = 0;
    [SerializeField] protected DamageType _damageType = DamageType.Physical;
    [SerializeField] protected int _weight = 1;
    [SerializeField] protected float _effectRadius;
    [Header("Indicator Prefabs")]
    [SerializeField] public GameObject castRangePrefab;
    [SerializeField] public GameObject effectRadiusPrefab;
    private GameObject castRangeIndicator;                // Приватное поле
    private GameObject effectRadiusIndicator;             // Приватное поле

    public virtual void Init(PlayerCore core)
    {
        _player = core;
        _playerSkills = core.GetComponent<PlayerSkills>();
        if (string.IsNullOrEmpty(_skillName))
        {
            Debug.LogError($"[SkillBase] SkillName not set for {name}");
        }
    }

    public bool IsOnCooldown()
    {
        return RemainingCooldown > 0;
    }

    public void StartCooldown()
    {
        if (_playerSkills != null)
            _playerSkills.StartSkillCooldown(SkillName);
    }

    public virtual void SetIndicatorVisibility(bool visible)
    {
        if (visible)
        {
            if (castRangeIndicator == null && castRangePrefab != null)
            {
                castRangeIndicator = Object.Instantiate(castRangePrefab, _player.transform);
                castRangeIndicator.transform.localScale = new Vector3(Range * 2, 0.1f, Range * 2);
                castRangeIndicator.transform.localPosition = Vector3.up * 0.01f;
                castRangeIndicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
                castRangeIndicator.name = $"{SkillName} Cast Range";
                Debug.Log($"[SkillBase] Created castRangeIndicator for {SkillName} at {castRangeIndicator.transform.position}");
            }
            if (castRangeIndicator != null) castRangeIndicator.SetActive(true);

            if (effectRadiusIndicator == null && effectRadiusPrefab != null && EffectRadius > 0)
            {
                effectRadiusIndicator = Object.Instantiate(effectRadiusPrefab);
                effectRadiusIndicator.transform.localScale = new Vector3(EffectRadius * 2, 0.1f, EffectRadius * 2);
                effectRadiusIndicator.name = $"{SkillName} Effect Radius";
                Debug.Log($"[SkillBase] Created effectRadiusIndicator for {SkillName} at {effectRadiusIndicator.transform.position} with radius {EffectRadius}");
            }
            if (effectRadiusIndicator != null) effectRadiusIndicator.SetActive(true);
        }
        else
        {
            if (castRangeIndicator != null) castRangeIndicator.SetActive(false);
            if (effectRadiusIndicator != null) effectRadiusIndicator.SetActive(false);
        }
    }

    public void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (!NetworkClient.active)
        {
            Debug.LogWarning($"[SkillBase] Skill execution failed for {_skillName}: Client is not connected.");
            return;
        }
        if (!player.isLocalPlayer)
        {
            Debug.LogWarning($"[SkillBase] Skill execution ignored for {_skillName}: Not a local player.");
            return;
        }
        if (IsOnCooldown())
        {
            Debug.LogWarning($"[SkillBase] Skill {_skillName} is on cooldown. Remaining: {RemainingCooldown:F2}s");
            return;
        }
        if (!player.netIdentity.isOwned)
        {
            Debug.LogWarning($"[SkillBase] Skill execution failed for {_skillName}: Player lacks authority.");
            return;
        }
        Debug.Log($"[SkillBase] Executing {_skillName}");
        ExecuteSkillImplementation(player, targetPosition, targetObject);
    }

    public virtual void CleanupIndicators()
    {
        if (castRangeIndicator != null && !castRangeIndicator.Equals(null))
        {
            Debug.Log($"[SkillBase] Cleaning up castRangeIndicator for {SkillName}, isPrefab: {PrefabUtility.IsPartOfAnyPrefab(castRangeIndicator)}");
            if (PrefabUtility.IsPartOfAnyPrefab(castRangeIndicator))
            {
                DestroyImmediate(castRangeIndicator, true);
            }
            else
            {
                Destroy(castRangeIndicator);
            }
            castRangeIndicator = null;
        }
        if (effectRadiusIndicator != null && !effectRadiusIndicator.Equals(null))
        {
            Debug.Log($"[SkillBase] Cleaning up effectRadiusIndicator for {SkillName}, isPrefab: {PrefabUtility.IsPartOfAnyPrefab(effectRadiusIndicator)}");
            if (PrefabUtility.IsPartOfAnyPrefab(effectRadiusIndicator))
            {
                DestroyImmediate(effectRadiusIndicator, true);
            }
            else
            {
                Destroy(effectRadiusIndicator);
            }
            effectRadiusIndicator = null;
        }
    }

    // Новый метод для доступа к effectRadiusIndicator
    public void SetEffectRadiusPosition(Vector3 position)
    {
        if (effectRadiusIndicator != null)
        {
            effectRadiusIndicator.transform.position = position;
        }
    }

    protected abstract void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
}