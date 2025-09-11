using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] public EquipmentSlot slotType;
    public ItemInfo itemInfo;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private GameObject dragIcon;

    private void Awake()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();
        core = GetComponentInParent<PlayerCore>();
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
        if (itemInfo.id > 0)
        {
            Item item = itemInfo.GetItem();
            if (item != null)
            {
                inventoryUI.ShowTooltip(item, transform.position);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI.HideTooltip();
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
}