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
    [SerializeField] private LayerMask Ground;

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
                    // Определяем, является ли навык AoE
                    bool isAoESkill = skillToCast is AreaOfEffectHealSkill || skillToCast is AreaOfEffectStunSkill;

                    if (isAoESkill && targetPosition.HasValue)
                    {
                        // Для AoE-навыков запускаем новую корутину, которая проверяет расстояние
                        _currentAction = StartCoroutine(CastSkillAction(targetPosition.Value, skillToCast));
                    }
                    else // Для всех остальных навыков (целевых)
                    {
                        // Запускаем старый, рабочий вариант
                        _currentAction = StartCoroutine(_core.Skills.CastSkill(targetPosition, targetObject, skillToCast));
                    }
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
        Debug.Log($"PlayerActionSystem: Moving to destination: {destination}");

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
        Debug.Log($"PlayerActionSystem: Movement action completed.");

        CompleteAction();
    }

    // Новая корутина для AoE-навыков
    private IEnumerator CastSkillAction(Vector3 targetPosition, ISkill skillToCast)
    {
        while (true)
        {
            if (_core.isDead || _core.isStunned)
            {
                CompleteAction();
                yield break;
            }

            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= skillToCast.Range)
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(targetPosition - transform.position);

                // Вызываем метод для каста навыка, когда персонаж в радиусе
                yield return StartCoroutine(_core.Skills.CastSkill(targetPosition, null, skillToCast));

                CompleteAction();
                yield break;
            }
            else
            {
                _core.Movement.MoveTo(targetPosition);
            }
            yield return null;
        }
    }

    private IEnumerator AttackAction(GameObject target)
    {
        while (target != null)
        {

            if (target == null)
            {
                Debug.Log("PlayerActionSystem: Target object is null. Stopping attack action.");
                CompleteAction();
                yield break;
            }

            if (_core.isDead || _core.isStunned)
            {
                CompleteAction();
                yield break;
            }

            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth == null)
            {
                Debug.Log($"PlayerActionSystem: Target lost or does not have Health component. Stopping attack.");
                break;
            }

            if (_core.Skills.skills.Count == 0)
            {
                Debug.Log($"PlayerActionSystem: No basic attack skill available. Stopping attack.");
                break;
            }
            SkillBase basicAttackSkill = _core.Skills.skills[0];

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance > basicAttackSkill.Range)
            {
                _core.Movement.MoveTo(target.transform.position);
                Debug.Log($"PlayerActionSystem: Target out of range. Moving towards target at {target.transform.position}. Distance: {distance}");
            }
            else
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(target.transform.position - transform.position);
                Debug.Log($"PlayerActionSystem: Target in range. Stopping to attack. Distance: {distance}");

                if (Time.time >= _core.Combat._lastAttackTime + basicAttackSkill.Cooldown)
                {
                    basicAttackSkill.Execute(_core, null, target);
                    _core.Combat._lastAttackTime = Time.time;
                    Debug.Log($"PlayerActionSystem: Executed attack on target {target.name}.");
                }
            }

            yield return null;
        }
        CompleteAction();
        Debug.Log($"PlayerActionSystem: Attack action completed.");
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