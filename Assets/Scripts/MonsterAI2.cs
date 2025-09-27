using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterAI2 : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Return }
    
    // AI параметры из SO
    private float patrolRadius = 10f;
    private float chaseTimeout = 30f;
    private LayerMask playerLayer;
    private float attackRange = 2f;
    private float detectionRange = 10f;
    private float attackCooldown = 2f;
    
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
    
    public void InitializeAI(MonsterInfo monsterInfo)
    {
        if (monsterInfo != null)
        {
            patrolRadius = monsterInfo.patrolRadius;
            chaseTimeout = monsterInfo.chaseTimeout;
            playerLayer = monsterInfo.playerLayer;
            attackRange = monsterInfo.attackRange;
            detectionRange = monsterInfo.detectionRange;
            attackCooldown = monsterInfo.attackCooldown;
            
            Debug.Log($"[MonsterAI2] {monster.monsterName} AI initialized - attackRange: {attackRange}, detectionRange: {detectionRange}, attackCooldown: {attackCooldown}");
        }
    }
    private void Update()
    {
        if (monster.headNetId != 0) return; // ��� AI ��� legs
        if (monster.GetComponent<Health>().CurrentHealth <= 0 || monster.IsDead)
        {
            enabled = false;
            return;
        }
        // ��������� ��� ������� ��������
        if (monster.IsStunned || monster.IsCooldown || !agent.isActiveAndEnabled)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }
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
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
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
        if (agent.remainingDistance < 1f && !monster.IsStunned)
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
        if (monster.IsCooldown) { 
            Debug.Log($"[MonsterAI2] {monster.monsterName} on cooldown, stopping");
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true; 
            return; 
        }
        float distance = Vector3.Distance(transform.position, target.transform.position);
        Debug.Log($"[MonsterAI2] {monster.monsterName} chasing {target.playerName}, distance: {distance:F1}, attackRange: {attackRange}, isStunned: {monster.IsStunned}");
        
        if (distance <= attackRange && !monster.IsStunned)
        {
            Debug.Log($"[MonsterAI2] {monster.monsterName} in attack range, attempting attack");
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            transform.LookAt(target.transform);
            TryAttack();
            if (target.isDead) { target = null; SwitchToReturn(); return; }
            chaseStartTime = Time.time; // Reset timer on attack
        }
        else if (!monster.IsStunned)
        {
            Debug.Log($"[MonsterAI2] {monster.monsterName} moving towards target, distance: {distance:F1}");
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }
    }
    private void TryAttack()
    {
        float timeSinceLastAttack = Time.time - lastAttackTime;
        Debug.Log($"[MonsterAI2] {monster.monsterName} TryAttack called. Time since last attack: {timeSinceLastAttack:F1}, cooldown: {attackCooldown}, isStunned: {monster.IsStunned}");
        
        if (Time.time >= lastAttackTime + attackCooldown && !monster.IsStunned)
        {
            Debug.Log($"[MonsterAI2] {monster.monsterName} Attack conditions met, attempting attack");
            
            // First try to use a special skill
            bool specialSkillUsed = monster.TryUseSkill(target);
            Debug.Log($"[MonsterAI2] {monster.monsterName} Special skill used: {specialSkillUsed}");
            
            if (specialSkillUsed)
            {
                lastAttackTime = Time.time;
                monster.IsCooldown = true;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                StartCoroutine(EndCooldown());
                Debug.Log($"[MonsterAI2] {monster.monsterName} Special skill executed, starting cooldown");
                return;
            }
            
            // If no special skill was used, use basic attack
            if (monster.basicAttackSkill != null)
            {
                Debug.Log($"[MonsterAI2] {monster.monsterName} Using basic attack skill: {monster.basicAttackSkill.SkillName}");
                lastAttackTime = Time.time;
                monster.basicAttackSkill.Execute(monster, null, target.gameObject);
                monster.IsCooldown = true;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                StartCoroutine(EndCooldown());
                Debug.Log($"[MonsterAI2] {monster.monsterName} Basic attack executed, starting cooldown");
            }
            else
            {
                Debug.LogWarning($"[MonsterAI2] {monster.monsterName} No basic attack skill assigned!");
            }
        }
        else
        {
            if (Time.time < lastAttackTime + attackCooldown)
            {
                Debug.Log($"[MonsterAI2] {monster.monsterName} Still on cooldown: {timeSinceLastAttack:F1}/{attackCooldown}");
            }
            if (monster.IsStunned)
            {
                Debug.Log($"[MonsterAI2] {monster.monsterName} Cannot attack - stunned");
            }
        }
    }
    private IEnumerator EndCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        monster.IsCooldown = false;
        if (!monster.IsStunned && agent.isOnNavMesh && agent != null) agent.isStopped = false;
    }
    private void ReturnToSpawn()
    {
        if (!monster.IsStunned)
        {
            agent.SetDestination(spawnPoint);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                SwitchToPatrol();
            }
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