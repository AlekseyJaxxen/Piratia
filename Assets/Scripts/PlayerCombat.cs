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

    public void HandleCombat()
    {
        if (!isLocalPlayer || _core.isDead || _core.isStunned) return;

        if (_core.Skills.IsSkillSelected) return;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[Combat] Left mouse button clicked.");
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                // 🚨 ИСПРАВЛЕНО: Проверяем оба тега.
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    Debug.Log($"[Combat] Raycast hit a valid target: {hit.collider.gameObject.name}");
                    _currentTarget = hit.collider.gameObject;

                    // 🚨 ИСПРАВЛЕНО: Передаем управление в PlayerActionSystem. 
                    // Он сам решит, атаковать или двигаться.
                    _core.ActionSystem.TryStartAction(PlayerAction.Attack);
                }
                else
                {
                    Debug.Log("[Combat] Raycast did not hit a valid target.");
                    ClearTarget();
                }
            }
            else
            {
                Debug.Log("[Combat] Raycast hit nothing.");
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

    public void StartAttack()
    {
        Debug.Log("[Combat] StartAttack called.");
        if (_isAttacking || Time.time - _lastAttackTime < attackCooldown)
        {
            Debug.Log("[Combat] Attack failed: already attacking or on cooldown.");
            _core.ActionSystem.CompleteAction(); // Завершаем действие, чтобы не блокировать
            return;
        }
        if (_currentTarget == null)
        {
            Debug.Log("[Combat] Attack failed: no target.");
            _core.ActionSystem.CompleteAction(); // Завершаем действие
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);

        if (distance > attackRange)
        {
            Debug.Log("[Combat] Target is out of range. Moving to target instead.");
            // 🚨 ИСПРАВЛЕНО: Вместо вызова TryStartAction,
            // просто вызываем CmdMoveTo, чтобы избежать рекурсии.
            _core.ActionSystem.CompleteAction();
            _core.ActionSystem.TryStartAction(PlayerAction.Move, _currentTarget.transform.position);
            return;
        }

        Debug.Log("[Combat] Attack is starting.");
        _isAttacking = true;
        _core.Movement.StopMovement();

        _core.Movement.RotateTo(_currentTarget.transform.position - transform.position);

        CancelInvoke(nameof(ApplyAttackDamage));
        CancelInvoke(nameof(CompleteAttack));

        Invoke(nameof(ApplyAttackDamage), attackDelay);
        Invoke(nameof(CompleteAttack), attackDelay + 0.2f);
    }

    private void ApplyAttackDamage()
    {
        if (_currentTarget == null) return;
        Debug.Log("[Combat] Applying attack damage.");
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

    private void CompleteAttack()
    {
        Debug.Log("[Combat] Completing attack.");
        _lastAttackTime = Time.time;
        _isAttacking = false;
        _core.ActionSystem.CompleteAction();
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