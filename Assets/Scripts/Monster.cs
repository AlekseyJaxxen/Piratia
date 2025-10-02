using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using DG.Tweening;
using System.Linq;

[System.Serializable]
public class DropEntry
{
    public Item item;
    [Range(0f, 1f)] public float dropChance = 0.1f;
}

public class Monster : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnMonsterIdChanged))] public int monsterId;
    public MonsterInfo info;
    [Header("Monster Settings")]
    [SyncVar(hook = nameof(OnNameChanged))] public string monsterName = "Monster";
    private float moveSpeed = 5f;
    private float attackCooldown = 2f;
    private GameObject deathVFXPrefab;
    public bool canMove = true;
    
    [Header("Combat Stats")]
    [SyncVar] public int hitRate = 10;
    [SyncVar] public int dodge = 10;
    
    // Методы для управления анимациями combined монстра
    // PlayAnimation работает и на сервере и на клиенте
    public void PlayAnimation(string animationName)
    {
        if (!NetworkServer.active && !NetworkClient.active)
        {
            // Вызываю локально (например в Editor)
            PlayAnimationLocal(animationName);
            return;
        }
        
        if (NetworkServer.active)
        {
            // Сервер: управляем анимацией через систему RPC
            if (isCombinedLegs && partnerMonster != null)
            {
                // Legs управляет аниматором head
                if (animationName != "Walk" && animationName != "Idle")
                {
                    Debug.Log($"[Monster] Server - Legs {name} playing animation {animationName} on head {partnerMonster.name}");
                }
                partnerMonster.RpcPlayAnimation(animationName);
            }
            else
            {
                if (animationName != "Walk" && animationName != "Idle")
                {
                    Debug.Log($"[Monster] Server - Monster {name} playing animation {animationName}");
                }
                PlayAnimationLocal(animationName);
            }
        }
        else if (NetworkClient.active)
        {
            // Клиент: играем анимацию локально
            if (animationName != "Walk" && animationName != "Idle")
            {
                Debug.Log($"[Monster] Client - Monster {name} playing animation {animationName}");
            }
            PlayAnimationLocal(animationName);
        }
    }
    
    /// <summary>
    /// Локальное воспроизведение анимации с поддержкой обеих систем
    /// </summary>
    private void PlayAnimationLocal(string animationName)
    {
        if (IsHumanoidMonster())
        {
            // Гуманоидный монстр: используем Animator
            _animator.Play(animationName);
        }
        else if (IsNonHumanoidMonster())
        {
            // Не-гуманоидный монстр: используем Animation
            if (_animation[animationName] != null)
            {
                _animation.Play(animationName);
            }
            else
            {
                Debug.LogWarning($"[Monster] Animation clip '{animationName}' not found on {monsterName}");
            }
        }
        else
        {
            Debug.LogWarning($"[Monster] Cannot play animation '{animationName}' on {monsterName}: no animation system found");
        }
    }
    
    [ClientRpc]
    public void RpcPlayAnimation(string animationName)
    {
        if (isCombinedLegs && partnerMonster != null)
        {
            // Legs управляет аниматором head НО для атаки используем правильную анимацию
            if (animationName == "LegsKick")
            {
                // Для legs атаки играем Kick на head аниматоре
                if (animationName != "Walk" && animationName != "Idle")
                {
                    Debug.Log($"[Monster] RPC: Legs {name} playing animation LegsKick on head {partnerMonster.name}");
                }
                partnerMonster.PlayAnimationLocal("LegsKick");
            }
            else
            {
                // Для остальных анимаций передаем управление head
                if (animationName != "Walk" && animationName != "Idle")
                {
                    Debug.Log($"[Monster] RPC: Legs {name} delegating animation {animationName} to head {partnerMonster.name}");
                }
                partnerMonster.RpcPlayAnimation(animationName);
            }
        }
        else
        {
            // Head или обычный монстр управляет своей анимационной системой
            if (animationName != "Walk" && animationName != "Idle")
            {
                Debug.Log($"[Monster] RPC: {name} playing animation {animationName}");
            }
            PlayAnimationLocal(animationName);
        }
    }
    
    [ClientRpc]
    public void RpcPlayAnimationById(int animationId)
    {
        if (isCombinedLegs && partnerMonster != null)
        {
            // Для combined монстров передаем управление head
            partnerMonster.RpcPlayAnimationById(animationId);
        }
        else
        {
            // Head или обычный монстр управляет своей анимационной системой
            Debug.Log($"[Monster] RPC: {name} playing animation by ID {animationId}");
            PlayAnimationById(animationId);
        }
    }
    
    // Упрощенные методы для combined монстров
    [Server]
    public void ExecuteAttack(PlayerCore target = null)
    {
        if (!canAttack || IsDead || IsStunned || IsSilenced) 
        {
            Debug.Log($"[Monster] ExecuteAttack blocked for {name}: canAttack={canAttack}, IsDead={IsDead}, IsStunned={IsStunned}, IsSilenced={IsSilenced}");
            return;
        }
        
        if (isCombinedHead)
        {
            // Атака головы
            string partnerName = partnerMonster != null ? partnerMonster.name : "null";
            Debug.Log($"[Monster] Head attacking: {name}, partner: {partnerName}, _animator: {_animator != null}");
            
            // ДИАГНОСТИКА: проверяем состояние в ExecuteAttack(PlayerCore)
            bool headIsAlive = !IsDead;
            bool partnerIsDead = partnerMonster == null || partnerMonster.IsDead;
            bool headShouldBeFallen = headIsAlive && partnerIsDead; // Голова жива НО legs мертвы = упала
            
            Debug.Log($"[Monster] ExecuteAttack(PlayerCore) - HeadAlive: {headIsAlive}, PartnerDead: {partnerIsDead}, shouldBeFallen: {headShouldBeFallen}");
            
            // Проверяем: голова упала (legs мертвы и он сама жива) ИЛИ головы просто стоит
            if (headShouldBeFallen)
            {
                // Голова упала - играем анимацию лежа
                Debug.Log($"[Monster] ExecuteAttack(PlayerCore) Playing HeadPunchLying for FALLEN head");
                PlayAnimation("HeadPunchLying");
                RpcPlayAnimation("HeadPunchLying");
            }
            else
            {
                // Голова стоит - играем обычную анимацию
                Debug.Log($"[Monster] ExecuteAttack(PlayerCore) Playing HeadPunch for STANDING head");
                PlayAnimation("HeadPunch");
                RpcPlayAnimation("HeadPunch");
            }
            
            // Используем MonsterBasicAttackSkill для нанесения урона
            if (basicAttackSkill != null && target != null)
            {
                Debug.Log($"[Monster] Head using basicAttackSkill to attack {target.name}");
                basicAttackSkill.Execute(this, null, target.gameObject);
            }
            else
            {
                Debug.LogWarning($"[Monster] Head attack failed: basicAttackSkill={basicAttackSkill != null}, target={target != null}");
            }
        }
        else if (isCombinedLegs)
        {
            // Атака ног
            Debug.Log($"[Monster] Legs attacking: {name}, partner: {partnerMonster?.name}");
            PlayAnimation("LegsKick");
            RpcPlayAnimation("LegsKick");
            
            // Используем MonsterBasicAttackSkill для нанесения урона
            if (basicAttackSkill != null && target != null)
            {
                Debug.Log($"[Monster] Legs using basicAttackSkill to attack {target.name}");
                basicAttackSkill.Execute(this, null, target.gameObject);
            }
            else
            {
                Debug.LogWarning($"[Monster] Legs attack failed: basicAttackSkill={basicAttackSkill != null}, target={target != null}");
            }
        }
        else if (basicAttackSkill != null)
        {
            // Обычная атака - используем существующий метод
            Debug.Log($"[Monster] Regular monster attacking: {name}");
            // basicAttackSkill.ExecuteAttack(this);
        }
    }
    private bool canAttack = true;
    private GameObject slowEffectPrefab;
    [Header("Aggro & Experience")]
    [SyncVar] public uint aggroTargetNetId = 0;
    private int experienceReward = 50;
    [Header("Drop Settings")]
    private List<DropEntry> dropTable = new List<DropEntry>();
    private GameObject droppedItemPrefab;
    private GameObject _slowEffectInstance;
    private NavMeshAgent _agent;
    private MonsterUI _monsterUI;
    private Rigidbody _rigidbody;
    public bool IsDead;
    [SyncVar] private float _slowPercentage = 0f;
    [SyncVar] private float _originalSpeed = 0f;
    [SyncVar] private ControlEffectType _currentControlEffect = ControlEffectType.None;
    [SyncVar] private float _controlEffectEndTime = 0f;
    [SyncVar(hook = nameof(OnStunStateChanged))] public bool IsStunned = false;
    [SyncVar(hook = nameof(OnSilenceStateChanged))] public bool IsSilenced = false;
    [SyncVar] private int _currentEffectWeight = 0;
    private float stoppingDistance = 1f;
    public MonsterBasicAttackSkill basicAttackSkill;
    [Header("Physics Settings")]
    public GameObject physicsModel;
    public Vector3 minForce = new Vector3(-5f, 2f, -5f);
    public Vector3 maxForce = new Vector3(5f, 5f, 0f);
    [SyncVar] public bool IsCooldown = false;
    private SkinnedMeshRenderer _skinnedRenderer;
    private MeshRenderer _meshRenderer;
    
    // Универсальный геттер для любого рендерера
    public Renderer GetRenderer()
    {
        return (Renderer)_skinnedRenderer ?? _meshRenderer;
    }
    
    // Система анимаций
    private Animator _animator;        // Для гуманоидов (SkinnedMeshRenderer)
    private Animation _animation;      // Для не-гуманоидов (MeshRenderer)
    
    // Кэш анимаций для работы с ID
    private string[] _animationNames;  // Массив имен анимаций для быстрого доступа по индексу
    private Dictionary<string, int> _animationIds; // Словарь имя -> ID для обратного поиска
    
    // Универсальный геттер для определения типа анимации
    public bool IsHumanoidMonster()
    {
        return _skinnedRenderer != null && _animator != null;
    }
    
    public bool IsNonHumanoidMonster()
    {
        return _meshRenderer != null && _animation != null;
    }
    
    /// <summary>
    /// Получает информацию о доступных анимациях
    /// </summary>
    public string[] GetAvailableAnimations()
    {
        if (IsHumanoidMonster())
        {
            // Для Animator получаем информацию из RuntimeAnimatorController
            if (_animator.runtimeAnimatorController != null)
            {
                var clips = _animator.runtimeAnimatorController.animationClips;
                string[] names = new string[clips.Length];
                for (int i = 0; i < clips.Length; i++)
                {
                    names[i] = clips[i].name;
                }
                return names;
            }
        }
        else if (IsNonHumanoidMonster())
        {
            // Для Animation получаем список клипов
            var clips = new string[_animation.GetClipCount()];
            int index = 0;
            foreach (AnimationState state in _animation)
            {
                clips[index] = state.name;
                index++;
            }
            return clips;
        }
        
        return new string[0];
    }
    
    /// <summary>
    /// Проверяет, доступна ли анимация
    /// </summary>
    public bool HasAnimation(string animationName)
    {
        if (IsHumanoidMonster())
        {
            // Для Animator проверяем через HasState (требует hash)
            return _animator.HasState(0, Animator.StringToHash(animationName));
        }
        else if (IsNonHumanoidMonster())
        {
            // Для Animation проверяем наличие клипа
            return _animation[animationName] != null;
        }
        
        return false;
    }
    
    /// <summary>
    /// Останавливает все анимации
    /// </summary>
    public void StopAllAnimations()
    {
        if (IsHumanoidMonster())
        {
            // Для Animator останавливаем через параметры или переходим в Idle
            _animator.Play("Idle");
        }
        else if (IsNonHumanoidMonster())
        {
            // Для Animation останавливаем все клипы
            _animation.Stop();
        }
    }
    
    /// <summary>
    /// Инициализирует кэш анимаций для работы с ID
    /// </summary>
    private void InitializeAnimationCache()
    {
        if (IsHumanoidMonster())
        {
            // Для Animator получаем информацию из RuntimeAnimatorController
            if (_animator.runtimeAnimatorController != null)
            {
                var clips = _animator.runtimeAnimatorController.animationClips;
                _animationNames = new string[clips.Length];
                _animationIds = new Dictionary<string, int>();
                
                for (int i = 0; i < clips.Length; i++)
                {
                    _animationNames[i] = clips[i].name;
                    _animationIds[clips[i].name] = i;
                }
                
                Debug.Log($"[Monster] Initialized animation cache for humanoid {monsterName}: {clips.Length} animations");
            }
            else
            {
                Debug.LogWarning($"[Monster] RuntimeAnimatorController is null for humanoid {monsterName}");
            }
        }
        else if (IsNonHumanoidMonster())
        {
            // Для Animation получаем список клипов
            int count = _animation.GetClipCount();
            Debug.Log($"[Monster] Animation component has {count} clips for {monsterName}");
            
            if (count > 0)
            {
                _animationNames = new string[count];
                _animationIds = new Dictionary<string, int>();
                
                int index = 0;
                foreach (AnimationState state in _animation)
                {
                    string animName = state.name;
                    if (string.IsNullOrEmpty(animName))
                    {
                        Debug.LogWarning($"[Monster] Found animation with empty name at index {index} for {monsterName}");
                        animName = $"Animation_{index}"; // Fallback имя
                    }
                    
                    _animationNames[index] = animName;
                    _animationIds[animName] = index;
                    Debug.Log($"[Monster] Cached animation {index}: '{animName}' for {monsterName}");
                    index++;
                }
                
                Debug.Log($"[Monster] Initialized animation cache for non-humanoid {monsterName}: {count} animations");
            }
            else
            {
                Debug.LogWarning($"[Monster] No animation clips found in Animation component for {monsterName}");
            }
        }
        else
        {
            Debug.LogWarning($"[Monster] Cannot initialize animation cache - no valid animation system found for {monsterName}");
        }
        
        // Выводим список доступных анимаций с их ID
        if (_animationNames != null && _animationNames.Length > 0)
        {
            Debug.Log($"[Monster] Available animations for {monsterName}: {string.Join(", ", _animationNames.Select((name, id) => $"{id}:{name}"))}");
        }
        else
        {
            Debug.LogWarning($"[Monster] No animations available for {monsterName}");
        }
    }
    
    /// <summary>
    /// Воспроизводит анимацию по ID
    /// </summary>
    public void PlayAnimationById(int animationId)
    {
        if (_animationNames == null || animationId < 0 || animationId >= _animationNames.Length)
        {
            Debug.LogWarning($"[Monster] Invalid animation ID {animationId} for {monsterName}. Available IDs: 0-{(_animationNames?.Length - 1 ?? -1)}");
            return;
        }
        
        string animationName = _animationNames[animationId];
        
        // Дополнительная защита от пустых имен
        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[Monster] Animation at ID {animationId} has empty name for {monsterName}. Skipping playback.");
            return;
        }
        
        Debug.Log($"[Monster] Playing animation by ID {animationId}: '{animationName}' on {monsterName}");
        PlayAnimation(animationName);
    }
    
    /// <summary>
    /// Получает ID анимации по имени
    /// </summary>
    public int GetAnimationId(string animationName)
    {
        if (_animationIds != null && _animationIds.TryGetValue(animationName, out int id))
        {
            return id;
        }
        
        Debug.LogWarning($"[Monster] Animation '{animationName}' not found for {monsterName}");
        return -1;
    }
    
    /// <summary>
    /// Получает имя анимации по ID
    /// </summary>
    public string GetAnimationName(int animationId)
    {
        if (_animationNames != null && animationId >= 0 && animationId < _animationNames.Length)
        {
            return _animationNames[animationId];
        }
        
        Debug.LogWarning($"[Monster] Invalid animation ID {animationId} for {monsterName}");
        return null;
    }
    
    /// <summary>
    /// Получает количество доступных анимаций
    /// </summary>
    public int GetAnimationCount()
    {
        return _animationNames?.Length ?? 0;
    }
    
    /// <summary>
    /// Получает словарь всех анимаций (имя -> ID)
    /// </summary>
    public Dictionary<string, int> GetAnimationIdMap()
    {
        return new Dictionary<string, int>(_animationIds ?? new Dictionary<string, int>());
    }
    
    /// <summary>
    /// Воспроизводит анимацию по универсальному ID
    /// </summary>
    public void PlayUniversalAnimation(UniversalAnimationId universalId)
    {
        int animationId = (int)universalId;
        PlayAnimationById(animationId);
    }
    
    /// <summary>
    /// Проверяет, доступна ли анимация по универсальному ID
    /// </summary>
    public bool HasUniversalAnimation(UniversalAnimationId universalId)
    {
        int animationId = (int)universalId;
        return animationId >= 0 && animationId < GetAnimationCount();
    }
    
    /// <summary>
    /// Получает имя анимации по универсальному ID
    /// </summary>
    public string GetUniversalAnimationName(UniversalAnimationId universalId)
    {
        int animationId = (int)universalId;
        return GetAnimationName(animationId);
    }
    private HealthMonster _health;
    private MonsterSkillExecutor _skillExecutor;
    // ��� combined
    // Simplified Combined Settings
    [Header("Combined Settings")]
    [SyncVar] public bool isCombinedHead = false;
    [SyncVar] public bool isCombinedLegs = false;
    [SyncVar] public uint partnerNetId = 0; // Ссылка на партнера
    
    public Monster partnerMonster;
    public bool isFalling = false;  // Флаг для отслеживания падения головы
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }
        _health = GetComponent<HealthMonster>();
        if (_health == null)
        {
            Debug.LogError("[Monster] HealthMonster component missing!");
        }
        _skillExecutor = GetComponent<MonsterSkillExecutor>();
        if (_skillExecutor == null)
        {
            // Автоматически добавляем MonsterSkillExecutor если его нет
            _skillExecutor = gameObject.AddComponent<MonsterSkillExecutor>();
            Debug.Log($"[Monster] Added MonsterSkillExecutor component to {monsterName}");
        }
        // Ищем рендереры и анимационные компоненты
        _skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (_skinnedRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_meshRenderer == null)
            {
                Debug.LogWarning($"[Monster] Neither SkinnedMeshRenderer nor MeshRenderer found on {monsterName}");
            }
            else
            {
                Debug.Log($"[Monster] Found MeshRenderer on {monsterName} (non-humanoid)");
                // Для не-гуманоидов ищем Animation компонент
                _animation = GetComponentInChildren<Animation>();
                if (_animation == null)
                {
                    Debug.LogWarning($"[Monster] Animation component not found for non-humanoid monster {monsterName}");
                }
                else
                {
                    Debug.Log($"[Monster] Found Animation component on {monsterName}");
                    InitializeAnimationCache();
                }
            }
        }
        else
        {
            Debug.Log($"[Monster] Found SkinnedMeshRenderer on {monsterName} (humanoid)");
            // Для гуманоидов ищем Animator компонент
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning($"[Monster] Animator not found for humanoid monster {monsterName}");
            }
            else
            {
                Debug.Log($"[Monster] Found Animator on {monsterName}");
                InitializeAnimationCache();
            }
        }
    }
    
    private void Start()
    {
        if (isCombinedHead || isCombinedLegs)
        {
            StartCoroutine(FindPartnerDelayed());
        }
    }
    
    private IEnumerator FindPartnerDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (NetworkServer.spawned.TryGetValue(partnerNetId, out var partnerIdentity))
        {
            partnerMonster = partnerIdentity.GetComponent<Monster>();
        }
    }
    
    // Оптимизация: синхронизация combined монстров с интервалом
    private float _lastPositionSync = 0f;
    private const float POSITION_SYNC_INTERVAL = 0.1f; // Синхронизируем каждые 100мс
    
    private void Update()
    {
        if (isCombinedLegs && partnerMonster != null)
        {
            // Оптимизация: обновляем позицию не каждый кадр
            if (Time.time - _lastPositionSync < POSITION_SYNC_INTERVAL)
                return;
            _lastPositionSync = Time.time;
            
            // Legs двигает Head только если head не упал И head не в процессе падения
            if (!partnerMonster.IsDead && partnerMonster.isFalling == false)
            {
                // Проверяем позицию head - устанавливаем только если нужно
                Vector3 expectedHeadPos = transform.position + Vector3.up * 15f;
                if (Vector3.Distance(partnerMonster.transform.position, expectedHeadPos) > 1f)
                {
                    partnerMonster.transform.position = expectedHeadPos;
                    partnerMonster.transform.rotation = transform.rotation;
                }
            }
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        LoadAndInitializeServer();
        // Health initialized on server
        StartCoroutine(CheckControlEffectExpiration());
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        LoadAndInitializeClient();
        _monsterUI = GetComponentInChildren<MonsterUI>();
        if (_monsterUI == null)
        {
            Debug.LogError($"[Monster] MonsterUI component not found on {gameObject.name}. Check if it's a child object and has the component.");
            return;
        }
        _monsterUI.target = transform;
        _monsterUI.SetData(monsterName, _health.CurrentHealth, _health.MaxHealth);
        // UI initialized on client
        _health.OnHealthUpdated += OnHealthUpdatedHandler;
    }
    private void LoadAndInitializeServer()
    {
        info = LoadMonsterInfo();
        if (info == null)
        {
            Debug.LogError($"[Monster] MonsterInfo not loaded for ID {monsterId}!");
            return;
        }
        monsterName = info.monsterName;
        moveSpeed = info.moveSpeed;
        attackCooldown = info.attackCooldown;
        deathVFXPrefab = info.deathVFXPrefab;
        canMove = info.canMove;
        canAttack = info.canAttack;
        slowEffectPrefab = info.slowEffectPrefab;
        experienceReward = info.experienceReward;
        dropTable = info.dropTable;
        droppedItemPrefab = info.droppedItemPrefab;
        // stoppingDistance теперь вычисляется динамически в MonsterAI2
        basicAttackSkill = info.basicAttackSkill;
        physicsModel = info.physicsModel;
        minForce = info.minForce;
        maxForce = info.maxForce;
        if (basicAttackSkill == null) Debug.LogError("Skill not assigned");
        if (canMove && _agent != null)
        {
            _agent.baseOffset = 0.2f;
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = stoppingDistance;
            if (!_agent.isOnNavMesh)
            {
                Debug.LogWarning($"[Monster] {monsterName} is not on NavMesh at {transform.position}. Disabling movement.");
                canMove = false;
            }
        }
        if (physicsModel == null)
        {
            Debug.LogWarning($"[Monster] PhysicsModel not assigned for {monsterName}, using default GameObject");
        }
        if (canAttack && basicAttackSkill == null)
        {
            Debug.LogError("[Monster] MonsterBasicAttackSkill component missing!");
            canAttack = false;
        }
        if (droppedItemPrefab == null)
            Debug.LogWarning("[Monster] DroppedItemPrefab not set!");
        _health.MaxHealth = info.maxHealth;
        _health.CurrentHealth = info.maxHealth;
        
        // Initialize combat stats
        hitRate = info.hitRate;
        dodge = info.dodge;

        // Initialize AI with SO parameters
        MonsterAI2 ai2 = GetComponent<MonsterAI2>();
        if (ai2 != null)
        {
            ai2.InitializeAI(info);
        }

        // Initialize skill executor
        if (_skillExecutor != null)
        {
            _skillExecutor.InitializeSkills(info.monsterSkills);
        }

        // Combined
        if (info.isCombined)
        {
            if (info.legsInfo != null)
            {
                GameObject legsGO = Instantiate(MonsterSpawner.Instance.monsterPrefab, transform.position, Quaternion.identity);
                Monster legsMonster = legsGO.GetComponent<Monster>();
                legsMonster.monsterId = info.legsInfo.monsterId;
                legsMonster.isCombinedLegs = true;
                legsMonster.canMove = true;  // Legs должны двигаться
                legsMonster.canAttack = true;  // Legs должны атаковать
                if (legsMonster._agent != null) legsMonster._agent.enabled = true; // ��� AI
                
                NetworkServer.Spawn(legsGO);
                
                // Устанавливаем связи
                isCombinedHead = true;
                partnerNetId = legsGO.GetComponent<NetworkIdentity>().netId;
                legsMonster.partnerNetId = netIdentity.netId;
                // Legs spawned for combined monster
            }
            transform.position += Vector3.up * 15f; // Head �� 15
            if (_agent != null) _agent.baseOffset = 15f; // ������� �����
        }
        // Legs инициализируются автоматически через isCombinedLegs
    }

    private void LoadAndInitializeClient()
    {
        info = LoadMonsterInfo();
        if (info == null)
        {
            Debug.LogError($"[Monster] MonsterInfo not loaded for ID {monsterId}!");
            return;
        }
        if (info.modelPrefab != null)
        {
            GameObject model = Instantiate(info.modelPrefab, transform.position, transform.rotation, transform);
            if (info.isCombined)
            {
                model.transform.localPosition = Vector3.down * 15f;
            }
            else
            {
                model.transform.localPosition = Vector3.zero;
            }
            // Добавляем tag для легкого поиска
            model.tag = "MonsterModel";
            // Model prefab instantiated - ищем рендереры и анимационные компоненты
            _skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skinnedRenderer == null)
            {
                _meshRenderer = model.GetComponentInChildren<MeshRenderer>();
                if (_meshRenderer == null)
                {
                    Debug.LogWarning($"[Monster] Neither SkinnedMeshRenderer nor MeshRenderer found after instantiating model on {monsterName}");
                }
                else
                {
                    Debug.Log($"[Monster] Found MeshRenderer on instantiated model {monsterName} (non-humanoid)");
                    
                    // Для не-гуманоидов ищем Animation компонент
                    if (!isCombinedLegs)
                    {
                        _animation = model.GetComponent<Animation>();
                        if (_animation == null)
                        {
                            _animation = model.GetComponentInChildren<Animation>();
                        }
                        if (_animation == null)
                        {
                            Debug.LogWarning($"[Monster] Animation component not found after instantiating non-humanoid model on {monsterName}");
                        }
                        else
                        {
                            Debug.Log($"[Monster] Found Animation component on {monsterName}");
                            InitializeAnimationCache();
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"[Monster] Found SkinnedMeshRenderer on instantiated model {monsterName} (humanoid)");
                
                // Получаем Animator для управления анимациями гуманоидов
                // Для legs монстра не ищем аниматор - он будет использовать аниматор head
                if (!isCombinedLegs)
                {
                    _animator = model.GetComponent<Animator>();
                    if (_animator == null)
                    {
                        _animator = model.GetComponentInChildren<Animator>();
                    }
                    if (_animator == null)
                    {
                        Debug.LogWarning($"[Monster] Animator not found after instantiating humanoid model on {monsterName}");
                    }
                    else
                    {
                        Debug.Log($"[Monster] Found Animator on {monsterName}");
                        InitializeAnimationCache();
                    }
                }
                else
                {
                    Debug.Log($"[Monster] Legs monster {monsterName} - animator will be shared with head");
                }
            }
        }
        else
        {
            Debug.LogWarning("[Monster] modelPrefab not assigned in MonsterInfo!");
        }
    }

    private MonsterInfo LoadMonsterInfo()
    {
        MonsterDatabase db = Resources.Load<MonsterDatabase>("MonsterData/MonsterDatabase");
        if (db != null && monsterId - 1 >= 0 && monsterId - 1 < db.monsters.Count)
        {
            return db.monsters[monsterId - 1];
        }
        return null;
    }

    private void OnMonsterIdChanged(int oldId, int newId)
    {
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _health.OnHealthUpdated -= OnHealthUpdatedHandler;
        if (_monsterUI != null) Destroy(_monsterUI.gameObject);
    }

    private void OnNameChanged(string _, string newName)
    {
        if (_monsterUI != null)
        {
            _monsterUI.SetData(newName, _health.CurrentHealth, _health.MaxHealth);
        }
    }

    private void OnStunStateChanged(bool _, bool newValue)
    {
        // Stun state changed
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = newValue;
        }
    }

    private void OnSilenceStateChanged(bool _, bool newValue)
    {
        // Silence state changed
    }

    private void OnHealthUpdatedHandler(int currentHP, int maxHP)
    {
        if (_monsterUI != null)
        {
            _monsterUI.SetData(monsterName, currentHP, maxHP);
            // UI updated with new HP
        }
        else
        {
            Debug.LogWarning($"[Monster] _monsterUI is null in OnHealthUpdatedHandler! This may happen on the server side at start.");
        }
        if (currentHP <= 0 && !IsDead && isServer)
        {
            Die();
        }
    }

    [ClientRpc]
    public void RpcUpdateMonsterUI(int currentHealth, int maxHealth)
    {
        if (_monsterUI != null)
        {
            _monsterUI.SetData(monsterName, currentHealth, maxHealth);
            // Monster UI updated
        }
    }

    [Server]
    public void UpdateAggro(uint attackerNetId, int damage)
    {
        // Находим игрока по netId
        if (NetworkServer.spawned.TryGetValue(attackerNetId, out NetworkIdentity attackerIdentity))
        {
            PlayerCore attackerPlayer = attackerIdentity.GetComponent<PlayerCore>();
            if (attackerPlayer != null)
            {
                // Используем новую систему аггро из MonsterAI2
                MonsterAI2 ai2 = GetComponent<MonsterAI2>();
                if (ai2 != null)
                {
                    ai2.AddAggro(attackerPlayer, damage);
                }
                
                // Сохраняем для совместимости со старой системой
                if (aggroTargetNetId == 0 || damage > 0)
                {
                    aggroTargetNetId = attackerNetId;
                }
            }
        }
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        if (_currentControlEffect != ControlEffectType.None && Time.time < _controlEffectEndTime && skillWeight <= _currentEffectWeight)
        {
            // Control effect blocked by higher weight
            return;
        }
        if (_currentControlEffect != ControlEffectType.None)
        {
            ClearControlEffect();
        }
        _currentControlEffect = effectType;
        _currentEffectWeight = skillWeight;
        _controlEffectEndTime = Time.time + duration;
        if (effectType == ControlEffectType.Stun)
        {
            IsStunned = true;
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            // Stun effect applied
        }
        else if (effectType == ControlEffectType.Slow)
        {
            _slowPercentage = skillWeight / 100f;
            _originalSpeed = moveSpeed;
            if (_agent != null && _agent.isOnNavMesh)
            {
                float newSpeed = moveSpeed * (1f - _slowPercentage);
                _agent.speed = Mathf.Max(0.1f, newSpeed);
                // Slow effect applied
            }
            RpcApplySlowEffect(true);
        }
        else if (effectType == ControlEffectType.Silence)
        {
            IsSilenced = true;
            // Silence effect applied
        }
    }

    [Server]
    public void ApplySlow(float percentage, float duration, int skillWeight)
    {
        if (_currentControlEffect != ControlEffectType.None && Time.time < _controlEffectEndTime && skillWeight <= _currentEffectWeight)
        {
            // Slow effect blocked by higher weight
            return;
        }
        if (_currentControlEffect != ControlEffectType.None)
        {
            ClearControlEffect();
        }
        _currentControlEffect = ControlEffectType.Slow;
        _currentEffectWeight = skillWeight;
        _slowPercentage = percentage;
        _originalSpeed = moveSpeed;
        if (_agent != null && _agent.isOnNavMesh)
        {
            float newSpeed = moveSpeed * (1f - _slowPercentage);
            _agent.speed = Mathf.Max(0.1f, newSpeed);
            // Slow applied
        }
        _controlEffectEndTime = Time.time + duration;
        RpcApplySlowEffect(true);
    }

    [Server]
    private void ClearControlEffect()
    {
        if (_currentControlEffect == ControlEffectType.Stun)
        {
            IsStunned = false;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
            }
        }
        else if (_currentControlEffect == ControlEffectType.Slow && _originalSpeed > 0f)
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.speed = _originalSpeed;
            _slowPercentage = 0f;
            _originalSpeed = 0f;
            RpcApplySlowEffect(false);
        }
        else if (_currentControlEffect == ControlEffectType.Silence)
        {
            IsSilenced = false;
        }
        _currentControlEffect = ControlEffectType.None;
        _currentEffectWeight = 0;
        _controlEffectEndTime = 0f;
        // Control effect cleared
    }

    [ClientRpc]
    private void RpcApplySlowEffect(bool isActive)
    {
        if (isActive)
        {
            if (slowEffectPrefab != null && _slowEffectInstance == null)
            {
                _slowEffectInstance = Instantiate(slowEffectPrefab, transform);
                // Slow effect particles spawned
            }
            else
            {
                Renderer renderer = GetRenderer();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.5f, 0.5f, 1f, 1f);
                    // Slow visual effect applied
                }
            }
        }
        else
        {
            if (_slowEffectInstance != null)
            {
                Destroy(_slowEffectInstance);
                _slowEffectInstance = null;
                // Slow effect particles removed
            }
            Renderer renderer = GetRenderer();
            if (renderer != null)
            {
                renderer.material.color = Color.white;
                // Slow visual effect removed
            }
        }
    }

    [Server]
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        // Monster died
        if (aggroTargetNetId != 0 && NetworkServer.spawned.TryGetValue(aggroTargetNetId, out var identity))
        {
            PlayerCore killer = identity.GetComponent<PlayerCore>();
            if (killer != null && killer.Stats != null)
            {
                killer.Stats.AddExperience(experienceReward);
                // XP given to killer
            }
            else
            {
                Debug.LogWarning($"[Monster] Killer null: identity={identity?.gameObject?.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[Monster] No aggroTarget or not spawned: {aggroTargetNetId}");
        }
        // Уведомляем всех клиентов о смерти монстра
        RpcOnMonsterDeath();
        
        // Упрощенная логика смерти для combined монстров
        Debug.Log($"[Monster] Die() called for {name}, isCombinedHead: {isCombinedHead}, isCombinedLegs: {isCombinedLegs}, partnerNetId: {partnerNetId}");
        
        // Если это голова combined монстра - убиваем ноги
        if (isCombinedHead && partnerMonster != null)
        {
            Debug.Log($"[Monster] Head died, killing legs: {partnerMonster.name}");
            partnerMonster.Die();
        }
        
        // Если это ноги combined монстра - заставляем голову упасть
        if (isCombinedLegs && partnerMonster != null)
        {
            Debug.Log($"[Monster] Legs died, making head fall instantly: {partnerMonster.name}");
            partnerMonster.FallInstantly();
            // Уведомляем всех клиентов о смерти legs для синхронизации
            RpcOnLegsDeath(partnerMonster.netIdentity.netId);
        }
        
        // Уничтожаем монстра сразу после смерти
        StartCoroutine(DespawnAfterDelay(0.1f));
        
        // Обычные предметы из дроп-таблицы
        foreach (var entry in dropTable)
        {
            if (entry.item != null && Random.value <= entry.dropChance)
            {
                // Проверяем, использует ли предмет динамические статы
                if (entry.item.useDynamicStats)
                {
                    Item generatedItem = entry.item.GenerateDynamicItem();
                    if (generatedItem != null)
                    {
                        // Создаем ItemInfo с динамическими статами
                        ItemInfo dynamicItemInfo = new ItemInfo
                        {
                            id = entry.item.id,
                            quantity = 1,
                            hasDynamicStats = true,
                            dynamicItemName = generatedItem.itemName,
                            strengthBonus = generatedItem.strengthBonus,
                            agilityBonus = generatedItem.agilityBonus,
                            spiritBonus = generatedItem.spiritBonus,
                            constitutionBonus = generatedItem.constitutionBonus,
                            accuracyBonus = generatedItem.accuracyBonus,
                            minAttackConstantBonus = generatedItem.minAttackConstantBonus,
                            maxAttackConstantBonus = generatedItem.maxAttackConstantBonus,
                            maxHpConstantBonus = generatedItem.maxHpConstantBonus,
                            maxSpConstantBonus = generatedItem.maxSpConstantBonus,
                            crtConstantBonus = generatedItem.crtConstantBonus,
                            mspdConstantBonus = generatedItem.mspdConstantBonus,
                            physicalResist = generatedItem.physicalResist,
                            dynamicRarity = generatedItem.rarity
                        };
                        
                        // Создаем дропнутый предмет с динамическими статами
                        SpawnDroppedItemWithDynamicStats(dynamicItemInfo);
                        Debug.Log($"[Monster] Dynamic item dropped: {generatedItem.itemName} (ID: {entry.item.id}, Stats: Str+{generatedItem.strengthBonus}, Agi+{generatedItem.agilityBonus})");
                    }
                }
                else
                {
                    SpawnDroppedItem(entry.item.id, 1);
                    // Item dropped
                }
            }
        }
        
        // Сгенерированные предметы
        if (info.useGeneratedItems && info.itemGenerator != null)
        {
            foreach (var entry in info.generatedDropTable)
            {
                if (Random.value <= entry.dropChance)
                {
                    // Используем новый метод для генерации динамических предметов
                    Item generatedItem = info.itemGenerator.GenerateDynamicItemForDrop(entry.level);
                    if (generatedItem != null)
                    {
                        // Создаем ItemInfo с динамическими статами
                        ItemInfo dynamicItemInfo = new ItemInfo
                        {
                            id = generatedItem.id,
                            quantity = 1,
                            hasDynamicStats = true,
                            dynamicItemName = generatedItem.itemName,
                            strengthBonus = generatedItem.strengthBonus,
                            agilityBonus = generatedItem.agilityBonus,
                            spiritBonus = generatedItem.spiritBonus,
                            constitutionBonus = generatedItem.constitutionBonus,
                            accuracyBonus = generatedItem.accuracyBonus,
                            minAttackConstantBonus = generatedItem.minAttackConstantBonus,
                            maxAttackConstantBonus = generatedItem.maxAttackConstantBonus,
                            maxHpConstantBonus = generatedItem.maxHpConstantBonus,
                            maxSpConstantBonus = generatedItem.maxSpConstantBonus,
                            crtConstantBonus = generatedItem.crtConstantBonus,
                            mspdConstantBonus = generatedItem.mspdConstantBonus,
                            physicalResist = generatedItem.physicalResist,
                            dynamicRarity = generatedItem.rarity
                        };
                        
                        // Создаем дропнутый предмет с динамическими статами
                        SpawnDroppedItemWithDynamicStats(dynamicItemInfo);
                        Debug.Log($"[Monster] Generated dynamic item dropped: {generatedItem.itemName} (Level {entry.level}, ID: {generatedItem.id})");
                    }
                }
            }
        }
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }
        Rigidbody physicsRigidbody = (physicsModel != null ? physicsModel : gameObject).GetComponent<Rigidbody>();
        if (physicsRigidbody != null)
        {
            physicsRigidbody.isKinematic = false;
            Vector3 randomForce = new Vector3(
                Random.Range(minForce.x, maxForce.x),
                Random.Range(minForce.y, maxForce.y),
                Random.Range(minForce.z, maxForce.z)
            );
            physicsRigidbody.AddForce(randomForce, ForceMode.Impulse);
            // Random force applied
        }
        else
        {
            Debug.LogWarning($"[Monster] No Rigidbody found on {(physicsModel != null ? physicsModel.name : "default GameObject")} for {monsterName}");
        }
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            // Set to Ignore Raycast layer
        }
        canMove = false;
        canAttack = false;
        RpcDie();
        RpcHideMonsterUI();
    }

    [Server]
    private void SpawnDroppedItem(int itemID, int quantity)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogError("[Monster] DroppedItemPrefab not set!");
            return;
        }
        GameObject droppedItem = Instantiate(droppedItemPrefab, transform.position + Random.insideUnitSphere * 1f + Vector3.up * 0.5f, Quaternion.identity);
        DroppedItem droppedScript = droppedItem.GetComponent<DroppedItem>();
        if (droppedScript != null)
        {
            droppedScript.itemID = itemID;
            droppedScript.quantity = quantity;
            droppedScript.ownerNetId = aggroTargetNetId;
            droppedScript.dropTime = Time.time;
        }
        NetworkServer.Spawn(droppedItem);
        // Dropped item spawned
    }
    
    [Server]
    private void SpawnDroppedItemWithDynamicStats(ItemInfo dynamicItemInfo)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogError("[Monster] DroppedItemPrefab not set!");
            return;
        }
        GameObject droppedItem = Instantiate(droppedItemPrefab, transform.position + Random.insideUnitSphere * 1f + Vector3.up * 0.5f, Quaternion.identity);
        DroppedItem droppedScript = droppedItem.GetComponent<DroppedItem>();
        if (droppedScript != null)
        {
            droppedScript.InitializeWithDynamicItemInfo(dynamicItemInfo);
            droppedScript.ownerNetId = aggroTargetNetId;
            droppedScript.dropTime = Time.time;
        }
        NetworkServer.Spawn(droppedItem);
        // Dynamic dropped item spawned
    }

    [ClientRpc]
    private void RpcDie()
    {
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1f);
        }
        Rigidbody physicsRigidbody = (physicsModel != null ? physicsModel : gameObject).GetComponent<Rigidbody>();
        if (physicsRigidbody != null)
        {
            physicsRigidbody.isKinematic = false;
            Vector3 randomForce = new Vector3(
                Random.Range(minForce.x, maxForce.x),
                Random.Range(minForce.y, maxForce.y),
                Random.Range(minForce.z, maxForce.z)
            );
            physicsRigidbody.AddForce(randomForce, ForceMode.Impulse);
        }
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    [ClientRpc]
    private void RpcHideMonsterUI()
    {
        if (_monsterUI != null)
        {
            _monsterUI.gameObject.SetActive(false);
            // Monster UI hidden
        }
    }
    
    [ClientRpc]
    private void RpcOnMonsterDeath()
    {
        // Уведомляем клиентов о смерти этого монстра
        IsDead = true;
        
        // Отключаем коллайдер на клиенте
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }
    
    [ClientRpc]
    private void RpcOnLegsDeath(uint headNetId)
    {
        Debug.Log($"[Monster] RpcOnLegsDeath received for headNetId: {headNetId} - OBSOLETE METHOD");
        // Этот RPC больше не нужен - FallInstantly уже управляет всем
    }

    // FallOnClient() удален - теперь используется FallInstantly()

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
            // Monster destroyed
        }
    }

    private void OnDestroy()
    {
        if (_monsterUI != null) Destroy(_monsterUI.gameObject);
    }

    [Server]
    public void ExecuteAttack(uint targetNetId, string skillName, int damage, bool isCritical)
    {
        Debug.Log($"[Monster] ExecuteAttack called on {name}: canAttack={canAttack}, IsDead={IsDead}, IsStunned={IsStunned}, IsSilenced={IsSilenced}");
        if (!canAttack || IsDead || IsStunned || IsSilenced) 
        {
            Debug.LogWarning($"[Monster] Attack blocked on {name}: canAttack={canAttack}, IsDead={IsDead}, IsStunned={IsStunned}, IsSilenced={IsSilenced}");
            return;
        }
        
        // Проигрываем анимацию атаки для combined монстров
        if (isCombinedHead)
        {
            Debug.Log($"[Monster] Head attacking: {name}");
            
            // ДИАГНОСТИКА: проверяем состояние
            bool headIsAlive = !IsDead;
            bool partnerIsDead = partnerMonster == null || partnerMonster.IsDead;
            bool headShouldBeFallen = headIsAlive && partnerIsDead; // Голова жива НО legs мертвы = упала
            
            Debug.Log($"[Monster] HeadAttackDebug - HeadAlive: {headIsAlive}, PartnerDead: {partnerIsDead}, shouldBeFallen: {headShouldBeFallen}");
            
            // Проверяем: голова упала (legs мертв и голова жива) ИЛИ головы просто стоит
            if (headShouldBeFallen)
            {
                // Голова упала - играем анимацию лежа
                Debug.Log($"[Monster] Playing HeadPunchLying animation for FALLEN head");
                PlayAnimation("HeadPunchLying");
                RpcPlayAnimation("HeadPunchLying");
            }
            else
            {
                // Голова стоит - играем обычную анимацию
                Debug.Log($"[Monster] Playing HeadPunch animation for STANDING head");
                PlayAnimation("HeadPunch");
                RpcPlayAnimation("HeadPunch");
            }
        }
        else if (isCombinedLegs)
        {
            Debug.Log($"[Monster] Legs attacking: {name}");
            PlayAnimation("LegsKick");
            RpcPlayAnimation("LegsKick");
        }
        
        GameObject targetObject = NetworkServer.spawned.ContainsKey(targetNetId) ? NetworkServer.spawned[targetNetId].gameObject : null;
        if (targetObject == null)
        {
            Debug.LogWarning($"[Monster] Target with netId {targetNetId} not found for attack");
            return;
        }
        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            Debug.Log($"[Monster] {name} attacking {targetObject.name} for {damage} damage");
            targetHealth.TakeDamage(damage, DamageType.Physical, isCritical, netIdentity, 1f, true);
            // Attack executed
            Vector3 startPosition = transform.position + Vector3.up * 1f;
            Vector3 endPosition = targetObject.transform.position + Vector3.up * 1f;
            RpcPlayVFX(startPosition, transform.rotation, endPosition, isCritical);
        }
        else
        {
            Debug.LogWarning($"[Monster] Target {targetObject.name} has no Health component");
        }
        IsCooldown = true;
        StartCoroutine(EndCooldown(attackCooldown));
    }

    [ClientRpc]
    private void RpcPlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical)
    {
        if (basicAttackSkill != null)
        {
            basicAttackSkill.PlayVFX(startPosition, startRotation, endPosition, isCritical, this);
        }
    }

    private IEnumerator EndCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        IsCooldown = false;
        if (_agent != null && _agent.isOnNavMesh && !IsStunned)
        {
            _agent.isStopped = false;
        }
    }

    [Server]
    public void ReceiveControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        ApplyControlEffect(effectType, duration, skillWeight);
    }

    [Server]
    public bool TryUseSkill(PlayerCore target = null)
    {
        if (_skillExecutor != null)
        {
            return _skillExecutor.TryUseSkill(target);
        }
        return false;
    }

    private IEnumerator CheckControlEffectExpiration()
    {
        while (true)
        {
            if (isServer && _currentControlEffect != ControlEffectType.None && Time.time >= _controlEffectEndTime)
            {
                ClearControlEffect();
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    [Server]
    public void FallInstantly()
    {
        Debug.Log($"[Monster] FallInstantly called for {name}");
        
        canMove = false;
        isFalling = true;  // Устанавливаем флаг падения
        
        // ПРИНУДИТЕЛЬНО отключаем и перемещаем NavMeshAgent
        if (_agent != null) 
        {
            Debug.Log($"[Monster] NavMeshAgent before fall - enabled: {_agent.enabled}, baseOffset: {_agent.baseOffset}, pos: {_agent.transform.position}");
            _agent.baseOffset = 0f; // Сброс ПЕРЕД отключением
            _agent.enabled = false; // Отключаем СНАЧАЛА
            Debug.Log($"[Monster] NavMeshAgent disabled");
        }
        
        // Устанавливаем позицию на сервере - ПРИНУДИТЕЛЬНО!
        Vector3 groundPos = new Vector3(transform.position.x, 1f, transform.position.z);
        Debug.Log($"[Monster] BEFORE position change: {transform.position}");
        transform.position = groundPos;
        Debug.Log($"[Monster] AFTER position change: {transform.position}");
        
        // Принудительно синхронизируем через NetworkTransform - проверяем компонент
        NetworkTransformHybrid networkTransform = GetComponent<NetworkTransformHybrid>();
        if (networkTransform != null)
        {
            Debug.Log($"[Monster] NetworkTransformHybrid found, forcing position sync");
            // Устанавливаем позицию напрямую и принуждаем NetworkTransform к синхронизации
            networkTransform.transform.position = groundPos;
            transform.position = groundPos;
        }
        else
        {
            Debug.LogError($"[Monster] NetworkTransformHybrid not found on {name}!");
        }
        
        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА - если позиция не установилась
        if (Vector3.Distance(transform.position, groundPos) > 0.1f)
        {
            Debug.LogError($"[Monster] POSITION NOT SET! Trying to force: {groundPos}, actual: {transform.position}");
            transform.position = groundPos; // Повторная попытка
        }
        
        // Обновляем позицию модели для combined монстра
        if (info.isCombined)
        {
            Renderer renderer = GetRenderer();
            if (renderer != null)
            {
                renderer.transform.localPosition = Vector3.zero;
            }
        }
        
        // Проигрываем анимацию лежания для упавшей головы
        if (isCombinedHead)
        {
            Debug.Log($"[Monster] Playing LyingIdle animation for fallen head: {name}");
            PlayAnimation("LyingIdle");
            RpcPlayAnimation("LyingIdle");
            
            // Принудительно опускаем модель на землю
            GameObject[] modelObjects = GameObject.FindGameObjectsWithTag("MonsterModel");
            foreach (GameObject modelObject in modelObjects)
            {
                if (modelObject.transform.parent == transform)
                {
                    modelObject.transform.localPosition = new Vector3(2.4f, 0f, 4.6f);
                    Debug.Log($"[Monster] Model positioned at localPos {modelObject.transform.localPosition}");
                    break;
                }
            }
        }
        
        // Уведомляем клиентов о мгновенном падении
        RpcFallInstantly(groundPos);
    }
    
    [ClientRpc]
    private void RpcFallInstantly(Vector3 groundPos)
    {
        Debug.Log($"[Monster] RpcFallInstantly called for {name} at position {groundPos}");
        Debug.Log($"[Monster] Client BEFORE position change: {transform.position}");
        
        // ПРИНУДИТЕЛЬНО отключаем NavMeshAgent на клиенте
        if (_agent != null)
        {
            Debug.Log($"[Monster] Client NavMeshAgent before - enabled: {_agent.enabled}, baseOffset: {_agent.baseOffset}");
            _agent.baseOffset = 0f; // Сброс ПЕРЕД отключением
            _agent.enabled = false; // Отключаем
        }
        
        // Дополнительно синхронизируем через NetworkTransformHybrid на клиенте
        NetworkTransformHybrid networkTransform = GetComponent<NetworkTransformHybrid>();
        if (networkTransform != null)
        {
            Debug.Log($"[Monster] Client NetworkTransformHybrid found, forcing position");
            // Принудительно обновляем трансформ и принуждаем к мгновенной синхронизации
            networkTransform.transform.position = groundPos;
            transform.position = groundPos;
        }
        
        // Синхронизируем позицию с сервера - ПРИНУДИТЕЛЬНО!
        transform.position = groundPos;
        Debug.Log($"[Monster] Client AFTER position change: {transform.position}");
        
        // ПРОВЕРКА - если позиция не установилась на клиенте
        if (Vector3.Distance(transform.position, groundPos) > 0.1f)
        {
            Debug.LogError($"[Monster] CLIENT POSITION NOT SET! Expected: {groundPos}, Actual: {transform.position}");
            transform.position = groundPos; // Повторная попытка
        }
        
        // Сбрасываем позицию модели для combined монстра на клиенте
        if (info.isCombined)
        {
            Renderer renderer = GetRenderer();
            if (renderer != null)
            {
                renderer.transform.localPosition = Vector3.zero;
            }
        }
        
        // Принудительно опускаем модель на землю на клиенте
        if (isCombinedHead)
        {
            PlayAnimation("LyingIdle");
            
            GameObject[] modelObjects = GameObject.FindGameObjectsWithTag("MonsterModel");
            foreach (GameObject modelObject in modelObjects)
            {
                if (modelObject.transform.parent == transform)
                {
                    modelObject.transform.localPosition = new Vector3(2.4f, 0f, 4.6f);
                    Debug.Log($"[Monster] Client model positioned at localPos {modelObject.transform.localPosition}");
                    break;
                }
            }
        }
        
        
        // Финальная проверка позиции
        if (Vector3.Distance(transform.position, groundPos) > 0.1f)
        {
            Debug.LogWarning($"[Monster] FINAL CHECK: Position correction needed: {groundPos} vs {transform.position}");
            transform.position = groundPos;
        }
        
        isFalling = false;  // Сбрасываем флаг падения сразу
        
        Debug.Log($"[Monster] FallInstantly completed: {name} at position {transform.position} - OBJECT FREEZE");
    }
    
    // Старый метод Fall (оставляем для совместимости, но не используем)
    [Server]
    public void Fall()
    {
        Debug.LogWarning($"[Monster] Old Fall() method called - should use FallInstantly() instead");
        FallInstantly(); // Перенаправляем на новый метод
    }
    
    // Старый RpcFall() удален - используем RpcFallInstantly()
    
    // FallCoroutine() удален - теперь используется мгновенное падение FallInstantly()

    // private void OnMonsterIdChanged(int oldId, int newId)
    // {
    // }
    
    private Transform FindHeadTransform(Transform modelTransform)
    {
        // Ищем по имени "Head" или содержащему "head"
        Transform head = modelTransform.Find("Head");
        if (head == null)
        {
            // Ищем среди всех дочерних объектов
            foreach (Transform child in modelTransform)
            {
                if (child.name.ToLower().Contains("head"))
                {
                    head = child;
                    break;
                }
            }
        }
        return head;
    }
    
    private Transform FindBoneByName(Transform modelTransform, string boneName)
    {
        // Рекурсивный поиск кости по имени
        if (modelTransform.name == boneName)
        {
            return modelTransform;
        }
        
        foreach (Transform child in modelTransform)
        {
            Transform found = FindBoneByName(child, boneName);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
    
    // Debug Gizmos для визуализации радиусов
    private void OnDrawGizmosSelected()
    {
        if (info == null) return;
        
        // Цвета для разных радиусов
        Gizmos.color = Color.red;
        // Используем радиус атаки из basicAttackSkill
        float attackRange = info.basicAttackSkill != null ? info.basicAttackSkill.Range : 2f;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, info.detectionRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, info.patrolRadius);
        
        // Для combined монстров показываем дополнительные радиусы
        if (info.isCombined)
        {
            // Радиус атаки головы (на высоте головы или на земле если упала)
            Vector3 headPos;
            if (isCombinedHead && IsDead)
            {
                // Голова упала - показываем радиус на земле
                headPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            }
            else if (isCombinedHead)
            {
                // Голова на высоте - показываем радиус на высоте головы (transform.position уже на высоте 15f)
                headPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            }
            else
            {
                // Это legs объект - показываем радиус головы на высоте головы
                headPos = new Vector3(transform.position.x, transform.position.y + 15f, transform.position.z);
            }
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(headPos, info.headAttackRange);
            
            // Радиус атаки ног (на высоте ног)
            Vector3 legsPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(legsPos, info.legsAttackRange);
            
            // Линия между головой и ногами
            Gizmos.color = Color.white;
            Gizmos.DrawLine(headPos, legsPos);
        }
        
        // Показываем направление взгляда
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}