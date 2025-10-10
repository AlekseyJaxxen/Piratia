using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.Events;

public class TradeSystem : NetworkBehaviour
{
    [Header("Trade Settings")]
    public int tradeSlotsCount = 20; // Количество слотов для каждого игрока (игрок + партнер)
    public float maxTradeDistance = 5f;
    public float tradeTimeoutSeconds = 300f; // 5 минут
    
    [Header("Anti-Spam Settings")]
    public float commandCooldownSeconds = 1f;
    
    // Защита от спама команд
    private float lastCommandTime = 0f;
    private float tradeStartTime = 0f;
    
    [Header("UI References")]
    [SerializeField] private TradeRequestUI tradeRequestUI;
    [SerializeField] private TradeUI tradeUI;
    
    [Header("Events")]
    public UnityEvent OnTradeStarted = new UnityEvent();
    public UnityEvent OnTradeEnded = new UnityEvent();
    
    // Синхронизированные данные торговли
    [SyncVar(hook = nameof(OnTradePartnerChanged))] public uint tradePartnerNetId;
    [SyncVar(hook = nameof(OnTradeStateChanged))] public TradeState tradeState;
    [SyncVar(hook = nameof(OnPlayerConfirmedChanged))] public bool playerConfirmed;
    [SyncVar(hook = nameof(OnPlayerTradeConfirmedChanged))] public bool playerTradeConfirmed;
    
    // Локальные данные
    public readonly SyncList<ItemInfo> tradeItems = new SyncList<ItemInfo>();
    public readonly SyncList<ItemInfo> partnerTradeItems = new SyncList<ItemInfo>();
    
    public PlayerCore playerCore;
    private Inventory inventory;
    private InventoryUI cachedInventoryUI;
    
    public enum TradeState
    {
        None,
        WaitingForAcceptance,
        Active,
        Confirmed,
        Completed,
        Cancelled
    }
    
    public static TradeSystem Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        playerCore = GetComponent<PlayerCore>();
        inventory = GetComponent<Inventory>();
        
        // Инициализируем списки предметов
        tradeItems.Callback += OnTradeItemsListChanged;
        partnerTradeItems.Callback += OnPartnerTradeItemsChanged;
        
