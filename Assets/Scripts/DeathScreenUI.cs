using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject deathScreenPanel;
    public TextMeshProUGUI respawnTimerText;
    public Button respawnButton;
    public float respawnTime = 5.0f;

    private float _respawnCountdown;
    private bool _isDead = false;

    private void Awake()
    {
        deathScreenPanel = GameObject.Find("DeathScreenPanel");
        if (deathScreenPanel == null)
        {
            Debug.LogError("DeathScreenPanel not found!");
            return;
        }

        respawnTimerText = deathScreenPanel.transform.Find("RespawnTimerText").GetComponent<TextMeshProUGUI>();
        respawnButton = deathScreenPanel.transform.Find("RespawnButton").GetComponent<Button>();

        deathScreenPanel.SetActive(false);

        if (respawnButton != null)
        {
            respawnButton.onClick.AddListener(OnRespawnButtonClicked);
            respawnButton.interactable = false;
        }
    }

    private void Update()
    {
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
                respawnTimerText.text = "Ready to respawn!";
            }
        }
    }

    public void ShowDeathScreen()
    {
        _isDead = true;
        _respawnCountdown = respawnTime;

        deathScreenPanel.SetActive(true);

        if (respawnTimerText != null)
        {
            respawnTimerText.text = $"Respawn in: {Mathf.CeilToInt(_respawnCountdown)}s";
        }

        if (respawnButton != null)
        {
            respawnButton.interactable = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideDeathScreen()
    {
        _isDead = false;
        deathScreenPanel.SetActive(false);


    }

    private void OnRespawnButtonClicked()
    {
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            localPlayer.CmdRequestRespawn();
        }

        HideDeathScreen();
    }

    public void ForceHide()
    {
        _isDead = false;
        deathScreenPanel.SetActive(false);
    }
}