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
        // SetHealth(_health);
        
        // Auto-find MonsterAnimation if not assigned
        if (monsterAnimation == null)
        {
            monsterAnimation = GetComponent<MonsterAnimation>();
            if (monsterAnimation == null)
            {
                monsterAnimation = GetComponentInChildren<MonsterAnimation>();
            }
            // MonsterAnimation auto-found
        }
        
        Debug.Log($"[HealthMonster] Initialized health for {gameObject.name}: {CurrentHealth}");
    }
    [Server]
    public new void TakeDamage(int damage, DamageType damageType, bool isCritical, NetworkIdentity attacker)
    {
        if (CurrentHealth <= 0) return;
        if (_monster == null)
        {
            _monster = GetComponent<Monster>();
            if (_monster == null)
            {
                Debug.LogError($"[HealthMonster] Monster component missing on {gameObject.name}");
                return;
            }
        }
        // Aggro ����� ������
        _monster.UpdateAggro(attacker.netId, damage);
        base.TakeDamage(damage, damageType, isCritical, attacker);
        // Damage taken
        _monster.RpcUpdateMonsterUI(CurrentHealth, MaxHealth);
        RpcPlayDamageFlash();
        // Death is handled by base.TakeDamage() in Health.cs
        // Damage text is also handled by base.TakeDamage() in Health.cs
    }
    [ClientRpc]
    private void RpcPlayDamageFlash()
    {
        if (monsterAnimation != null)
        {
            monsterAnimation.PlayDamageFlash();
            monsterAnimation.PlayShake();
            // Damage flash triggered
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