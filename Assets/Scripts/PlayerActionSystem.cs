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
        Debug.Log($"PlayerActionSystem: Trying to start new action: {actionType}");
        if (_isPerformingAction)
        {
            Debug.Log($"PlayerActionSystem: An action is already being performed ({_currentActionType}). Completing it now.");
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
            // Добавлена проверка состояния
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
        while (target != null)
        {
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth == null)
            {
                break;
            }

            if (_core.Skills.skills.Count == 0)
            {
                break;
            }
            SkillBase basicAttackSkill = _core.Skills.skills[0];

            // Используем свойство Range, заданное в самом навыке
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance > basicAttackSkill.Range)
            {
                _core.Movement.MoveTo(target.transform.position);
            }
            else
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(target.transform.position - transform.position);

                if (Time.time >= _core.Combat._lastAttackTime + _core.Combat.attackCooldown)
                {
                    basicAttackSkill.Execute(_core, null, target);
                    _core.Combat._lastAttackTime = Time.time;
                }
            }

            yield return null;
        }
    }

    public void CompleteAction()
    {
        Debug.Log($"PlayerActionSystem: Completing action {_currentActionType}");
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