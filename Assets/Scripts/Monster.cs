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
    [SyncVar] public bool IsCooldown = false;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private GameObject monsterUIPrefab; // Новый префаб для UI (HP + Name)
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool canAttack = true;
    private NavMeshAgent _agent;
    private MonsterUI _monsterUI; // Новый компонент UI
    public bool IsDead;
    [SyncVar] private float _slowPercentage = 0f;
    [SyncVar] private float _originalSpeed = 0f;
    [SyncVar] private ControlEffectType _currentControlEffect = ControlEffectType.None;
    [SyncVar] private float _controlEffectEndTime = 0f;
    [SyncVar(hook = nameof(OnStunStateChanged))] public bool IsStunned = false;
    [SyncVar] private int _currentEffectWeight = 0;
    [SerializeField] public float stoppingDistance = 1f;
    [SerializeField] public MonsterBasicAttackSkill basicAttackSkill;
    // Ссылка на компонент Rigidbody и Collider на дочернем объекте-модели
    [SerializeField] private Rigidbody modelRigidbody;
    [SerializeField] private Collider modelCollider;
    // Новая переменная для силы толчка при смерти
    [SerializeField] private float pushForce = 500f;

    private void Awake()
    {
        if (basicAttackSkill == null) Debug.LogError("Skill not assigned");
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
            if (basicAttackSkill == null)
            {
                Debug.LogError("[Monster] MonsterBasicAttackSkill component missing!");
                canAttack = false;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (monsterUIPrefab != null)
        {
            GameObject uiInstance = Instantiate(monsterUIPrefab, transform);
            _monsterUI = uiInstance.GetComponent<MonsterUI>();
            if (_monsterUI != null)
            {
                _monsterUI.target = transform;
                _monsterUI.UpdateName(monsterName);
                _monsterUI.UpdateHP(currentHealth, maxHealth);
                // Устанавливаем цвет имени монстра на красный.
                _monsterUI.SetNameColor(Color.red);
            }
        }
        StartCoroutine(CheckControlEffectExpiration());
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
        if (_monsterUI != null)
        {
            _monsterUI.UpdateHP(newHealth, maxHealth);
            Debug.Log($"[Monster] UI updated: {newHealth}/{maxHealth} for {monsterName}");
        }
        if (newHealth <= 0 && !IsDead)
        {
            Die();
        }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[Monster] Stun state changed: {oldValue} -> {newValue}, isClient={isClient}, isServer={isServer}");
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = newValue;
        }
    }

    [Server]
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"[Monster] Die called for {monsterName}, Health: {currentHealth}");

        // Отключаем навигационный агент и коллайдер на родительском объекте
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        BoxCollider parentCollider = GetComponent<BoxCollider>();
        if (parentCollider != null)
        {
            parentCollider.enabled = false;
            Debug.Log($"[Monster] BoxCollider disabled for {monsterName}");
        }

        // Включаем физику на дочерней модели
        if (modelRigidbody != null)
        {
            modelRigidbody.isKinematic = false; // Отключаем кинематику
            modelRigidbody.useGravity = true;    // Включаем гравитацию

            // Применяем толчок, чтобы модель отлетела назад
            modelRigidbody.AddForce(-transform.forward * pushForce, ForceMode.Impulse);
        }

        if (modelCollider != null)
        {
            modelCollider.enabled = true; // Включаем коллайдер, чтобы модель могла упасть и соприкасаться с полом
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
            GameObject vfx = Object.Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Object.Destroy(vfx, 1f);
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
        if (_monsterUI != null) Object.Destroy(_monsterUI.gameObject);
    }

    private void OnDestroy()
    {
        if (_monsterUI != null) Object.Destroy(_monsterUI.gameObject);
    }

    // Новый метод для обработки атаки
    [Server]
    public void ExecuteAttack(uint targetNetId, string skillName, int damage, bool isCritical)
    {
        if (!canAttack || IsDead || IsStunned) return;
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

    // Метод для получения эффектов контроля от скиллов
    [Server]
    public void ReceiveControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        ApplyControlEffect(effectType, duration, skillWeight);
    }

    // Проверка истечения эффектов контроля
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
}