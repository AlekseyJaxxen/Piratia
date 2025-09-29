using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class MinimapGenerator : NetworkBehaviour
{
    [Header("Minimap Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private int minimapSize = 256;
    [SerializeField] private float minimapRange = 50f;
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Player Reference")]
    [SerializeField] private Transform playerToFollow;
    
    [Header("Colors")]
    [SerializeField] private Color playerColor = Color.green;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color neutralColor = Color.yellow;
    
    // Компоненты мини-карты
    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private GameObject minimapPanel;
    private RawImage minimapImage;
    private RawImage playerArrow;
    private Canvas minimapCanvas;
    
    // Кэш объектов
    private Transform playerTransform;
    private Camera playerCamera;
    private float lastUpdateTime;
    
    // Кэш маркеров
    private Dictionary<Transform, GameObject> markerCache = new Dictionary<Transform, GameObject>();
    
    void Start()
    {
        // Проверяем, что мини-карта еще не создана
        if (minimapPanel != null)
        {
            Debug.LogWarning("Minimap already exists, skipping creation");
            return;
        }
        
        // Проверяем, что это локальный игрок
        if (!isLocalPlayer)
        {
            Debug.Log("Not local player, skipping minimap creation");
            return;
        }
        
        // Создаем мини-карту с задержкой
        StartCoroutine(DelayedMinimapCreation());
    }
    
    IEnumerator DelayedMinimapCreation()
    {
        // Ждем 2 секунды
        yield return new WaitForSeconds(2f);
        
        // Находим игрока для следования
        FindPlayerToFollow();
        
        // Проверяем, что игрок найден
        if (playerTransform == null)
        {
            Debug.LogError("PlayerTransform is null after delay! Please assign playerToFollow in Inspector.");
            yield break;
        }
        
        Debug.Log($"PlayerTransform found after delay: {playerTransform.name}");
        
        // Создаем мини-карту только если игрок найден
        CreateMinimap();
        
        // Находим камеру игрока
        playerCamera = FindObjectOfType<Camera>();
    }
    
    void FindPlayerToFollow()
    {
        // Если игрок назначен в Inspector, используем его
        if (playerToFollow != null)
        {
            playerTransform = playerToFollow;
            Debug.Log($"Using assigned player: {playerTransform.name}");
            return;
        }
        
        // Иначе используем текущий объект (если компонент на игроке)
        if (transform.GetComponent<PlayerCore>() != null || transform.GetComponent<NetworkIdentity>() != null)
        {
            playerTransform = transform;
            Debug.Log($"Using current object as player: {playerTransform.name}");
            return;
        }
        
        // Иначе ищем игрока автоматически
        Transform player = FindPlayer();
        
        if (player != null)
        {
            playerTransform = player;
            Debug.Log($"Auto-found player: {playerTransform.name}");
        }
        else
        {
            Debug.LogError("Player not found! Please assign playerToFollow in Inspector.");
        }
    }
    
    Transform FindPlayer()
    {
        // По компоненту NetworkIdentity с isLocalPlayer
        NetworkIdentity[] networkIdentities = FindObjectsOfType<NetworkIdentity>();
        foreach (NetworkIdentity ni in networkIdentities)
        {
            if (ni.isLocalPlayer)
            {
                Debug.Log("Player found by NetworkIdentity isLocalPlayer");
                return ni.transform;
            }
        }
        
        Debug.LogWarning("Player not found by isLocalPlayer");
        return null;
    }
    
    void CreateMinimap()
    {
        // Создаем Canvas для мини-карты
        CreateMinimapCanvas();
        
        // Создаем камеру мини-карты
        CreateMinimapCamera();
        
        // Создаем RenderTexture
        CreateMinimapTexture();
        
        // Создаем UI элементы
        CreateMinimapUI();
        
        // Настраиваем камеру
        SetupMinimapCamera();
        
        // Создаем тестовые объекты для проверки
        CreateTestObjects();
    }
    
    void CreateMinimapCanvas()
    {
        GameObject canvasGO = new GameObject("MinimapCanvas");
        canvasGO.transform.SetParent(transform);
        
        minimapCanvas = canvasGO.AddComponent<Canvas>();
        minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        minimapCanvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();
    }
    
    void CreateMinimapCamera()
    {
        GameObject cameraGO = new GameObject("MinimapCamera");
        // НЕ прикрепляем к игроку, чтобы камера не крутилась
        cameraGO.transform.SetParent(null);
        
        minimapCamera = cameraGO.AddComponent<Camera>();
        minimapCamera.orthographic = false; // Perspective камера
        minimapCamera.fieldOfView = 60f; // Угол обзора
        // Видим только статичные объекты (ground, строения), но не игровые объекты
        // Исключаем слои с игроками и монстрами
        minimapCamera.cullingMask = LayerMask.GetMask("Default", "Ground", "Buildings", "Environment") & ~LayerMask.GetMask("Player", "Monster", "Enemy");
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0, 0, 0, 0.0f); // Прозрачный фон
        minimapCamera.depth = 10; // Выше основной камеры
        
        // Настраиваем камеру для вида сверху (Perspective)
        minimapCamera.transform.position = new Vector3(0, 50, 0);
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
    
    void CreateMinimapTexture()
    {
        minimapTexture = new RenderTexture(minimapSize, minimapSize, 24); // 24-bit depth buffer
        minimapTexture.filterMode = FilterMode.Point;
        minimapTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
        minimapCamera.targetTexture = minimapTexture;
    }
    
    void CreateMinimapUI()
    {
        // Создаем панель мини-карты
        minimapPanel = new GameObject("MinimapPanel");
        minimapPanel.transform.SetParent(minimapCanvas.transform);
        
        RectTransform panelRect = minimapPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-150, -150);
        panelRect.sizeDelta = new Vector2(minimapSize, minimapSize);
        
        Image panelImage = minimapPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.3f); // Полупрозрачный фон для видимости карты
        
        // Добавляем возможность перетаскивания
        AddDragFunctionality(panelRect);
        
        // Создаем изображение мини-карты
        GameObject mapImageGO = new GameObject("MinimapImage");
        mapImageGO.transform.SetParent(minimapPanel.transform);
        
        RectTransform mapRect = mapImageGO.AddComponent<RectTransform>();
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        
        minimapImage = mapImageGO.AddComponent<RawImage>();
        minimapImage.texture = minimapTexture;
        
        // Создаем стрелку направления камеры
        CreatePlayerArrow();
        
        // Создаем символ N (North)
        CreateNorthSymbol();
        
        // Создаем рамку (убрано)
        // CreateMinimapBorder();
    }
    
    void CreatePlayerArrow()
    {
        GameObject arrowGO = new GameObject("PlayerArrow");
        arrowGO.transform.SetParent(minimapPanel.transform);
        
        RectTransform arrowRect = arrowGO.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrowRect.sizeDelta = new Vector2(20, 30);
        
        playerArrow = arrowGO.AddComponent<RawImage>();
        playerArrow.color = playerColor;
        
        // Создаем текстуру стрелки
        Texture2D arrowTexture = CreateArrowTexture();
        playerArrow.texture = arrowTexture;
    }
    
    void CreateNorthSymbol()
    {
        GameObject northGO = new GameObject("NorthSymbol");
        northGO.transform.SetParent(minimapPanel.transform);
        
        RectTransform northRect = northGO.AddComponent<RectTransform>();
        northRect.anchorMin = new Vector2(0.5f, 1f);
        northRect.anchorMax = new Vector2(0.5f, 1f);
        northRect.anchoredPosition = new Vector2(0, -20);
        northRect.sizeDelta = new Vector2(20, 20);
        
        Text northText = northGO.AddComponent<Text>();
        northText.text = "N";
        northText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        northText.fontSize = 16;
        northText.color = Color.white;
        northText.alignment = TextAnchor.MiddleCenter;
    }
    
    void SetupMinimapCamera()
    {
        // Настраиваем камеру для вида сверху
        minimapCamera.transform.position = new Vector3(0, 100, 0);
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Добавляем отладочную информацию
        Debug.Log($"Minimap camera setup: pos={minimapCamera.transform.position}, rot={minimapCamera.transform.rotation}, size={minimapCamera.orthographicSize}");
    }
    
    void CreateMinimapLayer()
    {
        // Создаем слой для мини-карты
        int minimapLayer = LayerMask.NameToLayer("Minimap");
        if (minimapLayer == -1)
        {
            Debug.LogWarning("Minimap layer not found. Please create 'Minimap' layer in project settings.");
        }
    }
    
    // Добавляем метод для создания тестовых объектов
    void CreateTestObjects()
    {
        // Создаем тестовый куб для проверки
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.transform.position = new Vector3(10, 0, 10);
        testCube.name = "TestCube";
        
        // Создаем тестовую сферу
        GameObject testSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        testSphere.transform.position = new Vector3(-10, 0, -10);
        testSphere.name = "TestSphere";
        
        Debug.Log("Created test objects for minimap");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMinimap();
        }
        
        // Обновляем мини-карту с интервалом, когда панель активна
        if (minimapPanel != null && minimapPanel.activeSelf && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMinimap();
            lastUpdateTime = Time.time;
        }
    }
    
    void ToggleMinimap()
    {
        minimapPanel.SetActive(!minimapPanel.activeSelf);
    }
    
    void UpdateMinimap()
    {
        if (playerTransform == null) 
        {
            Debug.LogWarning("PlayerTransform is null in UpdateMinimap");
            return;
        }
        
        if (minimapCamera == null)
        {
            Debug.LogWarning("MinimapCamera is null in UpdateMinimap");
            return;
        }
        
        // Обновляем позицию камеры мини-карты (следует за игроком, но не крутится)
        Vector3 playerPos = playerTransform.position;
        minimapCamera.transform.position = new Vector3(playerPos.x, 50, playerPos.z);
        // Камера всегда смотрит вниз (не крутится) - фиксированная ориентация
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Отладочная информация
        if (Time.frameCount % 60 == 0) // Каждую секунду
        {
            Debug.Log($"Minimap update: playerPos={playerPos}, cameraPos={minimapCamera.transform.position}, cameraRot={minimapCamera.transform.rotation}");
        }
        
        // Обновляем направление стрелки (показывает направление камеры)
        if (playerCamera != null)
        {
            float yRotation = playerCamera.transform.eulerAngles.y;
            playerArrow.transform.rotation = Quaternion.Euler(0, 0, -yRotation);
        }
        
        // Обновляем маркеры объектов
        UpdateObjectMarkers();
    }
    
    void UpdateObjectMarkers()
    {
        // Находим всех игроков
        PlayerCore[] players = FindObjectsOfType<PlayerCore>();
        foreach (PlayerCore player in players)
        {
            if (player == playerTransform.GetComponent<PlayerCore>()) continue;
            
            CreateOrUpdateMarker(player.transform, GetPlayerColor(player));
        }
        
        // Находим всех монстров (только живых)
        Monster[] monsters = FindObjectsOfType<Monster>();
        foreach (Monster monster in monsters)
        {
            // Показываем маркер только если монстр жив
            if (!monster.IsDead)
            {
                CreateOrUpdateMarker(monster.transform, enemyColor);
            }
            else
            {
                // Удаляем маркер мертвого монстра
                if (markerCache.ContainsKey(monster.transform))
                {
                    Destroy(markerCache[monster.transform]);
                    markerCache.Remove(monster.transform);
                }
            }
        }
        
        // Очищаем кэш от несуществующих объектов
        CleanupMarkerCache();
    }
    
    void CleanupMarkerCache()
    {
        // Удаляем маркеры для объектов, которые больше не существуют
        List<Transform> keysToRemove = new List<Transform>();
        foreach (var kvp in markerCache)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (Transform key in keysToRemove)
        {
            if (markerCache.ContainsKey(key))
            {
                if (markerCache[key] != null)
                {
                    Destroy(markerCache[key]);
                }
                markerCache.Remove(key);
            }
        }
    }
    
    Color GetPlayerColor(PlayerCore player)
    {
        // Логика определения цвета игрока
        if (player.team == PlayerTeam.Red) return Color.red;
        if (player.team == PlayerTeam.Blue) return Color.blue;
        return neutralColor;
    }
    
    void CreateOrUpdateMarker(Transform target, Color color)
    {
        // Проверяем расстояние
        float distance = Vector3.Distance(target.position, playerTransform.position);
        if (distance > minimapRange)
        {
            // Удаляем маркер если объект слишком далеко
            if (markerCache.ContainsKey(target))
            {
                Destroy(markerCache[target]);
                markerCache.Remove(target);
            }
            return;
        }
        
        // Создаем или обновляем маркер
        if (!markerCache.ContainsKey(target))
        {
            markerCache[target] = CreateMarker(target, color);
        }
        
        // Обновляем позицию маркера
        UpdateMarkerPosition(markerCache[target], target);
        
        // Показываем/скрываем маркер в зависимости от расстояния
        GameObject marker = markerCache[target];
        if (marker != null)
        {
            marker.SetActive(distance <= minimapRange);
        }
    }
    
    GameObject CreateMarker(Transform target, Color color)
    {
        GameObject marker = new GameObject($"Marker_{target.name}");
        marker.transform.SetParent(minimapPanel.transform);
        
        RectTransform markerRect = marker.AddComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(8, 8);
        
        Image markerImage = marker.AddComponent<Image>();
        markerImage.color = color;
        
        return marker;
    }
    
    void UpdateMarkerPosition(GameObject marker, Transform target)
    {
        Vector3 relativePos = target.position - playerTransform.position;
        Vector2 minimapPos = new Vector2(
            relativePos.x / minimapRange * minimapSize,
            relativePos.z / minimapRange * minimapSize
        );
        
        // Ограничиваем позицию маркера в пределах мини-карты
        float halfSize = minimapSize * 0.5f;
        minimapPos.x = Mathf.Clamp(minimapPos.x, -halfSize, halfSize);
        minimapPos.y = Mathf.Clamp(minimapPos.y, -halfSize, halfSize);
        
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        markerRect.anchoredPosition = minimapPos;
    }
    
    Texture2D CreateArrowTexture()
    {
        Texture2D texture = new Texture2D(20, 30);
        Color[] pixels = new Color[20 * 30];
        
        // Рисуем стрелку в виде буквы V (правильная форма)
        for (int y = 0; y < 30; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                int index = y * 20 + x;
                
                // Стрелка в виде V
                if (y < 15)
                {
                    // Вертикальная линия (ствол стрелки)
                    if (x >= 9 && x <= 10)
                    {
                        pixels[index] = Color.white;
                    }
                }
                else
                {
                    // Наклонные линии V (наконечник)
                    int centerX = 10;
                    int offset = y - 15;
                    if (offset <= 10) // Ограничиваем размер наконечника
                    {
                        if ((x >= centerX - offset && x <= centerX - offset + 1) ||
                            (x >= centerX + offset - 1 && x <= centerX + offset))
                        {
                            pixels[index] = Color.white;
                        }
                    }
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    Sprite CreateBorderSprite()
    {
        Texture2D borderTexture = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        
        for (int i = 0; i < 16; i++)
        {
            pixels[i] = Color.white;
        }
        
        borderTexture.SetPixels(pixels);
        borderTexture.Apply();
        
        return Sprite.Create(borderTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }
    
    void AddDragFunctionality(RectTransform panelRect)
    {
        // Добавляем компонент для перетаскивания
        MinimapDragger dragger = minimapPanel.AddComponent<MinimapDragger>();
        dragger.Initialize(panelRect);
    }
    
    void OnDestroy()
    {
        // Очищаем кэш маркеров
        foreach (var marker in markerCache.Values)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        markerCache.Clear();
        
        // Очищаем RenderTexture
        if (minimapTexture != null)
        {
            minimapTexture.Release();
        }
        
        // Уничтожаем камеру мини-карты
        if (minimapCamera != null)
        {
            Destroy(minimapCamera.gameObject);
        }
    }
}

// Компонент для перетаскивания мини-карты
public class MinimapDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Vector2 offset;
    private bool isDragging = false;
    private Vector2 targetPosition;
    
    public void Initialize(RectTransform rect)
    {
        rectTransform = rect;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        offset = rectTransform.anchoredPosition - localPoint;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        targetPosition = localPoint + offset;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
    
    void LateUpdate()
    {
        if (isDragging && rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPosition;
        }
    }
}
