using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class ReviveRequestUI : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject revivePanel;
    public TextMeshProUGUI messageText;
    public Button acceptButton;
    public Button declineButton; // Опционально, для отказа
    private bool _isShown = false;


    private void Awake()
    {
        if (revivePanel != null) revivePanel.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (revivePanel == null)
        {
            Debug.LogError("RevivePanel not assigned in Inspector!");
            return;
        }
        if (messageText == null)
        {
            Debug.LogError("MessageText not assigned in Inspector!");
            return;
        }
        if (acceptButton == null)
        {
            Debug.LogError("AcceptButton not assigned in Inspector!");
            return;
        }
        revivePanel.SetActive(false);
        acceptButton.onClick.AddListener(OnAcceptButtonClicked);
        if (declineButton != null)
        {
            declineButton.onClick.AddListener(OnDeclineButtonClicked);
        }
    }

    public void Show(string casterName = "")
    {
        Debug.Log($"[ReviveRequestUI] Showing for {casterName}");
        if (!isLocalPlayer) return;
        _isShown = true;
        revivePanel.SetActive(true);
        messageText.text = string.IsNullOrEmpty(casterName) ? "Take revive" : $"Take revive from {casterName}?";
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        if (!isLocalPlayer) return;
        _isShown = false;
        revivePanel.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnAcceptButtonClicked()
    {
        if (!isLocalPlayer || !_isShown) return;
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            localPlayer.CmdAcceptRevive();
        }
        Hide();
    }

    private void OnDeclineButtonClicked()
    {
        Hide(); // Просто скрыть, без действий
    }
}