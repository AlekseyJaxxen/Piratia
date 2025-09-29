// BACKUP FILE - COMMENTED OUT TO AVOID DUPLICATE CLASSES
/*
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.Collections;

public class MonsterAI2 : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float patrolRadius = 10f;
    public float chaseTimeout = 30f;
    public LayerMask playerLayer = -1;
    
    private Monster monster;
    private NavMeshAgent agent;
    private CombinedMonsterCoordinator coordinator;
    private Vector3 spawnPoint;
    private PlayerCore target;
    private float chaseStartTime;
    
    public enum State
    {
        Idle,
        Patrol,
        Chase,
        Return
    }
    
    private State currentState = State.Idle;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        monster = GetComponent<Monster>();
        coordinator = GetComponent<CombinedMonsterCoordinator>();
        spawnPoint = transform.position;
    }
    
    public void InitializeAI(MonsterInfo info)
    {
        if (info != null)
        {
            detectionRange = info.detectionRange;
            attackRange = info.attackRange;
            patrolRadius = info.patrolRadius;
            chaseTimeout = info.chaseTimeout;
            playerLayer = info.playerLayer;
        }
    }
    
    private void Update()
    {
        // Для combined монстров: AI движения только на legs объекте
        if (monster.info != null && monster.info.isCombined)
        {
            // Логи для отладки
            if (Time.frameCount % 60 == 0) // Каждую секунду
            {
                Debug.Log($"[MonsterAI2] Combined monster: {monster.name}, headNetId={monster.headNetId}, isHead={monster.headNetId == 0}, isLegs={monster.headNetId != 0}");
            }
            
            if (monster.headNetId != 0) 
            {
                // Это legs объект - выполняем AI движения
                UpdateCombinedLegsAI();
                return;
            }
            else
            {
                // Это head объект - только атаки, без движения
                UpdateCombinedHeadAI();
                return;
            }
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
                ScanForTargets();
                if (target == null) SwitchToPatrol();
                break;
            case State.Patrol:
                Patrol();
                ScanForTargets();
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
    
    private void UpdateCombinedLegsAI()
    {
        // Логи для отладки
        if (Time.frameCount % 60 == 0) // Каждую секунду
        {
            Debug.Log($"[MonsterAI2] UpdateCombinedLegsAI called: monster={monster.name}, coordinator={coordinator != null}, headNetId={monster.headNetId}");
        }
        
        // Проверяем состояние legs
        if (monster.GetComponent<Health>().CurrentHealth <= 0 || monster.IsDead)
        {
            // Legs мертвы - останавливаем движение
            if (agent != null && agent.isOnNavMesh) 
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            // TODO: Комментированная логика для ползания после смерти legs
            // if (coordinator != null)
            // {
            //     coordinator.EnableCrawlingMode();
            // }
            
            enabled = false;
            return;
        }
        
        if (monster.IsStunned || monster.IsCooldown || !agent.isActiveAndEnabled || !monster.canMove)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }
        
        // Получаем цель от coordinator
        if (coordinator != null)
        {
            PlayerCore coordinatorTarget = coordinator.GetCurrentTarget();
            
            // Логи для отладки
            if (Time.frameCount % 60 == 0) // Каждую секунду
            {
                Debug.Log($"[MonsterAI2] UpdateCombinedLegsAI: coordinatorTarget={coordinatorTarget?.name}, currentState={currentState}, monster.IsDead={monster.IsDead}, agent.isActiveAndEnabled={agent?.isActiveAndEnabled}");
            }
            
            if (coordinatorTarget != null)
            {
                target = coordinatorTarget;
                if (currentState != State.Chase) SwitchToChase();
                
                // Движемся к цели
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.SetDestination(target.transform.position);
                    agent.isStopped = false;
                    
                    // Логи для отладки
                    if (Time.frameCount % 60 == 0) // Каждую секунду
                    {
                        Debug.Log($"[MonsterAI2] Moving to target: destination={target.transform.position}, agent.velocity={agent.velocity.magnitude:F2}, agent.remainingDistance={agent.remainingDistance:F2}, agent.pathStatus={agent.pathStatus}");
                    }
                }
                else
                {
                    // Логи для отладки
                    if (Time.frameCount % 60 == 0) // Каждую секунду
                    {
                        Debug.Log($"[MonsterAI2] Cannot move: agent={agent != null}, isActiveAndEnabled={agent?.isActiveAndEnabled}, isOnNavMesh={agent?.isOnNavMesh}");
                    }
                }
            }
            else
            {
                target = null;
                if (currentState != State.Patrol) SwitchToPatrol();
                
                // Патрулируем
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    if (agent.remainingDistance < 0.5f)
                    {
                        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                        randomDirection += spawnPoint;
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
                        {
                            agent.SetDestination(hit.position);
                        }
                    }
                    agent.isStopped = false;
                }
            }
        }
        else
        {
            // Логи для отладки
            if (Time.frameCount % 60 == 0) // Каждую секунду
            {
                Debug.Log($"[MonsterAI2] UpdateCombinedLegsAI: coordinator is null!");
            }
        }
    }
    
    private void UpdateCombinedHeadAI()
    {
        // Логи для отладки
        if (Time.frameCount % 60 == 0) // Каждую секунду
        {
            Debug.Log($"[MonsterAI2] UpdateCombinedHeadAI called: monster={monster.name}, coordinator={coordinator != null}");
        }
        
        // Проверяем состояние head
        if (monster.GetComponent<Health>().CurrentHealth <= 0 || monster.IsDead)
        {
            enabled = false;
            return;
        }
        
        // Получаем цель от coordinator
        if (coordinator != null)
        {
            PlayerCore coordinatorTarget = coordinator.GetCurrentTarget();
            
            if (coordinatorTarget != null)
            {
                // Поворачиваемся к цели
                Vector3 directionToTarget = coordinatorTarget.transform.position - transform.position;
                directionToTarget.y = 0;
                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }
        }
    }
    
    private void ScanForTargets()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
        foreach (var target in targets)
        {
            PlayerCore player = target.GetComponent<PlayerCore>();
            if (player != null && !player.isDead)
            {
                this.target = player;
                return;
            }
        }
        this.target = null;
    }
    
    private void Patrol()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            if (agent.remainingDistance < 0.5f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                randomDirection += spawnPoint;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
                {
                    agent.SetDestination(hit.position);
                }
            }
            agent.isStopped = false;
        }
    }
    
    private void Chase()
    {
        if (target != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(target.transform.position);
            agent.isStopped = false;
        }
    }
    
    private void ReturnToSpawn()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(spawnPoint);
            agent.isStopped = false;
            
            if (Vector3.Distance(transform.position, spawnPoint) < 1f)
            {
                SwitchToPatrol();
            }
        }
    }
    
    private void SwitchToPatrol()
    {
        currentState = State.Patrol;
    }
    
    private void SwitchToChase()
    {
        currentState = State.Chase;
        chaseStartTime = Time.time;
    }
    
    private void SwitchToReturn()
    {
        currentState = State.Return;
        target = null;
    }
}
*/
