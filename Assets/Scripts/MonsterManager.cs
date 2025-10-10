using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

/// <summary>
/// Централизованный менеджер для управления монстрами
/// Заменяет использование FindObjectOfType для поиска монстров
/// </summary>
public class MonsterManager : NetworkBehaviour
{
    public static MonsterManager Instance { get; private set; }
    
    [Header("Monster Tracking")]
    private Dictionary<uint, Monster> monstersByNetId = new Dictionary<uint, Monster>();
    private Dictionary<int, List<Monster>> monstersByType = new Dictionary<int, List<Monster>>();
    private Dictionary<string, List<Monster>> monstersByArea = new Dictionary<string, List<Monster>>();
    
    [Header("Performance Settings")]
    [SerializeField] private float updateInterval = 0.1f;
    private float lastUpdateTime;
    
    [Header("Events")]
    public System.Action<Monster> OnMonsterSpawned;
    public System.Action<Monster> OnMonsterDied;
    public System.Action<Monster> OnMonsterDespawned;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Регистрирует монстра в менеджере
    /// </summary>
    [Server]
    public void RegisterMonster(Monster monster)
    {
        if (monster == null) return;
        
        uint netId = monster.netId;
        monstersByNetId[netId] = monster;
        
        // Добавляем по типу
        if (!monstersByType.ContainsKey(monster.monsterId))
        {
            monstersByType[monster.monsterId] = new List<Monster>();
        }
        monstersByType[monster.monsterId].Add(monster);
        
        // Добавляем по области (если есть система областей)
        string areaId = GetMonsterArea(monster);
        if (!string.IsNullOrEmpty(areaId))
        {
            if (!monstersByArea.ContainsKey(areaId))
            {
                monstersByArea[areaId] = new List<Monster>();
            }
            monstersByArea[areaId].Add(monster);
        }
        
        OnMonsterSpawned?.Invoke(monster);
        Debug.Log($"[MonsterManager] Registered monster: {monster.monsterName} (NetId: {netId}, Type: {monster.monsterId})");
    }
    
    /// <summary>
    /// Удаляет монстра из менеджера
    /// </summary>
    [Server]
    public void UnregisterMonster(Monster monster)
    {
        if (monster == null) return;
        
        uint netId = monster.netId;
        monstersByNetId.Remove(netId);
        
        // Удаляем по типу
        if (monstersByType.TryGetValue(monster.monsterId, out var typeList))
        {
            typeList.Remove(monster);
            if (typeList.Count == 0)
            {
                monstersByType.Remove(monster.monsterId);
            }
        }
        
        // Удаляем по области
        string areaId = GetMonsterArea(monster);
        if (!string.IsNullOrEmpty(areaId) && monstersByArea.TryGetValue(areaId, out var areaList))
        {
            areaList.Remove(monster);
            if (areaList.Count == 0)
            {
                monstersByArea.Remove(areaId);
            }
        }
        
        OnMonsterDespawned?.Invoke(monster);
        Debug.Log($"[MonsterManager] Unregistered monster: {monster.monsterName} (NetId: {netId})");
    }
    
    /// <summary>
    /// Получает монстра по NetId
    /// </summary>
    public Monster GetMonsterByNetId(uint netId)
    {
        return monstersByNetId.TryGetValue(netId, out var monster) ? monster : null;
    }
    
    /// <summary>
    /// Получает всех монстров
    /// </summary>
    public List<Monster> GetAllMonsters()
    {
        return monstersByNetId.Values.ToList();
    }
    
    /// <summary>
    /// Получает монстров по типу
    /// </summary>
    public List<Monster> GetMonstersByType(int monsterId)
    {
        return monstersByType.TryGetValue(monsterId, out var monsters) ? monsters : new List<Monster>();
    }
    
    /// <summary>
    /// Получает монстров в области
    /// </summary>
    public List<Monster> GetMonstersInArea(string areaId)
    {
        if (string.IsNullOrEmpty(areaId)) return new List<Monster>();
        return monstersByArea.TryGetValue(areaId, out var monsters) ? monsters : new List<Monster>();
    }
    
