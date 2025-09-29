// BACKUP FILE - COMMENTED OUT TO AVOID DUPLICATE CLASSES
/*
using UnityEngine;
using Mirror;

public class CombinedMonsterCoordinator : NetworkBehaviour
{
    [Header("Combined Monster Settings")]
    [SerializeField] private float headAttackRange = 8f;
    [SerializeField] private float legsAttackRange = 7f;
    [SerializeField] private float headScanHeight = 15f;
    [SerializeField] private float legsScanHeight = 1f;
    [SerializeField] private LayerMask playerLayer = 1;
    
    [Header("Attack Priorities")]
    [SerializeField] private bool prioritizeClosestTarget = true;
    [SerializeField] private float attackCooldown = 2f;
    
    private Monster headMonster;
    private Monster legsMonster;
    private float lastAttackTime;
    private PlayerCore currentTarget;
    
    public bool CanAttack => Time.time - lastAttackTime >= attackCooldown;
    public bool HasValidTarget => currentTarget != null && !currentTarget.isDead;
    
    private void Start()
    {
        // Находим голову и ноги с задержкой
        StartCoroutine(FindHeadAndLegsDelayed());
    }
    
    private System.Collections.IEnumerator FindHeadAndLegsDelayed()
    {
        // Ждем дольше, чтобы связи между головой и ногами успели синхронизироваться
        yield return new WaitForSeconds(2f);
        
        // Получаем текущий объект как голову
        Monster currentHead = GetComponent<Monster>();
        if (currentHead == null || (currentHead.info != null && !currentHead.info.isCombined))
        {
            Debug.LogError($"[CombinedMonsterCoordinator] This coordinator is not attached to a combined head monster!");
            yield break;
        }
        
        headMonster = currentHead;
        
        // Ищем ноги по legsNetId
        if (headMonster.legsNetId != 0)
        {
            if (NetworkServer.spawned.TryGetValue(headMonster.legsNetId, out var legsIdentity))
            {
                legsMonster = legsIdentity.GetComponent<Monster>();
            }
        }
        
        // Если не нашли по NetId, ищем по позиции (fallback)
        if (legsMonster == null)
        {
            Debug.LogWarning($"[CombinedMonsterCoordinator] Could not find legs by NetId, searching by position...");
            Monster[] allMonsters = FindObjectsOfType<Monster>();
            foreach (Monster monster in allMonsters)
            {
                if (monster.headNetId == headMonster.netIdentity.netId)
                {
                    legsMonster = monster;
                    break;
                }
            }
        }
        
        if (headMonster == null || legsMonster == null)
        {
            Debug.LogError($"[CombinedMonsterCoordinator] Could not find both head and legs monsters! Head: {headMonster?.name}, Legs: {legsMonster?.name}");
        }
        else
        {
            Debug.Log($"[CombinedMonsterCoordinator] Successfully found both monsters: Head={headMonster.name} (isCombined={headMonster.info?.isCombined}, legsNetId={headMonster.legsNetId}), Legs={legsMonster.name} (headNetId={legsMonster.headNetId})");
        }
    }
    
    public void Update()
    {
        if (!isServer || headMonster == null || legsMonster == null) return;
        
        // Сканируем цели
        ScanForTargets();
        
        // Принимаем решение об атаке
        if (HasValidTarget && CanAttack)
        {
            DecideAndExecuteAttack();
        }
        else if (HasValidTarget && !CanAttack)
        {
            // Если есть цель, но нельзя атаковать - двигаемся к ней
            MoveToTarget();
        }
        else if (!HasValidTarget)
        {
            // Если нет цели - проигрываем анимацию покоя (только если не проигрывается)
            // headMonster.PlayAnimation("Idle");
            // headMonster.RpcPlayAnimation("Idle");
        }
        
        // Логи для отладки
        if (Time.frameCount % 60 == 0) // Каждую секунду
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            Debug.Log($"[CombinedMonsterCoordinator] Update: hasTarget={HasValidTarget}, canAttack={CanAttack}, target={currentTarget?.name}, legsDead={legsMonster.IsDead}, timeSinceLastAttack={timeSinceLastAttack:F1}s, attackCooldown={attackCooldown}s, playerLayer={playerLayer.value}");
        }
    }
    
    private void ScanForTargets()
    {
        // Сканируем на высоте головы (крыши) - используем больший радиус для обнаружения
        Vector3 headScanPos = new Vector3(headMonster.transform.position.x, headScanHeight, headMonster.transform.position.z);
        Collider[] headTargets = Physics.OverlapSphere(headScanPos, headAttackRange * 2f, playerLayer);
        
        // Сканируем на высоте ног (земли) - используем больший радиус для обнаружения
        Vector3 legsScanPos = new Vector3(legsMonster.transform.position.x, legsScanHeight, legsMonster.transform.position.z);
        Collider[] legsTargets = Physics.OverlapSphere(legsScanPos, legsAttackRange * 2f, playerLayer);
        
        // Объединяем результаты
        List<PlayerCore> foundTargets = new List<PlayerCore>();
        
        foreach (var target in headTargets)
        {
            PlayerCore player = target.GetComponent<PlayerCore>();
            if (player != null && !player.isDead)
            {
                foundTargets.Add(player);
            }
        }
        
        foreach (var target in legsTargets)
        {
            PlayerCore player = target.GetComponent<PlayerCore>();
            if (player != null && !player.isDead && !foundTargets.Contains(player))
            {
                foundTargets.Add(player);
            }
        }
        
        // Выбираем ближайшую цель
        if (foundTargets.Count > 0)
        {
            if (prioritizeClosestTarget)
            {
                float closestDistance = float.MaxValue;
                PlayerCore closestTarget = null;
                
                foreach (var target in foundTargets)
                {
                    float distance = Vector3.Distance(headMonster.transform.position, target.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = target;
                    }
                }
                
                currentTarget = closestTarget;
            }
            else
            {
                currentTarget = foundTargets[0];
            }
        }
        else
        {
            currentTarget = null;
        }
    }
    
    private void DecideAndExecuteAttack()
    {
        if (currentTarget == null) return;
        
        // Вычисляем расстояния от реальных позиций head и legs
        Vector3 headPos = headMonster.transform.position;
        Vector3 legsPos = legsMonster.transform.position;
        
        float headDistance = Vector3.Distance(headPos, currentTarget.transform.position);
        float legsDistance = Vector3.Distance(legsPos, currentTarget.transform.position);
        
        // Определяем тип атаки на основе расстояния
        if (legsDistance <= legsAttackRange)
        {
            // Атака ногами (ближе к земле)
            ExecuteLegsAttack();
        }
        else if (headDistance <= headAttackRange)
        {
            // Атака рукой (стоя или лежа)
            ExecuteHeadAttack();
        }
        else if (legsMonster.IsDead)
        {
            // Если ноги мертвы, голова может атаковать лежа
            ExecuteHeadAttackLying();
        }
    }
    
    private void ExecuteLegsAttack()
    {
        Debug.Log($"[CombinedMonsterCoordinator] Executing legs attack on {currentTarget.name}");
        headMonster.PlayLegsAttackAnimation();
        lastAttackTime = Time.time;
    }
    
    private void ExecuteHeadAttack()
    {
        Debug.Log($"[CombinedMonsterCoordinator] Executing head attack on {currentTarget.name}");
        headMonster.PlayHeadAttackAnimation();
        lastAttackTime = Time.time;
    }
    
    private void ExecuteHeadAttackLying()
    {
        Debug.Log($"[CombinedMonsterCoordinator] Executing head attack lying on {currentTarget.name}");
        headMonster.PlayHeadAttackLyingAnimation();
        lastAttackTime = Time.time;
    }
    
    private void MoveToTarget()
    {
        if (legsMonster.IsDead) return; // Не может двигаться без ног
        
        Debug.Log($"[CombinedMonsterCoordinator] Moving to target {currentTarget.name}");
        
        // Проигрываем анимацию ходьбы на голове
        headMonster.PlayAnimation("Walk");
        headMonster.RpcPlayAnimation("Walk");
        
        // Legs объект сам управляет движением через UpdateCombinedLegsAI
        // Здесь мы только устанавливаем цель
    }
    
    // Метод для получения текущей цели (для MonsterAI2)
    public PlayerCore GetCurrentTarget()
    {
        return currentTarget;
    }
    
    // Метод для проверки, может ли монстр атаковать
    public bool CanMonsterAttack()
    {
        return CanAttack && HasValidTarget;
    }
    
    // Метод для включения режима ползания (закомментированный)
    // public void EnableCrawlingMode()
    // {
    //     // Логика для ползания после смерти ног
    //     Debug.Log("[CombinedMonsterCoordinator] Enabling crawling mode for head");
    // }
    
    private void OnDrawGizmosSelected()
    {
        if (headMonster == null || legsMonster == null) return;
        
        // Голова - сканирование и атака
        Vector3 headScanPos = new Vector3(headMonster.transform.position.x, headScanHeight, headMonster.transform.position.z);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(headScanPos, headAttackRange * 2f); // Detection
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(headScanPos, headAttackRange); // Attack
        
        // Ноги - сканирование и атака
        Vector3 legsScanPos = new Vector3(legsMonster.transform.position.x, legsScanHeight, legsMonster.transform.position.z);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(legsScanPos, legsAttackRange * 2f); // Detection
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(legsScanPos, legsAttackRange); // Attack
        
        // Текущая цель
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(headMonster.transform.position, currentTarget.transform.position);
            Gizmos.DrawWireCube(currentTarget.transform.position, Vector3.one * 0.5f);
        }
        
        // Линия между головой и ногами
        Gizmos.color = Color.white;
        Gizmos.DrawLine(headMonster.transform.position, legsMonster.transform.position);
    }
}
*/
