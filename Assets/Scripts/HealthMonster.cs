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
    public new void TakeDamage(int damage, DamageType damageType, bool isCritical, NetworkIdentity attacker, bool isBasicAttack = false)
    {
        Debug.Log($"[HealthMonster] TakeDamage called on {gameObject.name}, currentHealth: {CurrentHealth}, damage: {damage}, attacker: {attacker?.name}");
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
        base.TakeDamage(damage, damageType, isCritical, attacker, 1f, isBasicAttack);
        // Damage taken
        _monster.RpcUpdateMonsterUI(CurrentHealth, MaxHealth);
        RpcPlayDamageFlash();
        
        // Проверяем, умрет ли монстр после этого урона
        if (CurrentHealth <= 0)
        {
            Debug.Log($"[HealthMonster] Monster {gameObject.name} will die, isCombinedHead: {_monster.isCombinedHead}, isCombinedLegs: {_monster.isCombinedLegs}");
        }
        // Death is handled by base.TakeDamage() in Health.cs
        // Damage text is also handled by base.TakeDamage() in Health.cs
    }
    [ClientRpc]
    private void RpcPlayDamageFlash()
    {
        // Старые эффекты через MonsterAnimation (для совместимости)
        if (monsterAnimation != null)
        {
            monsterAnimation.PlayDamageFlash();
            monsterAnimation.PlayShake();
            // Damage flash triggered
        }
        
        // Новые DoTween эффекты для не-гуманоидных монстров
        if (_monster != null && _monster.IsNonHumanoidMonster())
        {
            try
            {
                _monster.PlaySimpleHitEffect();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HealthMonster] Error playing hit effect for {_monster.monsterName}: {e.Message}");
            }
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