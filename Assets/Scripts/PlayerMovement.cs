using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.5f;

    private NavMeshAgent _agent;
    public NavMeshAgent Agent => _agent;

    private PlayerCore _core;

    public bool IsMoving => _agent.velocity.magnitude > 0.1f;

    public void Init(PlayerCore core)
    {
        _core = core;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.updateRotation = false;
    }

    public void HandleMovement()
    {
        if (_core.isDead || _core.EffectManager.IsStunned) return; // FIX: Changed _core.isStunned

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);

            // Если умение выбрано
            if (_core.Skills.IsSkillSelected)
            {
                bool isTargetedSkill = _core.Skills.ActiveSkill is ProjectileDamageSkill || _core.Skills.ActiveSkill is TargetedStunSkill || _core.Skills.ActiveSkill is SlowSkill;

                // Если умение ТАРГЕТНОЕ, используем interactableLayers
                if (isTargetedSkill)
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                    {
                        // Проверяем, является ли цель игроком
                        if (hit.collider.CompareTag("Player"))
                        {
                            PlayerCore targetCore = hit.collider.GetComponent<PlayerCore>();

                            // Если цель - враг, начинаем каст.
                            // Если цель - союзник, ничего не делаем.
                            if (targetCore != null && targetCore.team != _core.team)
                            {
                                _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, hit.collider.gameObject, _core.Skills.ActiveSkill);
                                _core.Skills.CancelSkillSelection();
                            }
                            else
                            {
                                // Если цель - союзник, мы просто выходим,
                                // и выбор умения сохраняется.
                                return;
                            }
                        }
                        // Если цель - враг (с тегом "Enemy"), начинаем каст
                        else if (hit.collider.CompareTag("Enemy"))
                        {
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, hit.collider.gameObject, _core.Skills.ActiveSkill);
                            _core.Skills.CancelSkillSelection();
                        }
                    }
                }
                // Если умение НЕ ТАРГЕТНОЕ (AoE), используем ТОЛЬКО groundLayer
                else
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.groundLayer))
                    {
                        _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, null, _core.Skills.ActiveSkill);
                        _core.Skills.CancelSkillSelection();
                    }
                }
            }
            // Если умение НЕ выбрано, используем стандартную логику движения/атаки
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
                {
                    if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                    {
                        _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                        return;
                    }
                    if (hit.collider.CompareTag("Ground"))
                    {
                        _core.Combat.ClearTarget();
                        _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                    }
                }
            }
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (_agent == null) return;
        _agent.isStopped = false;
        _agent.SetDestination(destination);
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
        }
    }

    public float GetOriginalSpeed()
    {
        return moveSpeed;
    }
}