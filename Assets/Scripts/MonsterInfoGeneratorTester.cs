using UnityEngine;
using UnityEditor;

/// <summary>
/// Простой тестер для MonsterInfoGenerator
/// </summary>
public class MonsterInfoGeneratorTester : MonoBehaviour
{
    [ContextMenu("Test Generator")]
    void TestGenerator()
    {
        Debug.Log("=== ТЕСТИРОВАНИЕ MONSTER INFO GENERATOR ===");
        
        // Создаем временный генератор
        MonsterInfoGenerator generator = gameObject.AddComponent<MonsterInfoGenerator>();
        
        // Инициализируем шаблоны
        generator.InitializeTemplates();
        
        Debug.Log($"Шаблонов создано: {generator.monsterTemplates.Count}");
        
        // Тестируем генерацию
        MonsterInfo tankMonster = generator.GenerateMonsterInfo(MonsterInfoGenerator.MonsterCategory.Tank, 5);
        if (tankMonster != null)
        {
            Debug.Log($"✅ Tank Monster Level 5 создан: {tankMonster.monsterName}");
            Debug.Log($"   HP: {tankMonster.maxHealth}");
            Debug.Log($"   Hit Rate: {tankMonster.hitRate}");
            Debug.Log($"   Dodge: {tankMonster.dodge}");
            Debug.Log($"   Use Temporary Box: {tankMonster.useTemporaryBox}");
            if (tankMonster.useTemporaryBox)
            {
                Debug.Log($"   Box Type: {tankMonster.boxType}");
                Debug.Log($"   Box Color: {tankMonster.boxColor}");
                Debug.Log($"   Box Size: {tankMonster.boxSize}");
            }
        }
        else
        {
            Debug.LogError("❌ Не удалось создать Tank Monster");
        }
        
        // Удаляем временный компонент
        DestroyImmediate(generator);
        
        Debug.Log("=== ТЕСТ ЗАВЕРШЕН ===");
    }
    
    [ContextMenu("Test All Categories")]
    void TestAllCategories()
    {
        Debug.Log("=== ТЕСТИРОВАНИЕ ВСЕХ КАТЕГОРИЙ ===");
        
        MonsterInfoGenerator generator = gameObject.AddComponent<MonsterInfoGenerator>();
        generator.InitializeTemplates();
        
        foreach (MonsterInfoGenerator.MonsterCategory category in System.Enum.GetValues(typeof(MonsterInfoGenerator.MonsterCategory)))
        {
            MonsterInfo monster = generator.GenerateMonsterInfo(category, 3);
            if (monster != null)
            {
                Debug.Log($"✅ {category} Level 3: {monster.monsterName} (HP: {monster.maxHealth})");
            }
            else
            {
                Debug.LogError($"❌ Не удалось создать {category} Level 3");
            }
        }
        
        DestroyImmediate(generator);
        Debug.Log("=== ТЕСТ ВСЕХ КАТЕГОРИЙ ЗАВЕРШЕН ===");
    }
    
    [ContextMenu("Test Save Assets")]
    void TestSaveAssets()
    {
        Debug.Log("=== ТЕСТИРОВАНИЕ СОХРАНЕНИЯ ASSETS ===");
        
        MonsterInfoGenerator generator = gameObject.AddComponent<MonsterInfoGenerator>();
        generator.outputFolder = "Assets/Resources/MonsterData/TestGenerated";
        
        // Генерируем несколько монстров
        generator.GenerateMonsterInfoRange(MonsterInfoGenerator.MonsterCategory.Tank, 1, 3);
        
        Debug.Log("Проверьте папку Assets/Resources/MonsterData/TestGenerated/");
        
        DestroyImmediate(generator);
        Debug.Log("=== ТЕСТ СОХРАНЕНИЯ ЗАВЕРШЕН ===");
    }
}
