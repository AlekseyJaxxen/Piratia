using UnityEngine;

/// <summary>
/// Тест для проверки загрузки MonsterInfo на клиентах
/// </summary>
public class MonsterInfoLoadTest : MonoBehaviour
{
    [ContextMenu("Test MonsterInfo Loading")]
    void TestMonsterInfoLoading()
    {
        Debug.Log("=== Testing MonsterInfo Loading ===");
        
        // Тестируем загрузку MonsterDatabase
        MonsterDatabase db = Resources.Load<MonsterDatabase>("MonsterData/MonsterDatabase");
        if (db != null)
        {
            Debug.Log($"✅ MonsterDatabase loaded: {db.monsters.Count} monsters");
            for (int i = 0; i < db.monsters.Count; i++)
            {
                if (db.monsters[i] != null)
                {
                    Debug.Log($"  - ID: {db.monsters[i].monsterId}, Name: {db.monsters[i].monsterName}");
                }
            }
        }
        else
        {
            Debug.LogError("❌ MonsterDatabase not found!");
        }
        
        // Тестируем загрузку сгенерированных монстров
        MonsterInfo[] generatedMonsters = Resources.LoadAll<MonsterInfo>("MonsterData/Generated");
        Debug.Log($"✅ Generated monsters loaded: {generatedMonsters.Length} monsters");
        foreach (MonsterInfo monsterInfo in generatedMonsters)
        {
            if (monsterInfo != null)
            {
                Debug.Log($"  - ID: {monsterInfo.monsterId}, Name: {monsterInfo.monsterName}");
            }
        }
        
        // Тестируем загрузку конкретного монстра
        TestLoadSpecificMonster(1);
        TestLoadSpecificMonster(5);
        TestLoadSpecificMonster(10);
    }
    
    void TestLoadSpecificMonster(int monsterId)
    {
        Debug.Log($"=== Testing Load Monster ID: {monsterId} ===");
        
        // Симулируем LoadMonsterInfo логику
        MonsterDatabase db = Resources.Load<MonsterDatabase>("MonsterData/MonsterDatabase");
        if (db != null && monsterId - 1 >= 0 && monsterId - 1 < db.monsters.Count)
        {
            MonsterInfo monsterInfo = db.monsters[monsterId - 1];
            Debug.Log($"✅ Found in MonsterDatabase: {monsterInfo.monsterName}");
            return;
        }
        
        // Ищем в сгенерированных
        MonsterInfo[] generatedMonsters = Resources.LoadAll<MonsterInfo>("MonsterData/Generated");
        foreach (MonsterInfo monsterInfo in generatedMonsters)
        {
            if (monsterInfo != null && monsterInfo.monsterId == monsterId)
            {
                Debug.Log($"✅ Found in Generated: {monsterInfo.monsterName}");
                return;
            }
        }
        
        Debug.LogError($"❌ Monster ID {monsterId} not found anywhere!");
    }
    
    [ContextMenu("Check Resources Structure")]
    void CheckResourcesStructure()
    {
        Debug.Log("=== Checking Resources Structure ===");
        
        // Проверяем что есть в Resources
        Object[] allResources = Resources.LoadAll("");
        Debug.Log($"Total Resources: {allResources.Length}");
        
        foreach (Object resource in allResources)
        {
            if (resource != null)
            {
                Debug.Log($"  - {resource.name} ({resource.GetType().Name})");
            }
        }
        
        // Проверяем MonsterData папку
        MonsterDatabase[] monsterDbs = Resources.LoadAll<MonsterDatabase>("MonsterData");
        Debug.Log($"MonsterDatabases in MonsterData: {monsterDbs.Length}");
        
        MonsterInfo[] monsterInfos = Resources.LoadAll<MonsterInfo>("MonsterData");
        Debug.Log($"MonsterInfos in MonsterData: {monsterInfos.Length}");
        
        MonsterInfo[] generatedInfos = Resources.LoadAll<MonsterInfo>("MonsterData/Generated");
        Debug.Log($"Generated MonsterInfos: {generatedInfos.Length}");
    }
}
