using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class MonsterCore : PlayerCore
{
    [Header("Monster Settings")]
    [SerializeField] private MonsterAI monsterAI;
    [SerializeField] private GameObject dropItemPrefab;
    [SerializeField] private List<DropItem> dropTable = new List<DropItem>();
    [SerializeField] private int experienceReward = 100;

    [System.Serializable]
    public struct DropItem
    {
        public GameObject itemPrefab;
        public float dropChance;
    }

    protected override void Awake()
    {
        base.Awake();
        team = PlayerTeam.None;
        if (monsterAI == null)
        {
            monsterAI = GetComponent<MonsterAI>();
            if (monsterAI == null)
            {
                Debug.LogError("[MonsterCore] MonsterAI component missing!");
            }
            else
            {
                monsterAI.Init(this);
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        foreach (Transform child in transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Enemy");
        }
        Debug.Log($"[MonsterCore] Initialized for {playerName}, tag={gameObject.tag}, layer={LayerMask.LayerToName(gameObject.layer)}");

        // Disable PlayerUI for monsters
        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui != null)
        {
            ui.gameObject.SetActive(false);
            Debug.Log("[MonsterCore] Disabled PlayerUI for monster.");
        }
    }

    [Server]
    protected override void ServerUpdate()
    {
        base.ServerUpdate();
        if (!isDead && monsterAI != null)
        {
            monsterAI.ServerUpdate();
        }
    }

    [Server]
    public void OnDeath(PlayerCore killer)
    {
        if (isDead) return;
        SetDeathState(true);
        RpcPlayDeathVFX();
        DropItems();
        GrantExperienceToKiller(killer);
        StartCoroutine(RespawnAfterDelay());
    }

    [Server]
    private void DropItems()
    {
        foreach (var drop in dropTable)
        {
            if (Random.value * 100f <= drop.dropChance)
            {
                Vector3 dropPosition = transform.position + Random.insideUnitSphere * 1f;
                dropPosition.y = transform.position.y;
                GameObject item = Instantiate(drop.itemPrefab, dropPosition, Quaternion.identity);
                NetworkServer.Spawn(item);
                Debug.Log($"[MonsterCore] Dropped item at {dropPosition}");
            }
        }
    }

    [Server]
    private void GrantExperienceToKiller(PlayerCore killer)
    {
        if (killer != null)
        {
            killer.CmdAddExperience(experienceReward);
            Debug.Log($"[MonsterCore] Granted {experienceReward} EXP to {killer.playerName}");
        }
    }

    [Server]
    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnTime);
        ServerRespawnPlayer(_initialSpawnPosition);
    }

    [ClientRpc]
    private void RpcPlayDeathVFX()
    {
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
    }
}

public class MonsterAI : NetworkBehaviour
{
    private MonsterCore _core;
    private NavMeshAgent _agent;
    private GameObject _target;
    private Vector3 _patrolPoint;
    private float _detectionRange = 10f;
    private float _attackRange = 2f;
    private float _patrolRadius = 5f;
    private float _nextPatrolTime;

    public void Init(MonsterCore core)
    {
        _core = core;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("[MonsterAI] NavMeshAgent component missing!");
            return;
        }
        _patrolPoint = transform.position;
        _nextPatrolTime = Time.time;
        Debug.Log($"[MonsterAI] Initialized for {_core.playerName}");
    }

    [Server]
    public void ServerUpdate()
    {
        if (_core.isDead || _core.isStunned) return;

        _target = FindNearestEnemy();
        if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance <= _attackRange)
            {
                _core.Movement.StopMovement();
                _core.ActionSystem.TryStartAction(PlayerAction.Attack, null, _target);
            }
            else
            {
                _core.Movement.MoveTo(_target.transform.position);
            }
        }
        else if (Time.time >= _nextPatrolTime)
        {
            Patrol();
        }
    }

    [Server]
    private GameObject FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRange, _core.interactableLayers);
        GameObject closest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            PlayerCore player = hit.GetComponent<PlayerCore>();
            if (player != null && player.team != PlayerTeam.None && !player.isDead)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = hit.gameObject;
                }
            }
        }
        return closest;
    }

    [Server]
    private void Patrol()
    {
        Vector3 randomOffset = Random.insideUnitSphere * _patrolRadius;
        randomOffset.y = 0;
        Vector3 newPatrolPoint = _patrolPoint + randomOffset;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPatrolPoint, out hit, _patrolRadius, NavMesh.AllAreas))
        {
            _core.Movement.MoveTo(hit.position);
            _nextPatrolTime = Time.time + Random.Range(3f, 6f);
            Debug.Log($"[MonsterAI] Patrolling to {hit.position}");
        }
    }
}