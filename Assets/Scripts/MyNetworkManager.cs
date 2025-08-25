using Mirror;
using UnityEngine;
using System.Linq;

public class MyNetworkManager : NetworkManager
{
    public GameObject[] playerPrefabs;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // ������������ ��� ��������� ���������� ��� ���������
        NetworkServer.RegisterHandler<NetworkPlayerInfo>(OnReceivePlayerInfo);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // ��������������� ����������, ����� �������� ������ ������
        NetworkServer.UnregisterHandler<NetworkPlayerInfo>();
    }

    // ���� ����� ���������� �� �������, ����� �� ������������ � �������
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("������ ����������� � �������. ���������� ���������� �� ������...");

        // �������� ���������� �� UI
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();

        // ������� � ���������� ��������� �� ������
        NetworkClient.Send(new NetworkPlayerInfo
        {
            playerName = uiInfo.name,
            playerTeam = uiInfo.team,
            playerPrefabIndex = uiInfo.prefabIndex
        });
    }

    // ���� ����� ���������� �� �������, ����� �� �������� ��������� �� �������
    [Server]
    private void OnReceivePlayerInfo(NetworkConnectionToClient conn, NetworkPlayerInfo info)
    {
        Debug.Log($"������ ������� ���������� �� �������: ���: {info.playerName}, �������: {info.playerTeam}, ������: {info.playerPrefabIndex}");

        // ���������, ��� ������ ������� ���������
        if (info.playerPrefabIndex < 0 || info.playerPrefabIndex >= playerPrefabs.Length)
        {
            Debug.LogError($"������� ������������ ������ �������: {info.playerPrefabIndex}.");
            return;
        }

        // 1. ������� ��������� �������
        GameObject playerInstance = Instantiate(playerPrefabs[info.playerPrefabIndex]);

        // 2. ����������� ���������, ��� � ������
        PlayerCore playerCore = playerInstance.GetComponent<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.team = info.playerTeam;
            playerCore.playerName = info.playerName;
        }

        // 3. ��������� ��������� ��������� � ����
        NetworkServer.AddPlayerForConnection(conn, playerInstance);

        Debug.Log($"����� {info.playerName} ������� ��������� � �������� {playerInstance.name}.");
    }

    // ���� ����� ������ �� ������������, ��� ��� ��� ������ ������ ��������� � OnReceivePlayerInfo
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // �� ����������� ���� ����� ��� ������, ��� ��� � ��� ��� ���������� �� ������� �����.
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