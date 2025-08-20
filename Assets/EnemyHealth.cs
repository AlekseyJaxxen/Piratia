using Mirror;
using UnityEngine;

[RequireComponent(typeof(DamageFlash))]
public class EnemyHealth : NetworkBehaviour
{
    [SyncVar]
    public int health = 100;

    private DamageFlash damageFlash;

    void Start()
    {
        damageFlash = GetComponent<DamageFlash>();
    }

    public void TakeDamage(int damage)
    {
        if (!isLocal) return;

        health -= damage;
        damageFlash.RpcFlashDamage();

        if (health <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}