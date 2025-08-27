using Mirror;

public struct NetworkPlayerInfo : NetworkMessage
{
    public string playerName;
    public PlayerTeam playerTeam;
    public int playerPrefabIndex;
}

//public enum PlayerTeam
//{
 //   None,
 //   Red,
 //   Blue
//}