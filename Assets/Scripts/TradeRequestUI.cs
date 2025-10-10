using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TradeRequestUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject requestPanel;
    [SerializeField] private TextMeshProUGUI requestText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Header("Settings")]
    [SerializeField] private float expireTime = 30f; // 30 секунд на ответ
    
    private uint requesterNetId;
    private string requesterName;
    private float requestTime;
    private Coroutine timerCoroutine;
    private TradeSystem tradeSystem;
    
    public static TradeRequestUI Instance { get; private set; }
    
    private void Awake()
    {
        // Проверяем, что это локальный игрок
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore != null && playerCore.isLocalPlayer)
        {
            Instance = this;
            Debug.Log("[TradeRequestUI] Initialized for local player");
        }
        else
        {
            Debug.Log("[TradeRequestUI] Not local player - UI will be inactive");
        }
        
        // Скрываем панель по умолчанию
        HideTradeRequest();
    }
    
    public void Initialize(TradeSystem tradeSystem)
    {
        this.tradeSystem = tradeSystem;
        Debug.Log("[TradeRequestUI] Initialized with TradeSystem");
    }
    
    private void Start()
    {
        // Настраиваем кнопки
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }
        
        if (declineButton != null)
        {
            declineButton.onClick.AddListener(OnDeclineClicked);
        }
    }
    
    public void ShowTradeRequest(uint requesterNetId, string requesterName)
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeRequestUI] Not local player, ignoring trade request");
            return;
        }
        
        Debug.Log($"[TradeRequestUI] ShowTradeRequest called for {requesterName}");
        
        this.requesterNetId = requesterNetId;
        this.requesterName = requesterName;
        this.requestTime = Time.time;
        
        // Устанавливаем текст запроса
        if (requestText != null)
        {
            requestText.text = $"Trade request from {requesterName}\n\nDo you want to start trading?";
            Debug.Log($"[TradeRequestUI] Request text set: {requestText.text}");
        }
        else
        {
            Debug.LogError("[TradeRequestUI] requestText is null!");
        }
        
        // Показываем панель
        if (requestPanel != null)
        {
            requestPanel.SetActive(true);
            Debug.Log("[TradeRequestUI] Request panel activated");
        }
        else
        {
            Debug.LogError("[TradeRequestUI] requestPanel is null!");
        }
        
        // Запускаем таймер
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(UpdateTimer());
        
        Debug.Log($"[TradeRequestUI] Showing trade request from {requesterName}");
    }
    
    public void HideTradeRequest()
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeRequestUI] Not local player, ignoring hide trade request");
            return;
        }
        
        if (requestPanel != null)
        {
            requestPanel.SetActive(false);
        }
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        if (timerText != null)
        {
            timerText.text = "";
        }
        
        Debug.Log("[TradeRequestUI] Hiding trade request");
    }
    
    private void OnAcceptClicked()
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeRequestUI] Not local player, ignoring accept click");
            return;
        }
        
        Debug.Log($"[TradeRequestUI] Trade request accepted from {requesterName}");
        
        // Отправляем команду принятия торговли
        if (tradeSystem != null)
        {
            tradeSystem.CmdAcceptTradeRequest(requesterNetId);
        }
        
        HideTradeRequest();
    }
    
    private void OnDeclineClicked()
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeRequestUI] Not local player, ignoring decline click");
            return;
        }
        
        Debug.Log($"[TradeRequestUI] Trade request declined from {requesterName}");
        
        // Отправляем команду отклонения торговли
        if (tradeSystem != null)
        {
            tradeSystem.CmdDeclineTradeRequest(requesterNetId);
        }
        
        HideTradeRequest();
    }
    
    private IEnumerator UpdateTimer()
    {
        while (requestPanel != null && requestPanel.activeSelf)
        {
            float elapsedTime = Time.time - requestTime;
            float remainingTime = expireTime - elapsedTime;
            
            if (remainingTime <= 0)
            {
                // Время истекло, автоматически отклоняем запрос
                Debug.Log("[TradeRequestUI] Trade request expired");
                OnDeclineClicked();
                yield break;
            }
            
            // Обновляем текст таймера
            if (timerText != null)
            {
                timerText.text = $"Time: {Mathf.CeilToInt(remainingTime)}s";
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void Update()
    {
        // Закрываем по ESC
        if (requestPanel != null && requestPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            OnDeclineClicked();
        }
    }
    
    private void OnDestroy()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }
}
