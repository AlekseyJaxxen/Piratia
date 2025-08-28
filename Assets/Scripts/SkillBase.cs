using UnityEngine;
using Mirror;

public abstract class SkillBase : MonoBehaviour, ISkill
{
    public float Cooldown => _cooldown;
    public float Range => _range;
    public float CastTime => _castTime;
    public KeyCode Hotkey { get => _hotkey; set => _hotkey = value; }
    public Texture2D CastCursor => _castCursor;
    public int ManaCost => _manaCost;
    public DamageType SkillDamageType => _damageType;
    public float RemainingCooldown => Mathf.Max(0, _cooldown - (Time.time - _lastUseTime));
    public float CooldownProgressNormalized => 1f - (RemainingCooldown / _cooldown);
    public string SkillName => _skillName;
    public int Weight => _weight; // Новое свойство для веса скилла
    public float EffectRadius => _effectRadius; // Добавлено
    public bool ignoreGlobalCooldown = false;
    protected PlayerCore _player;
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
    [SerializeField] protected int _weight = 1; // Новое поле для веса скилла
    [SerializeField] protected float _effectRadius; // Добавлено для радиуса эффекта
    protected float _lastUseTime;
    protected GameObject castRangeIndicator; // Радиус действия (вокруг игрока)
    public GameObject effectRadiusIndicator; // Радиус применения (на цели)
    public virtual void Init(PlayerCore core)
    {
        _player = core;
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
    public virtual void SetIndicatorVisibility(bool visible)
    {
        if (visible)
        {
            // Создать индикатор радиуса действия (вокруг игрока)
            if (castRangeIndicator == null)
            {
                castRangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                castRangeIndicator.GetComponent<Collider>().enabled = false;
                castRangeIndicator.transform.localScale = new Vector3(Range * 2, 0.1f, Range * 2);
                Renderer castRend = castRangeIndicator.GetComponent<Renderer>();
                castRend.material = new Material(Shader.Find("Sprites/Default")) { color = new Color(0, 1, 0, 0.3f) };
                // castRend.material = new Material(Shader.Find("Standard")) { color = new Color(0, 1, 0, 0.3f) }; // Прозрачный зеленый, не выделяющийся
                castRangeIndicator.name = $"{SkillName} Cast Range";
                castRangeIndicator.transform.SetParent(_player.transform); castRangeIndicator.transform.localPosition = Vector3.up * 0.01f;
                castRangeIndicator.transform.localPosition = Vector3.up * 0.01f;
                castRangeIndicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            castRangeIndicator.SetActive(true);
            // Создать индикатор радиуса применения (на цели)
            if (effectRadiusIndicator == null && EffectRadius > 0)
            {
                effectRadiusIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                effectRadiusIndicator.GetComponent<Collider>().enabled = false;
                effectRadiusIndicator.transform.localScale = new Vector3(EffectRadius * 2, 0.1f, EffectRadius * 2);
                Renderer effectRend = effectRadiusIndicator.GetComponent<Renderer>();
                effectRend.material = new Material(Shader.Find("Sprites/Default")) { color = new Color(1, 0, 0, 0.3f) };
                effectRadiusIndicator.name = $"{SkillName} Effect Radius";
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
        Debug.Log($"[SkillBase] Executing {_skillName}, lastUseTime={_lastUseTime}, Time.time={Time.time}");
        ExecuteSkillImplementation(player, targetPosition, targetObject);
    }
    private void OnDisable()
    {
        if (castRangeIndicator != null) Destroy(castRangeIndicator);
        if (effectRadiusIndicator != null) Destroy(effectRadiusIndicator);
    }
    protected abstract void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
}