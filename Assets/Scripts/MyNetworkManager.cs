using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    // Этот метод вызывается, когда игрок нажимает "Хост"
    public void StartHostButton()
    {
        StartHost();
    }

    // Этот метод вызывается, когда игрок нажимает "Клиент"
    public void StartClientButton()
    {
        StartClient();
    }

    // Этот метод вызывается на клиенте, когда он успешно подключается к серверу
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // Запросить создание игрового объекта игрока на сервере
        NetworkClient.AddPlayer();
    }

    // Этот метод вызывается на сервере, когда клиент запрашивает создание игрока
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Создаём экземпляр префаба игрока в случайной точке возрождения
        Transform start = GetStartPosition();
        GameObject player = start != null
            ? Instantiate(playerPrefab, start.position, start.rotation)
            : Instantiate(playerPrefab);

        // Добавляем игрока в сеть
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}