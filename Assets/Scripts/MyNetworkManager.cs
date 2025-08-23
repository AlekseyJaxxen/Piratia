using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // ƒобавьте это дл€ использовани€ Linq

public class MyNetworkManager : NetworkManager
{
    // Ётот метод вызываетс€, когда игрок нажимает "’ост"
    public void StartHostButton()
    {
        StartHost();
    }

    // Ётот метод вызываетс€, когда игрок нажимает " лиент"
    public void StartClientButton()
    {
        StartClient();
    }

    // Ётот метод вызываетс€ на клиенте, когда он успешно подключаетс€ к серверу
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // «апросить создание игрового объекта игрока на сервере
        NetworkClient.AddPlayer();
    }

    // Ётот метод вызываетс€ на сервере, когда клиент запрашивает создание игрока
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 1. Ќаходим игрока, который только что подключилс€, чтобы получить его команду.
        // Ёто требует, чтобы PlayerCore уже существовал на клиенте и передал данные.
        // ѕоскольку вы передаете им€ и команду после создани€, мы не можем получить
        // ее здесь. ћы должны получить ее от какого-либо другого источника, например, из
        // `NetworkConnectionToClient.authenticationData` или другой логики.

        // ƒл€ простоты, давайте получим команду из PlayerUI_Team, где она временно хранитс€.
        PlayerTeam desiredTeam = PlayerUI_Team.GetTempPlayerTeam();

        // 2. Ќайти все точки возрождени€, которые соответствуют нужной команде.
        var teamSpawnPoints = FindObjectsOfType<TeamSpawnPoint>()
            .Where(sp => sp.team == desiredTeam)
            .ToList();

        Transform start = null;

        if (teamSpawnPoints.Count > 0)
        {
            // ¬ыбрать случайную точку возрождени€ из списка
            start = teamSpawnPoints[Random.Range(0, teamSpawnPoints.Count)].transform;
        }
        else
        {
            Debug.LogError($"No spawn points found for team {desiredTeam}! Spawning at default location.");
            start = GetStartPosition();
        }

        // 3. —оздать экземпл€р префаба игрока в нужной точке
        GameObject player = start != null
            ? Instantiate(playerPrefab, start.position, start.rotation)
            : Instantiate(playerPrefab);

        // 4. ƒобавл€ем игрока в сеть
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}