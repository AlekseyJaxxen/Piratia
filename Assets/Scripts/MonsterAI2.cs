using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    
    // Динамические параметры атаки
    private float currentAttackRange = 2f;
    private float currentStoppingDistance = 1f;
    private float attackRangeVariation = 0.3f; // ±0.3 от базового радиуса
    
    // Система аггро
    [System.Serializable]
    public class AggroEntry
    {
        public PlayerCore player;
        public float aggroValue;
        public float lastDamageTime;
        public float lastSeenTime;
        
        public AggroEntry(PlayerCore player, float aggroValue)
        {
            this.player = player;
            this.aggroValue = aggroValue;
            this.lastDamageTime = Time.time;
            this.lastSeenTime = Time.time;
        }
    }
    
    private List<AggroEntry> aggroList = new List<AggroEntry>();
    private const int maxAggroTargets = 5;
    private float aggroDecayRate = 0.5f; // Уменьшение аггро в секунду
    private float aggroTimeout = 10f; // Время до удаления из списка аггро
    
    private NavMeshAgent agent;
    private Monster monster;
    private Vector3 spawnPoint;
    private State currentState = State.Idle;
    private PlayerCore target;
    private float lastAttackTime = -Mathf.Infinity; // Первая атака всегда будет ждать полный кулдаун
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
            detectionRange = monsterInfo.detectionRange;
            attackCooldown = monsterInfo.attackCooldown;
            headAttackRange = monsterInfo.headAttackRange;
            legsAttackRange = monsterInfo.legsAttackRange;
            
            // attackRange теперь берется из basicAttackSkill
            if (monsterInfo.basicAttackSkill != null)
            {
                attackRange = monsterInfo.basicAttackSkill.Range;
            }
            
            // Инициализируем динамические параметры атаки
            UpdateAttackRanges();
        }
    }
    
    private void UpdateAttackRanges()
    {
        // Получаем радиус атаки из MonsterBasicAttackSkill
        float baseRange = attackRange;
        if (monster.info != null && monster.info.basicAttackSkill != null)
        {
            baseRange = monster.info.basicAttackSkill.Range;
        }
        
        // Добавляем случайную вариацию (±30% от базового радиуса)
        float variation = Random.Range(-attackRangeVariation, attackRangeVariation);
        currentAttackRange = Mathf.Max(0.5f, baseRange + variation);
        
        // stoppingDistance всегда меньше attackRange, чтобы монстр мог атаковать
        // Используем 70-90% от текущего радиуса атаки для остановки
        float stoppingPercentage = Random.Range(0.7f, 0.9f);
        currentStoppingDistance = Mathf.Max(0.1f, currentAttackRange * stoppingPercentage);
        
        // Обновляем stopping distance у NavMeshAgent
        if (agent != null)
        {
            agent.stoppingDistance = currentStoppingDistance;
        }
        
        Debug.Log($"[MonsterAI2] Updated attack ranges: base={baseRange}, current={currentAttackRange:F2}, stopping={currentStoppingDistance:F2} (stopping is {stoppingPercentage*100:F0}% of attack range)");
    }
    
    // Система аггро
    public void AddAggro(PlayerCore player, float damage)
    {
        if (player == null || player.isDead) return;
        
        // Ищем существующую запись
        AggroEntry existingEntry = aggroList.FirstOrDefault(entry => entry.player == player);
        
        if (existingEntry != null)
        {
            // Обновляем существующую запись
            existingEntry.aggroValue += damage;
            existingEntry.lastDamageTime = Time.time;
            existingEntry.lastSeenTime = Time.time;
        }
        else
        {
            // Добавляем новую запись
            if (aggroList.Count >= maxAggroTargets)
            {
                // Удаляем запись с наименьшим аггро
                AggroEntry lowestAggro = aggroList.OrderBy(entry => entry.aggroValue).First();
                aggroList.Remove(lowestAggro);
            }
            
            aggroList.Add(new AggroEntry(player, damage));
        }
        
        // Сортируем по аггро (по убыванию)
        aggroList = aggroList.OrderByDescending(entry => entry.aggroValue).ToList();
        
        Debug.Log($"[MonsterAI2] Added aggro: {player.name} +{damage}, total aggro: {aggroList.Count}");
    }
    
    private void UpdateAggroSystem()
    {
        float currentTime = Time.time;
        
        // Уменьшаем аггро со временем
        for (int i = aggroList.Count - 1; i >= 0; i--)
        {
            AggroEntry entry = aggroList[i];
            
            // Уменьшаем аггро
            entry.aggroValue -= aggroDecayRate * aggroUpdateInterval;
            
            // Удаляем записи с нулевым или отрицательным аггро
            if (entry.aggroValue <= 0 || currentTime - entry.lastSeenTime > aggroTimeout)
            {
                aggroList.RemoveAt(i);
                continue;
            }
            
            // Проверяем, видим ли мы игрока
            if (CanSeePlayer(entry.player))
            {
                entry.lastSeenTime = currentTime;
            }
        }
        
        // Обновляем цель на основе аггро
        UpdateTargetFromAggro();
    }
    
    private void UpdateTargetFromAggro()
    {
        // Ищем игрока с наибольшим аггро, которого мы можем видеть
        AggroEntry bestTarget = aggroList.FirstOrDefault(entry => 
            entry.player != null && 
            !entry.player.isDead && 
            !entry.player.Skills._isInvisible &&
            CanSeePlayer(entry.player));
        
        if (bestTarget != null && bestTarget.player != target)
        {
            target = bestTarget.player;
            Debug.Log($"[MonsterAI2] Target changed to {target.name} (aggro: {bestTarget.aggroValue:F1})");
        }
        else if (bestTarget == null && target != null)
        {
            // Текущая цель недоступна, ищем альтернативу в увеличенном радиусе
            float extendedRange = detectionRange * 1.5f;
            AggroEntry alternativeTarget = aggroList.FirstOrDefault(entry => 
                entry.player != null && 
                !entry.player.isDead && 
                !entry.player.Skills._isInvisible &&
                Vector3.Distance(transform.position, entry.player.transform.position) <= extendedRange);
            
            if (alternativeTarget != null)
            {
                target = alternativeTarget.player;
                Debug.Log($"[MonsterAI2] Target switched to alternative: {target.name} (extended range)");
            }
            else
            {
                target = null;
                Debug.Log($"[MonsterAI2] No valid targets found, clearing target");
            }
        }
    }
    
    private bool CanSeePlayer(PlayerCore player)
    {
        if (player == null || player.isDead) return false;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= detectionRange;
    }
    
    // Оптимизация: интервалы обновления для AI
    private float lastAIUpdate = 0f;
    private float aiUpdateInterval = 0.2f; // Обновляем AI каждые 200мс вместо каждого кадра
    private float lastAggroUpdate = 0f;
    private float aggroUpdateInterval = 1f; // Обновляем аггро каждую секунду
    
    private void Update()
    {
        // Оптимизация: обновляем AI не каждый кадр, а с интервалом
        if (Time.time - lastAIUpdate < aiUpdateInterval)
            return;
        lastAIUpdate = Time.time;
        
        // Обновляем систему аггро
        if (Time.time - lastAggroUpdate >= aggroUpdateInterval)
        {
            UpdateAggroSystem();
            lastAggroUpdate = Time.time;
        }
        
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
                float effectiveHeadRange = headAttackRange + Random.Range(-attackRangeVariation, attackRangeVariation);
                if (distance <= effectiveHeadRange && !monster.IsStunned && Time.time - lastAttackTime >= attackCooldown)
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
                float effectiveHeadRange = headAttackRange + Random.Range(-attackRangeVariation, attackRangeVariation);
                if (headDistance < legsDistance && headDistance <= effectiveHeadRange && !monster.IsStunned && Time.time - lastAttackTime >= attackCooldown)
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
                float effectiveHeadRange = headAttackRange + Random.Range(-attackRangeVariation, attackRangeVariation);
                if (distance <= effectiveHeadRange && !monster.IsStunned && Time.time - lastAttackTime >= attackCooldown)
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
        // Если у нас есть аггро список, используем его для выбора цели
        if (aggroList.Count > 0)
        {
            UpdateTargetFromAggro();
            return;
        }
        
        // Иначе ищем ближайшего игрока в радиусе обнаружения
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
        
        // Используем динамический радиус атаки
        float effectiveAttackRange = monster.isCombinedLegs ? legsAttackRange : currentAttackRange;
        
        if (distance <= effectiveAttackRange && !monster.IsStunned)
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
            // Проверяем, не находимся ли мы уже слишком близко к цели
            if (distance > currentStoppingDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(target.transform.position);
            }
            else
            {
                // Мы уже в оптимальной позиции для атаки, останавливаемся
                agent.isStopped = true;
                transform.LookAt(target.transform);
            }
            
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
        if (Time.time - lastAttackTime >= attackCooldown && !monster.IsStunned)
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
                    CmdExecuteAttack(target.netId); // Используем Command
                    monster.IsCooldown = true;
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
                    StartCoroutine(EndCooldown());
                    return;
                }
                else if (monster.isCombinedHead)
                {
                    Debug.Log($"[MonsterAI2] Combined head ExecuteAttack called for {monster.name} on target {target.name}");
                    lastAttackTime = Time.time;
                    CmdExecuteAttack(target.netId); // Используем Command
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
                Debug.Log($"[MonsterAI2] Basic attack skill executed by {monster.name} on target {target.name}");
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
            float timeSinceLastAttack = Time.time - lastAttackTime;
            Debug.Log($"[MonsterAI2] TryAttack blocked for {monster.name}: time since last attack={timeSinceLastAttack:F3}s/{attackCooldown:F3}s, stunned={monster.IsStunned}");
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
    
    [Command]
    private void CmdExecuteAttack(uint targetNetId)
    {
        if (target != null && target.netId == targetNetId)
        {
            monster.ExecuteAttack(target);
        }
    }
    
    private void SwitchToPatrol() { currentState = State.Patrol; }
    private void SwitchToReturn() { currentState = State.Return; }
}