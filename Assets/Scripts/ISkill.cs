using UnityEngine;

public interface ISkill
{
    float Cooldown { get; }
    float Range { get; }
    float CastTime { get; }
    KeyCode Hotkey { get; }
    GameObject RangeIndicator { get; }
    GameObject TargetIndicator { get; }
    Texture2D CastCursor { get; }
    int ManaCost { get; }
    DamageType SkillDamageType { get; }
    float RemainingCooldown { get; }
    float CooldownProgressNormalized { get; }
    string SkillName { get; }
    int Weight { get; } // Добавлено свойство Weight

    void Init(PlayerCore core);
    bool IsOnCooldown();
    void StartCooldown();
    void SetIndicatorVisibility(bool isVisible);
    void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
}