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

        if (_isDead)
        {
            _respawnCountdown -= Time.deltaTime;

            if (respawnTimerText != null)
            {
                respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
            }

            if (_respawnCountdown <= 0 && respawnButton != null && !respawnButton.interactable)
            {
                respawnButton.interactable = true;
                if (respawnTimerText != null)
                    respawnTimerText.text = "Ready to respawn!";
            }
        }
    }

    [TargetRpc]
    public void TargetShowDeathScreen(NetworkConnection conn)
    {
        if (!isLocalPlayer) return;

        if (deathScreenPanel == null || respawnTimerText == null || respawnButton == null)
        {
            Debug.LogError("UI elements are not initialized in ShowDeathScreen!");
            return;
        }

        _isDead = true;
        _respawnCountdown = respawnTime;

        deathScreenPanel.SetActive(true);
        respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
        respawnButton.interactable = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [TargetRpc]
    public void TargetHideDeathScreen(NetworkConnection conn)
    {
        if (!isLocalPlayer) return;

        if (deathScreenPanel != null)
        {
            _isDead = false;
            deathScreenPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnRespawnButtonClicked()
    {
        if (!isLocalPlayer) return;

        PlayerCore localPlayer = GetComponent<PlayerCore>();
        if (localPlayer != null)
        {
            localPlayer.CmdRequestRespawn();
        }
        TargetHideDeathScreen(connectionToClient);
    }

    [TargetRpc]
    public void TargetForceHide(NetworkConnection conn)
    {
        if (!isLocalPlayer) return;

        if (deathScreenPanel != null)
        {
            _isDead = false;
            deathScreenPanel.SetActive(false);
        }
    }
}