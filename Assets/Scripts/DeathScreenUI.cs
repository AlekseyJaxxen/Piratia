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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void InitializeUI()
    {
        if (deathScreenPanel == null)
        {
            Debug.LogError("DeathScreenPanel not assigned in Inspector!");
            return;
        }
        if (respawnTimerText == null)
        {
            Debug.LogError("RespawnTimerText not assigned in Inspector!");
            return;
        }
        if (respawnButton == null)
        {
            Debug.LogError("RespawnButton not assigned in Inspector!");
            return;
        }
        Debug.Log($"DeathScreenUI initialized for {GetComponentInParent<PlayerCore>().playerName}");
        deathScreenPanel.SetActive(false);
        respawnButton.onClick.AddListener(OnRespawnButtonClicked);
        respawnButton.interactable = false;

    }

    private void Update()
    {
        if (!isLocalPlayer || !_isDead || _canRespawn) return;

        _respawnCountdown -= Time.deltaTime;
        if (_respawnCountdown <= 0)
        {
            _canRespawn = true;
            respawnButton.interactable = true;
            respawnTimerText.text = "Ready to respawn!";
        }
        else
        {
            respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
        }
    }

    public void ShowDeathScreen()
    {
        if (!isLocalPlayer) return;
        _isDead = true;
        _canRespawn = false;
        _respawnCountdown = respawnTime;
        deathScreenPanel.SetActive(true);
        respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
        respawnButton.interactable = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log($"Showing DeathScreenPanel for {GetComponentInParent<PlayerCore>().playerName}");
    }

    public void HideDeathScreen()
    {
        if (!isLocalPlayer) return;
        _isDead = false;
        _canRespawn = false;
        deathScreenPanel.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log($"Hiding DeathScreenPanel for {GetComponentInParent<PlayerCore>().playerName}");
    }

    private void OnRespawnButtonClicked()
    {
        if (!isLocalPlayer || !_canRespawn) return;
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            localPlayer.CmdRequestRespawn();
        }
        HideDeathScreen();
    }
}