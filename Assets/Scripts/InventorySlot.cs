using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    public ItemInfo itemInfo;
    public int slotIndex;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private Canvas canvas;
    private GameObject dragIcon;
    private bool isTooltipActive;

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
            core = FindObjectOfType<PlayerCore>();
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
            Debug.Log($"[InventorySlot] Set item: {item.itemName} (ID: {info.id}) in slot {slotIndex}, quantity: {info.quantity}");
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
        if (isTooltipActive)
        {
            inventoryUI.HideTooltip();
            isTooltipActive = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Item item = itemInfo.GetItem();
        if (item != null && !isTooltipActive)
        {
            inventoryUI.ShowTooltip(item, transform.position);
            isTooltipActive = true;
            Debug.Log($"[InventorySlot] Showing tooltip for {item.itemName} (slot {slotIndex})");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isTooltipActive)
        {
            inventoryUI.HideTooltip();
            isTooltipActive = false;
            Debug.Log($"[InventorySlot] Hiding tooltip (slot {slotIndex})");
        }
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
        dragImage.rectTransform.position = itemIcon.rectTransform.position;
        Debug.Log($"[InventorySlot] Begin drag: {item.itemName} (ID: {itemInfo.id}) (slot {slotIndex})");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemInfo.id < 0 || dragIcon == null) return;
        dragIcon.GetComponent<RectTransform>().anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            if (dragIcon != null) Destroy(dragIcon);
            return;
        }
        if (core == null)
        {
            Debug.LogError("[InventorySlot] Cannot perform drag operation: PlayerCore is null!");
            if (dragIcon != null) Destroy(dragIcon);
            inventoryUI.draggedSlot = null;
            return;
        }
        InventorySlot targetSlot = eventData.pointerEnter?.GetComponent<InventorySlot>();
        EquipmentSlotUI targetEquipSlot = eventData.pointerEnter?.GetComponent<EquipmentSlotUI>() ?? eventData.pointerEnter?.GetComponentInParent<EquipmentSlotUI>();
        SkillButton hotbarButton = eventData.pointerEnter?.GetComponent<SkillButton>();

        if (targetSlot != null && targetSlot != this)
        {
            Debug.Log($"[InventorySlot] Swapping slots: {slotIndex} <-> {targetSlot.slotIndex}");
            core.CmdSwapInventoryItems(slotIndex, targetSlot.slotIndex);
        }
        else if (targetEquipSlot != null)
        {
            Debug.Log($"[InventorySlot] Detected targetEquipSlot: {targetEquipSlot.gameObject.name}, slotType: {targetEquipSlot.slotType}, item slotType: {item.equipmentSlot}, raycastTarget: {(eventData.pointerEnter != null ? eventData.pointerEnter.GetComponent<Image>()?.raycastTarget : "null")}");
            if (item.equipmentSlot == targetEquipSlot.slotType)
            {
                Debug.Log($"[InventorySlot] Equipping item: {item.itemName} (ID: {itemInfo.id}) to {targetEquipSlot.slotType} from slot {slotIndex}");
                core.CmdEquipItem(itemInfo, slotIndex, targetEquipSlot.slotType);
            }
            else
            {
                Debug.LogWarning($"[InventorySlot] Cannot equip {item.itemName} to {targetEquipSlot.slotType}: incompatible slot type (expected {item.equipmentSlot})");
            }
        }
        else if (hotbarButton != null && item.canHotbar && hotbarButton.buttonIndex != 0)
        {
            Debug.Log($"[InventorySlot] Assigning item: {item.itemName} (ID: {itemInfo.id}) to hotbar slot {hotbarButton.buttonIndex}");
            PlayerUI.Instance.AssignItemToHotbar(item, hotbarButton);
            core.CmdUseItem(item.id, slotIndex);
        }
        else
        {
            Debug.LogWarning($"[InventorySlot] Drag ended without action: {item.itemName} (slot {slotIndex}), pointerEnter={eventData.pointerEnter?.name ?? "null"}, components={GetComponentsOnPointerEnter(eventData.pointerEnter)}");
        }
        if (dragIcon != null) Destroy(dragIcon);
        inventoryUI.draggedSlot = null;
    }

    private string GetComponentsOnPointerEnter(GameObject go)
    {
        if (go == null) return "null";
        var components = go.GetComponents<Component>();
        return string.Join(", ", System.Linq.Enumerable.Select(components, c => c.GetType().Name));
    }
}