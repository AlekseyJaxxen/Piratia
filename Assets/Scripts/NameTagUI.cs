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
        if (teamText != null) teamText.text = team.ToString();
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
            color = IsAlly(team, localTeam) ? Color.green : Color.red;
        }
        if (nameText != null) nameText.color = color;
        if (teamText != null) teamText.color = color;
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
}