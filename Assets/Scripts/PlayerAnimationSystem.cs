using UnityEngine;
using Mirror;
using DG.Tweening;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

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
    private string _currentAnimation = "Player_Idle"; // Базовая анимация для инициализации системы анимаций
    private Inventory _inventory;
    private Item.WeaponType _currentWeaponType = Item.WeaponType.None;
    private Dictionary<string, List<string>> _actionAnimations = new Dictionary<string, List<string>>();

    private void Awake()
    {
        _actionSystem = GetComponent<PlayerActionSystem>();
        _core = GetComponent<PlayerCore>();
        _stats = GetComponent<CharacterStats>();
        _inventory = GetComponent<Inventory>();
        // Debug logs commented out - user requested reduction of non-experience logs
        // if (_actionSystem == null) Debug.LogError("[PlayerAnimationSystem] PlayerActionSystem is null!");
        // if (_core == null) Debug.LogError("[PlayerAnimationSystem] PlayerCore is null!");
        // if (_stats == null) Debug.LogError("[PlayerAnimationSystem] CharacterStats is null!");
        // if (_inventory == null) Debug.LogError("[PlayerAnimationSystem] Inventory is null!");
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
            // Debug.LogError("[PlayerAnimationSystem] Stats or characterModels are null or empty!");
            return;
        }
        ClassData classData = Resources.Load<ClassData>($"ClassData/{_stats.characterClass}");
        if (classData == null)
        {
            // Debug.LogError($"[PlayerAnimationSystem] Failed to load ClassData for {_stats.characterClass}");
            return;
        }
        if (_activeModel != null)
        {
            _activeModel.SetActive(false);
        }
        _activeModel = characterModels.FirstOrDefault(model => model.name == classData.modelPrefab.name);
        if (_activeModel == null)
        {
            // Debug.LogError($"[PlayerAnimationSystem] Model for {_stats.characterClass} not found! Available models: {string.Join(", ", characterModels.Select(m => m.name))}");
            return;
        }
        _activeModel.SetActive(true);
        _animator = _activeModel.GetComponent<Animator>();
        if (_animator == null)
        {
            // Debug.LogError($"[PlayerAnimationSystem] Animator not found on model {_activeModel.name} for {_stats.characterClass}");
            return;
        }
        if (classData.animatorController != null)
        {
            _animator.runtimeAnimatorController = classData.animatorController;
        }
        else
        {
            // Debug.LogWarning($"[PlayerAnimationSystem] AnimatorController not set in ClassData for {_stats.characterClass}");
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
            // Debug.Log($"[PlayerAnimationSystem] Renderer found on {_activeModel.name} or its children");
        }
        else
        {
            // Debug.LogError($"[PlayerAnimationSystem] No Renderer found on active model {_activeModel.name} or its children!");
        }
        // Debug.Log($"[PlayerAnimationSystem] Set model {_activeModel.name} and animator for {_stats.characterClass}");
        UpdateWeaponAnimations(); // ������������� ��������
    }

    private void UpdateWeaponAnimations()
    {
        // Система поиска анимаций в Animator Controller:
        // - Базовые анимации: Idle, Player_Walk, Player_Attack, Player_Cast, Death
        // - Варианты анимаций: Player_Attack2, Player_Attack3, Player_Walk2 и т.д. (до 5 вариантов, например до Player_Attack5)
        // - С оружием: Player_Attack_OneHandedSword, Player_Walk_TwoHandedSword, Player_Cast_Staff и т.д. (WeaponType из Item)
        // - Варианты с оружием: Player_Attack_OneHandedSword2, Player_Attack_OneHandedSword3 и т.д.
        // - Если анимация с оружием не найдена, используется fallback на базовую анимацию
        // - Все анимации должны быть в базовом слое (layer 0), с переходами (transitions), и loop для Walk/Idle/Attack анимаций

        _currentWeaponType = GetCurrentWeaponType();
        _actionAnimations.Clear();
        string[] actions = { "Idle", "Walk", "Attack", "Cast" };
        
        foreach (var action in actions)
        {
            List<string> anims = new List<string>();
            
            // 1. Пробуем найти анимации с оружием
            if (_currentWeaponType != Item.WeaponType.None)
            {
                string weaponBaseName = $"Player_{action}_{_currentWeaponType}";
                AddAnimationsToList(anims, weaponBaseName);
            }
            
            // 2. Если не найдены анимации с оружием, используем базовые
            if (anims.Count == 0)
            {
                string baseName = $"Player_{action}";
                AddAnimationsToList(anims, baseName);
            }
            
            // 3. Если все еще нет анимаций, используем стартовую анимацию аниматора
            if (anims.Count == 0)
            {
                string fallbackName = GetFallbackAnimationName(action);
                if (!string.IsNullOrEmpty(fallbackName))
                {
                    anims.Add(fallbackName);
                }
            }
            
            _actionAnimations[action] = anims;
            Debug.Log($"[PlayerAnimationSystem] Cached {action} animations for {_currentWeaponType}: {string.Join(", ", anims)}");
        }
    }
    
    /// <summary>
    /// Добавляет анимации в список, проверяя их существование в аниматоре
    /// </summary>
    private void AddAnimationsToList(List<string> anims, string baseName)
    {
        // Проверяем базовую анимацию (без номера)
        int hash = Animator.StringToHash(baseName);
        if (_animator.HasState(0, hash))
        {
            anims.Add(baseName);
        }
        
        // Проверяем варианты анимаций (1,2,3...)
        for (int i = 2; i <= 5; i++) // Начинаем с 2, так как базовая уже проверена
        {
            string name = $"{baseName}{i}";
            hash = Animator.StringToHash(name);
            if (_animator.HasState(0, hash))
            {
                anims.Add(name);
            }
        }
    }
    
    /// <summary>
    /// Получает имя fallback анимации для действия
    /// </summary>
    private string GetFallbackAnimationName(string action)
    {
        // Пробуем различные варианты fallback анимаций
        string[] fallbackNames = {
            action, // Простое имя (Idle, Walk, Attack, Cast)
            $"Player_{action}", // С префиксом Player_
            "Idle", // Всегда есть базовая Idle анимация
            "Player_Idle" // Базовая Idle с префиксом
        };
        
        foreach (string fallbackName in fallbackNames)
        {
            int hash = Animator.StringToHash(fallbackName);
            if (_animator.HasState(0, hash))
            {
                Debug.LogWarning($"[PlayerAnimationSystem] Using fallback animation '{fallbackName}' for action '{action}'");
                return fallbackName;
            }
        }
        
        // Если ничего не найдено, попробуем получить стартовую анимацию аниматора
        string defaultAnimation = GetDefaultAnimatorState();
        if (!string.IsNullOrEmpty(defaultAnimation))
        {
            Debug.LogWarning($"[PlayerAnimationSystem] Using default animator state '{defaultAnimation}' for action '{action}'");
            return defaultAnimation;
        }
        
        Debug.LogError($"[PlayerAnimationSystem] No fallback animation found for action '{action}'!");
        return null;
    }
    
    /// <summary>
    /// Получает стартовую анимацию аниматора
    /// </summary>
    private string GetDefaultAnimatorState()
    {
        if (_animator == null) return null;
        
#if UNITY_EDITOR
        // Получаем все состояния аниматора (только в Editor)
        AnimatorController controller = _animator.runtimeAnimatorController as AnimatorController;
        if (controller == null) return null;
        
        // Ищем первое состояние в базовом слое
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine.states.Length > 0)
        {
            string defaultStateName = stateMachine.states[0].state.name;
            Debug.Log($"[PlayerAnimationSystem] Found default animator state: '{defaultStateName}'");
            return defaultStateName;
        }
