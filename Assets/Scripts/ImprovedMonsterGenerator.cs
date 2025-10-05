using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Улучшенный генератор монстров на основе анализа примеров
/// </summary>
public class ImprovedMonsterGenerator : MonoBehaviour
{
    [Header("Monster Generation Settings")]
    public Transform spawnParent;
    public GameObject monsterPrefab;
    
    [Header("Visual Settings")]
    public Material tankMaterial;      // Синий - толстый моб
    public Material fastMaterial;     // Красный - быстрый моб
    public Material magicMaterial;    // Фиолетовый - магический моб
    public Material rangedMaterial;   // Желтый - дальний моб
    
    [System.Serializable]
    public class MonsterTypeConfig
    {
        [Header("Basic Info")]
        public string name;
        public MonsterCategory category;
        public int minLevel;
        public int maxLevel;
        
        [Header("Stat Multipliers")]
        [Range(0.5f, 2f)] public float hpMultiplier = 1f;
        [Range(0.5f, 2f)] public float attackMultiplier = 1f;
        [Range(0.5f, 2f)] public float defenseMultiplier = 1f;
        [Range(0.5f, 2f)] public float speedMultiplier = 1f;
        [Range(0.5f, 2f)] public float dodgeMultiplier = 1f;
        [Range(0.5f, 2f)] public float hitRateMultiplier = 1f;
        
        [Header("Visual")]
        public Vector3 boxSize = Vector3.one;
        public Color boxColor = Color.white;
        public string description;
    }
    
    public enum MonsterCategory
    {
        Tank,        // Толстый моб - повышенная защита, пониженный уворот, медленная скорость атаки, сильный урон
        Fast,        // Быстрый моб - пониженная защита, повышенный уворот, нормальная скорость бега, средняя скорость атаки, малый урон
        Magic,       // Моб для магических классов - повышенная защита, повышенная физическая защита, медленная скорость, сильный урон, повышенная уворот
        Ranged       // Моб с дальней атакой - средний во всем, но быстрее остальных
    }
    
    [Header("Monster Type Configurations")]
    public List<MonsterTypeConfig> monsterTypes = new List<MonsterTypeConfig>();
    
    void Start()
    {
        InitializeMonsterTypes();
    }
    
    /// <summary>
    /// Инициализирует типы монстров на основе анализа
    /// </summary>
    void InitializeMonsterTypes()
    {
        monsterTypes.Clear();
        
        // Толстый моб (Tank) - синий бокс
        monsterTypes.Add(new MonsterTypeConfig
        {
            name = "Tank Monster",
            category = MonsterCategory.Tank,
            minLevel = 1,
            maxLevel = 100,
            hpMultiplier = 1.5f,
            attackMultiplier = 1.2f,
            defenseMultiplier = 1.4f,
            speedMultiplier = 0.7f,
            dodgeMultiplier = 0.6f,
            hitRateMultiplier = 0.9f,
            boxSize = new Vector3(1.2f, 1.0f, 1.2f),
            boxColor = Color.blue,
            description = "Толстый моб с высокой защитой и уроном, но медленный"
        });
        
        // Быстрый моб (Fast) - красный бокс
        monsterTypes.Add(new MonsterTypeConfig
        {
            name = "Fast Monster",
            category = MonsterCategory.Fast,
            minLevel = 1,
            maxLevel = 100,
            hpMultiplier = 0.7f,
            attackMultiplier = 0.8f,
            defenseMultiplier = 0.6f,
            speedMultiplier = 1.4f,
            dodgeMultiplier = 1.5f,
            hitRateMultiplier = 1.2f,
            boxSize = new Vector3(0.8f, 0.8f, 0.8f),
            boxColor = Color.red,
            description = "Быстрый моб с высоким уворотом, но низкой защитой"
        });
        
        // Магический моб (Magic) - фиолетовый бокс
        monsterTypes.Add(new MonsterTypeConfig
        {
            name = "Magic Monster",
            category = MonsterCategory.Magic,
            minLevel = 1,
            maxLevel = 100,
            hpMultiplier = 1.2f,
            attackMultiplier = 1.4f,
            defenseMultiplier = 1.3f,
            speedMultiplier = 0.8f,
            dodgeMultiplier = 1.2f,
            hitRateMultiplier = 1.1f,
            boxSize = new Vector3(1.0f, 1.2f, 1.0f),
            boxColor = Color.magenta,
            description = "Магический моб с высоким уроном и защитой"
        });
        
        // Дальний моб (Ranged) - желтый бокс
        monsterTypes.Add(new MonsterTypeConfig
        {
            name = "Ranged Monster",
            category = MonsterCategory.Ranged,
            minLevel = 1,
            maxLevel = 100,
            hpMultiplier = 1.0f,
            attackMultiplier = 1.0f,
            defenseMultiplier = 1.0f,
            speedMultiplier = 1.2f,
            dodgeMultiplier = 1.0f,
            hitRateMultiplier = 1.3f,
            boxSize = new Vector3(0.9f, 1.4f, 0.9f),
            boxColor = Color.yellow,
            description = "Дальний моб со сбалансированными характеристиками"
        });
    }
    
