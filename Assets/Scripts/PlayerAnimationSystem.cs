using UnityEngine;
using Mirror;
using DG.Tweening;
using System.Linq;
using System.Collections.Generic;

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
    private string _currentAnimation = "Idle"; // ������� ������� ��� ��������� ��������� �������
    private Inventory _inventory;
    private Item.WeaponType _currentWeaponType = Item.WeaponType.None;
    private Dictionary<string, List<string>> _actionAnimations = new Dictionary<string, List<string>>();

    private void Awake()
    {
        _actionSystem = GetComponent<PlayerActionSystem>();
        _core = GetComponent<PlayerCore>();
        _stats = GetComponent<CharacterStats>();
        _inventory = GetComponent<Inventory>();
        if (_actionSystem == null) Debug.LogError("[PlayerAnimationSystem] PlayerActionSystem is null!");
        if (_core == null) Debug.LogError("[PlayerAnimationSystem] PlayerCore is null!");
        if (_stats == null) Debug.LogError("[PlayerAnimationSystem] CharacterStats is null!");
        if (_inventory == null) Debug.LogError("[PlayerAnimationSystem] Inventory is null!");
        characterModels = GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("CharacterModel"))
            .Select(t => t.gameObject)
            .ToArray();
        foreach (var model in characterModels)
        {
            model.SetActive(false);
        }
        _stats.OnCharacterClassChangedEvent += OnCharacterClassChanged;
        _inventory.OnEquipmentChanged.AddListener(UpdateWeaponAnimations);
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
        UpdateWeaponAnimations(); // ������������� ��������
    }

    private void UpdateWeaponAnimations()
    {
        // ������� �� ���������� �������� � Animator Controller:
        // - ������� ������: Idle, Player_Walk, Player_Attack, Player_Cast, Death
        // - �������� �������: Player_Attack2, Player_Attack3, Player_Walk2 � �.�. (�� 5 ��������, ��� ��������� �� Player_Attack5)
        // - � ����� ������: Player_Attack_OneHandedSword, Player_Walk_TwoHandedSword, Player_Cast_Staff � �.�. (WeaponType �� Item)
        // - �������� � �������: Player_Attack_OneHandedSword2, Player_Attack_OneHandedSword3 � �.�.
        // - ���� ����� � ��������� ������ ����������, �� �����������; fallback �� ������� ��� ��������
        // - ��� ������ ������ ���� � ������� ���� (layer 0), ��� ��������� (transitions), � loop ��� Walk/Idle/Attack ���� �����

        _currentWeaponType = GetCurrentWeaponType();
        _actionAnimations.Clear();
        string[] actions = { "Walk", "Attack", "Cast" };
        foreach (var action in actions)
        {
            string baseName = $"Player_{action}";
            string suffixed = _currentWeaponType != Item.WeaponType.None ? $"Player_{action}_{_currentWeaponType}" : baseName;
            int hash = Animator.StringToHash(suffixed);
            if (_animator.HasState(0, hash))
            {
                baseName = suffixed;
            }
            // �������� �������� (1,2,3...)
            List<string> anims = new List<string>();
            for (int i = 1; i <= 5; i++) // ���������, ���� ����� ������
            {
                string name = i == 1 ? baseName : $"{baseName}{i}";
                hash = Animator.StringToHash(name);
                if (_animator.HasState(0, hash))
                {
                    anims.Add(name);
                }
            }
            if (anims.Count == 0)
            {
                // Fallback �� ������� ��� �������� � ��������
                anims.Add($"Player_{action}");
            }
            _actionAnimations[action] = anims;
            Debug.Log($"[PlayerAnimationSystem] Cached {action} animations for {_currentWeaponType}: {string.Join(", ", anims)}");
        }
    }

    private Item.WeaponType GetCurrentWeaponType()
    {
        // Проверяем двуручное оружие
        Item twoHandedWeapon = _inventory.leftHandSlot.GetItem();
        if (twoHandedWeapon != null && twoHandedWeapon.itemType == ItemType.Weapon && twoHandedWeapon.isTwoHanded)
        {
            return twoHandedWeapon.weaponType;
        }

        // Проверяем дуальное оружие (два одноручных оружия)
        Item rightHandWeapon = _inventory.rightHandSlot.GetItem();
        Item leftHandWeapon = _inventory.leftHandSlot.GetItem();
        
        bool hasRightWeapon = rightHandWeapon != null && rightHandWeapon.itemType == ItemType.Weapon && !rightHandWeapon.isTwoHanded;
        bool hasLeftWeapon = leftHandWeapon != null && leftHandWeapon.itemType == ItemType.Weapon && !leftHandWeapon.isTwoHanded;
        
        if (hasRightWeapon && hasLeftWeapon)
        {
            // Дуальное оружие - возвращаем специальный тип
            return Item.WeaponType.DualWeapons;
        }
        else if (hasRightWeapon)
        {
            return rightHandWeapon.weaponType;
        }
        else if (hasLeftWeapon)
        {
            return leftHandWeapon.weaponType;
        }

        // Проверяем старый слот weapon (для совместимости)
        Item weapon = _inventory.weaponSlot.GetItem();
        if (weapon != null && weapon.itemType == ItemType.Weapon) return weapon.weaponType;

        return Item.WeaponType.None;
    }

    private string GetRandomAnimation(string action)
    {
        if (_actionAnimations.TryGetValue(action, out var anims) && anims.Count > 0)
        {
            return anims[Random.Range(0, anims.Count)];
        }
        return $"Player_{action}"; // Ult fallback
    }

    public GameObject GetActiveModel()
    {
        return _activeModel;
    }

    public void PlayDamageFlash()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Rewind();
            damageFlashSequence.Play();
        }
    }

    // Оптимизация: интервалы обновления для анимаций
    private float _lastAnimationUpdate = 0f;
    private const float ANIMATION_UPDATE_INTERVAL = 0.1f; // Обновляем анимации каждые 100мс
    
    private void Update()
    {
        if (_actionSystem == null || _animator == null || _core == null || !isOwned) return;
        
        // Оптимизация: проверяем критические состояния каждый кадр, анимации - с интервалом
        if (_core.isDead && _currentAnimation != "Death")
        {
            CmdPlayAnimation("Death");
            Debug.Log("[PlayerAnimationSystem] Played Death animation");
        }
        else if (!_core.isDead && _currentAnimation == "Death")
        {
            ResetAnimations();
        }
        
        if (_wasPerformingAction && !_actionSystem.IsPerformingAction)
        {
            ResetAnimations();
        }
        _wasPerformingAction = _actionSystem.IsPerformingAction;
        
        // Оптимизация: обновляем анимации с интервалом
        if (Time.time - _lastAnimationUpdate >= ANIMATION_UPDATE_INTERVAL)
        {
            UpdateAnimations();
            _lastAnimationUpdate = Time.time;
        }
    }

    [Client]
    private void UpdateAnimations()
    {
        if (_core.isDead) return;

        string targetAnimation = "Idle";

        if (_actionSystem.CurrentAction == PlayerAction.Move)
        {
            targetAnimation = GetRandomAnimation("Walk");
            _animator.speed = 1f;
        }
        else if (_actionSystem.CurrentAction == PlayerAction.Attack && _actionSystem.CurrentTarget != null && _actionSystem.CurrentSkill != null)
        {
            float attackRange = _actionSystem.CurrentSkill.Range;
            float distance = Vector3.Distance(transform.position, _actionSystem.CurrentTarget.transform.position);
            if (distance > attackRange)
            {
                targetAnimation = GetRandomAnimation("Walk");
                _animator.speed = 1f;
            }
            else
            {
                if (_actionSystem.CurrentSkill is BasicAttackSkill)
                {
                    if (_currentAnimation.Contains("Attack")) // ���� ��� �����, ��������� ������� ��������
                    {
                        targetAnimation = _currentAnimation;
                    }
                    else
                    {
                        targetAnimation = GetRandomAnimation("Attack"); // �������� ����� ������ ��� �������� � �����
                    }
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
                targetAnimation = "Idle";
                _animator.speed = 1f;
                return;
            }
            if (distance > castRange)
            {
                targetAnimation = GetRandomAnimation("Walk");
                _animator.speed = 1f;
            }
            else
            {
                if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && _currentAnimation.Contains("Cast")) return; // �� ��������� ����
                targetAnimation = GetRandomAnimation("Cast");
            }
        }
        else
        {
            targetAnimation = "Idle";
        }

        if (_currentAnimation != targetAnimation)
        {
            CmdPlayAnimation(targetAnimation);
            Debug.Log($"[PlayerAnimationSystem] Played {targetAnimation}, CurrentAction: {_actionSystem.CurrentAction}");
        }
    }

    [Command]
    private void CmdPlayAnimation(string stateName)
    {
        RpcPlayAnimation(stateName);
    }

    [ClientRpc]
    private void RpcPlayAnimation(string stateName)
    {
        if (_animator != null)
        {
            if (stateName.Contains("Attack"))
            {
                AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
                float duration = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1f;
                _animator.speed = duration * _stats.attackSpeed;
            }
            if (stateName == _currentAnimation)
            {
                _animator.Play(stateName, 0, 0f); // Restart ��������
            }
            else
            {
                _animator.CrossFade(stateName, 0.1f, 0);
            }
            _currentAnimation = stateName; // ��������� �������� �� ���� ��������
        }
    }

    [Client]
    public void ResetAnimations()
    {
        if (_animator != null && !_core.Movement.IsMoving)
        {
            _animator.speed = 1f;
            CmdPlayAnimation("Idle");
            Debug.Log("[PlayerAnimationSystem] Animations reset to Idle");
        }
    }

    [Client]
    public void TriggerAttackAnimation()
    {
        CmdPlayAnimation(GetRandomAnimation("Attack"));
    }

    private void OnDisable()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Kill();
        }
    }
}