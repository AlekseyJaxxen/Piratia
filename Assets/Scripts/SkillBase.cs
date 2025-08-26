using UnityEngine;
using Mirror;

public abstract class SkillBase : MonoBehaviour, ISkill
{
    public float Cooldown => _cooldown;
    public float Range => _range;
    public float CastTime => _castTime;
    public KeyCode Hotkey => _hotkey;
    public GameObject RangeIndicator => _rangeIndicator;
    public GameObject TargetIndicator => _targetIndicatorInstance;
    public Texture2D CastCursor => _castCursor;
    public int ManaCost => _manaCost;
    public DamageType SkillDamageType => _damageType;
    public float RemainingCooldown => Mathf.Max(0, _cooldown - (Time.time - _lastUseTime));
    public float CooldownProgressNormalized => 1f - (RemainingCooldown / _cooldown);
    public string SkillName => _skillName;

    [Header("Base Skill Settings")]
    [SerializeField] protected string _skillName;
    [SerializeField] protected KeyCode _hotkey;
    [SerializeField] protected float _range;
    [SerializeField] protected float _cooldown;
    [SerializeField] protected float _castTime;
    [SerializeField] protected GameObject _rangeIndicator;
    [SerializeField] protected GameObject _targetIndicatorPrefab;
    [SerializeField] protected Texture2D _castCursor;
    [Header("Mana Cost")]
    [SerializeField] protected int _manaCost = 0;
    [SerializeField] protected DamageType _damageType = DamageType.Physical;

    protected float _lastUseTime;
    protected GameObject _targetIndicatorInstance;

    public virtual void Init(PlayerCore core)
    {
        if (_targetIndicatorPrefab != null)
        {
            _targetIndicatorInstance = Instantiate(_targetIndicatorPrefab);
            _targetIndicatorInstance.SetActive(false);
        }
        if (_rangeIndicator != null)
        {
            _rangeIndicator.SetActive(false);
        }
        if (string.IsNullOrEmpty(_skillName))
        {
            Debug.LogError($"[SkillBase] SkillName not set for {gameObject.name}");
        }
    }

    public bool IsOnCooldown()
    {
        return Time.time - _lastUseTime < _cooldown;
    }

    public void StartCooldown()
    {
        _lastUseTime = Time.time;
    }

    public virtual void SetIndicatorVisibility(bool isVisible)
    {
        if (_rangeIndicator != null)
        {
            _rangeIndicator.SetActive(isVisible);
            if (isVisible)
            {
                _rangeIndicator.transform.position = transform.position;
                _rangeIndicator.transform.localScale = new Vector3(_range, 1f, _range);
            }
        }
        if (_targetIndicatorInstance != null)
        {
            _targetIndicatorInstance.SetActive(isVisible);
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
        Debug.Log($"[SkillBase] Executing {_skillName}, lastUseTime={_lastUseTime}, Time.time={Time.time}");
        ExecuteSkillImplementation(player, targetPosition, targetObject);
    }

    private void OnDisable()
    {
        if (_targetIndicatorInstance != null)
        {
            Destroy(_targetIndicatorInstance);
        }
    }

    protected abstract void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
}