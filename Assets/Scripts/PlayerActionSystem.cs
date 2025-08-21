using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class PlayerActionSystem : NetworkBehaviour
{
    private PlayerCore _core;
    private Coroutine _currentAction;
    private bool _isPerformingAction;

    public bool IsPerformingAction => _isPerformingAction;

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    public bool TryStartAction(PlayerAction actionType, Vector3? targetPosition = null, GameObject targetObject = null, ISkill skillToCast = null)
    {
        if (_isPerformingAction)
        {
            CompleteAction();
        }

        switch (actionType)
        {
            case PlayerAction.Move:
                if (targetPosition.HasValue)
                {
                    _currentAction = StartCoroutine(MoveAction(targetPosition.Value));
                    return true;
                }
                break;
            case PlayerAction.Attack:
                if (targetObject != null)
                {
                    _currentAction = StartCoroutine(AttackAction(targetObject));
                    return true;
                }
                break;
            case PlayerAction.SkillCast:
                if (skillToCast != null)
                {
                    _currentAction = StartCoroutine(SkillCastAction(targetPosition, targetObject, skillToCast));
                    return true;
                }
                break;
        }

        return false;
    }

    private IEnumerator MoveAction(Vector3 destination)
    {
        _isPerformingAction = true;
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
        _isPerformingAction = true;
        _core.Combat.SetCurrentTarget(target);

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

        CompleteAction();
    }

    private IEnumerator SkillCastAction(Vector3? targetPosition, GameObject targetObject, ISkill skillToCast)
    {
        _isPerformingAction = true;

        if (_core != null && _core.Skills != null)
        {
            yield return StartCoroutine(_core.Skills.CastSkill(targetPosition, targetObject, skillToCast));
        }
        else
        {
            Debug.LogWarning("PlayerCore or Skills component is null in SkillCastAction");
        }
        CompleteAction();
    }

    public void CompleteAction()
    {
        _isPerformingAction = false;
        if (_currentAction != null)
        {
            StopCoroutine(_currentAction);
            _currentAction = null;
        }
        _core.Combat.ClearTarget();
        _core.Movement.StopMovement();
    }
}