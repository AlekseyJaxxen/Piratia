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
        if (_core.isDead || _core.isStunned || !_core.ActionSystem.CanStartNewAction) return;

        if (_core.Skills.IsSkillSelected) return;

        // 🚨 ИСПРАВЛЕНО: Добавлена проверка, чтобы движение не перехватывало клики,
        // которые должны быть обработаны другими системами.
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                // Проверяем, не является ли цель врагом или игроком,
                // так как эти цели обрабатываются в PlayerCombat.
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    return;
                }

                if (hit.collider.CompareTag("Ground"))
                {
                    _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
                }
            }
        }
    }

    public void MoveTo(Vector3 destination)
    {
        _agent.isStopped = false;
        _agent.SetDestination(destination);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }
}