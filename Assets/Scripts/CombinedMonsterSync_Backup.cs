// BACKUP FILE - COMMENTED OUT TO AVOID DUPLICATE CLASSES
/*
using UnityEngine;
using Mirror;

public class CombinedMonsterSync : NetworkBehaviour
{
    private Monster monster;
    private bool isLegs = false;
    private uint headNetId = 0;
    private Vector3 positionOffset = Vector3.zero;
    
    private void Awake()
    {
        monster = GetComponent<Monster>();
    }
    
    private void Start()
    {
        // Определяем, являемся ли мы ногами
        if (monster != null && monster.headNetId != 0)
        {
            isLegs = true;
            headNetId = monster.headNetId;
            positionOffset = new Vector3(0, -15f, 0); // Ноги на 15 единиц ниже головы
            
            // Legs initialized
        }
    }
    
    private void Update()
    {
        if (isLegs && headNetId != 0)
        {
            SyncWithHead();
        }
    }
    
    private void SyncWithHead()
    {
        // Находим голову по NetworkIdentity
        if (NetworkServer.spawned.TryGetValue(headNetId, out var headIdentity))
        {
            Transform headTransform = headIdentity.transform;
            if (headTransform != null)
            {
                // Синхронизируем позицию головы с ногами (ноги двигают голову)
                Vector3 targetPosition = transform.position - positionOffset;
                headTransform.position = targetPosition;
                
                // Синхронизируем поворот головы с ногами
                headTransform.rotation = transform.rotation;
            }
        }
        else if (isClient)
        {
            // На клиенте пытаемся найти голову через FindObjectsOfType
            Monster[] allMonsters = FindObjectsOfType<Monster>();
            foreach (Monster mon in allMonsters)
            {
                if (mon.netIdentity.netId == headNetId)
                {
                    Vector3 targetPosition = transform.position - positionOffset;
                    mon.transform.position = targetPosition;
                    mon.transform.rotation = transform.rotation;
                    break;
                }
            }
        }
    }
    
    public void InitializeAsLegs(uint headId)
    {
        isLegs = true;
        headNetId = headId;
        positionOffset = new Vector3(0, -15f, 0);
        
        // Manually initialized as legs
    }
}
*/
