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
    // Combined attack ranges
    private float headAttackRange = 3f;
    private float legsAttackRange = 2f;
    
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
            headAttackRange = monsterInfo.headAttackRange;
            legsAttackRange = monsterInfo.legsAttackRange;
        }
    }
    
    // Оптимизация: интервалы обновления для AI
    private float lastAIUpdate = 0f;
    private float aiUpdateInterval = 0.2f; // Обновляем AI каждые 200мс вместо каждого кадра
    
    private void Update()
    {
        // Оптимизация: обновляем AI не каждый кадр, а с интервалом
        if (Time.time - lastAIUpdate < aiUpdateInterval)
            return;
        lastAIUpdate = Time.time;
        
        // Упрощенная система для combined монстров
        if (monster.isCombinedHead)
        {
            // Head только поворачивается к цели
            UpdateCombinedHeadAI();
            return;
        }
        else if (monster.isCombinedLegs)
        {
            // Legs управляет движением
            UpdateCombinedLegsAI();
            return;
        }
        
        // Обычные монстры (не combined)
        if (monster.GetComponent<Health>().CurrentHealth <= 0 || monster.IsDead)
        {
            enabled = false;
            return;
        }
        
        if (monster.IsStunned || monster.IsCooldown || !agent.isActiveAndEnabled || !monster.canMove)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }
        
        // Обычная логика для не-combined монстров
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
    
    private void UpdateCombinedLegsAI()
    {
        // Упрощенная логика для legs
        
        // Проверяем состояние legs
        if (monster.GetComponent<Health>().CurrentHealth <= 0 || monster.IsDead)
        {
            // Legs мертвы - останавливаем движение
            if (agent != null && agent.isOnNavMesh) 
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            // Legs мертвы - отключаем AI
            
            enabled = false;
            return;
        }
        
        if (monster.IsStunned || monster.IsCooldown || !agent.isActiveAndEnabled || !monster.canMove)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }
        
        // Используем обычную логику AI для legs
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
                if (target == null) SwitchToReturn();
                if (Time.time - chaseStartTime > chaseTimeout) SwitchToReturn();
                break;
            case State.Return:
                ReturnToSpawn();
                break;
        }
    }
    
    private void UpdateCombinedHeadAI()
    {
        // Head объект не двигается, только поворачивается к цели и атакует
        if (monster.GetComponent<Health>().CurrentHealth <= 0 || monster.IsDead)
        {
            enabled = false;
            return;
        }
        
        // Head только поворачивается к цели
        FindTarget();
        
        if (target != null && !target.Skills._isInvisible)
        {
            // Head не поворачивается за игроком - остается в фиксированном положении
            // Поворот отключен для упавшей головы
            
            // Head атакует только если legs мертв или если head ближе к цели
            if (monster.partnerMonster != null && monster.partnerMonster.IsDead)
            {
                // Проверяем расстояние до цели для атаки (от позиции головы)
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= headAttackRange && !monster.IsStunned && Time.time >= lastAttackTime + attackCooldown)
                {
                    // Head атакует только после смерти legs
                    Debug.Log($"[MonsterAI2] Head attacking after legs death, distance: {distance}, range: {headAttackRange}");
                    monster.ExecuteAttack(target);
                    lastAttackTime = Time.time;
                    monster.IsCooldown = true;
                    StartCoroutine(EndCooldown());
                }
            }
            else if (monster.partnerMonster != null && !monster.partnerMonster.IsDead)
            {
                // Проверяем, кто ближе к цели
                float headDistance = Vector3.Distance(transform.position, target.transform.position);
                float legsDistance = Vector3.Distance(monster.partnerMonster.transform.position, target.transform.position);
                
                Debug.Log($"[MonsterAI2] Head distance: {headDistance}, legs distance: {legsDistance}, head range: {headAttackRange}");
                
                // Head атакует только если он ближе к цели и в радиусе атаки
                if (headDistance < legsDistance && headDistance <= headAttackRange && !monster.IsStunned && Time.time >= lastAttackTime + attackCooldown)
                {
                    Debug.Log($"[MonsterAI2] Head attacking, head closer to target");
                    monster.ExecuteAttack(target);
                    lastAttackTime = Time.time;
                    monster.IsCooldown = true;
                    StartCoroutine(EndCooldown());
                }
            }
            else
            {
                // Head без партнера (legs мертв) - атакует с земли
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= headAttackRange && !monster.IsStunned && Time.time >= lastAttackTime + attackCooldown)
                {
                    Debug.Log($"[MonsterAI2] Head attacking without partner, distance: {distance}, range: {headAttackRange}");
                    if (monster != null && !monster.IsDead)
                    {
                        monster.ExecuteAttack(target);
                        lastAttackTime = Time.time;
                        monster.IsCooldown = true;
                        StartCoroutine(EndCooldown());
                    }
                }
            }
        }
    }
    
    public void SetTarget(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            target = targetTransform.GetComponent<PlayerCore>();
            if (target != null && currentState != State.Chase)
            {
                SwitchToChase();
            }
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
            if (player != null && !player.isDead && !player.Skills._isInvisible)
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
        // Проверяем, что агент активен и на NavMesh
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && agent.remainingDistance < 1f && !monster.IsStunned)
        {
            Vector3 randomPoint = spawnPoint + Random.insideUnitSphere * patrolRadius;
            randomPoint.y = transform.position.y;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        
        // Анимация движения для combined legs
        if (monster.isCombinedLegs && agent != null && agent.isActiveAndEnabled && agent.velocity.magnitude > 0.1f)
        {
            monster.PlayAnimation("Walk");
        }
        else if (monster.isCombinedLegs)
        {
            monster.PlayAnimation("Idle");
        }
    }
    
    private void Chase()
    {
        FindTarget();
        if (target == null || target.isDead || target.Skills._isInvisible) { 
            target = null; 
            SwitchToReturn(); 
            return; 
        }
        if (monster.IsCooldown) { 
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true; 
            return; 
        }
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        if (distance <= legsAttackRange && !monster.IsStunned)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            transform.LookAt(target.transform);
            
            // Legs атакует только если head не атакует или если legs ближе к цели
            if (monster.partnerMonster != null && !monster.partnerMonster.IsDead)
            {
                float headDistance = Vector3.Distance(monster.partnerMonster.transform.position, target.transform.position);
                Debug.Log($"[MonsterAI2] Legs attack check: legs distance={distance}, head distance={headDistance}, head cooldown={monster.partnerMonster.IsCooldown}");
                if (distance < headDistance || !monster.partnerMonster.IsCooldown)
                {
                    Debug.Log($"[MonsterAI2] Legs attacking: legs closer or head not on cooldown");
                    TryAttack();
                }
                else
                {
                    Debug.Log($"[MonsterAI2] Legs attack blocked: head closer and on cooldown");
                }
            }
            else
            {
                Debug.Log($"[MonsterAI2] Legs attacking: no partner or partner dead");
                TryAttack();
            }
            
            if (target.isDead || target.Skills._isInvisible) { target = null; SwitchToReturn(); return; }
            chaseStartTime = Time.time; // Reset timer on attack
            
            // Анимация Idle когда в радиусе атаки
            if (monster.isCombinedLegs)
            {
                monster.PlayAnimation("Idle");
            }
            else if (monster.isCombinedHead && monster.IsDead)
            {
                // Упавшая голова играет LyingIdle вместо Idle
                monster.PlayAnimation("LyingIdle");
            }
        }
        else if (!monster.IsStunned && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.transform.position);
            
            // Анимация движения для combined legs
        if (monster.isCombinedLegs && agent.velocity.magnitude > 0.1f)
        {
            monster.PlayAnimation("Walk");
        }
        else if (monster.isCombinedLegs)
        {
            monster.PlayAnimation("Idle");
        }
        else if (monster.isCombinedHead && monster.IsDead)
        {
            // Упавшая голова играет LyingIdle вместо Idle
            monster.PlayAnimation("LyingIdle");
        }
        }
    }
    
    private void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown && !monster.IsStunned)
        {
            Debug.Log($"[MonsterAI2] TryAttack called for {monster.name}, target: {target?.name}");
            
            // Для combined monsters сначала пробуем специальный скилл
            if (monster.isCombinedLegs || monster.isCombinedHead)
            {
                bool combinedSpecialSkillUsed = monster.TryUseSkill(target);
                
                if (combinedSpecialSkillUsed)
                {
                    Debug.Log($"[MonsterAI2] Special skill used by combined {monster.name}");
                    lastAttackTime = Time.time;
                    monster.IsCooldown = true;
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
                    StartCoroutine(EndCooldown());
                    return;
                }
                
                // Если специальный скилл не сработал, используем ExecuteAttack для правильных анимаций
                if (monster.isCombinedLegs)
                {
                    Debug.Log($"[MonsterAI2] Combined legs ExecuteAttack called for {monster.name} on target {target.name}");
                    lastAttackTime = Time.time;
                    monster.ExecuteAttack(target); // Передаем target!
                    monster.IsCooldown = true;
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
                    StartCoroutine(EndCooldown());
                    return;
                }
                else if (monster.isCombinedHead)
                {
                    Debug.Log($"[MonsterAI2] Combined head ExecuteAttack called for {monster.name} on target {target.name}");
                    lastAttackTime = Time.time;
                    monster.ExecuteAttack(target); // Передаем target!
                    monster.IsCooldown = true;
                    StartCoroutine(EndCooldown());
                    return;
                }
            }
            
            // First try to use a special skill (для обычных_monстров)
            bool specialSkillUsed = monster.TryUseSkill(target);
            
            if (specialSkillUsed)
            {
                Debug.Log($"[MonsterAI2] Special skill used by {monster.name}");
                lastAttackTime = Time.time;
                monster.IsCooldown = true;
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
                StartCoroutine(EndCooldown());
                return;
            }
            
            // If no special skill was used, use basic attack (для обычных монстров)
            if (monster.basicAttackSkill != null)
            {
                Debug.Log($"[MonsterAI2] Basic attack skill used by {monster.name}");
                lastAttackTime = Time.time;
                monster.basicAttackSkill.Execute(monster, null, target.gameObject);
                monster.IsCooldown = true;
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
                StartCoroutine(EndCooldown());
            }
            else
            {
                Debug.LogWarning($"[MonsterAI2] {monster.monsterName} No basic attack skill assigned!");
            }
        }
        else
        {
            Debug.Log($"[MonsterAI2] TryAttack blocked for {monster.name}: cooldown={Time.time - lastAttackTime}/{attackCooldown}, stunned={monster.IsStunned}");
        }
    }
    
    private IEnumerator EndCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        monster.IsCooldown = false;
        if (!monster.IsStunned && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = false;
    }
    
    private void ReturnToSpawn()
    {
        if (!monster.IsStunned && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(spawnPoint);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                SwitchToPatrol();
            }
        }
        
        // Анимация движения для combined legs
        if (monster.isCombinedLegs && agent != null && agent.isActiveAndEnabled && agent.velocity.magnitude > 0.1f)
        {
            monster.PlayAnimation("Walk");
        }
        else if (monster.isCombinedLegs)
        {
            monster.PlayAnimation("Idle");
        }
        else if (monster.isCombinedHead && monster.IsDead)
        {
            // Упавшая голова играет LyingIdle вместо Idle
            monster.PlayAnimation("LyingIdle");
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