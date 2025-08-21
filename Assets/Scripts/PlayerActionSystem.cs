using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class PlayerActionSystem : NetworkBehaviour
{
    private PlayerCore _core;
    private Coroutine _currentAction;
    private bool _isPerformingAction;

    public bool CanStartNewAction => !_isPerformingAction;

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    public bool TryStartAction(PlayerAction actionType, Vector3? targetPosition = null, GameObject targetObject = null, ISkill skillToCast = null)
    {
        Debug.Log($"[ActionSystem] Attempting to start action: {actionType}. IsPerformingAction: {_isPerformingAction}");
        if (_isPerformingAction)
        {
            return false;
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
                _currentAction = StartCoroutine(AttackAction());
                return true;
            case PlayerAction.SkillCast:
                _currentAction = StartCoroutine(SkillCastAction(targetPosition, targetObject, skillToCast));
                return true;
        }
        return false;
    }

    private IEnumerator MoveAction(Vector3 destination)
    {
        _isPerformingAction = true;
        _core.Movement.MoveTo(destination);

        yield return new WaitUntil(() => !_core.Movement.Agent.pathPending);

        while (_core.Movement.Agent.remainingDistance > _core.Movement.Agent.stoppingDistance)
        {
            if (_core.isDead || _core.isStunned)
            {
                _core.Movement.StopMovement();
                CompleteAction();
                yield break;
            }
            yield return null;
        }

        _core.Movement.StopMovement();
        CompleteAction();
    }

    private IEnumerator AttackAction()
    {
        Debug.Log("[ActionSystem] Starting AttackAction coroutine.");
        _isPerformingAction = true;
        _core.Combat.StartAttack();
        // 🚨 ИСПРАВЛЕНО: Убрано лишнее ожидание. Combat сам вызовет CompleteAction.
        yield break;
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
        Debug.Log("[ActionSystem] Completing action.");
        _isPerformingAction = false;
        if (_currentAction != null)
        {
            StopCoroutine(_currentAction);
            _currentAction = null;
        }
    }
}