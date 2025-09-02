using Mirror;

public struct NetworkPlayerInfo : NetworkMessage
{
    public string playerName;
    public PlayerTeam playerTeam;
    public int playerPrefabIndex;
    public CharacterClass characterClass; // Добавляем класс
}