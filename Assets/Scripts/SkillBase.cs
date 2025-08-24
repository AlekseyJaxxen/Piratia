using UnityEngine;
using Mirror;

public abstract class SkillBase : NetworkBehaviour, ISkill
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

    [Header("Base Skill Settings")]
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
        if (isLocalPlayer && _targetIndicatorPrefab != null)
        {
            _targetIndicatorInstance = Instantiate(_targetIndicatorPrefab);
            _targetIndicatorInstance.SetActive(false);
        }

        if (isLocalPlayer && _rangeIndicator != null)
        {
            _rangeIndicator.SetActive(false);
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
            _targetIndicatorInstance.SetActive(false);
        }
    }

    public void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        // ”бираем проверку маны здесь - переносим на сервер
        ExecuteSkillImplementation(player, targetPosition, targetObject);
    }

    protected abstract void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
}