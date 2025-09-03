// GroundEffect.cs (отдельный скрипт)
using Mirror;
using UnityEngine;
using System.Collections;

public class GroundEffect : NetworkBehaviour
{
    private float slowPercent;
    private float dur;
    private float rad;
    private PlayerTeam team;

    public void Init(float slow, float duration, float radius, PlayerTeam ownerTeam)
    {
        slowPercent = slow;
        dur = duration;
        rad = radius;
        team = ownerTeam;
        StartCoroutine(DestroyAfter(duration));
    }

    private void Update()
    {
        if (!isServer) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, rad);
        foreach (Collider col in hits)
        {
            // Check for PlayerCore
            PlayerCore player = col.GetComponent<PlayerCore>();
            if (player != null && player.team != team)
            {
                player.ApplySlow(slowPercent, 1f, 1);
                continue; // Skip to the next collider if a player is found
            }

            // Check for Monster
            Monster monster = col.GetComponent<Monster>();
            if (monster != null)
            {
                // You'll need a method on the Monster class to handle the debuff
                monster.ApplySlow(slowPercent, 1f, 1);
            }
        }
    }

    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }
}