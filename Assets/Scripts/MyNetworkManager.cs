using Mirror;
using UnityEngine;
using System.Linq;

public class MyNetworkManager : NetworkManager
{
    public GameObject[] playerPrefabs;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Регистрируем наш кастомный обработчик для сообщения
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // Разрегистрируем обработчик, чтобы избежать утечек памяти
        NetworkServer.UnregisterHandler<NetworkPlayerInfo>();
    }

    // Этот метод вызывается на клиенте, когда он подключается к серверу
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Клиент подключился к серверу. Отправляем информацию об игроке...");

        // Получаем информацию из UI
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();

        // Создаем и отправляем сообщение на сервер
        NetworkClient.Send(new NetworkPlayerInfo
        {
            playerName = uiInfo.name,
            playerTeam = uiInfo.team,
            playerPrefabIndex = uiInfo.prefabIndex
        });
    }

    // Этот метод вызывается на сервере, когда он получает сообщение от клиента
    [Server]
    private void OnReceivePlayerInfo(NetworkConnectionToClient conn, NetworkPlayerInfo info)
    {
        Debug.Log($"Сервер получил информацию от клиента: Имя: {info.playerName}, Команда: {info.playerTeam}, Префаб: {info.playerPrefabIndex}");

        // Проверяем, что индекс префаба корректен
        if (info.playerPrefabIndex < 0 || info.playerPrefabIndex >= playerPrefabs.Length)
        {
            Debug.LogError($"Получен некорректный индекс префаба: {info.playerPrefabIndex}.");
            return;
        }

        // 1. Создаем экземпляр префаба
        GameObject playerInstance = Instantiate(playerPrefabs[info.playerPrefabIndex]);

        // 2. Настраиваем экземпляр, как и раньше
        PlayerCore playerCore = playerInstance.GetComponent<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.team = info.playerTeam;
            playerCore.playerName = info.playerName;
        }

        // 3. Добавляем созданный экземпляр в сеть
        NetworkServer.AddPlayerForConnection(conn, playerInstance);

        Debug.Log($"Игрок {info.playerName} успешно заспавнен с префабом {playerInstance.name}.");
    }

    // Этот метод теперь не используется, так как вся логика спавна находится в OnReceivePlayerInfo
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Не используйте этот метод для спавна, так как у вас нет информации от клиента здесь.
    }

    public Transform GetTeamSpawnPoint(PlayerTeam team)
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        foreach (GameObject spawnPoint in spawnPoints)
        {
            TeamSpawnPoint teamSpawn = spawnPoint.GetComponent<TeamSpawnPoint>();
            if (teamSpawn != null && teamSpawn.team == team)
            {
                return spawnPoint.transform;
            }
        }

        if (spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
        }

        return null;
    }
}