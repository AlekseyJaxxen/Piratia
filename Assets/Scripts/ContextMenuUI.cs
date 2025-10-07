using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ContextMenuUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    
    [Header("Styling")]
    [SerializeField] private Color buttonNormalColor = Color.white;
    [SerializeField] private Color buttonHoverColor = Color.gray;
    [SerializeField] private Color buttonPressedColor = Color.blue;
    
    private List<Button> contextButtons = new List<Button>();
    private PlayerCore targetPlayer;
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private float lastShowTime = 0f;
    
    public static ContextMenuUI Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Проверяем Canvas
        if (parentCanvas != null)
        {
            Debug.Log($"[ContextMenuUI] Found Canvas: {parentCanvas.name}, Render Mode: {parentCanvas.renderMode}, Sort Order: {parentCanvas.sortingOrder}");
            Debug.Log($"[ContextMenuUI] Canvas size: {parentCanvas.pixelRect.size}, Scale Factor: {parentCanvas.scaleFactor}");
        }
        else
        {
            Debug.LogError("[ContextMenuUI] No Canvas found in parent hierarchy!");
        }
        
        // Создаем панель контекстного меню если её нет
        if (contextMenuPanel == null)
        {
            CreateContextMenuPanel();
        }
        
        // Скрываем меню по умолчанию
        HideContextMenu();
    }
    
    private void CreateContextMenuPanel()
    {
        // Создаем основную панель
        contextMenuPanel = new GameObject("ContextMenuPanel");
        contextMenuPanel.transform.SetParent(transform, false);
        
        // Добавляем Image компонент для фона
        Image panelImage = contextMenuPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        panelImage.raycastTarget = true; // Блокируем raycast
        
        // Настраиваем RectTransform как в tooltip системе
        RectTransform panelRect = contextMenuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1); // Левый верхний угол
        panelRect.anchorMax = new Vector2(0, 1); // Левый верхний угол
        panelRect.pivot = new Vector2(0, 1); // Левый верхний угол
        panelRect.sizeDelta = new Vector2(56f, 75f); // Размер панели для кнопок 50x20 с отступами 3px
        panelRect.anchoredPosition = Vector2.zero;
        
        // Убеждаемся, что панель видна
        contextMenuPanel.SetActive(true);
        
        Debug.Log("[ContextMenuUI] Context menu panel created with size: " + panelRect.sizeDelta);
        
        // Создаем контейнер для кнопок
        GameObject container = new GameObject("ButtonContainer");
        container.transform.SetParent(contextMenuPanel.transform, false);
        container.SetActive(true);
        
        // Настраиваем Vertical Layout Group
        VerticalLayoutGroup layoutGroup = container.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 3f; // Отступ между кнопками 3 пикселя
        layoutGroup.padding = new RectOffset(3, 3, 3, 3); // Отступы от краев 3 пикселя
        layoutGroup.childControlWidth = false; // НЕ контролируем ширину (фиксированная)
        layoutGroup.childControlHeight = false; // НЕ контролируем высоту (фиксированная)
        layoutGroup.childForceExpandWidth = false; // НЕ растягиваем по ширине
        layoutGroup.childForceExpandHeight = false; // НЕ растягиваем по высоте
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // Выравнивание по левому верхнему углу
        
        // Настраиваем Content Size Fitter
        ContentSizeFitter sizeFitter = container.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Настраиваем RectTransform контейнера
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        buttonContainer = container.transform;
        
        Debug.Log("[ContextMenuUI] ButtonContainer created with Layout Group and Content Size Fitter");
        
        // Создаем префаб кнопки
        CreateButtonPrefab();
    }
    
    private void CreateButtonPrefab()
    {
        buttonPrefab = new GameObject("ContextButton");
        
        // Добавляем Image компонент
        Image buttonImage = buttonPrefab.AddComponent<Image>();
        buttonImage.color = buttonNormalColor;
        buttonImage.enabled = true;
        buttonImage.raycastTarget = true; // Блокируем raycast для кнопок
        
        Debug.Log("[ContextMenuUI] Button prefab image created with color: " + buttonImage.color);
        
        // Добавляем Button компонент
        Button button = buttonPrefab.AddComponent<Button>();
        
        // Настраиваем цвета кнопки
        ColorBlock colors = button.colors;
        colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHoverColor;
        button.colors = colors;
        
        // Настраиваем RectTransform
        RectTransform buttonRect = buttonPrefab.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1); // Левый верхний угол
        buttonRect.anchorMax = new Vector2(0, 1); // Фиксированная позиция
        buttonRect.pivot = new Vector2(0, 1); // Левый верхний угол
        buttonRect.sizeDelta = new Vector2(50f, 20f); // Размер кнопки 50x20
        buttonRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("[ContextMenuUI] Button prefab rect configured: " + buttonRect.sizeDelta);
        
        // Создаем текст
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonPrefab.transform);
        
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "Button";
        text.fontSize = 10f; // Очень маленький размер шрифта
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        // Настраиваем RectTransform текста
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Делаем префаб неактивным
        buttonPrefab.SetActive(false);
    }
    
    public void ShowContextMenu(Vector2 screenPosition, PlayerCore target)
    {
        targetPlayer = target;
        
        // Проверяем, что панель существует
        if (contextMenuPanel == null)
        {
            Debug.LogError("[ContextMenuUI] contextMenuPanel is null! Creating new panel...");
            CreateContextMenuPanel();
        }
        
        // Очищаем старые кнопки
        ClearButtons();
        
        // Создаем кнопки
        CreateContextButtons();
        
        // Показываем панель и все её дочерние объекты
        contextMenuPanel.SetActive(true);
        
        // Убеждаемся, что контейнер кнопок тоже активен
        if (buttonContainer != null)
        {
            buttonContainer.gameObject.SetActive(true);
        }
        
        // Позиционируем меню
        PositionMenu(screenPosition);
        
        // Запоминаем время показа
        lastShowTime = Time.time;
        
        Debug.Log($"[ContextMenuUI] Showing context menu for player: {target?.playerName ?? "Unknown"}");
        Debug.Log($"[ContextMenuUI] Panel active: {contextMenuPanel.activeSelf}, Position: {screenPosition}");
        Debug.Log($"[ContextMenuUI] ButtonContainer active: {buttonContainer?.gameObject.activeSelf ?? false}");
        
        // Проверяем видимость панели
        if (contextMenuPanel != null)
        {
            RectTransform panelRect = contextMenuPanel.GetComponent<RectTransform>();
        Debug.Log($"[ContextMenuUI] Panel position: {panelRect.anchoredPosition}, Size: {panelRect.sizeDelta}");
        Debug.Log($"[ContextMenuUI] Panel world position: {panelRect.position}");
        Debug.Log($"[ContextMenuUI] Panel active in hierarchy: {contextMenuPanel.activeInHierarchy}");
        
        // Проверяем Canvas
        if (parentCanvas != null)
        {
            Debug.Log($"[ContextMenuUI] Canvas render mode: {parentCanvas.renderMode}, world camera: {parentCanvas.worldCamera}");
            Debug.Log($"[ContextMenuUI] Canvas pixel rect: {parentCanvas.pixelRect}");
        }
        
        // Проверяем Image компонент
        Image panelImage = contextMenuPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            Debug.Log($"[ContextMenuUI] Panel image color: {panelImage.color}, enabled: {panelImage.enabled}");
        }
        
        // Проверяем дочерние объекты
        Debug.Log($"[ContextMenuUI] Panel has {contextMenuPanel.transform.childCount} children");
        for (int i = 0; i < contextMenuPanel.transform.childCount; i++)
        {
            Transform child = contextMenuPanel.transform.GetChild(i);
            Debug.Log($"[ContextMenuUI] Child {i}: {child.name}, active: {child.gameObject.activeSelf}");
            
            // Если это ButtonContainer, проверяем его детей
            if (child.name == "ButtonContainer")
            {
                Debug.Log($"[ContextMenuUI] ButtonContainer has {child.childCount} children");
                for (int j = 0; j < child.childCount; j++)
                {
                    Transform buttonChild = child.GetChild(j);
                    Debug.Log($"[ContextMenuUI] Button {j}: {buttonChild.name}, active: {buttonChild.gameObject.activeSelf}");
                }
            }
        }
        }
    }
    
    public void HideContextMenu()
    {
        Debug.Log("[ContextMenuUI] Hiding context menu");
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }
        targetPlayer = null;
        ClearButtons();
    }
    
    private void CreateContextButtons()
    {
        Debug.Log("[ContextMenuUI] Creating context buttons...");
        
        // Создаем 3 кнопки для взаимодействия с игроком
        CreateButton("Trade", OnTrade);
        CreateButton("Invite Party", OnInviteParty);
        CreateButton("Add Friend", OnAddFriend);
        
        Debug.Log($"[ContextMenuUI] Created {contextButtons.Count} buttons");
        
        // Принудительно обновляем Layout Group после создания всех кнопок
        if (buttonContainer != null)
        {
            // Обновляем Layout несколько раз для надежности
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer.GetComponent<RectTransform>());
            Debug.Log("[ContextMenuUI] Layout rebuilt and canvas updated");
        }
    }
    
    private void CreateButton(string text, UnityEngine.Events.UnityAction action)
    {
        Debug.Log($"[ContextMenuUI] Creating button: {text}");
        
        if (buttonPrefab == null)
        {
            Debug.LogError("[ContextMenuUI] buttonPrefab is null!");
            return;
        }
        
        if (buttonContainer == null)
        {
            Debug.LogError("[ContextMenuUI] buttonContainer is null!");
            return;
        }
        
        Debug.Log($"[ContextMenuUI] ButtonPrefab active: {buttonPrefab.activeSelf}, ButtonContainer active: {buttonContainer.gameObject.activeSelf}");
        
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        buttonObj.SetActive(true);
        
        // Убеждаемся, что кнопка добавлена в контейнер
        buttonObj.transform.SetParent(buttonContainer, false);
        
        Debug.Log($"[ContextMenuUI] Instantiated button object: {buttonObj.name}, active: {buttonObj.activeSelf}");
        
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(action);
            Debug.Log($"[ContextMenuUI] Button component found and listener added");
        }
        else
        {
            Debug.LogError("[ContextMenuUI] Button component not found on button prefab!");
        }
        
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = text;
            Debug.Log($"[ContextMenuUI] Text set to: {text}");
        }
        else
        {
            Debug.LogError("[ContextMenuUI] TextMeshProUGUI not found in button!");
        }
        
        // Проверяем RectTransform кнопки
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            Debug.Log($"[ContextMenuUI] Button rect size: {buttonRect.sizeDelta}, position: {buttonRect.anchoredPosition}");
        }
        
        // Проверяем Image кнопки
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            Debug.Log($"[ContextMenuUI] Button image color: {buttonImage.color}, enabled: {buttonImage.enabled}");
        }
        else
        {
            Debug.LogError("[ContextMenuUI] Image component not found on button!");
        }
        
        contextButtons.Add(button);
        Debug.Log($"[ContextMenuUI] Created button: {text}");
        
        // Проверяем количество детей в контейнере
        Debug.Log($"[ContextMenuUI] ButtonContainer now has {buttonContainer.childCount} children");
        
        // Принудительно обновляем Layout Group
        if (buttonContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer.GetComponent<RectTransform>());
        }
    }
    
    private void ClearButtons()
    {
        foreach (Button button in contextButtons)
        {
            if (button != null && button.gameObject != null)
            {
                Destroy(button.gameObject);
            }
        }
        contextButtons.Clear();
    }
    
    private void PositionMenu(Vector2 screenPosition)
    {
        if (contextMenuPanel == null || parentCanvas == null) 
        {
            Debug.LogError("[ContextMenuUI] Panel or Canvas is null in PositionMenu!");
            return;
        }
        
        // Используем тот же подход, что и в tooltip системе
        // Позиционируем левым верхним углом с отступом от курсора
        Vector3 tooltipPosition = screenPosition + new Vector2(25f, 25f);
        
        // Устанавливаем позицию напрямую, как в tooltip системе
        contextMenuPanel.transform.position = tooltipPosition;
        
        Debug.Log($"[ContextMenuUI] Positioned menu at screen: {screenPosition}, tooltip position: {tooltipPosition}");
        
        // Проверяем, не выходит ли меню за границы экрана
        RectTransform panelRect = contextMenuPanel.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        // Если меню выходит за правую границу, сдвигаем влево
        if (corners[2].x > canvasCorners[2].x)
        {
            Vector3 newPosition = contextMenuPanel.transform.position;
            newPosition.x -= (corners[2].x - canvasCorners[2].x);
            contextMenuPanel.transform.position = newPosition;
        }
        
        // Если меню выходит за верхнюю границу, сдвигаем вниз
        if (corners[2].y > canvasCorners[2].y)
        {
            Vector3 newPosition = contextMenuPanel.transform.position;
            newPosition.y -= (corners[2].y - canvasCorners[2].y);
            contextMenuPanel.transform.position = newPosition;
        }
    }
    
    // Действия для взаимодействия с игроком
    private void OnTrade()
    {
        Debug.Log($"[ContextMenuUI] Trade clicked for player: {targetPlayer?.playerName ?? "Unknown"}");
        // TODO: Реализовать систему торговли
        HideContextMenu();
    }
    
    private void OnInviteParty()
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("[ContextMenuUI] Cannot invite to party: target player is null");
            HideContextMenu();
            return;
        }
        
        // Проверяем, что цель не в группе
        if (!string.IsNullOrEmpty(targetPlayer.partyId))
        {
            Debug.Log($"[ContextMenuUI] Cannot invite {targetPlayer.playerName}: already in party {targetPlayer.partyId}");
            HideContextMenu();
            return;
        }
        
        // Проверяем, что мы не приглашаем сами себя
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null && targetPlayer.netId == localPlayer.netId)
        {
            Debug.Log("[ContextMenuUI] Cannot invite yourself to party");
            HideContextMenu();
            return;
        }
        
        // Отправляем приглашение
        if (localPlayer != null)
        {
            localPlayer.CmdInviteToParty(targetPlayer.netId);
            Debug.Log($"[ContextMenuUI] Sent party invite to: {targetPlayer.playerName}");
        }
        else
        {
            Debug.LogError("[ContextMenuUI] Local player is null, cannot send party invite");
        }
        
        HideContextMenu();
    }
    
    private void OnAddFriend()
    {
        Debug.Log($"[ContextMenuUI] Add Friend clicked for player: {targetPlayer?.playerName ?? "Unknown"}");
        // TODO: Реализовать систему друзей
        HideContextMenu();
    }
    
    public bool IsMenuVisible()
    {
        return contextMenuPanel != null && contextMenuPanel.activeSelf;
    }
    
    private void Update()
    {
        // Закрываем меню при клике вне его
        if (IsMenuVisible() && Input.GetMouseButtonDown(0))
        {
            // Проверяем, был ли клик по меню
            Vector2 mousePosition = Input.mousePosition;
            if (!IsMouseOverMenu(mousePosition))
            {
                Debug.Log("[ContextMenuUI] Hiding menu due to left click outside");
                HideContextMenu();
            }
        }
        
        // Закрываем меню при правом клике (но не сразу после показа)
        if (IsMenuVisible() && Input.GetMouseButtonDown(1))
        {
            // Добавляем небольшую задержку, чтобы не закрывать меню сразу после показа
            if (Time.time - lastShowTime > 0.1f)
            {
                Debug.Log("[ContextMenuUI] Hiding menu due to right click");
                HideContextMenu();
            }
        }
    }
    
    private bool IsMouseOverMenu(Vector2 mousePosition)
    {
        if (contextMenuPanel == null) return false;
        
        // Проверяем, попал ли клик по панели или кнопкам
        RectTransform panelRect = contextMenuPanel.GetComponent<RectTransform>();
        bool overPanel = RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePosition, parentCanvas.worldCamera);
        
        // Также проверяем кнопки
        bool overButton = false;
        foreach (Button button in contextButtons)
        {
            if (button != null && button.gameObject != null)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePosition, parentCanvas.worldCamera))
                {
                    overButton = true;
                    break;
                }
            }
        }
        
        return overPanel || overButton;
    }
}
