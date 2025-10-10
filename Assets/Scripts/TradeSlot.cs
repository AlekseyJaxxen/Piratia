using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class TradeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image highlightImage;
    
    [Header("Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;
    
    private int slotIndex;
    private bool isPlayerSlot;
    private TradeUI tradeUI;
    private ItemInfo itemInfo;
    
    // Drag and drop fields
    private Canvas canvas;
    private GameObject dragIcon;
    
    // Tooltip fields
    private Coroutine tooltipCoroutine;
    
    private void Awake()
    {
        // Находим компоненты если они не назначены
        if (itemIcon == null)
        {
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        }
        
        if (quantityText == null)
        {
            quantityText = transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (highlightImage == null)
        {
            highlightImage = transform.Find("Highlight")?.GetComponent<Image>();
        }
        
        // Находим Canvas для drag and drop
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[TradeSlot] Canvas not found in parent hierarchy - drag and drop may not work");
        }
    }
    
    public void Initialize(int index, bool isPlayer, TradeUI ui)
    {
        slotIndex = index;
        isPlayerSlot = isPlayer;
        tradeUI = ui;
        
        // Проверяем, что это локальный игрок - инициализируем только для него
        PlayerCore playerCore = GetComponentInParent<PlayerCore>();
        if (playerCore == null || !playerCore.isLocalPlayer)
        {
            Debug.Log($"[TradeSlot] Not local player - skipping initialization for slot {index}");
            return;
        }
        
        // Устанавливаем цвет слота в зависимости от типа
        Image slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            if (isPlayerSlot)
            {
                slotImage.color = normalColor;
            }
            else
            {
                slotImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Серый для слотов партнера
            }
        }
    }
    
    public void SetItem(ItemInfo info)
    {
        itemInfo = info;
        
        if (itemInfo.id == 0)
        {
            // Пустой слот
            ClearSlot();
            return;
        }
        
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogWarning($"[TradeSlot] Item with ID {itemInfo.id} not found in database");
            ClearSlot();
            return;
        }
        
        // Устанавливаем иконку
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[TradeSlot] itemIcon is null - cannot display icon for item {item.itemName}");
        }
        
        // Устанавливаем количество
        if (quantityText != null)
        {
            quantityText.text = itemInfo.quantity > 1 ? itemInfo.quantity.ToString() : "";
        }
        
        // Устанавливаем цвет в зависимости от типа слота
        Image slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            if (isPlayerSlot)
            {
                // Красный цвет для предметов игрока в торговле
                slotImage.color = redColor;
            }
            else
            {
                // Нормальный цвет для предметов партнера
                slotImage.color = normalColor;
            }
        }
    }
    
    public void ClearSlot()
    {
        itemInfo = new ItemInfo { id = 0, quantity = 0 };
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        
        if (quantityText != null)
        {
            quantityText.text = "";
        }
        
        Image slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            if (isPlayerSlot)
            {
                slotImage.color = normalColor;
            }
            else
            {
                slotImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }
    }
    
    public void SetHighlighted(bool highlighted)
    {
        if (highlightImage != null)
        {
            highlightImage.enabled = highlighted;
        }
    }
    
    // Обработчики событий мыши
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlighted(true);
        
        if (tradeUI != null && itemInfo.id != 0 && tooltipCoroutine == null)
        {
            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(eventData.position));
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
        
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        
        if (tradeUI != null)
        {
            tradeUI.HideTooltip();
        }
    }
    
    private IEnumerator ShowTooltipAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(0.3f);
        if (tradeUI != null && !tradeUI.isTooltipActive)
        {
            // Позиционируем tooltip левым верхним углом на 25px от курсора
            Vector3 tooltipPosition = position + new Vector3(25f, 25f, 0f);
            tradeUI.OnSlotHovered(slotIndex, isPlayerSlot, tooltipPosition);
        }
        tooltipCoroutine = null;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeSlot] Not local player, ignoring click");
            return;
        }
        
        if (tradeUI != null)
        {
            tradeUI.OnSlotClicked(slotIndex, isPlayerSlot);
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Проверяем, что это локальный игрок
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer)
        {
            Debug.Log("[TradeSlot] Not local player, ignoring drop");
            return;
        }
        
        Debug.Log($"[TradeSlot] OnDrop called on slot {slotIndex}, isPlayerSlot: {isPlayerSlot}");
        
        // Проверяем, что это слот игрока (можно добавлять предметы только в свои слоты)
        if (!isPlayerSlot)
        {
            Debug.Log("[TradeSlot] Cannot drop items into partner slots");
            return;
        }
        
        // Получаем InventorySlot из перетаскиваемого объекта
        InventorySlot draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (draggedSlot == null)
        {
            Debug.Log("[TradeSlot] No InventorySlot found in dragged object");
            return;
        }
        
        // Проверяем, что в слоте инвентаря есть предмет
        if (draggedSlot.itemInfo.id == 0)
        {
            Debug.Log("[TradeSlot] Dragged slot is empty");
            return;
        }
        
        Debug.Log($"[TradeSlot] Dropping item {draggedSlot.itemInfo.id} from inventory slot to trade slot {slotIndex}");
        
        // Получаем TradeSystem локального игрока
        TradeSystem tradeSystem = localPlayer.GetComponent<TradeSystem>();
        if (tradeSystem != null)
        {
            // Добавляем предмет в слот торговли (удаление из инвентаря происходит внутри команды)
            // Для слотов партнера нужно использовать локальный индекс
            int actualSlotIndex = isPlayerSlot ? slotIndex : slotIndex - tradeUI.playerSlots.Count;
            tradeSystem.CmdAddItemToTrade(actualSlotIndex, draggedSlot.itemInfo);
        }
        else
        {
            Debug.LogError("[TradeSlot] TradeSystem not found on local player!");
        }
    }
    
    // Публичные методы для получения информации о слоте
    public int GetSlotIndex()
    {
        return slotIndex;
    }
    
    public bool IsPlayerSlot()
    {
        return isPlayerSlot;
    }
    
    public ItemInfo GetItemInfo()
    {
        return itemInfo;
    }
    
    public bool IsEmpty()
    {
        return itemInfo.id == 0;
    }
    
    // Drag and drop methods
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Проверяем, что это локальный игрок и слот игрока с предметом
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer || !isPlayerSlot || itemInfo.id == 0 || canvas == null)
        {
            return;
        }
        
        // Получаем предмет для получения иконки
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogWarning($"[TradeSlot] Cannot drag item {itemInfo.id} - item is null");
            return;
        }
        
        // Получаем спрайт иконки (приоритет: itemIcon.sprite, затем item.icon)
        Sprite iconSprite = null;
        if (itemIcon != null && itemIcon.sprite != null)
        {
            iconSprite = itemIcon.sprite;
        }
        else if (item.icon != null)
        {
            iconSprite = item.icon;
        }
        
        if (iconSprite == null)
        {
            Debug.LogWarning($"[TradeSlot] Cannot drag item {itemInfo.id} - no icon sprite available");
            return;
        }
        
        // Создаем иконку для перетаскивания
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = iconSprite;
        dragImage.rectTransform.sizeDelta = itemIcon != null ? itemIcon.rectTransform.sizeDelta : new Vector2(64, 64);
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = eventData.position;
        
        Debug.Log($"[TradeSlot] Started dragging item {itemInfo.id} ({item.itemName}) from slot {slotIndex}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.GetComponent<RectTransform>().position = eventData.position;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        
        // Проверяем, что это локальный игрок и слот игрока
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null || !localPlayer.isLocalPlayer || !isPlayerSlot)
        {
            return;
        }
        
        // Если предмет перетащен вне торгового слота, удаляем его из торговли
        TradeSlot targetTradeSlot = eventData.pointerEnter?.GetComponent<TradeSlot>() ?? eventData.pointerEnter?.GetComponentInParent<TradeSlot>();
        
        if (targetTradeSlot == null || targetTradeSlot == this)
        {
            // Предмет перетащен вне торгового слота - удаляем из торговли
            TradeSystem tradeSystem = localPlayer.GetComponent<TradeSystem>();
            if (tradeSystem != null)
            {
                // Для слотов партнера нужно использовать локальный индекс
                int actualSlotIndex = isPlayerSlot ? slotIndex : slotIndex - tradeUI.playerSlots.Count;
                tradeSystem.CmdRemoveItemFromTrade(actualSlotIndex);
                Debug.Log($"[TradeSlot] Removed item {itemInfo.id} from trade slot {slotIndex} by dragging outside");
            }
        }
        else
        {
            Debug.Log($"[TradeSlot] Item {itemInfo.id} dragged to another trade slot");
        }
    }
}
