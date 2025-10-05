using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Простой генератор монстров с визуальными 3D боксами
/// </summary>
public class MonsterGenerator : MonoBehaviour
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
    public class MonsterType
    {
        [Header("Basic Info")]
        public string name;
        public MonsterCategory category;
        public int minLevel;
        public int maxLevel;
        
        [Header("Base Stats")]
        public int baseHp;
        public int baseMinAttack;
        public int baseMaxAttack;
        public int baseDefense;
        public int basePhysicalResistance;
        public int baseHitRate;
        public int baseDodge;
        public int baseAttackSpeed;
        public int baseMovementSpeed;
        public int baseAttackRange;
        
        [Header("Stat Multipliers")]
        [Range(0.5f, 2f)] public float hpMultiplier = 1f;
        [Range(0.5f, 2f)] public float attackMultiplier = 1f;
        [Range(0.5f, 2f)] public float defenseMultiplier = 1f;
        [Range(0.5f, 2f)] public float speedMultiplier = 1f;
        [Range(0.5f, 2f)] public float dodgeMultiplier = 1f;
        
        [Header("Visual")]
        public Vector3 boxSize = Vector3.one;
        public Color boxColor = Color.white;
    }
    
    public enum MonsterCategory
    {
        Tank,        // Толстый моб - повышенная защита, пониженный уворот, медленная скорость атаки, сильный урон
        Fast,        // Быстрый моб - пониженная защита, повышенный уворот, нормальная скорость бега, средняя скорость атаки, малый урон
        Magic,       // Моб для магических классов - повышенная защита, повышенная физическая защита, медленная скорость, сильный урон, повышенная уворот
        Ranged       // Моб с дальней атакой - средний во всем, но быстрее остальных
    }
    
    [Header("Monster Types")]
    public List<MonsterType> monsterTypes = new List<MonsterType>();
    
    void Start()
    {
        InitializeMonsterTypes();
    }
    
    /// <summary>
    /// Инициализирует типы монстров на основе анализа примеров
    /// </summary>
    void InitializeMonsterTypes()
    {
        monsterTypes.Clear();
        
        // Толстый моб (Tank) - синий бокс
        monsterTypes.Add(new MonsterType
        {
            name = "Tank Monster",
            category = MonsterCategory.Tank,
            minLevel = 1,
            maxLevel = 5,
            baseHp = 100,
            baseMinAttack = 15,
            baseMaxAttack = 20,
            baseDefense = 8,
            basePhysicalResistance = 4,
            baseHitRate = 12,
            baseDodge = 8,
            baseAttackSpeed = 2500,
            baseMovementSpeed = 200,
            baseAttackRange = 150,
            hpMultiplier = 1.5f,
            attackMultiplier = 1.3f,
            defenseMultiplier = 1.4f,
            speedMultiplier = 0.7f,
            dodgeMultiplier = 0.6f,
            boxSize = new Vector3(1.2f, 1.0f, 1.2f),
            boxColor = Color.blue
        });
        
        // Быстрый моб (Fast) - красный бокс
        monsterTypes.Add(new MonsterType
        {
            name = "Fast Monster",
            category = MonsterCategory.Fast,
            minLevel = 1,
            maxLevel = 5,
            baseHp = 70,
            baseMinAttack = 10,
            baseMaxAttack = 15,
            baseDefense = 4,
            basePhysicalResistance = 2,
            baseHitRate = 15,
            baseDodge = 18,
            baseAttackSpeed = 1500,
            baseMovementSpeed = 350,
            baseAttackRange = 120,
            hpMultiplier = 0.7f,
            attackMultiplier = 0.8f,
            defenseMultiplier = 0.6f,
            speedMultiplier = 1.4f,
            dodgeMultiplier = 1.5f,
            boxSize = new Vector3(0.8f, 0.8f, 0.8f),
            boxColor = Color.red
        });
        
        // Магический моб (Magic) - фиолетовый бокс
        monsterTypes.Add(new MonsterType
        {
            name = "Magic Monster",
            category = MonsterCategory.Magic,
            minLevel = 1,
            maxLevel = 5,
            baseHp = 90,
            baseMinAttack = 18,
            baseMaxAttack = 25,
            baseDefense = 10,
            basePhysicalResistance = 6,
            baseHitRate = 14,
            baseDodge = 16,
            baseAttackSpeed = 2000,
            baseMovementSpeed = 180,
            baseAttackRange = 200,
            hpMultiplier = 1.2f,
            attackMultiplier = 1.4f,
            defenseMultiplier = 1.3f,
            speedMultiplier = 0.8f,
            dodgeMultiplier = 1.2f,
            boxSize = new Vector3(1.0f, 1.2f, 1.0f),
            boxColor = Color.magenta
        });
        
        // Дальний моб (Ranged) - желтый бокс
        monsterTypes.Add(new MonsterType
        {
            name = "Ranged Monster",
            category = MonsterCategory.Ranged,
            minLevel = 1,
            maxLevel = 5,
            baseHp = 80,
            baseMinAttack = 12,
            baseMaxAttack = 18,
            baseDefense = 6,
            basePhysicalResistance = 3,
            baseHitRate = 16,
            baseDodge = 12,
            baseAttackSpeed = 1800,
            baseMovementSpeed = 300,
            baseAttackRange = 300,
            hpMultiplier = 1.0f,
            attackMultiplier = 1.0f,
            defenseMultiplier = 1.0f,
            speedMultiplier = 1.2f,
            dodgeMultiplier = 1.0f,
            boxSize = new Vector3(0.9f, 1.4f, 0.9f),
            boxColor = Color.yellow
        });
    }
    
    /// <summary>
    /// Генерирует монстра определенного типа и уровня
    /// </summary>
    public GameObject GenerateMonster(MonsterCategory category, int level)
    {
        MonsterType monsterType = GetMonsterType(category);
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
        
        // Настраиваем характеристики монстра
        ConfigureMonsterStats(monsterScript, monsterType, level);
        
        Debug.Log($"Generated {monsterType.name} Level {level} with category {category}");
        return monster;
    }
    
    /// <summary>
    /// Создает 3D бокс для визуализации типа монстра
    /// </summary>
    void CreateMonsterBox(GameObject monster, MonsterType monsterType)
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
    /// Настраивает характеристики монстра на основе типа и уровня
    /// </summary>
    void ConfigureMonsterStats(Monster monsterScript, MonsterType monsterType, int level)
    {
        // Рассчитываем характеристики с учетом уровня и множителей
        float levelMultiplier = CalculateLevelMultiplier(level);
        
        // HP растет экспоненциально с уровнем
        int hp = Mathf.RoundToInt(monsterType.baseHp * monsterType.hpMultiplier * levelMultiplier);
        
        // Урон растет линейно с уровнем
        int minAttack = Mathf.RoundToInt(monsterType.baseMinAttack * monsterType.attackMultiplier * levelMultiplier);
        int maxAttack = Mathf.RoundToInt(monsterType.baseMaxAttack * monsterType.attackMultiplier * levelMultiplier);
        
        // Защита растет медленнее
        int defense = Mathf.RoundToInt(monsterType.baseDefense * monsterType.defenseMultiplier * Mathf.Sqrt(levelMultiplier));
        int physicalResistance = Mathf.RoundToInt(monsterType.basePhysicalResistance * monsterType.defenseMultiplier * Mathf.Sqrt(levelMultiplier));
        
        // Скорости остаются относительно постоянными
        int hitRate = Mathf.RoundToInt(monsterType.baseHitRate * levelMultiplier);
        int dodge = Mathf.RoundToInt(monsterType.baseDodge * monsterType.dodgeMultiplier * levelMultiplier);
        
        // Скорости атаки и движения с множителями
        int attackSpeed = Mathf.RoundToInt(monsterType.baseAttackSpeed / monsterType.speedMultiplier);
        int movementSpeed = Mathf.RoundToInt(monsterType.baseMovementSpeed * monsterType.speedMultiplier);
        
        // Дальность атаки остается постоянной
        int attackRange = monsterType.baseAttackRange;
        
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
        
        Debug.Log($"Configured {monsterType.name} Level {level}:");
        Debug.Log($"  HP: {hp}, Attack: {minAttack}-{maxAttack}");
        Debug.Log($"  Defense: {defense}, Physical Resistance: {physicalResistance}");
        Debug.Log($"  Hit Rate: {hitRate}, Dodge: {dodge}");
        Debug.Log($"  Attack Speed: {attackSpeed}, Movement Speed: {movementSpeed}");
        Debug.Log($"  Attack Range: {attackRange}");
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
    MonsterType GetMonsterType(MonsterCategory category)
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
        List<MonsterType> availableTypes = new List<MonsterType>();
        
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
        
        MonsterType randomType = availableTypes[Random.Range(0, availableTypes.Count)];
        return GenerateMonster(randomType.category, level);
    }
    
    /// <summary>
    /// Генерирует группу монстров для определенного уровня
    /// </summary>
    public void GenerateMonsterGroup(int level, int count)
    {
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
        
        // Генерируем по одному монстру каждого типа для уровней 1-5
        for (int level = 1; level <= 5; level++)
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
