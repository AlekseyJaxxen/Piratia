using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections;

public class PlayerUI_Team : MonoBehaviour
{
    public GameObject teamSelectionPanel;
    public Button redTeamButton;
    public Button blueTeamButton;
    public Button playerPrefab1Button;
    public Button playerPrefab2Button;
    public Button warriorButton;
    public Button mageButton;
    public Button archerButton;
    public Button tankButton; // Добавляем кнопку для Tank
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
        public CharacterClass characterClass = CharacterClass.Warrior;
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
        if (warriorButton != null)
        {
            warriorButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Warrior));
        }
        if (mageButton != null)
        {
            mageButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Mage));
        }
        if (archerButton != null)
        {
            archerButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Archer));
        }
        if (tankButton != null)
        {
            tankButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Tank));
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
        UpdateClassButtonColors(tempPlayerInfo.characterClass);
        Debug.Log($"[PlayerUI_Team] Initialized with tempPlayerInfo: Name={tempPlayerInfo.name}, Team={tempPlayerInfo.team}, PrefabIndex={tempPlayerInfo.prefabIndex}, Class={tempPlayerInfo.characterClass}");
    }

    private void OnClassSelected(CharacterClass selectedClass)
    {
        tempPlayerInfo.characterClass = selectedClass;
        UpdateClassButtonColors(selectedClass);
        Debug.Log($"[PlayerUI_Team] Выбран класс локально: {selectedClass}");
        if (NetworkClient.isConnected && PlayerCore.localPlayerCoreInstance != null)
        {
            PlayerCore.localPlayerCoreInstance.CmdSetClass(selectedClass);
            Debug.Log($"[PlayerUI_Team] Sent CmdSetClass: {selectedClass}");
        }
    }

    public void OnHostClicked()
    {
        OnChangeNameClicked();
        Debug.Log("Кнопка Host нажата.");
        MyNetworkManager myNetworkManager = NetworkManager.singleton.GetComponent<MyNetworkManager>();
        if (myNetworkManager != null)
        {
            myNetworkManager.StartHost();
            teamSelectionPanel.SetActive(false);
            StartCoroutine(SendInitialPlayerInfoForHost());
        }
    }

    public void OnClientClicked()
    {
        OnChangeNameClicked();
        Debug.Log("Кнопка Client нажата.");
        MyNetworkManager myNetworkManager = NetworkManager.singleton.GetComponent<MyNetworkManager>();
        if (myNetworkManager != null)
        {
            myNetworkManager.StartClient();
            teamSelectionPanel.SetActive(false);
            StartCoroutine(SendInitialPlayerInfoForClient());
        }
    }

    private IEnumerator SendInitialPlayerInfoForHost()
    {
        yield return new WaitUntil(() => NetworkServer.active && PlayerCore.localPlayerCoreInstance != null);
        yield return new WaitForSeconds(0.1f); // Небольшая задержка для синхронизации
        OnTeamSelected(tempPlayerInfo.team);
        PlayerCore.localPlayerCoreInstance.CmdSetClass(tempPlayerInfo.characterClass);
        Debug.Log($"[PlayerUI_Team] Sent initial player info for host: Name={tempPlayerInfo.name}, Team={tempPlayerInfo.team}, Class={tempPlayerInfo.characterClass}");
    }

    private IEnumerator SendInitialPlayerInfoForClient()
    {
        yield return new WaitUntil(() => NetworkClient.isConnected && PlayerCore.localPlayerCoreInstance != null);
        yield return new WaitForSeconds(0.1f); // Небольшая задержка для синхронизации
        PlayerCore.localPlayerCoreInstance.CmdChangeTeam(tempPlayerInfo.team);
        PlayerCore.localPlayerCoreInstance.CmdChangeName(tempPlayerInfo.name);
        PlayerCore.localPlayerCoreInstance.CmdSetClass(tempPlayerInfo.characterClass);
        Debug.Log($"[PlayerUI_Team] Sent initial player info for client: Name={tempPlayerInfo.name}, Team={tempPlayerInfo.team}, Class={tempPlayerInfo.characterClass}");
    }

    private void OnTeamSelected(PlayerTeam selectedTeam)
    {
        tempPlayerInfo.team = selectedTeam;
        UpdateButtonColors(selectedTeam);
        Debug.Log($"Выбрана команда локально: {selectedTeam}");
        if (NetworkClient.isConnected && PlayerCore.localPlayerCoreInstance != null)
        {
            PlayerCore.localPlayerCoreInstance.CmdChangeTeam(selectedTeam);
            PlayerCore.localPlayerCoreInstance.CmdSetClass(tempPlayerInfo.characterClass);
        }
        else if (NetworkServer.active)
        {
            GameObject playerInstance = NetworkServer.localConnection?.identity?.gameObject;
            if (playerInstance != null)
            {
                PlayerCore playerCore = playerInstance.GetComponent<PlayerCore>();
                if (playerCore != null)
                {
                    playerCore.playerName = tempPlayerInfo.name;
                    playerCore.team = selectedTeam;
                    playerCore.CmdSetClass(tempPlayerInfo.characterClass);
                    Debug.Log($"[PlayerUI_Team] Set host player info: Name={tempPlayerInfo.name}, Team={selectedTeam}, Class={tempPlayerInfo.characterClass}");
                }
            }
        }
    }

    private void OnPrefabSelected(int index)
    {
        tempPlayerInfo.prefabIndex = index;
        Debug.Log($"Выбран префаб игрока локально: {index}");
    }

    private void OnChangeNameClicked()
    {
        string newName = nameInputField.text;
        tempPlayerInfo.name = newName;
        Debug.Log($"Имя изменено локально на: {newName}");
        if (NetworkClient.isConnected && PlayerCore.localPlayerCoreInstance != null)
        {
            PlayerCore.localPlayerCoreInstance.CmdChangeName(newName);
            PlayerCore.localPlayerCoreInstance.CmdSetClass(tempPlayerInfo.characterClass);
        }
        else if (NetworkServer.active)
        {
            GameObject playerInstance = NetworkServer.localConnection?.identity?.gameObject;
            if (playerInstance != null)
            {
                PlayerCore playerCore = playerInstance.GetComponent<PlayerCore>();
                if (playerCore != null)
                {
                    playerCore.playerName = newName;
                    playerCore.team = tempPlayerInfo.team;
                    playerCore.CmdSetClass(tempPlayerInfo.characterClass);
                    Debug.Log($"[PlayerUI_Team] Set host player info: Name={newName}, Team={tempPlayerInfo.team}, Class={tempPlayerInfo.characterClass}");
                }
            }
        }
    }

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

    private void UpdateClassButtonColors(CharacterClass currentClass)
    {
        Color selectedColor = Color.yellow;
        Color defaultColor = Color.white;
        if (warriorButton != null)
            warriorButton.GetComponent<Image>().color = (currentClass == CharacterClass.Warrior) ? selectedColor : defaultColor;
        if (mageButton != null)
            mageButton.GetComponent<Image>().color = (currentClass == CharacterClass.Mage) ? selectedColor : defaultColor;
        if (archerButton != null)
            archerButton.GetComponent<Image>().color = (currentClass == CharacterClass.Archer) ? selectedColor : defaultColor;
        if (tankButton != null)
            tankButton.GetComponent<Image>().color = (currentClass == CharacterClass.Tank) ? selectedColor : defaultColor;
    }
}