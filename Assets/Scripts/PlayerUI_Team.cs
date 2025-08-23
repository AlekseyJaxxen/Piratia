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

    // Этот экземпляр будет хранить выбранные данные до подключения
    private static PlayerInfo tempPlayerInfo = new PlayerInfo();

    // Класс для временного хранения данных игрока
    private class PlayerInfo
    {
        public string name = "Player";
        public PlayerTeam team = PlayerTeam.Red;
    }

    void Start()
    {
        // Привязываем кнопки
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

        // Показываем панель при запуске
        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(true);
        }

        // Устанавливаем начальные значения
        OnNameChanged(nameInputField.text);
        OnTeamSelected(tempPlayerInfo.team);
    }

    public void OnHostClicked()
    {
        // Запустить хост, используя наш кастомный Network Manager
        NetworkManager.singleton.GetComponent<MyNetworkManager>().StartHostButton();
        teamSelectionPanel.SetActive(false); // Скрыть меню после старта игры
    }

    public void OnClientClicked()
    {
        // Запустить клиент
        NetworkManager.singleton.GetComponent<MyNetworkManager>().StartClientButton();
        teamSelectionPanel.SetActive(false); // Скрыть меню после старта игры
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

    // Этот статический метод будет вызываться из PlayerCore после создания игрока
    public static void SendPlayerInfoCommand(PlayerCore playerCoreInstance)
    {
        if (playerCoreInstance != null)
        {
            // Отправляем команду на сервер с сохраненными данными
            playerCoreInstance.CmdSetPlayerInfo(tempPlayerInfo.name, tempPlayerInfo.team);
            Debug.Log($"Sent command to set name and team: {tempPlayerInfo.name}, {tempPlayerInfo.team}");
        }
    }

    public static PlayerTeam GetTempPlayerTeam()
    {
        return tempPlayerInfo.team;
    }
}