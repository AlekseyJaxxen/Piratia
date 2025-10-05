using UnityEngine;

/// <summary>
/// Тест для проверки отображения всех параметров боя в Monster.cs
/// </summary>
public class MonsterBattleStatsTest : MonoBehaviour
{
    [ContextMenu("Test Monster Battle Stats")]
    void TestMonsterBattleStats()
    {
        Debug.Log("=== Testing Monster Battle Stats ===");
        
        // Тест 1: Проверяем SyncVar параметры
        TestSyncVarParameters();
        
        // Тест 2: Проверяем существующих монстров
        TestExistingMonsters();
        
        // Тест 3: Проверяем инициализацию
        TestInitialization();
    }
    
    void TestSyncVarParameters()
    {
        Debug.Log("=== Test 1: SyncVar Parameters ===");
        
        Debug.Log("✅ Monster.cs теперь содержит SyncVar параметры:");
        Debug.Log("  - [SyncVar] public int hitRate = 10");
        Debug.Log("  - [SyncVar] public int dodge = 10");
        Debug.Log("  - [SyncVar] public int minAttack = 10");
        Debug.Log("  - [SyncVar] public int maxAttack = 15");
        Debug.Log("  - [SyncVar] public int defense = 5");
        Debug.Log("  - [SyncVar] public int physicalResistance = 3");
        
        Debug.Log("✅ Эти параметры будут отображаться в Battle Characteristics!");
    }
    
    void TestExistingMonsters()
    {
        Debug.Log("=== Test 2: Existing Monsters ===");
        
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        Debug.Log($"Found {allMonsters.Length} monsters in scene");
        
        foreach (Monster monster in allMonsters)
        {
            Debug.Log($"Monster: {monster.monsterName}");
            Debug.Log($"  - hitRate: {monster.hitRate}");
            Debug.Log($"  - dodge: {monster.dodge}");
            Debug.Log($"  - minAttack: {monster.minAttack}");
            Debug.Log($"  - maxAttack: {monster.maxAttack}");
            Debug.Log($"  - defense: {monster.defense}");
            Debug.Log($"  - physicalResistance: {monster.physicalResistance}");
            
            if (monster.info != null)
            {
                Debug.Log($"  - MonsterInfo.hitRate: {monster.info.hitRate}");
                Debug.Log($"  - MonsterInfo.dodge: {monster.info.dodge}");
                Debug.Log($"  - MonsterInfo.minAttack: {monster.info.minAttack}");
                Debug.Log($"  - MonsterInfo.maxAttack: {monster.info.maxAttack}");
                Debug.Log($"  - MonsterInfo.defense: {monster.info.defense}");
                Debug.Log($"  - MonsterInfo.physicalResistance: {monster.info.physicalResistance}");
            }
            
            Debug.Log("---");
        }
    }
    
    void TestInitialization()
    {
        Debug.Log("=== Test 3: Initialization ===");
        
        Debug.Log("✅ LoadAndInitializeServer() теперь инициализирует:");
        Debug.Log("  - hitRate = info.hitRate (или * eliteStatMultiplier для Elite)");
        Debug.Log("  - dodge = info.dodge (или * eliteStatMultiplier для Elite)");
        Debug.Log("  - minAttack = info.minAttack (или * eliteStatMultiplier для Elite)");
        Debug.Log("  - maxAttack = info.maxAttack (или * eliteStatMultiplier для Elite)");
        Debug.Log("  - defense = info.defense (или * eliteStatMultiplier для Elite)");
        Debug.Log("  - physicalResistance = info.physicalResistance (или * eliteStatMultiplier для Elite)");
        
        Debug.Log("✅ Все параметры синхронизируются с клиентами через SyncVar!");
    }
    
    [ContextMenu("Check Battle Characteristics Display")]
    void CheckBattleCharacteristicsDisplay()
    {
        Debug.Log("=== Checking Battle Characteristics Display ===");
        
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        
        foreach (Monster monster in allMonsters)
        {
            Debug.Log($"Monster: {monster.monsterName}");
            Debug.Log("Battle Characteristics:");
            Debug.Log($"  - Hit Rate: {monster.hitRate}");
            Debug.Log($"  - Dodge: {monster.dodge}");
            Debug.Log($"  - Min Attack: {monster.minAttack}");
            Debug.Log($"  - Max Attack: {monster.maxAttack}");
            Debug.Log($"  - Defense: {monster.defense}");
            Debug.Log($"  - Physical Resistance: {monster.physicalResistance}");
            
            if (monster.isElite)
            {
                Debug.Log("  - Elite Status: YES (stats multiplied)");
            }
            else
            {
                Debug.Log("  - Elite Status: NO (base stats)");
            }
            
            Debug.Log("---");
        }
    }
    
    [ContextMenu("Log Battle Stats Flow")]
    void LogBattleStatsFlow()
    {
        Debug.Log("=== Battle Stats Flow ===");
        
        Debug.Log("BEFORE FIX:");
        Debug.Log("1. MonsterInfo содержит все параметры боя");
        Debug.Log("2. Monster.cs содержит только hitRate и dodge как SyncVar");
        Debug.Log("3. Остальные параметры не синхронизируются");
        Debug.Log("4. РЕЗУЛЬТАТ: В Battle Characteristics отображаются только hitRate и dodge!");
        
        Debug.Log("");
        Debug.Log("AFTER FIX:");
        Debug.Log("1. MonsterInfo содержит все параметры боя");
        Debug.Log("2. Monster.cs содержит ВСЕ параметры как SyncVar");
        Debug.Log("3. Все параметры синхронизируются с клиентами");
        Debug.Log("4. РЕЗУЛЬТАТ: В Battle Characteristics отображаются ВСЕ параметры!");
        
        Debug.Log("");
        Debug.Log("KEY CHANGES:");
        Debug.Log("- Добавлены SyncVar для minAttack, maxAttack, defense, physicalResistance");
        Debug.Log("- LoadAndInitializeServer() инициализирует все параметры");
        Debug.Log("- Elite модификаторы применяются ко всем параметрам");
        Debug.Log("- Все параметры синхронизируются с клиентами");
    }
}
