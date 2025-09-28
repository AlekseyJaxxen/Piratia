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
    private MonsterInfo info;
    [Header("Monster Settings")]
    [SyncVar(hook = nameof(OnNameChanged))] public string monsterName = "Monster";
    private float moveSpeed = 5f;
    private float attackCooldown = 2f;
    private GameObject deathVFXPrefab;
    public bool canMove = true;
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
    private HealthMonster _health;
    private MonsterSkillExecutor _skillExecutor;
    // ��� combined
    [SyncVar] public uint legsNetId = 0;
    [SyncVar] public uint headNetId = 0;
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
            Debug.LogWarning("[Monster] MonsterSkillExecutor component missing!");
        }
        _renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (_renderer == null)
        {
            Debug.LogWarning($"[Monster] SkinnedMeshRenderer not found on {monsterName}");
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
                legsMonster.canMove = false;
                legsMonster.canAttack = false;
                if (legsMonster._agent != null) legsMonster._agent.enabled = false; // ��� AI
                
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
            transform.position += Vector3.up * 15f; // Head �� 15
            if (_agent != null) _agent.baseOffset = 15f; // ������� �����
        }
        else if (headNetId != 0)
        {
            // Legs
            canAttack = false;
            canMove = false;
            if (_agent != null) _agent.enabled = false; // ��������� AI ��� legs
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
            else if (_renderer != null)
            {
                _renderer.material.color = new Color(0.5f, 0.5f, 1f, 1f);
                // Slow visual effect applied
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
            if (_renderer != null)
            {
                _renderer.material.color = Color.white;
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
        
        // Уведомляем клиентов о смерти головы
        Debug.Log($"[Monster] Die() called for {name}, headNetId: {headNetId}, isServer: {isServer}");
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
    private void RpcOnLegsDeath(uint headId)
    {
        Debug.Log($"[Monster] RpcOnLegsDeath called for headId: {headId} on client, isServer: {isServer}, isClient: {isClient}");
        // Находим голову и заставляем её упасть на клиенте
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        bool headFound = false;
        foreach (Monster monster in allMonsters)
        {
            if (monster.netIdentity.netId == headId)
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
            Debug.LogWarning($"[Monster] Head monster with netId {headId} not found on client");
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
        
        // Сбрасываем позицию модели для combined монстра
        if (info.isCombined && _renderer != null)
        {
            _renderer.transform.localPosition = Vector3.zero;
            Debug.Log($"[Monster] Reset model position to zero for combined monster");
        }
        
        // Запускаем анимацию падения
        Vector3 targetPos = new Vector3(transform.position.x, 1f, transform.position.z) + Random.insideUnitSphere * 15f;
        targetPos.y = 1f;
        Debug.Log($"[Monster] Starting fall animation from {transform.position} to {targetPos}");
        StartCoroutine(FallCoroutine(targetPos));
    }

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
            targetHealth.TakeDamage(damage, DamageType.Physical, isCritical, netIdentity);
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
        
        // Сбрасываем позицию модели для combined монстра
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
        
        // Сбрасываем позицию модели для combined монстра на клиенте
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

    // private void OnMonsterIdChanged(int oldId, int newId)
    // {
    // }
}