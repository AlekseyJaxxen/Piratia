using UnityEngine;
using Mirror;

public interface ISkill
{
    float Cooldown { get; }
    float Range { get; }
    float CastTime { get; }
    KeyCode Hotkey { get; set; }
    Texture2D CastCursor { get; }
    int ManaCost { get; }
    DamageType SkillDamageType { get; }
    float RemainingCooldown { get; }
    float CooldownProgressNormalized { get; }
    string SkillName { get; }
    string Description { get; }
    int Weight { get; }
    float EffectRadius { get; }
    bool ignoreGlobalCooldown { get; }
    void Init(PlayerCore core);
    bool IsOnCooldown();
    void StartCooldown();
    void SetIndicatorVisibility(bool isVisible);
    void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
    void CleanupIndicators();
    void SetEffectRadiusPosition(Vector3 position);
    void ApplyInvisibilityEffect(bool isActive);
}

public enum DamageType
{
    Physical,
    Magic
}

public abstract class SkillBase : ScriptableObject, ISkill
{
    public enum CastType
    {
        TargetedEnemy, // �� �����/������� (projectile dmg, debuff)
        TargetedAlly, // �� ��������/���� (heal, buff)
        GroundAoEInstant, // ��� �� ����� ���������� (����/�������)
        GroundAoEPersistent, // ��� �� ����� persistent (����������, reveal)
        SelfBuff, // �������� ����������
        ToggleBuff // �������������� (toggle on/off)
    }

    [SerializeField] protected CastType castType;
    public CastType SkillCastType => castType;
    public virtual float Cooldown => _cooldown;
    public virtual float Range => _range;
    public float CastTime => _castTime;
    public KeyCode Hotkey { get => _hotkey; set => _hotkey = value; }
    public Texture2D CastCursor => _castCursor;
    public int ManaCost => _manaCost;
    public DamageType SkillDamageType => _damageType;
    public float RemainingCooldown => _playerSkills != null ? _playerSkills.GetRemainingCooldown(SkillName) : 0f;
    public float CooldownProgressNormalized => Cooldown > 0 ? 1f - (RemainingCooldown / Cooldown) : 1f;
    public string SkillName => _skillName;
    public string Description => _description;
    public int Weight => _weight;
    public float EffectRadius => _effectRadius;
    public bool ignoreGlobalCooldown => _ignoreGlobalCooldown;
    protected PlayerCore _player;
    protected PlayerSkills _playerSkills;
    [SerializeField] protected Sprite _icon;
    public Sprite Icon => _icon;

    [Header("Base Skill Settings")]
    [SerializeField] protected string _skillName;
    [SerializeField] protected string _description;
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
    [SerializeField] protected bool _ignoreGlobalCooldown = false;
    [Header("Indicator Prefabs")]
    [SerializeField] public GameObject castRangePrefab;
    [SerializeField] public GameObject effectRadiusPrefab;
    protected GameObject castRangeIndicator;
    protected GameObject effectRadiusIndicator;
    public GameObject VFXPrefab; // ������� VFX-������ (��� �������������)
    public GameObject GetVFXPrefab() => VFXPrefab;

    public virtual void Init(PlayerCore core)
    {
        _player = core;
        _playerSkills = core.GetComponent<PlayerSkills>();
        if (string.IsNullOrEmpty(_skillName))
        {
            Debug.LogError($"[SkillBase] SkillName not set for {name}");
        }
    }
    
    /// <summary>
    /// Устанавливает ссылку на игрока для скила
    /// </summary>
    public void SetPlayer(PlayerCore core)
    {
        Init(core);
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
                // Cast range indicator created
            }
            if (castRangeIndicator != null) castRangeIndicator.SetActive(true);
            if (effectRadiusIndicator == null && effectRadiusPrefab != null && EffectRadius > 0)
            {
                effectRadiusIndicator = Object.Instantiate(effectRadiusPrefab);
                effectRadiusIndicator.transform.localScale = new Vector3(EffectRadius * 2, 0.1f, EffectRadius * 2);
                effectRadiusIndicator.name = $"{SkillName} Effect Radius";
                // Effect radius indicator created
            }
            if (effectRadiusIndicator != null) effectRadiusIndicator.SetActive(true);
        }
        else
        {
            if (castRangeIndicator != null) castRangeIndicator.SetActive(false);
            if (effectRadiusIndicator != null) effectRadiusIndicator.SetActive(false);
        }
    }

    public virtual void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (!NetworkClient.active && !NetworkServer.active)
        {
            Debug.LogWarning($"[SkillBase] Skill execution failed for {_skillName}: Neither client nor server is active.");
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
        
        // КРИТИЧНО: Проверяем стан и тишину перед кастом на клиенте для предотвращения кликспамма
        if (player.isStunned)
        {
            Debug.LogWarning($"[SkillBase] Cannot cast skill {_skillName} while stunned: {player.name}");
            return;
        }
        
        // Проверяем тишину (кроме простых атак)
        if (player.isSilenced && !(this is BasicAttackSkill))
        {
            Debug.LogWarning($"[SkillBase] Cannot cast skill {_skillName} while silenced: {player.name}");
            return;
        }
        
        // Executing skill
        ExecuteSkillImplementation(player, targetPosition, targetObject);
    }

    public virtual void CleanupIndicators()
    {
        if (castRangeIndicator != null && !castRangeIndicator.Equals(null))
        {
            // Cleaning up cast range indicator
            Destroy(castRangeIndicator);
            castRangeIndicator = null;
        }
        if (effectRadiusIndicator != null && !effectRadiusIndicator.Equals(null))
        {
            Debug.Log($"[SkillBase] Cleaning up effectRadiusIndicator for {SkillName}");
            Destroy(effectRadiusIndicator);
            effectRadiusIndicator = null;
        }
    }

    public virtual void SetEffectRadiusPosition(Vector3 position)
    {
        if (effectRadiusIndicator != null)
        {
            effectRadiusIndicator.transform.position = position;
        }
    }

    public virtual void ApplyInvisibilityEffect(bool isActive)
    {
        // ������� ����������, ���������������� � InvisibilitySkill
    }

    protected abstract void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject);

    public virtual void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        // ������� ���������� - �����
    }

    public virtual void ExecuteOnClient(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        // ������� ���������� - �����
    }
}