using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // �������� ��� ��� ������������� Linq

public class MyNetworkManager : NetworkManager
{
    // ���� ����� ����������, ����� ����� �������� "����"
    public void StartHostButton()
    {
        StartHost();
    }

    // ���� ����� ����������, ����� ����� �������� "������"
    public void StartClientButton()
    {
        StartClient();
    }

    // ���� ����� ���������� �� �������, ����� �� ������� ������������ � �������
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // ��������� �������� �������� ������� ������ �� �������
        NetworkClient.AddPlayer();
    }

    // ���� ����� ���������� �� �������, ����� ������ ����������� �������� ������
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 1. ������� ������, ������� ������ ��� �����������, ����� �������� ��� �������.
        // ��� �������, ����� PlayerCore ��� ����������� �� ������� � ������� ������.
        // ��������� �� ��������� ��� � ������� ����� ��������, �� �� ����� ��������
        // �� �����. �� ������ �������� �� �� ������-���� ������� ���������, ��������, ��
        // `NetworkConnectionToClient.authenticationData` ��� ������ ������.

        // ��� ��������, ������� ������� ������� �� PlayerUI_Team, ��� ��� �������� ��������.
        PlayerTeam desiredTeam = PlayerUI_Team.GetTempPlayerTeam();

        // 2. ����� ��� ����� �����������, ������� ������������� ������ �������.
        var teamSpawnPoints = FindObjectsOfType<TeamSpawnPoint>()
            .Where(sp => sp.team == desiredTeam)
            .ToList();

        Transform start = null;

        if (teamSpawnPoints.Count > 0)
        {
            // ������� ��������� ����� ����������� �� ������
            start = teamSpawnPoints[Random.Range(0, teamSpawnPoints.Count)].transform;
        }
        else
        {
            Debug.LogError($"No spawn points found for team {desiredTeam}! Spawning at default location.");
            start = GetStartPosition();
        }

        // 3. ������� ��������� ������� ������ � ������ �����
        GameObject player = start != null
            ? Instantiate(playerPrefab, start.position, start.rotation)
            : Instantiate(playerPrefab);

        // 4. ��������� ������ � ����
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}