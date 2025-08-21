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

    bool IsOnCooldown();
    void StartCooldown();
    void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject);
    void SetIndicatorVisibility(bool isVisible);
}