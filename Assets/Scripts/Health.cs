using UnityEngine;
using Mirror;
using TMPro;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [HideInInspector]
    public int MaxHealth = 1000;

    // Ссылка на UI
    private PlayerUI playerUI;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int _currentHealth;

    // Ссылка на префаб цифр урона
    [Header("Damage Text")]
    public GameObject floatingTextPrefab;
    public float damageTextSpawnHeight = 2.5f; // Новая переменная для высоты спауна
    public float damageTextRandomness = 0.5f; // Новая переменная для случайности

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

        // Вызываем Rpc-метод для отображения лечения на всех клиентах
        RpcShowHealNumber(amount);
    }

    [Server]
    public void Init()
    {
        CurrentHealth = MaxHealth;
    }

    // Новый публичный метод для установки здоровья
    [Server]
    public void SetHealth(int amount)
    {
        CurrentHealth = amount;
        Debug.Log($"[Server] {gameObject.name} health set to: {CurrentHealth}");
    }

    [Server]
    public void TakeDamage(int amount, bool isCritical = false)
    {
        CurrentHealth -= amount;
        Debug.Log($"[Server] {gameObject.name} took {amount} damage. Current health: {CurrentHealth}");

        // Вызываем Rpc-метод для отображения урона на всех клиентах
        RpcShowDamageNumber(amount, isCritical);

        if (CurrentHealth <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has died.");
            GetComponent<PlayerCore>()?.SetDeathState(true);
        }
    }

    [ClientRpc]
    private void RpcShowDamageNumber(int damage, bool isCritical)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);

            // NOTE: You will need to modify your FloatingDamageText script to handle 'isCritical'.
            // For example, it could change the color or size of the text.
            /*
            FloatingDamageText damageTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (damageTextScript != null)
            {
                damageTextScript.SetDamageText(damage, isCritical);
            }
            */
        }
    }

    [ClientRpc]
    private void RpcShowHealNumber(int healAmount)
    {
        if (floatingTextPrefab != null)
        {
            // Позиция для спауна цифры лечения
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);

            /*
            FloatingDamageText healTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (healTextScript != null)
            {
                healTextScript.SetHealText(healAmount);
            }
            */
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