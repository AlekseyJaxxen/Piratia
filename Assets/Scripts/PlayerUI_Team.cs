using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerUI_Team : MonoBehaviour
{
    public GameObject teamSelectionPanel;
    public Button redTeamButton;
    public Button blueTeamButton;
    public Button playerPrefab1Button;
    public Button playerPrefab2Button;
    public TMP_InputField nameInputField;
    public Button hostButton;
    public Button clientButton;
    public Button changeNameButton;
    public Button disconnectButton;
    public Button returnToMainMenuButton;

    private static PlayerInfo tempPlayerInfo = new PlayerInfo();

    public class PlayerInfo
    {
        public string name = "Player";
        public PlayerTeam team = PlayerTeam.Red;
        public int prefabIndex = 0;
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

        if (playerPrefab1Button != null)
        {
            playerPrefab1Button.onClick.AddListener(() => OnPrefabSelected(0));
        }

        if (playerPrefab2Button != null)
        {
            playerPrefab2Button.onClick.AddListener(() => OnPrefabSelected(1));
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

        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(OnDisconnectClicked);
        }

        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        }

        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(true);
        }

        if (nameInputField != null)
        {
            nameInputField.text = tempPlayerInfo.name;
        }

        UpdateButtonColors(tempPlayerInfo.team);
    }

    public void OnHostClicked()
    {
        Debug.Log("Кнопка Host нажата.");
        MyNetworkManager myNetworkManager = NetworkManager.singleton.GetComponent<MyNetworkManager>();
        if (myNetworkManager != null)
        {
            myNetworkManager.StartHost();
        }
    }

    public void OnClientClicked()
    {
        Debug.Log("Кнопка Client нажата.");
        MyNetworkManager myNetworkManager = NetworkManager.singleton.GetComponent<MyNetworkManager>();
        if (myNetworkManager != null)
        {
            myNetworkManager.StartClient();
        }
    }

    private void OnTeamSelected(PlayerTeam selectedTeam)
    {
        if (NetworkClient.isConnected && PlayerCore.localPlayerCoreInstance != null)
        {
            PlayerCore.localPlayerCoreInstance.CmdChangeTeam(selectedTeam);
        }
        else
        {
            tempPlayerInfo.team = selectedTeam;
            UpdateButtonColors(selectedTeam);
        }
        Debug.Log($"Выбрана команда локально: {selectedTeam}");
    }

    private void OnPrefabSelected(int index)
    {
        tempPlayerInfo.prefabIndex = index;
        Debug.Log($"Выбран префаб игрока локально: {index}");
    }

    private void OnChangeNameClicked()
    {
        string newName = nameInputField.text;
        if (NetworkClient.isConnected && PlayerCore.localPlayerCoreInstance != null)
        {
            PlayerCore.localPlayerCoreInstance.CmdChangeName(newName);
        }
        else
        {
            tempPlayerInfo.name = newName;
        }
        Debug.Log($"Имя изменено локально на: {newName}");
    }

    // Метод для дисконнекта
    public void OnDisconnectClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
            Debug.Log("Хост отключился.");
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
            Debug.Log("Клиент отключился.");
        }
    }

    // Метод для возвращения в главное меню
    public void OnReturnToMainMenuClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
            Debug.Log("Хост отключился и возвращается в главное меню.");
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
            Debug.Log("Клиент отключился и возвращается в главное меню.");
        }
    }

    public static PlayerTeam GetTempPlayerTeam()
    {
        return tempPlayerInfo.team;
    }

    public static PlayerInfo GetTempPlayerInfo()
    {
        return tempPlayerInfo;
    }

    private void UpdateButtonColors(PlayerTeam currentTeam)
    {
        Color selectedColor = Color.yellow;
        Color defaultColor = Color.white;

        if (redTeamButton != null)
            redTeamButton.GetComponent<Image>().color = (currentTeam == PlayerTeam.Red) ? selectedColor : defaultColor;

        if (blueTeamButton != null)
            blueTeamButton.GetComponent<Image>().color = (currentTeam == PlayerTeam.Blue) ? selectedColor : defaultColor;
    }
}