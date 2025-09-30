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
    public void TakeDamage(int baseDamage, DamageType damageType, bool isCritical, NetworkIdentity attacker = null, float damageMultiplier = 1f, bool isBasicAttack = false)
    {
        PlayerCore attackerCore = attacker?.GetComponent<PlayerCore>();
        if (attackerCore != null && attackerCore.isDead)
        {
            Debug.LogWarning($"[Health] Ignoring damage from dead attacker {attackerCore.playerName}");
            return;
        }
        
        // Check hit/miss only for basic attacks
        if (isBasicAttack && damageType == DamageType.Physical && !CheckHit(attacker))
        {
            Debug.Log($"[Health] Basic attack missed! Attacker: {attacker?.name}, Target: {gameObject.name}");
            RpcShowMissText();
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
        RpcShowDamageNumber(finalDamage, isCritical, damageType, attacker);
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
    private bool CheckHit(NetworkIdentity attacker)
    {
        if (attacker == null) return true; // No attacker, always hit
        
        int attackerHit = 0;
        int targetDodge = 0;
        
        // Get attacker hit rate
        PlayerCore attackerPlayer = attacker.GetComponent<PlayerCore>();
        Monster attackerMonster = attacker.GetComponent<Monster>();
        
        if (attackerPlayer != null)
        {
            CharacterStats attackerStats = attackerPlayer.GetComponent<CharacterStats>();
            if (attackerStats != null)
            {
                attackerHit = Mathf.RoundToInt(attackerStats.hitChance);
            }
        }
        else if (attackerMonster != null)
        {
            attackerHit = attackerMonster.hitRate;
        }
        
        // Get target dodge
        PlayerCore targetPlayer = GetComponent<PlayerCore>();
        Monster targetMonster = GetComponent<Monster>();
        
        if (targetPlayer != null)
        {
            CharacterStats targetStats = targetPlayer.GetComponent<CharacterStats>();
            if (targetStats != null)
            {
                targetDodge = Mathf.RoundToInt(targetStats.dodgeChance);
            }
        }
        else if (targetMonster != null)
        {
            targetDodge = targetMonster.dodge;
        }
        
        // Calculate hit chance: 100 - (dodge - hit_rate + 10) = 100 - dodge + hit_rate - 10 = 90 + hit_rate - dodge (min 10, max 100)
        int hitChance = Mathf.Clamp(90 + attackerHit - targetDodge, 10, 100);
        
        // Roll for hit
        int roll = Random.Range(1, 101); // 1-100
        bool hit = roll <= hitChance;
        
        Debug.Log($"[Health] Hit check: Attacker hit={attackerHit}, Target dodge={targetDodge}, Hit chance={hitChance}%, Roll={roll}, Result={(hit ? "HIT" : "MISS")}");
        
        return hit;
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
    private void RpcShowDamageNumber(int damage, bool isCritical, DamageType damageType, NetworkIdentity attacker)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            FloatingDamageText damageTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (damageTextScript != null)
            {
                // Определяем тип урона
                bool isOtherPlayer = attacker != null && !attacker.isLocalPlayer;
                bool isReceivedDamage = gameObject.GetComponent<NetworkIdentity>().isLocalPlayer;
                
                damageTextScript.SetDamageText(damage, isCritical, isOtherPlayer, isReceivedDamage);
                Debug.Log($"[Client] Spawned damage text: -{damage} (isCritical: {isCritical}, isOtherPlayer: {isOtherPlayer}, isReceivedDamage: {isReceivedDamage}) at {spawnPosition} for {gameObject.name}");
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
    private void RpcShowMissText()
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            FloatingDamageText missTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (missTextScript != null)
            {
                missTextScript.SetMissText();
                Debug.Log($"[Client] Spawned miss text: MISS at {spawnPosition} for {gameObject.name}");
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