#else
        // В runtime пробуем найти анимацию через текущее состояние
        AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName("Idle") || currentState.IsName("Player_Idle"))
        {
            return currentState.IsName("Idle") ? "Idle" : "Player_Idle";
        }
        
        // Пробуем найти любую доступную анимацию
        string[] commonAnimations = { "Idle", "Player_Idle", "Player_Walk", "Player_Attack" };
        foreach (string animName in commonAnimations)
        {
            int hash = Animator.StringToHash(animName);
            if (_animator.HasState(0, hash))
            {
                Debug.Log($"[PlayerAnimationSystem] Found runtime fallback animation: '{animName}'");
                return animName;
            }
        }
#endif
        
        return null;
    }
    
    /// <summary>
    /// Отладочный метод для вывода всех доступных анимаций
    /// </summary>
    [ContextMenu("Debug Available Animations")]
    public void DebugAvailableAnimations()
    {
        if (_animator == null)
        {
            Debug.LogError("[PlayerAnimationSystem] Animator is null!");
            return;
        }
        
#if UNITY_EDITOR
        AnimatorController controller = _animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("[PlayerAnimationSystem] AnimatorController is null!");
            return;
        }
        
        Debug.Log($"[PlayerAnimationSystem] === Available Animations Debug ===");
        Debug.Log($"[PlayerAnimationSystem] Current Weapon Type: {_currentWeaponType}");
        
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Debug.Log($"[PlayerAnimationSystem] Total states in animator: {stateMachine.states.Length}");
        
        foreach (var state in stateMachine.states)
        {
            Debug.Log($"[PlayerAnimationSystem] - {state.state.name}");
        }
