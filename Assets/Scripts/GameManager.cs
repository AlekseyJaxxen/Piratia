using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Централизованный менеджер для управления игровыми системами
/// Заменяет использование FindObjectOfType для поиска систем
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("System References")]
    [SerializeField] private StarterItemsSystem starterItemsSystem;
    [SerializeField] private NpcContextMenu npcContextMenu;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Camera mainCamera;
    
    [Header("Managers")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private MonsterManager monsterManager;
    
    [Header("Events")]
    public System.Action OnGameInitialized;
    public System.Action OnGameStarted;
    public System.Action OnGameEnded;
    
    private Dictionary<System.Type, object> systemCache = new Dictionary<System.Type, object>();
    private bool isInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeGame()
    {
        if (isInitialized) return;
        
        // Инициализируем менеджеры
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
            if (playerManager == null)
            {
                GameObject managerGO = new GameObject("PlayerManager");
                playerManager = managerGO.AddComponent<PlayerManager>();
            }
        }
        
        if (monsterManager == null)
        {
            monsterManager = FindObjectOfType<MonsterManager>();
            if (monsterManager == null)
            {
                GameObject managerGO = new GameObject("MonsterManager");
                monsterManager = managerGO.AddComponent<MonsterManager>();
            }
        }
        
        // Кэшируем системы
        CacheSystems();
        
        isInitialized = true;
        OnGameInitialized?.Invoke();
        
        Debug.Log("[GameManager] Game initialized successfully");
    }
    
    private void CacheSystems()
    {
        // Кэшируем основные системы
        if (starterItemsSystem == null)
        {
            starterItemsSystem = FindObjectOfType<StarterItemsSystem>();
        }
        
        if (npcContextMenu == null)
        {
            npcContextMenu = FindObjectOfType<NpcContextMenu>();
        }
        
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        // Добавляем в кэш
        if (starterItemsSystem != null) systemCache[typeof(StarterItemsSystem)] = starterItemsSystem;
        if (npcContextMenu != null) systemCache[typeof(NpcContextMenu)] = npcContextMenu;
        if (mainCanvas != null) systemCache[typeof(Canvas)] = mainCanvas;
        if (mainCamera != null) systemCache[typeof(Camera)] = mainCamera;
    }
    
    /// <summary>
    /// Получает систему по типу из кэша
    /// </summary>
    public T GetSystem<T>() where T : UnityEngine.Object
    {
        if (systemCache.TryGetValue(typeof(T), out var system))
        {
            return system as T;
        }
        
        // Если не найдено в кэше, ищем и кэшируем
        T foundSystem = FindObjectOfType<T>();
        if (foundSystem != null)
        {
            systemCache[typeof(T)] = foundSystem;
        }
        
        return foundSystem;
    }
    
    /// <summary>
    /// Регистрирует систему в менеджере
    /// </summary>
    public void RegisterSystem<T>(T system) where T : UnityEngine.Object
    {
        if (system != null)
        {
            systemCache[typeof(T)] = system;
            Debug.Log($"[GameManager] Registered system: {typeof(T).Name}");
        }
    }
    
    /// <summary>
    /// Получает StarterItemsSystem
    /// </summary>
    public StarterItemsSystem GetStarterItemsSystem()
    {
        return GetSystem<StarterItemsSystem>();
    }
    
    /// <summary>
    /// Получает NpcContextMenu
    /// </summary>
    public NpcContextMenu GetNpcContextMenu()
    {
        return GetSystem<NpcContextMenu>();
    }
    
    /// <summary>
    /// Получает главный Canvas
    /// </summary>
    public Canvas GetMainCanvas()
    {
        return GetSystem<Canvas>();
    }
    
    /// <summary>
    /// Получает главную камеру
    /// </summary>
    public Camera GetMainCamera()
    {
        return GetSystem<Camera>();
    }
    
    /// <summary>
    /// Получает PlayerManager
    /// </summary>
    public PlayerManager GetPlayerManager()
    {
        return playerManager;
    }
    
    /// <summary>
    /// Получает MonsterManager
    /// </summary>
    public MonsterManager GetMonsterManager()
    {
        return monsterManager;
    }
    
    /// <summary>
    /// Получает локального игрока
    /// </summary>
    public PlayerCore GetLocalPlayer()
    {
        return playerManager?.GetLocalPlayer();
    }
    
    /// <summary>
    /// Получает всех игроков
    /// </summary>
    public List<PlayerCore> GetAllPlayers()
    {
        return playerManager?.GetAllPlayers() ?? new List<PlayerCore>();
    }
    
    /// <summary>
    /// Получает игроков в группе
    /// </summary>
    public List<PlayerCore> GetPlayersInParty(string partyId)
    {
        return playerManager?.GetPlayersInParty(partyId) ?? new List<PlayerCore>();
    }
    
    /// <summary>
    /// Получает всех монстров
    /// </summary>
    public List<Monster> GetAllMonsters()
    {
        return monsterManager?.GetAllMonsters() ?? new List<Monster>();
    }
    
    /// <summary>
    /// Получает монстров по типу
    /// </summary>
    public List<Monster> GetMonstersByType(int monsterId)
    {
        return monsterManager?.GetMonstersByType(monsterId) ?? new List<Monster>();
    }
    
    /// <summary>
    /// Получает монстров в радиусе
    /// </summary>
    public List<Monster> GetMonstersInRadius(Vector3 position, float radius)
    {
        return monsterManager?.GetMonstersInRadius(position, radius) ?? new List<Monster>();
    }
    
    /// <summary>
    /// Проверяет, инициализирована ли игра
    /// </summary>
    public bool IsGameInitialized()
    {
        return isInitialized;
    }
    
    /// <summary>
    /// Запускает игру
    /// </summary>
    [Server]
    public void StartGame()
    {
        OnGameStarted?.Invoke();
        Debug.Log("[GameManager] Game started");
    }
    
    /// <summary>
    /// Завершает игру
    /// </summary>
    [Server]
    public void EndGame()
    {
        OnGameEnded?.Invoke();
        Debug.Log("[GameManager] Game ended");
    }
    
    /// <summary>
    /// Очищает кэш систем
    /// </summary>
    public void ClearSystemCache()
    {
        systemCache.Clear();
        Debug.Log("[GameManager] System cache cleared");
    }
    
    /// <summary>
    /// Получает статистику менеджера
    /// </summary>
    public (int cachedSystems, bool initialized) GetStats()
    {
        return (systemCache.Count, isInitialized);
    }
}
