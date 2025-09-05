using UnityEngine;
using Mirror;
using System.Linq;

public class PlayerAnimationSystem : NetworkBehaviour
{
    private PlayerActionSystem _actionSystem;
    [SerializeField] private Animator _animator;
    private NetworkAnimator _networkAnimator;
    private bool _wasPerformingAction;
    private PlayerCore _core;
    private CharacterStats _stats;
    [SerializeField] private GameObject[] characterModels;
    private GameObject _activeModel;

    private void Awake()
    {
        _actionSystem = GetComponent<PlayerActionSystem>();
        _core = GetComponent<PlayerCore>();
        _stats = GetComponent<CharacterStats>();
        _networkAnimator = GetComponent<NetworkAnimator>();

        if (_actionSystem == null) Debug.LogError("[PlayerAnimationSystem] PlayerActionSystem is null!");
        if (_core == null) Debug.LogError("[PlayerAnimationSystem] PlayerCore is null!");
        if (_stats == null) Debug.LogError("[PlayerAnimationSystem] CharacterStats is null!");
        if (_networkAnimator == null) Debug.LogError("[PlayerAnimationSystem] NetworkAnimator is null!");

        characterModels = GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("CharacterModel"))
            .Select(t => t.gameObject)
            .ToArray();

        foreach (var model in characterModels)
        {
            model.SetActive(false);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateCharacterModelAndAnimator();
        _stats.OnCharacterClassChangedEvent += OnCharacterClassChanged; // Подписываемся на публичное событие
    }

    private void OnCharacterClassChanged(CharacterClass oldClass, CharacterClass newClass)
    {
        UpdateCharacterModelAndAnimator();
    }

    [Client]
    private void UpdateCharacterModelAndAnimator()
    {
        if (_stats == null || characterModels == null || characterModels.Length == 0) return;

        ClassData classData = Resources.Load<ClassData>($"ClassData/{_stats.characterClass}");
        if (classData == null)
        {
            Debug.LogError($"[PlayerAnimationSystem] Failed to load ClassData for {_stats.characterClass}");
            return;
        }

        if (_activeModel != null)
        {
            _activeModel.SetActive(false);
        }

        _activeModel = characterModels.FirstOrDefault(model => model.name == classData.modelPrefab.name);
        if (_activeModel == null)
        {
            Debug.LogError($"[PlayerAnimationSystem] Model for {_stats.characterClass} not found!");
            return;
        }

        _activeModel.SetActive(true);

        _animator = _activeModel.GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError($"[PlayerAnimationSystem] Animator not found on model for {_stats.characterClass}");
            return;
        }

        if (classData.animatorController != null)
        {
            _animator.runtimeAnimatorController = classData.animatorController;
        }
        else
        {
            Debug.LogWarning($"[PlayerAnimationSystem] AnimatorController not set in ClassData for {_stats.characterClass}");
        }

        _networkAnimator.animator = _animator;
        Debug.Log($"[PlayerAnimationSystem] Set model {_activeModel.name} and animator for {_stats.characterClass}");
    }

    private void Update()
    {
        if (_actionSystem == null || _animator == null || _core == null || !isOwned) return;

        if (_core.isDead && !_animator.GetBool("IsDead"))
        {
            _networkAnimator.SetTrigger("Death");
            _animator.SetBool("IsDead", true);
            Debug.Log("[PlayerAnimationSystem] Triggered Death animation");
        }
        else if (!_core.isDead && _animator.GetBool("IsDead"))
        {
            _animator.SetBool("IsDead", false);
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
        if (_core.isDead) return;

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
                    float attackSpeed = _stats.attackSpeed;
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
            _animator.ResetTrigger("Death");
            _animator.SetBool("IsDead", false);
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