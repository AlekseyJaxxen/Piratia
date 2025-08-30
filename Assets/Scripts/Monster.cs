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
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] public MonsterBasicAttackSkill attackSkill;
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool canAttack = true;
    private NavMeshAgent _agent;
    private MonsterHealthBarUI _healthBarUI;
    private NameTagUI _nameTagUI;
    public bool IsDead;
    [SyncVar] private float _slowPercentage = 0f;
    [SyncVar] private float _originalSpeed = 0f;
    [SyncVar] private ControlEffectType _currentControlEffect = ControlEffectType.None;
    [SyncVar] private float _controlEffectEndTime = 0f;
    [SyncVar(hook = nameof(OnStunStateChanged))] public bool IsStunned = false;
    [SyncVar] private int _currentEffectWeight = 0;
    [SerializeField] public float stoppingDistance = 1f;

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
                _agent.baseOffset = 0.2f;
                _agent.speed = moveSpeed;
                _agent.stoppingDistance = stoppingDistance;
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
        if (isServer)
        {
            if (_currentControlEffect != ControlEffectType.None && Time.time >= _controlEffectEndTime)
            {
                ClearControlEffect();
            }
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

    [Server]
    public void TakeDamage(int damage)
    {
        if (IsDead) return;
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
            IsStunned = true;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                Debug.Log($"[Monster] Stun applied, NavMeshAgent stopped: isStopped={_agent.isStopped}, isOnNavMesh={_agent.isOnNavMesh}");
            }
            Debug.Log($"[Monster] Applied stun effect to {monsterName}, weight={skillWeight}, duration={duration}");
        }
        else if (effectType == ControlEffectType.Slow)
        {
            _slowPercentage = duration;
            _originalSpeed = moveSpeed;
            if (_agent != null && _agent.isOnNavMesh) _agent.speed = moveSpeed * (1f - _slowPercentage);
            Debug.Log($"[Monster] Applied slow effect to {monsterName}, weight={skillWeight}, percentage={_slowPercentage}, duration={duration}");
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
        Debug.Log($"[Monster] Applied slow to {monsterName}: percentage={percentage}, duration={duration}, weight={skillWeight}");
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
        }
        _currentControlEffect = ControlEffectType.None;
        _currentEffectWeight = 0;
        _controlEffectEndTime = 0f;
        Debug.Log($"[Monster] Cleared control effect for {monsterName}");
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
        Debug.Log($"[Monster] Stun state changed: {oldValue} -> {newValue}, isClient={isClient}, isServer={isServer}");
    }

    [Server]
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"[Monster] Die called for {monsterName}, Health: {currentHealth}");
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            Debug.Log($"[Monster] BoxCollider disabled for {monsterName}");
        }
        canMove = false;
        canAttack = false;
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