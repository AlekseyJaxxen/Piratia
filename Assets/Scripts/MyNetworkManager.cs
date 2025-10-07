using UnityEngine;
using Mirror;
using System.Collections;

public class MyNetworkManager : NetworkManager
{
    [Header("Player Settings")]
    public GameObject[] playerPrefabs;

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("[MyNetworkManager] Host started");
        // Для хоста нужно обработать спавн игрока отдельно
        StartCoroutine(HandleHostPlayerSpawn());
    }

    private IEnumerator HandleHostPlayerSpawn()
    {
        Debug.Log("[MyNetworkManager] HandleHostPlayerSpawn started");
        
        // Ждем пока сервер и клиент будут готовы
        Debug.Log("[MyNetworkManager] Waiting for server and client to be ready...");
        yield return new WaitUntil(() => NetworkServer.active && NetworkClient.isConnected);
        Debug.Log("[MyNetworkManager] Server and client are ready");
        
        yield return new WaitForSeconds(0.5f); // Увеличиваем задержку для стабильности
        Debug.Log("[MyNetworkManager] Wait completed, proceeding with spawn handling");
        
        // Получаем информацию о игроке из UI
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();
        Debug.Log($"[MyNetworkManager] UI Info retrieved: Name={uiInfo.name}, Team={uiInfo.team}, Prefab={uiInfo.prefabIndex}, Class={uiInfo.characterClass}");
        
        // Ищем уже созданного игрока хоста с несколькими попытками
        GameObject hostPlayer = null;
        int attempts = 0;
        while (hostPlayer == null && attempts < 10)
        {
            Debug.Log($"[MyNetworkManager] Searching for host player, attempt {attempts + 1}/10");
            hostPlayer = FindHostPlayer();
            if (hostPlayer == null)
            {
                Debug.Log($"[MyNetworkManager] Host player not found, attempt {attempts + 1}/10, waiting...");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            else
            {
                Debug.Log($"[MyNetworkManager] Host player found on attempt {attempts + 1}");
            }
        }
        
        if (hostPlayer != null)
        {
            Debug.Log($"[MyNetworkManager] Found existing host player: {hostPlayer.name} at position {hostPlayer.transform.position}");
            
            // Устанавливаем позицию спавна для команды
            Debug.Log($"[MyNetworkManager] Looking for spawn point for team: {uiInfo.team}");
            Transform spawnPoint = GetTeamSpawnPoint(uiInfo.team);
            if (spawnPoint != null)
            {
                Debug.Log($"[MyNetworkManager] Found spawn point at position: {spawnPoint.position}");
                Debug.Log($"[MyNetworkManager] Current host player position: {hostPlayer.transform.position}");
                
                hostPlayer.transform.position = spawnPoint.position;
                Debug.Log($"[MyNetworkManager] Host player moved to position: {hostPlayer.transform.position}");
                
                // Дополнительно принудительно обновляем позицию через NetworkTransformHybrid
                NetworkTransformHybrid networkTransform = hostPlayer.GetComponent<NetworkTransformHybrid>();
                if (networkTransform != null)
                {
                    Debug.Log("[MyNetworkManager] Found NetworkTransformHybrid, syncing position");
                    networkTransform.CmdTeleport(spawnPoint.position, hostPlayer.transform.rotation);
                    Debug.Log($"[MyNetworkManager] Host player position synced via NetworkTransformHybrid to {spawnPoint.position}");
                }
                else
                {
                    Debug.LogWarning("[MyNetworkManager] NetworkTransformHybrid component not found on host player");
                }
            }
            else
            {
                Debug.LogError($"[MyNetworkManager] No valid spawn point found for team {uiInfo.team}!");
            }
            
            // Настраиваем PlayerCore
            PlayerCore playerCore = hostPlayer.GetComponent<PlayerCore>();
            if (playerCore != null)
            {
                Debug.Log($"[MyNetworkManager] Setting PlayerCore: Name={uiInfo.name}, Team={uiInfo.team}");
                playerCore.playerName = uiInfo.name;
                playerCore.team = uiInfo.team;
                Debug.Log($"[MyNetworkManager] Host player settings updated: Name={playerCore.playerName}, Team={playerCore.team}");
            }
            else
            {
                Debug.LogError("[MyNetworkManager] PlayerCore component missing on host player!");
                yield break;
            }
            
            // Настраиваем CharacterStats
            CharacterStats characterStats = hostPlayer.GetComponent<CharacterStats>();
            if (characterStats != null)
            {
                Debug.Log($"[MyNetworkManager] Setting CharacterStats class to: {uiInfo.characterClass}");
                characterStats.characterClass = uiInfo.characterClass;
                Debug.Log($"[MyNetworkManager] Host player character class set to: {characterStats.characterClass}");
            }
            else
            {
                Debug.LogWarning("[MyNetworkManager] CharacterStats component not found on host player");
            }
            
            Debug.Log($"[MyNetworkManager] Host player spawn handling completed successfully. Final position: {hostPlayer.transform.position}");
        }
        else
        {
            Debug.LogError("[MyNetworkManager] Host player not found after 10 attempts!");
        }
    }
    
    private GameObject FindHostPlayer()
    {
        // Ищем игрока с локальным соединением
        foreach (var connection in NetworkServer.connections)
        {
            if (connection.Value != null && connection.Value.identity != null)
            {
                GameObject player = connection.Value.identity.gameObject;
                PlayerCore playerCore = player.GetComponent<PlayerCore>();
                if (playerCore != null && playerCore.isLocalPlayer)
                {
                    Debug.Log($"[MyNetworkManager] Found host player: {player.name} at position {player.transform.position}");
                    return player;
                }
            }
        }
        
        // Альтернативный способ поиска - через NetworkClient
        if (NetworkClient.localPlayer != null)
        {
            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            Debug.Log($"[MyNetworkManager] Found host player via NetworkClient: {localPlayer.name} at position {localPlayer.transform.position}");
            return localPlayer;
        }
        
        Debug.Log("[MyNetworkManager] Host player not found in connections or NetworkClient");
        return null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // ������������ ���������� ��� ��������� �� �������
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
        Debug.Log("[MyNetworkManager] Server started, handler registered for NetworkPlayerInfo");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[MyNetworkManager] Client connected to server. Sending player info...");

        // �������� ���������� � ������ ������ �� ���������� ���������
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();

        // ���������� ��������� �� ������, ���������� ��� ����������� ���������� ��� �������� ������
        NetworkClient.Send(new NetworkPlayerInfo
        {
            playerName = uiInfo.name,
            playerTeam = uiInfo.team,
            playerPrefabIndex = uiInfo.prefabIndex,
            characterClass = uiInfo.characterClass
        });

        // �������� ������� ��� "��������"
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

    // ���������� ��������� NetworkPlayerInfo �� �������
    [Server]
    private void OnReceivePlayerInfo(NetworkConnectionToClient conn, NetworkPlayerInfo info)
    {
        Debug.Log($"[MyNetworkManager] Server received player info: Name: {info.playerName}, Team: {info.playerTeam}, Prefab: {info.playerPrefabIndex}, Class: {info.characterClass}, ConnectionId: {conn.connectionId}");

        // ���� � ������ ��� ���� ������, �������� ���
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
            Debug.LogWarning($"[MyNetworkManager] Player {info.playerName} has no team assigned. Assigning default team: Solo");
            info.playerTeam = PlayerTeam.Solo;
        }

        // ������� ��������� ������ �� �������
        GameObject playerInstance = Instantiate(playerPrefabs[info.playerPrefabIndex]);

        // ������� � ������������� ����� ������ ��� �������
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

        // ����������� ��������� PlayerCore
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

        // ������� ��������� CharacterStats � �������� ������������� �����
        CharacterStats characterStats = playerInstance.GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            // ������������� �����, ������� ����� ��������������� � ���������
            characterStats.characterClass = info.characterClass;

            // ������������� �������� ������ ��� ����������� ������ �� �������.
            // ��� �����������, ��� ���� ������� ���������� ��������, ��� ���
            // ��� ���� SyncVar ����� �� ���������, ���� ����� �� ���������.
            characterStats.LoadClassData();
            characterStats.CalculateDerivedStats();

            Debug.Log($"[MyNetworkManager] Server set and calculated player stats for class: {info.characterClass}");
        }
        else
        {
            Debug.LogError("[MyNetworkManager] CharacterStats component missing on spawned player!");
        }

        // ��������� ������ ��� ����������
        NetworkServer.AddPlayerForConnection(conn, playerInstance);

        // ����������� ������� authority ��� ��� ��������
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

    // ���� ����� ������ �� ������������, ��� ��� OnReceivePlayerInfo ������ ������������ ���������� ������
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("[MyNetworkManager] OnServerAddPlayer called, but we are using OnReceivePlayerInfo handler instead.");
    }

    // Получение точки спавна для команды игрока
    public Transform GetTeamSpawnPoint(PlayerTeam team)
    {
        Debug.Log($"[MyNetworkManager] GetTeamSpawnPoint called for team: {team}");
        
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Debug.Log($"[MyNetworkManager] Found {spawnPoints.Length} spawn points with tag 'SpawnPoint'");
        
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[MyNetworkManager] No spawn points found with tag 'SpawnPoint'!");
            return null;
        }
        
        foreach (GameObject spawnPoint in spawnPoints)
        {
            TeamSpawnPoint teamSpawn = spawnPoint.GetComponent<TeamSpawnPoint>();
            if (teamSpawn != null)
            {
                Debug.Log($"[MyNetworkManager] Spawn point '{spawnPoint.name}' has team: {teamSpawn.team}");
                // Solo players can spawn at any spawn point
                if (team == PlayerTeam.Solo || teamSpawn.team == team)
                {
                    Debug.Log($"[MyNetworkManager] Found matching spawn point for team {team}: {spawnPoint.name} at {spawnPoint.transform.position}");
                    return spawnPoint.transform;
                }
            }
            else
            {
                Debug.LogWarning($"[MyNetworkManager] Spawn point '{spawnPoint.name}' has no TeamSpawnPoint component!");
            }
        }
        
        Debug.LogWarning($"[MyNetworkManager] No spawn point found for team {team}, using fallback");
        if (spawnPoints.Length > 0)
        {
            Transform fallback = spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
            Debug.Log($"[MyNetworkManager] Fallback spawn point: {fallback.name} at {fallback.position}");
            return fallback;
        }
        
        Debug.LogError("[MyNetworkManager] No spawn points available at all!");
        return transform;
    }
}

// ��������� ���������, ������������� �� ������� � �������
public struct NetworkPlayerInfo : NetworkMessage
{
    public string playerName;
    public PlayerTeam playerTeam;
    public int playerPrefabIndex;
    public CharacterClass characterClass;
}