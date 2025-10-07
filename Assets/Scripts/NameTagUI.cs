using UnityEngine;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI teamText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    public Transform target;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(mainCamera.transform);
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        }
    }

    public void UpdateNameAndTeam(string name, PlayerTeam team, PlayerTeam localTeam, bool isLocalPlayer = false)
    {
        if (nameText != null) nameText.text = name;
        // Update team text to show dynamic team info
        string teamInfo = GetTeamDisplayText(team);
        if (teamText != null) teamText.text = teamInfo;
        // ��������� ����� � �������� � �������, ����� � �������
        // Check if this is the local player's own name tag
        bool isOwnNameTag = isLocalPlayer;
        
        Color color;
        if (isOwnNameTag)
        {
            color = Color.white; // Own name tag is white
        }
        else
        {
            // Get PlayerCore components for party checking
            PlayerCore targetPlayerCore = GetComponentInParent<PlayerCore>();
            PlayerCore localPlayerCore = PlayerCore.localPlayerCoreInstance;
            
            color = GetNameColor(targetPlayerCore, localPlayerCore, team, localTeam);
        }
        if (nameText != null) nameText.color = color;
        if (teamText != null) teamText.color = color;
    }
    
    /// <summary>
    /// Determines the color for a player's name based on their relationship to the local player
    /// </summary>
    private Color GetNameColor(PlayerCore targetPlayerCore, PlayerCore localPlayerCore, PlayerTeam targetTeam, PlayerTeam localTeam)
    {
        if (targetPlayerCore == null || localPlayerCore == null)
        {
            // Fallback to basic team logic if PlayerCore components are not available
            return IsAlly(targetTeam, localTeam) ? Color.green : Color.red;
        }
        
        // Check if players are in the same party
        if (!string.IsNullOrEmpty(targetPlayerCore.partyId) && 
            !string.IsNullOrEmpty(localPlayerCore.partyId) && 
            targetPlayerCore.partyId == localPlayerCore.partyId)
        {
            return Color.cyan; // Party members are cyan/blue
        }
        
        // Check if players are in the same guild
        if (!string.IsNullOrEmpty(targetPlayerCore.guildId) && 
            !string.IsNullOrEmpty(localPlayerCore.guildId) && 
            targetPlayerCore.guildId == localPlayerCore.guildId)
        {
            return Color.yellow; // Guild members are yellow
        }
        
        // Check if players are in the same faction
        if (!string.IsNullOrEmpty(targetPlayerCore.factionId) && 
            !string.IsNullOrEmpty(localPlayerCore.factionId) && 
            targetPlayerCore.factionId == localPlayerCore.factionId)
        {
            return Color.magenta; // Faction members are magenta
        }
        
        // Fallback to basic team logic
        return IsAlly(targetTeam, localTeam) ? Color.green : Color.red;
    }
    
    /// <summary>
    /// Checks if target team is an ally to local team
    /// Solo players are never allies to each other
    /// </summary>
    private bool IsAlly(PlayerTeam targetTeam, PlayerTeam localTeam)
    {
        if (localTeam == PlayerTeam.None) return false; // Default to red if no local team (safer for PvP)
        
        // Solo players are never allies to each other
        if (localTeam == PlayerTeam.Solo && targetTeam == PlayerTeam.Solo)
        {
            return false; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return localTeam == targetTeam;
    }
    
    /// <summary>
    /// Gets display text for team information including dynamic teams
    /// </summary>
    private string GetTeamDisplayText(PlayerTeam team)
    {
        // Get the PlayerCore component to access dynamic team info
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore == null) return team.ToString();
        
        string displayText = team.ToString();
        
        // Add dynamic team info if available
        if (!string.IsNullOrEmpty(playerCore.guildId))
        {
            displayText += $" [G:{playerCore.guildId}]";
        }
        if (!string.IsNullOrEmpty(playerCore.partyId))
        {
            displayText += $" [P:{playerCore.partyId}]";
        }
        if (!string.IsNullOrEmpty(playerCore.factionId))
        {
            displayText += $" [F:{playerCore.factionId}]";
        }
        
        return displayText;
    }
}