using UnityEngine;
using Mirror;

public class ConsoleCommands : NetworkBehaviour
{
    void Update()
    {
        // Консольные команды через клавиши - только для хоста
        if (!isServer) return;
        
        // Все команды удалены
    }
}
