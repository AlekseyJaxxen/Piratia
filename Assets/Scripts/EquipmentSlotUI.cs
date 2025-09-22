using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] public EquipmentSlot slotType;
    public ItemInfo itemInfo;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private GameObject dragIcon;
    private Coroutine tooltipCoroutine;
    private float lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;
    private void Awake()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();
        core = GetComponentInParent<PlayerCore>();
        if (core == null)
        {
            core = FindObjectOfType<PlayerCore>();
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
            Debug.Log($"[EquipmentSlotUI] Set item: {item.itemName} (ID: {info.id}) in slot {slotType}");
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            Debug.Log($"[EquipmentSlotUI] Cleared slot {slotType}");
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Item item = itemInfo.GetItem();
        if (item != null && tooltipCoroutine == null)
        {
            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(item, eventData.position));
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        inventoryUI.HideTooltip();
        Debug.Log($"[EquipmentSlotUI] Hiding tooltip for slot {slotType}");
    }
    private IEnumerator ShowTooltipAfterDelay(Item item, Vector3 position)
    {
        yield return new WaitForSeconds(0.5f);
        if (inventoryUI != null && !inventoryUI.isTooltipActive)
        {
            inventoryUI.ShowTooltip(item, position);
            Debug.Log($"[EquipmentSlotUI] Showing tooltip for {item.itemName} (slot {slotType})");
        }
        tooltipCoroutine = null;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemInfo.id <= 0) return;
        Item item = itemInfo.GetItem();
        if (item == null) return;
        Canvas canvas = inventoryUI.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[EquipmentSlotUI] Canvas not found in parent of InventoryUI!");
            return;
        }
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = itemIcon.sprite;
        dragImage.rectTransform.sizeDelta = itemIcon.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = eventData.position;
        Debug.Log($"[EquipmentSlotUI] Begin drag: {item.itemName} (ID: {itemInfo.id}) from slot {slotType}");
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        dragIcon.GetComponent<RectTransform>().position = eventData.position;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        if (itemInfo.id <= 0) return;
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Destroy(dragIcon);
            return;
        }
        InventorySlot targetSlot = eventData.pointerEnter?.GetComponent<InventorySlot>();
        if (targetSlot != null)
        {
            Debug.Log($"[EquipmentSlotUI] Unequipping item: {item.itemName} (ID: {itemInfo.id}) from slot {slotType} to inventory slot {targetSlot.slotIndex}");
            core.CmdUnequipItem(slotType);
        }
        else if (item.canDrop)
        {
            Debug.Log($"[EquipmentSlotUI] Dropping item: {item.itemName} (ID: {itemInfo.id}) from slot {slotType}");
            core.CmdDropItem(itemInfo.id, -1);
            core.CmdUnequipItem(slotType);
        }
        else
        {
            Debug.LogWarning($"[EquipmentSlotUI] Drag ended without action: {item.itemName} (ID: {itemInfo.id}) from slot {slotType}, pointerEnter={eventData.pointerEnter?.name ?? "null"}");
        }
        Destroy(dragIcon);
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryUI.draggedSlot == null || inventoryUI.draggedSlot.itemInfo.id <= 0)
        {
            Debug.LogWarning($"[EquipmentSlotUI] OnDrop failed: draggedSlot is null or invalid (ID: {(inventoryUI.draggedSlot?.itemInfo.id ?? -1)})");
            inventoryUI.draggedSlot = null;
            return;
        }
        Item item = inventoryUI.draggedSlot.itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogWarning($"[EquipmentSlotUI] OnDrop failed: Item with ID {inventoryUI.draggedSlot.itemInfo.id} not found");
            inventoryUI.draggedSlot = null;
            return;
        }
        if (!item.CanEquipToSlot(slotType))
        {
            Debug.LogWarning($"[EquipmentSlotUI] Cannot equip {item.itemName}: slot {slotType} does not match item slot {item.equipmentSlot} or {item.alternativeSlot}");
            inventoryUI.draggedSlot = null;
            return;
        }
        if (!item.IsEquipable(core.Stats.level, core.Stats.characterClass))
        {
            Debug.LogWarning($"[EquipmentSlotUI] Cannot equip {item.itemName}: player level {core.Stats.level} or class {core.Stats.characterClass} does not match required level {item.requiredLevel} or class {item.characterClass}");
            inventoryUI.draggedSlot = null;
            return;
        }
        // Авто-своп: если слот занят, unequip сначала
        if (itemInfo.id > 0)
        {
            Debug.Log($"[EquipmentSlotUI] Slot {slotType} occupied, auto-unequip first");
            core.CmdUnequipItem(slotType);
        }
        Debug.Log($"[EquipmentSlotUI] OnDrop: Equipping {item.itemName} (ID: {inventoryUI.draggedSlot.itemInfo.id}) from slot {inventoryUI.draggedSlot.slotIndex} to {slotType}");
        core.CmdEquipItem(inventoryUI.draggedSlot.itemInfo, inventoryUI.draggedSlot.slotIndex, slotType);
        inventoryUI.draggedSlot = null;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemInfo.id > 0)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick < DOUBLE_CLICK_TIME)
            {
                core.CmdUnequipItem(slotType);
                Debug.Log($"[EquipmentSlotUI] Double-click unequipping item: {itemInfo.GetItem()?.itemName ?? "null"} from {slotType}");
            }
            lastClickTime = Time.time;
        }
    }
}