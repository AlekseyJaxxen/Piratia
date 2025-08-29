using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.AI;

public class Monster : NetworkBehaviour
{
    [Header("Monster Settings")]
    [SyncVar] public string monsterName = "Monster";
    [SyncVar] public int maxHealth = 1000;
    [SyncVar(hook = nameof(OnHealthChanged))] public int currentHealth;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] private MonsterBasicAttackSkill attackSkill;
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool canAttack = true;

    private NavMeshAgent _agent;
    private PlayerCore _target;
    private float _lastAttackTime;
    private MonsterHealthBarUI _healthBarUI;
    private NameTagUI _nameTagUI;
    private bool _isDead;
    [SyncVar] private float _slowPercentage = 0f;
    [SyncVar] private float _originalSpeed = 0f;
    [SyncVar] private ControlEffectType _currentControlEffect = ControlEffectType.None;
    [SyncVar] private float _controlEffectEndTime = 0f;
    [SyncVar(hook = nameof(OnStunStateChanged))] private bool _isStunned = false;
    [SyncVar] private int _currentEffectWeight = 0; // Added missing field

    private void Awake()
    {
        if (canMove)
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                Debug.LogError("[Monster] NavMeshAgent component missing!");
                canMove = false;
            }
            else
            {
                _agent.speed = moveSpeed;
                _agent.stoppingDistance = attackRange;
                if (!_agent.isOnNavMesh)
                {
                    Debug.LogWarning($"[Monster] {monsterName} is not on NavMesh at {transform.position}. Disabling movement.");
                    canMove = false;
                }
            }
        }

        currentHealth = maxHealth;

        if (canAttack)
        {
            attackSkill = GetComponent<MonsterBasicAttackSkill>();
            if (attackSkill == null)
            {
                Debug.LogError("[Monster] MonsterBasicAttackSkill component missing!");
                canAttack = false;
            }
        }

        _healthBarUI = GetComponentInChildren<MonsterHealthBarUI>();
        if (_healthBarUI == null)
        {
            Debug.LogWarning($"[Monster] MonsterHealthBarUI component missing for {monsterName}");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(SetupUIDelayed());
    }

    private IEnumerator SetupUIDelayed()
    {
        while (true)
        {
            GameObject canvasObject = GameObject.Find("TeamSelectionCanvas");
            Canvas mainCanvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;

            if (mainCanvas != null && mainCanvas.gameObject.activeInHierarchy)
            {
                if (nameTagPrefab != null)
                {
                    GameObject nameTagInstance = Instantiate(nameTagPrefab, mainCanvas.transform);
                    _nameTagUI = nameTagInstance.GetComponent<NameTagUI>();
                    if (_nameTagUI != null)
                    {
                        _nameTagUI.target = transform;
                        _nameTagUI.UpdateNameAndTeam(monsterName, PlayerTeam.None, PlayerCore.localPlayerCoreInstance != null ? PlayerCore.localPlayerCoreInstance.team : PlayerTeam.None);
                        Debug.Log($"[Monster] NameTagUI initialized for {monsterName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Monster] NameTagUI component missing on nameTagPrefab for {monsterName}");
                    }
                }
                yield break;
            }

            Debug.LogWarning($"[Monster] No TeamSelectionCanvas found or not active for {monsterName}, retrying...");
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (isServer && !_isDead)
        {
            if (_currentControlEffect != ControlEffectType.None && Time.time >= _controlEffectEndTime)
            {
                ClearControlEffect();
            }

            if (canMove && _agent != null && _agent.isOnNavMesh)
            {
                FindTarget();
                if (_target != null)
                {
                    float distance = Vector3.Distance(transform.position, _target.transform.position);
                    if (distance <= attackRange && !_isStunned && canAttack)
                    {
                        _agent.isStopped = true;
                        RotateTo(_target.transform.position - transform.position);
                        TryAttack();
                    }
                    else
                    {
                        _agent.isStopped = false;
                        _agent.SetDestination(_target.transform.position);
                    }
                }
            }
            else if (canAttack && !_isStunned)
            {
                FindTarget();
                if (_target != null && Vector3.Distance(transform.position, _target.transform.position) <= attackRange)
                {
                    RotateTo(_target.transform.position - transform.position);
                    TryAttack();
                }
            }
        }
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 10f, playerLayer);
        float closestDistance = float.MaxValue;
        PlayerCore closestPlayer = null;

        foreach (Collider hit in hits)
        {
            PlayerCore player = hit.GetComponent<PlayerCore>();
            if (player != null && !player.isDead)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
        }
        _target = closestPlayer;
        if (_target != null)
            Debug.Log($"[Monster] Target found: {_target.gameObject.name}");
    }

    private void TryAttack()
    {
        if (!canAttack) return;
        if (Time.time >= _lastAttackTime + attackCooldown && _target != null && attackSkill != null)
        {
            _lastAttackTime = Time.time;
            attackSkill.Execute(this, null, _target.gameObject);
        }
    }

    [Server]
    public void ExecuteAttack(uint targetNetId, string skillName, int damage, bool isCritical)
    {
        if (!NetworkServer.spawned.ContainsKey(targetNetId))
        {
            Debug.LogWarning($"[Monster] Target netId {targetNetId} not found for {skillName}");
            return;
        }

        NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
        Health targetHealth = targetIdentity.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, DamageType.Physical, isCritical, GetComponent<NetworkIdentity>());
            RpcPlayAttackVFX(transform.position, transform.rotation, targetIdentity.transform.position, isCritical, skillName);
        }
    }

    [ClientRpc]
    private void RpcPlayAttackVFX(Vector3 startPos, Quaternion startRotation, Vector3 endPos, bool isCritical, string skillName)
    {
        if (attackSkill != null)
        {
            attackSkill.PlayVFX(startPos, startRotation, endPos, isCritical);
        }
    }

    private void RotateTo(Vector3 direction)
    {
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        HealthMonster health = GetComponent<HealthMonster>();
        if (health == null)
        {
            Debug.LogError($"[Monster] HealthMonster component missing on {monsterName}");
            return;
        }
        health.TakeDamage(damage, DamageType.Physical, false, null);
        currentHealth = health.CurrentHealth;
        if (currentHealth <= 0)
        {
            Die();
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
            _isStunned = true;
            Debug.Log($"[Monster] Applied stun effect, weight={skillWeight}, duration={duration}");
        }
        else if (effectType == ControlEffectType.Slow)
        {
            _slowPercentage = duration;
            _originalSpeed = moveSpeed;
            if (_agent != null && _agent.isOnNavMesh) _agent.speed = moveSpeed * (1f - _slowPercentage);
            Debug.Log($"[Monster] Applied slow effect, weight={skillWeight}, percentage={_slowPercentage}, duration={duration}");
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
        if (_agent != null && _agent.isOnNavMesh) _agent.speed = moveSpeed * (1f - _slowPercentage);
        _controlEffectEndTime = Time.time + duration;
        Debug.Log($"[Monster] Applied slow: percentage={percentage}, duration={duration}, weight={skillWeight}");
    }

    [Server]
    private void ClearControlEffect()
    {
        if (_currentControlEffect == ControlEffectType.Stun)
        {
            _isStunned = false;
        }
        else if (_currentControlEffect == ControlEffectType.Slow && _originalSpeed > 0f)
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.speed = _originalSpeed;
            _slowPercentage = 0f;
            _originalSpeed = 0f;
        }
        _currentControlEffect = ControlEffectType.None;
        _currentEffectWeight = 0;
        _controlEffectEndTime = 0f;
        Debug.Log("[Monster] Cleared control effect");
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (_healthBarUI != null)
        {
            _healthBarUI.UpdateHP(newHealth, maxHealth);
            Debug.Log($"[Monster] HealthBarUI updated: {newHealth}/{maxHealth} for {monsterName}");
        }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[Monster] Stun state changed: {oldValue} -> {newValue}");
    }

    [Server]
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        Debug.Log($"[Monster] Die called for {monsterName}, Health: {currentHealth}");
        if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            Debug.Log($"[Monster] BoxCollider disabled for {monsterName}");
        }
        RpcDie();
        StartCoroutine(DespawnAfterDelay(1f));
    }

    [ClientRpc]
    private void RpcDie()
    {
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1f);
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(1f);
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
            Debug.Log($"[Monster] Destroyed {monsterName}");
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (_nameTagUI != null) Destroy(_nameTagUI.gameObject);
    }

    private void OnDestroy()
    {
        if (_nameTagUI != null) Destroy(_nameTagUI.gameObject);
    }
}