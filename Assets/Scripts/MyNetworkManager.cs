using UnityEngine;
using Mirror;

public class MyNetworkManager : NetworkManager
{
    public GameObject[] playerPrefabs;

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
        Debug.Log("[MyNetworkManager] Server started, handler registered for NetworkPlayerInfo");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        NetworkServer.UnregisterHandler<NetworkPlayerInfo>();
        Debug.Log("[MyNetworkManager] Server stopped, handler unregistered");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[MyNetworkManager] Client connected to server. Sending player info...");
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();
        NetworkClient.Send(new NetworkPlayerInfo
        {
            playerName = uiInfo.name,
            playerTeam = uiInfo.team,
            playerPrefabIndex = uiInfo.prefabIndex
        });
    }

    [Server]
    private void OnReceivePlayerInfo(NetworkConnectionToClient conn, NetworkPlayerInfo info)
    {
        Debug.Log($"[MyNetworkManager] Server received player info: Name: {info.playerName}, Team: {info.playerTeam}, Prefab: {info.playerPrefabIndex}, ConnectionId: {conn.connectionId}");

        // ѕровер€ем, нет ли уже игрока дл€ этого соединени€
        if (conn.identity != null)
        {
            Debug.LogWarning($"[MyNetworkManager] Player already exists for connection {conn.connectionId}. Replacing player.");
            NetworkServer.ReplacePlayerForConnection(conn, null);
        }

        if (info.playerPrefabIndex < 0 || info.playerPrefabIndex >= playerPrefabs.Length)
        {
            Debug.LogError($"[MyNetworkManager] Invalid prefab index: {info.playerPrefabIndex}");
            return;
        }

        // ѕровер€ем, выбрана ли команда
        if (info.playerTeam == PlayerTeam.None)
        {
            Debug.LogWarning($"[MyNetworkManager] Player {info.playerName} has no team assigned. Assigning default team: Red");
            info.playerTeam = PlayerTeam.Red;
        }

        GameObject playerInstance = Instantiate(playerPrefabs[info.playerPrefabIndex]);
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

        PlayerCore playerCore = playerInstance.GetComponent<PlayerCore>();
        if (playerCore != null)
        {
            // ѕр€мое присваивание SyncVar на сервере
            playerCore.playerName = info.playerName;
            playerCore.team = info.playerTeam;
            Debug.Log($"[MyNetworkManager] Set player info: Name={info.playerName}, Team={info.playerTeam}");
        }
        else
        {
            Debug.LogError("[MyNetworkManager] PlayerCore component missing on spawned player!");
            return;
        }

        // Ќазначаем игрока дл€ соединени€ с авторизацией
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
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

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // ќтключаем базовую логику спавна, чтобы избежать дублировани€
        Debug.Log("[MyNetworkManager] OnServerAddPlayer called, handled by OnReceivePlayerInfo");
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
        Debug.LogWarning("[MyNetworkManager] No spawn points found for team " + team);
        return transform;
    }
}