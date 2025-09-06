using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerActionSystem : NetworkBehaviour
{
    private PlayerCore _core;
    private Coroutine _currentAction;
    private bool _isPerformingAction;
    private PlayerAction _currentActionType;
    private ISkill _currentSkill;
    private bool _isCasting;
    private GameObject _currentTargetIndicator; // Индикатор цели атаки
    public bool IsPerformingAction => _isPerformingAction;
    public PlayerAction CurrentAction => _currentActionType;
    public ISkill CurrentSkill => _currentSkill;
    public GameObject CurrentTarget => _core?.Combat?.Target;
    public Vector3? CurrentTargetPosition { get; private set; }

    public void Init(PlayerCore core)
    {
        _core = core;
        if (_core == null)
        {
            Debug.LogError("[PlayerActionSystem] PlayerCore is null during initialization!");
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            UpdateTargetIndicator(); // Обновляем индикатор цели атаки
        }
    }

    private void OnDisable()
    {
        CompleteAction();
        ClearTargetIndicator();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        CompleteAction();
        ClearTargetIndicator();
        Debug.Log("[PlayerActionSystem] Cleaned up on client disconnect.");
    }

    private int GetPriority(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.Move: return 1;
            case PlayerAction.Attack: return 2;
            case PlayerAction.SkillCast: return 3;
            default: return 0;
        }
    }

    public bool TryStartAction(PlayerAction actionType, Vector3? targetPosition = null, GameObject targetObject = null, ISkill skillToCast = null)
    {
        Debug.Log($"[PlayerActionSystem] Trying to start new action: {actionType}, isOwned: {isOwned}");
        if (_core == null)
        {
            Debug.LogError("[PlayerActionSystem] _core is null!");
            return false;
        }
        bool canInterruptAndStart = true;
        if (actionType == PlayerAction.SkillCast)
        {
            if (!_core.CanCastSkill(skillToCast))
            {
                Debug.LogWarning("[PlayerActionSystem] Cannot cast skill: player is dead, stunned, or silenced");
                return false;
            }
            if (skillToCast == null)
            {
                canInterruptAndStart = false;
            }
            else
            {
                var skillBase = (SkillBase)skillToCast;
                bool isTargeted = skillBase.SkillCastType == SkillBase.CastType.TargetedEnemy || skillBase.SkillCastType == SkillBase.CastType.TargetedAlly;
                if (!isTargeted && targetPosition.HasValue)
                {
                    canInterruptAndStart = true;
                }
                else if (isTargeted && targetObject != null)
                {
                    canInterruptAndStart = true;
                }
                else
                {
                    Debug.LogWarning($"[PlayerActionSystem] Invalid SkillCast parameters for {skillBase.SkillName}: AoE needs targetPosition, targeted needs targetObject");
                    canInterruptAndStart = false;
                }
            }
        }
        else if (actionType == PlayerAction.Move && !targetPosition.HasValue)
        {
            canInterruptAndStart = false;
        }
        else if (actionType == PlayerAction.Attack && targetObject == null)
        {
            canInterruptAndStart = false;
        }
        if (!canInterruptAndStart)
        {
            Debug.LogWarning($"[PlayerActionSystem] Cannot start action {actionType}: invalid parameters or configuration");
            return false;
        }
        if (_isPerformingAction)
        {
            int newPriority = GetPriority(actionType);
            int currentPriority = GetPriority(_currentActionType);
            if (actionType == PlayerAction.Move && _isCasting)
            {
                Debug.Log($"[PlayerActionSystem] Ignoring Move: player is casting a skill");
                return false;
            }
            if (actionType == PlayerAction.Move && _currentActionType == PlayerAction.SkillCast)
            {
                if (_currentSkill != null)
                {
                    var skillBase = (SkillBase)_currentSkill;
                    bool isTargeted = skillBase.SkillCastType == SkillBase.CastType.TargetedEnemy || skillBase.SkillCastType == SkillBase.CastType.TargetedAlly;
                    if (isTargeted)
                    {
                        Debug.Log($"[PlayerActionSystem] Ignoring Move: current action is targeted SkillCast");
                        return false;
                    }
                }
            }
            if (newPriority >= currentPriority || actionType == PlayerAction.Move)
            {
                Debug.Log($"[PlayerActionSystem] Interrupting {_currentActionType} for higher/equal priority or Move: {actionType}");
                CompleteAction();
            }
            else
            {
                Debug.Log($"[PlayerActionSystem] Ignoring {actionType}: lower priority than {_currentActionType}");
                return false;
            }
        }
        _isPerformingAction = true;
        _currentActionType = actionType;
        switch (actionType)
        {
            case PlayerAction.Move:
                _currentAction = StartCoroutine(MoveAction(targetPosition.Value));
                return true;
            case PlayerAction.Attack:
                _currentAction = StartCoroutine(AttackAction(targetObject, skillToCast));
                return true;
            case PlayerAction.SkillCast:
                _currentSkill = skillToCast;
                if (targetObject != null)
                {
                    _currentAction = StartCoroutine(CastSkillAction(targetObject, skillToCast));
                }
                else
                {
                    CurrentTargetPosition = targetPosition.Value;
                    _currentAction = StartCoroutine(CastSkillAction(targetPosition.Value, skillToCast));
                }
                return true;
        }
        _isPerformingAction = false;
        _currentActionType = PlayerAction.None;
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

    private IEnumerator AttackAction(GameObject target, ISkill skill = null)
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
        _core.Combat.SetCurrentTarget(target);
        Debug.Log($"[PlayerActionSystem] Starting AttackAction on target: {target.name}, has NetworkIdentity: {target.GetComponent<NetworkIdentity>() != null}");
        PlayerCore targetPlayerCore = target.GetComponent<PlayerCore>();
        Monster targetMonster = target.GetComponent<Monster>();
        if (targetPlayerCore == null && targetMonster == null)
        {
            Debug.LogError($"[PlayerActionSystem] Target {target.name} has neither PlayerCore nor Monster component");
            CompleteAction();
            yield break;
        }
        if (targetPlayerCore != null && targetPlayerCore.team == _core.team)
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
        if (skill == null)
        {
            skill = _core.Skills.skills[0];
            if (skill == null)
            {
                Debug.LogError("[PlayerActionSystem] Basic attack skill is null");
                CompleteAction();
                yield break;
            }
        }
        _currentSkill = skill;
        float attackRange = skill.Range;
        float attackCooldown = skill is BasicAttackSkill ? 1f / _core.Stats.attackSpeed : 0f;
        bool isLooping = skill is BasicAttackSkill;
        PlayerAnimationSystem animationSystem = GetComponent<PlayerAnimationSystem>();
        while (target != null && targetHealth.CurrentHealth > 0)
        {
            if (_core.isDead || _core.isStunned)
            {
                Debug.Log("[PlayerActionSystem] Attack stopped: player is dead or stunned");
                CompleteAction();
                yield break;
            }
            float distance = Vector3.Distance(transform.position, target.transform.position);
            Debug.Log($"[PlayerActionSystem] Distance to target {target.name}: {distance}, skill range: {attackRange}");
            if (distance > attackRange)
            {
                _core.Movement.MoveTo(target.transform.position);
                _core.Movement.UpdateRotation();
                Debug.Log($"[PlayerActionSystem] Target out of range. Moving towards target at {target.transform.position}. Distance: {distance}");
            }
            else
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(target.transform.position - transform.position);
                Debug.Log($"[PlayerActionSystem] Target in range. Stopping to attack. Distance: {distance}");
                if (Time.time < _core.Combat._lastAttackTime + attackCooldown)
                {
                    yield return null;
                    continue;
                }
                if (!_core.CanCastSkill(skill))
                {
                    Debug.Log($"[PlayerActionSystem] Cannot execute skill {((SkillBase)skill).SkillName}: player is dead, stunned, or silenced");
                    CompleteAction();
                    yield break;
                }
                Debug.Log($"[PlayerActionSystem] Executing attack with skill: {((SkillBase)skill).SkillName}");
                skill.Execute(_core, null, target);
                _core.Combat._lastAttackTime = Time.time;
                if (animationSystem != null)
                {
                    animationSystem.ResetAnimations();
                }
                if (!isLooping)
                {
                    if (((SkillBase)skill).CastTime > 0)
                    {
                        _isCasting = true;
                        yield return new WaitForSeconds(((SkillBase)skill).CastTime);
                        _isCasting = false;
                    }
                    break;
                }
                else
                {
                    if (animationSystem != null)
                    {
                        animationSystem.TriggerAttackAnimation();
                    }
                    yield return new WaitForSeconds(attackCooldown);
                }
            }
            yield return null;
        }
        Debug.Log($"[PlayerActionSystem] Attack action completed: target is null or dead");
        CompleteAction();
    }

    private IEnumerator CastSkillAction(GameObject targetObject, ISkill skillToCast)
    {
        if (_core == null || _core.Movement == null)
        {
            Debug.LogError("[PlayerActionSystem] Cannot perform CastSkillAction: _core or Movement is null");
            CompleteAction();
            yield break;
        }
        if (targetObject == null)
        {
            Debug.LogError("[PlayerActionSystem] Target object is null in CastSkillAction");
            CompleteAction();
            yield break;
        }
        _core.Combat.SetCurrentTarget(targetObject);
        float originalStoppingDistance = _core.Movement.Agent.stoppingDistance;
        _core.Movement.Agent.stoppingDistance = 0f;
        const float castRangeOffset = 0.2f;
        while (true)
        {
            if (_core.isDead || _core.isStunned || (_core.isSilenced && !(skillToCast is BasicAttackSkill)))
            {
                Debug.Log("[PlayerActionSystem] Skill cast stopped: player is dead, stunned, or silenced (and not using BasicAttackSkill)");
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            float distance = Vector3.Distance(transform.position, targetObject.transform.position);
            float effectiveRange = skillToCast.Range - castRangeOffset;
            if (distance <= effectiveRange)
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(targetObject.transform.position - transform.position);
                skillToCast.Execute(_core, targetObject.transform.position, targetObject);
                if (((SkillBase)skillToCast).CastTime > 0)
                {
                    _isCasting = true;
                    yield return new WaitForSeconds(((SkillBase)skillToCast).CastTime);
                    _isCasting = false;
                }
                _core.Skills.CancelSkillSelection();
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            else
            {
                _core.Movement.MoveTo(targetObject.transform.position);
                _core.Movement.UpdateRotation();
            }
            yield return null;
        }
    }

    private IEnumerator CastSkillAction(Vector3 targetPosition, ISkill skillToCast)
    {
        if (_core == null || _core.Movement == null)
        {
            Debug.LogError("[PlayerActionSystem] Cannot perform CastSkillAction: _core or Movement is null");
            CompleteAction();
            yield break;
        }
        float originalStoppingDistance = _core.Movement.Agent.stoppingDistance;
        _core.Movement.Agent.stoppingDistance = 0f;
        const float castRangeOffset = 0.2f;
        while (true)
        {
            if (_core.isDead || _core.isStunned || (_core.isSilenced && !(skillToCast is BasicAttackSkill)))
            {
                Debug.Log("[PlayerActionSystem] Skill cast stopped: player is dead, stunned, or silenced (and not using BasicAttackSkill)");
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            float distance = Vector3.Distance(transform.position, targetPosition);
            float effectiveRange = skillToCast.Range - castRangeOffset;
            if (distance <= effectiveRange)
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(targetPosition - transform.position);
                skillToCast.Execute(_core, targetPosition, null);
                if (((SkillBase)skillToCast).CastTime > 0)
                {
                    _isCasting = true;
                    yield return new WaitForSeconds(((SkillBase)skillToCast).CastTime);
                    _isCasting = false;
                }
                _core.Skills.CancelSkillSelection();
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            else
            {
                _core.Movement.MoveTo(targetPosition);
                _core.Movement.UpdateRotation();
            }
            yield return null;
        }
    }

    public void CompleteAction()
    {
        Debug.Log($"[PlayerActionSystem] Completing action {_currentActionType}");
        _isPerformingAction = false;
        _currentActionType = PlayerAction.None;
        _currentSkill = null;
        _isCasting = false;
        CurrentTargetPosition = null;
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
        GetComponent<PlayerAnimationSystem>()?.ResetAnimations();
        ClearTargetIndicator();
    }

    [Client]
    private void UpdateTargetIndicator()
    {
        if (CurrentTarget != null && _core.GetTargetIndicatorPrefab() != null)
        {
            if (_currentTargetIndicator == null)
            {
                _currentTargetIndicator = Instantiate(_core.GetTargetIndicatorPrefab(), CurrentTarget.transform.position + Vector3.up * 2f, Quaternion.identity);
                Debug.Log($"[PlayerActionSystem] Spawned target indicator for {CurrentTarget.name}");
            }
            _currentTargetIndicator.transform.position = CurrentTarget.transform.position + Vector3.up * 1f;
        }
        else
        {
            ClearTargetIndicator();
        }
    }

    [Client]
    private void ClearTargetIndicator()
    {
        if (_currentTargetIndicator != null)
        {
            Destroy(_currentTargetIndicator);
            _currentTargetIndicator = null;
            Debug.Log("[PlayerActionSystem] Destroyed target indicator");
        }
    }
}