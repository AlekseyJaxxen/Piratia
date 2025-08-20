using UnityEngine;
using Mirror;
using System.Collections;

public class Health : NetworkBehaviour
{
    [Header("Health")]
    [SyncVar(hook = nameof(HandleHealthChanged))]
    public int maxHealth = 100;

    [SyncVar(hook = nameof(HandleHealthChanged))]
    private int _currentHealth;

    [Header("VFX")]
    public GameObject damageEffectPrefab;
    public GameObject deathEffectPrefab;
    public GameObject healEffectPrefab;

    public event System.Action<int, int> HealthChanged;

    private PlayerCore _playerCore;

    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    #region Initialization
    private void Awake()
    {
        _playerCore = GetComponent<PlayerCore>();
    }

    public override void OnStartServer()
    {
        // Initialize health on server; SyncVar will propagate to clients
        _currentHealth = Mathf.Clamp(_currentHealth <= 0 ? maxHealth : _currentHealth, 0, maxHealth);
    }
    #endregion

    #region Health Changed Hook
    private void HandleHealthChanged(int oldValue, int newValue)
    {
        HealthChanged?.Invoke(oldValue, newValue);
    }
    #endregion

    #region Damage
    [Server]
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || _currentHealth <= 0) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        RpcPlayDamageEffect();

        if (_currentHealth <= 0)
        {
            RpcDie();
            _playerCore?.SetDeathState(true);
            StartCoroutine(RespawnRoutine());
        }
    }

    [ClientRpc]
    private void RpcPlayDamageEffect()
    {
        if (damageEffectPrefab == null) return;

        Quaternion effectRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
        GameObject effect = Instantiate(damageEffectPrefab, transform.position, effectRotation);
        Destroy(effect, 2f);
    }

    [ClientRpc]
    private void RpcDie()
    {
        if (deathEffectPrefab == null) return;

        GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, 3f);
    }
    #endregion

    #region Healing
    public void Heal(int amount)
    {
        if (amount <= 0) return;

        // Always call the Command; Mirror will handle server/client
        CmdHeal(amount);
    }

    [Command]
    private void CmdHeal(int amount)
    {
        if (amount <= 0) return;
        HealServer(amount);
    }

    [Server]
    private void HealServer(int amount)
    {
        if (_currentHealth <= 0) return; // Optionally allow resurrection

        int old = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);

        if (_currentHealth != old)
            RpcPlayHealEffect();
    }

    private IEnumerator RespawnRoutine()
    {
        // Ждем 5 секунд перед возрождением
        yield return new WaitForSeconds(5f);

        // Восстанавливаем здоровье до максимума
        _currentHealth = maxHealth;
        // Сбрасываем флаг смерти в PlayerCore
        _playerCore?.SetDeathState(false);

        // 🚨 НОВОЕ: Отправляем команду всем клиентам, чтобы они обновили позицию
        RpcRespawn();
    }

    [ClientRpc]
    private void RpcPlayHealEffect()
    {
        if (healEffectPrefab == null) return;

        GameObject effect = Instantiate(healEffectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, 2f);
    }

    private void RpcRespawn()
    {
        // 🚨 НОВОЕ: Обновляем позицию и, возможно, другие состояния на клиентах
        // Это будет работать, только если у вас есть список точек возрождения.
        // Если их нет, можно просто сбросить позицию на начальную или в центр.
        transform.position = Vector3.zero; // Пример
        Debug.Log("Игрок возрожден!");
    }
    #endregion
}
