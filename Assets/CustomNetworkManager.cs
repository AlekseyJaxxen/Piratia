using Mirror;
using UnityEngine;
using System.Collections;

public class CustomNetworkManager : NetworkManager
{
    [Header("Server Settings")]
    public int maxPlayers = 4;
    public float playerSpawnInterval = 1f;

    // ѕравильна€ сигнатура метода дл€ текущей версии Mirror
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        if (numPlayers >= maxPlayers)
        {
            conn.Disconnect();
            Debug.Log($"Server full, rejected connection: {conn.connectionId}");
            return;
        }

        Debug.Log($"Client connected: {conn.connectionId}");
    }

    // ѕравильна€ сигнатура метода дл€ текущей версии Mirror
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        StartCoroutine(SpawnPlayerWithDelay(conn));
    }

    private IEnumerator SpawnPlayerWithDelay(NetworkConnectionToClient conn)
    {
        yield return new WaitForSeconds(playerSpawnInterval);

        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"Player spawned for connection: {conn.connectionId}");
    }

    // ѕравильна€ сигнатура метода дл€ текущей версии Mirror
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log($"Client disconnected: {conn.connectionId}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server stopped");
    }
}