        // Инициализируем пустые слоты
        InitializeTradeSlots();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isClient)
        {
            // Инициализируем UI компоненты
            InitializeUI();
        }
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        
        // Если игрок отключился во время торговли, отменяем торговлю
        if (tradeState == TradeState.Active)
        {
            Debug.Log($"[TradeSystem] Player {playerCore.playerName} disconnected during trade - cancelling trade");
            
            // Уведомляем партнера об отключении
            TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
            if (partnerTradeSystem != null)
            {
                partnerTradeSystem.RpcOnTradePartnerDisconnected(playerCore.playerName);
                // Завершаем торговлю у партнера (без RPC, так как RpcOnTradePartnerDisconnected уже закрывает UI)
                partnerTradeSystem.EndTradeWithoutRPC();
            }
        }
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        
        // Если игрок отключился во время торговли, отменяем торговлю
        if (tradeState == TradeState.Active)
        {
            Debug.Log($"[TradeSystem] Player {playerCore.playerName} disconnected during trade - cancelling trade");
            
            // Уведомляем партнера об отключении
            TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
            if (partnerTradeSystem != null)
            {
                partnerTradeSystem.RpcOnTradePartnerDisconnected(playerCore.playerName);
                // Завершаем торговлю у партнера (без RPC, так как RpcOnTradePartnerDisconnected уже закрывает UI)
                partnerTradeSystem.EndTradeWithoutRPC();
            }
        }
    }
    
    private void InitializeUI()
    {
        // Инициализируем TradeRequestUI
        if (tradeRequestUI != null)
        {
            tradeRequestUI.Initialize(this);
            Debug.Log($"[TradeSystem] TradeRequestUI initialized for {playerCore.playerName}");
        }
        else
        {
            Debug.LogError("[TradeSystem] TradeRequestUI reference is not assigned!");
        }
        
        // Инициализируем TradeUI
        if (tradeUI != null)
        {
            tradeUI.Initialize(this);
            Debug.Log($"[TradeSystem] TradeUI initialized for {playerCore.playerName}");
        }
        else
        {
            Debug.LogError("[TradeSystem] TradeUI reference is not assigned!");
        }
    }
    
    private void InitializeTradeSlots()
    {
        // Очищаем списки
        tradeItems.Clear();
        partnerTradeItems.Clear();
        
        // Заполняем пустыми слотами
        for (int i = 0; i < tradeSlotsCount; i++)
        {
            tradeItems.Add(new ItemInfo { id = 0, quantity = 0 });
            partnerTradeItems.Add(new ItemInfo { id = 0, quantity = 0 });
        }
    }
    
    // Сетевые команды для торговли
    [Command(requiresAuthority = false)]
    public void CmdRequestTrade(uint targetPlayerNetId)
    {
        if (tradeState != TradeState.None)
        {
            Debug.Log($"[TradeSystem] Player {playerCore.playerName} is already in trade");
            return;
        }
        
        PlayerCore targetPlayer = GetPlayerByNetId(targetPlayerNetId);
        if (targetPlayer == null)
        {
            Debug.Log($"[TradeSystem] Target player with netId {targetPlayerNetId} not found");
            return;
        }
        
        // БЕЗОПАСНОСТЬ: Проверяем расстояние между игроками
        float distance = Vector3.Distance(playerCore.transform.position, targetPlayer.transform.position);
        if (distance > maxTradeDistance)
        {
            Debug.LogWarning($"[TradeSystem] Player {playerCore.playerName} tried to trade with {targetPlayer.playerName} from too far away ({distance:F1}m)");
            return;
        }
        
        TradeSystem targetTradeSystem = targetPlayer.GetComponent<TradeSystem>();
        if (targetTradeSystem == null)
        {
            Debug.Log($"[TradeSystem] Target player {targetPlayer.playerName} has no TradeSystem component");
            return;
        }
        
        if (targetTradeSystem.tradeState != TradeState.None)
        {
            Debug.Log($"[TradeSystem] Target player {targetPlayer.playerName} is already in trade");
            return;
        }
        
        // Отправляем запрос на торговлю
        targetTradeSystem.RpcReceiveTradeRequest(playerCore.netId, playerCore.playerName);
        Debug.Log($"[TradeSystem] Trade request sent from {playerCore.playerName} to {targetPlayer.playerName}");
    }
    
    [ClientRpc]
    public void RpcReceiveTradeRequest(uint requesterNetId, string requesterName)
    {
        if (!playerCore.isLocalPlayer) return;
        
        Debug.Log($"[TradeSystem] Received trade request from {requesterName}");
        
                // Показываем popup с предложением торговли
                if (tradeRequestUI != null)
                {
                    tradeRequestUI.ShowTradeRequest(requesterNetId, requesterName);
                }
    }
    
    [Command(requiresAuthority = false)]
    public void CmdAcceptTradeRequest(uint requesterNetId)
    {
        if (tradeState != TradeState.None)
        {
            Debug.Log($"[TradeSystem] Player {playerCore.playerName} is already in trade");
            return;
        }
        
        PlayerCore requester = GetPlayerByNetId(requesterNetId);
        if (requester == null)
        {
            Debug.Log($"[TradeSystem] Requester with netId {requesterNetId} not found");
            return;
        }
        
        TradeSystem requesterTradeSystem = requester.GetComponent<TradeSystem>();
        if (requesterTradeSystem == null)
        {
            Debug.Log($"[TradeSystem] Requester {requester.playerName} has no TradeSystem component");
            return;
        }
        
        // Начинаем торговлю
        StartTrade(requesterTradeSystem);
        requesterTradeSystem.StartTrade(this);
        
        Debug.Log($"[TradeSystem] Trade started between {playerCore.playerName} and {requester.playerName}");
    }
    
    [Command(requiresAuthority = false)]
    public void CmdDeclineTradeRequest(uint requesterNetId)
    {
        PlayerCore requester = GetPlayerByNetId(requesterNetId);
        if (requester != null)
        {
            TradeSystem requesterTradeSystem = requester.GetComponent<TradeSystem>();
            if (requesterTradeSystem != null)
            {
                requesterTradeSystem.RpcTradeRequestDeclined(playerCore.playerName);
            }
        }
        
        Debug.Log($"[TradeSystem] Trade request from {requesterNetId} declined by {playerCore.playerName}");
    }
    
    [ClientRpc]
    public void RpcTradeRequestDeclined(string declinerName)
    {
        if (!playerCore.isLocalPlayer) return;
        
        Debug.Log($"[TradeSystem] Trade request declined by {declinerName}");
        
                // Скрываем popup
                if (tradeRequestUI != null)
                {
                    tradeRequestUI.HideTradeRequest();
                }
    }
    
    private void StartTrade(TradeSystem partnerTradeSystem)
    {
        tradePartnerNetId = partnerTradeSystem.playerCore.netId;
        tradeState = TradeState.Active;
        playerConfirmed = false;
        playerTradeConfirmed = false;
        
        // Записываем время начала торговли для таймаута
        tradeStartTime = Time.time;
        
        // Инициализируем слоты торговли
        InitializeTradeSlots();
        
        // Применяем стан обоим игрокам
        BlockPlayerMovement();
        partnerTradeSystem.BlockPlayerMovement();
        
        // Уведомляем клиентов - только локальный игрок покажет UI
        RpcOnTradeStarted();
        
        Debug.Log($"[TradeSystem] Trade started for {playerCore.playerName}");
    }
    
    [ClientRpc]
    public void RpcOnTradeStarted()
    {
        Debug.Log($"[TradeSystem] RpcOnTradeStarted called - tradeState: {tradeState}");
        
        // Устанавливаем состояние торговли на клиенте
        tradeState = TradeState.Active;
        
        Debug.Log($"[TradeSystem] Trade started for {playerCore.playerName}");
        
        // Открываем инвентарь только для локального игрока
        if (playerCore.isLocalPlayer)
        {
            OpenInventoryForTrade();
            // Обновляем визуальное состояние предметов в инвентаре
            UpdateInventoryVisuals();
        }
        
        OnTradeStarted.Invoke();
    }
    
    private void OpenInventoryForTrade()
    {
        // ПРОИЗВОДИТЕЛЬНОСТЬ: Используем кэшированную ссылку вместо FindFirstObjectByType
        if (cachedInventoryUI == null)
        {
            cachedInventoryUI = FindFirstObjectByType<InventoryUI>();
            Debug.Log($"[TradeSystem] Found InventoryUI: {cachedInventoryUI != null}");
        }
        
        if (cachedInventoryUI != null)
        {
            Debug.Log("[TradeSystem] Opening inventory for trade...");
            cachedInventoryUI.ShowInventory();
            Debug.Log("[TradeSystem] Opened inventory for trade");
        }
        else
        {
            Debug.LogError("[TradeSystem] InventoryUI not found - cannot open inventory for trade");
        }
    }
    
    private void BlockPlayerMovement()
    {
        // Применяем stun напрямую на сервере
        if (playerCore != null)
        {
            playerCore.ApplyControlEffect(ControlEffectType.Stun, 999f, 1); // Длительный stun с низким весом
            Debug.Log("[TradeSystem] Player movement blocked for trading (server)");
        }
    }
    
    [Command(requiresAuthority = false)]
    public void CmdConfirmTrade()
    {
        // БЕЗОПАСНОСТЬ: Защита от спама команд
        if (Time.time - lastCommandTime < commandCooldownSeconds)
        {
            Debug.LogWarning($"[TradeSystem] Player {playerCore.playerName} spamming confirm command");
            return;
        }
        lastCommandTime = Time.time;
        
        if (tradeState != TradeState.Active)
        {
            Debug.Log($"[TradeSystem] Cannot confirm trade: trade not active");
            return;
        }
        
        // БЕЗОПАСНОСТЬ: Проверяем таймаут торговли
        if (Time.time - tradeStartTime > tradeTimeoutSeconds)
        {
            Debug.LogWarning($"[TradeSystem] Trade timeout for {playerCore.playerName} - cancelling trade");
            CancelTrade();
            return;
        }
        
        // Устанавливаем подтверждение текущего игрока
        playerConfirmed = true;
        Debug.Log($"[TradeSystem] Player {playerCore.playerName} confirmed trade items. Player: {playerConfirmed}");
        
        // Получаем систему торговли партнера для проверки его статуса
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        
        // Каждый игрок подтверждает предметы независимо
        Debug.Log($"[TradeSystem] Player {playerCore.playerName} confirmed items - ready for trade");
        
        // Синхронизируем предметы с партнером только после подтверждения
        if (partnerTradeSystem != null)
        {
            // Передаем все предметы игрока партнеру
            for (int i = 0; i < tradeItems.Count; i++)
            {
                ItemInfo item = tradeItems[i];
                if (item.id != 0)
                {
                    // Обновляем partnerTradeItems на сервере
                    partnerTradeSystem.UpdatePartnerTradeItemOnServer(i, item);
                    // Уведомляем партнера об обновлении предмета
                    partnerTradeSystem.RpcUpdatePartnerTradeItem(i, item);
                }
            }
        }
        
        // Уведомляем текущего игрока о подтверждении предметов
        RpcOnItemsConfirmed();
        
        Debug.Log($"[TradeSystem] Trade confirmed by {playerCore.playerName}");
    }
    
    [Command(requiresAuthority = false)]
    public void CmdCompleteTrade()
    {
        if (tradeState != TradeState.Active)
        {
            Debug.Log($"[TradeSystem] Cannot complete trade: trade not active (current state: {tradeState})");
            return;
        }
        
        // Устанавливаем подтверждение торговли текущего игрока
        playerTradeConfirmed = true;
        Debug.Log($"[TradeSystem] Player {playerCore.playerName} confirmed trade completion");
        
        // Получаем систему торговли партнера для проверки его статуса
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        
        // Проверяем, подтвердили ли оба игрока торговлю
        bool bothTradeConfirmed = playerTradeConfirmed && (partnerTradeSystem != null && partnerTradeSystem.playerTradeConfirmed);
        
        if (bothTradeConfirmed)
        {
            Debug.Log($"[TradeSystem] Both players confirmed trade completion - executing trade");
            // Выполняем обмен предметов только один раз (на сервере первого игрока)
            CompleteTrade();
        }
        else
        {
            Debug.Log($"[TradeSystem] Waiting for partner trade confirmation");
        }
    }
    
    
    [Server]
    public void UpdatePartnerTradeItemOnServer(int slotIndex, ItemInfo itemInfo)
    {
        // Обновляем partnerTradeItems на сервере (владелец SyncList)
        if (slotIndex >= 0 && slotIndex < partnerTradeItems.Count)
        {
            partnerTradeItems[slotIndex] = itemInfo;
            Debug.Log($"[TradeSystem] Updated partner trade item on server at slot {slotIndex}: {itemInfo.id}");
        }
    }
    
    [ClientRpc]
    public void RpcUpdatePartnerTradeItem(int slotIndex, ItemInfo itemInfo)
    {
        // НЕ изменяем SyncList напрямую - это вызывает ошибку "Synclists can only be modified by the owner"
        // Обновляем UI только для локального игрока
        if (playerCore.isLocalPlayer && tradeUI != null)
        {
            tradeUI.UpdateTradeUI();
        }
        Debug.Log($"[TradeSystem] Received partner trade item update at slot {slotIndex}: {itemInfo.id}");
    }
    
    [ClientRpc]
    public void RpcOnItemsConfirmed()
    {
        Debug.Log($"[TradeSystem] Items confirmed by both players");
        
        // Обновляем UI только для локального игрока
        if (playerCore.isLocalPlayer && tradeUI != null)
        {
            tradeUI.UpdateTradeUI();
        }
    }
    
    private void CompleteTrade()
    {
        // Обмениваем предметы между игроками
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            // Передаем предметы партнера текущему игроку
            for (int i = 0; i < partnerTradeItems.Count; i++)
            {
                ItemInfo item = partnerTradeItems[i];
                if (item.id != 0)
                {
                    // Добавляем ItemInfo напрямую, сохраняя все динамические свойства
                    bool added = inventory.AddItemInfo(item);
                    if (added)
                    {
                        Debug.Log($"[TradeSystem] Added {item.quantity}x item {item.id} from partner to inventory");
                    }
                    else
                    {
                        Debug.LogError($"[TradeSystem] Failed to add item {item.id} to inventory - trade aborted");
                        CancelTrade();
                        return;
                    }
                }
            }
            
            // Удаляем предметы текущего игрока из его инвентаря и передаем партнеру
            for (int i = 0; i < tradeItems.Count; i++)
            {
                ItemInfo item = tradeItems[i];
                if (item.id != 0)
                {
                    // БЕЗОПАСНОСТЬ: Проверяем, что предмет все еще есть в инвентаре перед удалением
                    Debug.Log($"[TradeSystem] Checking if player {playerCore.playerName} has item {item.id} x{item.quantity}");
                    if (inventory.HasItem(item.id, item.quantity))
                    {
                        Debug.Log($"[TradeSystem] Player {playerCore.playerName} has item {item.id} x{item.quantity}, removing...");
                        // Удаляем предмет из инвентаря текущего игрока
                        bool removed = inventory.RemoveItem(item.id, item.quantity);
                        
                        if (removed)
                        {
                            // Добавляем ItemInfo в инвентарь партнера только если удаление прошло успешно
                            bool addedToPartner = partnerTradeSystem.inventory.AddItemInfo(item);
                            if (addedToPartner)
                            {
                                Debug.Log($"[TradeSystem] Moved {item.quantity}x item {item.id} from {playerCore.playerName} to partner");
                            }
                            else
                            {
                                Debug.LogError($"[TradeSystem] Failed to add item {item.id} to partner inventory - trade aborted");
                                CancelTrade();
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogError($"[TradeSystem] Failed to remove item {item.id} from {playerCore.playerName} inventory - trade aborted");
                            // Отменяем торговлю при ошибке
                            CancelTrade();
                            return;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[TradeSystem] Item {item.id} not found in {playerCore.playerName} inventory - trade aborted");
                        // Отменяем торговлю при ошибке
                        CancelTrade();
                        return;
                    }
                }
            }
        }
        
        tradeState = TradeState.Completed;
        
        // Уведомляем клиентов о завершении
        RpcOnTradeCompleted();
        
        // Завершаем торговлю у текущего игрока
        EndTrade();
        
        // Завершаем торговлю у партнера
        if (partnerTradeSystem != null)
        {
            partnerTradeSystem.EndTrade();
        }
        
        Debug.Log($"[TradeSystem] Trade completed for {playerCore.playerName}");
    }
    
    [ClientRpc]
    public void RpcOnTradeCompleted()
    {
        Debug.Log($"[TradeSystem] Trade completed successfully");
        
        // Скрываем UI торговли только для локального игрока
        if (playerCore.isLocalPlayer && tradeUI != null)
        {
            tradeUI.HideTradeWindow();
        }
        
        // Обновляем инвентарь для локального игрока
        if (playerCore.isLocalPlayer && cachedInventoryUI != null)
        {
            cachedInventoryUI.UpdateInventoryUI();
        }
        
        OnTradeEnded.Invoke();
    }
    
    [Command(requiresAuthority = false)]
    public void CmdCancelTrade()
    {
        if (tradeState == TradeState.None)
        {
            Debug.Log($"[TradeSystem] No active trade to cancel");
            return;
        }
        
        // Уведомляем партнера об отмене
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            partnerTradeSystem.RpcOnTradeCancelled();
            // Завершаем торговлю у партнера (без RPC, так как RpcOnTradeCancelled уже закрывает UI)
            partnerTradeSystem.EndTradeWithoutRPC();
        }
        
        // Завершаем торговлю у текущего игрока
        EndTrade();
        
        Debug.Log($"[TradeSystem] Trade cancelled by {playerCore.playerName}");
    }
    
    [ClientRpc]
    public void RpcOnTradeCancelled()
    {
        Debug.Log($"[TradeSystem] Trade was cancelled by partner");
        
        // Событие OnTradeEnded.Invoke() само скроет окно через TradeUI.OnTradeEnded()
        OnTradeEnded.Invoke();
    }
    
    [ClientRpc]
    public void RpcOnTradePartnerDisconnected(string disconnectedPlayerName)
    {
        Debug.Log($"[TradeSystem] Trade partner {disconnectedPlayerName} disconnected - cancelling trade");
        
        // Событие OnTradeEnded.Invoke() само скроет окно через TradeUI.OnTradeEnded()
        OnTradeEnded.Invoke();
        
        // Показываем уведомление локальному игроку
        if (playerCore.isLocalPlayer)
        {
            Debug.Log($"[TradeSystem] Trade cancelled: {disconnectedPlayerName} disconnected");
            // Здесь можно добавить показ уведомления в UI
        }
    }
    
    [ClientRpc]
    public void RpcOnTradeEnded()
    {
        Debug.Log($"[TradeSystem] Trade ended - invoking OnTradeEnded event");
        
        // Событие OnTradeEnded.Invoke() само скроет окно через TradeUI.OnTradeEnded()
        OnTradeEnded.Invoke();
    }
    
    public void EndTrade()
    {
        // Возвращаем предметы из слотов торговли в инвентарь
        ReturnItemsToInventory();
        
        // Разблокируем движение игрока
        UnblockPlayerMovement();
        
        // Разблокируем движение партнера
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            partnerTradeSystem.UnblockPlayerMovement();
        }
        
        tradePartnerNetId = 0;
        tradeState = TradeState.None;
        playerConfirmed = false;
        playerTradeConfirmed = false;
        
        // Сбрасываем время начала торговли
        tradeStartTime = 0f;
        
        // Очищаем слоты торговли
        InitializeTradeSlots();
        
        // Обновляем визуальное состояние предметов в инвентаре (убираем красные квадраты)
        UpdateInventoryVisuals();
        
        // Уведомляем клиентов о завершении торговли
        RpcOnTradeEnded();
        
        Debug.Log($"[TradeSystem] Trade ended for {playerCore.playerName}");
    }
    
    private void EndTradeWithoutRPC()
    {
        // Возвращаем предметы из слотов торговли в инвентарь
        ReturnItemsToInventory();
        
        // Разблокируем движение игрока
        UnblockPlayerMovement();
        
        // Разблокируем движение партнера
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            partnerTradeSystem.UnblockPlayerMovement();
        }
        
        tradePartnerNetId = 0;
        tradeState = TradeState.None;
        playerConfirmed = false;
        playerTradeConfirmed = false;
        
        // Сбрасываем время начала торговли
        tradeStartTime = 0f;
        
        // Очищаем слоты торговли
        InitializeTradeSlots();
        
        // Обновляем визуальное состояние предметов в инвентаре (убираем красные квадраты)
        UpdateInventoryVisuals();
        
        // НЕ отправляем RPC, так как UI уже закрыт через RpcOnTradeCancelled
        
        Debug.Log($"[TradeSystem] Trade ended without RPC for {playerCore.playerName}");
    }
    
    private void CancelTrade()
    {
        // Уведомляем партнера об отмене
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            partnerTradeSystem.RpcOnTradeCancelled();
            // Завершаем торговлю у партнера (без RPC, так как RpcOnTradeCancelled уже закрывает UI)
            partnerTradeSystem.EndTradeWithoutRPC();
        }
        
        // Завершаем торговлю у текущего игрока
        EndTrade();
        
        Debug.Log($"[TradeSystem] Trade cancelled due to timeout for {playerCore.playerName}");
    }
    
    private void UnblockPlayerMovement()
    {
        // Снимаем stun напрямую на сервере
        if (playerCore != null)
        {
            playerCore.ClearStunEffect();
            Debug.Log("[TradeSystem] Player movement unblocked after trading (server)");
        }
    }
    
    private void ReturnItemsToInventory()
    {
        // Предметы не нужно возвращать в инвентарь, так как они там остались
        // Просто очищаем слоты торговли
        int itemsCleared = 0;
        
        for (int i = 0; i < tradeItems.Count; i++)
        {
            ItemInfo item = tradeItems[i];
            if (item.id != 0)
            {
                itemsCleared++;
                Debug.Log($"[TradeSystem] Cleared trade slot {i} with item {item.id} (item remains in inventory)");
            }
        }
        
        Debug.Log($"[TradeSystem] Cleared {itemsCleared} trade slots (items remain in inventory)");
    }
    
    // Вспомогательные методы
    private PlayerCore GetPlayerByNetId(uint netId)
    {
        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            return identity.GetComponent<PlayerCore>();
        }
        return null;
    }
    
    private TradeSystem GetTradeSystemByNetId(uint netId)
    {
        PlayerCore player = GetPlayerByNetId(netId);
        return player?.GetComponent<TradeSystem>();
    }
    
    // Хуки для синхронизации
    private void OnTradePartnerChanged(uint oldValue, uint newValue)
    {
        Debug.Log($"[TradeSystem] Trade partner changed to {newValue}");
    }
    
    private void OnTradeStateChanged(TradeState oldValue, TradeState newValue)
    {
        Debug.Log($"[TradeSystem] Trade state changed from {oldValue} to {newValue}");
    }
    
    private void OnPlayerConfirmedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[TradeSystem] Player confirmed: {newValue}");
        
        // Обновляем UI только для локального игрока
        if (playerCore.isLocalPlayer && tradeUI != null)
        {
            tradeUI.UpdateTradeUI();
        }
    }
    
    private void OnPlayerTradeConfirmedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[TradeSystem] Player trade confirmed: {newValue}");
        
        // Обновляем UI только для локального игрока
        if (playerCore.isLocalPlayer && tradeUI != null)
        {
            tradeUI.UpdateTradeUI();
        }
    }
    
    
    private void OnTradeItemsListChanged(SyncList<ItemInfo>.Operation op, int index, ItemInfo oldItem, ItemInfo newItem)
    {
        Debug.Log($"[TradeSystem] Trade items changed: {op} at index {index}");
        
        // Обновляем UI если торговля активна или подтверждена
        if ((tradeState == TradeState.Active || tradeState == TradeState.Confirmed) && tradeUI != null)
        {
            tradeUI.UpdateTradeUI();
        }
        
        // Обновляем визуальное состояние предметов в инвентаре
        UpdateInventoryVisuals();
    }
    
    private void OnPartnerTradeItemsChanged(SyncList<ItemInfo>.Operation op, int index, ItemInfo oldItem, ItemInfo newItem)
    {
        Debug.Log($"[TradeSystem] Partner trade items changed: {op} at index {index}");
        
        // Обновляем UI если торговля активна или подтверждена
        if ((tradeState == TradeState.Active || tradeState == TradeState.Confirmed) && tradeUI != null)
        {
            tradeUI.UpdateTradeUI();
        }
    }
    
    // Команды для добавления/удаления предметов в торговле
    [Command(requiresAuthority = false)]
    public void CmdAddItemToTrade(int slotIndex, ItemInfo itemInfo)
    {
        // БЕЗОПАСНОСТЬ: Защита от спама команд
        if (Time.time - lastCommandTime < commandCooldownSeconds)
        {
            Debug.LogWarning($"[TradeSystem] Player {playerCore.playerName} spamming add item command");
            return;
        }
        lastCommandTime = Time.time;
        
        if (tradeState != TradeState.Active)
        {
            Debug.Log($"[TradeSystem] Cannot add item to trade: trade not active");
            return;
        }
        
        // БЕЗОПАСНОСТЬ: Проверяем таймаут торговли
        if (Time.time - tradeStartTime > tradeTimeoutSeconds)
        {
            Debug.LogWarning($"[TradeSystem] Trade timeout for {playerCore.playerName} - cancelling trade");
            CancelTrade();
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= tradeItems.Count)
        {
            Debug.LogError($"[TradeSystem] Invalid slot index: {slotIndex}");
            return;
        }
        
        if (itemInfo.id == 0)
        {
            Debug.LogWarning("[TradeSystem] Cannot add empty item to trade");
            return;
        }
        
        // БЕЗОПАСНОСТЬ: Проверяем, что игрок действительно владеет предметом
        if (!inventory.HasItem(itemInfo.id, itemInfo.quantity))
        {
            Debug.LogWarning($"[TradeSystem] Player {playerCore.playerName} tried to trade item {itemInfo.id} they don't own");
            return;
        }
        
        // БЕЗОПАСНОСТЬ: Проверяем, что предмет не находится уже в торговле
        if (tradeItems.Contains(itemInfo))
        {
            Debug.LogWarning($"[TradeSystem] Player {playerCore.playerName} tried to trade duplicate item {itemInfo.id}");
            return;
        }
        
        // Создаем копию предмета для торговли (не удаляем из инвентаря)
        ItemInfo tradeItemCopy = new ItemInfo
        {
            id = itemInfo.id,
            quantity = itemInfo.quantity,
            hasDynamicStats = itemInfo.hasDynamicStats,
            dynamicItemName = itemInfo.dynamicItemName,
            strengthBonus = itemInfo.strengthBonus,
            agilityBonus = itemInfo.agilityBonus,
            spiritBonus = itemInfo.spiritBonus,
            constitutionBonus = itemInfo.constitutionBonus,
            accuracyBonus = itemInfo.accuracyBonus,
            minAttackConstantBonus = itemInfo.minAttackConstantBonus,
            maxAttackConstantBonus = itemInfo.maxAttackConstantBonus,
            maxHpConstantBonus = itemInfo.maxHpConstantBonus,
            maxSpConstantBonus = itemInfo.maxSpConstantBonus,
            crtConstantBonus = itemInfo.crtConstantBonus,
            mspdConstantBonus = itemInfo.mspdConstantBonus,
            constantDefence = itemInfo.constantDefence,
            physicalResistBonus = itemInfo.physicalResistBonus,
            hpRecoveryBonus = itemInfo.hpRecoveryBonus,
            spRecoveryBonus = itemInfo.spRecoveryBonus,
            dodgeBonus = itemInfo.dodgeBonus,
            attackSpeedBonus = itemInfo.attackSpeedBonus,
            attackSpeedPercentBonus = itemInfo.attackSpeedPercentBonus,
            dynamicRarity = itemInfo.dynamicRarity
        };
        
        // Добавляем копию предмета в слот торговли
        tradeItems[slotIndex] = tradeItemCopy;
        
        // НЕ синхронизируем с партнером до подтверждения предметов
        // Предметы будут видны партнеру только после нажатия Confirm
        
        // Обновляем визуальное состояние предметов в инвентаре
        UpdateInventoryVisuals();
        
        Debug.Log($"[TradeSystem] Added item {itemInfo.id} to trade slot {slotIndex} by {playerCore.playerName}");
    }
    
    [Command(requiresAuthority = false)]
    public void CmdRemoveItemFromTrade(int slotIndex)
    {
        // БЕЗОПАСНОСТЬ: Защита от спама команд
        if (Time.time - lastCommandTime < commandCooldownSeconds)
        {
            Debug.LogWarning($"[TradeSystem] Player {playerCore.playerName} spamming remove item command");
            return;
        }
        lastCommandTime = Time.time;
        
        if (tradeState != TradeState.Active)
        {
            Debug.Log($"[TradeSystem] Cannot remove item from trade: trade not active");
            return;
        }
        
        // БЕЗОПАСНОСТЬ: Проверяем таймаут торговли
        if (Time.time - tradeStartTime > tradeTimeoutSeconds)
        {
            Debug.LogWarning($"[TradeSystem] Trade timeout for {playerCore.playerName} - cancelling trade");
            CancelTrade();
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= tradeItems.Count)
        {
            Debug.LogError($"[TradeSystem] Invalid slot index: {slotIndex}");
            return;
        }
        
        // Получаем информацию о предмете для логирования
        ItemInfo itemToRemove = tradeItems[slotIndex];
        if (itemToRemove.id != 0)
        {
            Debug.Log($"[TradeSystem] Removing item {itemToRemove.id} from trade slot {slotIndex}");
        }
        
        // Очищаем слот торговли (предмет остается в инвентаре игрока)
        tradeItems[slotIndex] = new ItemInfo { id = 0, quantity = 0 };
        
        // НЕ синхронизируем с партнером до подтверждения предметов
        // Предметы будут видны партнеру только после нажатия Confirm
        
        // Обновляем визуальное состояние предметов в инвентаре
        UpdateInventoryVisuals();
        
        Debug.Log($"[TradeSystem] Removed item from trade slot {slotIndex} by {playerCore.playerName}");
    }
    
    // Публичные методы для UI
    public bool IsTradeActive()
    {
        return tradeState == TradeState.Active;
    }
    
    public bool IsPlayerConfirmed()
    {
        return playerConfirmed;
    }
    
    public bool IsPartnerConfirmed()
    {
        // Получаем статус подтверждения от партнера напрямую
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            return partnerTradeSystem.playerConfirmed;
        }
        return false;
    }
    
    public bool IsPlayerTradeConfirmed()
    {
        return playerTradeConfirmed;
    }
    
    public bool IsPartnerTradeConfirmed()
    {
        // Получаем статус подтверждения торговли от партнера напрямую
        TradeSystem partnerTradeSystem = GetTradeSystemByNetId(tradePartnerNetId);
        if (partnerTradeSystem != null)
        {
            return partnerTradeSystem.playerTradeConfirmed;
        }
        return false;
    }
    
    public string GetTradePartnerName()
    {
        PlayerCore partner = GetPlayerByNetId(tradePartnerNetId);
        return partner?.playerName ?? "Unknown";
    }
    
    public ItemInfo GetTradeItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < tradeItems.Count)
        {
            return tradeItems[slotIndex];
        }
        return new ItemInfo { id = 0, quantity = 0 };
    }
    
    public ItemInfo GetPartnerTradeItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < partnerTradeItems.Count)
        {
            return partnerTradeItems[slotIndex];
        }
        return new ItemInfo { id = 0, quantity = 0 };
    }
    
    private void UpdateInventoryVisuals()
    {
        // Обновляем визуальное состояние предметов в инвентаре только для локального игрока
        if (playerCore != null && playerCore.isLocalPlayer && cachedInventoryUI != null)
        {
            cachedInventoryUI.UpdateInventoryUI();
        }
    }
}
