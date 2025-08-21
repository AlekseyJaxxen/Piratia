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
        if (_core.isDead || _core.isStunned) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                // Проверяем, какой тип действия сейчас выполняется
                bool isSkillCastInProgress = _core.ActionSystem.CurrentAction == PlayerAction.SkillCast;

                // Если сейчас идет каст скилла (и персонаж движется к цели), то новый клик отменяет его и начинает движение/атаку
                if (isSkillCastInProgress)
                {
                    if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                    {
                        // Клик на врага во время каста - переключаемся на атаку
                        _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                        _core.Skills.CancelSkillSelection();
                        return;
                    }
                    else if (hit.collider.CompareTag("Ground"))
                    {
                        // Клик на землю во время каста - переключаемся на движение
                        _core.Combat.ClearTarget();
                        _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                        _core.Skills.CancelSkillSelection();
                        return;
                    }
                }

                // Если навык выбран, но еще не начат каст
                if (_core.Skills.IsSkillSelected)
                {
                    // Проверяем, какой тип навыка выбран
                    if (_core.Skills.ActiveSkill is ProjectileDamageSkill || _core.Skills.ActiveSkill is TargetedStunSkill)
                    {
                        // Для навыков, требующих цель
                        if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                        {
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, hit.collider.gameObject, _core.Skills.ActiveSkill);
                            _core.Skills.CancelSkillSelection();
                        }
                    }
                    else
                    {
                        // Для навыков, кастующихся на землю
                        if (hit.collider.CompareTag("Ground"))
                        {
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, null, _core.Skills.ActiveSkill);
                            _core.Skills.CancelSkillSelection();
                        }
                    }
                }
                // Иначе, левый клик используется для обычного движения или атаки
                else
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
}