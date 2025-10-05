using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    public ItemInfo itemInfo;
    public int slotIndex;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private Canvas canvas;
    private GameObject dragIcon;
    private Coroutine tooltipCoroutine;
    private float lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    private void Awake()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();
        core = GetComponentInParent<PlayerCore>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (core == null)
        {
            core = Object.FindFirstObjectByType<PlayerCore>();
            if (core == null)
            {
                Debug.LogError("[InventorySlot] PlayerCore not found in hierarchy or scene!");
            }
        }
        if (inventoryUI == null)
        {
            Debug.LogError("[InventorySlot] InventoryUI not found in parent!");
        }
    }

    public void SetItem(ItemInfo info)
    {
        itemInfo = info;
        Item item = info.GetItem();
        if (item != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
            quantityText.text = info.quantity > 1 ? info.quantity.ToString() : "";
            // Item set in slot
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        itemInfo = new ItemInfo();
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        quantityText.text = "";
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        if (inventoryUI != null)
            inventoryUI.HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInfo.id > 0 && tooltipCoroutine == null)
        {
            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(itemInfo, eventData.position));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        if (inventoryUI != null)
            inventoryUI.HideTooltip();
    }

    private IEnumerator ShowTooltipAfterDelay(ItemInfo itemInfo, Vector3 position)
    {
        yield return new WaitForSeconds(0.3f);
        if (inventoryUI != null && !inventoryUI.isTooltipActive)
        {
            // Позиционируем tooltip левым верхним углом на 25px от курсора
            Vector3 tooltipPosition = position + new Vector3(25f, 25f, 0f);
            inventoryUI.ShowTooltip(itemInfo, tooltipPosition);
        }
        tooltipCoroutine = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryUI.draggedSlot == null || inventoryUI.draggedSlot == this) return;
        Item draggedItem = inventoryUI.draggedSlot.itemInfo.GetItem();
        Item thisItem = itemInfo.GetItem();
        if (draggedItem != null && thisItem != null && draggedItem.id == thisItem.id && itemInfo.quantity < thisItem.maxStack)
        {
            core.CmdStackItems(inventoryUI.draggedSlot.slotIndex, slotIndex, thisItem.maxStack - itemInfo.quantity);
                // Items stacked
        }
        else
        {
            // Slots swapped
            core.CmdSwapInventoryItems(inventoryUI.draggedSlot.slotIndex, slotIndex);
        }
        inventoryUI.draggedSlot = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Item item = itemInfo.GetItem();
        if (item == null || canvas == null) return;
        inventoryUI.draggedSlot = this;
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = itemIcon.sprite;
        dragImage.rectTransform.sizeDelta = itemIcon.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = eventData.position;
        // Begin drag
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemInfo.id < 0 || dragIcon == null) return;
        dragIcon.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            if (dragIcon != null) Destroy(dragIcon);
            inventoryUI.draggedSlot = null;
            return;
        }
        if (core == null)
        {
            Debug.LogError("[InventorySlot] Cannot perform drag operation: PlayerCore is null!");
            if (dragIcon != null) Destroy(dragIcon);
            inventoryUI.draggedSlot = null;
            return;
        }
        EquipmentSlotUI targetEquipSlot = eventData.pointerEnter?.GetComponent<EquipmentSlotUI>() ?? eventData.pointerEnter?.GetComponentInParent<EquipmentSlotUI>();
        SkillButton targetButton = eventData.pointerEnter?.GetComponent<SkillButton>() ?? eventData.pointerEnter?.GetComponentInParent<SkillButton>();
        if (targetEquipSlot != null)
        {
            if (item.CanEquipToSlot(targetEquipSlot.slotType) && item.IsEquipable(core.Stats.level, core.Stats.characterClass))
            {
                // Equipping item
                core.CmdEquipItem(itemInfo, slotIndex, targetEquipSlot.slotType);
            }
            else
            {
                // Если не подходит к конкретному слоту, попробуем найти подходящий слот автоматически
                EquipmentSlotUI autoSlot = InventoryUI.Instance.FindMatchingEquipmentSlot(item);
                if (autoSlot != null && item.IsEquipable(core.Stats.level, core.Stats.characterClass))
                {
                    Debug.Log($"[InventorySlot] Auto-equipping {item.itemName} to {autoSlot.slotType} instead of {targetEquipSlot.slotType}");
                    core.CmdEquipItem(itemInfo, slotIndex, autoSlot.slotType);
                }
                else
                {
                    Debug.LogWarning($"[InventorySlot] Cannot equip {item.itemName}: slot {targetEquipSlot.slotType} does not match {item.equipmentSlot} or {item.alternativeSlot} or level {core.Stats.level} < {item.requiredLevel} or class {core.Stats.characterClass} != {item.characterClass}");
                }
            }
        }
        else if (targetButton != null && item.canHotbar && targetButton.buttonIndex != 0)
        {
            Debug.Log($"[InventorySlot] Assigning item: {item.itemName} (ID: {itemInfo.id}) to hotbar slot {targetButton.buttonIndex}");
            PlayerUI.Instance.AssignItemToHotbar(item, targetButton, slotIndex);
        }
        else if (item.canDrop)
        {
            Debug.Log($"[InventorySlot] Dropping item: {item.itemName} (ID: {itemInfo.id}) from slot {slotIndex}");
            core.CmdDropItem(item.id, slotIndex);
        }
        else
        {
            Debug.LogWarning($"[InventorySlot] Drag ended without action: {item.itemName} (slot {slotIndex}), pointerEnter={eventData.pointerEnter?.name ?? "null"}, components={GetComponentsOnPointerEnter(eventData.pointerEnter)}");
        }
        if (dragIcon != null) Destroy(dragIcon);
        inventoryUI.draggedSlot = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemInfo.id > 0)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick < DOUBLE_CLICK_TIME)
            {
                Item item = itemInfo.GetItem();
                if (item != null)
                {
                    // Обработка сундуков
                    if (item.itemType == ItemType.Chest && item.canUse)
                    {
                        Debug.Log($"[InventorySlot] Double-click opening chest: {itemInfo.GetItemName()} (ID: {itemInfo.id}) from slot {slotIndex}");
                        core.CmdSelectItem(itemInfo.id, slotIndex);
                        return;
                    }
                    
                    // Обработка экипируемых предметов
                    if (item.IsEquipable(core.Stats.level, core.Stats.characterClass))
                    {
                        // Проверяем InventoryUI.Instance
                        if (InventoryUI.Instance == null)
                        {
                            Debug.LogError("[InventorySlot] InventoryUI.Instance is null, cannot equip item");
                            return;
                        }
                        
                        // Используем InventoryUI.FindMatchingEquipmentSlot для правильной логики двуручного оружия
                        EquipmentSlotUI matchingSlot = InventoryUI.Instance.FindMatchingEquipmentSlot(item);
                        if (matchingSlot != null)
                        {
                            Debug.Log($"[InventorySlot] Double-click equipping item: {itemInfo.GetItemName()} (ID: {itemInfo.id}) to {matchingSlot.slotType} from slot {slotIndex}");
                            core.CmdEquipItem(itemInfo, slotIndex, matchingSlot.slotType);
                        }
                        else
                        {
                            Debug.LogWarning($"[InventorySlot] Cannot equip {itemInfo.GetItemName()} on double-click: no matching slot found");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[InventorySlot] Cannot equip {itemInfo.GetItemName()} on double-click: item is null or level {core.Stats.level} < {item?.requiredLevel} or class {core.Stats.characterClass} != {item?.characterClass}");
                    }
                }
            }
            lastClickTime = Time.time;
        }
    }

    private string GetComponentsOnPointerEnter(GameObject go)
    {
        if (go == null) return "null";
        var components = go.GetComponents<Component>();
        return string.Join(", ", System.Linq.Enumerable.Select(components, c => c.GetType().Name));
    }

}