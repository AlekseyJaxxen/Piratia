using UnityEngine;
using Mirror;
using TMPro;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    public int MaxHealth = 100;

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
    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        Debug.Log($"[Server] {gameObject.name} took {amount} damage. Current health: {CurrentHealth}");

        // Вызываем Rpc-метод для отображения урона на всех клиентах
        RpcShowDamageNumber(amount);

        if (CurrentHealth <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has died.");
            GetComponent<PlayerCore>()?.SetDeathState(true);
        }
    }

    [ClientRpc]
    private void RpcShowDamageNumber(int damage)
    {
        if (floatingTextPrefab != null)
        {
            // Создаем цифру урона немного выше персонажа
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);

            // Передаем значение урона
            FloatingDamageText damageTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (damageTextScript != null)
            {
                damageTextScript.SetDamageText(damage);
            }
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