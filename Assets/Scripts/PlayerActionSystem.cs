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
        if (_core == null)
        {
            Debug.LogError("[PlayerActionSystem] PlayerCore is null during initialization!");
        }
    }

    private void OnDisable()
    {
        CompleteAction();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        CompleteAction();
        Debug.Log("[PlayerActionSystem] Cleaned up on client disconnect.");
    }

    public bool TryStartAction(PlayerAction actionType, Vector3? targetPosition = null, GameObject targetObject = null, ISkill skillToCast = null)
    {
        Debug.Log($"[PlayerActionSystem] Trying to start new action: {actionType}, isOwned: {isOwned}");
        if (_isPerformingAction)
        {
            Debug.Log($"[PlayerActionSystem] An action is already being performed ({_currentActionType}). Completing it now.");
            CompleteAction();
        }

        if (_core == null)
        {
            Debug.LogError("[PlayerActionSystem] _core is null!");
            return false;
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
                    bool isAoESkill = !(skillToCast is ProjectileDamageSkill || skillToCast is TargetedStunSkill || skillToCast is SlowSkill);
                    if (isAoESkill && targetPosition.HasValue)
                    {
                        _currentAction = StartCoroutine(CastSkillAction(targetPosition.Value, skillToCast));
                    }
                    else
                    {
                        _currentAction = StartCoroutine(_core.Skills.CastSkill(targetPosition, targetObject, skillToCast));
                    }
                    return true;
                }
                break;
        }

        Debug.LogWarning($"[PlayerActionSystem] Failed to start action {actionType}: invalid parameters");
        return false;
    }

    private IEnumerator MoveAction(Vector3 destination)
    {
        if (_core == null || _core.Movement == null || _core.Movement.Agent == null)
        {
            Debug.LogError("[PlayerActionSystem] Cannot perform MoveAction: _core, Movement, or Agent is null");
            CompleteAction();
            yield break;
        }

        _core.Combat.ClearTarget();
        _core.Movement.MoveTo(destination);
        Debug.Log($"[PlayerActionSystem] Moving to destination: {destination}");

        yield return new WaitUntil(() => !_core.Movement.Agent.pathPending);

        while (_core.Movement.Agent.remainingDistance > _core.Movement.Agent.stoppingDistance)
        {
            if (_core.isDead || _core.isStunned)
            {
                Debug.Log("[PlayerActionSystem] Movement stopped: player is dead or stunned");
                CompleteAction();
                yield break;
            }
            _core.Movement.UpdateRotation();
            yield return null;
        }
        Debug.Log($"[PlayerActionSystem] Movement action completed.");
        CompleteAction();
    }

    private IEnumerator AttackAction(GameObject target)
    {
        if (_core == null || _core.Movement == null || _core.Combat == null || _core.Stats == null || _core.Skills == null)
        {
            Debug.LogError("[PlayerActionSystem] Cannot perform AttackAction: _core, Movement, Combat, Stats, or Skills is null");
            CompleteAction();
            yield break;
        }

        if (target == null)
        {
            Debug.LogError("[PlayerActionSystem] Target is null in AttackAction");
            CompleteAction();
            yield break;
        }

        Debug.Log($"[PlayerActionSystem] Starting AttackAction on target: {target.name}, has NetworkIdentity: {target.GetComponent<NetworkIdentity>() != null}");

        PlayerCore targetCore = target.GetComponent<PlayerCore>();
        if (targetCore == null)
        {
            Debug.LogError($"[PlayerActionSystem] Target {target.name} has no PlayerCore component");
            CompleteAction();
            yield break;
        }

        if (targetCore.team == _core.team)
        {
            Debug.Log($"[PlayerActionSystem] Attack ignored: target {target.name} is on the same team");
            CompleteAction();
            yield break;
        }

        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth == null)
        {
            Debug.LogError($"[PlayerActionSystem] Target {target.name} has no Health component");
            CompleteAction();
            yield break;
        }

        if (_core.Skills.skills.Count == 0)
        {
            Debug.LogWarning($"[PlayerActionSystem] No basic attack skill available. Stopping attack.");
            CompleteAction();
            yield break;
        }

        SkillBase basicAttackSkill = _core.Skills.skills[0];
        if (basicAttackSkill == null)
        {
            Debug.LogError($"[PlayerActionSystem] Basic attack skill is null");
            CompleteAction();
            yield break;
        }

        float attackCooldown = 1f / _core.Stats.attackSpeed;

        while (target != null && targetHealth.CurrentHealth > 0)
        {
            if (_core.isDead || _core.isStunned)
            {
                Debug.Log("[PlayerActionSystem] Attack stopped: player is dead or stunned");
                CompleteAction();
                yield break;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            Debug.Log($"[PlayerActionSystem] Distance to target {target.name}: {distance}, skill range: {basicAttackSkill.Range}");

            if (distance > basicAttackSkill.Range)
            {
                _core.Movement.MoveTo(target.transform.position);
                Debug.Log($"[PlayerActionSystem] Target out of range. Moving towards target at {target.transform.position}. Distance: {distance}");
            }
            else
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(target.transform.position - transform.position);
                Debug.Log($"[PlayerActionSystem] Target in range. Stopping to attack. Distance: {distance}");

                if (Time.time >= _core.Combat._lastAttackTime + attackCooldown)
                {
                    Debug.Log($"[PlayerActionSystem] Executing attack with skill: {basicAttackSkill.SkillName}");
                    basicAttackSkill.Execute(_core, null, target);
                    _core.Combat._lastAttackTime = Time.time;
                    yield return new WaitForSeconds(attackCooldown);
                }
            }

            yield return null;
        }

        Debug.Log($"[PlayerActionSystem] Attack action completed: target is null or dead");
        CompleteAction();
    }

    private IEnumerator CastSkillAction(Vector3 targetPosition, ISkill skillToCast)
    {
        if (_core == null || _core.Movement == null)
        {
            Debug.LogError("[PlayerActionSystem] Cannot perform CastSkillAction: _core or Movement is null");
            CompleteAction();
            yield break;
        }

        while (true)
        {
            if (_core.isDead || _core.isStunned)
            {
                Debug.Log("[PlayerActionSystem] Skill cast stopped: player is dead or stunned");
                CompleteAction();
                yield break;
            }

            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= skillToCast.Range)
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(targetPosition - transform.position);
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

    public void CompleteAction()
    {
        Debug.Log($"[PlayerActionSystem] Completing action {_currentActionType}");
        _isPerformingAction = false;
        _currentActionType = PlayerAction.None;
        if (_currentAction != null)
        {
            StopCoroutine(_currentAction);
            _currentAction = null;
        }
        if (_core != null && _core.Combat != null)
        {
            _core.Combat.ClearTarget();
        }
        if (_core != null && _core.Movement != null)
        {
            _core.Movement.StopMovement();
        }
    }
}