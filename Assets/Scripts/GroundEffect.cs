using Mirror;
using UnityEngine;
using System.Collections;

public class GroundEffect : NetworkBehaviour
{
    private float slowPercent;
    private float dur;
    private float rad;
    private PlayerTeam team;
    private int layerMask;

    public void Init(float slow, float duration, float radius, PlayerTeam ownerTeam, int layerMask)
    {
        slowPercent = slow;
        dur = duration;
        rad = radius;
        team = ownerTeam;
        this.layerMask = layerMask;
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
                CharacterStats stats = player.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.ApplySlow(slowPercent, 1f);
                    Debug.Log($"[GroundEffect] Applied slow to player {player.gameObject.name}, percentage={slowPercent}, duration=1f");
                }
                continue;
            }
            Monster monster = col.GetComponent<Monster>();
            if (monster != null && monster.gameObject.CompareTag("Enemy"))
            {
                monster.ReceiveControlEffect(ControlEffectType.Slow, 1f, Mathf.RoundToInt(slowPercent * 100));
                Debug.Log($"[GroundEffect] Applied slow to monster {monster.monsterName}, percentage={slowPercent}, duration=1f");
            }
        }
    }

    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }
}