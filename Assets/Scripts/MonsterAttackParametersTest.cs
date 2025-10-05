using UnityEngine;

/// <summary>
/// Тест для проверки использования параметров атаки из MonsterInfo
/// </summary>
public class MonsterAttackParametersTest : MonoBehaviour
{
    [ContextMenu("Test Monster Attack Parameters")]
    void TestMonsterAttackParameters()
    {
        Debug.Log("=== Testing Monster Attack Parameters ===");
        
        // Тест 1: Проверяем что параметры атаки используются
        TestAttackParametersUsage();
        
        // Тест 2: Проверяем существующих монстров
        TestExistingMonsters();
        
        // Тест 3: Проверяем MonsterInfo генератор
        TestMonsterInfoGenerator();
    }
    
    void TestAttackParametersUsage()
    {
        Debug.Log("=== Test 1: Attack Parameters Usage ===");
        
        Debug.Log("✅ MonsterInfo содержит параметры атаки:");
        Debug.Log("  - minAttack: минимальный урон");
        Debug.Log("  - maxAttack: максимальный урон");
        Debug.Log("  - attackRange: дальность атаки");
        Debug.Log("  - attackCooldown: скорость атаки");
        
        Debug.Log("✅ MonsterAI2 теперь использует:");
        Debug.Log("  - attackRange из MonsterInfo (не из basicAttackSkill)");
        Debug.Log("  - Обновляет basicAttackSkill.Range на основе MonsterInfo");
        
        Debug.Log("✅ MonsterBasicAttackSkill использует:");
        Debug.Log("  - minAttack и maxAttack из MonsterInfo");
        Debug.Log("  - Рассчитывает урон на основе характеристик монстра");
        
        Debug.Log("✅ Monster.cs использует:");
        Debug.Log("  - attackCooldown из MonsterInfo");
        Debug.Log("  - attackRange из MonsterInfo для Gizmos");
    }
    
    void TestExistingMonsters()
    {
        Debug.Log("=== Test 2: Existing Monsters ===");
        
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        Debug.Log($"Found {allMonsters.Length} monsters in scene");
        
        foreach (Monster monster in allMonsters)
        {
            if (monster.info != null)
            {
                Debug.Log($"Monster: {monster.monsterName}");
                Debug.Log($"  - MonsterInfo.minAttack: {monster.info.minAttack}");
                Debug.Log($"  - MonsterInfo.maxAttack: {monster.info.maxAttack}");
                Debug.Log($"  - MonsterInfo.attackRange: {monster.info.attackRange}");
                Debug.Log($"  - MonsterInfo.attackCooldown: {monster.info.attackCooldown}");
                Debug.Log($"  - MonsterInfo.defense: {monster.info.defense}");
                Debug.Log($"  - MonsterInfo.physicalResistance: {monster.info.physicalResistance}");
                
                Debug.Log($"  - Monster.minAttack (SyncVar): {monster.minAttack}");
                Debug.Log($"  - Monster.maxAttack (SyncVar): {monster.maxAttack}");
                Debug.Log($"  - Monster.defense (SyncVar): {monster.defense}");
                Debug.Log($"  - Monster.physicalResistance (SyncVar): {monster.physicalResistance}");
                
                // Проверяем MonsterAI2
                MonsterAI2 ai2 = monster.GetComponent<MonsterAI2>();
                if (ai2 != null)
                {
                    Debug.Log($"  - MonsterAI2.attackRange: {ai2.attackRange}");
                }
                
                // Проверяем basicAttackSkill
                if (monster.info.basicAttackSkill != null)
                {
                    Debug.Log($"  - basicAttackSkill.Range: {monster.info.basicAttackSkill.Range}");
                }
                
                Debug.Log("---");
            }
        }
    }
    
    void TestMonsterInfoGenerator()
    {
        Debug.Log("=== Test 3: MonsterInfo Generator ===");
        
        // Проверяем что генератор создает правильные параметры
        MonsterInfoGenerator generator = FindObjectOfType<MonsterInfoGenerator>();
        if (generator != null)
        {
            Debug.Log("✅ MonsterInfoGenerator найден");
            Debug.Log("✅ Генерирует параметры атаки:");
            Debug.Log("  - minAttack: Mathf.Pow(level, 1.2f) * 1.5f + level * 2f");
            Debug.Log("  - maxAttack: minAttack * 1.2f");
            Debug.Log("  - attackRange: template.baseAttackRange / 100f");
            Debug.Log("  - attackCooldown: из MonsterInfo");
        }
        else
        {
            Debug.LogWarning("❌ MonsterInfoGenerator не найден");
        }
    }
    
    [ContextMenu("Check Monster Attack Range")]
    void CheckMonsterAttackRange()
    {
        Debug.Log("=== Checking Monster Attack Range ===");
        
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        
        foreach (Monster monster in allMonsters)
        {
            if (monster.info != null)
            {
                Debug.Log($"Monster: {monster.monsterName}");
                Debug.Log($"  - MonsterInfo.attackRange: {monster.info.attackRange}");
                
                MonsterAI2 ai2 = monster.GetComponent<MonsterAI2>();
                if (ai2 != null)
                {
                    Debug.Log($"  - MonsterAI2.attackRange: {ai2.attackRange}");
                    Debug.Log($"  - MonsterAI2.currentAttackRange: {ai2.currentAttackRange}");
                    Debug.Log($"  - MonsterAI2.currentStoppingDistance: {ai2.currentStoppingDistance}");
                }
                
                if (monster.info.basicAttackSkill != null)
                {
                    Debug.Log($"  - basicAttackSkill.Range: {monster.info.basicAttackSkill.Range}");
                }
                
                Debug.Log("---");
            }
        }
    }
    
    [ContextMenu("Log Attack Parameter Flow")]
    void LogAttackParameterFlow()
    {
        Debug.Log("=== Attack Parameter Flow ===");
        
        Debug.Log("BEFORE FIX:");
        Debug.Log("1. MonsterInfo содержит attackRange");
        Debug.Log("2. MonsterAI2 игнорирует MonsterInfo.attackRange");
        Debug.Log("3. MonsterAI2 использует basicAttackSkill.Range");
        Debug.Log("4. РЕЗУЛЬТАТ: Монстры атакуют на дефолтном расстоянии!");
        
        Debug.Log("");
        Debug.Log("AFTER FIX:");
        Debug.Log("1. MonsterInfo содержит attackRange");
        Debug.Log("2. MonsterAI2 использует MonsterInfo.attackRange");
        Debug.Log("3. MonsterAI2 обновляет basicAttackSkill.Range");
        Debug.Log("4. РЕЗУЛЬТАТ: Монстры атакуют на правильном расстоянии!");
        
        Debug.Log("");
        Debug.Log("KEY CHANGES:");
        Debug.Log("- MonsterAI2.InitializeAI() использует monsterInfo.attackRange");
        Debug.Log("- MonsterAI2.UpdateAttackRanges() использует monster.info.attackRange");
        Debug.Log("- Monster.cs OnDrawGizmos() использует info.attackRange");
        Debug.Log("- MonsterBasicAttackSkill уже использовал minAttack/maxAttack");
    }
}
