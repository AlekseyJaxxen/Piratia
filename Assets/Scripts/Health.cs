using UnityEngine;
using Mirror;
using TMPro;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [SyncVar(hook = nameof(OnMaxHealthChanged))]
    public int MaxHealth = 1000;
    private PlayerUI playerUI;
    private HealthBarUI healthBarUI;
    [SyncVar(hook = nameof(OnHealthChanged))]
    private int _currentHealth;
    [SyncVar]
    public NetworkIdentity LastAttacker;
    public event System.Action<int, int> OnHealthUpdated;
    [Header("Damage Text")]
    public GameObject floatingTextPrefab;
    public float damageTextSpawnHeight = 2.5f;
    public float damageTextRandomness = 0.5f;

    public int CurrentHealth
    {
        get => _currentHealth;
        [Server]
        set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            RpcUpdateHealthUI(_currentHealth, MaxHealth);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Init();
    }

    private void Start()
    {
        if (isLocalPlayer && !gameObject.CompareTag("Enemy"))
        {
            playerUI = GetComponentInChildren<PlayerUI>();
        }
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        if (healthBarUI == null && !gameObject.CompareTag("Enemy"))
        {
            Debug.LogWarning($"[Health] HealthBarUI not found for {gameObject.name}, waiting for instantiation...");
        }
    }

    [Server]
    public void Init()
    {
        CurrentHealth = MaxHealth;
        // Health initialized on server
        RpcUpdateHealthUI(CurrentHealth, MaxHealth);
    }

    [Server]
    public void Heal(int amount)
    {
        CurrentHealth += amount;
        // Healed on server
        RpcShowHealNumber(amount);
    }

    [Server]
    public void SetHealth(int amount)
    {
        CurrentHealth = amount;
        // Health set on server
    }

    [Server]
    public void SetMaxHealth(int newMaxHealth)
    {
        MaxHealth = newMaxHealth;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        // Max health set on server
        if (NetworkServer.spawned.ContainsKey(netId))
        {
            RpcUpdateHealthUI(CurrentHealth, MaxHealth);
        }
        else
        {
            Debug.LogWarning($"[Health] Object {gameObject.name} not spawned yet, delaying RpcUpdateHealthUI");
        }
    }

    [Server]
    public void TakeDamage(int baseDamage, DamageType damageType, bool isCritical, NetworkIdentity attacker = null, float damageMultiplier = 1f)
    {
        PlayerCore attackerCore = attacker?.GetComponent<PlayerCore>();
        if (attackerCore != null && attackerCore.isDead)
        {
            Debug.LogWarning($"[Health] Ignoring damage from dead attacker {attackerCore.playerName}");
            return;
        }
        int finalDamage = CalculateFinalDamage(baseDamage, damageType);
        finalDamage = Mathf.RoundToInt(finalDamage * damageMultiplier);
        if (isCritical)
        {
            CharacterStats attackerStats = attacker?.GetComponent<CharacterStats>();
            float critMultiplier = attackerStats != null ? attackerStats.criticalHitMultiplier : 2.0f;
            finalDamage = Mathf.RoundToInt(finalDamage * critMultiplier);
            // Critical hit applied
        }
        CurrentHealth -= finalDamage;
        LastAttacker = attacker;
        // Damage taken on server
        RpcShowDamageNumber(finalDamage, isCritical, damageType);
        PlayerSkills skills = GetComponent<PlayerSkills>();
        if (skills != null)
        {
            // Player took damage
            if (skills._isInvisible)
            {
                skills.SetToggleBuff("Invisibility", false); // Замена InterruptInvisibility
            }
        }
        else
        {
            Debug.LogWarning($"[Health] PlayerSkills component not found on {gameObject.name}");
        }
        if (CurrentHealth <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has died. Setting death state.");
            Monster monster = GetComponent<Monster>();
            PlayerCore player = GetComponent<PlayerCore>();
            if (monster != null)
            {
                monster.Die();
            }
            else if (player != null)
            {
                player.SetDeathState(true);
            }
        }
    }

    [Server]
    private int CalculateFinalDamage(int baseDamage, DamageType damageType)
    {
        CharacterStats stats = GetComponent<CharacterStats>();
        if (stats == null) return baseDamage;
        switch (damageType)
        {
            case DamageType.Physical:
                float damageAfterResistance = baseDamage * (1f - stats.physicalResistance / 100f);
                int damageAfterArmor = Mathf.RoundToInt(damageAfterResistance) - stats.armor;
                return Mathf.Max((int)CombatConstants.MIN_PHYSICAL_DAMAGE, damageAfterArmor);
            case DamageType.Magic:
                return baseDamage;
            default:
                return baseDamage;
        }
    }

    public void SetHealthBarUI(HealthBarUI healthBarUI)
    {
        this.healthBarUI = healthBarUI;
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHP(_currentHealth, MaxHealth);
            Debug.Log($"[Health] HealthBarUI set for {gameObject.name}, initial health: {_currentHealth}/{MaxHealth}");
        }
    }

    [ClientRpc]
    private void RpcShowDamageNumber(int damage, bool isCritical, DamageType damageType)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            FloatingDamageText damageTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (damageTextScript != null)
            {
                damageTextScript.SetDamageText(damage, isCritical);
                Debug.Log($"[Client] Spawned damage text: -{damage} (isCritical: {isCritical}) at {spawnPosition} for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Health] FloatingDamageText component missing on floatingTextPrefab for {gameObject.name}");
                Destroy(floatingTextInstance);
            }
        }
        else
        {
            Debug.LogWarning($"[Health] floatingTextPrefab is null for {gameObject.name}");
        }
    }

    [ClientRpc]
    private void RpcShowHealNumber(int healAmount)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            FloatingDamageText healTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (healTextScript != null)
            {
                healTextScript.SetHealText(healAmount);
                Debug.Log($"[Client] Spawned heal text: +{healAmount} at {spawnPosition} for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Health] FloatingDamageText component missing on floatingTextPrefab for {gameObject.name}");
                Destroy(floatingTextInstance);
            }
        }
        else
        {
            Debug.LogWarning($"[Health] floatingTextPrefab is null for {gameObject.name}");
        }
    }

    [ClientRpc]
    private void RpcUpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (isLocalPlayer && playerUI != null)
        {
            playerUI.UpdateHealthBar(currentHealth, maxHealth);
        }
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHP(currentHealth, maxHealth);
        }
        OnHealthUpdated?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[Client] RpcUpdateHealthUI: {currentHealth}/{maxHealth} for {gameObject.name}");
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"[Client] Health changed from {oldHealth} to {newHealth} for {gameObject.name}");
        OnHealthUpdated?.Invoke(newHealth, MaxHealth);
        if (isLocalPlayer && playerUI != null)
        {
            playerUI.UpdateHealthBar(newHealth, MaxHealth);
        }
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHP(newHealth, MaxHealth);
        }
        if (newHealth < oldHealth)
        {
            PlayerAnimation anim = GetComponent<PlayerAnimation>();
            if (anim != null) anim.PlayDamageFlash();
            if (healthBarUI != null) healthBarUI.PlayDamageFlash();
        }
    }

    private void OnMaxHealthChanged(int oldMaxHealth, int newMaxHealth)
    {
        Debug.Log($"[Client] Max Health changed from {oldMaxHealth} to {newMaxHealth} for {gameObject.name}");
        OnHealthUpdated?.Invoke(CurrentHealth, newMaxHealth);
        if (isLocalPlayer && playerUI != null)
        {
            playerUI.UpdateHealthBar(CurrentHealth, newMaxHealth);
        }
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHP(CurrentHealth, newMaxHealth);
        }
    }
}