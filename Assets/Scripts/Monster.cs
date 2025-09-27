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
    private bool canMove = true;
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
        Debug.Log($"[Monster] Initialized health on server to: {_health.CurrentHealth}/{_health.MaxHealth}");
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
        Debug.Log($"[Monster] OnStartClient called. UI initialized with currentHealth: {_health.CurrentHealth}. IsHost={isServer}");
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
                NetworkServer.Spawn(legsGO);
                legsNetId = legsGO.GetComponent<NetworkIdentity>().netId;
                Debug.Log($"[Monster] Spawned legs for head {monsterName} at {legsGO.transform.position}");
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
            Debug.Log($"[Monster] Instantiated modelPrefab for {monsterName}");
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
        Debug.Log($"[Monster] Stun state changed: {newValue}, isClient={isClient}, isServer={isServer}");
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = newValue;
        }
    }

    private void OnSilenceStateChanged(bool _, bool newValue)
    {
        Debug.Log($"[Monster] Silence state changed: {newValue}, isClient={isClient}, isServer={isServer}");
    }

    private void OnHealthUpdatedHandler(int currentHP, int maxHP)
    {
        if (_monsterUI != null)
        {
            _monsterUI.SetData(monsterName, currentHP, maxHP);
            Debug.Log($"[Monster] OnHealthUpdatedHandler called. UI updated. New HP: {currentHP}, IsClient={isClient}");
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
            Debug.Log($"[Monster] RpcUpdateMonsterUI called: {currentHealth}/{maxHealth}");
        }
    }

    [Server]
    public void UpdateAggro(uint attackerNetId, int damage)
    {
        Debug.Log($"[Monster] UpdateAggro called: attackerNetId={attackerNetId}, damage={damage}, current aggro={aggroTargetNetId}");
        if (aggroTargetNetId == 0 || damage > 0)
        {
            aggroTargetNetId = attackerNetId;
            Debug.Log($"[Monster] Aggro updated to {attackerNetId} (damage: {damage})");
        }
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        if (_currentControlEffect != ControlEffectType.None && Time.time < _controlEffectEndTime && skillWeight <= _currentEffectWeight)
        {
            Debug.Log($"[Monster] Cannot apply {effectType} (weight {skillWeight}): {_currentControlEffect} (weight {_currentEffectWeight}) is active until {_controlEffectEndTime}");
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
            Debug.Log($"[Monster] Applied stun effect to {monsterName}, weight={skillWeight}, duration={duration}");
        }
        else if (effectType == ControlEffectType.Slow)
        {
            _slowPercentage = skillWeight / 100f;
            _originalSpeed = moveSpeed;
            if (_agent != null && _agent.isOnNavMesh)
            {
                float newSpeed = moveSpeed * (1f - _slowPercentage);
                _agent.speed = Mathf.Max(0.1f, newSpeed);
                Debug.Log($"[Monster] Applied slow effect to {monsterName}, weight={skillWeight}, percentage={_slowPercentage}, newSpeed={_agent.speed}, duration={duration}");
            }
            RpcApplySlowEffect(true);
        }
        else if (effectType == ControlEffectType.Silence)
        {
            IsSilenced = true;
            Debug.Log($"[Monster] Applied silence effect to {monsterName}, weight={skillWeight}, duration={duration}");
        }
    }

    [Server]
    public void ApplySlow(float percentage, float duration, int skillWeight)
    {
        if (_currentControlEffect != ControlEffectType.None && Time.time < _controlEffectEndTime && skillWeight <= _currentEffectWeight)
        {
            Debug.Log($"[Monster] Cannot apply slow (weight {skillWeight}): {_currentControlEffect} (weight {_currentEffectWeight}) is active until {_controlEffectEndTime}");
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
            Debug.Log($"[Monster] Applied slow to {monsterName}: percentage={percentage}, duration={duration}, weight={skillWeight}, newSpeed={_agent.speed}");
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
        Debug.Log($"[Monster] Cleared control effect for {monsterName}");
    }

    [ClientRpc]
    private void RpcApplySlowEffect(bool isActive)
    {
        if (isActive)
        {
            if (slowEffectPrefab != null && _slowEffectInstance == null)
            {
                _slowEffectInstance = Instantiate(slowEffectPrefab, transform);
                Debug.Log($"[Monster] Spawned slow effect particles for {monsterName}");
            }
            else if (_renderer != null)
            {
                _renderer.material.color = new Color(0.5f, 0.5f, 1f, 1f);
                Debug.Log($"[Monster] Applied slow visual effect (color change) to {monsterName}");
            }
        }
        else
        {
            if (_slowEffectInstance != null)
            {
                Destroy(_slowEffectInstance);
                _slowEffectInstance = null;
                Debug.Log($"[Monster] Removed slow effect particles from {monsterName}");
            }
            if (_renderer != null)
            {
                _renderer.material.color = Color.white;
                Debug.Log($"[Monster] Removed slow visual effect from {monsterName}");
            }
        }
    }

    [Server]
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"[Monster] Die called for {monsterName}, Health: {_health.CurrentHealth}, aggroTargetNetId={aggroTargetNetId}");
        Debug.Log($"[Monster] Die: aggroTargetNetId={aggroTargetNetId}, NetworkServer.spawned.Count={NetworkServer.spawned.Count}");
        if (aggroTargetNetId != 0 && NetworkServer.spawned.TryGetValue(aggroTargetNetId, out var identity))
        {
            PlayerCore killer = identity.GetComponent<PlayerCore>();
            if (killer != null && killer.Stats != null)
            {
                killer.Stats.AddExperience(experienceReward);
                Debug.Log($"[Monster] Gave {experienceReward} XP to killer {killer.playerName}, level before: {killer.Stats.level}");
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
        if (headNetId != 0 && NetworkServer.spawned.TryGetValue(headNetId, out var headIdentity))
        {
            Monster headMonster = headIdentity.GetComponent<Monster>();
            if (headMonster != null)
            {
                headMonster.Fall();
            }
        }
        foreach (var entry in dropTable)
        {
            if (entry.item != null && Random.value <= entry.dropChance)
            {
                SpawnDroppedItem(entry.item.id, 1);
                Debug.Log($"[Monster] Dropped item: {entry.item.itemName} (chance: {entry.dropChance})");
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
            Debug.Log($"[Monster] Applied random force {randomForce} to {monsterName}");
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
            Debug.Log($"[Monster] Set {monsterName} to Ignore Raycast layer");
        }
        canMove = false;
        canAttack = false;
        RpcDie();
        RpcHideMonsterUI();
        StartCoroutine(DespawnAfterDelay(2f));
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
        Debug.Log($"[Monster] Spawned dropped item: ID {itemID}, quantity {quantity} at {droppedItem.transform.position}, owner={aggroTargetNetId}");
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
            Debug.Log($"[Monster] RpcHideMonsterUI called for {gameObject.name}");
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
            Debug.Log($"[Monster] Destroyed {monsterName}");
        }
    }

    private void OnDestroy()
    {
        if (_monsterUI != null) Destroy(_monsterUI.gameObject);
    }

    [Server]
    public void ExecuteAttack(uint targetNetId, string skillName, int damage, bool isCritical)
    {
        if (!canAttack || IsDead || IsStunned || IsSilenced) return;
        GameObject targetObject = NetworkServer.spawned.ContainsKey(targetNetId) ? NetworkServer.spawned[targetNetId].gameObject : null;
        if (targetObject == null)
        {
            Debug.LogWarning($"[Monster] Target with netId {targetNetId} not found for attack");
            return;
        }
        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, DamageType.Physical, isCritical, netIdentity);
            Debug.Log($"[Monster] Attacked {targetObject.name} with {damage} damage, isCritical: {isCritical}");
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
        if (_agent != null) _agent.enabled = false;
        RpcFall();
    }
    [ClientRpc]
    private void RpcFall()
    {
        Vector3 targetPos = new Vector3(transform.position.x, 1f, transform.position.z) + Random.insideUnitSphere * 15f;
        targetPos.y = 1f;
        transform.DOJump(targetPos, 5f, 1, 1f).SetEase(Ease.InQuad);
        transform.DORotate(new Vector3(90f, 0f, 0f), 1f).SetEase(Ease.InQuad).OnComplete(() => {
            Debug.Log($"[Monster] {monsterName} fallen");
        });
    }

    // private void OnMonsterIdChanged(int oldId, int newId)
    // {
    // }
}