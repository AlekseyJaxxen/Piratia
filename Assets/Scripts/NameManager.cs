using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NameManager : MonoBehaviour
{
    public static NameManager Instance { get; private set; }
    private List<PlayerCore> players = new List<PlayerCore>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(PlayerCore player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"[NameManager] Registered player: {player.playerName}");
            StartCoroutine(UpdateNameTagsDelayed());
            EnsureHealthBarInitialized(player); // Инициализация HealthBar
        }
    }

    public void UnregisterPlayer(PlayerCore player)
    {
        if (player != null)
        {
            players.Remove(player);
            Debug.Log($"[NameManager] Unregistered player: {player.playerName}");
            HealthBarUI healthBar = player.GetHealthBarUI();
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(false); // Деактивируем HealthBar при удалении
            }
        }
    }

    public void UpdateAllNameTags()
    {
        StartCoroutine(UpdateNameTagsDelayed());
    }

    private IEnumerator UpdateNameTagsDelayed()
    {
        yield return new WaitForSeconds(1f); // Задержка для синхронизации сети
        int maxRetries = 5; // Увеличенное количество попыток
        int retryCount = 0;
        while (retryCount < maxRetries)
        {
            bool allInitialized = true;
            PlayerTeam localTeam = PlayerCore.localPlayerCoreInstance != null ? PlayerCore.localPlayerCoreInstance.team : PlayerTeam.None;
            foreach (PlayerCore player in players)
            {
                if (player == null) continue; // Пропускаем null игроков

                NameTagUI nameTag = player.GetNameTagUI();
                if (nameTag != null)
                {
                    nameTag.UpdateNameAndTeam(player.playerName, player.team, localTeam);
                    Debug.Log($"[NameManager] Updated NameTag for {player.playerName}, Team: {player.team}, LocalTeam: {localTeam}");
                }
                else
                {
                    Debug.LogWarning($"[NameManager] NameTagUI is null for {player.playerName}, retrying...");
                    allInitialized = false;
                }

                HealthBarUI healthBar = player.GetHealthBarUI();
                if (healthBar != null)
                {
                    int currentHealth = player.GetCurrentHealth();
                    int maxHealth = player.GetMaxHealth();
                    healthBar.gameObject.SetActive(currentHealth > 0); // Активируем/деактивируем в зависимости от жизни
                    healthBar.UpdateHP(currentHealth, maxHealth);
                }
                else
                {
                    Debug.LogWarning($"[NameManager] HealthBarUI is null for {player.playerName}, retrying...");
                    allInitialized = false;
                }
            }
            if (allInitialized) break;
            retryCount++;
            yield return new WaitForSeconds(0.5f); // Ждем перед повторной попыткой
        }
        if (retryCount >= maxRetries)
        {
            Debug.LogError("[NameManager] Failed to initialize NameTagUI or HealthBarUI for some players after retries!");
        }
    }

    private void EnsureHealthBarInitialized(PlayerCore player)
    {
        if (player.Health == null)
        {
            Debug.LogError($"[NameManager] Health component is null for {player.playerName}");
            return;
        }

        HealthBarUI healthBar = player.GetHealthBarUI();
        if (healthBar == null && player.GetHealthBarPrefab() != null)
        {
            GameObject healthBarInstance = Instantiate(player.GetHealthBarPrefab(), player.transform);
            healthBarInstance.SetActive(player.GetCurrentHealth() > 0); // Активируем только если жив
            healthBar = healthBarInstance.GetComponent<HealthBarUI>();
            player.SetHealthBarUI(healthBar); // Устанавливаем UI
            if (healthBar != null)
            {
                healthBar.Initialize(player);
                Debug.Log($"[NameManager] HealthBar initialized for {player.playerName}");
            }
            else
            {
                Debug.LogError($"[NameManager] Failed to get HealthBarUI component for {player.playerName}");
            }
        }
        else if (healthBar != null)
        {
            healthBar.UpdateHP(player.GetCurrentHealth(), player.GetMaxHealth());
            healthBar.gameObject.SetActive(player.GetCurrentHealth() > 0);
        }
    }
}