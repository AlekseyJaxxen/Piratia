using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class NpcBehaviour : NetworkBehaviour
{
    [Header("NPC Settings")]
    [SyncVar(hook = nameof(OnNpcNameChanged))]
    [SerializeField] private string npcName = "NPC";
    [SyncVar(hook = nameof(OnDialogueTextChanged))]
    [SerializeField] private string dialogueText = "Hello, adventurer!";
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private float nameTagScale = 0.005f;
    [SerializeField] private float nameTagFontSize = 1f;
    
    [Header("UI References")]
    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private TextMeshProUGUI nameTagText;
    
    [Header("Context Menu Buttons")]
    [SyncVar(hook = nameof(OnButtonTextsChanged))]
    [SerializeField] private string[] buttonTexts = { "Bank", "Trade" };
    
    private GameObject nameTagInstance;
    private NpcContextMenu contextMenu;
    private bool isPlayerNearby = false;
    private PlayerCore nearbyPlayer;
    
    public string NpcName => npcName;
    public string DialogueText => dialogueText;
    public float InteractionRadius => interactionRadius;
    public string[] ButtonTexts => buttonTexts;
    
    // Методы для установки значений (только на сервере)
    [Server]
    public void SetNpcName(string name)
    {
        npcName = name;
    }
    
    [Server]
    public void SetDialogueText(string text)
    {
        dialogueText = text;
    }
    
    [Server]
    public void SetButtonTexts(string[] texts)
    {
        buttonTexts = texts;
    }
    
    [Server]
    public void SetInteractionRadius(float radius)
    {
        interactionRadius = radius;
    }
    
    // Хуки для синхронизации
    private void OnNpcNameChanged(string oldName, string newName)
    {
        npcName = newName;
        if (nameTagText != null)
        {
            nameTagText.text = newName;
        }
        Debug.Log($"[NpcBehaviour] NpcName synchronized: {newName}");
    }
    
    private void OnDialogueTextChanged(string oldText, string newText)
    {
        dialogueText = newText;
        Debug.Log($"[NpcBehaviour] DialogueText synchronized: {newText}");
    }
    
    private void OnButtonTextsChanged(string[] oldTexts, string[] newTexts)
    {
        buttonTexts = newTexts;
        Debug.Log($"[NpcBehaviour] ButtonTexts synchronized: {string.Join(", ", newTexts)}");
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        CreateNameTag();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Создаем name tag только на клиенте, не на сервере
        if (!isServer)
        {
            CreateNameTag();
        }
    }
    
    private void CreateNameTag()
    {
        // Проверяем, не создан ли уже name tag
        if (nameTagInstance != null)
        {
            return;
        }
        
        if (nameTagPrefab == null)
        {
            // Создаем простой name tag если префаб не назначен
            CreateDefaultNameTag();
        }
        else
        {
            nameTagInstance = Instantiate(nameTagPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            nameTagInstance.transform.SetParent(worldSpaceCanvas?.transform ?? transform);
            
            nameTagText = nameTagInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (nameTagText != null)
            {
                nameTagText.text = npcName;
            }
        }
    }
    
    private void CreateDefaultNameTag()
    {
        // Создаем Canvas если его нет
        if (worldSpaceCanvas == null)
        {
            GameObject canvasObj = new GameObject("NPC_WorldSpaceCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.zero;
            
            worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
            worldSpaceCanvas.worldCamera = Camera.main;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = nameTagScale;
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Создаем name tag
        GameObject nameTagObj = new GameObject("NameTag");
        nameTagObj.transform.SetParent(worldSpaceCanvas.transform);
        nameTagObj.transform.localPosition = new Vector3(0, 2f, 0);
        nameTagObj.transform.localScale = Vector3.one;
        
        // Настраиваем размер поля текста
        RectTransform nameTagRect = nameTagObj.AddComponent<RectTransform>();
        nameTagRect.sizeDelta = new Vector2(10, 5);
        
        // Добавляем TextMeshPro
        nameTagText = nameTagObj.AddComponent<TextMeshProUGUI>();
        nameTagText.text = npcName;
        nameTagText.fontSize = nameTagFontSize;
        nameTagText.color = Color.white;
        nameTagText.alignment = TextAlignmentOptions.Center;
        nameTagText.fontStyle = FontStyles.Bold;
        
        // Добавляем Outline для лучшей читаемости
        Outline outline = nameTagObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);
        
        nameTagInstance = nameTagObj;
    }
    
    private void Update()
    {
        if (isServer)
        {
            CheckPlayerProximity();
        }
        
        // Поворачиваем name tag к камере
        if (nameTagInstance != null && Camera.main != null)
        {
            nameTagInstance.transform.LookAt(Camera.main.transform);
            nameTagInstance.transform.Rotate(0, 180, 0);
        }
        
        // Обновляем размер шрифта если он изменился
        if (nameTagText != null && nameTagText.fontSize != nameTagFontSize)
        {
            nameTagText.fontSize = nameTagFontSize;
        }
        
        // Обновляем масштаб Canvas если он изменился
        if (worldSpaceCanvas != null)
        {
            CanvasScaler scaler = worldSpaceCanvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.scaleFactor != nameTagScale)
            {
                scaler.scaleFactor = nameTagScale;
            }
        }
    }
    
    private void CheckPlayerProximity()
    {
        PlayerCore[] players = FindObjectsOfType<PlayerCore>();
        bool wasPlayerNearby = isPlayerNearby;
        isPlayerNearby = false;
        nearbyPlayer = null;
        
        foreach (var player in players)
        {
            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= interactionRadius)
            {
                isPlayerNearby = true;
                nearbyPlayer = player;
                break;
            }
        }
        
        // Если игрок отошел, закрываем контекстное меню
        if (wasPlayerNearby && !isPlayerNearby && contextMenu != null)
        {
            contextMenu.HideContextMenu();
        }
    }
    
    public void ShowContextMenu(PlayerCore player)
    {
        Debug.Log($"[NpcBehaviour] ShowContextMenu called for {npcName}");
        
        if (contextMenu == null)
        {
            Debug.Log("[NpcBehaviour] ContextMenu is null, searching for existing one...");
            contextMenu = FindObjectOfType<NpcContextMenu>();
            if (contextMenu == null)
            {
                Debug.Log("[NpcBehaviour] No existing ContextMenu found, creating new one...");
                // Создаем NpcContextMenu если его нет
                CreateNpcContextMenu();
            }
            else
            {
                Debug.Log("[NpcBehaviour] Found existing ContextMenu");
            }
        }
        
        if (contextMenu != null)
        {
            Debug.Log($"[NpcBehaviour] Calling contextMenu.ShowContextMenu for {npcName}");
            contextMenu.ShowContextMenu(this, player);
        }
        else
        {
            Debug.LogError("[NpcBehaviour] ContextMenu is still null after creation attempt!");
        }
    }
    
    private void CreateNpcContextMenu()
    {
        Debug.Log("[NpcBehaviour] Creating NpcContextMenu...");
        
        // Создаем Canvas для контекстного меню
        GameObject canvasObj = new GameObject("NpcContextMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Создаем NpcContextMenu
        GameObject contextMenuObj = new GameObject("NpcContextMenu");
        contextMenuObj.transform.SetParent(canvasObj.transform, false);
        
        contextMenu = contextMenuObj.AddComponent<NpcContextMenu>();
        
        // Делаем объект постоянным между сценами (только для root объекта)
        DontDestroyOnLoad(canvasObj);
        
        Debug.Log("[NpcBehaviour] Created NpcContextMenu automatically");
        
        if (contextMenu == null)
        {
            Debug.LogError("[NpcBehaviour] Failed to create NpcContextMenu component!");
        }
        else
        {
            Debug.Log("[NpcBehaviour] NpcContextMenu component created successfully");
        }
    }
    
    public void OnBankButton()
    {
        Debug.Log($"[NpcBehaviour] Bank button clicked for {npcName}");
        // TODO: Реализовать банковскую систему
    }
    
    public void OnTradeButton()
    {
        Debug.Log($"[NpcBehaviour] Trade button clicked for {npcName}");
        // TODO: Реализовать торговую систему
    }
    
    private void OnDestroy()
    {
        if (nameTagInstance != null)
        {
            Destroy(nameTagInstance);
        }
    }
    
    [ContextMenu("Update Name Tag")]
    private void UpdateNameTag()
    {
        if (nameTagText != null)
        {
            nameTagText.text = npcName;
            nameTagText.fontSize = nameTagFontSize;
            Debug.Log($"[NpcBehaviour] Updated name tag: {npcName}, font size: {nameTagFontSize}");
        }
        else
        {
            Debug.LogWarning("[NpcBehaviour] Name tag text is null, cannot update");
        }
    }
    
    [ContextMenu("Recreate Name Tag")]
    private void RecreateNameTag()
    {
        // Уничтожаем старый name tag
        if (nameTagInstance != null)
        {
            DestroyImmediate(nameTagInstance);
            nameTagInstance = null;
            nameTagText = null;
        }
        
        // Создаем новый
        CreateNameTag();
        Debug.Log($"[NpcBehaviour] Recreated name tag with font size: {nameTagFontSize}");
    }
    
    [ContextMenu("Test Show Context Menu")]
    private void TestShowContextMenu()
    {
        Debug.Log($"[NpcBehaviour] Testing ShowContextMenu for {npcName}");
        Debug.Log($"[NpcBehaviour] Button texts: {string.Join(", ", buttonTexts)}");
        
        // Находим локального игрока
        PlayerCore localPlayer = FindObjectOfType<PlayerCore>();
        if (localPlayer != null)
        {
            ShowContextMenu(localPlayer);
        }
        else
        {
            Debug.LogWarning("[NpcBehaviour] No PlayerCore found for testing");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Показываем радиус взаимодействия в редакторе
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
