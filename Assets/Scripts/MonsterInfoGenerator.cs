using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Генератор MonsterInfo ScriptableObject на основе типов монстров
/// </summary>
public class MonsterInfoGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public string outputFolder = "Assets/Resources/MonsterData/Generated";
    public GameObject defaultModelPrefab;
    public MonsterBasicAttackSkill defaultBasicAttackSkill; // ОДИН скилл на всех монстров (как у игроков)
    public GameObject defaultDeathVFXPrefab;
    public GameObject defaultDroppedItemPrefab;
    
    [Header("Temporary Box Settings")]
    public bool useTemporaryBoxes = false; // Если true, создает временные box вместо modelPrefab
    public MonsterBoxPrefabGenerator boxGenerator;
    
    [System.Serializable]
    public class MonsterTemplate
    {
        [Header("Basic Info")]
        public string name;
        public MonsterCategory category;
        public int minLevel;
        public int maxLevel;
        public string description;
        
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
        public int baseExperience;
        
        [Header("Stat Multipliers")]
        [Range(0.5f, 2f)] public float hpMultiplier = 1f;
        [Range(0.5f, 2f)] public float attackMultiplier = 1f;
        [Range(0.5f, 2f)] public float defenseMultiplier = 1f;
        [Range(0.5f, 2f)] public float speedMultiplier = 1f;
        [Range(0.5f, 2f)] public float dodgeMultiplier = 1f;
        [Range(0.5f, 2f)] public float hitRateMultiplier = 1f;
        
        [Header("Visual Settings")]
        public GameObject modelPrefab;
        public Vector3 modelScale = Vector3.one;
        public Color monsterColor = Color.white;
        
        [Header("AI Settings")]
        public float patrolRadius = 10f;
        public float detectionRange = 10f;
        public float chaseTimeout = 30f;
        
        [Header("Skills")]
        public List<MonsterSkillTemplate> skills = new List<MonsterSkillTemplate>();
    }
    
    [System.Serializable]
    public class MonsterSkillTemplate
    {
        public SkillBase skill;
        public float cooldown = 5f;
        public float useChance = 0.3f;
        public float minHealthPercentage = 0.5f;
        public float maxHealthPercentage = 1f;
        public float minDistance = 0f;
        public float maxDistance = 10f;
        public bool requiresTarget = true;
    }
    
    public enum MonsterCategory
    {
        Tank,        // Толстый моб - повышенная защита, пониженный уворот, медленная скорость атаки, сильный урон
        Fast,        // Быстрый моб - пониженная защита, повышенный уворот, нормальная скорость бега, средняя скорость атаки, малый урон
        Magic,       // Моб для магических классов - повышенная защита, повышенная физическая защита, медленная скорость, сильный урон, повышенная уворот
        Ranged       // Моб с дальней атакой - средний во всем, но быстрее остальных
    }
    
    [Header("Monster Templates")]
    public List<MonsterTemplate> monsterTemplates = new List<MonsterTemplate>();
    
    void Start()
    {
        InitializeTemplates();
    }
    
    /// <summary>
    /// Инициализирует шаблоны монстров
    /// </summary>
    public void InitializeTemplates()
    {
        monsterTemplates.Clear();
        
        // Tank Monster Template
        monsterTemplates.Add(new MonsterTemplate
        {
            name = "TankMonster",
            category = MonsterCategory.Tank,
            minLevel = 1,
            maxLevel = 100,
            description = "Толстый моб с высокой защитой и уроном, но медленный",
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
            baseExperience = 50,
            hpMultiplier = 1.5f,
            attackMultiplier = 1.2f,
            defenseMultiplier = 1.4f,
            speedMultiplier = 0.7f,
            dodgeMultiplier = 0.6f,
            hitRateMultiplier = 0.9f,
            modelScale = new Vector3(1.2f, 1.0f, 1.2f),
            monsterColor = Color.blue,
            patrolRadius = 8f,
            detectionRange = 12f,
            chaseTimeout = 25f
        });
        
        // Fast Monster Template
        monsterTemplates.Add(new MonsterTemplate
        {
            name = "FastMonster",
            category = MonsterCategory.Fast,
            minLevel = 1,
            maxLevel = 100,
            description = "Быстрый моб с высоким уворотом, но низкой защитой",
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
            baseExperience = 40,
            hpMultiplier = 0.7f,
            attackMultiplier = 0.8f,
            defenseMultiplier = 0.6f,
            speedMultiplier = 1.4f,
            dodgeMultiplier = 1.5f,
            hitRateMultiplier = 1.2f,
            modelScale = new Vector3(0.8f, 0.8f, 0.8f),
            monsterColor = Color.red,
            patrolRadius = 12f,
            detectionRange = 15f,
            chaseTimeout = 20f
        });
        
        // Magic Monster Template
        monsterTemplates.Add(new MonsterTemplate
        {
            name = "MagicMonster",
            category = MonsterCategory.Magic,
            minLevel = 1,
            maxLevel = 100,
            description = "Магический моб с высоким уроном и защитой",
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
            baseExperience = 60,
            hpMultiplier = 1.2f,
            attackMultiplier = 1.4f,
            defenseMultiplier = 1.3f,
            speedMultiplier = 0.8f,
            dodgeMultiplier = 1.2f,
            hitRateMultiplier = 1.1f,
            modelScale = new Vector3(1.0f, 1.2f, 1.0f),
            monsterColor = Color.magenta,
            patrolRadius = 6f,
            detectionRange = 18f,
            chaseTimeout = 35f
        });
        
        // Ranged Monster Template
        monsterTemplates.Add(new MonsterTemplate
        {
            name = "RangedMonster",
            category = MonsterCategory.Ranged,
            minLevel = 1,
            maxLevel = 100,
            description = "Дальний моб со сбалансированными характеристиками",
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
            baseExperience = 55,
            hpMultiplier = 1.0f,
            attackMultiplier = 1.0f,
            defenseMultiplier = 1.0f,
            speedMultiplier = 1.2f,
            dodgeMultiplier = 1.0f,
            hitRateMultiplier = 1.3f,
            modelScale = new Vector3(0.9f, 1.4f, 0.9f),
            monsterColor = Color.yellow,
            patrolRadius = 10f,
            detectionRange = 20f,
            chaseTimeout = 30f
        });
        
        // Mushroom Mob Template (на основе вашего примера)
        monsterTemplates.Add(new MonsterTemplate
        {
            name = "MushroomMob",
            category = MonsterCategory.Tank,
            minLevel = 1,
            maxLevel = 10,
            description = "Гриб-монстр с высокой защитой и средним уроном",
            baseHp = 120,
            baseMinAttack = 12,
            baseMaxAttack = 18,
            baseDefense = 10,
            basePhysicalResistance = 5,
            baseHitRate = 10,
            baseDodge = 6,
            baseAttackSpeed = 2000,
            baseMovementSpeed = 150,
            baseAttackRange = 120,
            baseExperience = 30,
            hpMultiplier = 1.3f,
            attackMultiplier = 1.0f,
            defenseMultiplier = 1.5f,
            speedMultiplier = 0.6f,
            dodgeMultiplier = 0.5f,
            hitRateMultiplier = 0.8f,
            modelScale = new Vector3(1.1f, 1.1f, 1.1f),
            monsterColor = Color.green,
            patrolRadius = 5f,
            detectionRange = 8f,
            chaseTimeout = 40f
        });
    }
    
    /// <summary>
    /// Генерирует MonsterInfo для определенного уровня
    /// </summary>
    public MonsterInfo GenerateMonsterInfo(MonsterCategory category, int level)
    {
        // Убеждаемся, что шаблоны инициализированы
        if (monsterTemplates == null || monsterTemplates.Count == 0)
        {
            InitializeTemplates();
        }
        
        MonsterTemplate template = GetTemplate(category);
        if (template == null)
        {
            Debug.LogError($"Template for category {category} not found!");
            Debug.LogError($"Available templates: {monsterTemplates.Count}");
            foreach (var t in monsterTemplates)
            {
                Debug.LogError($"  - {t.name} ({t.category})");
            }
            return null;
        }
        
        if (level < template.minLevel || level > template.maxLevel)
        {
            Debug.LogWarning($"Level {level} is outside range for {template.name} ({template.minLevel}-{template.maxLevel})");
        }
        
        // Создаем новый MonsterInfo
        MonsterInfo monsterInfo = ScriptableObject.CreateInstance<MonsterInfo>();
        
        // Настраиваем базовые параметры
        ConfigureMonsterInfo(monsterInfo, template, level);
        
        return monsterInfo;
    }
    
    /// <summary>
    /// Настраивает MonsterInfo на основе шаблона и уровня
    /// </summary>
    void ConfigureMonsterInfo(MonsterInfo monsterInfo, MonsterTemplate template, int level)
    {
        // Формулы на основе анализа примеров монстров
        float levelMultiplier = CalculateLevelMultiplier(level);
        
        // Базовые настройки
        monsterInfo.monsterName = $"{template.name}_Lv{level}";
        monsterInfo.monsterId = GetNextMonsterId();
        monsterInfo.aiType = "AI2";
        
        // HP формула: level^1.8 * 2.5 + level * 5
        monsterInfo.maxHealth = Mathf.RoundToInt((Mathf.Pow(level, 1.8f) * 2.5f + level * 5f) * template.hpMultiplier);
        
        // Attack формула: level^1.2 * 1.5 + level * 2 (ИСПРАВЛЕНИЕ 3: добавляем minAttack и maxAttack)
        monsterInfo.minAttack = Mathf.RoundToInt((Mathf.Pow(level, 1.2f) * 1.5f + level * 2f) * template.attackMultiplier);
        monsterInfo.maxAttack = Mathf.RoundToInt(monsterInfo.minAttack * 1.2f);
        
        // Defense формула: level^1.5 * 0.8 + level * 1.5 (ИСПРАВЛЕНИЕ 3: добавляем defense)
        monsterInfo.defense = Mathf.RoundToInt((Mathf.Pow(level, 1.5f) * 0.8f + level * 1.5f) * template.defenseMultiplier);
        
        // Physical Resistance формула: level^1.3 * 0.3 + level * 0.5 (ИСПРАВЛЕНИЕ 3: добавляем physicalResistance)
        monsterInfo.physicalResistance = Mathf.RoundToInt((Mathf.Pow(level, 1.3f) * 0.3f + level * 0.5f) * template.defenseMultiplier);
        
        // Hit Rate формула: level^1.1 * 2 + level * 3
        monsterInfo.hitRate = Mathf.RoundToInt((Mathf.Pow(level, 1.1f) * 2f + level * 3f) * template.hitRateMultiplier);
        
        // Dodge формула: level^1.1 * 2 + level * 3
        monsterInfo.dodge = Mathf.RoundToInt((Mathf.Pow(level, 1.1f) * 2f + level * 3f) * template.dodgeMultiplier);
        
        // Скорости (ИСПРАВЛЕНИЕ 1: делим на 100)
        monsterInfo.moveSpeed = (template.baseMovementSpeed * template.speedMultiplier) / 100f;
        monsterInfo.attackCooldown = template.baseAttackSpeed / (1000f * template.speedMultiplier); // Конвертируем в секунды
        
        // Experience формула: level^2.2 * 0.5 + level * 2
        monsterInfo.experienceReward = Mathf.RoundToInt(Mathf.Pow(level, 2.2f) * 0.5f + level * 2f);
        
        // Дальность атаки (ИСПРАВЛЕНИЕ 3: добавляем attackRange)
        monsterInfo.attackRange = template.baseAttackRange / 100f; // Делим на 100 для соответствия масштабу игры
        
        // Projectile настройки (ИСПРАВЛЕНИЕ 4: добавляем projectile настройки)
        ConfigureProjectileSettings(monsterInfo, template, level);
        
        // AI настройки
        monsterInfo.patrolRadius = template.patrolRadius;
        monsterInfo.detectionRange = template.detectionRange;
        monsterInfo.chaseTimeout = template.chaseTimeout;
        
        // ИСПРАВЛЕНИЕ 5: Автоматически используем временные box
        monsterInfo.useTemporaryBox = true;
        monsterInfo.boxType = ConvertToMonsterBoxType(template.category);
        monsterInfo.boxColor = template.monsterColor;
        monsterInfo.boxSize = template.modelScale;
        monsterInfo.modelPrefab = null; // Не используем modelPrefab при временных box
        
        // ИСПРАВЛЕНИЕ 2: Назначаем basicAttackSkill (ОДИН на всех, как у игроков)
        if (defaultBasicAttackSkill != null)
        {
            monsterInfo.basicAttackSkill = defaultBasicAttackSkill;
            Debug.Log($"[MonsterInfoGenerator] Assigned basicAttackSkill '{defaultBasicAttackSkill.name}' to {monsterInfo.monsterName}");
            // Урон будет рассчитываться из MonsterInfo.minAttack/maxAttack в CalculateMonsterDamage()
        }
        else
        {
            Debug.LogWarning($"[MonsterInfoGenerator] defaultBasicAttackSkill is NULL! Cannot assign to {monsterInfo.monsterName}");
        }
        
        // ИСПРАВЛЕНИЕ 4: Назначаем droppedItemPrefab автоматически
        monsterInfo.deathVFXPrefab = defaultDeathVFXPrefab;
        monsterInfo.droppedItemPrefab = defaultDroppedItemPrefab;
        
        // Базовые настройки
        monsterInfo.canMove = true;
        monsterInfo.canAttack = true;
        
        // Collider настройки
        monsterInfo.scaleBoxCollider = true;
        monsterInfo.boxColliderScale = template.modelScale;
        
        // Elite настройки
        monsterInfo.isElite = false;
        monsterInfo.eliteStatMultiplier = 1.5f;
        monsterInfo.eliteModelScale = 1.5f;
        
        // Combined настройки
        monsterInfo.isCombined = false;
        monsterInfo.headAttackRange = template.baseAttackRange;
        monsterInfo.legsAttackRange = template.baseAttackRange * 0.8f;
        
        // Базовый скилл атаки уже назначен выше в ConfigureMonsterInfo
        
        // Настройка скиллов
        ConfigureMonsterSkills(monsterInfo, template, level);
        
        Debug.Log($"Generated MonsterInfo: {monsterInfo.monsterName}");
        Debug.Log($"  HP: {monsterInfo.maxHealth}, Attack: {monsterInfo.minAttack}-{monsterInfo.maxAttack}");
        Debug.Log($"  Defense: {monsterInfo.defense}, Physical Resistance: {monsterInfo.physicalResistance}");
        Debug.Log($"  Hit Rate: {monsterInfo.hitRate}, Dodge: {monsterInfo.dodge}");
        Debug.Log($"  Speed: {monsterInfo.moveSpeed}, Attack Cooldown: {monsterInfo.attackCooldown}");
        Debug.Log($"  Attack Range: {monsterInfo.attackRange}");
        Debug.Log($"  Experience: {monsterInfo.experienceReward}");
    }
    
    /// <summary>
    /// Настраивает projectile параметры для монстра
    /// </summary>
    void ConfigureProjectileSettings(MonsterInfo monsterInfo, MonsterTemplate template, int level)
    {
        // Ranged монстры используют projectile
        if (template.category == MonsterCategory.Ranged)
        {
            monsterInfo.useProjectile = true;
            monsterInfo.projectileSpeed = 15f + (level * 0.5f); // Скорость растет с уровнем
            monsterInfo.projectilePrefab = null; // Будет назначен вручную или через систему
            
            Debug.Log($"[MonsterInfoGenerator] {monsterInfo.monsterName} configured as ranged monster with projectile speed: {monsterInfo.projectileSpeed}");
        }
        else
        {
            monsterInfo.useProjectile = false;
            monsterInfo.projectileSpeed = 10f; // Базовое значение
            monsterInfo.projectilePrefab = null;
        }
    }
    
    /// <summary>
    /// Настраивает скиллы монстра
    /// </summary>
    void ConfigureMonsterSkills(MonsterInfo monsterInfo, MonsterTemplate template, int level)
    {
        monsterInfo.monsterSkills.Clear();
        
        foreach (var skillTemplate in template.skills)
        {
            if (skillTemplate.skill != null)
            {
                MonsterSkillEntry skillEntry = new MonsterSkillEntry
                {
                    skill = skillTemplate.skill,
                    cooldown = skillTemplate.cooldown,
                    useChance = skillTemplate.useChance,
                    minHealthPercentage = skillTemplate.minHealthPercentage,
                    maxHealthPercentage = skillTemplate.maxHealthPercentage,
                    minDistance = skillTemplate.minDistance,
                    maxDistance = skillTemplate.maxDistance,
                    requiresTarget = skillTemplate.requiresTarget
                };
                
                monsterInfo.monsterSkills.Add(skillEntry);
            }
        }
    }
    
    /// <summary>
    /// Рассчитывает множитель уровня для характеристик
    /// </summary>
    float CalculateLevelMultiplier(int level)
    {
        return 1f + (level - 1) * 0.3f + Mathf.Pow(level - 1, 1.5f) * 0.1f;
    }
    
    /// <summary>
    /// Получает следующий ID монстра
    /// </summary>
    int GetNextMonsterId()
    {
        // Простая реализация - в реальном проекте нужно проверять существующие ID
        return Random.Range(1000, 9999);
    }
    
    /// <summary>
    /// Получает шаблон по категории
    /// </summary>
    MonsterTemplate GetTemplate(MonsterCategory category)
    {
        foreach (var template in monsterTemplates)
        {
            if (template.category == category)
                return template;
        }
        return null;
    }
    
    /// <summary>
    /// Преобразует MonsterCategory в MonsterBoxType
    /// </summary>
    MonsterBoxType ConvertToMonsterBoxType(MonsterCategory category)
    {
        switch (category)
        {
            case MonsterCategory.Tank:
                return MonsterBoxType.Tank;
            case MonsterCategory.Fast:
                return MonsterBoxType.Fast;
            case MonsterCategory.Magic:
                return MonsterBoxType.Magic;
            case MonsterCategory.Ranged:
                return MonsterBoxType.Ranged;
            default:
                return MonsterBoxType.Tank;
        }
    }
    
    /// <summary>
    /// Генерирует MonsterInfo для диапазона уровней
    /// </summary>
    public void GenerateMonsterInfoRange(MonsterCategory category, int startLevel, int endLevel)
    {
        Debug.Log($"Generating MonsterInfo for {category} levels {startLevel}-{endLevel}");
        
        // Убеждаемся, что шаблоны инициализированы
        if (monsterTemplates == null || monsterTemplates.Count == 0)
        {
            InitializeTemplates();
        }
        
        for (int level = startLevel; level <= endLevel; level++)
        {
            MonsterInfo monsterInfo = GenerateMonsterInfo(category, level);
            if (monsterInfo != null)
            {
                // Сохраняем как asset (только в Editor)
                #if UNITY_EDITOR
                SaveMonsterInfoAsset(monsterInfo, category, level);
                #endif
            }
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Сохраняет MonsterInfo как asset (только в Editor)
    /// </summary>
    void SaveMonsterInfoAsset(MonsterInfo monsterInfo, MonsterCategory category, int level)
    {
        // Создаем папку если не существует
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        
        // Создаем имя файла
        string fileName = $"{category}_{level}";
        string assetPath = $"{outputFolder}/{fileName}.asset";
        
        // Сохраняем asset
        UnityEditor.AssetDatabase.CreateAsset(monsterInfo, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log($"Saved MonsterInfo: {assetPath}");
        
        // Автоматически добавляем в MonsterDatabase если есть Manager
        MonsterDatabaseManager dbManager = FindObjectOfType<MonsterDatabaseManager>();
        if (dbManager != null)
        {
            dbManager.OnMonsterInfoGenerated(monsterInfo);
        }
    }
    #endif
    
    // Контекстное меню для тестирования
    [ContextMenu("Generate All Monster Types Level 1-10")]
    void GenerateAllMonsterTypesLevel1To10()
    {
        Debug.Log("Generating all monster types for levels 1-10...");
        
        foreach (MonsterCategory category in System.Enum.GetValues(typeof(MonsterCategory)))
        {
            GenerateMonsterInfoRange(category, 1, 10);
        }
    }
    
    [ContextMenu("Generate MushroomMob Level 1-5")]
    void GenerateMushroomMobLevel1To5()
    {
        Debug.Log("Generating MushroomMob for levels 1-5...");
        
        MonsterTemplate mushroomTemplate = monsterTemplates.Find(t => t.name == "MushroomMob");
        if (mushroomTemplate != null)
        {
            for (int level = 1; level <= 5; level++)
            {
                MonsterInfo monsterInfo = GenerateMonsterInfo(mushroomTemplate.category, level);
                if (monsterInfo != null)
                {
                    #if UNITY_EDITOR
                    SaveMonsterInfoAsset(monsterInfo, mushroomTemplate.category, level);
                    #endif
                }
            }
        }
    }
    
    [ContextMenu("Generate Test MonsterInfo")]
    void GenerateTestMonsterInfo()
    {
        Debug.Log("Generating test MonsterInfo...");
        
        // Генерируем по одному MonsterInfo каждого типа для уровня 5
        foreach (MonsterCategory category in System.Enum.GetValues(typeof(MonsterCategory)))
        {
            MonsterInfo monsterInfo = GenerateMonsterInfo(category, 5);
            if (monsterInfo != null)
            {
                Debug.Log($"Generated {category} Level 5: {monsterInfo.monsterName}");
            }
        }
    }
}
