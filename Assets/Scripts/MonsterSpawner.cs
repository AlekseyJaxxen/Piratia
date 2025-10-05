using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using System;

[System.Serializable]
public class SpawnConfig
{
    public int monsterId;
    public GameObject spawnCenter; // ИСПРАВЛЕНИЕ 1: GameObject вместо Vector3 для простоты понимания
    public float radius = 100f;
    public int count = 100;
    public float respawnTime = 60f; // в секундах
    [Header("Elite Settings")]
    [Range(0f, 100f)] public float eliteChance = 10f; // Процент Elite монстров
    [Header("Spawn Memory Settings")]
    public bool useSpawnMemory = true; // Использовать память о месте спавна для респавна
    public float spawnMemoryRadius = 5f; // Радиус вокруг оригинального места спавна
}

public class MonsterSpawner : NetworkBehaviour
{
    [SerializeField] public GameObject monsterPrefab; // сделан public
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private List<SpawnConfig> spawnConfigs = new List<SpawnConfig>(); // список монстров
    [SerializeField] private Transform chestSpawnPoint;
    [SerializeField] private Transform monstersContainer; // Основной контейнер для всех монстров
    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private GameObject spawnedChest;
    private Dictionary<SpawnConfig, List<GameObject>> spawnedPerConfig = new Dictionary<SpawnConfig, List<GameObject>>();
    private Dictionary<SpawnConfig, Coroutine> respawnCoroutines = new Dictionary<SpawnConfig, Coroutine>();
    private Dictionary<SpawnConfig, Transform> spawnRegionContainers = new Dictionary<SpawnConfig, Transform>(); // Контейнеры для каждого региона

    public static MonsterSpawner Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        
        // ИСПРАВЛЕНИЕ 2: Создаем контейнер если не назначен
        if (monstersContainer == null)
        {
            GameObject containerObj = new GameObject("MonstersContainer");
            monstersContainer = containerObj.transform;
            monstersContainer.parent = transform;
        }
        
