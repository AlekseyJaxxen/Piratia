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
                // If a skill is selected, try to cast it.
                if (_core.Skills.IsSkillSelected)
                {
                    if ((hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player")))
                    {
                        // For targeted skills, check if the hit object is a target.
                        if (_core.Skills.ActiveSkill is TargetedStunSkill || _core.Skills.ActiveSkill is ProjectileDamageSkill)
                        {
                            Debug.Log($"PlayerMovement: Casting skill '{_core.Skills.ActiveSkill.GetType().Name}' on target.");
                            _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, hit.collider.gameObject, _core.Skills.ActiveSkill);
                            _core.Skills.CancelSkillSelection();
                            return;
                        }
                    }

                    // If the skill is not targeted or the click was on the ground, cast on the ground.
                    if (hit.collider.CompareTag("Ground"))
                    {
                        Debug.Log($"PlayerMovement: Casting skill '{_core.Skills.ActiveSkill.GetType().Name}' on ground.");
                        _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, null, _core.Skills.ActiveSkill);
                        _core.Skills.CancelSkillSelection();
                        return;
                    }
                }
                // If no skill is selected, handle regular movement or attack.
                else
                {
                    if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                    {
                        Debug.Log("PlayerMovement: Initiating attack on target.");
                        _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, hit.collider.gameObject);
                    }
                    else if (hit.collider.tag == "Ground")
                    {
                        Debug.Log("PlayerMovement: Initiating movement.");
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
}