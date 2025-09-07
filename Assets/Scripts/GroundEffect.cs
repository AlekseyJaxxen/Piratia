using Mirror;
using UnityEngine;
using System.Collections;

public class GroundEffect : NetworkBehaviour
{
    private float slowPercent;
    private float dur;
    private float rad;
    private PlayerTeam team;
    private int layerMask; // Добавляем маску слоев

    public void Init(float slow, float duration, float radius, PlayerTeam ownerTeam, int layerMask)
    {
        slowPercent = slow;
        dur = duration;
        rad = radius;
        team = ownerTeam;
        this.layerMask = layerMask; // Сохраняем маску
        StartCoroutine(DestroyAfter(duration));
    }

    private void Update()
    {
        if (!isServer) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, rad, layerMask);
        foreach (Collider col in hits)
        {
            PlayerCore player = col.GetComponent<PlayerCore>();
            if (player != null && player.team != team)
            {
                player.ApplySlow(slowPercent, 1f, 1);
                continue;
            }
            Monster monster = col.GetComponent<Monster>();
            if (monster != null)
            {
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