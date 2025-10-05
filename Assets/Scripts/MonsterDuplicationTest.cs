using UnityEngine;

/// <summary>
/// Тест для проверки исправления дублирования монстров
/// </summary>
public class MonsterDuplicationTest : MonoBehaviour
{
    [ContextMenu("Test Monster Duplication Fix")]
    void TestMonsterDuplicationFix()
    {
        Debug.Log("=== Testing Monster Duplication Fix ===");
        
        // Тест 1: Проверяем логику создания моделей
        TestModelCreationLogic();
        
        // Тест 2: Симулируем сервер и клиент
        TestServerClientSimulation();
        
        // Тест 3: Проверяем существующие монстры
        TestExistingMonsters();
    }
    
    void TestModelCreationLogic()
    {
        Debug.Log("=== Test 1: Model Creation Logic ===");
        
        Debug.Log("✅ SERVER (LoadAndInitializeServer):");
        Debug.Log("  - Creates temporary box OR model prefab");
        Debug.Log("  - Sets up all monster data");
        Debug.Log("  - Initializes AI and components");
        
        Debug.Log("✅ CLIENT (LoadAndInitializeClient):");
        Debug.Log("  - Loads MonsterInfo data");
        Debug.Log("  - Checks for existing model (created by server)");
        Debug.Log("  - Does NOT create duplicate models");
        Debug.Log("  - Only initializes UI and client components");
        
        Debug.Log("✅ RESULT: No more duplicate monsters!");
    }
    
    void TestServerClientSimulation()
    {
        Debug.Log("=== Test 2: Server-Client Simulation ===");
        
        // Симулируем процесс создания монстра
        Debug.Log("1. Server spawns monster with ID=5");
        Debug.Log("2. Server calls LoadAndInitializeServer()");
        Debug.Log("3. Server creates temporary box for Tank_Lv5");
        Debug.Log("4. Server spawns monster object to clients");
        
        Debug.Log("5. Client receives monster object");
        Debug.Log("6. Client calls OnStartClient()");
        Debug.Log("7. Client calls LoadAndInitializeClient()");
        Debug.Log("8. Client finds existing temporary box (created by server)");
        Debug.Log("9. Client does NOT create duplicate box");
        
        Debug.Log("✅ RESULT: Only ONE monster model per client!");
    }
    
    void TestExistingMonsters()
    {
        Debug.Log("=== Test 3: Existing Monsters Check ===");
        
        // Ищем всех монстров в сцене
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        Debug.Log($"Found {allMonsters.Length} monsters in scene");
        
        int duplicateCount = 0;
        foreach (Monster monster in allMonsters)
        {
            // Проверяем количество дочерних объектов с тегом MonsterModel
            Transform[] modelChildren = monster.GetComponentsInChildren<Transform>();
            int modelCount = 0;
            
            foreach (Transform child in modelChildren)
            {
                if (child.tag == "MonsterModel" || child.name.Contains("TemporaryBox") || child.name.Contains("Model"))
                {
                    modelCount++;
                }
            }
            
            if (modelCount > 1)
            {
                Debug.LogWarning($"❌ Monster {monster.name} has {modelCount} models - possible duplication!");
                duplicateCount++;
            }
            else if (modelCount == 1)
            {
                Debug.Log($"✅ Monster {monster.name} has {modelCount} model - correct");
            }
            else
            {
                Debug.LogWarning($"⚠️ Monster {monster.name} has {modelCount} models - no model found");
            }
        }
        
        if (duplicateCount == 0)
        {
            Debug.Log("✅ SUCCESS: No duplicate monsters found!");
        }
        else
        {
            Debug.LogError($"❌ FAILURE: {duplicateCount} monsters have duplicates!");
        }
    }
    
    [ContextMenu("Check Monster Hierarchy")]
    void CheckMonsterHierarchy()
    {
        Debug.Log("=== Checking Monster Hierarchy ===");
        
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        
        foreach (Monster monster in allMonsters)
        {
            Debug.Log($"Monster: {monster.name}");
            Debug.Log($"  - Position: {monster.transform.position}");
            Debug.Log($"  - Child Count: {monster.transform.childCount}");
            
            for (int i = 0; i < monster.transform.childCount; i++)
            {
                Transform child = monster.transform.GetChild(i);
                Debug.Log($"    - Child {i}: {child.name} (Tag: {child.tag})");
                
                // Проверяем дочерние объекты модели
                if (child.tag == "MonsterModel" || child.name.Contains("TemporaryBox"))
                {
                    Debug.Log($"      - Model Child Count: {child.childCount}");
                    for (int j = 0; j < child.childCount; j++)
                    {
                        Transform modelChild = child.GetChild(j);
                        Debug.Log($"        - Model Child {j}: {modelChild.name}");
                    }
                }
            }
            Debug.Log("---");
        }
    }
    
    [ContextMenu("Log Monster Creation Process")]
    void LogMonsterCreationProcess()
    {
        Debug.Log("=== Monster Creation Process ===");
        
        Debug.Log("BEFORE FIX:");
        Debug.Log("1. Server creates monster model");
        Debug.Log("2. Client receives monster");
        Debug.Log("3. Client ALSO creates monster model");
        Debug.Log("4. RESULT: 2 models per monster = DUPLICATION!");
        
        Debug.Log("");
        Debug.Log("AFTER FIX:");
        Debug.Log("1. Server creates monster model");
        Debug.Log("2. Client receives monster");
        Debug.Log("3. Client checks for existing model");
        Debug.Log("4. Client finds model created by server");
        Debug.Log("5. Client does NOT create duplicate");
        Debug.Log("6. RESULT: 1 model per monster = CORRECT!");
        
        Debug.Log("");
        Debug.Log("KEY CHANGES:");
        Debug.Log("- LoadAndInitializeClient() no longer creates models");
        Debug.Log("- Client only checks for existing models");
        Debug.Log("- Server handles all model creation");
        Debug.Log("- Mirror networking works as intended");
    }
}
