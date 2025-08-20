using Mirror;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public GameObject enemyPrefab;
    public int maxEnemies = 5;

    public override void OnStartServer()
    {
        for (int i = 0; i < maxEnemies; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(enemy); // Важно для синхронизации
        }
    }
}