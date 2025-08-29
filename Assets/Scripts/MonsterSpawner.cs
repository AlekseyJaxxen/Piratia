using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class MonsterSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private List<Transform> spawnPoints; // Изменено на List<Transform>
    [SerializeField] private float respawnInterval = 10f;
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
            SpawnMonster(point.position); // Используем position из Transform
        }
        InvokeRepeating(nameof(CheckAndRespawn), respawnInterval, respawnInterval);
        Debug.Log("[MonsterSpawner] Started spawning monsters");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        CancelInvoke(nameof(CheckAndRespawn));
        Debug.Log("[MonsterSpawner] Stopped spawning monsters");
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
        if (NavMesh.SamplePosition(position, out hit, 5f, NavMesh.AllAreas))
        {
            position = hit.position;
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
                Debug.LogError("[MonsterSpawner] Monster component missing on spawned monster!");
            }
            NetworkServer.Spawn(monster);
            spawnedMonsters.Add(monster);
            Debug.Log($"[MonsterSpawner] Spawned monster at {position}");
        }
        else
        {
            Debug.LogWarning($"[MonsterSpawner] Spawn point {position} is not on NavMesh. Monster not spawned.");
        }
    }

    [Server]
    private void CheckAndRespawn()
    {
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null || spawnedMonsters[i].GetComponent<HealthMonster>().CurrentHealth <= 0)
            {
                spawnedMonsters.RemoveAt(i);
                SpawnMonster(spawnPoints[i % spawnPoints.Count].position);
            }
        }
    }
}