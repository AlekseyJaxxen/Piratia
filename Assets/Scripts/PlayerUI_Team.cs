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
    public Button changeNameButton;

    private static PlayerInfo tempPlayerInfo = new PlayerInfo();

    private class PlayerInfo
    {
        public string name = "Player";
        public PlayerTeam team = PlayerTeam.Red;
    }

    void Start()
    {
        if (redTeamButton != null)
        {
            redTeamButton.onClick.AddListener(() => OnTeamSelected(PlayerTeam.Red));
        }

        if (blueTeamButton != null)
        {
            blueTeamButton.onClick.AddListener(() => OnTeamSelected(PlayerTeam.Blue));
        }

        if (hostButton != null)
        {
            hostButton.onClick.AddListener(OnHostClicked);
        }

        if (clientButton != null)
        {
            clientButton.onClick.AddListener(OnClientClicked);
        }

        if (changeNameButton != null)
        {
            changeNameButton.onClick.AddListener(OnChangeNameClicked);
        }

        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(true);
        }

        if (nameInputField != null)
        {
            nameInputField.text = tempPlayerInfo.name;
        }

        // Устанавливаем начальное состояние кнопок
        UpdateButtonColors(tempPlayerInfo.team);
    }

    public void OnHostClicked()
    {
        MyNetworkManager myNetworkManager = NetworkManager.singleton.GetComponent<MyNetworkManager>();
        if (myNetworkManager != null)
        {
            myNetworkManager.StartHostButton();
        }
    }

    public void OnClientClicked()
    {
        MyNetworkManager myNetworkManager = NetworkManager.singleton.GetComponent<MyNetworkManager>();
        if (myNetworkManager != null)
        {
            myNetworkManager.StartClientButton();
        }
    }

    private void OnTeamSelected(PlayerTeam selectedTeam)
    {
        // Проверяем, существует ли локальный игрок (т.е. мы в игре)
        PlayerCore localPlayerCore = PlayerCore.localPlayerCoreInstance;
        if (localPlayerCore != null && localPlayerCore.isLocalPlayer)
        {
            // Если да, отправляем команду на сервер
            localPlayerCore.CmdSetPlayerInfo(localPlayerCore.playerName, selectedTeam);
            Debug.Log($"Sent command to change team to: {selectedTeam}");
        }
        else
        {
            // Если нет, обновляем только локальную переменную
            tempPlayerInfo.team = selectedTeam;
            Debug.Log($"Selected team locally: {selectedTeam}");
        }

        UpdateButtonColors(selectedTeam);
    }

    private void OnChangeNameClicked()
    {
        // Просто обновляем локальную переменную
        tempPlayerInfo.name = nameInputField.text;
        Debug.Log($"Name changed locally to: {tempPlayerInfo.name}");
    }

    public static void SendPlayerInfoCommand(PlayerCore playerCoreInstance)
    {
        if (playerCoreInstance != null)
        {
            playerCoreInstance.CmdSetPlayerInfo(tempPlayerInfo.name, tempPlayerInfo.team);
            Debug.Log($"Sent command to set name and team: {tempPlayerInfo.name}, {tempPlayerInfo.team}");
        }
    }

    public static PlayerTeam GetTempPlayerTeam()
    {
        return tempPlayerInfo.team;
    }

    private void UpdateButtonColors(PlayerTeam currentTeam)
    {
        // Можно добавить визуальный эффект, чтобы показывать, какая команда выбрана
        Color selectedColor = Color.yellow;
        Color defaultColor = Color.white;

        if (redTeamButton != null)
            redTeamButton.GetComponent<Image>().color = (currentTeam == PlayerTeam.Red) ? selectedColor : defaultColor;

        if (blueTeamButton != null)
            blueTeamButton.GetComponent<Image>().color = (currentTeam == PlayerTeam.Blue) ? selectedColor : defaultColor;
    }
}