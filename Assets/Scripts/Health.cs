using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    public int MaxHealth = 100;

    // Ссылка на UI
    private PlayerUI playerUI;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int _currentHealth;

    public int CurrentHealth
    {
        get => _currentHealth;
        [Server]
        set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            playerUI = GetComponentInChildren<PlayerUI>();
        }
    }

    [Server]
    public void Heal(int amount)
    {
        CurrentHealth += amount;
        Debug.Log($"[Server] {gameObject.name} healed for {amount}. Current health: {CurrentHealth}");
    }

    [Server]
    public void Init()
    {
        CurrentHealth = MaxHealth;
    }

    [Server]
    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        Debug.Log($"[Server] {gameObject.name} took {amount} damage. Current health: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has died.");
            GetComponent<PlayerCore>()?.SetDeathState(true);
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"[Client] Health changed from {oldHealth} to {newHealth}");
        if (playerUI != null)
        {
            playerUI.UpdateHealthBar(newHealth, MaxHealth);
        }
    }
}