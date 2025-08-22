using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerCore _core;
    private GameObject _target;
    private float _lastAttackTime = -Mathf.Infinity;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 10;

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    public void HandleCombat()
    {
        if (_core.ActionSystem.CurrentAction != PlayerAction.Attack) return;
        if (_target == null) return;
    }

    [Server]
    public void PerformAttack()
    {
        if (_target == null) return;

        PlayerCore attackerCore = GetComponent<PlayerCore>();
        PlayerCore targetCore = _target.GetComponent<PlayerCore>();

        if (targetCore != null && attackerCore != null)
        {
            // Проверка на принадлежность к команде
            if (attackerCore.team == targetCore.team)
            {
                Debug.Log("Cannot attack a teammate!");
                return;
            }
        }

        if (Vector3.Distance(transform.position, _target.transform.position) <= attackRange)
        {
            Health targetHealth = _target.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(attackDamage);
            }
        }
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