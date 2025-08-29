using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class MonsterSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject[] monsterPrefabs;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private Transform chestSpawnPoint;
    [SerializeField] private float respawnInterval = 10f;
    [SerializeField] private int maxMonsters = 5;
    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private GameObject spawnedChest;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(SpawnMonstersDelayed());
    }

    private IEnumerator SpawnMonstersDelayed()
    {
        yield return new WaitUntil(() => NavMesh.CalculateTriangulation().vertices.Length > 0 && GameObject.Find("TeamSelectionCanvas") != null);
        SpawnChest();
        foreach (var point in spawnPoints)
        {
            SpawnMonster(point.position);
        }
        InvokeRepeating(nameof(CheckAndRespawn), respawnInterval, respawnInterval);
        Debug.Log("[MonsterSpawner] Started spawning monsters");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        CancelInvoke(nameof(CheckAndRespawn));
        foreach (var monster in spawnedMonsters)
        {
            if (monster != null)
            {
                NetworkServer.Destroy(monster);
            }
        }
        spawnedMonsters.Clear();
        if (spawnedChest != null)
        {
            NetworkServer.Destroy(spawnedChest);
        }
        Debug.Log("[MonsterSpawner] Stopped spawning monsters and cleared spawnedMonsters list");
    }

    [Server]
    private void SpawnMonster(Vector3 position)
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogError("[MonsterSpawner] Monster prefabs not assigned!");
            return;
        }
        GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 10f, NavMesh.AllAreas))
        {
            position = hit.position;
            Debug.Log($"[MonsterSpawner] Adjusted spawn position to NavMesh: {position}");
            GameObject monster = Instantiate(prefab, position, Quaternion.identity);
            NavMeshAgent agent = monster.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh)
            {
                Debug.LogError($"[MonsterSpawner] Monster at {position} failed to initialize on NavMesh!");
                Destroy(monster);
                return;
            }
            NetworkServer.Spawn(monster);
            spawnedMonsters.Add(monster);
            Debug.Log($"[MonsterSpawner] Spawned monster at {position}");
        }
        else
        {
            Debug.LogError($"[MonsterSpawner] Spawn point {position} is not on NavMesh!");
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
    private void CheckAndRespawn()
    {
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null ||
                spawnedMonsters[i].GetComponent<HealthMonster>() == null ||
                spawnedMonsters[i].GetComponent<HealthMonster>().CurrentHealth <= 0)
            {
                spawnedMonsters.RemoveAt(i);
            }
        }
        if (spawnedChest == null ||
            (spawnedChest.GetComponent<HealthMonster>() != null && spawnedChest.GetComponent<HealthMonster>().CurrentHealth <= 0))
        {
            SpawnChest();
        }
        if (spawnedMonsters.Count < maxMonsters && spawnPoints.Count > 0)
        {
            StartCoroutine(SpawnAfterDelay(spawnPoints[Random.Range(0, spawnPoints.Count)].position, 3.5f));
        }
        Debug.Log($"[MonsterSpawner] Active monsters: {spawnedMonsters.Count}");
    }

    [Server]
    private IEnumerator SpawnAfterDelay(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnMonster(position);
    }
}