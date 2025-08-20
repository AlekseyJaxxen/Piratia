using UnityEngine;
using System.Collections;

public class PlayerActionSystem : MonoBehaviour
{
    private PlayerCore _core;
    private Coroutine _currentAction;
    private bool _isPerformingAction;

    public bool CanStartNewAction => !_isPerformingAction;

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    public bool TryStartAction(PlayerAction actionType, Vector3? targetPosition = null)
    {
        if (_isPerformingAction) return false;

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
                if (targetPosition.HasValue)
                {
                    _currentAction = StartCoroutine(SkillCastAction(targetPosition.Value));
                    return true;
                }
                break;
        }
        return false;
    }

    private IEnumerator MoveAction(Vector3 destination)
    {
        _isPerformingAction = true;
        _core.Movement.MoveTo(destination);

        // 🚨 ИЗМЕНЕНО: Новая, более надежная логика ожидания
        // Ждем, пока агент не начнет двигаться и не приблизится к цели
        while (_core.Movement.HasPath && Vector3.Distance(transform.position, destination) > _core.Movement.Agent.stoppingDistance)
        {
            // Условие выхода в случае смерти или стана
            if (_core.isDead || _core.isStunned)
            {
                _core.Movement.StopMovement();
                _isPerformingAction = false;
                yield break;
            }
            yield return null;
        }

        Debug.Log("[Client] Movement action completed.");
        _isPerformingAction = false;
        _currentAction = null;
    }

    private IEnumerator AttackAction()
    {
        _isPerformingAction = true;
        _core.Combat.StartAttack();
        yield return new WaitUntil(() => !_core.Combat.IsAttacking || _core.isDead || _core.isStunned || !_core.ActionSystem.CanStartNewAction);
        _isPerformingAction = false;
        _currentAction = null;
    }

    private IEnumerator SkillCastAction(Vector3 targetPosition)
    {
        _isPerformingAction = true;
        if (_core != null && _core.Skills != null)
        {
            yield return StartCoroutine(_core.Skills.CastSkill(targetPosition));
        }
        else
        {
            Debug.LogWarning("PlayerCore or Skills component is null in SkillCastAction");
        }
        _isPerformingAction = false;
        _currentAction = null;
    }

    public void CompleteAction()
    {
        if (_currentAction != null)
        {
            StopCoroutine(_currentAction);
            _currentAction = null;
            _isPerformingAction = false;
            _core.Movement.StopMovement();
            _core.Combat.ClearTarget();
            _core.Skills.CancelSkillSelection();
        }
    }
}

public enum PlayerAction
{
    Move,
    Attack,
    SkillCast,
    Stunned
}