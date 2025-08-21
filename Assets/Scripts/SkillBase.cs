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

    [Header("Base Skill Settings")]
    [SerializeField] protected KeyCode _hotkey;
    [SerializeField] protected float _range;
    [SerializeField] protected float _cooldown;
    [SerializeField] protected float _castTime;
    [SerializeField] protected GameObject _rangeIndicator;
    [SerializeField] protected GameObject _targetIndicatorPrefab;
    [SerializeField] protected Texture2D _castCursor;

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
                _rangeIndicator.transform.localScale = new Vector3(_range * 2, 0.1f, _range * 2);
            }
        }
        if (_targetIndicatorInstance != null)
        {
            _targetIndicatorInstance.SetActive(false);
        }
    }

    public abstract void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
}