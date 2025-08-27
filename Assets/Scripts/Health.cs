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
        if (isLocalPlayer)
        {
            playerUI = GetComponentInChildren<PlayerUI>();
        }
        // Find HealthBarUI for all entities
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        if (healthBarUI == null)
        {
            Debug.LogWarning($"[Health] HealthBarUI not found for {gameObject.name}, waiting for instantiation...");
        }
    }

    [Server]
    public void Init()
    {
        CurrentHealth = MaxHealth;
        Debug.Log($"[Server] {gameObject.name} health initialized: {CurrentHealth}/{MaxHealth}");
        RpcUpdateHealthUI(CurrentHealth, MaxHealth);
    }

    [Server]
    public void Heal(int amount)
    {
        CurrentHealth += amount;
        Debug.Log($"[Server] {gameObject.name} healed for {amount}. Current health: {CurrentHealth}");
        RpcShowHealNumber(amount);
    }

    [Server]
    public void SetHealth(int amount)
    {
        CurrentHealth = amount;
        Debug.Log($"[Server] {gameObject.name} health set to: {CurrentHealth}");
    }

    [Server]
    public void SetMaxHealth(int newMaxHealth)
    {
        MaxHealth = newMaxHealth;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        Debug.Log($"[Server] {gameObject.name} max health set to: {MaxHealth}");
        RpcUpdateHealthUI(CurrentHealth, MaxHealth);
    }

    [Server]
    public void TakeDamage(int baseDamage, DamageType damageType, bool isCritical = false, NetworkIdentity attacker = null)
    {
        int finalDamage = CalculateFinalDamage(baseDamage, damageType);
        CurrentHealth -= finalDamage;
        LastAttacker = attacker;
        Debug.Log($"[Server] {gameObject.name} took {finalDamage} damage from {attacker?.gameObject.name}. Current health: {CurrentHealth}");
        RpcShowDamageNumber(finalDamage, isCritical, damageType);

        if (CurrentHealth <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has died. Setting death state.");
            MonsterCore monster = GetComponent<MonsterCore>();
            PlayerCore player = GetComponent<PlayerCore>();
            if (monster != null && attacker != null)
            {
                monster.OnDeath(attacker.GetComponent<PlayerCore>());
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
            }
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
            }
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