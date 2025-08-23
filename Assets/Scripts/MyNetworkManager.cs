using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        // ������ ��������� ������� ������ � ��������� ����� �����������
        Transform start = GetStartPosition();
        GameObject player = start != null
            ? Instantiate(playerPrefab, start.position, start.rotation)
            : Instantiate(playerPrefab);

        // ��������� ������ � ����
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}