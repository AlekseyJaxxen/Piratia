using UnityEngine;
using Mirror;

public class PlayerAnimationSystem : NetworkBehaviour
{
    private PlayerActionSystem _actionSystem;
    [SerializeField] private Animator _animator;
    private NetworkAnimator _networkAnimator;
    private bool _wasPerformingAction;

    private void Awake()
    {
        _actionSystem = GetComponent<PlayerActionSystem>();
        _networkAnimator = GetComponent<NetworkAnimator>();

        if (_actionSystem == null)
        {
            Debug.LogError("[PlayerAnimationSystem] PlayerActionSystem is null!");
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
        if (_actionSystem == null || _animator == null || !isOwned) return;

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
        bool isMoving = _actionSystem.CurrentAction == PlayerAction.Move;

        // При начале движения сбрасываем триггеры атаки и каста
        if (_actionSystem.CurrentAction == PlayerAction.Move)
        {
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("SkillCast");
            _animator.SetBool("IsMoving", true); // Немедленно включаем анимацию движения
            return;
        }

        // Проверяем анимации атаки и каста только если не движемся
        if (_actionSystem.CurrentAction == PlayerAction.Attack && _actionSystem.CurrentTarget != null && _actionSystem.CurrentSkill != null)
        {
            float attackRange = _actionSystem.CurrentSkill.Range;
            float distance = Vector3.Distance(transform.position, _actionSystem.CurrentTarget.transform.position);
            isMoving = distance > attackRange;
            if (!isMoving && distance <= attackRange)
            {
                _networkAnimator.SetTrigger("Attack");
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
                return;
            }
            isMoving = distance > castRange;
            if (!isMoving && distance <= castRange)
            {
                _networkAnimator.SetTrigger("SkillCast");
            }
        }

        _animator.SetBool("IsMoving", isMoving);
    }

    [Client]
    public void ResetAnimations()
    {
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("SkillCast");
        }
    }
}