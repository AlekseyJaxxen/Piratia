using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterAI2 : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Return }
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float chaseTimeout = 30f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    private NavMeshAgent agent;
    private Monster monster;
    private Vector3 spawnPoint;
    private State currentState = State.Idle;
    private PlayerCore target;
    private float lastAttackTime;
    private float chaseStartTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        monster = GetComponent<Monster>();
        spawnPoint = transform.position;
    }

    private void Update()
    {
        if (monster.currentHealth <= 0)
        {
            enabled = false;
            return;
        }

        if (monster.IsDead) { enabled = false; return; }

        if (monster.IsStunned) return;

        if (monster.IsDead || monster.IsStunned || !agent.isActiveAndEnabled) return;
        switch (currentState)
        {
            case State.Idle:
                FindTarget();
                if (target != null) SwitchToChase();
                else SwitchToPatrol();
                break;
            case State.Patrol:
                Patrol();
                FindTarget();
                if (target != null) SwitchToChase();
                break;
            case State.Chase:
                Chase();
                if (Time.time - chaseStartTime > chaseTimeout) SwitchToReturn();
                break;
            case State.Return:
                ReturnToSpawn();
                break;
        }
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 10f, playerLayer);
        float closestDistance = float.MaxValue;
        PlayerCore closestPlayer = null;
        foreach (Collider hit in hits)
        {
            PlayerCore player = hit.GetComponent<PlayerCore>();
            if (player != null && !player.isDead)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
        }
        target = closestPlayer;
    }

    private void Patrol()
    {
        if (agent.remainingDistance < 1f)
        {
            Vector3 randomPoint = spawnPoint + Random.insideUnitSphere * patrolRadius;
            randomPoint.y = transform.position.y;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void Chase()
    {
        FindTarget();
        if (target == null || target.isDead) { target = null; SwitchToReturn(); return; }
        if (monster.IsCooldown) { agent.isStopped = true; return; }
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= attackRange)
        {
            agent.isStopped = true;
            transform.LookAt(target.transform);
            TryAttack();
            if (target.isDead) { target = null; SwitchToReturn(); return; }
            chaseStartTime = Time.time; // Reset timer on attack
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }
    }

    private void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown && monster.basicAttackSkill != null)
        {
            lastAttackTime = Time.time;
            monster.basicAttackSkill.Execute(monster, null, target.gameObject);
            monster.IsCooldown = true;
            agent.isStopped = true;
            StartCoroutine(EndCooldown());
        }
    }

    private IEnumerator EndCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        monster.IsCooldown = false;
        if (!monster.IsStunned && agent.isOnNavMesh) agent.isStopped = false;
    }

    private void ReturnToSpawn()
    {
        agent.SetDestination(spawnPoint);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SwitchToPatrol();
        }
    }

    private void SwitchToChase()
    {
        currentState = State.Chase;
        chaseStartTime = Time.time;
    }

    private void SwitchToPatrol() { currentState = State.Patrol; }

    private void SwitchToReturn() { currentState = State.Return; }
}