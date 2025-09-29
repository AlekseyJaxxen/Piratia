using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Monster/MonsterDatabase", order = 2)]
public class MonsterDatabase : ScriptableObject
{
    public List<MonsterInfo> monsters = new List<MonsterInfo>(); // Список SO по ID (индекс = ID-1 или мап)
}