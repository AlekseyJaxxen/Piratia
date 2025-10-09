using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NpcContextMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button contextButtonPrefab;
    
    [Header("Settings")]
    [SerializeField] private float menuOffset = 10f;
    
    private List<Button> contextButtons = new List<Button>();
    private NpcBehaviour currentNpc;
    private PlayerCore currentPlayer;
    private Canvas canvas;
    
    public static NpcContextMenu Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad будет вызван для root объекта (Canvas) в NpcBehaviour
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        canvas = GetComponentInParent<Canvas>();
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }
        
        // Очищаем старые Panel'ы если они есть
        CleanupOldPanels();
        
        // Если UI элементы не назначены, создаем их автоматически
        if (contextMenuPanel == null)
        {
            CreateDefaultUI();
        }
    }
    
    private void Start()
    {
        // UI элементы уже созданы в Awake если нужно
    }
    
    private void CleanupOldPanels()
    {
        if (canvas == null) return;
        
        // Удаляем все старые ContextMenuPanel кроме текущей
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name == "ContextMenuPanel" && child.gameObject != contextMenuPanel)
            {
                Debug.Log($"[NpcContextMenu] Removing old panel: {child.name}");
                Destroy(child.gameObject);
            }
        }
        
        // Также удаляем старые ContextButton'ы, которые могли остаться в корне Canvas
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name.StartsWith("ContextButton"))
            {
                Debug.Log($"[NpcContextMenu] Removing old button: {child.name}");
                Destroy(child.gameObject);
            }
        }
    }
    
    private void CreateDefaultUI()
    {
        Debug.Log("[NpcContextMenu] Creating default UI...");
        
        // Используем существующий Canvas или создаем новый только если его нет
        if (canvas == null)
        {
            // Сначала попробуем найти существующий Canvas
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = existingCanvas;
                Debug.Log("[NpcContextMenu] Using existing Canvas");
            }
            else
            {
                Debug.Log("[NpcContextMenu] Creating new Canvas...");
                GameObject canvasObj = new GameObject("NpcContextMenuCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("[NpcContextMenu] Canvas created");
            }
        }
        
        // Проверяем, есть ли уже Panel в Canvas
        if (contextMenuPanel == null)
        {
            // Ищем существующий Panel
            Transform existingPanel = canvas.transform.Find("ContextMenuPanel");
            if (existingPanel != null)
            {
                contextMenuPanel = existingPanel.gameObject;
                Debug.Log("[NpcContextMenu] Found existing Panel");
            }
            else
            {
                // Создаем новую панель меню
                GameObject panelObj = new GameObject("ContextMenuPanel");
                panelObj.transform.SetParent(canvas.transform, false);
                
                contextMenuPanel = panelObj;
                RectTransform panelRect = panelObj.AddComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(200, 180);  // Увеличили высоту
                
                Image panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                
                // Добавляем Outline
                Outline panelOutline = panelObj.AddComponent<Outline>();
                panelOutline.effectColor = Color.white;
                panelOutline.effectDistance = new Vector2(2, 2);
                
                Debug.Log("[NpcContextMenu] Created new Panel");
            }
        }
        
        // Проверяем, есть ли уже ButtonContainer в Panel
        if (buttonContainer == null)
        {
            Transform existingContainer = contextMenuPanel.transform.Find("ButtonContainer");
            if (existingContainer != null)
            {
                buttonContainer = existingContainer;
                Debug.Log("[NpcContextMenu] Found existing ButtonContainer");
            }
            else
            {
                // Создаем контейнер для кнопок
                GameObject containerObj = new GameObject("ButtonContainer");
                containerObj.transform.SetParent(contextMenuPanel.transform, false);
                
                buttonContainer = containerObj.transform;
                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                
                GridLayoutGroup gridLayout = containerObj.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(85, 30);
                gridLayout.spacing = new Vector2(5, 5);
                gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 2; // 2 колонки
                
                Debug.Log("[NpcContextMenu] Created new ButtonContainer");
            }
        }
        
        // Создаем тексты для имени и диалога только если их нет
        if (npcNameText == null || dialogueText == null)
        {
            CreateTextElements(contextMenuPanel);
        }
        
        // Создаем префаб кнопки только если его нет
        if (contextButtonPrefab == null)
        {
            CreateButtonPrefab();
        }
        
        contextMenuPanel.SetActive(false);
        
        Debug.Log($"[NpcContextMenu] Default UI created - Panel: {contextMenuPanel != null}, Container: {buttonContainer != null}, Prefab: {contextButtonPrefab != null}");
    }
    
    private void CreateTextElements(GameObject parent)
    {
        // Имя NPC - в верхней части
        GameObject nameObj = new GameObject("NpcNameText");
        nameObj.transform.SetParent(parent.transform, false);
        
        npcNameText = nameObj.AddComponent<TextMeshProUGUI>();
        npcNameText.text = "NPC Name";
        npcNameText.fontSize = 16;
        npcNameText.color = Color.yellow;
        npcNameText.alignment = TextAlignmentOptions.Center;
        npcNameText.fontStyle = FontStyles.Bold;
        
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.8f);  // Выше
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(5, 5);
        nameRect.offsetMax = new Vector2(-5, -5);
        
        // Диалог NPC - в средней части
        GameObject dialogueObj = new GameObject("DialogueText");
        dialogueObj.transform.SetParent(parent.transform, false);
        
        dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "Dialogue text";
        dialogueText.fontSize = 12;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.Center;
        
        RectTransform dialogueRect = dialogueObj.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0, 0.5f);  // Выше
        dialogueRect.anchorMax = new Vector2(1, 0.8f);  // Выше
        dialogueRect.offsetMin = new Vector2(5, 30);   // 30 по bottom
        dialogueRect.offsetMax = new Vector2(-5, -20); // -20 по top
        
        // Настраиваем контейнер кнопок - в нижней части
        if (buttonContainer != null)
        {
            RectTransform containerRect = buttonContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0, 0);
                containerRect.anchorMax = new Vector2(1, 0.5f);  // Ниже
                containerRect.offsetMin = new Vector2(10, 10);
                containerRect.offsetMax = new Vector2(-10, -5);
                Debug.Log("[NpcContextMenu] Button container configured");
            }
        }
        else
        {
            Debug.LogWarning("[NpcContextMenu] Button container is null in CreateTextElements");
        }
    }
    
    private void CreateButtonPrefab()
    {
        Debug.Log("[NpcContextMenu] Creating button prefab");
        
        GameObject buttonObj = new GameObject("ContextButton");
        
        // Добавляем RectTransform
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(85, 30);
        
        contextButtonPrefab = buttonObj.AddComponent<Button>();
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Добавляем Outline
        Outline buttonOutline = buttonObj.AddComponent<Outline>();
        buttonOutline.effectColor = Color.gray;
        buttonOutline.effectDistance = new Vector2(1, 1);
        
        // Создаем текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Button";
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Настраиваем цвета кнопки
        ColorBlock colors = contextButtonPrefab.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        contextButtonPrefab.colors = colors;
        
        Debug.Log("[NpcContextMenu] Button prefab created successfully");
    }
    
    public void ShowContextMenu(NpcBehaviour npc, PlayerCore player)
    {
        Debug.Log($"[NpcContextMenu] ShowContextMenu called with NPC: {npc?.NpcName}, Player: {player?.playerName}");
        
        if (npc == null || player == null)
        {
            Debug.LogWarning("[NpcContextMenu] Cannot show context menu: NPC or Player is null");
            return;
        }
        
        currentNpc = npc;
        currentPlayer = player;
        
        Debug.Log($"[NpcContextMenu] Current NPC set to: {currentNpc.NpcName}");
        Debug.Log($"[NpcContextMenu] Button texts: {string.Join(", ", currentNpc.ButtonTexts)}");
        
        // Проверяем и создаем UI элементы если их нет
        if (contextMenuPanel == null || buttonContainer == null || contextButtonPrefab == null)
        {
            Debug.Log("[NpcContextMenu] UI elements missing, creating them...");
            CreateDefaultUI();
        }
        
        // Очищаем старые кнопки перед созданием новых
        ClearButtons();
        
        // Обновляем тексты
        if (npcNameText != null)
        {
            npcNameText.text = npc.NpcName;
        }
        
        if (dialogueText != null)
        {
            dialogueText.text = npc.DialogueText;
        }
        
        // Создаем кнопки
        CreateContextButtons();
        
        // Позиционируем меню
        PositionMenu();
        
        // Показываем меню
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(true);
        }
        
        Debug.Log($"[NpcContextMenu] Showing context menu for {npc.NpcName}");
    }
    
    public void HideContextMenu()
    {
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }
        
        // Не очищаем кнопки здесь, так как они уже очищены в ShowContextMenu
        currentNpc = null;
        currentPlayer = null;
        
        Debug.Log("[NpcContextMenu] Context menu hidden");
    }
    
    private void CreateContextButtons()
    {
        Debug.Log($"[NpcContextMenu] Creating buttons - NPC: {currentNpc?.NpcName}, Container: {buttonContainer != null}, Prefab: {contextButtonPrefab != null}");
        
        if (currentNpc == null || buttonContainer == null || contextButtonPrefab == null)
        {
            Debug.LogWarning("[NpcContextMenu] Cannot create buttons: missing references");
            Debug.LogWarning($"[NpcContextMenu] NPC: {currentNpc != null}, Container: {buttonContainer != null}, Prefab: {contextButtonPrefab != null}");
            return;
        }
        
        string[] buttonTexts = currentNpc.ButtonTexts;
        Debug.Log($"[NpcContextMenu] Button texts count: {buttonTexts?.Length ?? 0}");
        
        if (buttonTexts == null || buttonTexts.Length == 0)
        {
            Debug.LogWarning("[NpcContextMenu] No button texts provided");
            return;
        }
        
        for (int i = 0; i < buttonTexts.Length; i++)
        {
            string buttonText = buttonTexts[i];
            Debug.Log($"[NpcContextMenu] Creating button {i}: {buttonText}");
            Button button = CreateButton(buttonText, i);
            if (button != null)
            {
                contextButtons.Add(button);
                Debug.Log($"[NpcContextMenu] Button {i} created successfully");
            }
            else
            {
                Debug.LogError($"[NpcContextMenu] Failed to create button {i}");
            }
        }
        
        Debug.Log($"[NpcContextMenu] Created {contextButtons.Count} buttons");
        
        // Принудительно обновляем Layout
        if (buttonContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }
    }
    
    private Button CreateButton(string text, int index)
    {
        Debug.Log($"[NpcContextMenu] Creating button: {text}");
        
        if (contextButtonPrefab == null)
        {
            Debug.LogError("[NpcContextMenu] contextButtonPrefab is null!");
            return null;
        }
        
        if (buttonContainer == null)
        {
            Debug.LogError("[NpcContextMenu] buttonContainer is null!");
            return null;
        }
        
        Button button = Instantiate(contextButtonPrefab, buttonContainer);
        button.name = $"ContextButton_{text}";
        
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = text;
            Debug.Log($"[NpcContextMenu] Button text set to: {text}");
        }
        else
        {
            Debug.LogWarning($"[NpcContextMenu] No TextMeshProUGUI found in button for: {text}");
        }
        
        // Настраиваем действие кнопки
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnButtonClicked(text, index));
        
        Debug.Log($"[NpcContextMenu] Button {text} created successfully");
        return button;
    }
    
    private void OnButtonClicked(string buttonText, int index)
    {
        Debug.Log($"[NpcContextMenu] Button clicked: {buttonText} for {currentNpc?.NpcName}");
        
        if (currentNpc != null)
        {
            // Вызываем соответствующий метод NPC
            switch (buttonText.ToLower())
            {
                case "bank":
                    currentNpc.OnBankButton();
                    break;
                case "trade":
                    currentNpc.OnTradeButton();
                    break;
                default:
                    Debug.LogWarning($"[NpcContextMenu] Unknown button action: {buttonText}");
                    break;
            }
        }
        
        HideContextMenu();
    }
    
    private void ClearButtons()
    {
        // Удаляем кнопки из списка
        foreach (var button in contextButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        contextButtons.Clear();
        
        // Также удаляем все дочерние объекты из buttonContainer
        if (buttonContainer != null)
        {
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = buttonContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        Debug.Log("[NpcContextMenu] Buttons cleared");
    }
    
    private void PositionMenu()
    {
        if (contextMenuPanel == null || canvas == null) return;
        
        // Позиционируем меню рядом с курсором
        Vector2 mousePosition = Input.mousePosition;
        Vector2 menuSize = contextMenuPanel.GetComponent<RectTransform>().sizeDelta;
        
        // Учитываем размеры экрана
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // Корректируем позицию если меню выходит за границы экрана
        if (mousePosition.x + menuSize.x + menuOffset > screenWidth)
        {
            mousePosition.x = screenWidth - menuSize.x - menuOffset;
        }
        
        if (mousePosition.y - menuSize.y - menuOffset < 0)
        {
            mousePosition.y = menuSize.y + menuOffset;
        }
        
        contextMenuPanel.transform.position = mousePosition;
    }
    
    private void Update()
    {
        // Закрываем меню при клике вне его
        if (contextMenuPanel != null && contextMenuPanel.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // Проверяем, не кликнули ли мы по самому меню
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    contextMenuPanel.GetComponent<RectTransform>(), 
                    Input.mousePosition, 
                    canvas.worldCamera))
                {
                    HideContextMenu();
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
