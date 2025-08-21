using UnityEngine;
using Mirror;
using TMPro; // Добавьте эту строку для работы с TextMeshPro

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
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);

            // Debug-лог, чтобы проверить, что объект создан и где
            Debug.Log($"Damage text created at position: {floatingTextInstance.transform.position}");

            // Получаем основную камеру и поворачиваем текст к ней
            Transform mainCamera = Camera.main.transform;
            if (mainCamera != null)
            {
                floatingTextInstance.transform.LookAt(mainCamera);
                // Чтобы текст не был перевернут
                floatingTextInstance.transform.Rotate(0, 180, 0);
            }

            // Передаем значение урона
            floatingTextInstance.GetComponent<FloatingDamageText>()?.SetDamageText(damage);
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