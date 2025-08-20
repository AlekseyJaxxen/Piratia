using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.5f;

    private NavMeshAgent _agent;
    private PlayerCore _core;

    public NavMeshAgent Agent => _agent;

    // 🚨 НОВОЕ: Свойство для проверки наличия пути
    public bool HasPath => _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance;

    public bool IsMoving => _agent.velocity.magnitude > 0.1f;

    public void Init(PlayerCore core)
    {
        _core = core;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.updateRotation = false;
    }

    public void TryMoveToDestination()
    {
        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                _core.Combat.ClearTarget();
                _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
            }
        }
    }

    public void MoveTo(Vector3 destination)
    {
        _agent.SetDestination(destination);
        _agent.isStopped = false;
        RotateTo((destination - transform.position).normalized);
        Debug.Log($"[Client] Moving to {destination} for {gameObject.name}");
    }

    public void StopMovement()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
        Debug.Log($"[Client] Stopped movement for {gameObject.name}");
    }

    public void RotateTo(Vector3 direction)
    {
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            _core.modelPivot.rotation = lookRotation;
            Debug.Log($"[Client] Rotated to {lookRotation} for {gameObject.name}");
        }
    }
}