    /// <summary>
    /// Генерирует монстра определенного типа и уровня
    /// </summary>
    public GameObject GenerateMonster(MonsterCategory category, int level)
    {
        MonsterTypeConfig monsterType = GetMonsterType(category);
        if (monsterType == null)
        {
            Debug.LogError($"Monster type {category} not found!");
            return null;
        }
        
        if (level < monsterType.minLevel || level > monsterType.maxLevel)
        {
            Debug.LogWarning($"Level {level} is outside range for {monsterType.name} ({monsterType.minLevel}-{monsterType.maxLevel})");
        }
        
        // Создаем монстра
        GameObject monster = Instantiate(monsterPrefab, spawnParent);
        monster.name = $"{monsterType.name} Lv.{level}";
        
        // Получаем компонент Monster
        Monster monsterScript = monster.GetComponent<Monster>();
        if (monsterScript == null)
        {
            Debug.LogError("Monster prefab doesn't have Monster component!");
            Destroy(monster);
            return null;
        }
        
        // Создаем 3D бокс для визуализации типа монстра
        CreateMonsterBox(monster, monsterType);
        
        // Настраиваем характеристики монстра на основе анализа
        ConfigureMonsterStats(monsterScript, monsterType, level);
        
        Debug.Log($"Generated {monsterType.name} Level {level} with category {category}");
        Debug.Log($"Description: {monsterType.description}");
        return monster;
    }
    
    /// <summary>
    /// Создает 3D бокс для визуализации типа монстра
    /// </summary>
    void CreateMonsterBox(GameObject monster, MonsterTypeConfig monsterType)
    {
        // Создаем дочерний объект для бокса
        GameObject boxObject = new GameObject("MonsterBox");
        boxObject.transform.SetParent(monster.transform);
        boxObject.transform.localPosition = Vector3.zero;
        
        // Добавляем компоненты для создания куба
        MeshRenderer renderer = boxObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = boxObject.AddComponent<MeshFilter>();
        
        // Создаем куб
        meshFilter.mesh = CreateCubeMesh();
        
        // Настраиваем материал
        Material material = new Material(Shader.Find("Standard"));
        material.color = monsterType.boxColor;
        material.SetFloat("_Metallic", 0.3f);
        material.SetFloat("_Smoothness", 0.7f);
        renderer.material = material;
        
        // Масштабируем бокс
        boxObject.transform.localScale = monsterType.boxSize;
        
        // Добавляем легкое свечение для лучшей видимости
        Light light = boxObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = monsterType.boxColor;
        light.intensity = 0.5f;
        light.range = 2f;
        
        // Добавляем текст с описанием
        GameObject textObject = new GameObject("MonsterInfo");
        textObject.transform.SetParent(boxObject.transform);
        textObject.transform.localPosition = Vector3.up * 2f;
        
        // Создаем простой текст (можно заменить на TextMeshPro)
        GameObject textMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        textMesh.transform.SetParent(textObject.transform);
        textMesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        textMesh.name = "TextIndicator";
        
        // Настраиваем цвет текста
        MeshRenderer textRenderer = textMesh.GetComponent<MeshRenderer>();
        Material textMaterial = new Material(Shader.Find("Standard"));
        textMaterial.color = Color.white;
        textRenderer.material = textMaterial;
    }
    
