using UnityEngine;
using Mirror;
using TMPro;

public class HealthMonster : Health
{
    private Monster _monster;
    [SerializeField] int _health;
    [SerializeField] private MonsterAnimation monsterAnimation;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _monster = GetComponent<Monster>();
        if (_monster == null)
        {
            Debug.LogError($"[HealthMonster] Monster component missing on {gameObject.name}");
            return;
        }
        SetHealth(_health);
        Debug.Log($"[HealthMonster] Initialized health for {gameObject.name}: {CurrentHealth}");
    }
    [Server]
    public new void TakeDamage(int damage, DamageType damageType, bool isCritical, NetworkIdentity attacker)
    {
        if (CurrentHealth <= 0) return;
        base.TakeDamage(damage, damageType, isCritical, attacker);
        if (_monster == null)
        {
            _monster = GetComponent<Monster>();
            if (_monster == null)
            {
                Debug.LogError($"[HealthMonster] Monster component missing on {gameObject.name}");
                return;
            }
        }
        Debug.Log($"[HealthMonster] Damage taken: {damage}, Current health: {CurrentHealth}, Monster health: {CurrentHealth}/{MaxHealth}");
        _monster.RpcUpdateMonsterUI(CurrentHealth, MaxHealth);
        RpcShowDamageNumber(damage, isCritical);
        RpcPlayDamageFlash();
        if (CurrentHealth <= 0)
        {
            _monster.Die();
        }
    }
    [ClientRpc]
    private void RpcShowDamageNumber(int damage, bool isCritical)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 2f, 0);
            GameObject damageTextObj = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            FloatingDamageText damageTextScript = damageTextObj.GetComponent<FloatingDamageText>();
            if (damageTextScript != null)
            {
                damageTextScript.SetDamageText(damage, isCritical);
                Debug.Log($"[HealthMonster] Spawned damage text: -{damage} at {spawnPosition}, isCritical: {isCritical}");
            }
            else
            {
                Debug.LogWarning("[HealthMonster] FloatingDamageText component missing on floatingTextPrefab");
                Destroy(damageTextObj);
            }
        }
        else
        {
            Debug.LogWarning("[HealthMonster] floatingTextPrefab is null");
        }
    }
    [ClientRpc]
    private void RpcPlayDamageFlash()
    {
        if (monsterAnimation != null)
        {
            monsterAnimation.PlayDamageFlash();
            monsterAnimation.PlayShake();
            Debug.Log($"[HealthMonster] Triggered damage flash for {gameObject.name}");
        }
    }
    private void UpdateMonsterHealth(int newHealth, int maxHealth)
    {
        if (_monster != null)
        {
            Debug.Log($"[HealthMonster] Health updated via event, newHealth: {newHealth}, maxHealth: {maxHealth}");
        }
    }
    private void OnEnable()
    {
        OnHealthUpdated += UpdateMonsterHealth;
    }
    private void OnDisable()
    {
        OnHealthUpdated -= UpdateMonsterHealth;
    }
}