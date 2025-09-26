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
    public Vector3 position;
    public float radius = 100f;
    public int count = 100;
    public float respawnTime = 60f; // В секундах
}

public class MonsterSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject monsterPrefab; // Один префаб Monster
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private List<SpawnConfig> spawnConfigs = new List<SpawnConfig>(); // Список конфигов
    [SerializeField] private Transform chestSpawnPoint;
    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private GameObject spawnedChest;
    private Transform monstersContainer;
    private Dictionary<SpawnConfig, List<GameObject>> spawnedPerConfig = new Dictionary<SpawnConfig, List<GameObject>>();
    private Dictionary<SpawnConfig, Coroutine> respawnCoroutines = new Dictionary<SpawnConfig, Coroutine>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        monstersContainer = new GameObject("MonstersContainer").transform;
        monstersContainer.parent = transform;
        StartCoroutine(SpawnMonstersDelayed());
    }

    private IEnumerator SpawnMonstersDelayed()
    {
        yield return new WaitUntil(() => NavMesh.CalculateTriangulation().vertices.Length > 0 && GameObject.Find("TeamSelectionCanvas") != null);
        SpawnChest();
        foreach (var config in spawnConfigs)
        {
            spawnedPerConfig[config] = new List<GameObject>();
            SpawnGroup(config);
            respawnCoroutines[config] = StartCoroutine(CheckAndRespawnGroup(config));
        }
        Debug.Log("[MonsterSpawner] Started spawning monsters");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        foreach (var coroutine in respawnCoroutines.Values)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        respawnCoroutines.Clear();
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
        Debug.Log("[MonsterSpawner] Stopped spawning monsters and cleared spawnedMonsters list");
    }

    [Server]
    private void SpawnGroup(SpawnConfig config)
    {
        int toSpawn = config.count - spawnedPerConfig[config].Count;
        MonsterDatabase db = Resources.Load<MonsterDatabase>("MonsterData/MonsterDatabase"); // Путь к твоему SO
        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-config.radius, config.radius), 0f, UnityEngine.Random.Range(-config.radius, config.radius));
            Vector3 position = config.position + offset;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, config.radius, NavMesh.AllAreas))
            {
                position = hit.position;
                GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
                monster.transform.SetParent(monstersContainer);
                Monster monsterScript = monster.GetComponent<Monster>();
                if (monsterScript != null)
                {
                    if (db != null && config.monsterId - 1 < db.monsters.Count)
                    {
                        monsterScript.monsterId = config.monsterId; // Изменено: присваиваем ID вместо info
                    }
                    else
                    {
                        Debug.LogError($"[MonsterSpawner] No MonsterInfo for ID {config.monsterId}");
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
                Debug.Log($"[MonsterSpawner] Spawned monster ID {config.monsterId} at {position}");
            }
            else
            {
                Debug.LogError($"[MonsterSpawner] Spawn point {position} is not on NavMesh!");
            }
        }
    }

    [Server]
    private void SpawnChest()
    {
        if (chestPrefab == null || chestSpawnPoint == null)
        {
            Debug.LogError("[MonsterSpawner] Chest prefab or spawn point not assigned!");
            return;
        }
        Vector3 position = chestSpawnPoint.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 10f, NavMesh.AllAreas))
        {
            position = hit.position;
        }
        spawnedChest = Instantiate(chestPrefab, position, Quaternion.identity);
        NavMeshAgent agent = spawnedChest.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false; // Make immobile
        }
        NetworkServer.Spawn(spawnedChest);
        Debug.Log($"[MonsterSpawner] Spawned chest at {position}");
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
            Debug.Log($"[MonsterSpawner] Group {config.monsterId}: Active {spawnedPerConfig[config].Count}/{config.count}");
        }
    }
}