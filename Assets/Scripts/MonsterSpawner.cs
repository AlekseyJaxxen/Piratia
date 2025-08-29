using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class MonsterSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private float respawnInterval = 10f;
    [SerializeField] private int maxMonsters = 5;
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(SpawnMonstersDelayed());
    }

    private IEnumerator SpawnMonstersDelayed()
    {
        yield return new WaitUntil(() => NavMesh.CalculateTriangulation().vertices.Length > 0 && GameObject.Find("TeamSelectionCanvas") != null);
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
        Debug.Log("[MonsterSpawner] Stopped spawning monsters and cleared spawnedMonsters list");
    }

    [Server]
    private void SpawnMonster(Vector3 position)
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[MonsterSpawner] Monster prefab not assigned!");
            return;
        }
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 10f, NavMesh.AllAreas))
        {
            position = hit.position;
            Debug.Log($"[MonsterSpawner] Adjusted spawn position to NavMesh: {position}");
            GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
            Monster monsterComp = monster.GetComponent<Monster>();
            if (monsterComp != null)
            {
                monsterComp.monsterName = $"Monster_{Random.Range(1000, 9999)}";
                monsterComp.maxHealth = 1000;
                monsterComp.currentHealth = monsterComp.maxHealth;
            }
            else
            {
                Debug.LogError("[MonsterSpawner] Monster component missing!");
                Destroy(monster);
                return;
            }
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