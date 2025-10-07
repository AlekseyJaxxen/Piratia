using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections;

public class PartyInviteUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject invitePanel;
    [SerializeField] private TextMeshProUGUI inviteText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Header("Styling")]
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    [SerializeField] private Color acceptButtonColor = Color.green;
    [SerializeField] private Color declineButtonColor = Color.red;
    
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private uint inviterNetId;
    private string inviterName;
    private float inviteTime;
    private float expireTime = 30f; // 30 секунд на ответ
    private Coroutine timerCoroutine;
    private bool isJoinRequest = false; // true для запроса на присоединение, false для приглашения
    
    public static PartyInviteUI Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Создаем pop-up панель если её нет
        if (invitePanel == null)
        {
            CreateInvitePanel();
        }
        
        // Скрываем панель по умолчанию
        HideInvite();
    }
    
    private void CreateInvitePanel()
    {
        // Создаем основную панель
        invitePanel = new GameObject("PartyInvitePanel");
        invitePanel.transform.SetParent(transform, false);
        
        // Добавляем Image компонент для фона
        Image panelImage = invitePanel.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = true;
        
        // Настраиваем RectTransform
        RectTransform panelRect = invitePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); // Центр экрана
        panelRect.anchorMax = new Vector2(0.5f, 0.5f); // Центр экрана
        panelRect.pivot = new Vector2(0.5f, 0.5f); // Центр
        panelRect.sizeDelta = new Vector2(300f, 150f);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Создаем текст приглашения
        CreateInviteText(panelRect);
        
        // Создаем кнопки
        CreateButtons(panelRect);
        
        // Создаем таймер
        CreateTimer(panelRect);
        
        Debug.Log("[PartyInviteUI] Party invite panel created");
        
        // Проверяем созданную структуру
        Debug.Log($"[PartyInviteUI] Panel created with {invitePanel.transform.childCount} children");
        for (int i = 0; i < invitePanel.transform.childCount; i++)
        {
            Transform child = invitePanel.transform.GetChild(i);
            Debug.Log($"[PartyInviteUI] Created child {i}: {child.name}");
        }
    }
    
    private void CreateInviteText(RectTransform parent)
    {
        GameObject textObj = new GameObject("InviteText");
        textObj.transform.SetParent(parent, false);
        
        inviteText = textObj.AddComponent<TextMeshProUGUI>();
        inviteText.text = "Player invites you to join their party";
        inviteText.fontSize = 16f;
        inviteText.color = Color.white;
        inviteText.alignment = TextAlignmentOptions.Center;
        
        // Настраиваем RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.6f);
        textRect.anchorMax = new Vector2(1, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    private void CreateButtons(RectTransform parent)
    {
        // Создаем контейнер для кнопок
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(parent, false);
        
        // Настраиваем Grid Layout Group
        GridLayoutGroup layoutGroup = buttonContainer.AddComponent<GridLayoutGroup>();
        layoutGroup.cellSize = new Vector2(100f, 40f);
        layoutGroup.spacing = new Vector2(20f, 0f);
        layoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layoutGroup.constraintCount = 2; // 2 кнопки в ряд
        
        // Настраиваем RectTransform контейнера
        RectTransform containerRect = buttonContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0.1f);
        containerRect.anchorMax = new Vector2(1, 0.5f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Создаем кнопку Accept
        acceptButton = CreateButton("Accept", acceptButtonColor, containerRect.transform);
        acceptButton.onClick.AddListener(OnAcceptClicked);
        
        // Создаем кнопку Decline
        declineButton = CreateButton("Decline", declineButtonColor, containerRect.transform);
        declineButton.onClick.AddListener(OnDeclineClicked);
        
        // Принудительно обновляем Layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        
        Debug.Log("[PartyInviteUI] Buttons created and layout updated");
    }
    
    private Button CreateButton(string text, Color color, Transform parent)
    {
        GameObject buttonObj = new GameObject(text + "Button");
        buttonObj.transform.SetParent(parent, false);
        buttonObj.SetActive(true); // Убеждаемся, что кнопка активна
        
        // Добавляем Image компонент
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        buttonImage.raycastTarget = true;
        buttonImage.enabled = true;
        
        // Добавляем Button компонент
        Button button = buttonObj.AddComponent<Button>();
        
        // Настраиваем цвета кнопки
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        
        // Настраиваем RectTransform для Grid Layout
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0); // Левый нижний угол
        buttonRect.anchorMax = new Vector2(0, 0); // Левый нижний угол
        buttonRect.pivot = new Vector2(0.5f, 0.5f); // Центр
        buttonRect.sizeDelta = new Vector2(100f, 40f);
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Создаем текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        textObj.SetActive(true); // Убеждаемся, что текст активен
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 14f;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        // Настраиваем RectTransform текста
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Debug.Log($"[PartyInviteUI] Created button: {text}, active: {buttonObj.activeSelf}");
        
        return button;
    }
    
    private void CreateTimer(RectTransform parent)
    {
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(parent, false);
        
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "30";
        timerText.fontSize = 14f;
        timerText.color = Color.yellow;
        timerText.alignment = TextAlignmentOptions.Center;
        
        // Настраиваем RectTransform
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0, 0.5f);
        timerRect.anchorMax = new Vector2(1, 0.6f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;
    }
    
    public void ShowInvite(string inviterName, uint inviterNetId)
    {
        this.inviterName = inviterName;
        this.inviterNetId = inviterNetId;
        this.inviteTime = Time.time;
        this.isJoinRequest = false; // Это приглашение
        
        // Обновляем текст приглашения
        if (inviteText != null)
        {
            inviteText.text = $"{inviterName} invites you to join their party";
        }
        
        // Показываем панель и все её дочерние объекты
        invitePanel.SetActive(true);
        
        // Убеждаемся, что все кнопки активны
        if (acceptButton != null) acceptButton.gameObject.SetActive(true);
        if (declineButton != null) declineButton.gameObject.SetActive(true);
        
        // Запускаем таймер
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(UpdateTimer());
        
        Debug.Log($"[PartyInviteUI] Showing party invite from: {inviterName}");
        Debug.Log($"[PartyInviteUI] Panel active: {invitePanel.activeSelf}");
        Debug.Log($"[PartyInviteUI] Accept button active: {acceptButton?.gameObject.activeSelf ?? false}");
        Debug.Log($"[PartyInviteUI] Decline button active: {declineButton?.gameObject.activeSelf ?? false}");
        
        // Проверяем компоненты панели
        if (invitePanel != null)
        {
            Debug.Log($"[PartyInviteUI] Panel has {invitePanel.transform.childCount} children");
            for (int i = 0; i < invitePanel.transform.childCount; i++)
            {
                Transform child = invitePanel.transform.GetChild(i);
                Debug.Log($"[PartyInviteUI] Child {i}: {child.name}, active: {child.gameObject.activeSelf}");
                
                // Если это ButtonContainer, проверяем его детей
                if (child.name == "ButtonContainer")
                {
                    Debug.Log($"[PartyInviteUI] ButtonContainer has {child.childCount} children");
                    for (int j = 0; j < child.childCount; j++)
                    {
                        Transform buttonChild = child.GetChild(j);
                        Debug.Log($"[PartyInviteUI] Button {j}: {buttonChild.name}, active: {buttonChild.gameObject.activeSelf}");
                        
                        // Проверяем компоненты кнопки
                        Button button = buttonChild.GetComponent<Button>();
                        Image image = buttonChild.GetComponent<Image>();
                        Debug.Log($"[PartyInviteUI] Button {j} - Button: {button != null}, Image: {image != null}, raycastTarget: {image?.raycastTarget ?? false}");
                    }
                }
            }
        }
    }
    
    public void ShowJoinRequest(string requesterName, uint requesterNetId)
    {
        this.inviterName = requesterName;
        this.inviterNetId = requesterNetId;
        this.inviteTime = Time.time;
        this.isJoinRequest = true; // Это запрос на присоединение
        
        // Обновляем текст запроса
        if (inviteText != null)
        {
            inviteText.text = $"{requesterName} wants to join your party";
        }
        
        // Показываем панель и все её дочерние объекты
        invitePanel.SetActive(true);
        
        // Убеждаемся, что все кнопки активны
        if (acceptButton != null) acceptButton.gameObject.SetActive(true);
        if (declineButton != null) declineButton.gameObject.SetActive(true);
        
        // Запускаем таймер
        timerCoroutine = StartCoroutine(UpdateTimer());
        
        Debug.Log($"[PartyInviteUI] Showing join request from: {requesterName}");
    }
    
    public void HideInvite()
    {
        if (invitePanel != null)
        {
            invitePanel.SetActive(false);
        }
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        Debug.Log("[PartyInviteUI] Hiding party invite");
    }
    
    private IEnumerator UpdateTimer()
    {
        while (Time.time - inviteTime < expireTime)
        {
            float remainingTime = expireTime - (Time.time - inviteTime);
            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(remainingTime).ToString();
            }
            yield return new WaitForSeconds(0.1f);
        }
        
        // Время истекло, автоматически отклоняем
        OnDeclineClicked();
    }
    
    private void OnAcceptClicked()
    {
        Debug.Log($"[PartyInviteUI] Accept clicked for {(isJoinRequest ? "join request" : "invite")} from: {inviterName}");
        
        // Отправляем команду на сервер
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            if (isJoinRequest)
            {
                // Принимаем запрос на присоединение к группе
                localPlayer.CmdAcceptJoinRequest(inviterNetId);
            }
            else
            {
                // Принимаем приглашение в группу
                localPlayer.CmdAcceptPartyInvite(inviterNetId);
            }
        }
        
        HideInvite();
    }
    
    private void OnDeclineClicked()
    {
        Debug.Log($"[PartyInviteUI] Decline clicked for {(isJoinRequest ? "join request" : "invite")} from: {inviterName}");
        
        // Отправляем команду на сервер
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            if (isJoinRequest)
            {
                // Отклоняем запрос на присоединение к группе
                localPlayer.CmdDeclineJoinRequest(inviterNetId);
            }
            else
            {
                // Отклоняем приглашение в группу
                localPlayer.CmdDeclinePartyInvite(inviterNetId);
            }
        }
        
        HideInvite();
    }
    
    public bool IsInviteVisible()
    {
        return invitePanel != null && invitePanel.activeSelf;
    }
}
