using Mirror;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    public float attackDelay = 0.3f;
    public GameObject targetIndicatorPrefab;

    private GameObject _currentTarget;
    private float _lastAttackTime;
    private PlayerCore _core;
    private bool _isAttacking;
    private GameObject _targetIndicator;

    public bool IsAttacking => _isAttacking;

    public void Init(PlayerCore core)
    {
        _core = core;
        if (isLocalPlayer)
        {
            _targetIndicator = Instantiate(targetIndicatorPrefab);
            _targetIndicator.SetActive(false);
        }
    }

    public void HandleCombat()
    {
        if (!isLocalPlayer || _core.isDead || _core.isStunned) return;

        if (Input.GetMouseButtonDown(0) && !_core.Skills.AnySkillRangeActive())
        {
            HandleLeftClick();
        }

        UpdateTargetIndicator();

        if (_currentTarget != null && !_isAttacking)
        {
            ProcessAttack();
        }
    }

    private void HandleLeftClick()
    {
        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
        {
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
            {
                _core.ActionSystem.CompleteAction();
                _currentTarget = hit.collider.gameObject;
                ProcessAttack();
            }
            else if (hit.collider.CompareTag("Ground"))
            {
                ClearTarget();
                _core.ActionSystem.TryStartAction(PlayerAction.Move, hit.point);
            }
        }
    }

    private void UpdateTargetIndicator()
    {
        if (!isLocalPlayer || _targetIndicator == null) return;

        if (_currentTarget != null)
        {
            _targetIndicator.SetActive(true);
            _targetIndicator.transform.position = _currentTarget.transform.position + Vector3.up * 1f;
        }
        else
        {
            _targetIndicator.SetActive(false);
        }
    }

    private void ProcessAttack()
    {
        if (_currentTarget == null)
        {
            ClearTarget();
            _core.ActionSystem.CompleteAction();
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);
        Debug.Log($"[Client] Distance to target: {distance}, AttackRange: {attackRange}");

        if (distance <= attackRange)
        {
            _core.ActionSystem.TryStartAction(PlayerAction.Attack);
        }
        else
        {
            _core.ActionSystem.TryStartAction(PlayerAction.Move, _currentTarget.transform.position);
        }
    }

    public void StartAttack()
    {
        if (Time.time - _lastAttackTime < attackCooldown || _isAttacking)
        {
            Debug.Log($"[Client] Cannot attack: Cooldown active or already attacking");
            return;
        }
        if (_currentTarget == null)
        {
            Debug.Log($"[Client] Cannot attack: Target is null");
            return;
        }

        _isAttacking = true;
        _core.Movement.StopMovement();
        _core.Movement.RotateTo(_currentTarget.transform.position - transform.position);
        // _core.Animation.PlayAttackAnimation();
        Invoke(nameof(ApplyAttackDamage), attackDelay);
        Invoke(nameof(CompleteAttack), attackDelay + 0.2f);
    }

    private void ApplyAttackDamage()
    {
        if (_currentTarget == null) return;

        Health targetHealth = _currentTarget.GetComponent<Health>();
        if (targetHealth != null)
        {
            CmdApplyDamage(_currentTarget.GetComponent<NetworkIdentity>().netId, attackDamage);
        }
    }

    [Command]
    private void CmdApplyDamage(uint targetNetId, int damage)
    {
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            targetIdentity.GetComponent<Health>()?.TakeDamage(damage);
        }
    }

    private void CompleteAttack()
    {
        _lastAttackTime = Time.time;
        _isAttacking = false;
    }

    public void ClearTarget()
    {
        _currentTarget = null;
    }
}