using UnityEngine;
using Mirror;
using System.Collections;
using System;

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

    public void SetCurrentTarget(GameObject target)
    {
        _currentTarget = target;
    }

    public void HandleCombat()
    {
        if (!isLocalPlayer || _core.isDead || _core.isStunned) return;

        if (_core.Skills.IsSkillSelected) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    if (!_core.ActionSystem.IsPerformingAction)
                    {
                        _core.ActionSystem.TryStartAction(PlayerAction.Attack, targetObject: hit.collider.gameObject);
                    }
                }
            }
        }

        UpdateTargetIndicator();
    }

    private void UpdateTargetIndicator()
    {
        if (_targetIndicator == null) return;

        if (_currentTarget != null)
        {
            _targetIndicator.SetActive(true);
            _targetIndicator.transform.position = _currentTarget.transform.position + Vector3.up * 0.1f;
        }
        else
        {
            _targetIndicator.SetActive(false);
        }
    }

    public void PerformAttack()
    {
        if (Time.time - _lastAttackTime < attackCooldown)
        {
            return;
        }

        _lastAttackTime = Time.time;

        CancelInvoke(nameof(ApplyAttackDamage));
        Invoke(nameof(ApplyAttackDamage), attackDelay);
    }

    private void ApplyAttackDamage()
    {
        if (_currentTarget == null) return;

        Health targetHealth = _currentTarget.GetComponent<Health>();
        if (targetHealth != null)
        {
            CmdApplyDamage(targetHealth.GetComponent<NetworkIdentity>().netId, attackDamage);
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

    public void StopAttacking()
    {
        _isAttacking = false;
        CancelInvoke();
    }

    public void ClearTarget()
    {
        _currentTarget = null;
    }
}