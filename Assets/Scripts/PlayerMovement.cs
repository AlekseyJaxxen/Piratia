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
            Debug.LogError("[PlayerMovement] Init failed: PlayerCore is null");
            return;
        }
        _core = core;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("[PlayerMovement] NavMeshAgent component missing!");
            return;
        }
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.updateRotation = false;
        Debug.Log($"[PlayerMovement] Initialized with moveSpeed={moveSpeed}, core.isOwned={_core.netIdentity.isOwned}, core.Camera={(_core.Camera != null ? _core.Camera.name : "null")}");
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
            Debug.LogError("[PlayerMovement] HandleMovement failed: _core is null");
            return;
        }
        if (_core.isDead || _core.isStunned)
        {
            Debug.Log($"[PlayerMovement] Input ignored: isDead={_core.isDead}, isStunned={_core.isStunned}");
            return;
        }
        if (!isLocalPlayer)
        {
            Debug.Log("[PlayerMovement] Input ignored: not local player");
            return;
        }
        if (!_core.netIdentity.isOwned)
        {
            Debug.Log("[PlayerMovement] Input ignored: player lacks authority");
            return;
        }
        if (_core.Camera == null || _core.Camera.CameraInstance == null)
        {
            Debug.LogError("[PlayerMovement] Camera or CameraInstance is null");
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverPlayerUI())
            {
                Debug.Log("[PlayerMovement] Click ignored: Pointer is over LocalPlayerUI element");
                return;
            }
            Debug.Log($"[PlayerMovement] Left mouse button clicked at position: {Input.mousePosition}");
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            Debug.Log($"[PlayerMovement] Raycast from mouse position: {Input.mousePosition}, camera: {_core.Camera.CameraInstance.name}");
            if (_core.Skills.IsSkillSelected)
            {
                Debug.Log($"[PlayerMovement] Skill selected: {_core.Skills.ActiveSkill?.SkillName ?? "null"}");
                var skill = (SkillBase)_core.Skills.ActiveSkill;
                bool isTargeted = skill.SkillCastType == SkillBase.CastType.TargetedEnemy || skill.SkillCastType == SkillBase.CastType.TargetedAlly;
                bool isSelf = skill.SkillCastType == SkillBase.CastType.SelfBuff || skill.SkillCastType == SkillBase.CastType.ToggleBuff;
                if (isSelf)
                {
                    _core.Skills.CmdExecuteSkill(_core, null, _core.netId, skill.SkillName, skill.Weight);
                    _core.Skills.CancelSkillSelection();
                    return;
                }
                if (isTargeted)
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                    {
                        Debug.Log($"[PlayerMovement] Raycast hit: {hit.collider.name}, tag={hit.collider.tag}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, components={string.Join(", ", hit.collider.GetComponents<Component>().Select(c => c.GetType().Name))}");
                        GameObject target = hit.collider.gameObject;
                        bool validTarget = false;
                        PlayerCore targetCore = target.GetComponentInParent<PlayerCore>();
                        Monster targetMonster = target.GetComponentInParent<Monster>();
                        if (skill.SkillCastType == SkillBase.CastType.TargetedAlly)
                        {
                            if (targetCore != null && (targetCore.team == _core.team || target == _core.gameObject))
                                validTarget = true;
                        }
                        else if (skill.SkillCastType == SkillBase.CastType.TargetedEnemy)
                        {
                            if (targetCore != null && targetCore.team != _core.team)
                                validTarget = true;
                            else if (targetMonster != null)
                                validTarget = true;
                        }
                        if (validTarget)
                        {
                            Debug.Log($"[PlayerMovement] Starting SkillCast on target: {target.name}");
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, target, _core.Skills.ActiveSkill);
                        }
                        else
                        {
                            Debug.Log("[PlayerMovement] Ignored: invalid target for skill");
                        }
                    }
                    else
                    {
                        Debug.Log("[PlayerMovement] Raycast missed for targeted skill");
                    }
                }
                else
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.groundLayer))
                    {
                        Debug.Log($"[PlayerMovement] Starting SkillCast at ground position: {hit.point}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                        _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, null, _core.Skills.ActiveSkill);
                        if (skill.SkillCastType != SkillBase.CastType.GroundAoEPersistent)
                            _core.Skills.CancelSkillSelection();
                    }
                    else
                    {
                        Debug.Log("[PlayerMovement] Raycast missed for ground-targeted skill");
                    }
                }
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                {
                    // Игнорируем хиты на самого игрока (и детей)
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) return;
                    Debug.Log($"[PlayerMovement] Raycast hit: {hit.collider.name}, tag={hit.collider.tag}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, components={string.Join(", ", hit.collider.GetComponents<Component>().Select(c => c.GetType().Name))}");
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                    {
                        Debug.Log("[PlayerMovement] Hit object is on Ignore Raycast layer. Ignoring.");
                        return;
                    }
                    // Добавлено: DroppedItem
                    DroppedItem droppedItem = hit.collider.GetComponent<DroppedItem>() ?? hit.collider.GetComponentInParent<DroppedItem>();
                    if (droppedItem != null)
                    {
                        float distance = Vector3.Distance(transform.position, hit.point);
                        if (distance <= droppedItem.pickupDistance)
                        {
                            Debug.Log($"[PlayerMovement] Pickup item: {droppedItem.itemID}");
                            _core.CmdPickupDroppedItem(droppedItem.netId);
                        }
                        else
                        {
                            Debug.Log($"[PlayerMovement] Moving to item: {droppedItem.itemID}");
                            _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                        }
                        return;
                    }
                    if (hit.collider.CompareTag("Player"))
                    {
                        PlayerCore targetCore = hit.collider.GetComponentInParent<PlayerCore>();
                        if (targetCore != null && targetCore.team != _core.team)
                        {
                            Debug.Log($"[PlayerMovement] Starting Attack on target: {hit.collider.name}, netId={targetCore.netId}");
                            if (_core.Skills.GetGlobalRemainingCooldown() > 0) return;
                            _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                        }
                        else
                        {
                            Debug.Log($"[PlayerMovement] Attack ignored: target {hit.collider.name} is on the same team or invalid");
                        }
                    }
                    else if (hit.collider.CompareTag("Enemy"))
                    {
                        Debug.Log($"[PlayerMovement] Starting Attack on enemy: {hit.collider.name}");
                        if (_core.Skills.GetGlobalRemainingCooldown() > 0) return;
                        _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                    }
                    else if (hit.collider.CompareTag("Ground"))
                    {
                        Debug.Log($"[PlayerMovement] Starting Move to position: {hit.point}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                        _core.Combat.ClearTarget();
                        _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                    }
                    else
                    {
                        Debug.Log($"[PlayerMovement] Raycast hit ignored: tag={hit.collider.tag}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    }
                }
                else
                {
                    Debug.Log("[PlayerMovement] Raycast missed for interactable layers");
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
            // Минимальное изменение: игнорируем UI, если это canvas DroppedItem
            if (result.gameObject.layer == LayerMask.NameToLayer("LocalPlayerUI") ||
                (result.gameObject.GetComponent<Canvas>() != null && !IsDroppedItemUI(result.gameObject)))
            {
                Debug.Log($"[PlayerMovement] Pointer over UI: {result.gameObject.name}, layer={LayerMask.LayerToName(result.gameObject.layer)}");
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
    public void MoveTo(Vector3 destination)
    {
        if (_agent == null)
        {
            Debug.LogError("[PlayerMovement] NavMeshAgent is null");
            return;
        }
        _agent.isStopped = false;
        _agent.SetDestination(destination);
        Debug.Log($"[PlayerMovement] Moving to destination: {destination}");
    }
    public void UpdateRotation()
    {
        if (Agent.velocity.sqrMagnitude > 0.1f)
        {
            RotateTo(Agent.velocity);
        }
    }
    public void StopMovement()
    {
        if (_agent != null && !_agent.isStopped)
        {
            _agent.isStopped = true;
            Debug.Log("[PlayerMovement] Movement stopped");
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
            Debug.Log($"[PlayerMovement] Movement speed set to: {newSpeed}");
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
                Debug.Log($"[PlayerMovement] Spawned move indicator at {destination}");
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
            Debug.Log("[PlayerMovement] Destroyed move indicator");
        }
    }
}