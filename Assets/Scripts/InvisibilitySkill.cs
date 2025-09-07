using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "InvisibilitySkill", menuName = "Skills/InvisibilitySkill")]
public class InvisibilitySkill : SkillBase
{
    [Header("Invisibility Settings")]
    public float duration = 10f; // Настраиваемая длительность
    public GameObject invisibilityEffectPrefab; // Эффект частиц для невидимости (опционально)
    private GameObject _invisibilityEffectInstance;
    private int _originalLayer; // Для сохранения слоя игрока

    public override void Init(PlayerCore core)
    {
        base.Init(core);
        castType = CastType.ToggleBuff;
        if (invisibilityEffectPrefab != null)
        {
            _invisibilityEffectInstance = Object.Instantiate(invisibilityEffectPrefab, core.transform);
            _invisibilityEffectInstance.SetActive(false);
        }
        _originalLayer = core.gameObject.layer; // Сохраняем слой "Player"
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
        // Меняем слой игрока
        if (isActive)
        {
            _player.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        else
        {
            _player.gameObject.layer = _originalLayer; // Восстанавливаем слой "Player"
        }

        // Управляем эффектом частиц, если он есть
        if (_invisibilityEffectInstance != null)
        {
            _invisibilityEffectInstance.SetActive(isActive);
        }

        // Вызываем RPC для управления видимостью Models
        PlayerSkills skills = _player.GetComponent<PlayerSkills>();
        if (skills != null)
        {
            skills.RpcSetInvisibilityVisibility(isActive, _player.team);
        }
    }
}