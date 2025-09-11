using UnityEngine;
using Mirror;
using DG.Tweening;
using System.Linq;

public class PlayerAnimationSystem : NetworkBehaviour
{
    private PlayerActionSystem _actionSystem;
    [SerializeField] private Animator _animator;
    private PlayerCore _core;
    private CharacterStats _stats;
    [SerializeField] private GameObject[] characterModels;
    private GameObject _activeModel;
    private bool _wasPerformingAction;
    private Renderer modelRenderer;
    private Color originalColor;
    private Sequence damageFlashSequence;
    [SyncVar(hook = nameof(OnIsMovingChanged))]
    private bool syncIsMoving;
    [SyncVar(hook = nameof(OnIsDeadChanged))]
    private bool syncIsDead;

    private void Awake()
    {
        _actionSystem = GetComponent<PlayerActionSystem>();
        _core = GetComponent<PlayerCore>();
        _stats = GetComponent<CharacterStats>();
        if (_actionSystem == null) Debug.LogError("[PlayerAnimationSystem] PlayerActionSystem is null!");
        if (_core == null) Debug.LogError("[PlayerAnimationSystem] PlayerCore is null!");
        if (_stats == null) Debug.LogError("[PlayerAnimationSystem] CharacterStats is null!");
        characterModels = GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("CharacterModel"))
            .Select(t => t.gameObject)
            .ToArray();
        foreach (var model in characterModels)
        {
            model.SetActive(false);
        }
        _stats.OnCharacterClassChangedEvent += OnCharacterClassChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateCharacterModelAndAnimator();
    }

    private void OnCharacterClassChanged(CharacterClass oldClass, CharacterClass newClass)
    {
        UpdateCharacterModelAndAnimator();
    }

    private void UpdateCharacterModelAndAnimator()
    {
        if (_stats == null || characterModels == null || characterModels.Length == 0)
        {
            Debug.LogError("[PlayerAnimationSystem] Stats or characterModels are null or empty!");
            return;
        }
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
            Debug.LogError($"[PlayerAnimationSystem] Model for {_stats.characterClass} not found! Available models: {string.Join(", ", characterModels.Select(m => m.name))}");
            return;
        }
        _activeModel.SetActive(true);
        _animator = _activeModel.GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError($"[PlayerAnimationSystem] Animator not found on model {_activeModel.name} for {_stats.characterClass}");
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
        // Поиск Renderer в активной модели или её дочерних объектах
        modelRenderer = _activeModel.GetComponentInChildren<Renderer>();
        if (modelRenderer != null)
        {
            originalColor = modelRenderer.material.color;
            damageFlashSequence = DOTween.Sequence();
            damageFlashSequence.Append(modelRenderer.material.DOColor(Color.red, 0.1f));
            damageFlashSequence.Append(modelRenderer.material.DOColor(originalColor, 0.1f));
            damageFlashSequence.SetAutoKill(false);
            damageFlashSequence.Pause();
            Debug.Log($"[PlayerAnimationSystem] Renderer found on {_activeModel.name} or its children");
        }
        else
        {
            Debug.LogError($"[PlayerAnimationSystem] No Renderer found on active model {_activeModel.name} or its children!");
        }
        Debug.Log($"[PlayerAnimationSystem] Set model {_activeModel.name} and animator for {_stats.characterClass}");
    }

    public void PlayDamageFlash()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Rewind();
            damageFlashSequence.Play();
        }
    }

    private void Update()
    {
        if (_actionSystem == null || _animator == null || _core == null || !isOwned) return;
        if (_core.isDead && !syncIsDead)
        {
            CmdSetTrigger("Death");
            CmdSetIsDead(true);
            Debug.Log("[PlayerAnimationSystem] Triggered Death animation");
        }
        else if (!_core.isDead && syncIsDead)
        {
            CmdSetIsDead(false);
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
            CmdResetTrigger("Attack");
            CmdResetTrigger("SkillCast");
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
                    CmdSetTrigger("Attack");
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
                CmdSetIsMoving(false);
                _animator.speed = 1f;
                CmdSetTrigger("Idle");
                return;
            }
            isMoving = distance > castRange;
            if (isMoving)
            {
                _animator.speed = 1f;
            }
            else if (!isMoving && distance <= castRange)
            {
                CmdSetTrigger("SkillCast");
            }
        }
        else
        {
            if (!isMoving)
            {
                CmdSetTrigger("Idle");
            }
        }
        if (syncIsMoving != isMoving)
        {
            CmdSetIsMoving(isMoving);
            Debug.Log($"[PlayerAnimationSystem] Set IsMoving to {isMoving}, CurrentAction: {_actionSystem.CurrentAction}");
        }
    }

    [Command]
    private void CmdSetIsMoving(bool value)
    {
        syncIsMoving = value;
    }

    [Command]
    private void CmdSetIsDead(bool value)
    {
        syncIsDead = value;
    }

    [Command]
    private void CmdSetTrigger(string trigger)
    {
        RpcSetTrigger(trigger);
    }

    [Command]
    private void CmdResetTrigger(string trigger)
    {
        RpcResetTrigger(trigger);
    }

    [ClientRpc]
    private void RpcSetTrigger(string trigger)
    {
        if (_animator != null)
            _animator.SetTrigger(trigger);
    }

    [ClientRpc]
    private void RpcResetTrigger(string trigger)
    {
        if (_animator != null)
            _animator.ResetTrigger(trigger);
    }

    private void OnIsMovingChanged(bool oldValue, bool newValue)
    {
        if (_animator != null)
            _animator.SetBool("IsMoving", newValue);
    }

    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        if (_animator != null)
            _animator.SetBool("IsDead", newValue);
    }

    [Client]
    public void ResetAnimations()
    {
        if (_animator != null && !_core.Movement.IsMoving)
        {
            CmdSetIsMoving(false);
            CmdResetTrigger("Attack");
            CmdResetTrigger("SkillCast");
            CmdResetTrigger("Death");
            CmdSetIsDead(false);
            _animator.speed = 1f;
            _animator.SetTrigger("Idle");
            Debug.Log("[PlayerAnimationSystem] Animations reset to Idle");
        }
    }

    [Client]
    public void TriggerAttackAnimation()
    {
        CmdSetTrigger("Attack");
    }

    private void OnDisable()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Kill();
        }
    }
}