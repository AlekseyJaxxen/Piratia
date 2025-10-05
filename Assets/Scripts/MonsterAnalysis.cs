using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Анализ примеров монстров для создания точной системы генерации
/// </summary>
public class MonsterAnalysis : MonoBehaviour
{
    [System.Serializable]
    public class MonsterExample
    {
        public string name;
        public int level;
        public int hp;
        public int minAttack;
        public int physicalResistance;
        public int defense;
        public int hitRate;
        public int dodge;
        public int attackSpeed;
        public int movementSpeed;
        public int experience;
        
        // Рассчитанные характеристики
        public float hpPerLevel;
        public float attackPerLevel;
        public float defensePerLevel;
        public float speedPerLevel;
    }
    
    [Header("Monster Examples")]
    public List<MonsterExample> examples = new List<MonsterExample>();
    
    void Start()
    {
        AnalyzeMonsterExamples();
    }
    
    /// <summary>
    /// Анализирует примеры монстров для понимания прогрессии
    /// </summary>
    void AnalyzeMonsterExamples()
    {
        examples.Clear();
        
        // Little Squidy - Level 5
        examples.Add(new MonsterExample
        {
            name = "Little Squidy",
            level = 5,
            hp = 90,
            minAttack = 16,
            physicalResistance = 3,
            defense = 6,
            hitRate = 16,
            dodge = 16,
            attackSpeed = 2000,
            movementSpeed = 250,
            experience = 15
        });
        
        // Forest Spirit - Level 3
        examples.Add(new MonsterExample
        {
            name = "Forest Spirit",
            level = 3,
            hp = 69,
            minAttack = 9,
            physicalResistance = 2,
            defense = 6,
            hitRate = 9,
            dodge = 9,
            attackSpeed = 2000,
            movementSpeed = 300,
            experience = 7
        });
        
        // Greedy Shroom - Level 6
        examples.Add(new MonsterExample
        {
            name = "Greedy Shroom",
            level = 6,
            hp = 105,
            minAttack = 17,
            physicalResistance = 3,
            defense = 6,
            hitRate = 19,
            dodge = 19,
            attackSpeed = 2000,
            movementSpeed = 250,
            experience = 21
        });
        
        // Grassland Wolf - Level 33
        examples.Add(new MonsterExample
        {
            name = "Grassland Wolf",
            level = 33,
            hp = 1065,
            minAttack = 92,
            physicalResistance = 10,
            defense = 60,
            hitRate = 108,
            dodge = 108,
            attackSpeed = 700,
            movementSpeed = 350,
            experience = 873
        });
        
        // Pumpkin Knight - Level 46
        examples.Add(new MonsterExample
        {
            name = "Pumpkin Knight",
            level = 46,
            hp = 2800,
            minAttack = 231,
            physicalResistance = 20,
            defense = 45,
            hitRate = 152,
            dodge = 152,
            attackSpeed = 2000,
            movementSpeed = 350,
            experience = 1700
        });
        
        // Feral White Bobcat - Level 70
        examples.Add(new MonsterExample
        {
            name = "Feral White Bobcat",
            level = 70,
            hp = 6096,
            minAttack = 460,
            physicalResistance = 28,
            defense = 147,
            hitRate = 246,
            dodge = 282,
            attackSpeed = 2000,
            movementSpeed = 350,
            experience = 5079
        });
        
        // Рассчитываем прогрессию
        CalculateProgression();
    }
    
    /// <summary>
    /// Рассчитывает прогрессию характеристик
    /// </summary>
    void CalculateProgression()
    {
        Debug.Log("=== АНАЛИЗ ПРОГРЕССИИ МОНСТРОВ ===");
        
        for (int i = 0; i < examples.Count; i++)
        {
            MonsterExample example = examples[i];
            
            // Рассчитываем характеристики на уровень
            example.hpPerLevel = (float)example.hp / example.level;
            example.attackPerLevel = (float)example.minAttack / example.level;
            example.defensePerLevel = (float)example.defense / example.level;
            example.speedPerLevel = (float)example.movementSpeed / example.level;
            
            Debug.Log($"=== {example.name} (Level {example.level}) ===");
            Debug.Log($"HP: {example.hp} ({example.hpPerLevel:F1} per level)");
            Debug.Log($"Attack: {example.minAttack} ({example.attackPerLevel:F1} per level)");
            Debug.Log($"Defense: {example.defense} ({example.defensePerLevel:F1} per level)");
            Debug.Log($"Physical Resistance: {example.physicalResistance}");
            Debug.Log($"Hit Rate: {example.hitRate}");
            Debug.Log($"Dodge: {example.dodge}");
            Debug.Log($"Attack Speed: {example.attackSpeed}");
            Debug.Log($"Movement Speed: {example.movementSpeed}");
            Debug.Log($"Experience: {example.experience}");
            Debug.Log("");
        }
        
        // Анализируем общие тренды
        AnalyzeTrends();
    }
    
