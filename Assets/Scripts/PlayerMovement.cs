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
            Debug.LogError("[PlayerMovement] HandleMovement failed: _core is null");
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
            Debug.LogError("[PlayerMovement] Camera or CameraInstance is null");
            return;
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
                    _core.Skills.CmdExecuteSkill(_core, null, _core.netId, skill.SkillName, skill.Weight);
                    _core.Skills.CancelSkillSelection();
                    return;
                }
                if (isTargeted)
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                    {
                        // Raycast hit
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
                            // Starting SkillCast on target
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, target, _core.Skills.ActiveSkill);
                        }
                        else
                        {
                            // Ignored: invalid target for skill
                        }
                    }
                    else
                    {
                        // Raycast missed for targeted skill
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
                    if (hit.collider.CompareTag("Player"))
                    {
                        PlayerCore targetCore = hit.collider.GetComponentInParent<PlayerCore>();
                        if (targetCore != null && targetCore.team != _core.team)
                        {
                            // Starting Attack on target
                            if (_core.Skills.GetGlobalRemainingCooldown() > 0) return;
                            _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                        }
                        else
                        {
                            // Attack ignored: same team or invalid
                        }
                    }
                    else if (hit.collider.CompareTag("Enemy"))
                    {
                        // Starting Attack on enemy
                        if (_core.Skills.GetGlobalRemainingCooldown() > 0) return;
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
}