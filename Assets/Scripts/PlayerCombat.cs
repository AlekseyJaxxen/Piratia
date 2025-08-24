using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerCore _core;
    private GameObject _target;
    [HideInInspector]
    public float _lastAttackTime = -Mathf.Infinity;

    // The following settings are now obsolete and managed by CharacterStats.cs
    // [Header("Combat Settings")]
    // public float attackRange = 2.5f;
    // public float attackCooldown = 1.0f;
    // public int attackDamage = 10;

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