        StartCoroutine(SpawnMonstersDelayed());
    }

    private IEnumerator SpawnMonstersDelayed()
    {
        yield return new WaitUntil(() => NavMesh.CalculateTriangulation().vertices.Length > 0 && GameObject.Find("TeamSelectionCanvas") != null);
        
        foreach (var config in spawnConfigs)
        {
            spawnedPerConfig[config] = new List<GameObject>();
            
            // Создаем контейнер для каждого spawn региона
            CreateSpawnRegionContainer(config);
            
            SpawnGroup(config);
            respawnCoroutines[config] = StartCoroutine(CheckAndRespawnGroup(config));
        }
        // Started spawning monsters
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        foreach (var coroutine in respawnCoroutines.Values)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        respawnCoroutines.Clear();
        
        // Очищаем контейнеры регионов
        foreach (var regionContainer in spawnRegionContainers.Values)
        {
            if (regionContainer != null)
            {
                Destroy(regionContainer.gameObject);
            }
        }
        spawnRegionContainers.Clear();
        
        foreach (var monster in spawnedMonsters)
        {
            if (monster != null)
            {
                NetworkServer.Destroy(monster);
            }
        }
        spawnedMonsters.Clear();
        spawnedPerConfig.Clear();
        if (spawnedChest != null)
        {
            NetworkServer.Destroy(spawnedChest);
        }
        // Stopped spawning monsters
    }
    
    /// <summary>
    /// Получает информацию о контейнерах spawn регионов
    /// </summary>
    public void LogSpawnRegionContainers()
    {
        Debug.Log("=== Spawn Region Containers ===");
        Debug.Log($"Total regions: {spawnRegionContainers.Count}");
        
        foreach (var kvp in spawnRegionContainers)
        {
            SpawnConfig config = kvp.Key;
            Transform container = kvp.Value;
            
            if (container != null)
            {
                int monsterCount = container.childCount;
                Debug.Log($"Region: {container.name}");
                Debug.Log($"  - Monster ID: {config.monsterId}");
                Debug.Log($"  - Spawn Center: {(config.spawnCenter != null ? config.spawnCenter.name : "NULL")}");
                Debug.Log($"  - Monster Count: {monsterCount}");
                Debug.Log($"  - Elite Chance: {config.eliteChance}%");
            }
        }
    }
    
    /// <summary>
    /// Создает контейнер для spawn региона
    /// </summary>
    private void CreateSpawnRegionContainer(SpawnConfig config)
    {
        if (config.spawnCenter == null)
        {
            Debug.LogError($"[MonsterSpawner] Cannot create container: SpawnCenter is null for monster ID {config.monsterId}!");
            return;
        }
        
        // Создаем имя контейнера на основе spawn center
        string containerName = $"SpawnRegion_{config.spawnCenter.name}_MonsterID_{config.monsterId}";
        
        // Создаем контейнер
        GameObject regionContainer = new GameObject(containerName);
        Transform regionTransform = regionContainer.transform;
        regionTransform.SetParent(monstersContainer);
        
        // Сохраняем ссылку на контейнер
        spawnRegionContainers[config] = regionTransform;
        
        Debug.Log($"[MonsterSpawner] Created spawn region container: {containerName}");
    }
    
    [Server]
    private void SpawnGroup(SpawnConfig config)
    {
        int toSpawn = config.count - spawnedPerConfig[config].Count;
        MonsterDatabase db = Resources.Load<MonsterDatabase>("MonsterData/MonsterDatabase"); // путь к вашим SO
        
        // ИСПРАВЛЕНИЕ 1: Проверяем что spawnCenter назначен
        if (config.spawnCenter == null)
        {
            Debug.LogError($"[MonsterSpawner] SpawnCenter is null for monster ID {config.monsterId}!");
            return;
        }
        
        Vector3 spawnPosition = config.spawnCenter.transform.position;
        
        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-config.radius, config.radius), 0f, UnityEngine.Random.Range(-config.radius, config.radius));
            Vector3 position = spawnPosition + offset;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, config.radius, NavMesh.AllAreas))
            {
                position = hit.position;
                GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
                
                // Помещаем в контейнер региона
                Transform regionContainer = spawnRegionContainers[config];
                if (regionContainer != null)
                {
                    monster.transform.SetParent(regionContainer);
                }
                else
                {
                    // Fallback на основной контейнер
                    monster.transform.SetParent(monstersContainer);
                    Debug.LogWarning($"[MonsterSpawner] Region container not found for config {config.monsterId}, using main container");
                }
                
                Monster monsterScript = monster.GetComponent<Monster>();
                if (monsterScript != null)
                {
                    if (db != null && config.monsterId - 1 < db.monsters.Count)
                    {
                        monsterScript.monsterId = config.monsterId;
                        
                        // ДИАГНОСТИКА: Логируем назначение ID
                        Debug.Log($"[MonsterSpawner] Assigned monsterId {config.monsterId} to monster {monster.name}");
                        
                        // Определяем, будет ли монстр Elite
                        float roll = UnityEngine.Random.Range(0f, 100f);
                        if (roll <= config.eliteChance)
                        {
                            monsterScript.isElite = true;
                            Debug.Log($"[MonsterSpawner] Elite monster spawned: {db.monsters[config.monsterId - 1].monsterName}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MonsterSpawner] No MonsterInfo for ID {config.monsterId}");
                        Debug.LogError($"[MonsterSpawner] Database has {db?.monsters.Count ?? 0} monsters");
                        Destroy(monster);
                        continue;
                    }
                }
                NavMeshAgent agent = monster.GetComponent<NavMeshAgent>();
                if (agent == null || !agent.isOnNavMesh)
                {
                    Debug.LogError($"[MonsterSpawner] Monster at {position} failed to initialize on NavMesh!");
                    Destroy(monster);
                    continue;
                }
                NetworkServer.Spawn(monster);
                spawnedMonsters.Add(monster);
                spawnedPerConfig[config].Add(monster);
                // Monster spawned
            }
            else
            {
                Debug.LogError($"[MonsterSpawner] Spawn point {position} is not on NavMesh!");
            }
        }
    }

   
    [Server]
    private IEnumerator CheckAndRespawnGroup(SpawnConfig config)
    {
        while (true)
        {
            yield return new WaitForSeconds(config.respawnTime);
            spawnedPerConfig[config].RemoveAll(m => m == null || m.GetComponent<HealthMonster>() == null || m.GetComponent<HealthMonster>().CurrentHealth <= 0);
            if (spawnedPerConfig[config].Count < config.count)
            {
                SpawnGroup(config);
            }
            // Group status updated
        }
    }
    
    // Context Menu для тестирования
    [ContextMenu("Log Spawn Region Containers")]
    void LogSpawnRegionContainersMenu()
    {
        LogSpawnRegionContainers();
    }
    
    [ContextMenu("Create All Spawn Region Containers")]
    void CreateAllSpawnRegionContainers()
    {
        Debug.Log("Creating all spawn region containers...");
        foreach (var config in spawnConfigs)
        {
            CreateSpawnRegionContainer(config);
        }
    }
}
