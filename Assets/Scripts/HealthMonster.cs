using UnityEngine;
using Mirror;

public class HealthMonster : Health
{
    private Monster _monster;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _monster = GetComponent<Monster>();
        if (_monster == null)
        {
            Debug.LogError($"[HealthMonster] Monster component missing on {gameObject.name}");
            return;
        }
        SetHealth(_monster.maxHealth);
        Debug.Log($"[HealthMonster] Initialized health for {gameObject.name}: {_monster.maxHealth}");
    }

    [Server]
    public new void TakeDamage(int damage, DamageType damageType, bool isCritical, NetworkIdentity attacker)
    {
        base.TakeDamage(damage, damageType, isCritical, attacker);
        if (_monster != null)
        {
            _monster.currentHealth = CurrentHealth;
            Debug.Log($"[HealthMonster] Damage taken: {damage}, Current health: {CurrentHealth} for {gameObject.name}");
        }
    }

    public void SetHealthBarUI(MonsterHealthBarUI healthBarUI)
    {
        // Do not call base.SetHealthBarUI, as it’s for the generic HealthBarUI
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHP(CurrentHealth, MaxHealth);
            Debug.Log($"[HealthMonster] MonsterHealthBarUI set for {gameObject.name}");
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

    private void UpdateMonsterHealth(int newHealth, int maxHealth)
    {
        if (_monster != null)
        {
            _monster.currentHealth = newHealth;
            Debug.Log($"[HealthMonster] Health updated: {newHealth}/{maxHealth} for {gameObject.name}");
        }
    }
}