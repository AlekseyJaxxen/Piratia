using UnityEngine;
using Mirror;

public class PlayerAnimationSystem : NetworkBehaviour
{
    private PlayerActionSystem _actionSystem;
    [SerializeField] private Animator _animator;
    private NetworkAnimator _networkAnimator;
    private bool _wasPerformingAction;
    private PlayerCore _core; // Добавляем ссылку на PlayerCore

    private void Awake()
    {
        _actionSystem = GetComponent<PlayerActionSystem>();
        _core = GetComponent<PlayerCore>(); // Получаем PlayerCore
        _networkAnimator = GetComponent<NetworkAnimator>();
        if (_actionSystem == null)
        {
            Debug.LogError("[PlayerAnimationSystem] PlayerActionSystem is null!");
        }
        if (_core == null)
        {
            Debug.LogError("[PlayerAnimationSystem] PlayerCore is null!");
        }
        if (_animator == null)
        {
            Debug.LogError("[PlayerAnimationSystem] Animator is null! Please assign the Animator component in the Inspector.");
        }
        if (_networkAnimator == null)
        {
            Debug.LogError("[PlayerAnimationSystem] NetworkAnimator is null! Add NetworkAnimator to the root GameObject.");
        }
        _wasPerformingAction = false;
    }

    private void Update()
    {
        if (_actionSystem == null || _animator == null || _core == null || !isOwned) return;

        // Проверяем состояние смерти
        if (_core.isDead && !_animator.GetBool("IsDead"))
        {
            _networkAnimator.SetTrigger("Death");
            _animator.SetBool("IsDead", true); // Устанавливаем флаг смерти
            Debug.Log("[PlayerAnimationSystem] Triggered Death animation");
        }
        else if (!_core.isDead && _animator.GetBool("IsDead"))
        {
            _animator.SetBool("IsDead", false); // Сбрасываем флаг смерти при возрождении
            ResetAnimations();
        }

        if (_wasPerformingAction && !_actionSystem.IsPerformingAction)
        {
            ResetAnimations();
        }
        _wasPerformingAction = _actionSystem.IsPerformingAction;
        UpdateAnimations();
    }

    [Client]
    private void UpdateAnimations()
    {
        if (_core.isDead) return; // Пропускаем обновление анимаций, если персонаж мёртв

        bool isMoving = _core.Movement.IsMoving;
        if (_actionSystem.CurrentAction == PlayerAction.Move)
        {
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("SkillCast");
            _animator.speed = 1f;
            isMoving = true;
        }
        else if (_actionSystem.CurrentAction == PlayerAction.Attack && _actionSystem.CurrentTarget != null && _actionSystem.CurrentSkill != null)
        {
            float attackRange = _actionSystem.CurrentSkill.Range;
            float distance = Vector3.Distance(transform.position, _actionSystem.CurrentTarget.transform.position);
            isMoving = distance > attackRange;
            if (isMoving)
            {
                _animator.speed = 1f;
            }
            else if (!isMoving && distance <= attackRange)
            {
                if (_actionSystem.CurrentSkill is BasicAttackSkill)
                {
                    float attackSpeed = _actionSystem.GetComponent<PlayerCore>().Stats.attackSpeed;
                    _animator.speed = attackSpeed;
                    _networkAnimator.SetTrigger("Attack");
                }
            }
        }
        else if (_actionSystem.CurrentAction == PlayerAction.SkillCast && _actionSystem.CurrentSkill != null)
        {
            float castRange = _actionSystem.CurrentSkill.Range;
            float distance;
            if (_actionSystem.CurrentTarget != null)
            {
                distance = Vector3.Distance(transform.position, _actionSystem.CurrentTarget.transform.position);
            }
            else if (_actionSystem.CurrentTargetPosition.HasValue)
            {
                distance = Vector3.Distance(transform.position, _actionSystem.CurrentTargetPosition.Value);
            }
            else
            {
                _animator.SetBool("IsMoving", false);
                _animator.speed = 1f;
                return;
            }
            isMoving = distance > castRange;
            if (isMoving)
            {
                _animator.speed = 1f;
            }
            else if (!isMoving && distance <= castRange)
            {
                _networkAnimator.SetTrigger("SkillCast");
            }
        }

        if (_animator.GetBool("IsMoving") != isMoving)
        {
            _animator.SetBool("IsMoving", isMoving);
            Debug.Log($"[PlayerAnimationSystem] Set IsMoving to {isMoving}, CurrentAction: {_actionSystem.CurrentAction}");
        }
    }

    [Client]
    public void ResetAnimations()
    {
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("SkillCast");
            _animator.ResetTrigger("Death"); // Сбрасываем триггер смерти
            _animator.SetBool("IsDead", false); // Сбрасываем флаг смерти
            _animator.speed = 1f;
            _animator.Play("Idle", 0, 0f);
            Debug.Log("[PlayerAnimationSystem] Animations reset to Idle");
        }
    }

    [Client]
    public void TriggerAttackAnimation()
    {
        if (_networkAnimator != null)
        {
            _networkAnimator.SetTrigger("Attack");
        }
    }
}