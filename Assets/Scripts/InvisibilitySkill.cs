using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "InvisibilitySkill", menuName = "Skills/InvisibilitySkill")]
public class InvisibilitySkill : SkillBase
{
    [Header("Invisibility Settings")]
    public float duration = 10f; // Настраиваемая длительность
    public GameObject invisibilityEffectPrefab; // Эффект частиц для невидимости (опционально)
    private GameObject _invisibilityEffectInstance;
    public int originalLayer { get; private set; } // Для сохранения слоя игрока

    public override void Init(PlayerCore core)
    {
        base.Init(core);
        castType = CastType.ToggleBuff;
        if (invisibilityEffectPrefab != null)
        {
            _invisibilityEffectInstance = Object.Instantiate(invisibilityEffectPrefab, core.transform);
            _invisibilityEffectInstance.SetActive(false);
        }
        originalLayer = core.gameObject.layer; // Сохраняем слой "Player"
        Debug.Log($"[InvisibilitySkill] Initialized with originalLayer={originalLayer} for {core.gameObject.name}");
    }

    protected override void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        // Логика на клиенте не нужна, т.к. toggle в HandleSkills вызывает Cmd
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        // Не используется, т.к. toggle в CmdToggleInvisibility
    }

    public override void CleanupIndicators()
    {
        base.CleanupIndicators();
        if (_invisibilityEffectInstance != null)
        {
            Debug.Log($"[InvisibilitySkill] Cleaning up invisibility effect for {SkillName}");
            Destroy(_invisibilityEffectInstance);
            _invisibilityEffectInstance = null;
        }
    }

    public override void ApplyInvisibilityEffect(bool isActive)
    {
        if (_player == null)
        {
            Debug.LogWarning($"[InvisibilitySkill] Player is null in ApplyInvisibilityEffect for {SkillName}");
            return;
        }
        // Управляем только эффектом частиц на клиенте
        if (_invisibilityEffectInstance != null)
        {
            _invisibilityEffectInstance.SetActive(isActive);
            Debug.Log($"[InvisibilitySkill] Invisibility effect {(isActive ? "enabled" : "disabled")} for {_player.gameObject.name}");
        }
    }
}