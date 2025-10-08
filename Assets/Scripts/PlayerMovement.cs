using UnityEngine;
using UnityEngine.AI;
using Mirror;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [HideInInspector]
    public float moveSpeed = 8f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.5f;
    private NavMeshAgent _agent;
    public NavMeshAgent Agent => _agent;
    private PlayerCore _core;
    private GameObject _currentMoveIndicator;
    public bool IsMoving => _agent != null && _agent.velocity.magnitude > 0.1f;
    public void Init(PlayerCore core)
    {
        if (core == null)
        {
            // Debug.LogError("[PlayerMovement] Init failed: PlayerCore is null");
            return;
        }
        _core = core;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            // Debug.LogError("[PlayerMovement] NavMeshAgent component missing!");
            return;
        }
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.updateRotation = false;
        // PlayerMovement initialized
    }
    private void Update()
    {
        if (isLocalPlayer)
        {
            HandleMovement();
            UpdateMoveIndicator();
        }
    }
    private void HandleMovement()
    {
        if (_core == null)
        {
            // Debug.LogError("[PlayerMovement] HandleMovement failed: _core is null");
            return;
        }
        if (_core.isDead || _core.isStunned)
        {
            // Input ignored - player dead/stunned
            return;
        }
        if (!isLocalPlayer)
        {
            // Input ignored - not local player
            return;
        }
        if (!_core.netIdentity.isOwned)
        {
            // Input ignored - lacks authority
            return;
        }
        if (_core.Camera == null || _core.Camera.CameraInstance == null)
        {
            // Debug.LogError("[PlayerMovement] Camera or CameraInstance is null");
            return;
        }
        // Handle right-click for context menu
        if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverPlayerUI())
            {
                // Click ignored - over UI
                return;
            }
            HandleRightClick();
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverPlayerUI())
            {
                // Click ignored - over UI
                return;
            }
            // Left mouse button clicked
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            // Raycast from mouse position
            if (_core.Skills.IsSkillSelected)
            {
                // Skill selected
                var skill = (SkillBase)_core.Skills.ActiveSkill;
                bool isTargeted = skill.SkillCastType == SkillBase.CastType.TargetedEnemy || skill.SkillCastType == SkillBase.CastType.TargetedAlly;
                bool isSelf = skill.SkillCastType == SkillBase.CastType.SelfBuff || skill.SkillCastType == SkillBase.CastType.ToggleBuff;
                if (isSelf)
                {
                    // SelfBuff теперь проходит через PlayerActionSystem для прерывания действий
                    _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, _core.gameObject, skill);
                    _core.Skills.CancelSkillSelection();
                    return;
                }
                // Special case for ReviveSkill - prevent self-cast
                if (skill is ReviveSkill)
                {
                    // ReviveSkill cannot be cast on self, but allow targeting others
                    // Continue to targeted skill logic below
                }
                if (isTargeted)
                {
                    Debug.Log($"[PlayerMovement] Targeted skill selected: {skill.SkillName}, isTargeted: {isTargeted}");
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                    {
                        // Raycast hit
                        GameObject target = hit.collider.gameObject;
                        bool validTarget = false;
                        PlayerCore targetCore = target.GetComponentInParent<PlayerCore>();
                        Monster targetMonster = target.GetComponentInParent<Monster>();
                        Debug.Log($"[PlayerMovement] Skill cast raycast hit: {target.name}, skill: {skill.SkillName}, skillType: {skill.SkillCastType}");
                        Debug.Log($"[PlayerMovement] Target core: {targetCore?.name}, isDead: {targetCore?.isDead}, team: {targetCore?.team}, caster team: {_core.team}");
                        if (skill.SkillCastType == SkillBase.CastType.TargetedAlly)
                        {
                            if (targetCore != null && (IsAlly(targetCore) || target == _core.gameObject))
                            {
                                // Special case for ReviveSkill - only allow dead allies, not self
                                if (skill is ReviveSkill)
                                {
                                    Debug.Log($"[PlayerMovement] ReviveSkill check - isDead: {targetCore.isDead}, same team: {targetCore.team == _core.team}, isSelf: {target == _core.gameObject}");
                                    if (targetCore.isDead && IsAlly(targetCore) && target != _core.gameObject)
                                    {
                                        validTarget = true;
                                        Debug.Log($"[PlayerMovement] ReviveSkill valid target!");
                                    }
                                }
                                else
                                {
                                    // For all other skills, only allow living allies
                                    if (!targetCore.isDead)
                                        validTarget = true;
                                }
                            }
                            // For Solo players, also allow using TargetedAlly skills on other Solo players (treat as enemies)
                            else if (targetCore != null && _core.team == PlayerTeam.Solo && targetCore.team == PlayerTeam.Solo && target != _core.gameObject)
                            {
                                if (!targetCore.isDead)
                                {
                                    validTarget = true;
                                    Debug.Log($"[PlayerMovement] Solo player using TargetedAlly skill on another Solo player (treated as enemy)");
                                }
                            }
                        }
                        else if (skill.SkillCastType == SkillBase.CastType.TargetedEnemy)
                        {
                            if (targetCore != null && !IsAlly(targetCore) && !targetCore.isDead)
                                validTarget = true;
                            else if (targetMonster != null)
                                validTarget = true;
                        }
                        if (validTarget)
                        {
                            // Starting SkillCast on target
                            Debug.Log($"[PlayerMovement] Starting SkillCast on valid target: {target.name}");
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, target, _core.Skills.ActiveSkill);
                        }
                        else
                        {
                            // Ignored: invalid target for skill
                            Debug.Log($"[PlayerMovement] Invalid target for skill {skill.SkillName}: {target.name}");
                        }
                    }
                    else
                    {
                        // Raycast missed for targeted skill
                        Debug.Log($"[PlayerMovement] Raycast missed for skill {skill.SkillName}");
                    }
                }
                else
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.groundLayer))
                    {
                        // Starting SkillCast at ground position
                        _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, null, _core.Skills.ActiveSkill);
                        if (skill.SkillCastType != SkillBase.CastType.GroundAoEPersistent)
                            _core.Skills.CancelSkillSelection();
                    }
                    else
                    {
                        // Raycast missed for ground-targeted skill
                    }
                }
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                {
                    // Игнорируем хиты на самого игрока (и детей)
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) return;
                    // Raycast hit
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                    {
                        // Hit object is on Ignore Raycast layer
                        return;
                    }
                    // Добавлено: DroppedItem
                    DroppedItem droppedItem = hit.collider.GetComponent<DroppedItem>() ?? hit.collider.GetComponentInParent<DroppedItem>();
                    if (droppedItem != null)
                    {
                        float distance = Vector3.Distance(transform.position, hit.point);
                        if (distance <= droppedItem.pickupDistance)
                        {
                            // Pickup item
                            _core.CmdPickupDroppedItem(droppedItem.netId);
                        }
                        else
                        {
                            // Moving to item
                            _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                        }
                        return;
                    }
                    if (hit.collider.CompareTag("Player") || hit.collider.gameObject.layer == LayerMask.NameToLayer("ReviveLayer"))
                    {
                        PlayerCore targetCore = hit.collider.GetComponentInParent<PlayerCore>();
                        Debug.Log($"[PlayerMovement] Hit object: {hit.collider.name}, layer: {hit.collider.gameObject.layer}, ReviveLayer: {LayerMask.NameToLayer("ReviveLayer")}");
                        Debug.Log($"[PlayerMovement] Target core: {targetCore?.name}, isDead: {targetCore?.isDead}, team: {targetCore?.team}, caster team: {_core.team}");
                        Debug.Log($"[PlayerMovement] IsSkillSelected: {_core.Skills.IsSkillSelected}, ActiveSkill: {_core.Skills.ActiveSkill?.SkillName}");
                        if (targetCore != null)
                        {
                            if (!IsAlly(targetCore) && !targetCore.isDead)
                            {
                                // Starting Attack on enemy
                                // Global cooldown check removed
                                _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                            }
                            else if (targetCore.isDead && _core.Skills.IsSkillSelected)
                            {
                                // Revive dead ally
                                ISkill selectedSkill = _core.Skills.ActiveSkill;
                                Debug.Log($"[PlayerMovement] Selected skill: {selectedSkill?.SkillName}, is ReviveSkill: {selectedSkill is ReviveSkill}");
                                if (selectedSkill is ReviveSkill)
                                {
                                    Debug.Log($"[PlayerMovement] Starting revive action on {targetCore.name}");
                                    _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, hit.collider.gameObject, selectedSkill);
                                }
                            }
                        }
                    }
                    else if (hit.collider.CompareTag("Enemy"))
                    {
                        // Starting Attack on enemy
                        // Global cooldown check removed
                        _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                    }
                    else if (hit.collider.CompareTag("Ground"))
                    {
                        // Starting Move to position
                        _core.Combat.ClearTarget();
                        _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                    }
                    else
                    {
                        // Raycast hit ignored
                    }
                }
                else
                {
                    // Raycast missed for interactable layers
                }
            }
        }
    }
    private bool IsPointerOverPlayerUI()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("[PlayerMovement] EventSystem.current is null!");
            return false;
        }
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            // Проверяем, попал ли клик по контекстному меню
            if (IsContextMenuUI(result.gameObject))
            {
                // Pointer over context menu
                return true;
            }
            
            // Проверяем, попал ли клик по панели группы
            if (IsPartyPanelUI(result.gameObject))
            {
                // Pointer over party panel
                return true;
            }
            
            // Минимальное изменение: игнорируем UI, если это canvas DroppedItem
            if (result.gameObject.layer == LayerMask.NameToLayer("LocalPlayerUI") ||
                (result.gameObject.GetComponent<Canvas>() != null && !IsDroppedItemUI(result.gameObject)))
            {
                // Pointer over UI
                return true;
            }
        }
        return false;
    }
    private bool IsDroppedItemUI(GameObject uiObj)
    {
        Transform current = uiObj.transform;
        while (current != null)
        {
            if (current.GetComponent<DroppedItem>() != null) return true;
            current = current.parent;
        }
        return false;
    }
    
    private bool IsContextMenuUI(GameObject uiObj)
    {
        Transform current = uiObj.transform;
        while (current != null)
        {
            // Проверяем, является ли это контекстным меню или его частью
            if (current.name == "ContextMenuUI" || 
                current.name == "ContextMenuPanel" || 
                current.name == "ButtonContainer" ||
                current.name.Contains("ContextButton"))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    private bool IsPartyPanelUI(GameObject uiObj)
    {
        Transform current = uiObj.transform;
        while (current != null)
        {
            // Проверяем, является ли это панелью группы или её частью
            if (current.name == "PartyUIPanel" || 
                current.name == "PartyPanel" || 
                current.name == "PartyMembersContainer" ||
                current.name.Contains("PartyMember"))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    public void MoveTo(Vector3 destination)
    {
        if (_agent == null)
        {
            Debug.LogError("[PlayerMovement] NavMeshAgent is null");
            return;
        }
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 1f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }
        _agent.isStopped = false;
        _agent.SetDestination(destination);
        // Moving to destination
    }
    public void UpdateRotation()
    {
        if (Agent.velocity.sqrMagnitude > 0.1f)
        {
            RotateTo(Agent.velocity);
        }
    }
    private void HandleRightClick()
    {
        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Проверяем, попал ли луч в игрока
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _core.interactableLayers))
        {
            PlayerCore targetPlayer = hit.collider.GetComponent<PlayerCore>();
            if (targetPlayer != null)
            {
                // Показываем контекстное меню для игрока
                ShowContextMenuForPlayer(targetPlayer);
                return;
            }
        }
        
        // Если клик не по игроку, скрываем контекстное меню
        HideContextMenu();
    }
    
    private void ShowContextMenuForPlayer(PlayerCore targetPlayer)
    {
        if (ContextMenuUI.Instance != null)
        {
            ContextMenuUI.Instance.ShowContextMenu(Input.mousePosition, targetPlayer);
        }
    }
    
    private void HideContextMenu()
    {
        if (ContextMenuUI.Instance != null)
        {
            ContextMenuUI.Instance.HideContextMenu();
        }
    }

    /// <summary>
    /// Находит ближайшую доступную точку к цели по NavMesh
    /// </summary>
    public Vector3 GetClosestReachablePoint(Vector3 targetPosition, float maxSearchDistance = 10f)
    {
        // Сначала пробуем прямой путь
        NavMeshPath directPath = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, directPath))
        {
            if (directPath.status == NavMeshPathStatus.PathComplete)
            {
                return targetPosition; // Прямой путь доступен
            }
            
            // Если путь частичный, берем последнюю доступную точку
            if (directPath.status == NavMeshPathStatus.PathPartial && directPath.corners.Length > 1)
            {
                return directPath.corners[directPath.corners.Length - 1];
            }
        }
        
        // Ищем ближайшую точку на NavMesh к цели
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, maxSearchDistance, NavMesh.AllAreas))
        {
            // Проверяем, можем ли мы дойти до этой точки
            NavMeshPath pathToClosest = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, pathToClosest))
            {
                if (pathToClosest.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }
        
        // Если ничего не найдено, возвращаем текущую позицию
        Debug.LogWarning($"[PlayerMovement] Cannot find reachable point near {targetPosition}");
        return transform.position;
    }
    
    /// <summary>
    /// Проверяет, можем ли мы достичь цель и возвращает информацию о пути
    /// </summary>
    public bool CanReachTarget(Vector3 targetPosition, out Vector3 closestPoint, out float distanceToTarget)
    {
        closestPoint = GetClosestReachablePoint(targetPosition);
        distanceToTarget = Vector3.Distance(closestPoint, targetPosition);
        
        // Считаем, что цель достижима, если мы можем подойти к ней ближе чем на 15 метров
        return distanceToTarget <= 15f;
    }
    
    /// <summary>
    /// Улучшенный MoveTo с возвратом результата
    /// </summary>
    public bool MoveToTarget(Vector3 targetPosition, out Vector3 actualDestination)
    {
        if (_agent == null)
        {
            Debug.LogError("[PlayerMovement] NavMeshAgent is null");
            actualDestination = transform.position;
            return false;
        }
        
        // Находим ближайшую доступную точку
        actualDestination = GetClosestReachablePoint(targetPosition);
        
        // Если мы уже находимся в этой точке, не двигаемся
        if (Vector3.Distance(transform.position, actualDestination) < 0.5f)
        {
            return true; // Уже на месте
        }
        
        _agent.isStopped = false;
        bool pathSet = _agent.SetDestination(actualDestination);
        
        if (pathSet)
        {
            Debug.Log($"[PlayerMovement] Moving to closest reachable point: {actualDestination} (target was: {targetPosition})");
        }
        else
        {
            Debug.LogWarning($"[PlayerMovement] Failed to set path to {actualDestination}");
        }
        
        return pathSet;
    }
    
    public void StopMovement()
    {
        if (_agent != null && !_agent.isStopped)
        {
            _agent.isStopped = true;
            // Movement stopped
            ClearMoveIndicator();
        }
    }
    public void RotateTo(Vector3 direction)
    {
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation;
        }
    }
    [Server]
    public void SetMovementSpeed(float newSpeed)
    {
        RpcSetMovementSpeed(newSpeed);
    }
    [ClientRpc]
    private void RpcSetMovementSpeed(float newSpeed)
    {
        if (_agent != null)
        {
            _agent.speed = newSpeed;
            // Movement speed set
        }
    }
    public float GetOriginalSpeed()
    {
        return _core != null && _core.Stats != null ? _core.Stats.movementSpeed : moveSpeed;
    }
    [Client]
    private void UpdateMoveIndicator()
    {
        if (IsMoving && _core.GetMoveIndicatorPrefab() != null)
        {
            Vector3 destination = _agent.destination;
            if (_currentMoveIndicator == null)
            {
                _currentMoveIndicator = Instantiate(_core.GetMoveIndicatorPrefab(), destination, Quaternion.identity);
                // Spawned move indicator
            }
            _currentMoveIndicator.transform.position = destination;
        }
        else
        {
            ClearMoveIndicator();
        }
    }
    [Client]
    private void ClearMoveIndicator()
    {
        if (_currentMoveIndicator != null)
        {
            Destroy(_currentMoveIndicator);
            _currentMoveIndicator = null;
            // Destroyed move indicator
        }
    }
    
    /// <summary>
    /// Checks if target player is an ally
    /// Supports dynamic teams: guild, party, faction, and basic teams
    /// </summary>
    private bool IsAlly(PlayerCore target)
    {
        if (target == null) return false;
        
        // A player is always an ally to themselves
        if (_core == target)
        {
            return true;
        }
        
        // Check basic team logic first
        if (_core.team == target.team && _core.team != PlayerTeam.Solo)
        {
            return true;
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(_core.guildId) && _core.guildId == target.guildId)
        {
            return true;
        }
        
        // Check party membership
        if (!string.IsNullOrEmpty(_core.partyId) && _core.partyId == target.partyId)
        {
            return true;
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(_core.factionId) && _core.factionId == target.factionId)
        {
            return true;
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (_core.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return false;
        }
        
        return false;
    }
}