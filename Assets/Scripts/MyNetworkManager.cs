using UnityEngine;
using Mirror;
using System.Collections;

public class MyNetworkManager : NetworkManager
{
    [Header("Player Settings")]
    public GameObject[] playerPrefabs;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Регистрируем обработчик для сообщения от клиента
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
        Debug.Log("[MyNetworkManager] Server started, handler registered for NetworkPlayerInfo");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // Отменяем регистрацию обработчика при остановке сервера
        NetworkServer.UnregisterHandler<NetworkPlayerInfo>();
        Debug.Log("[MyNetworkManager] Server stopped, handler unregistered");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[MyNetworkManager] Client connected to server. Sending player info...");

        // Получаем информацию о выборе игрока из временного хранилища
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();

        // Отправляем сообщение на сервер, содержащее всю необходимую информацию для создания игрока
        NetworkClient.Send(new NetworkPlayerInfo
        {
            playerName = uiInfo.name,
            playerTeam = uiInfo.team,
            playerPrefabIndex = uiInfo.prefabIndex,
            characterClass = uiInfo.characterClass
        });

        // Отмечаем клиента как "готового"
        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
            Debug.Log("[MyNetworkManager] Client set to Ready");
        }
        else
        {
            Debug.Log("[MyNetworkManager] Client already ready");
        }
    }

    // Обработчик сообщения NetworkPlayerInfo на сервере
    [Server]
    private void OnReceivePlayerInfo(NetworkConnectionToClient conn, NetworkPlayerInfo info)
    {
        Debug.Log($"[MyNetworkManager] Server received player info: Name: {info.playerName}, Team: {info.playerTeam}, Prefab: {info.playerPrefabIndex}, Class: {info.characterClass}, ConnectionId: {conn.connectionId}");

        // Если у игрока уже есть объект, заменяем его
        if (conn.identity != null)
        {
            Debug.LogWarning($"[MyNetworkManager] Player already exists for connection {conn.connectionId}. Replacing player.");
            NetworkServer.ReplacePlayerForConnection(conn, null, new ReplacePlayerOptions());
        }

        if (info.playerPrefabIndex < 0 || info.playerPrefabIndex >= playerPrefabs.Length)
        {
            Debug.LogError($"[MyNetworkManager] Invalid prefab index: {info.playerPrefabIndex}");
            return;
        }

        if (info.playerTeam == PlayerTeam.None)
        {
            Debug.LogWarning($"[MyNetworkManager] Player {info.playerName} has no team assigned. Assigning default team: Red");
            info.playerTeam = PlayerTeam.Red;
        }

        // Создаем экземпляр игрока на сервере
        GameObject playerInstance = Instantiate(playerPrefabs[info.playerPrefabIndex]);

        // Находим и устанавливаем точку спавна для команды
        Transform spawnPoint = GetTeamSpawnPoint(info.playerTeam);
        if (spawnPoint != null)
        {
            playerInstance.transform.position = spawnPoint.position;
            Debug.Log($"[MyNetworkManager] Player {info.playerName} spawned at position: {spawnPoint.position}");
        }
        else
        {
            Debug.LogWarning("[MyNetworkManager] No valid spawn point found, using default position");
        }

        // Настраиваем компонент PlayerCore
        PlayerCore playerCore = playerInstance.GetComponent<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.playerName = info.playerName;
            playerCore.team = info.playerTeam;
        }
        else
        {
            Debug.LogError("[MyNetworkManager] PlayerCore component missing on spawned player!");
            return;
        }

        // Находим компонент CharacterStats и напрямую устанавливаем класс
        CharacterStats characterStats = playerInstance.GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            // Устанавливаем класс, который будет синхронизирован с клиентами
            characterStats.characterClass = info.characterClass;

            // Принудительно вызываем методы для перерасчета статов на сервере.
            // Это гарантирует, что хост получит правильные значения, так как
            // для него SyncVar может не сработать, если класс не изменился.
            characterStats.LoadClassData();
            characterStats.CalculateDerivedStats();

            Debug.Log($"[MyNetworkManager] Server set and calculated player stats for class: {info.characterClass}");
        }
        else
        {
            Debug.LogError("[MyNetworkManager] CharacterStats component missing on spawned player!");
        }

        // Добавляем игрока для соединения
        NetworkServer.AddPlayerForConnection(conn, playerInstance);

        // Присваиваем клиенту authority над его объектом
        NetworkIdentity identity = playerInstance.GetComponent<NetworkIdentity>();
        if (identity != null)
        {
            identity.AssignClientAuthority(conn);
            Debug.Log($"[MyNetworkManager] Assigned client authority for player {info.playerName}. isOwned={identity.isOwned}");
        }
        else
        {
            Debug.LogError("[MyNetworkManager] NetworkIdentity component missing on spawned player!");
        }

        Debug.Log($"[MyNetworkManager] Player {info.playerName} successfully spawned with prefab {playerInstance.name}. isOwned={identity.isOwned}");
    }

    // Этот метод больше не используется, так как OnReceivePlayerInfo теперь обрабатывает добавление игрока
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("[MyNetworkManager] OnServerAddPlayer called, but we are using OnReceivePlayerInfo handler instead.");
    }

    // Вспомогательный метод для поиска точки спавна команды
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
        Debug.LogWarning("[MyNetworkManager] No spawn points found for team " + team);
        return transform;
    }
}

// Структура сообщения, отправляемого от клиента к серверу
public struct NetworkPlayerInfo : NetworkMessage
{
    public string playerName;
    public PlayerTeam playerTeam;
    public int playerPrefabIndex;
    public CharacterClass characterClass;
}