using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class TradeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tradePanel;
    [SerializeField] private Transform playerSlotsContainer;
    [SerializeField] private Transform partnerSlotsContainer;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI partnerNameText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject itemTooltip;
    [SerializeField] private TextMeshProUGUI tooltipText;
    
    [Header("Settings")]
    [SerializeField] private float tooltipOffsetX = 25f;
    [SerializeField] private float tooltipOffsetY = 25f;
    
    public TradeSystem tradeSystem;
    public List<TradeSlot> playerSlots = new List<TradeSlot>();
    private List<TradeSlot> partnerSlots = new List<TradeSlot>();
    public bool isTooltipActive;
    
    public static TradeUI Instance { get; private set; }
    
    private void Awake()
    {
        // Проверяем, что это локальный игрок
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore != null && playerCore.isLocalPlayer)
        {
            Instance = this;
            Debug.Log("[TradeUI] Initialized for local player");
        }
        else
        {
            Debug.Log("[TradeUI] Not local player - UI will be inactive");
        }
        
        // Скрываем UI по умолчанию
        HideTradeWindow();
    }
    
    public void Initialize(TradeSystem tradeSystem)
    {
        this.tradeSystem = tradeSystem;
        
        // Подписываемся на события только для локального игрока
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (tradeSystem != null && playerCore != null && playerCore.isLocalPlayer)
        {
            tradeSystem.OnTradeStarted.AddListener(OnTradeStarted);
            tradeSystem.OnTradeEnded.AddListener(OnTradeEnded);
            Debug.Log($"[TradeUI] Initialized with TradeSystem for local player {playerCore.playerName}");
        }
        else if (tradeSystem != null)
        {
            Debug.Log($"[TradeUI] Initialized with TradeSystem but not local player - no event subscription");
        }
        
        // Находим слоты торговли
        FindTradeSlots();
    }
    
    private void FindTradeSlots()
    {
        // Проверяем, что это локальный игрок - инициализируем слоты только для него
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore == null || !playerCore.isLocalPlayer)
        {
            Debug.Log("[TradeUI] Not local player - skipping slot initialization");
            return;
        }
        
        // Находим все слоты игрока
        TradeSlot[] playerSlotArray = playerSlotsContainer.GetComponentsInChildren<TradeSlot>();
        playerSlots.AddRange(playerSlotArray);
        
        // Находим все слоты партнера
        TradeSlot[] partnerSlotArray = partnerSlotsContainer.GetComponentsInChildren<TradeSlot>();
        partnerSlots.AddRange(partnerSlotArray);
        
        // Ограничиваем количество слотов до tradeSlotsCount (по 10 на игрока и партнера)
        int maxSlotsPerPlayer = tradeSystem != null ? tradeSystem.tradeSlotsCount / 2 : 10; // 10 слотов на игрока
        if (playerSlots.Count > maxSlotsPerPlayer)
        {
            Debug.LogWarning($"[TradeUI] Player has {playerSlots.Count} slots, limiting to {maxSlotsPerPlayer}");
            playerSlots = playerSlots.GetRange(0, maxSlotsPerPlayer);
        }
        if (partnerSlots.Count > maxSlotsPerPlayer)
        {
            Debug.LogWarning($"[TradeUI] Partner has {partnerSlots.Count} slots, limiting to {maxSlotsPerPlayer}");
            partnerSlots = partnerSlots.GetRange(0, maxSlotsPerPlayer);
        }
        
        // Инициализируем слоты
        for (int i = 0; i < playerSlots.Count; i++)
        {
            playerSlots[i].Initialize(i, true, this);
        }
        
        for (int i = 0; i < partnerSlots.Count; i++)
        {
            // Слоты партнера должны иметь индексы после слотов игрока
            partnerSlots[i].Initialize(i + playerSlots.Count, false, this);
        }
        
        Debug.Log($"[TradeUI] Found {playerSlots.Count} player slots and {partnerSlots.Count} partner slots");
    }
    
    private void Start()
    {
        // Получаем PlayerCore в начале метода
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        
        // Если TradeSystem не был инициализирован через Initialize(), получаем его из родительского объекта
        if (tradeSystem == null)
        {
            if (playerCore != null)
            {
                tradeSystem = playerCore.GetComponent<TradeSystem>();
                
                // Подписываемся на события только если это локальный игрок
                if (tradeSystem != null && playerCore.isLocalPlayer)
                {
                    tradeSystem.OnTradeStarted.AddListener(OnTradeStarted);
                    tradeSystem.OnTradeEnded.AddListener(OnTradeEnded);
                    Debug.Log($"[TradeUI] Subscribed to TradeSystem events for local player {playerCore.playerName}");
                }
                else if (tradeSystem != null)
                {
                    Debug.Log($"[TradeUI] TradeSystem found but not local player - no event subscription");
                }
            }
        }
        
        // Настраиваем кнопки
        SetupButtons();
        
        // Находим слоты
        FindTradeSlots();
        
        if (playerCore != null)
        {
            Debug.Log($"[TradeUI] Start completed for {playerCore.playerName} (isLocalPlayer: {playerCore.isLocalPlayer})");
        }
        else
        {
            Debug.Log("[TradeUI] Start completed");
        }
    }
    
    private void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        
        if (tradeButton != null)
        {
            tradeButton.onClick.AddListener(OnTradeButtonClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }
    
    
    private void OnConfirmButtonClicked()
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeUI] Not local player, ignoring confirm click");
            return;
        }
        
        if (tradeSystem != null)
        {
            tradeSystem.CmdConfirmTrade();
            Debug.Log("[TradeUI] Confirm button clicked by local player");
        }
    }
    
    private void OnTradeButtonClicked()
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeUI] Not local player, ignoring trade click");
            return;
        }
        
        if (tradeSystem != null && tradeSystem.IsPlayerConfirmed() && !tradeSystem.IsPlayerTradeConfirmed())
        {
            // Завершаем торговлю
            tradeSystem.CmdCompleteTrade();
            Debug.Log("[TradeUI] Trade button clicked by local player - trade completed");
        }
        else
        {
            Debug.Log("[TradeUI] Trade button clicked but trade not ready or already confirmed");
        }
    }
    
    private void OnCancelButtonClicked()
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeUI] Not local player, ignoring cancel click");
            return;
        }
        
        if (tradeSystem != null)
        {
            tradeSystem.CmdCancelTrade();
            Debug.Log("[TradeUI] Cancel button clicked by local player");
        }
        
        HideTradeWindow();
    }
    
    public void ShowTradeWindow()
    {
        if (tradePanel != null)
        {
            tradePanel.SetActive(true);
            UpdateTradeUI();
            Debug.Log("[TradeUI] Trade window shown");
        }
        else
        {
            Debug.LogError("[TradeUI] tradePanel is null, cannot show trade window");
        }
    }
    
    public void HideTradeWindow()
    {
        if (tradePanel != null)
        {
            tradePanel.SetActive(false);
            Debug.Log("[TradeUI] Trade window hidden");
        }
        
        HideTooltip();
    }
    
    public void UpdateTradeUI()
    {
        if (tradeSystem == null) return;
        
        // Обновляем имена игроков
        if (playerNameText != null)
        {
            playerNameText.text = tradeSystem.playerCore.playerName;
        }
        
        if (partnerNameText != null)
        {
            partnerNameText.text = tradeSystem.GetTradePartnerName();
        }
        
        // Обновляем слоты игрока (снизу - предметы локального игрока)
        for (int i = 0; i < playerSlots.Count && i < tradeSystem.tradeItems.Count; i++)
        {
            playerSlots[i].SetItem(tradeSystem.tradeItems[i]);
        }
        
        // Обновляем слоты партнера (сверху - предметы партнера)
        for (int i = 0; i < partnerSlots.Count && i < tradeSystem.partnerTradeItems.Count; i++)
        {
            partnerSlots[i].SetItem(tradeSystem.partnerTradeItems[i]);
        }
        
        // Обновляем состояние кнопок
        UpdateButtonStates();
        
        // Обновляем статус
        UpdateStatusText();
    }
    
    private void UpdateButtonStates()
    {
        if (tradeSystem == null) return;
        
        bool isPlayerConfirmed = tradeSystem.IsPlayerConfirmed();
        bool isPartnerConfirmed = tradeSystem.IsPartnerConfirmed();
        bool isPlayerTradeConfirmed = tradeSystem.IsPlayerTradeConfirmed();
        bool isPartnerTradeConfirmed = tradeSystem.IsPartnerTradeConfirmed();
        
        Debug.Log($"[TradeUI] UpdateButtonStates - PlayerConfirmed: {isPlayerConfirmed}, TradeState: {tradeSystem.tradeState}");
        
        // Кнопка "Подтвердить сделку" активна если игрок подтвердил предметы (независимо от партнера)
        if (tradeButton != null)
        {
            bool canTrade = isPlayerConfirmed && tradeSystem.tradeState == TradeSystem.TradeState.Active;
            tradeButton.interactable = canTrade && !isPlayerTradeConfirmed;
            Debug.Log($"[TradeUI] Trade button - canTrade: {canTrade}, interactable: {tradeButton.interactable}");
        }
        
        // Кнопка "Зафиксировать" активна если игрок еще не подтвердил и торговля активна
        if (confirmButton != null)
        {
            bool canConfirm = !isPlayerConfirmed && tradeSystem.tradeState == TradeSystem.TradeState.Active;
            confirmButton.interactable = canConfirm;
            Debug.Log($"[TradeUI] Confirm button - canConfirm: {canConfirm}, interactable: {confirmButton.interactable}");
        }
        else
        {
            Debug.LogError("[TradeUI] Confirm button is null!");
        }
    }
    
    private void UpdateStatusText()
    {
        if (statusText == null || tradeSystem == null) return;
        
        bool isPlayerConfirmed = tradeSystem.IsPlayerConfirmed();
        bool isPartnerConfirmed = tradeSystem.IsPartnerConfirmed();
        bool isPlayerTradeConfirmed = tradeSystem.IsPlayerTradeConfirmed();
        bool isPartnerTradeConfirmed = tradeSystem.IsPartnerTradeConfirmed();
        
        if (isPlayerTradeConfirmed && isPartnerTradeConfirmed)
        {
            statusText.text = "Trade completed!";
            statusText.color = Color.green;
        }
        else if (isPlayerTradeConfirmed)
        {
            statusText.text = "You confirmed trade - Waiting for partner...";
            statusText.color = Color.yellow;
        }
        else if (isPartnerTradeConfirmed)
        {
            statusText.text = "Partner confirmed trade - Click 'Trade' to complete";
            statusText.color = Color.yellow;
        }
        else if (isPlayerConfirmed)
        {
            statusText.text = "Items confirmed - Click 'Trade' to complete";
            statusText.color = Color.green;
        }
        else if (isPlayerConfirmed && isPartnerConfirmed)
        {
            statusText.text = "Both players confirmed items - Trade locked";
            statusText.color = Color.green;
        }
        else if (isPlayerConfirmed)
        {
            statusText.text = "You confirmed items - Waiting for partner...";
            statusText.color = Color.yellow;
        }
        else if (isPartnerConfirmed)
        {
            statusText.text = "Partner confirmed items - Click 'Confirm' to proceed";
            statusText.color = Color.yellow;
        }
        else
        {
            statusText.text = "Add items and click 'Confirm' when ready";
            statusText.color = Color.white;
        }
    }
    
    private void OnTradeStarted()
    {
        // Показываем окно торговли только для локального игрока
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore != null && playerCore.isLocalPlayer)
        {
            ShowTradeWindow();
            // Принудительно обновляем UI после показа окна
            UpdateTradeUI();
            Debug.Log($"[TradeUI] Trade window shown for local player {playerCore.playerName}");
        }
        else
        {
            Debug.Log($"[TradeUI] OnTradeStarted ignored - not local player or no PlayerCore found");
        }
    }
    
    private void OnTradeEnded()
    {
        // Скрываем окно торговли только для локального игрока
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore != null && playerCore.isLocalPlayer)
        {
            HideTradeWindow();
            Debug.Log($"[TradeUI] Trade window hidden for local player {playerCore.playerName}");
        }
        else
        {
            Debug.Log($"[TradeUI] OnTradeEnded ignored - not local player or no PlayerCore found");
        }
    }
    
    // Методы для работы с tooltip
    public void ShowTooltip(Vector3 position, ItemInfo itemInfo)
    {
        if (itemTooltip == null || tooltipText == null || isTooltipActive) return;
        
        if (itemInfo.id == 0)
        {
            HideTooltip();
            return;
        }
        
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            HideTooltip();
            return;
        }
        
        // Получаем PlayerCore для проверки возможности экипировки
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore == null) return;
        
        bool canEquip = item.IsEquipable(playerCore.Stats.level, playerCore.Stats.characterClass);
        string rarityColor = GetRarityColor(itemInfo.GetItemRarity());
        
        StringBuilder sb = new StringBuilder();
        
        // Имя предмета с усилением (если есть)
        string itemName = itemInfo.GetItemName();
        // if (itemInfo.enhancementLevel > 0)
        // {
        //     itemName += $" +{itemInfo.enhancementLevel}";
        // }
        sb.AppendLine($"<size=16><b><color={rarityColor}>{itemName}</color></b></size>");
        
        // Основные характеристики предмета (с динамическими статами)
        int totalConstantDefence = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.ConstantDefence));
        int totalPhysicalResistance = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.PhysicalResistance));
        int totalMinAttack = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MinAttack));
        int totalMaxAttack = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxAttack));
        
        if (totalConstantDefence != 0 || totalPhysicalResistance != 0 || totalMinAttack != 0 || totalMaxAttack != 0)
        {
            string stats = "";
            if (totalMinAttack != 0 || totalMaxAttack != 0)
            {
                stats += $"Attack [{totalMinAttack}/{totalMaxAttack}]";
            }
            if (totalConstantDefence != 0)
            {
                if (stats != "") stats += "\n";
                stats += $"Defense [{totalConstantDefence}]";
            }
            if (totalPhysicalResistance != 0)
            {
                if (stats != "") stats += "\n";
                stats += $"Physical Resistance [{totalPhysicalResistance}%]";
            }
            sb.AppendLine($"<size=11><color=#FFFFFF>{stats}</color></size>");
        }
        
        // Прочность
        if (item.durability > 0)
        {
            sb.AppendLine($"<size=11><color=#FFFFFF>Durability [{item.durability}/{item.durability}]</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Требования к предмету
        if (item.requiredLevel > 0)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Level Requirement: {item.requiredLevel}</color></size>");
        }
        
        if (item.characterClass != CharacterClass.None)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Class Requirement: {item.characterClass}</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Бонусы к характеристикам (синим цветом)
        bool hasBonuses = false;
        
        // Основные статы (с динамическими статами)
        int totalStrength = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Strength));
        int totalAgility = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Agility));
        int totalSpirit = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Spirit));
        int totalConstitution = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Constitution));
        int totalAccuracy = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Accuracy));
        
        if (totalStrength != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Strength Bonus: +{totalStrength}</color></size>");
            hasBonuses = true;
        }
        if (totalAgility != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Agility Bonus: +{totalAgility}</color></size>");
            hasBonuses = true;
        }
        if (totalSpirit != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Spirit Bonus: +{totalSpirit}</color></size>");
            hasBonuses = true;
        }
        if (totalConstitution != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Constitution Bonus: +{totalConstitution}</color></size>");
            hasBonuses = true;
        }
        if (totalAccuracy != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Accuracy Bonus: +{totalAccuracy}</color></size>");
            hasBonuses = true;
        }
        
        // Другие статы (с динамическими статами)
        int totalMaxHP = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxHP));
        int totalMaxMP = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxMP));
        int totalMovementSpeed = itemInfo.GetDisplayValue(ItemInfo.StatType.MovementSpeed);
        float totalAttackSpeed = itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackSpeed);
        float totalAttackSpeedPercent = itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackSpeedPercent);
        int totalCriticalChance = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Critical));
        
        if (totalMaxHP != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Maximum HP Bonus: +{totalMaxHP}</color></size>");
            hasBonuses = true;
        }
        if (totalMaxMP != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Maximum SP Bonus: +{totalMaxMP}</color></size>");
            hasBonuses = true;
        }
        if (totalMovementSpeed != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Movement Speed Bonus: +{totalMovementSpeed}</color></size>");
            hasBonuses = true;
        }
        if (totalAttackSpeed != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Attack Speed Bonus: +{totalAttackSpeed:F2}</color></size>");
            hasBonuses = true;
        }
        if (totalAttackSpeedPercent != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Attack Speed Bonus: +{totalAttackSpeedPercent:F1}%</color></size>");
            hasBonuses = true;
        }
        if (totalCriticalChance != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Critical Chance Bonus: +{totalCriticalChance}%</color></size>");
            hasBonuses = true;
        }
        
        // Пустая строка после бонусов
        if (hasBonuses)
        {
            sb.AppendLine("");
        }
        
        // Сокеты (если есть)
        // if (item.socketCount > 0)
        // {
        //     sb.AppendLine($"<size=10><color=#FFFFFF>Socket(s): {item.socketCount}</color></size>");
        //     // Здесь можно добавить отображение вставленных камней
        //     sb.AppendLine("");
        // }
        
        // Торговая ценность
        if (item.price > 0)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Trade Value: {item.price:N0}</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Интерактивные элементы для торговли
        sb.AppendLine($"<size=10><color=#00FF00>Buy</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Sell</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Trade</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Количество в стопке
        sb.AppendLine($"<size=10><color=#FFFFFF>Stack: {itemInfo.quantity}</color></size>");
        
        // Ошибки экипировки
        if (!canEquip)
        {
            sb.AppendLine($"<size=10><color=#FF0000>Cannot equip: level or class mismatch</color></size>");
        }
        
        // Показываем tooltip
        itemTooltip.SetActive(true);
        tooltipText.text = sb.ToString().TrimEnd('\n');
        
        // Настройка параметров фона в коде
        if (itemTooltip != null)
        {
            RectTransform tooltipRect = itemTooltip.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.pivot = new Vector2(0, 1);
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
            }
            
            // Настройка фона - более темный полупрозрачный фон
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                backgroundImage.raycastTarget = false;
            }
        }
        
        // Настройка параметров Text в коде
        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0, 1);
            }
        }
        
        // Позиционирование левым верхним углом на 25px от курсора
        itemTooltip.transform.position = position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f);
        
        isTooltipActive = true;
    }
    
    public void HideTooltip()
    {
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
        isTooltipActive = false;
    }
    
    // Методы для работы со слотами
    public void OnSlotClicked(int slotIndex, bool isPlayerSlot)
    {
        Debug.Log($"[TradeUI] Slot {slotIndex} clicked (Player: {isPlayerSlot})");
    }
    
    public void OnSlotHovered(int slotIndex, bool isPlayerSlot, Vector3 position)
    {
        if (tradeSystem == null) return;
        
        ItemInfo itemInfo;
        if (isPlayerSlot)
        {
            itemInfo = tradeSystem.GetTradeItem(slotIndex);
        }
        else
        {
            // Для слотов партнера нужно вычислить локальный индекс
            int partnerSlotIndex = slotIndex - playerSlots.Count;
            itemInfo = tradeSystem.GetPartnerTradeItem(partnerSlotIndex);
        }
        
        ShowTooltip(position, itemInfo);
    }
    
    public void OnSlotUnhovered()
    {
        HideTooltip();
    }
    
    private void Update()
    {
        // Скрываем tooltip при движении мыши
        if (isTooltipActive && Input.GetMouseButtonDown(0))
        {
            HideTooltip();
        }
    }
    
    private string GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return "#FFFFFF"; // Белый
            case Rarity.Uncommon:
                return "#00FF00"; // Зеленый
            case Rarity.Rare:
                return "#0080FF"; // Синий
            case Rarity.Epic:
                return "#8000FF"; // Фиолетовый
            case Rarity.Legendary:
                return "#FF8000"; // Оранжевый
            default:
                return "#FFFFFF"; // Белый по умолчанию
        }
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (tradeSystem != null)
        {
            tradeSystem.OnTradeStarted.RemoveListener(OnTradeStarted);
            tradeSystem.OnTradeEnded.RemoveListener(OnTradeEnded);
            
            PlayerCore playerCore = GetComponentInParent<PlayerCore>();
            if (playerCore != null)
            {
                Debug.Log($"[TradeUI] Unsubscribed from events for {playerCore.playerName}");
            }
            else
            {
                Debug.Log("[TradeUI] Unsubscribed from events");
            }
        }
    }
}
