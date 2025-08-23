using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class MyNetworkManager : NetworkManager
{
    // Этот метод вызывается, когда игрок нажимает "Хост"
    public void StartHostButton()
    {
        StartHost();
    }

    // Этот метод вызывается, когда игрок нажимает "Клиент"
    public void StartClientButton()
    {
        StartClient();
    }

    // Этот метод вызывается на клиенте, когда он успешно подключается к серверу
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // Запросить создание игрового объекта игрока на сервере
        NetworkClient.AddPlayer();
    }

    // Этот метод вызывается на сервере, когда клиент запрашивает создание игрока
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 1. Находим игрока, который только что подключился, чтобы получить его команду.
        PlayerTeam desiredTeam = PlayerUI_Team.GetTempPlayerTeam();

        // 2. Найти все точки возрождения, которые соответствуют нужной команде.
        var teamSpawnPoints = FindObjectsOfType<TeamSpawnPoint>()
            .Where(sp => sp.team == desiredTeam)
            .ToList();

        Transform start = null;

        if (teamSpawnPoints.Count > 0)
        {
            // Выбрать случайную точку возрождения из списка
            start = teamSpawnPoints[Random.Range(0, teamSpawnPoints.Count)].transform;
        }
        else
        {
            Debug.LogError($"No spawn points found for team {desiredTeam}! Spawning at default location.");
            start = GetStartPosition();
        }

        // 3. Создать экземпляр префаба игрока в нужной точке
        GameObject player = start != null
            ? Instantiate(playerPrefab, start.position, start.rotation)
            : Instantiate(playerPrefab);

        // ИСПРАВЛЕНИЕ: Устанавливаем команду игрока сразу после создания объекта,
        // чтобы она была синхронизирована с самого начала.
        PlayerCore playerCore = player.GetComponent<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.team = desiredTeam;
        }

        // 4. Добавляем игрока в сеть
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public Transform GetTeamSpawnPoint(PlayerTeam desiredTeam)
    {
        var teamSpawnPoints = FindObjectsOfType<TeamSpawnPoint>()
            .Where(sp => sp.team == desiredTeam)
            .ToList();

        if (teamSpawnPoints.Count > 0)
        {
            return teamSpawnPoints[Random.Range(0, teamSpawnPoints.Count)].transform;
        }
        return null;
    }
}