using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class PlayerActionSystem : NetworkBehaviour
{
    private PlayerCore _core;
    private Coroutine _currentAction;
    private bool _isPerformingAction;
    private PlayerAction _currentActionType;

    public bool IsPerformingAction => _isPerformingAction;
    public PlayerAction CurrentAction => _currentActionType;

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    public bool TryStartAction(PlayerAction actionType, Vector3? targetPosition = null, GameObject targetObject = null, ISkill skillToCast = null)
    {
        Debug.Log($"PlayerActionSystem: Trying to start new action: {actionType}"); // LOG
        // Отменяем предыдущее действие, если начинаем новое
        if (_isPerformingAction)
        {
            Debug.Log($"PlayerActionSystem: An action is already being performed ({_currentActionType}). Completing it now."); // LOG
            CompleteAction();
        }

        switch (actionType)
        {
            case PlayerAction.Move:
                _isPerformingAction = true;
                _currentActionType = PlayerAction.Move;
                if (targetPosition.HasValue)
                {
                    _currentAction = StartCoroutine(MoveAction(targetPosition.Value));
                    return true;
                }
                break;
            case PlayerAction.Attack:
                _isPerformingAction = true;
                _currentActionType = PlayerAction.Attack;
                if (targetObject != null)
                {
                    _currentAction = StartCoroutine(AttackAction(targetObject));
                    return true;
                }
                break;
            case PlayerAction.SkillCast:
                _isPerformingAction = true;
                _currentActionType = PlayerAction.SkillCast;
                if (skillToCast != null)
                {
                    _currentAction = StartCoroutine(_core.Skills.CastSkill(targetPosition, targetObject, skillToCast));
                    return true;
                }
                break;
        }

        return false;
    }

    private IEnumerator MoveAction(Vector3 destination)
    {
        _core.Combat.ClearTarget();
        _core.Movement.MoveTo(destination);

        yield return new WaitUntil(() => !_core.Movement.Agent.pathPending);

        while (_core.Movement.Agent.remainingDistance > _core.Movement.Agent.stoppingDistance)
        {
            if (_core.isDead || _core.isStunned)
            {
                CompleteAction();
                yield break;
            }
            _core.Movement.UpdateRotation();
            yield return null;
        }

        CompleteAction();
    }

    private IEnumerator AttackAction(GameObject target)
    {
        _core.Combat.SetCurrentTarget(target);
        Debug.Log($"PlayerActionSystem: Starting attack action on target {target.name}"); // LOG

        while (target != null && target.GetComponent<Health>()?.CurrentHealth > 0 && !_core.isDead && !_core.isStunned)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance > _core.Combat.attackRange)
            {
                _core.Movement.MoveTo(target.transform.position);
                _core.Movement.UpdateRotation();
            }
            else
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(target.transform.position - transform.position);
                _core.Combat.PerformAttack();
            }

            yield return new WaitForSeconds(_core.Combat.attackCooldown);
        }

        Debug.Log($"PlayerActionSystem: Attack action finished. Target is gone or state changed."); // LOG
        CompleteAction();
    }

    public void CompleteAction()
    {
        Debug.Log($"PlayerActionSystem: Completing action {_currentActionType}"); // LOG
        _isPerformingAction = false;
        _currentActionType = PlayerAction.None;
        if (_currentAction != null)
        {
            StopCoroutine(_currentAction);
            _currentAction = null;
        }
        _core.Combat.ClearTarget();
        _core.Movement.StopMovement();
    }
}