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

[System.Serializable]
public struct AggroEntry
{
    public uint playerNetId;
    public string playerName;
    public int damageDealt;
    public float damagePercentage;
    public float timestamp;
    
    public AggroEntry(uint netId, string name, int damage, float percentage, float time)
    {
        playerNetId = netId;
        playerName = name;
        damageDealt = damage;
        damagePercentage = percentage;
        timestamp = time;
    }
}

public class Monster : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnMonsterIdChanged))] public int monsterId;
    [SyncVar] public bool isElite = false;
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
                partnerMonster.RpcPlayAnimation(animationName);
                // Играем локально для комбинированного legs монстра тоже
                PlayAnimationLocal(animationName);
            }
            else
            {
                PlayAnimationLocal(animationName);
                
                // Отправляем RPC всем клиентам для синхронизации анимации
                if (NetworkServer.connections.Count > 0)
                {
                    RpcPlayAnimation(animationName);
                }
            }
        }
        else if (NetworkClient.active)
        {
            // Клиент: играем анимацию локально
            PlayAnimationLocal(animationName);
        }
    }
    
    /// <summary>
    /// Локальное воспроизведение анимации с поддержкой обеих систем
    /// </summary>
    private void PlayAnimationLocal(string animationName)
    {
        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: Если анимационные компоненты не инициализированы, пытаемся их инициализировать
        if (_animator == null && _animation == null)
        {
            EnsureAnimationComponentsInitialized();
        }
        
        // Проверяем, есть ли анимационная система
        if (!IsHumanoidMonster() && !IsNonHumanoidMonster())
        {
            // У монстра нет анимационной системы - это нормально для некоторых монстров
            return;
        }
        
        // Проверяем, есть ли анимация с таким именем
        if (!HasAnimation(animationName))
        {
            // Анимация не найдена - это нормально, не все монстры имеют все анимации
            return;
        }
        
        if (IsHumanoidMonster())
        {
            // Гуманоидный монстр: используем Animator
            if (_animator != null)
            {
                _animator.Play(animationName);
            }
        }
        else if (IsNonHumanoidMonster())
        {
            // Не-гуманоидный монстр: используем Animation
            if (_animation != null)
            {
                _animation.Play(animationName);
            }
        }
    }
    
    [ClientRpc]
    public void RpcPlayAnimation(string animationName)
    {
        // КРИТИЧНО: Принудительно инициализируем анимационные компоненты на клиенте
        // если они еще не были инициализированы
        EnsureAnimationComponentsInitialized();
        
        if (isCombinedLegs && partnerMonster != null)
        {
            // Legs управляет аниматором head НО для атаки используем правильную анимацию
            if (animationName == "LegsKick")
            {
                // Для legs атаки играем Kick на head аниматоре
                partnerMonster.PlayAnimationLocal("LegsKick");
            }
            else
            {
                // Для остальных анимаций передаем управление head
                partnerMonster.RpcPlayAnimation(animationName);
            }
        }
        else
        {
            // Head или обычный монстр управляет своей анимационной системой
            PlayAnimationLocal(animationName);
        }
    }
    
    [ClientRpc]
    public void RpcPlayAnimationById(int animationId)
    {
        // КРИТИЧНО: Принудительно инициализируем анимационные компоненты на клиенте
        EnsureAnimationComponentsInitialized();
        
        if (isCombinedLegs && partnerMonster != null)
        {
            // Для combined монстров передаем управление head
            partnerMonster.RpcPlayAnimationById(animationId);
        }
        else
        {
            // Head или обычный монстр управляет своей анимационной системой
            // Debug.Log($"[Monster] RPC: {name} playing animation by ID {animationId}");
            PlayAnimationByIdLocal(animationId);
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
                // RPC автоматически отправляется через PlayAnimation()
            }
            else
            {
                // Голова стоит - играем обычную анимацию
                Debug.Log($"[Monster] ExecuteAttack(PlayerCore) Playing HeadPunch for STANDING head");
                PlayAnimation("HeadPunch");
                // RPC автоматически отправляется через PlayAnimation()
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
            // RPC автоматически отправляется через PlayAnimation()
            
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
            // Обычная атака - воспроизводим анимацию атаки
            Debug.Log($"[Monster] Regular monster attacking: {name}");
            PlayAnimation("Attack"); // Стандартная анимация атаки для обычных монстров
            basicAttackSkill.Execute(this, null, target.gameObject);
        }
    }
    private bool canAttack = true;
    private GameObject slowEffectPrefab;
    [Header("Aggro & Experience")]
    [SyncVar] public uint aggroTargetNetId = 0;
    
    [Header("Death Settings")]
    [SerializeField] private float corpseVisibilityTime = 10f; // Время видимости трупа
    [SerializeField] private float corpseFadeStartTime = 8f; // Когда начинается исчезновение
    [SerializeField] private float corpseFadeDuration = 2f; // Длительность исчезновения
    private uint lastAttackerNetId = 0; // Последний игрок, который атаковал монстра
    
    // Система распределения опыта по урону
    private System.Collections.Generic.Dictionary<uint, int> damageDealers = new System.Collections.Generic.Dictionary<uint, int>();
    private int totalDamageDealt = 0;
    
    // Список последних атакующих (аггро список)
    private System.Collections.Generic.List<AggroEntry> aggroList = new System.Collections.Generic.List<AggroEntry>();
    private const int MAX_AGGRO_ENTRIES = 5;
    private int experienceReward;
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
    private MonsterHitEffects _hitEffects; // DoTween эффекты для не-гуманоидов
    
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
        // Если анимационные компоненты не инициализированы, пытаемся их инициализировать
        if (_animator == null && _animation == null)
        {
            EnsureAnimationComponentsInitialized();
        }
        
        if (IsHumanoidMonster() && _animator != null)
        {
            // Для Animator проверяем через HasState (требует hash)
            return _animator.HasState(0, Animator.StringToHash(animationName));
        }
        else if (IsNonHumanoidMonster() && _animation != null)
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
                
                // Debug.Log($"[Monster] Initialized animation cache for humanoid {monsterName}: {clips.Length} animations");
            }
            else
            {
                // Debug.LogWarning($"[Monster] RuntimeAnimatorController is null for humanoid {monsterName}");
            }
        }
        else if (IsNonHumanoidMonster())
        {
            // Для Animation получаем список клипов
            int count = _animation.GetClipCount();
            // Debug.Log($"[Monster] Animation component has {count} clips for {monsterName}");
            
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
                        // Debug.LogWarning($"[Monster] Found animation with empty name: {index} for {monsterName}");
                        animName = $"Animation_{index}"; // Fallback имя
                    }
                    
                    _animationNames[index] = animName;
                    _animationIds[animName] = index;
                    // Debug.Log($"[Monster] Cached animation {index}: '{animName}' for {monsterName}");
                    index++;
                }
                
                // Debug.Log($"[Monster] Initialized animation cache for non-humanoid {monsterName}: {count} animations");
            }
            else
            {
                // Debug.LogWarning($"[Monster] No animation clips found in Animation component for {monsterName}");
            }
        }
        else
        {
            // Debug.LogWarning($"[Monster] Cannot initialize animation cache - no valid animation system found for {monsterName}");
        }
        
        // Выводим список доступных анимаций с их ID
        if (_animationNames != null && _animationNames.Length > 0)
        {
            // Debug.Log($"[Monster] Available animations for {monsterName}: {string.Join(", ", _animationNames.Select((name, id) => $"{id}:{name}"))}");
        }
        else
        {
            // Debug.LogWarning($"[Monster] No animations available for {monsterName}");
        }
    }
    
    /// <summary>
    /// Воспроизводит анимацию по ID
    /// </summary>
    public void PlayAnimationById(int animationId)
    {
        if (_animationNames == null || animationId < 0 || animationId >= _animationNames.Length)
        {
            // Debug.LogWarning($"[Monster] Invalid animation ID {animationId} for {monsterName}. Available IDs: 0-{(_animationNames?.Length - 1 ?? -1)}");
            return;
        }
        
        string animationName = _animationNames[animationId];
        
        // Дополнительная защита от пустых имен
        if (string.IsNullOrEmpty(animationName))
        {
            // Debug.LogWarning($"[Monster] Animation at ID {animationId} has empty name for {monsterName}. Skipping playback.");
            return;
        }
        
        // Debug.Log($"[Monster] Playing animation by ID {animationId}: '{animationName}' on {monsterName}");
        PlayAnimation(animationName);
    }
    
    /// <summary>
    /// Локальное воспроизведение анимации по ID (без отправки RPC)
    /// </summary>
    private void PlayAnimationByIdLocal(int animationId)
    {
        if (_animationNames == null || animationId < 0 || animationId >= _animationNames.Length)
        {
            return;
        }
        
        string animationName = _animationNames[animationId];
        
        if (string.IsNullOrEmpty(animationName))
        {
            return;
        }
        
        PlayAnimationLocal(animationName);
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
    /// Проигрывает DoTween эффект получения удара для не-гуманоидных монстров
    /// </summary>
    public void PlayHitEffect(Vector3 hitDirection = default(Vector3))
    {
        if (IsNonHumanoidMonster() && _hitEffects != null)
        {
            if (NetworkServer.active)
            {
                // На сервере играем локально (для Host режима)
                _hitEffects.PlayHitEffect(hitDirection);
                
                // И отправляем RPC для всех клиентов
                if (NetworkServer.connections.Count > 0)
                {
                    RpcPlayHitEffect(hitDirection);
                }
            }
            else if (NetworkClient.active)
            {
                // На клиенте играем локально
                _hitEffects.PlayHitEffect(hitDirection);
            }
        }
        else if (IsNonHumanoidMonster() && _hitEffects == null)
        {
            Debug.LogWarning($"[Monster] DoTween effects not initialized for non-humanoid monster {monsterName}");
        }
        else if (IsHumanoidMonster())
        {
            Debug.Log($"[Monster] Hit effects not implemented for humanoid monster {monsterName} (uses Animator)");
        }
        else
        {
            Debug.LogWarning($"[Monster] Cannot play hit effect for {monsterName}: no hit effects component or invalid monster type");
        }
    }
    
    /// <summary>
    /// RPC для синхронизации эффектов удара по сети
    /// </summary>
    [ClientRpc]
    private void RpcPlayHitEffect(Vector3 hitDirection)
    {
        if (IsNonHumanoidMonster() && _hitEffects != null)
        {
            _hitEffects.PlayHitEffect(hitDirection);
        }
    }
    
    /// <summary>
    /// Проигрывает упрощенный эффект удара без направления
    /// </summary>
    public void PlaySimpleHitEffect()
    {
        PlayHitEffect(Vector3.zero);
    }
    
    /// <summary>
    /// Проигрывает анимацию смерти для не-гуманоидного монстра
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (IsNonHumanoidMonster() && _hitEffects != null)
        {
            if (NetworkServer.active)
            {
                // На сервере играем локально (для Host режима)
                _hitEffects.PlayDeathAnimation();
                
                // И отправляем RPC для всех клиентов
                if (NetworkServer.connections.Count > 0)
                {
                    RpcPlayDeathAnimation();
                }
            }
            else if (NetworkClient.active)
            {
                // На клиенте играем локально
                _hitEffects.PlayDeathAnimation();
            }
        }
        else if (IsHumanoidMonster())
        {
            Debug.Log($"[Monster] Death animation not implemented for humanoid monster {monsterName} (uses Animator)");
        }
        else
        {
            Debug.LogWarning($"[Monster] Cannot play death animation for {monsterName}: no hit effects component or invalid monster type");
        }
    }
    
    /// <summary>
    /// RPC для синхронизации анимации смерти по сети
    /// </summary>
    [ClientRpc]
    private void RpcPlayDeathAnimation()
    {
        if (IsNonHumanoidMonster() && _hitEffects != null)
        {
            _hitEffects.PlayDeathAnimation();
        }
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
    /// Инициализирует анимационные компоненты из модели (используется на сервере и клиенте)
    /// </summary>
    private void InitializeAnimationComponentsFromModel(GameObject model)
    {
        // Ищем рендереры и анимационные компоненты
        _skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
        if (_skinnedRenderer == null)
        {
            _meshRenderer = model.GetComponentInChildren<MeshRenderer>();
            if (_meshRenderer != null)
            {
                // Для не-гуманоидов ищем Animation компонент
                if (!isCombinedLegs)
                {
                    _animation = model.GetComponent<Animation>();
                    if (_animation == null)
                    {
                        _animation = model.GetComponentInChildren<Animation>();
                    }
                    if (_animation != null)
                    {
                        InitializeAnimationCache();
                        
                        // Инициализируем DoTween эффекты для не-гуманоидов
                        _hitEffects = model.GetComponentInChildren<MonsterHitEffects>();
                        if (_hitEffects == null)
                        {
                            Debug.LogWarning($"[Monster] No MonsterHitEffects component found for non-humanoid monster {monsterName}");
                        }
                    }
                }
            }
        }
        else
        {
            // Для гуманоидов ищем Animator компонент
            if (!isCombinedLegs)
            {
                _animator = model.GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = model.GetComponentInChildren<Animator>();
                }
                if (_animator != null)
                {
                    InitializeAnimationCache();
                }
            }
        }
    }
    
    /// <summary>
    /// Принудительно инициализирует анимационные компоненты на клиенте
    /// </summary>
    private void EnsureAnimationComponentsInitialized()
    {
        // Если анимационные компоненты уже инициализированы, ничего не делаем
        if (_animator != null || _animation != null)
        {
            return;
        }
        
        // Ищем модель монстра
        GameObject model = null;
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.CompareTag("MonsterModel"))
            {
                model = child.gameObject;
                break;
            }
        }
        
        if (model == null)
        {
            // Ищем любой дочерний объект с рендерером
            _skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skinnedRenderer == null)
            {
                _meshRenderer = GetComponentInChildren<MeshRenderer>();
                if (_meshRenderer != null)
                {
                    model = _meshRenderer.gameObject;
                }
            }
            else
            {
                model = _skinnedRenderer.gameObject;
            }
        }
        
        if (model != null)
        {
            // Ищем анимационные компоненты
            _skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skinnedRenderer == null)
            {
                _meshRenderer = model.GetComponentInChildren<MeshRenderer>();
                if (_meshRenderer != null)
                {
                    // Для не-гуманоидов ищем Animation компонент
                    if (!isCombinedLegs)
                    {
                        _animation = model.GetComponent<Animation>();
                        if (_animation == null)
                        {
                            _animation = model.GetComponentInChildren<Animation>();
                        }
                        if (_animation != null)
                        {
                            InitializeAnimationCache();
                            
                            // Инициализируем DoTween эффекты для не-гуманоидов
                            _hitEffects = model.GetComponentInChildren<MonsterHitEffects>();
                        }
                    }
                }
            }
            else
            {
                // Для гуманоидов ищем Animator компонент
                if (!isCombinedLegs)
                {
                    _animator = model.GetComponent<Animator>();
                    if (_animator == null)
                    {
                        _animator = model.GetComponentInChildren<Animator>();
                    }
                    if (_animator != null)
                    {
                        InitializeAnimationCache();
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Диагностический метод для проверки состояния анимационной системы
    /// </summary>
    [ContextMenu("Diagnose Animation System")]
    public void DiagnoseAnimationSystem()
    {
        Debug.Log($"=== Animation System Diagnosis for {monsterName} ===");
        Debug.Log($"IsHumanoidMonster: {IsHumanoidMonster()}");
        Debug.Log($"IsNonHumanoidMonster: {IsNonHumanoidMonster()}");
        Debug.Log($"_animator: {_animator != null}");
        Debug.Log($"_animation: {_animation != null}");
        Debug.Log($"_skinnedRenderer: {_skinnedRenderer != null}");
        Debug.Log($"_meshRenderer: {_meshRenderer != null}");
        
        if (_animator != null)
        {
            Debug.Log($"Animator Controller: {_animator.runtimeAnimatorController != null}");
            if (_animator.runtimeAnimatorController != null)
            {
                var clips = _animator.runtimeAnimatorController.animationClips;
                Debug.Log($"Animator Clips Count: {clips.Length}");
                for (int i = 0; i < clips.Length; i++)
                {
                    Debug.Log($"  Clip {i}: {clips[i].name}");
                }
            }
        }
        
        if (_animation != null)
        {
            Debug.Log($"Animation Clips Count: {_animation.GetClipCount()}");
            int index = 0;
            foreach (AnimationState state in _animation)
            {
                Debug.Log($"  Clip {index}: {state.name}");
                index++;
            }
        }
        
        Debug.Log($"NetworkServer.active: {NetworkServer.active}");
        Debug.Log($"NetworkClient.active: {NetworkClient.active}");
        Debug.Log($"isServer: {isServer}");
        Debug.Log($"isClient: {isClient}");
        Debug.Log($"isLocalPlayer: {isLocalPlayer}");
        Debug.Log($"NetworkServer.connections.Count: {NetworkServer.connections.Count}");
        Debug.Log("=== End Diagnosis ===");
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
                    
                    // Инициализируем DoTween эффекты для не-гуманоидов
                    _hitEffects = GetComponentInChildren<MonsterHitEffects>();
                    if (_hitEffects == null)
                    {
                        Debug.LogWarning($"[Monster] No MonsterHitEffects component found for non-humanoid monster {monsterName}. Consider adding it for hit effects.");
                    }
                    else
                    {
                        Debug.Log($"[Monster] MonsterHitEffects initialized for {monsterName}");
                    }
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
        
        // ДОПОЛНИТЕЛЬНО: Принудительно инициализируем анимационные компоненты
        // с небольшой задержкой, чтобы убедиться, что модель создана
        StartCoroutine(DelayedAnimationInitialization());
    }
    
    private System.Collections.IEnumerator DelayedAnimationInitialization()
    {
        // Ждем один кадр, чтобы убедиться, что LoadAndInitializeClient() завершился
        yield return null;
        
        // Проверяем, инициализированы ли анимационные компоненты
        if (_animator == null && _animation == null)
        {
            EnsureAnimationComponentsInitialized();
        }
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
        
        // Добавляем префикс [Elite] к имени
        if (isElite)
        {
            monsterName = "[Elite] " + monsterName;
        }
        
        // Применяем Elite модификаторы если монстр Elite
        if (isElite)
        {
            moveSpeed = info.moveSpeed * info.eliteStatMultiplier;
            attackCooldown = info.attackCooldown / info.eliteStatMultiplier; // Быстрее атакует
            experienceReward = Mathf.RoundToInt(info.experienceReward * info.eliteStatMultiplier);
            Debug.Log($"[Monster] Elite monster {monsterName} - stats multiplied by {info.eliteStatMultiplier}");
        }
        else
        {
            moveSpeed = info.moveSpeed;
            attackCooldown = info.attackCooldown;
            experienceReward = info.experienceReward;
        }
        
        deathVFXPrefab = info.deathVFXPrefab;
        canMove = info.canMove;
        canAttack = info.canAttack;
        slowEffectPrefab = info.slowEffectPrefab;
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
        
        // Применяем Elite модификаторы к здоровью и характеристикам
        if (isElite)
        {
            _health.MaxHealth = Mathf.RoundToInt(info.maxHealth * info.eliteStatMultiplier);
            _health.CurrentHealth = _health.MaxHealth;
            hitRate = Mathf.RoundToInt(info.hitRate * info.eliteStatMultiplier);
            dodge = Mathf.RoundToInt(info.dodge * info.eliteStatMultiplier);
        }
        else
        {
            _health.MaxHealth = info.maxHealth;
            _health.CurrentHealth = info.maxHealth;
            hitRate = info.hitRate;
            dodge = info.dodge;
        }

        // Инициализируем аггро систему для нового монстра
        damageDealers.Clear();
        totalDamageDealt = 0;
        aggroList.Clear();
        lastAttackerNetId = 0;
        aggroTargetNetId = 0;
        Debug.Log($"[Monster] Initialized aggro system for {monsterName}");

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

        // КРИТИЧНО: Создаем модель монстра на сервере для инициализации анимационных компонентов
        // Это необходимо для корректной работы анимаций в Server Only режиме
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
            
            // Масштабируем модель для Elite монстров
            if (isElite)
            {
                model.transform.localScale = Vector3.one * info.eliteModelScale;
                Debug.Log($"[Monster] Elite model scaled for {monsterName}: {info.eliteModelScale}");
            }
            
            // Масштабируем BoxCollider если нужно
            if (info.scaleBoxCollider)
            {
                BoxCollider boxCollider = GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Vector3 finalScale = info.boxColliderScale;
                    // Если монстр Elite, дополнительно масштабируем коллайдер
                    if (isElite)
                    {
                        finalScale *= info.eliteModelScale;
                    }
                    boxCollider.size = Vector3.Scale(boxCollider.size, finalScale);
                    Debug.Log($"[Monster] Scaled BoxCollider for {monsterName}: {finalScale}");
                }
                else
                {
                    Debug.LogWarning($"[Monster] BoxCollider not found on {monsterName} for scaling");
                }
            }
            else if (isElite)
            {
                // Если не масштабируем коллайдер специально, но монстр Elite - масштабируем по модели
                BoxCollider boxCollider = GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    boxCollider.size = Vector3.Scale(boxCollider.size, Vector3.one * info.eliteModelScale);
                    Debug.Log($"[Monster] Elite BoxCollider auto-scaled for {monsterName}: {info.eliteModelScale}");
                }
            }
            
            // Инициализируем анимационные компоненты на сервере
            InitializeAnimationComponentsFromModel(model);
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
            
            // Масштабируем модель для Elite монстров
            if (isElite)
            {
                model.transform.localScale = Vector3.one * info.eliteModelScale;
                Debug.Log($"[Monster] Elite model scaled for {monsterName} on client: {info.eliteModelScale}");
            }
            
            // Масштабируем BoxCollider если нужно
            if (info.scaleBoxCollider)
            {
                BoxCollider boxCollider = GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Vector3 finalScale = info.boxColliderScale;
                    // Если монстр Elite, дополнительно масштабируем коллайдер
                    if (isElite)
                    {
                        finalScale *= info.eliteModelScale;
                    }
                    boxCollider.size = Vector3.Scale(boxCollider.size, finalScale);
                    Debug.Log($"[Monster] Scaled BoxCollider for {monsterName} on client: {finalScale}");
                }
                else
                {
                    Debug.LogWarning($"[Monster] BoxCollider not found on {monsterName} for scaling on client");
                }
            }
            else if (isElite)
            {
                // Если не масштабируем коллайдер специально, но монстр Elite - масштабируем по модели
                BoxCollider boxCollider = GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    boxCollider.size = Vector3.Scale(boxCollider.size, Vector3.one * info.eliteModelScale);
                    Debug.Log($"[Monster] Elite BoxCollider auto-scaled for {monsterName} on client: {info.eliteModelScale}");
                }
            }
            
            // Инициализируем анимационные компоненты из модели
            InitializeAnimationComponentsFromModel(model);
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
        // Перезагружаем данные монстра когда ID изменился
        Debug.Log($"[Monster] Monster ID changed from {oldId} to {newId}. Reloading monster data.");
        if (isServer)
        {
            LoadAndInitializeServer();
        }
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
        Debug.Log($"[Monster] UpdateAggro called: attacker={attackerNetId}, damage={damage}");
        
        // Отслеживаем последнего атакующего независимо от аггро системы
        lastAttackerNetId = attackerNetId;
        
        // Отслеживаем урон для распределения опыта
        RecordDamage(attackerNetId, damage);
        
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
    private void RecordDamage(uint attackerNetId, int damage)
    {
        // Получаем имя игрока для логирования
        string playerName = "Unknown";
        if (NetworkServer.spawned.TryGetValue(attackerNetId, out var identity))
        {
            PlayerCore player = identity.GetComponent<PlayerCore>();
            if (player != null) playerName = player.name;
        }
        
        // Записываем урон от игрока для распределения опыта
        if (damageDealers.ContainsKey(attackerNetId))
        {
            damageDealers[attackerNetId] += damage;
        }
        else
        {
            damageDealers[attackerNetId] = damage;
        }
        
        totalDamageDealt += damage;
        
        // Обновляем аггро список
        UpdateAggroList(attackerNetId, playerName, damage);
        
        Debug.Log($"[Monster] Player {playerName} ({attackerNetId}) dealt {damage} damage. Total: {damageDealers[attackerNetId]}/{totalDamageDealt}");
    }
    
    [Server]
    private void UpdateAggroList(uint playerNetId, string playerName, int newDamage)
    {
        // Проверяем, есть ли уже игрок в аггро списке
        var existingEntry = aggroList.FirstOrDefault(entry => entry.playerNetId == playerNetId);
        
        if (existingEntry.playerNetId != 0)
        {
            // Обновляем существующую запись
            int index = aggroList.FindIndex(entry => entry.playerNetId == playerNetId);
            int totalDamage = damageDealers[playerNetId];
            float percentage = (float)totalDamage / totalDamageDealt;
            
            aggroList[index] = new AggroEntry(playerNetId, playerName, totalDamage, percentage, Time.time);
        }
        else
        {
            // Добавляем новую запись
            int totalDamage = damageDealers[playerNetId];
            float percentage = (float)totalDamage / totalDamageDealt;
            var newEntry = new AggroEntry(playerNetId, playerName, totalDamage, percentage, Time.time);
            aggroList.Add(newEntry);
            
            // Сортируем по урону (больший урон первым)
            aggroList = aggroList.OrderByDescending(entry => entry.damageDealt).ToList();
            
            // Ограничиваем количество записей
            if (aggroList.Count > MAX_AGGRO_ENTRIES)
            {
                aggroList.RemoveAt(aggroList.Count - 1);
            }
        }
        
        Debug.Log($"[Monster] Aggro list updated: {aggroList.Count} entries");
    }
    
    [Server]
    private void DistributeExperienceByDamage()
    {
        Debug.Log($"[Monster] Distributing experience to aggro list ({aggroList.Count} entries). Total damage: {totalDamageDealt}, Base XP: {experienceReward}");
        
        // Дополнительная диагностика
        Debug.Log($"[Monster] Experience distribution check:");
        Debug.Log($"[Monster] - aggroList.Count: {aggroList.Count}");
        Debug.Log($"[Monster] - damageDealers.Count: {damageDealers.Count}");
        Debug.Log($"[Monster] - totalDamageDealt: {totalDamageDealt}");
        Debug.Log($"[Monster] - experienceReward: {experienceReward}");
        Debug.Log($"[Monster] - lastAttackerNetId: {lastAttackerNetId}");
        Debug.Log($"[Monster] - aggroTargetNetId: {aggroTargetNetId}");
        
        if (aggroList.Count == 0)
        {
            Debug.LogWarning($"[Monster] No aggro entries found - no XP distributed");
            return;
        }
        
        // Распределяем опыт между игроками в аггро списке
        foreach (var aggroEntry in aggroList)
        {
            uint playerNetId = aggroEntry.playerNetId;
            
            // Вычисляем процент урона игрока
            float damagePercentage = aggroEntry.damagePercentage;
            int playerXP = Mathf.RoundToInt(experienceReward * damagePercentage);
            
            if (playerXP > 0)
            {
                PlayerCore player = null;
                
                // Пытаемся найти игрока
                if (NetworkServer.spawned.TryGetValue(playerNetId, out var identity))
                {
                    player = identity.GetComponent<PlayerCore>();
                }
                
                if (player != null && player.Stats != null)
                {
                    player.Stats.AddExperience(playerXP);
                    Debug.Log($"[Monster] XP distributed: {player.name} gets {playerXP} XP ({damagePercentage:P1} of {experienceReward}) - {aggroEntry.damageDealt} damage");
                }
                else
                {
                    Debug.LogWarning($"[Monster] Player {aggroEntry.playerName} ({playerNetId}) disconnected but earned {playerXP} XP ({damagePercentage:P1})");
                }
            }
        }
        
        // Выводим аггро список в консоль для отладки
        Debug.Log($"[Monster] Final aggro list:");
        for (int i = 0; i < aggroList.Count; i++)
        {
            var entry = aggroList[i];
            Debug.Log($"[Monster] #{i + 1}: {entry.playerName} - {entry.damageDealt} damage ({entry.damagePercentage:P1})");
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
        Debug.Log($"[Monster] Monster DIES! Name: {monsterName}, AggroEntries: {aggroList.Count}, DamageDealers: {damageDealers.Count}");
        
        // Распределяем опыт по нанесенному урону
        DistributeExperienceByDamage();
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
        
        // Уничтожаем монстра через время (оставляем труп видимым)
        StartCoroutine(DespawnAfterDelay(corpseVisibilityTime));
        
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
        // Отключаем колайдер и перемещаем на слой игнорируемых лучей
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        
        canMove = false;
        canAttack = false;
        
        // Визуальные эффекты смерти
        RpcDie();
        RpcHideMonsterUI();
        RpcMakeCorpseVisually();
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
    private void RpcMakeCorpseVisually()
    {
        // Делаем монстра визуально мертвым для всех клиентов
        Debug.Log($"[Monster] Making corpse visually dead for {monsterName}");
        
        // Изменяем материал на более темный/мертвый вид
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                mat.color = new Color(mat.color.r * 0.5f, mat.color.g * 0.5f, mat.color.b * 0.5f, mat.color.a);
            }
            
            // Отключаем тени чтобы труп не отбрасывал активные тени
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        // Отключаем физику Rigidbody если есть
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Запускаем DOTween анимацию смерти для не-гуманоидных монстров
        PlayDeathAnimation();
        
        // Добавляем эффект постепенного исчезновения через несколько секунд
        StartCoroutine(FadeOutCorpse());
    }
    
    private System.Collections.IEnumerator FadeOutCorpse()
    {
        // Ждем указанное время перед началом исчезновения
        yield return new WaitForSeconds(corpseFadeStartTime);
        
        // Проверяем, используется ли DOTween анимация смерти
        bool usingDOTweenDeath = IsNonHumanoidMonster() && _hitEffects != null;
        
        if (usingDOTweenDeath)
        {
            // Если используется DOTween, он сам управляет исчезновением
            Debug.Log($"[Monster] Using DOTween death animation fade-out for {monsterName}");
            yield break;
        }
        
        // Плавно исчезаем в течение указанного времени (для монстров без DOTween)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float fadeDuration = corpseFadeDuration;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);
            
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.materials;
                foreach (Material mat in materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }
            
            yield return null;
        }
        
        // В конце делаем полностью прозрачным
        foreach (var renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                Color color = mat.color;
                color.a = 0f;
                mat.color = color;
            }
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
                // RPC автоматически отправляется через PlayAnimation()
            }
            else
            {
                // Голова стоит - играем обычную анимацию
                Debug.Log($"[Monster] Playing HeadPunch animation for STANDING head");
                PlayAnimation("HeadPunch");
                // RPC автоматически отправляется через PlayAnimation()
            }
        }
        else if (isCombinedLegs)
        {
            Debug.Log($"[Monster] Legs attacking: {name}");
            PlayAnimation("LegsKick");
            // RPC автоматически отправляется через PlayAnimation()
        }
        else
        {
            // Обычные монстры (не комбинированные) - воспроизводим анимацию атаки
            Debug.Log($"[Monster] Regular monster executing attack: {name}");
            PlayAnimation("Attack"); // Стандартная анимация атаки для обычных монстров
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