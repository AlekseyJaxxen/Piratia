using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MonsterSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private List<Vector3> spawnPoints;
    [SerializeField] private float respawnInterval = 10f;

    private List<GameObject> spawnedMonsters = new List<GameObject>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var point in spawnPoints)
        {
            SpawnMonster(point);
        }
        InvokeRepeating(nameof(CheckAndRespawn), respawnInterval, respawnInterval);
    }

    [Server]
    private void SpawnMonster(Vector3 position)
    {
        GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
        NetworkServer.Spawn(monster);
        spawnedMonsters.Add(monster);
        Debug.Log($"[MonsterSpawner] Spawned monster at {position}");
    }

    [Server]
    private void CheckAndRespawn()
    {
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null || spawnedMonsters[i].GetComponent<Health>().CurrentHealth <= 0)
            {
                spawnedMonsters.RemoveAt(i);
                SpawnMonster(spawnPoints[i % spawnPoints.Count]);
            }
        }
    }
}