    /// <summary>
    /// Анализирует общие тренды прогрессии
    /// </summary>
    void AnalyzeTrends()
    {
        Debug.Log("=== ОБЩИЕ ТРЕНДЫ ПРОГРЕССИИ ===");
        
        // HP прогрессия
        Debug.Log("HP прогрессия:");
        for (int i = 0; i < examples.Count; i++)
        {
            MonsterExample example = examples[i];
            Debug.Log($"Level {example.level}: {example.hp} HP ({example.hpPerLevel:F1} per level)");
        }
        
        // Attack прогрессия
        Debug.Log("Attack прогрессия:");
        for (int i = 0; i < examples.Count; i++)
        {
            MonsterExample example = examples[i];
            Debug.Log($"Level {example.level}: {example.minAttack} Attack ({example.attackPerLevel:F1} per level)");
        }
        
        // Defense прогрессия
        Debug.Log("Defense прогрессия:");
        for (int i = 0; i < examples.Count; i++)
        {
            MonsterExample example = examples[i];
            Debug.Log($"Level {example.level}: {example.defense} Defense ({example.defensePerLevel:F1} per level)");
        }
        
        // Выводим формулы для генерации
        GenerateFormulas();
    }
    
    /// <summary>
    /// Генерирует формулы для создания монстров
    /// </summary>
    void GenerateFormulas()
    {
        Debug.Log("=== ФОРМУЛЫ ДЛЯ ГЕНЕРАЦИИ МОНСТРОВ ===");
        
        // Аппроксимируем формулы на основе данных
        Debug.Log("Примерные формулы:");
        Debug.Log("HP = level^1.8 * 2.5 + level * 5");
        Debug.Log("Attack = level^1.2 * 1.5 + level * 2");
        Debug.Log("Defense = level^1.5 * 0.8 + level * 1.5");
        Debug.Log("Physical Resistance = level^1.3 * 0.3 + level * 0.5");
        Debug.Log("Hit Rate = level^1.1 * 2 + level * 3");
        Debug.Log("Dodge = level^1.1 * 2 + level * 3");
        Debug.Log("Experience = level^2.2 * 0.5 + level * 2");
        
        // Тестируем формулы
        TestFormulas();
    }
    
    /// <summary>
    /// Тестирует формулы на примерах
    /// </summary>
    void TestFormulas()
    {
        Debug.Log("=== ТЕСТИРОВАНИЕ ФОРМУЛ ===");
        
        foreach (MonsterExample example in examples)
        {
            int level = example.level;
            
            // Тестируем HP формулу
            float calculatedHp = Mathf.Pow(level, 1.8f) * 2.5f + level * 5f;
            int hpDiff = Mathf.RoundToInt(calculatedHp) - example.hp;
            
            // Тестируем Attack формулу
            float calculatedAttack = Mathf.Pow(level, 1.2f) * 1.5f + level * 2f;
            int attackDiff = Mathf.RoundToInt(calculatedAttack) - example.minAttack;
            
            // Тестируем Defense формулу
            float calculatedDefense = Mathf.Pow(level, 1.5f) * 0.8f + level * 1.5f;
            int defenseDiff = Mathf.RoundToInt(calculatedDefense) - example.defense;
            
            Debug.Log($"Level {level} ({example.name}):");
            Debug.Log($"  HP: рассчитано {calculatedHp:F0}, реально {example.hp}, разница {hpDiff}");
            Debug.Log($"  Attack: рассчитано {calculatedAttack:F0}, реально {example.minAttack}, разница {attackDiff}");
            Debug.Log($"  Defense: рассчитано {calculatedDefense:F0}, реально {example.defense}, разница {defenseDiff}");
        }
    }
    
    /// <summary>
    /// Создает улучшенный генератор монстров на основе анализа
    /// </summary>
    [ContextMenu("Create Improved Generator")]
    void CreateImprovedGenerator()
    {
        Debug.Log("=== СОЗДАНИЕ УЛУЧШЕННОГО ГЕНЕРАТОРА ===");
        
        // Создаем формулы на основе анализа
        Debug.Log("Рекомендуемые формулы для генератора:");
        Debug.Log("HP = Mathf.Pow(level, 1.8f) * 2.5f + level * 5f");
        Debug.Log("Attack = Mathf.Pow(level, 1.2f) * 1.5f + level * 2f");
        Debug.Log("Defense = Mathf.Pow(level, 1.5f) * 0.8f + level * 1.5f");
        Debug.Log("Physical Resistance = Mathf.Pow(level, 1.3f) * 0.3f + level * 0.5f");
        Debug.Log("Hit Rate = Mathf.Pow(level, 1.1f) * 2f + level * 3f");
        Debug.Log("Dodge = Mathf.Pow(level, 1.1f) * 2f + level * 3f");
        Debug.Log("Experience = Mathf.Pow(level, 2.2f) * 0.5f + level * 2f");
        
        Debug.Log("Множители для типов монстров:");
        Debug.Log("Tank: HP x1.5, Attack x1.2, Defense x1.4, Speed x0.7");
        Debug.Log("Fast: HP x0.7, Attack x0.8, Defense x0.6, Speed x1.4");
        Debug.Log("Magic: HP x1.2, Attack x1.4, Defense x1.3, Speed x0.8");
        Debug.Log("Ranged: HP x1.0, Attack x1.0, Defense x1.0, Speed x1.2");
    }
}
