// BACKUP FILE - COMMENTED OUT TO AVOID DUPLICATE CLASSES
/*
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using DG.Tweening;

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
    
    // Методы для управления анимациями combined монстра
    [Server]
    public void PlayAnimation(string animationName)
    {
        if (_animator != null)
        {
            _animator.Play(animationName);
        }
    }
    
    [ClientRpc]
    public void RpcPlayAnimation(string animationName)
    {
        if (_animator != null)
        {
            _animator.Play(animationName);
        }
    }
    
    // Специальные методы для combined монстров
    [Server]
    public void PlayLegsAttackAnimation()
    {
        if (info.isCombined && headNetId != 0) // Это ноги
        {
            // Находим голову и проигрываем анимацию на её модели
            if (NetworkServer.spawned.TryGetValue(headNetId, out var headIdentity))
            {
                Monster headMonster = headIdentity.GetComponent<Monster>();
                if (headMonster != null)
                {
                    headMonster.PlayAnimation("Kick");
                    headMonster.RpcPlayAnimation("Kick");
                }
            }
        }
    }
    
    [Server]
    public void PlayHeadAttackAnimation()
    {
        if (info.isCombined && legsNetId != 0) // Это голова
        {
            // Проигрываем анимацию на своей модели
            PlayAnimation("HeadPunch");
            RpcPlayAnimation("HeadPunch");
        }
    }
    
    [Server]
    public void PlayHeadAttackLyingAnimation()
    {
        if (info.isCombined && legsNetId != 0) // Это голова
        {
            // Проигрываем анимацию удара рукой лежа
            PlayAnimation("HeadPunchLying");
            RpcPlayAnimation("HeadPunchLying");
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
    private SkinnedMeshRenderer _renderer;
    private Animator _animator;
    private HealthMonster _health;
    private MonsterSkillExecutor _skillExecutor;
    //  combined
    [SyncVar] public uint legsNetId = 0;
    [SyncVar] public uint headNetId = 0;
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<HealthMonster>();
        _skillExecutor = GetComponent<MonsterSkillExecutor>();
        _rigidbody = GetComponent<Rigidbody>();
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
        stoppingDistance = info.stoppingDistance;
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
                legsMonster.headNetId = netIdentity.netId;
                legsMonster.canMove = true;  // Legs должны двигаться
                legsMonster.canAttack = true;  // Legs должны атаковать
                if (legsMonster._agent != null) legsMonster._agent.enabled = true; //  AI
                
                // Инициализируем синхронизацию ног с головой
                CombinedMonsterSync legsSyncComponent = legsGO.GetComponent<CombinedMonsterSync>();
                if (legsSyncComponent == null)
                {
                    legsSyncComponent = legsGO.AddComponent<CombinedMonsterSync>();
                }
                
                NetworkServer.Spawn(legsGO);
                legsNetId = legsGO.GetComponent<NetworkIdentity>().netId;
                
                // Инициализируем синхронизацию после спавна
                legsSyncComponent.InitializeAsLegs(netIdentity.netId);
                // Legs spawned for combined monster
            }
            transform.position += Vector3.up * 15f; // Head 15
            if (_agent != null) _agent.baseOffset = 15f; //  
        }
        else if (headNetId != 0)
        {
            // Legs
            canAttack = true;  // Legs должны атаковать
            canMove = true;    // Legs должны двигаться
            if (_agent != null) _agent.enabled = true; // Включаем AI для legs
        }
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
            // Model prefab instantiated
            _renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_renderer == null)
            {
                Debug.LogWarning($"[Monster] SkinnedMeshRenderer not found after instantiating model on {monsterName}");
            }
            
            // Получаем Animator для управления анимациями
            _animator = model.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = model.GetComponentInChildren<Animator>();
            }
            if (_animator == null)
            {
                Debug.LogWarning($"[Monster] Animator not found after instantiating model on {monsterName}");
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

    private void OnMonsterIdChanged(int _, int newId)
    {
        monsterId = newId;
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
        // Aggro updated
        if (aggroTargetNetId == 0 || damage > 0)
        {
            aggroTargetNetId = attackerNetId;
            // Aggro target updated
        }
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        if (_currentControlEffect != ControlEffectType.None && Time.time < _controlEffectEndTime && skillWeight <= _currentEffectWeight)
        {
            return; // Более слабый эффект не может перезаписать более сильный
        }

        _currentControlEffect = effectType;
        _controlEffectEndTime = Time.time + duration;
        _currentEffectWeight = skillWeight;

        switch (effectType)
        {
            case ControlEffectType.Stun:
                IsStunned = true;
                break;
            case ControlEffectType.Silence:
                IsSilenced = true;
                break;
        }
    }

    private IEnumerator CheckControlEffectExpiration()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            if (Time.time >= _controlEffectEndTime)
            {
                switch (_currentControlEffect)
                {
                    case ControlEffectType.Stun:
                        IsStunned = false;
                        break;
                    case ControlEffectType.Silence:
                        IsSilenced = false;
                        break;
                }
                _currentControlEffect = ControlEffectType.None;
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
        
        // Уведомляем клиентов о смерти головы
        Debug.Log($"[Monster] Die() called for {name}, headNetId: {headNetId}, legsNetId: {legsNetId}, isServer: {isServer}");
        
        // Если это голова combined монстра - убиваем ноги
        if (info.isCombined && legsNetId != 0)
        {
            Debug.Log($"[Monster] Head died, killing legs with netId: {legsNetId}");
            if (NetworkServer.spawned.TryGetValue(legsNetId, out var legsIdentity))
            {
                Monster legsMonster = legsIdentity.GetComponent<Monster>();
                if (legsMonster != null)
                {
                    Debug.Log($"[Monster] Killing legs monster: {legsMonster.name}");
                    legsMonster.Die();
                }
                else
                {
                    Debug.LogWarning($"[Monster] Legs monster component not found for netId: {legsNetId}");
                }
            }
            else
            {
                Debug.LogWarning($"[Monster] Legs monster not found in spawned objects for netId: {legsNetId}");
            }
        }
        
        // Если это ноги combined монстра - заставляем голову упасть
        if (headNetId != 0)
        {
            Debug.Log($"[Monster] Calling RpcOnLegsDeath for headNetId: {headNetId}");
            RpcOnLegsDeath(headNetId);
            
            // Также заставляем голову упасть на сервере
            if (NetworkServer.spawned.TryGetValue(headNetId, out var headIdentity))
            {
                Monster headMonster = headIdentity.GetComponent<Monster>();
                if (headMonster != null)
                {
                    Debug.Log($"[Monster] Calling Fall() on server for head: {headMonster.name}");
                    headMonster.Fall();
                }
                else
                {
                    Debug.LogWarning($"[Monster] Head monster component not found for netId: {headNetId}");
                }
            }
            else
            {
                Debug.LogWarning($"[Monster] Head monster not found in spawned objects for netId: {headNetId}");
            }
        }
        
        // Уничтожаем монстра сразу после смерти
        StartCoroutine(DespawnAfterDelay(0.1f));
        foreach (var entry in dropTable)
        {
            if (entry.item != null && Random.value <= entry.dropChance)
            {
                SpawnDroppedItem(entry.item.id, 1);
                // Item dropped
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
                UnityEngine.Random.Range(minForce.x, maxForce.x),
                UnityEngine.Random.Range(minForce.y, maxForce.y),
                UnityEngine.Random.Range(minForce.z, maxForce.z)
            );
            physicsRigidbody.AddForce(randomForce, ForceMode.Impulse);
        }
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 5f);
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
    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcOnMonsterDeath()
    {
        if (info.isCombined && legsNetId != 0) // Это голова
        {
            // Отключаем коллайдер только для legs
            if (NetworkServer.spawned.TryGetValue(legsNetId, out var legsIdentity))
            {
                BoxCollider legsCollider = legsIdentity.GetComponent<BoxCollider>();
                if (legsCollider != null)
                {
                    legsCollider.enabled = false;
                }
            }
        }
        else if (headNetId != 0) // Это ноги
        {
            // Отключаем коллайдер ног
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
        }
    }

    [ClientRpc]
    private void RpcOnLegsDeath(uint headNetId)
    {
        Debug.Log($"[Monster] RpcOnLegsDeath called for headNetId: {headNetId}");
        
        // Ищем голову по netId
        bool headFound = false;
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        foreach (Monster monster in allMonsters)
        {
            if (monster.netIdentity.netId == headNetId)
            {
                Debug.Log($"[Monster] Found head monster: {monster.name}, calling FallOnClient");
                // НЕ устанавливаем IsDead = true для головы - она должна оставаться живой
                monster.FallOnClient();
                headFound = true;
                break;
            }
        }
        
        if (!headFound)
        {
            Debug.LogWarning($"[Monster] Head monster with netId {headNetId} not found for RpcOnLegsDeath");
        }
    }

    public void FallOnClient()
    {
        Debug.Log($"[Monster] FallOnClient called for {name}");
        // Падение на клиенте без серверных вызовов
        canMove = false;
        // НЕ меняем canAttack - голова должна оставаться атакуемой после падения
        
        // Отключаем NavMeshAgent при падении
        if (_agent != null) 
        {
            _agent.enabled = false;
            _agent.baseOffset = 0f;
        }
        
        // НЕ отключаем NetworkTransform - оставляем для синхронизации позиции
        // NetworkTransformHybrid networkTransform = GetComponent<NetworkTransformHybrid>();
        // if (networkTransform != null)
        // {
        //     networkTransform.enabled = false;
        //     Debug.Log($"[Monster] Disabled NetworkTransformHybrid for {name}");
        //     // Включаем обратно после анимации падения для синхронизации урона
        //     StartCoroutine(ReenableNetworkTransformAfterFall());
        // }
        
        // Анимируем падение
        Vector3 targetPos = new Vector3(transform.position.x, 1f, transform.position.z) + Random.insideUnitSphere * 15f;
        targetPos.y = 1f;
        Debug.Log($"[Monster] Starting fall animation from {transform.position} to {targetPos}");
        StartCoroutine(FallCoroutine(targetPos));
    }

    [Server]
    public void Fall()
    {
        canMove = false;
        if (_agent != null) 
        {
            _agent.enabled = false;
            _agent.baseOffset = 0f; // Сбрасываем baseOffset
        }
        
        // Устанавливаем позицию на сервере тоже
        Vector3 targetPos = new Vector3(transform.position.x, 1f, transform.position.z) + Random.insideUnitSphere * 15f;
        targetPos.y = 1f;
        transform.position = targetPos;
        Debug.Log($"[Monster] Server set head position to {targetPos}");
        
        // Устанавливаем финальную позицию модели для combined монстра
        if (info.isCombined && _renderer != null)
        {
            _renderer.transform.localPosition = Vector3.zero;
        }
        
        RpcFall();
    }
    [ClientRpc]
    private void RpcFall()
    {
        Vector3 targetPos = new Vector3(transform.position.x, 1f, transform.position.z) + Random.insideUnitSphere * 15f;
        targetPos.y = 1f;
        Debug.Log($"[Monster] RpcFall called, target position: {targetPos}");
        
        // Устанавливаем финальную позицию модели для combined монстра
        if (info.isCombined && _renderer != null)
        {
            _renderer.transform.localPosition = Vector3.zero;
        }
        
        // Используем Coroutine вместо DoTween
        StartCoroutine(FallCoroutine(targetPos));
    }
    
    private IEnumerator FallCoroutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        Vector3 startRot = transform.eulerAngles;
        Vector3 targetRot = new Vector3(90f, 0f, 0f);
        
        float duration = 1f;
        float elapsed = 0f;
        
        Debug.Log($"[Monster] FallCoroutine started: {name}, isCombined: {info?.isCombined}, _renderer: {_renderer != null}");
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Плавное движение к цели
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            // Плавный поворот
            transform.eulerAngles = Vector3.Lerp(startRot, targetRot, t);
            
            // Для combined монстра модель автоматически повторит поворот головы (она child)
            if (info.isCombined && _renderer != null)
            {
                // Модель должна оставаться на уровне головы (y=0 относительно transform)
                _renderer.transform.localPosition = Vector3.zero;
            }
            
            yield return null;
        }
        
        // Убеждаемся, что финальная позиция установлена
        transform.position = targetPos;
        transform.eulerAngles = targetRot;
        
        // Финальная позиция модели для combined монстра
        if (info.isCombined && _renderer != null)
        {
            // Модель автоматически повторит поворот головы (она child)
            _renderer.transform.localPosition = Vector3.zero;
            Debug.Log($"[Monster] Set model position to {_renderer.transform.localPosition} for combined monster");
        }
        
        // Фиксируем объект в пространстве - отключаем все компоненты движения
        if (_agent != null)
        {
            _agent.enabled = false; // Оставляем отключенным
            Debug.Log($"[Monster] NavMeshAgent remains disabled for {name} - object frozen");
        }
        
        // Отключаем Rigidbody если есть
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log($"[Monster] Rigidbody set to kinematic for {name} - object frozen");
        }
        
        Debug.Log($"[Monster] FallCoroutine completed: {name} at position {transform.position} - OBJECT FROZEN");
    }

    [ClientRpc]
    private void RpcDie()
    {
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }
    }

    [ClientRpc]
    private void RpcHideMonsterUI()
    {
        if (_monsterUI != null)
        {
            _monsterUI.gameObject.SetActive(false);
        }
    }

    [Server]
    public void ExecuteAttack()
    {
        if (!canAttack || IsDead || IsStunned || IsSilenced) return;
        
        if (basicAttackSkill != null)
        {
            basicAttackSkill.ExecuteAttack(this);
        }
    }

    [Server]
    public void SpawnDroppedItem(int itemId, int quantity)
    {
        if (droppedItemPrefab != null)
        {
            GameObject droppedItem = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity);
            Item item = droppedItem.GetComponent<Item>();
            if (item != null)
            {
                item.id = itemId;
                item.quantity = quantity;
            }
            NetworkServer.Spawn(droppedItem);
        }
    }

    [Server]
    public void ApplySlow(float percentage, float duration)
    {
        if (_slowEffectInstance != null)
        {
            Destroy(_slowEffectInstance);
        }
        
        _slowPercentage = percentage;
        _originalSpeed = moveSpeed;
        moveSpeed *= (1f - percentage / 100f);
        
        if (_agent != null)
        {
            _agent.speed = moveSpeed;
        }
        
        if (slowEffectPrefab != null)
        {
            _slowEffectInstance = Instantiate(slowEffectPrefab, transform);
        }
        
        StartCoroutine(RemoveSlowAfterDelay(duration));
    }

    private IEnumerator RemoveSlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (_slowEffectInstance != null)
        {
            Destroy(_slowEffectInstance);
            _slowEffectInstance = null;
        }
        
        moveSpeed = _originalSpeed;
        _slowPercentage = 0f;
        
        if (_agent != null)
        {
            _agent.speed = moveSpeed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (info == null) return;
        
        // Обычные монстры
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, info.attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, info.detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, info.patrolRadius);
        
        // Combined монстры
        if (info.isCombined)
        {
            Vector3 headPos = new Vector3(transform.position.x, transform.position.y + 15f, transform.position.z);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(headPos, 8f); // headAttackRange
            Vector3 legsPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(legsPos, 7f); // legsAttackRange
            Gizmos.color = Color.white;
            Gizmos.DrawLine(headPos, legsPos);
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
*/
