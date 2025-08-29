using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections;

public class MyNetworkManager : NetworkManager
{
    [Header("Player Settings")]
    public GameObject[] playerPrefabs;

    [Header("Monster Settings")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform[] monsterSpawnPoints;
    [SerializeField] private int maxMonsters = 5;
    [SerializeField] private float monsterSpawnDelay = 5f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
        Debug.Log("[MyNetworkManager] Server started, handler registered for NetworkPlayerInfo");

        // Запускаем спавн монстров
        if (monsterPrefab != null)
        {
            StartCoroutine(SpawnMonsters());
        }
        else
        {
            Debug.LogError("[MyNetworkManager] Monster prefab not assigned!");
        }
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
            playerCore.playerName = info.playerName;
            playerCore.team = info.playerTeam;
            Debug.Log($"[MyNetworkManager] Set player info: Name={info.playerName}, Team={info.playerTeam}");
        }
        else
        {
            Debug.LogError("[MyNetworkManager] PlayerCore component missing on spawned player!");
            return;
        }
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

    [Server]
    private IEnumerator SpawnMonsters()
    {
        while (true)
        {
            int currentMonsterCount = FindObjectsOfType<Monster>().Length;
            if (currentMonsterCount < maxMonsters)
            {
                SpawnMonster();
            }
            yield return new WaitForSeconds(monsterSpawnDelay);
        }
    }

    [Server]
    private void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[MyNetworkManager] Monster prefab not assigned!");
            return;
        }

        Transform spawnPoint = GetMonsterSpawnPoint();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;

        // Проверяем, находится ли позиция спавна на NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
            GameObject monsterInstance = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
            Monster monster = monsterInstance.GetComponent<Monster>();
            if (monster != null)
            {
                monster.monsterName = $"Monster_{Random.Range(1000, 9999)}";
                monster.maxHealth = 1000; // Устанавливаем здоровье
                monster.currentHealth = monster.maxHealth;
                Debug.Log($"[MyNetworkManager] Spawning monster {monster.monsterName} at {spawnPosition}");
            }
            else
            {
                Debug.LogError("[MyNetworkManager] Monster component missing on spawned monster!");
            }
            NetworkServer.Spawn(monsterInstance);
            Debug.Log($"[MyNetworkManager] Monster spawned at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning($"[MyNetworkManager] Spawn point {spawnPosition} is not on NavMesh. Monster not spawned.");
        }
    }

    [Server]
    private Transform GetMonsterSpawnPoint()
    {
        if (monsterSpawnPoints != null && monsterSpawnPoints.Length > 0)
        {
            return monsterSpawnPoints[Random.Range(0, monsterSpawnPoints.Length)];
        }
        Debug.LogWarning("[MyNetworkManager] No monster spawn points assigned, using default position");
        return transform;
    }
}