    /// <summary>
    /// Получает монстров в радиусе от позиции
    /// </summary>
    public List<Monster> GetMonstersInRadius(Vector3 position, float radius)
    {
        return monstersByNetId.Values
            .Where(m => Vector3.Distance(m.transform.position, position) <= radius)
            .ToList();
    }
    
    /// <summary>
    /// Получает ближайшего монстра к позиции
    /// </summary>
    public Monster GetNearestMonster(Vector3 position)
    {
        return monstersByNetId.Values
            .OrderBy(m => Vector3.Distance(m.transform.position, position))
            .FirstOrDefault();
    }
    
    /// <summary>
    /// Получает монстров в радиусе от игрока
    /// </summary>
    public List<Monster> GetMonstersNearPlayer(PlayerCore player, float radius)
    {
        if (player == null) return new List<Monster>();
        return GetMonstersInRadius(player.transform.position, radius);
    }
    
    /// <summary>
    /// Получает количество монстров по типу
    /// </summary>
    public int GetMonsterCountByType(int monsterId)
    {
        return GetMonstersByType(monsterId).Count;
    }
    
    /// <summary>
    /// Получает общее количество монстров
    /// </summary>
    public int GetTotalMonsterCount()
    {
        return monstersByNetId.Count;
    }
    
    /// <summary>
    /// Проверяет, есть ли монстры в области
    /// </summary>
    public bool HasMonstersInArea(string areaId)
    {
        return !string.IsNullOrEmpty(areaId) && monstersByArea.ContainsKey(areaId) && monstersByArea[areaId].Count > 0;
    }
    
    /// <summary>
    /// Получает область монстра (можно расширить логику)
    /// </summary>
    private string GetMonsterArea(Monster monster)
    {
        // Простая реализация - можно расширить
        Vector3 pos = monster.transform.position;
        
        // Определяем область по координатам
        if (pos.x > 0 && pos.z > 0) return "Area1";
        if (pos.x < 0 && pos.z > 0) return "Area2";
        if (pos.x > 0 && pos.z < 0) return "Area3";
        if (pos.x < 0 && pos.z < 0) return "Area4";
        
        return "Unknown";
    }
    
    /// <summary>
    /// Обновляет LOD для всех монстров (оптимизированная версия)
    /// </summary>
    public void UpdateMonsterLODs(PlayerCore localPlayer)
    {
        if (localPlayer == null) return;
        
        Vector3 playerPos = localPlayer.transform.position;
        
        foreach (var monster in monstersByNetId.Values)
        {
            if (monster == null) continue;
            
            float distance = Vector3.Distance(playerPos, monster.transform.position);
            
            // Обновляем LOD в зависимости от расстояния
            if (distance > 100f)
            {
                // Очень далеко - отключаем анимацию
                // TODO: Добавить метод SetLODLevel в класс Monster
                // monster.SetLODLevel(0);
            }
            else if (distance > 50f)
            {
                // Далеко - упрощенная анимация
                // TODO: Добавить метод SetLODLevel в класс Monster
                // monster.SetLODLevel(1);
            }
            else if (distance > 20f)
            {
                // Близко - полная анимация
                // TODO: Добавить метод SetLODLevel в класс Monster
                // monster.SetLODLevel(2);
            }
            else
            {
                // Очень близко - максимальное качество
                // TODO: Добавить метод SetLODLevel в класс Monster
                // monster.SetLODLevel(3);
            }
        }
    }
    
    /// <summary>
    /// Получает статистику менеджера
    /// </summary>
    public (int totalMonsters, int totalTypes, int totalAreas) GetStats()
    {
        return (monstersByNetId.Count, monstersByType.Count, monstersByArea.Count);
    }
    
    /// <summary>
    /// Очищает все данные (для смены сцены)
    /// </summary>
    [Server]
    public void ClearAll()
    {
        monstersByNetId.Clear();
        monstersByType.Clear();
        monstersByArea.Clear();
        Debug.Log("[MonsterManager] Cleared all monster data");
    }
}
