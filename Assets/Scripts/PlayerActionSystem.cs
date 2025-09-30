using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.AI;
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
        // Cleaned up on client disconnect
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
        // Trying to start new action
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
                bool isSelf = skillBase.SkillCastType == SkillBase.CastType.SelfBuff || skillBase.SkillCastType == SkillBase.CastType.ToggleBuff;
                
                if (isSelf && targetObject != null)
                {
                    canInterruptAndStart = true;
                }
                else if (!isTargeted && targetPosition.HasValue)
                {
                    canInterruptAndStart = true;
                }
                else if (isTargeted && targetObject != null)
                {
                    canInterruptAndStart = true;
                }
                else
                {
                    Debug.LogWarning($"[PlayerActionSystem] Invalid SkillCast parameters for {skillBase.SkillName}: SelfBuff needs targetObject, AoE needs targetPosition, targeted needs targetObject");
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
        // Добавь: прерывание невидимости при начале атаки/каста
        if ((actionType == PlayerAction.Attack || actionType == PlayerAction.SkillCast) && _core.Skills.toggleBuffStates.ContainsKey("Invisibility") && _core.Skills.toggleBuffStates["Invisibility"])
        {
            _core.Skills.CmdInterruptInvisibility();
            // Interrupted invisibility
        }
        if (_isPerformingAction)
        {
            int newPriority = GetPriority(actionType);
            int currentPriority = GetPriority(_currentActionType);
            if (actionType == PlayerAction.Move && _isCasting)
            {
                // Ignoring Move - player is casting
                return false;
            }
            // Разрешаем Move или новый Attack прерывать текущий Attack
            if (_currentActionType == PlayerAction.Attack && (actionType == PlayerAction.Move || actionType == PlayerAction.Attack))
            {
                // Allowing action to interrupt Attack
                CompleteAction();
            }
            // Разрешаем Move прерывать SkillCast (если не casting)
            else if (actionType == PlayerAction.Move && _currentActionType == PlayerAction.SkillCast)
            {
                // Allowing Move to interrupt SkillCast
                CompleteAction();
            }
            else if (newPriority >= currentPriority)
            {
                // Interrupting action for higher priority
                CompleteAction();
            }
            else
            {
                // Ignoring action: lower priority
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
                var skillBase = (SkillBase)skillToCast;
                bool isSelf = skillBase.SkillCastType == SkillBase.CastType.SelfBuff || skillBase.SkillCastType == SkillBase.CastType.ToggleBuff;
                
                if (isSelf && targetObject != null)
                {
                    // SelfBuff выполняется мгновенно, но прерывает текущие действия
                    _currentAction = StartCoroutine(CastSelfBuffAction(targetObject, skillToCast));
                }
                else if (targetObject != null)
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
        // Moving to destination
        yield return new WaitUntil(() => !_core.Movement.Agent.pathPending);
        while (_core.Movement.Agent.remainingDistance > _core.Movement.Agent.stoppingDistance)
        {
            if (_core.isDead || _core.isStunned)
            {
                // Movement stopped: player is dead or stunned
                CompleteAction();
                yield break;
            }
            _core.Movement.UpdateRotation();
            yield return null;
        }
        // Movement action completed
        CompleteAction();
    }

    private IEnumerator CastSelfBuffAction(GameObject targetObject, ISkill skillToCast)
    {
        if (_core == null)
        {
            Debug.LogError("[PlayerActionSystem] Cannot perform CastSelfBuffAction: _core is null");
            CompleteAction();
            yield break;
        }

        // SelfBuff выполняется мгновенно, но прерывает текущие действия
        Debug.Log($"[PlayerActionSystem] Executing SelfBuff: {((SkillBase)skillToCast).SkillName}");
        
        // Останавливаем движение
        _core.Movement.StopMovement();
        
        // Выполняем скилл
        _core.Skills.CmdExecuteSkill(_core, null, targetObject.GetComponent<NetworkIdentity>().netId, skillToCast.SkillName, ((SkillBase)skillToCast).Weight);
        
        // Отменяем выбор скилла
        _core.Skills.CancelSkillSelection();
        
        // Завершаем действие
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
        // Starting AttackAction on target
        PlayerCore targetPlayerCore = target.GetComponent<PlayerCore>();
        Monster targetMonster = target.GetComponent<Monster>();
        if (targetPlayerCore == null)
        {
            // Try to get PlayerCore from parent (for reviveCollider)
            targetPlayerCore = target.GetComponentInParent<PlayerCore>();
        }
        if (targetPlayerCore == null && targetMonster == null)
        {
            Debug.LogError($"[PlayerActionSystem] Target {target.name} has neither PlayerCore nor Monster component");
            CompleteAction();
            yield break;
        }
        if (targetPlayerCore != null && targetPlayerCore.team == _core.team)
        {
            // Attack ignored: same team
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
            // Проверка невидимости цели
            if (targetPlayerCore != null && targetPlayerCore.Skills._isInvisible)
            {
                Debug.Log($"[PlayerActionSystem] Attack stopped: target {target.name} is invisible");
                CompleteAction();
                yield break;
            }
            if (_core.isDead || _core.isStunned)
            {
                Debug.Log("[PlayerActionSystem] Attack stopped: player is dead or stunned");
                CompleteAction();
                yield break;
            }
            float distance = Vector3.Distance(transform.position, target.transform.position); // Full distance with Y
            Debug.Log($"[PlayerActionSystem] Distance to target {target.name}: {distance}, skill range: {attackRange}");
            if (distance > attackRange)
            {
                Vector3 direction = (target.transform.position - transform.position).normalized;
                Vector3 tempPos = transform.position + direction * 1f; // Step size
                tempPos.y = transform.position.y; // Уровень агента
                NavMeshHit hit;
                if (NavMesh.SamplePosition(tempPos, out hit, 1f, NavMesh.AllAreas))
                {
                    _core.Movement.MoveTo(hit.position);
                    _core.Movement.UpdateRotation();
                    Debug.Log($"[PlayerActionSystem] No full path. Manual step to {hit.position}");
                }
                else
                {
                    Debug.Log($"[PlayerActionSystem] No NavMesh for manual step to target {target.name}. Stopping attack.");
                    CompleteAction();
                    yield break;
                }
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
                _core.Skills.CmdExecuteSkill(_core, null, target.GetComponent<NetworkIdentity>().netId, skill.SkillName, ((SkillBase)skill).Weight);
                _core.Combat._lastAttackTime = Time.time;
                if (!isLooping)
                {
                    if (((SkillBase)skill).CastTime > 0)
                    {
                        _isCasting = true;
                        yield return new WaitForSeconds(((SkillBase)skill).CastTime);
                        _isCasting = false;
                    }
                    // Удаляем CancelSkillSelection для BasicAttackSkill
                    if (!(skill is BasicAttackSkill))
                    {
                        _core.Skills.CancelSkillSelection();
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
            if (_core.Movement.Agent.hasPath && _core.Movement.Agent.remainingDistance <= _core.Movement.Agent.stoppingDistance && distance > attackRange)
            {
                Debug.Log($"[PlayerActionSystem] Cannot get closer to target {target.name}. Stopping attack.");
                CompleteAction();
                yield break;
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
        _core.Skills.CancelSkillSelection(); // Закрываем режим сразу после начала
        while (true)
        {
            if (_core.isDead || _core.isStunned || (_core.isSilenced && !(skillToCast is BasicAttackSkill)))
            {
                Debug.Log("[PlayerActionSystem] Skill cast stopped: player is dead, stunned, or silenced (and not using BasicAttackSkill)");
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            PlayerCore targetPlayerCore = targetObject.GetComponent<PlayerCore>();
            if (targetPlayerCore != null && targetPlayerCore.Skills._isInvisible)
            {
                Debug.Log($"[PlayerActionSystem] Skill cast stopped: target {targetObject.name} is invisible");
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            float distance = Vector3.Distance(transform.position, targetObject.transform.position); // Full distance with Y
            float effectiveRange = skillToCast.Range - castRangeOffset;
            if (distance <= effectiveRange)
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(targetObject.transform.position - transform.position);
                NetworkIdentity targetNetId = targetObject.GetComponent<NetworkIdentity>();
                if (targetNetId == null)
                {
                    // Try to get NetworkIdentity from parent (for reviveCollider)
                    targetNetId = targetObject.GetComponentInParent<NetworkIdentity>();
                }
                if (targetNetId != null)
                {
                    _core.Skills.CmdExecuteSkill(_core, targetObject.transform.position, targetNetId.netId, skillToCast.SkillName, ((SkillBase)skillToCast).Weight);
                }
                else
                {
                    Debug.LogError($"[PlayerActionSystem] No NetworkIdentity found on target: {targetObject.name}");
                }
                if (((SkillBase)skillToCast).CastTime > 0)
                {
                    _isCasting = true;
                    yield return new WaitForSeconds(((SkillBase)skillToCast).CastTime);
                    _isCasting = false;
                }
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
            }
            else
            {
                Vector3 direction = (targetObject.transform.position - transform.position).normalized;
                Vector3 tempPos = transform.position + direction * 1f; // Step size
                tempPos.y = transform.position.y; // Уровень агента
                NavMeshHit hit;
                if (NavMesh.SamplePosition(tempPos, out hit, 1f, NavMesh.AllAreas))
                {
                    _core.Movement.MoveTo(hit.position);
                    _core.Movement.UpdateRotation();
                    Debug.Log($"[PlayerActionSystem] No full path. Manual step to {hit.position}");
                }
                else
                {
                    Debug.Log($"[PlayerActionSystem] No NavMesh for manual step to target {targetObject.name}. Stopping cast.");
                    _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                    CompleteAction();
                    yield break;
                }
            }
            if (_core.Movement.Agent.hasPath && _core.Movement.Agent.remainingDistance <= _core.Movement.Agent.stoppingDistance && distance > effectiveRange)
            {
                Debug.Log($"[PlayerActionSystem] Cannot get closer to target {targetObject.name}. Stopping cast.");
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
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
            float distance = Vector3.Distance(transform.position, targetPosition); // Full distance with Y
            float effectiveRange = skillToCast.Range - castRangeOffset;
            if (distance <= effectiveRange)
            {
                _core.Movement.StopMovement();
                _core.Movement.RotateTo(targetPosition - transform.position);
                _core.Skills.CmdExecuteSkill(_core, targetPosition, 0, skillToCast.SkillName, ((SkillBase)skillToCast).Weight);
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
                Vector3 direction = (targetPosition - transform.position).normalized;
                Vector3 tempPos = transform.position + direction * 1f; // Step size
                tempPos.y = transform.position.y; // Уровень агента
                NavMeshHit hit;
                if (NavMesh.SamplePosition(tempPos, out hit, 1f, NavMesh.AllAreas))
                {
                    _core.Movement.MoveTo(hit.position);
                    _core.Movement.UpdateRotation();
                    Debug.Log($"[PlayerActionSystem] No full path. Manual step to {hit.position}");
                }
                else
                {
                    Debug.Log($"[PlayerActionSystem] No NavMesh for manual step to target position {targetPosition}. Stopping cast.");
                    _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                    CompleteAction();
                    yield break;
                }
            }
            if (_core.Movement.Agent.hasPath && _core.Movement.Agent.remainingDistance <= _core.Movement.Agent.stoppingDistance && distance > effectiveRange)
            {
                Debug.Log($"[PlayerActionSystem] Cannot get closer to target position {targetPosition}. Stopping cast.");
                _core.Movement.Agent.stoppingDistance = originalStoppingDistance;
                CompleteAction();
                yield break;
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
            _currentTargetIndicator.transform.position = CurrentTarget.transform.position + Vector3.up * 2f;
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