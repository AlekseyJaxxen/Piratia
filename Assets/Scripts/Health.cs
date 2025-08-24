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
    public void TakeDamage(int baseDamage, DamageType damageType, bool isCritical = false)
    {
        int finalDamage = CalculateFinalDamage(baseDamage, damageType);

        CurrentHealth -= finalDamage;
        Debug.Log($"[Server] {gameObject.name} took {finalDamage} damage (base: {baseDamage}, type: {damageType}). Current health: {CurrentHealth}");

        // Вызываем Rpc-метод для отображения урона на всех клиентах
        RpcShowDamageNumber(finalDamage, isCritical, damageType);

        if (CurrentHealth <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has died.");
            GetComponent<PlayerCore>()?.SetDeathState(true);
        }
    }

    [Server]
    private int CalculateFinalDamage(int baseDamage, DamageType damageType)
    {
        CharacterStats stats = GetComponent<CharacterStats>();
        if (stats == null) return baseDamage;

        switch (damageType)
        {
            case DamageType.Physical:
                // 1. Применяем физическое сопротивление (%)
                float damageAfterResistance = baseDamage * (1f - stats.physicalResistance / 100f);

                // 2. Вычитаем броню (плоское значение)
                int damageAfterArmor = Mathf.RoundToInt(damageAfterResistance) - stats.armor;

                // 3. Обеспечиваем минимальный урон
                return Mathf.Max((int)CombatConstants.MIN_PHYSICAL_DAMAGE, damageAfterArmor);

            case DamageType.Magic:
                // Магический урон пока не уменьшается (можно добавить магическое сопротивление позже)
                return baseDamage;

            default:
                return baseDamage;
        }
    }

    [ClientRpc]
    private void RpcShowDamageNumber(int damage, bool isCritical, DamageType damageType)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageTextSpawnHeight;
            GameObject floatingTextInstance = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);

            // Модифицируйте FloatingDamageText чтобы он принимал тип урона
            /*
            FloatingDamageText damageTextScript = floatingTextInstance.GetComponent<FloatingDamageText>();
            if (damageTextScript != null)
            {
                damageTextScript.SetDamageText(damage, isCritical, damageType);
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