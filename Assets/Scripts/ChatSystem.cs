using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ChatSystem : NetworkBehaviour
{
    [Header("Chat UI")]
    public GameObject chatPanel;
    public ScrollRect chatScrollRect;
    public Transform chatContent;
    public TMP_InputField chatInputField;
    public Button sendButton;
    public Button toggleButton;
    
    [Header("Chat Settings")]
    public int maxChatMessages = 20;
    public float localMessageDuration = 3f;
    public float localMessageFadeTime = 1f;
    public KeyCode toggleKey = KeyCode.T;
    public bool startMinimized = true;
    public int maxMessageLength = 100;
    
    [Header("Local Message Position")]
    public Vector3 localMessageOffset = new Vector3(0, 2.5f, 0);
    public float messageHeight = 2.5f; // Высота сообщения над игроком
    public bool showLocalMessagesForAllPlayers = true;
    
    [Header("Local Message Prefab")]
    public GameObject localMessagePrefab;
    
    private Queue<GameObject> chatMessages = new Queue<GameObject>();
    private Dictionary<uint, GameObject> activeLocalMessages = new Dictionary<uint, GameObject>();
    private bool isChatVisible = false;
    private bool isTyping = false;
    
    public static ChatSystem Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Настройка UI
        SetupUI();
        
        // Показываем чат только для локального игрока
        if (isLocalPlayer)
        {
            if (startMinimized)
            {
                HideChat();
            }
            else
            {
                ShowChat();
            }
        }
        else
        {
            HideChat();
        }
    }
    
    void SetupUI()
    {
        // Настраиваем кнопку отправки
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendChatMessage);
        }
        
        // Настраиваем кнопку переключения
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleChat);
        }
        
        // Настраиваем поле ввода
        if (chatInputField != null)
        {
            chatInputField.onEndEdit.AddListener(OnChatInputEndEdit);
            chatInputField.characterLimit = maxMessageLength;
        }
    }
    
    void Update()
    {
        // Обработка горячих клавиш только для локального игрока
        if (isLocalPlayer)
        {
            // Enter - вход в режим набора (только если чат виден)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (isChatVisible && !isTyping)
                {
                    StartTyping();
                }
                else if (isTyping)
                {
                    SendChatMessage();
                }
            }
            
            // Escape - выход из режима набора без отправки
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isTyping)
                {
                    StopTyping();
                }
            }
            
            // T - переключение видимости чата (только если не набираем)
            if (Input.GetKeyDown(toggleKey) && !isTyping)
            {
                ToggleChat();
            }
        }
    }
    
    public void ToggleChat()
    {
        if (isChatVisible)
        {
            HideChat();
        }
        else
        {
            ShowChat();
        }
    }
    
    public void ShowChat()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
            isChatVisible = true;
        }
    }
    
    public void HideChat()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
            isChatVisible = false;
            
            // Выходим из режима набора
            StopTyping();
        }
    }
    
    public void StartTyping()
    {
        if (chatInputField != null)
        {
            isTyping = true;
            chatInputField.ActivateInputField();
            chatInputField.text = "";
        }
    }
    
    public void StopTyping()
    {
        if (chatInputField != null)
        {
            isTyping = false;
            chatInputField.DeactivateInputField();
            chatInputField.text = "";
        }
    }
    
    public bool IsTyping()
    {
        return isTyping;
    }
    
    void OnChatInputEndEdit(string message)
    {
        // Этот метод вызывается при потере фокуса, но мы обрабатываем Enter в Update
        // Здесь ничего не делаем, чтобы не отправлять сообщение при клике вне поля
    }
    
    public void SendChatMessage()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text))
        {
            StopTyping();
            return;
        }
        
        string message = chatInputField.text.Trim();
        
        // Проверяем длину сообщения
        if (message.Length > maxMessageLength)
        {
            message = message.Substring(0, maxMessageLength);
        }
        
        if (isLocalPlayer)
        {
            // Отправляем сообщение на сервер
            CmdSendChatMessage(message);
        }
        
        // Выходим из режима набора
        StopTyping();
    }
    
    [Command]
    void CmdSendChatMessage(string message)
    {
        // Получаем информацию об отправителе
        PlayerCore senderCore = GetComponent<PlayerCore>();
        string senderName = senderCore != null ? senderCore.playerName : "Unknown";
        
        // Отправляем сообщение всем клиентам
        RpcReceiveChatMessage(senderName, message, netId);
    }
    
    [ClientRpc]
    void RpcReceiveChatMessage(string senderName, string message, uint senderNetId)
    {
        // Добавляем сообщение в чат
        AddChatMessage(senderName, message);
        
        // Показываем локальное сообщение над персонажем
        if (showLocalMessagesForAllPlayers)
        {
            ShowLocalMessageForPlayer(message, senderNetId);
        }
        else if (senderNetId == netId)
        {
            // Показываем только свои сообщения
            ShowLocalMessage(message);
        }
    }
    
    void AddChatMessage(string senderName, string message)
    {
        if (chatContent == null) return;
        
        // Создаем новый элемент чата
        GameObject chatMessageObj = new GameObject("ChatMessage");
        chatMessageObj.transform.SetParent(chatContent, false);
        
        TextMeshProUGUI textComponent = chatMessageObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = $"<color=#00FF00>{senderName}:</color> {message}";
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        
        // Добавляем в очередь
        chatMessages.Enqueue(chatMessageObj);
        
        // Удаляем старые сообщения если превышен лимит
        while (chatMessages.Count > maxChatMessages)
        {
            GameObject oldMessage = chatMessages.Dequeue();
            if (oldMessage != null)
            {
                Destroy(oldMessage);
            }
        }
        
        // Прокручиваем вниз
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    void ShowLocalMessage(string message)
    {
        ShowLocalMessageForPlayer(message, netId);
    }
    
    void ShowLocalMessageForPlayer(string message, uint playerNetId)
    {
        if (localMessagePrefab == null) return;
        
        // Находим игрока по NetId
        GameObject playerObject = null;
        if (NetworkClient.spawned.ContainsKey(playerNetId))
        {
            playerObject = NetworkClient.spawned[playerNetId].gameObject;
        }
        
        if (playerObject == null) return;
        
        // Удаляем предыдущее локальное сообщение если есть
        if (activeLocalMessages.ContainsKey(playerNetId))
        {
            GameObject oldMessage = activeLocalMessages[playerNetId];
            if (oldMessage != null)
            {
                Destroy(oldMessage);
            }
            activeLocalMessages.Remove(playerNetId);
        }
        
        // Создаем новое локальное сообщение как независимый объект
        GameObject localMessage = Instantiate(localMessagePrefab);
        
        // Добавляем компонент для следования в экранных координатах
        ScreenSpaceFollow screenFollow = localMessage.GetComponent<ScreenSpaceFollow>();
        if (screenFollow == null)
        {
            screenFollow = localMessage.AddComponent<ScreenSpaceFollow>();
        }
        // Используем настройку высоты
        Vector3 offset = new Vector3(localMessageOffset.x, messageHeight, localMessageOffset.z);
        screenFollow.SetTarget(playerObject.transform, offset);
        
        // Настраиваем текст
        LocalMessageUI localMessageUI = localMessage.GetComponent<LocalMessageUI>();
        if (localMessageUI != null)
        {
            localMessageUI.SetMessage(message);
        }
        else
        {
            // Fallback для старого способа
            TextMeshProUGUI textComponent = localMessage.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
        }
        
        // Добавляем в активные сообщения
        activeLocalMessages[playerNetId] = localMessage;
        
        // Запускаем анимацию исчезновения
        StartCoroutine(FadeOutLocalMessage(localMessage, localMessageDuration, localMessageFadeTime));
    }
    
    System.Collections.IEnumerator FadeOutLocalMessage(GameObject messageObj, float duration, float fadeTime)
    {
        yield return new WaitForSeconds(duration);
        
        if (messageObj == null) yield break;
        
        // Используем LocalMessageUI для анимации исчезновения
        LocalMessageUI localMessageUI = messageObj.GetComponent<LocalMessageUI>();
        if (localMessageUI != null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                if (messageObj == null) yield break;
                
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                localMessageUI.SetAlpha(alpha);
                
                yield return null;
            }
        }
        else
        {
            // Fallback для старого способа
            TextMeshProUGUI textComponent = messageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                Color originalColor = textComponent.color;
                float elapsedTime = 0f;
                
                while (elapsedTime < fadeTime)
                {
                    if (messageObj == null) yield break;
                    
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                    textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    
                    yield return null;
                }
            }
        }
        
        if (messageObj != null)
        {
            Destroy(messageObj);
        }
        
        // Удаляем из активных сообщений
        if (activeLocalMessages.ContainsValue(messageObj))
        {
            var keyToRemove = 0u;
            foreach (var kvp in activeLocalMessages)
            {
                if (kvp.Value == messageObj)
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }
            activeLocalMessages.Remove(keyToRemove);
        }
    }
    
    // Публичные методы для внешнего использования
    public void SendSystemMessage(string message)
    {
        if (isLocalPlayer)
        {
            CmdSendSystemMessage(message);
        }
    }
    
    [Command]
    void CmdSendSystemMessage(string message)
    {
        RpcReceiveSystemMessage(message);
    }
    
    [ClientRpc]
    void RpcReceiveSystemMessage(string message)
    {
        AddSystemMessage(message);
    }
    
    void AddSystemMessage(string message)
    {
        if (chatContent == null) return;
        
        GameObject chatMessageObj = new GameObject("SystemMessage");
        chatMessageObj.transform.SetParent(chatContent, false);
        
        TextMeshProUGUI textComponent = chatMessageObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = $"<color=#FFA500>[SYSTEM]</color> {message}";
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        
        chatMessages.Enqueue(chatMessageObj);
        
        while (chatMessages.Count > maxChatMessages)
        {
            GameObject oldMessage = chatMessages.Dequeue();
            if (oldMessage != null)
            {
                Destroy(oldMessage);
            }
        }
        
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