    /// <summary>
    /// Создает простую сетку куба
    /// </summary>
    Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        
        // Вершины куба
        Vector3[] vertices = new Vector3[]
        {
            // Передняя грань
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            
            // Задняя грань
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f)
        };
        
        // Треугольники
        int[] triangles = new int[]
        {
            // Передняя грань
            0, 1, 2, 0, 2, 3,
            // Задняя грань
            4, 6, 5, 4, 7, 6,
            // Левая грань
            4, 0, 3, 4, 3, 7,
            // Правая грань
            1, 5, 6, 1, 6, 2,
            // Верхняя грань
            3, 2, 6, 3, 6, 7,
            // Нижняя грань
            4, 1, 0, 4, 5, 1
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    /// <summary>
    /// Настраивает характеристики монстра на основе анализа примеров
    /// </summary>
    void ConfigureMonsterStats(Monster monsterScript, MonsterTypeConfig monsterType, int level)
    {
        // Формулы на основе анализа примеров монстров
        float levelMultiplier = CalculateLevelMultiplier(level);
        
        // HP формула: level^1.8 * 2.5 + level * 5
        int hp = Mathf.RoundToInt((Mathf.Pow(level, 1.8f) * 2.5f + level * 5f) * monsterType.hpMultiplier);
        
        // Attack формула: level^1.2 * 1.5 + level * 2
        int minAttack = Mathf.RoundToInt((Mathf.Pow(level, 1.2f) * 1.5f + level * 2f) * monsterType.attackMultiplier);
        int maxAttack = Mathf.RoundToInt(minAttack * 1.2f); // Max attack на 20% больше min
        
        // Defense формула: level^1.5 * 0.8 + level * 1.5
        int defense = Mathf.RoundToInt((Mathf.Pow(level, 1.5f) * 0.8f + level * 1.5f) * monsterType.defenseMultiplier);
        
        // Physical Resistance формула: level^1.3 * 0.3 + level * 0.5
        int physicalResistance = Mathf.RoundToInt((Mathf.Pow(level, 1.3f) * 0.3f + level * 0.5f) * monsterType.defenseMultiplier);
        
        // Hit Rate формула: level^1.1 * 2 + level * 3
        int hitRate = Mathf.RoundToInt((Mathf.Pow(level, 1.1f) * 2f + level * 3f) * monsterType.hitRateMultiplier);
        
        // Dodge формула: level^1.1 * 2 + level * 3
        int dodge = Mathf.RoundToInt((Mathf.Pow(level, 1.1f) * 2f + level * 3f) * monsterType.dodgeMultiplier);
        
        // Attack Speed (обратно пропорционально скорости атаки)
        int attackSpeed = Mathf.RoundToInt(2000f / monsterType.speedMultiplier);
        
        // Movement Speed (прямо пропорционально скорости)
        int movementSpeed = Mathf.RoundToInt(250f * monsterType.speedMultiplier);
        
        // Attack Range (зависит от типа)
        int attackRange = GetAttackRange(monsterType.category);
        
        // Experience формула: level^2.2 * 0.5 + level * 2
        int experience = Mathf.RoundToInt(Mathf.Pow(level, 2.2f) * 0.5f + level * 2f);
        
        // Настраиваем монстра (предполагаем, что у Monster есть эти поля)
        // monsterScript.level = level;
        // monsterScript.maxHealth = hp;
        // monsterScript.minAttack = minAttack;
        // monsterScript.maxAttack = maxAttack;
        // monsterScript.defense = defense;
        // monsterScript.physicalResistance = physicalResistance;
        // monsterScript.hitRate = hitRate;
        // monsterScript.dodge = dodge;
        // monsterScript.attackSpeed = attackSpeed;
        // monsterScript.movementSpeed = movementSpeed;
        // monsterScript.attackRange = attackRange;
        // monsterScript.experienceReward = experience;
        
        Debug.Log($"=== {monsterType.name} Level {level} ===");
        Debug.Log($"HP: {hp} (x{monsterType.hpMultiplier})");
        Debug.Log($"Attack: {minAttack}-{maxAttack} (x{monsterType.attackMultiplier})");
        Debug.Log($"Defense: {defense} (x{monsterType.defenseMultiplier})");
        Debug.Log($"Physical Resistance: {physicalResistance} (x{monsterType.defenseMultiplier})");
        Debug.Log($"Hit Rate: {hitRate} (x{monsterType.hitRateMultiplier})");
        Debug.Log($"Dodge: {dodge} (x{monsterType.dodgeMultiplier})");
        Debug.Log($"Attack Speed: {attackSpeed} (x{1f/monsterType.speedMultiplier})");
        Debug.Log($"Movement Speed: {movementSpeed} (x{monsterType.speedMultiplier})");
        Debug.Log($"Attack Range: {attackRange}");
        Debug.Log($"Experience: {experience}");
    }
    
    /// <summary>
    /// Получает дальность атаки в зависимости от типа монстра
    /// </summary>
    int GetAttackRange(MonsterCategory category)
    {
        switch (category)
        {
            case MonsterCategory.Tank:
                return 150; // Ближний бой
            case MonsterCategory.Fast:
                return 120; // Ближний бой
            case MonsterCategory.Magic:
                return 200; // Магическая атака
            case MonsterCategory.Ranged:
                return 300; // Дальняя атака
            default:
                return 150;
        }
    }
    
    /// <summary>
    /// Рассчитывает множитель уровня для характеристик
    /// </summary>
    float CalculateLevelMultiplier(int level)
    {
        // Экспоненциальный рост с замедлением
        return 1f + (level - 1) * 0.3f + Mathf.Pow(level - 1, 1.5f) * 0.1f;
    }
    
    /// <summary>
    /// Получает тип монстра по категории
    /// </summary>
    MonsterTypeConfig GetMonsterType(MonsterCategory category)
    {
        foreach (var monsterType in monsterTypes)
        {
            if (monsterType.category == category)
                return monsterType;
        }
        return null;
    }
    
    /// <summary>
    /// Генерирует случайного монстра для определенного уровня
    /// </summary>
    public GameObject GenerateRandomMonster(int level)
    {
        List<MonsterTypeConfig> availableTypes = new List<MonsterTypeConfig>();
        
        foreach (var monsterType in monsterTypes)
        {
            if (level >= monsterType.minLevel && level <= monsterType.maxLevel)
            {
                availableTypes.Add(monsterType);
            }
        }
        
        if (availableTypes.Count == 0)
        {
            Debug.LogWarning($"No monster types available for level {level}");
            return null;
        }
        
        MonsterTypeConfig randomType = availableTypes[Random.Range(0, availableTypes.Count)];
        return GenerateMonster(randomType.category, level);
    }
    
    /// <summary>
    /// Генерирует группу монстров для определенного уровня
    /// </summary>
    public void GenerateMonsterGroup(int level, int count)
    {
        Debug.Log($"Generating {count} monsters for level {level}");
        
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(-10f, 10f),
                0f,
                Random.Range(-10f, 10f)
            );
            
            GameObject monster = GenerateRandomMonster(level);
            if (monster != null)
            {
                monster.transform.position = randomPosition;
            }
        }
    }
    
    // Контекстное меню для тестирования
    [ContextMenu("Generate Test Monsters")]
    void GenerateTestMonsters()
    {
        Debug.Log("Generating test monsters...");
        
        // Генерируем по одному монстру каждого типа для уровней 1, 5, 10, 20, 50
        int[] testLevels = { 1, 5, 10, 20, 50 };
        
        foreach (int level in testLevels)
        {
            foreach (MonsterCategory category in System.Enum.GetValues(typeof(MonsterCategory)))
            {
                GameObject monster = GenerateMonster(category, level);
                if (monster != null)
                {
                    monster.transform.position = new Vector3(
                        (int)category * 4f,
                        0f,
                        level * 0.5f
                    );
                }
            }
        }
    }
    
    [ContextMenu("Generate Level 1-10 Monsters")]
    void GenerateLevel1To10Monsters()
    {
        Debug.Log("Generating monsters for levels 1-10...");
        
        for (int level = 1; level <= 10; level++)
        {
            foreach (MonsterCategory category in System.Enum.GetValues(typeof(MonsterCategory)))
            {
                GameObject monster = GenerateMonster(category, level);
                if (monster != null)
                {
                    monster.transform.position = new Vector3(
                        (int)category * 3f,
                        0f,
                        level * 3f
                    );
                }
            }
        }
    }
}
