using UnityEngine;
using UnityEngine.AI;
using Mirror;

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
        }
    }

    public void HandleMovement()
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
                    skill.Execute(_core, null, _core.gameObject);
                    _core.Skills.CancelSkillSelection();
                    return;
                }

                if (isTargeted)
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                    {
                        Debug.Log($"[PlayerMovement] Raycast hit: {hit.collider.name}, tag={hit.collider.tag}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                        GameObject target = hit.collider.gameObject;
                        bool validTarget = false;
                        PlayerCore targetCore = target.GetComponent<PlayerCore>();
                        Monster targetMonster = target.GetComponent<Monster>();

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
                else // GroundAoE*
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
                    Debug.Log($"[PlayerMovement] Raycast hit: {hit.collider.name}, tag={hit.collider.tag}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    if (hit.collider.CompareTag("Player"))
                    {
                        PlayerCore targetCore = hit.collider.GetComponent<PlayerCore>();
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
                        Debug.Log($"[PlayerMovement] Raycast hit ignored: invalid tag {hit.collider.tag}");
                    }
                }
                else
                {
                    Debug.Log("[PlayerMovement] Raycast missed for interactable layers");
                }
            }
        }
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
}