using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class TeamManagementUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject teamPanel;
    public Button togglePanelButton;
    public TMP_InputField guildInputField;
    public TMP_InputField partyInputField;
    public TMP_InputField factionInputField;
    public Button joinGuildButton;
    public Button leaveGuildButton;
    public Button joinPartyButton;
    public Button leavePartyButton;
    public Button joinFactionButton;
    public Button leaveFactionButton;
    public TextMeshProUGUI currentTeamInfo;

    private PlayerCore localPlayer;

    void Start()
    {
        // Hide panel by default
        if (teamPanel != null) teamPanel.SetActive(false);
        
        // Setup button listeners
        if (togglePanelButton != null)
            togglePanelButton.onClick.AddListener(TogglePanel);
        
        if (joinGuildButton != null)
            joinGuildButton.onClick.AddListener(JoinGuild);
        if (leaveGuildButton != null)
            leaveGuildButton.onClick.AddListener(LeaveGuild);
        
        if (joinPartyButton != null)
            joinPartyButton.onClick.AddListener(JoinParty);
        if (leavePartyButton != null)
            leavePartyButton.onClick.AddListener(LeaveParty);
        
        if (joinFactionButton != null)
            joinFactionButton.onClick.AddListener(JoinFaction);
        if (leaveFactionButton != null)
            leaveFactionButton.onClick.AddListener(LeaveFaction);
    }

    void Update()
    {
        // Update local player reference
        if (localPlayer == null)
        {
            localPlayer = PlayerCore.localPlayerCoreInstance;
        }
        
        // Update current team info
        UpdateTeamInfo();
    }

    public void TogglePanel()
    {
        if (teamPanel != null)
        {
            teamPanel.SetActive(!teamPanel.activeSelf);
        }
    }

    public void JoinGuild()
    {
        if (localPlayer == null || guildInputField == null) return;
        
        string guildId = guildInputField.text.Trim();
        if (!string.IsNullOrEmpty(guildId))
        {
            localPlayer.CmdJoinGuild(guildId);
            guildInputField.text = "";
        }
    }

    public void LeaveGuild()
    {
        if (localPlayer == null) return;
        localPlayer.CmdLeaveGuild();
    }

    public void JoinParty()
    {
        if (localPlayer == null || partyInputField == null) return;
        
        string partyId = partyInputField.text.Trim();
        if (!string.IsNullOrEmpty(partyId))
        {
            localPlayer.CmdJoinParty(partyId);
            partyInputField.text = "";
        }
    }

    public void LeaveParty()
    {
        if (localPlayer == null) return;
        localPlayer.CmdLeaveParty();
    }

    public void JoinFaction()
    {
        if (localPlayer == null || factionInputField == null) return;
        
        string factionId = factionInputField.text.Trim();
        if (!string.IsNullOrEmpty(factionId))
        {
            localPlayer.CmdJoinFaction(factionId);
            factionInputField.text = "";
        }
    }

    public void LeaveFaction()
    {
        if (localPlayer == null) return;
        localPlayer.CmdLeaveFaction();
    }

    private void UpdateTeamInfo()
    {
        if (currentTeamInfo == null || localPlayer == null) return;
        
        string info = $"Team: {localPlayer.team}";
        
        if (!string.IsNullOrEmpty(localPlayer.guildId))
            info += $"\nGuild: {localPlayer.guildId}";
        
        if (!string.IsNullOrEmpty(localPlayer.partyId))
            info += $"\nParty: {localPlayer.partyId}";
        
        if (!string.IsNullOrEmpty(localPlayer.factionId))
            info += $"\nFaction: {localPlayer.factionId}";
        
        currentTeamInfo.text = info;
    }
}
