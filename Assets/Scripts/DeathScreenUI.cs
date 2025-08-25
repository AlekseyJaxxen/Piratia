using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class DeathScreenUI : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject deathScreenPanel;
    public TextMeshProUGUI respawnTimerText;
    public Button respawnButton;
    public float respawnTime = 5.0f;

    private float _respawnCountdown;
    private bool _isDead = false;
    private bool _canRespawn = false;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (deathScreenPanel == null)
        {
            deathScreenPanel = GameObject.Find("DeathScreenPanel");
            if (deathScreenPanel == null)
            {
                Debug.LogError("DeathScreenPanel not found!");
                return;
            }
        }

        if (respawnTimerText == null)
            respawnTimerText = deathScreenPanel.transform.Find("RespawnTimerText")?.GetComponent<TextMeshProUGUI>();

        if (respawnButton == null)
            respawnButton = deathScreenPanel.transform.Find("RespawnButton")?.GetComponent<Button>();

        if (respawnTimerText == null || respawnButton == null)
        {
            Debug.LogError("RespawnTimerText or RespawnButton not found!");
            return;
        }

        deathScreenPanel.SetActive(false);
        respawnButton.onClick.AddListener(OnRespawnButtonClicked);
        respawnButton.interactable = false;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (_isDead && !_canRespawn)
        {
            _respawnCountdown -= Time.deltaTime;

            // Останавливаем таймер на 0
            if (_respawnCountdown <= 0)
            {
                _respawnCountdown = 0;
                _canRespawn = true;

                if (respawnButton != null && !respawnButton.interactable)
                {
                    respawnButton.interactable = true;
                }

                if (respawnTimerText != null)
                    respawnTimerText.text = "Ready to respawn!";
            }
            else if (respawnTimerText != null)
            {
                // Показываем только целые секунды
                respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
            }
        }
    }

    public void ShowDeathScreen()
    {
        if (!isLocalPlayer) return;

        if (deathScreenPanel == null || respawnTimerText == null || respawnButton == null)
        {
            Debug.LogError("UI elements are not initialized in ShowDeathScreen!");
            return;
        }

        _isDead = true;
        _canRespawn = false;
        _respawnCountdown = respawnTime;

        deathScreenPanel.SetActive(true);
        respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
        respawnButton.interactable = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideDeathScreen()
    {
        if (!isLocalPlayer) return;

        if (deathScreenPanel != null)
        {
            _isDead = false;
            _canRespawn = false;
            deathScreenPanel.SetActive(false);
        }

        // НЕ блокируем курсор здесь - это должно управляться в другом месте
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    private void OnRespawnButtonClicked()
    {
        if (!isLocalPlayer || !_canRespawn) return;

        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            localPlayer.CmdRequestRespawn();
        }

        // Скрываем экран, но не управляем курсором здесь
        ForceHide();
    }

    public void ForceHide()
    {
        if (!isLocalPlayer) return;

        if (deathScreenPanel != null)
        {
            _isDead = false;
            _canRespawn = false;
            deathScreenPanel.SetActive(false);
        }
    }
}