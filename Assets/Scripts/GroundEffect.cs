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
    private NetworkIdentity casterIdentity;

    public void Init(float slow, float duration, float radius, PlayerTeam ownerTeam, int layerMask, NetworkIdentity caster = null)
    {
        slowPercent = slow;
        dur = duration;
        rad = radius;
        team = ownerTeam;
        this.layerMask = layerMask;
        casterIdentity = caster;
        StartCoroutine(DestroyAfter(duration));
    }

    private void Update()
    {
        if (!isServer) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, rad, layerMask);
        foreach (Collider col in hits)
        {
            PlayerCore player = col.GetComponent<PlayerCore>();
            if (player != null && IsEnemy(player, team) && !IsCaster(player))
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
    
    /// <summary>
    /// Checks if player is an enemy to the ground effect owner
    /// </summary>
    private bool IsEnemy(PlayerCore player, PlayerTeam ownerTeam)
    {
        if (player == null) return false;
        
        // Check basic team logic first
        if (player.team == ownerTeam && player.team != PlayerTeam.Solo)
        {
            return false; // Same team, not enemy
        }
        
        // For solo players, they are enemies to each other
        if (player.team == PlayerTeam.Solo && ownerTeam == PlayerTeam.Solo)
        {
            return true; // Solo players are enemies to each other
        }
        
        // For other teams, use normal team logic
        return player.team != ownerTeam;
    }
    
    /// <summary>
    /// Checks if the player is the caster of this ground effect
    /// </summary>
    private bool IsCaster(PlayerCore player)
    {
        if (player == null || casterIdentity == null) return false;
        return player.netIdentity == casterIdentity;
    }
}