#else
        Debug.Log($"[PlayerAnimationSystem] === Available Animations Debug ===");
        Debug.Log($"[PlayerAnimationSystem] Current Weapon Type: {_currentWeaponType}");
        Debug.Log($"[PlayerAnimationSystem] Debug mode only available in Editor");
#endif
        
        Debug.Log($"[PlayerAnimationSystem] === Cached Action Animations ===");
        foreach (var kvp in _actionAnimations)
        {
            Debug.Log($"[PlayerAnimationSystem] {kvp.Key}: {string.Join(", ", kvp.Value)}");
        }
        
        Debug.Log($"[PlayerAnimationSystem] === Fallback Tests ===");
        string[] testActions = { "Idle", "Walk", "Attack", "Cast" };
        foreach (string action in testActions)
        {
            string fallback = GetFallbackAnimationName(action);
            Debug.Log($"[PlayerAnimationSystem] {action} fallback: {fallback ?? "NOT FOUND"}");
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
        
        // Если нет анимаций в кэше, попробуем найти fallback
        string fallbackName = GetFallbackAnimationName(action);
        if (!string.IsNullOrEmpty(fallbackName))
        {
            Debug.LogWarning($"[PlayerAnimationSystem] Using direct fallback '{fallbackName}' for action '{action}'");
            return fallbackName;
        }
        
        // Последний fallback - базовая Idle анимация
        Debug.LogError($"[PlayerAnimationSystem] No animation found for action '{action}', using 'Idle' as last resort");
        return "Idle";
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
    private const float ANIMATION_UPDATE_INTERVAL = 0.05f; // Обновляем анимации каждые 50мс для лучшей синхронизации атак
    
    private void Update()
    {
        if (_actionSystem == null || _animator == null || _core == null || !isOwned) return;
        
        // Оптимизация: проверяем критические состояния каждый кадр, анимации - с интервалом
        if (_core.isDead && _currentAnimation != "Death")
        {
            CmdPlayAnimation("Death");
            // Debug.Log("[PlayerAnimationSystem] Played Death animation");
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
        // Для атак и кастов обновляем чаще для лучшей синхронизации
        float updateInterval = (_actionSystem.CurrentAction == PlayerAction.Attack || _actionSystem.CurrentAction == PlayerAction.SkillCast) ? 0.02f : ANIMATION_UPDATE_INTERVAL;
        
        if (Time.time - _lastAnimationUpdate >= updateInterval)
        {
            UpdateAnimations();
            _lastAnimationUpdate = Time.time;
        }
    }

    [Client]
    private void UpdateAnimations()
    {
        if (_core.isDead) return;

        string targetAnimation = GetRandomAnimation("Idle");
        

        if (_actionSystem.CurrentAction == PlayerAction.Move)
        {
            targetAnimation = GetRandomAnimation("Walk");
            _animator.speed = 1f;
        }
        else if (_actionSystem.CurrentAction == PlayerAction.Attack && _actionSystem.CurrentTarget != null && _actionSystem.CurrentSkill != null)
        {
            float attackRange = _actionSystem.CurrentSkill.Range;
            float distance = Vector3.Distance(transform.position, _actionSystem.CurrentTarget.transform.position);
            
            // Проверяем, действительно ли персонаж движется к цели
            bool isActuallyMoving = _core.Movement.IsMoving && _core.Movement.Agent.velocity.magnitude > 0.1f;
            
            if (distance > attackRange)
            {
                // Цель вне радиуса - показываем Walk только если действительно движемся
                if (isActuallyMoving)
                {
                    targetAnimation = GetRandomAnimation("Walk");
                    _animator.speed = 1f;
                }
                else
                {
                    // Если не движемся, показываем Idle (персонаж стоит и смотрит на цель)
                    targetAnimation = GetRandomAnimation("Idle");
                    _animator.speed = 1f;
                }
            }
            else
            {
                // Проверяем, действительно ли персонаж остановился для атаки или продолжает преследование
                bool isStillMoving = _core.Movement.IsMoving && _core.Movement.Agent.velocity.magnitude > 0.1f;
                
                if (isStillMoving)
                {
                    // Персонаж все еще движется - показываем Walk (преследование)
                    targetAnimation = GetRandomAnimation("Walk");
                    _animator.speed = 1f;
                }
                else
                {
                    // Персонаж остановился - проверяем состояние атаки
                    if (_actionSystem.isWaitingForAttackCooldown)
                    {
                        // Если ждем кулдаун, продолжаем текущую анимацию атаки или переключаемся на Idle
                        if (_currentAnimation.Contains("Attack"))
                        {
                            targetAnimation = _currentAnimation; // Продолжаем анимацию атаки
                            // Устанавливаем скорость анимации равную скорости атаки, но не меньше 1.0
                            float animationSpeed = Mathf.Max(1.0f, _stats.attackSpeed);
                            _animator.speed = animationSpeed;
                        }
                        else
                        {
                            targetAnimation = GetRandomAnimation("Idle"); // Если анимация атаки закончилась, переключаемся на Idle
                        }
                    }
                    else if (_actionSystem.CurrentSkill is BasicAttackSkill || _actionSystem.CurrentAction == PlayerAction.Attack)
                    {
                        // Атака: показываем анимацию атаки если не ждем кулдаун
                        if (_currentAnimation.Contains("Attack")) // Если уже атакуем, продолжаем анимацию
                        {
                            targetAnimation = _currentAnimation;
                            // Устанавливаем скорость анимации равную скорости атаки, но не меньше 1.0
                            float animationSpeed = Mathf.Max(1.0f, _stats.attackSpeed);
                            _animator.speed = animationSpeed;
                        }
                        else
                        {
                            targetAnimation = GetRandomAnimation("Attack"); // Запускаем новую анимацию если не атакуем и не ждем кулдаун
                        }
                    }
                    else
                    {
                        // Если не атака и не ждем кулдаун - показываем Idle
                        targetAnimation = GetRandomAnimation("Idle");
                        _animator.speed = 1f;
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
                targetAnimation = GetRandomAnimation("Idle");
                _animator.speed = 1f;
                return;
            }
            
            // Проверяем, действительно ли персонаж движется к цели для каста
            // Используем более надежную проверку: есть ли активное назначение движения
            bool isActuallyMoving = _core.Movement.Agent != null && 
                                   !_core.Movement.Agent.isStopped && 
                                   _core.Movement.Agent.hasPath && 
                                   _core.Movement.Agent.remainingDistance > 0.1f;
            
            if (distance > castRange)
            {
                // Цель вне радиуса - показываем Walk только если действительно движемся
                if (isActuallyMoving)
                {
                    targetAnimation = GetRandomAnimation("Walk");
                    _animator.speed = 1f;
                }
                else
                {
                    // Если не движемся, показываем Idle (персонаж стоит и смотрит на цель)
                    targetAnimation = GetRandomAnimation("Idle");
                    _animator.speed = 1f;
                }
            }
            else
            {
                // В радиусе каста - проверяем состояние каста
                if (_actionSystem.IsCasting)
                {
                    // Если кастуем, показываем анимацию каста
                    if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && _currentAnimation.Contains("Cast")) 
                    {
                        targetAnimation = _currentAnimation; // Продолжаем анимацию каста
                    }
                    else
                    {
                        targetAnimation = GetRandomAnimation("Cast"); // Запускаем новую анимацию каста
                    }
                }
                else
                {
                    // Если не кастуем, но в радиусе - показываем Idle
                    targetAnimation = GetRandomAnimation("Idle");
                    _animator.speed = 1f;
                }
            }
        }
        else
        {
            // Если нет активного действия, но текущая анимация еще не завершена - продолжаем её
            if (_currentAnimation.Contains("Cast") && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                targetAnimation = _currentAnimation; // Продолжаем анимацию каста
            }
            else if (_currentAnimation.Contains("Attack") && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                targetAnimation = _currentAnimation; // Продолжаем анимацию атаки
            }
        else
        {
            targetAnimation = GetRandomAnimation("Idle");
            // Сбрасываем скорость анимации для idle
            _animator.speed = 1f;
        }
        }

        if (_currentAnimation != targetAnimation)
        {
            CmdPlayAnimation(targetAnimation);
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
            // Устанавливаем правильную скорость анимации в зависимости от типа
            if (stateName.Contains("Attack"))
            {
                // Для атак: скорость равна attackSpeed, но не меньше 1.0
                float animationSpeed = Mathf.Max(1.0f, _stats.attackSpeed);
                _animator.speed = animationSpeed;
            }
            else
            {
                // Для всех остальных анимаций: нормальная скорость
                _animator.speed = 1f;
            }
            
            if (stateName == _currentAnimation)
            {
                // Для анимаций атаки всегда перезапускаем, чтобы обеспечить синхронизацию
                if (stateName.Contains("Attack"))
                {
                    _animator.Play(stateName, 0, 0f); // Мгновенный переход без CrossFade
                }
                else
                {
                    // Не перезапускаем анимацию, если она уже играет (кроме атак)
                    return;
                }
            }
            else
            {
                _animator.Play(stateName, 0, 0f); // Мгновенный переход без CrossFade
            }
            _currentAnimation = stateName; // Update current animation
        }
    }

    [Client]
    public void ResetAnimations()
    {
        if (_animator != null)
        {
            _animator.speed = 1f;
            
            // Если персонаж не движется, переключаемся на Idle
            if (!_core.Movement.IsMoving)
            {
                CmdPlayAnimation(GetRandomAnimation("Idle"));
            }
            else
            {
                // Если все еще движется, переключаемся на Walk
                CmdPlayAnimation(GetRandomAnimation("Walk"));
            }
        }
    }

    [Client]
    public void TriggerAttackAnimation(float attackSpeed = 1.0f)
    {
        // Устанавливаем скорость анимации равную скорости атаки, но не меньше 1.0
        float animationSpeed = Mathf.Max(1.0f, attackSpeed);
        _animator.speed = animationSpeed;
        
        // Принудительно запускаем анимацию атаки, даже если она уже играет
        string attackAnimation = GetRandomAnimation("Attack");
        CmdPlayAnimation(attackAnimation);
    }

    private void OnDisable()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Kill();
        }
    }
}