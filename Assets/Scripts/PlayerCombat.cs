using UnityEngine;
using Mirror;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerCore _core;
    private GameObject _target;
    [HideInInspector]
    public float _lastAttackTime = -Mathf.Infinity;

    public GameObject Target => _target; // Добавлено публичное свойство для доступа к _target

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    public void HandleCombat()
    {
        if (_core.ActionSystem.CurrentAction != PlayerAction.Attack) return;
        if (_target == null) return;
    }

    public void SetCurrentTarget(GameObject target)
    {
        _target = target;
    }

    public void ClearTarget()
    {
        _target = null;
    }

    public void StopAttacking()
    {
        _target = null;
    }
}