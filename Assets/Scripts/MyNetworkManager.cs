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
        // ������������ ���������� ��� ��������� �� �������
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
        Debug.Log("[MyNetworkManager] Server started, handler registered for NetworkPlayerInfo");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // �������� ����������� ����������� ��� ��������� �������
        NetworkServer.UnregisterHandler<NetworkPlayerInfo>();
        Debug.Log("[MyNetworkManager] Server stopped, handler unregistered");
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
            Debug.LogWarning($"[MyNetworkManager] Player {info.playerName} has no team assigned. Assigning default team: Red");
            info.playerTeam = PlayerTeam.Red;
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

    // ��������������� ����� ��� ������ ����� ������ �������
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

// ��������� ���������, ������������� �� ������� � �������
public struct NetworkPlayerInfo : NetworkMessage
{
    public string playerName;
    public PlayerTeam playerTeam;
    public int playerPrefabIndex;
    public CharacterClass characterClass;
}