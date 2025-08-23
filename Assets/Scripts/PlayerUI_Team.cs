using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerUI_Team : MonoBehaviour
{
    public GameObject teamSelectionPanel;
    public Button redTeamButton;
    public Button blueTeamButton;
    public TMP_InputField nameInputField;
    public Button hostButton;
    public Button clientButton;

    // ���� ��������� ����� ������� ��������� ������ �� �����������
    private static PlayerInfo tempPlayerInfo = new PlayerInfo();

    // ����� ��� ���������� �������� ������ ������
    private class PlayerInfo
    {
        public string name = "Player";
        public PlayerTeam team = PlayerTeam.Red;
    }

    void Start()
    {
        // ����������� ������
        if (redTeamButton != null)
        {
            redTeamButton.onClick.AddListener(() => OnTeamSelected(PlayerTeam.Red));
        }

        if (blueTeamButton != null)
        {
            blueTeamButton.onClick.AddListener(() => OnTeamSelected(PlayerTeam.Blue));
        }

        if (nameInputField != null)
        {
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        if (hostButton != null)
        {
            hostButton.onClick.AddListener(OnHostClicked);
        }

        if (clientButton != null)
        {
            clientButton.onClick.AddListener(OnClientClicked);
        }

        // ���������� ������ ��� �������
        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(true);
        }

        // ������������� ��������� ��������
        OnNameChanged(nameInputField.text);
        OnTeamSelected(tempPlayerInfo.team);
    }

    public void OnHostClicked()
    {
        // ��������� ����, ��������� ��� ��������� Network Manager
        NetworkManager.singleton.GetComponent<MyNetworkManager>().StartHostButton();
        teamSelectionPanel.SetActive(false); // ������ ���� ����� ������ ����
    }

    public void OnClientClicked()
    {
        // ��������� ������
        NetworkManager.singleton.GetComponent<MyNetworkManager>().StartClientButton();
        teamSelectionPanel.SetActive(false); // ������ ���� ����� ������ ����
    }

    private void OnTeamSelected(PlayerTeam selectedTeam)
    {
        tempPlayerInfo.team = selectedTeam;
        Debug.Log($"Selected Team: {selectedTeam}");
    }

    private void OnNameChanged(string newName)
    {
        tempPlayerInfo.name = newName;
    }

    // ���� ����������� ����� ����� ���������� �� PlayerCore ����� �������� ������
    public static void SendPlayerInfoCommand(PlayerCore playerCoreInstance)
    {
        if (playerCoreInstance != null)
        {
            // ���������� ������� �� ������ � ������������ �������
            playerCoreInstance.CmdSetPlayerInfo(tempPlayerInfo.name, tempPlayerInfo.team);
            Debug.Log($"Sent command to set name and team: {tempPlayerInfo.name}, {tempPlayerInfo.team}");
        }
    }

    public static PlayerTeam GetTempPlayerTeam()
    {
        return tempPlayerInfo.team;
    }
}