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
    private const float DOUBLE_CLICK_TIME = 0.3f; // Время для двойного клика

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
        tooltipCoroutine = StartCoroutine(DelayedHideTooltip());
    }

    private IEnumerator ShowTooltipAfterDelay(Item item, Vector3 position)
    {
        yield return new WaitForSeconds(0.5f);
        if (inventoryUI != null && !inventoryUI.isTooltipActive)
        {
            inventoryUI.ShowTooltip(item, transform.position + new Vector3(100f, 0f, 0f));
            Debug.Log($"[InventorySlot] Showing tooltip for {item.itemName} (slot {slotIndex})");
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
            Debug.Log($"[InventorySlot] Stacked {draggedItem.itemName} from slot {inventoryUI.draggedSlot.slotIndex} to {slotIndex}");
        }
        else
        {
            Debug.Log($"[InventorySlot] Swapped slots {inventoryUI.draggedSlot.slotIndex} <-> {slotIndex}");
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
        Debug.Log($"[InventorySlot] Begin drag: {item.itemName} (ID: {itemInfo.id}) (slot {slotIndex})");
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
            EquipmentSlotUI matchingSlot = inventoryUI.FindMatchingEquipmentSlot(item.equipmentSlot);
            if (matchingSlot != null)
            {
                Debug.Log($"[InventorySlot] Equipping item: {item.itemName} (ID: {itemInfo.id}) to {matchingSlot.slotType} from slot {slotIndex}");
                core.CmdEquipItem(itemInfo, slotIndex, matchingSlot.slotType);
            }
            else
            {
                Debug.LogWarning($"[InventorySlot] Cannot equip {item.itemName}: no matching slot for {item.equipmentSlot}");
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
        if (eventData.clickCount == 2 && itemInfo.id > 0)
        {
            Item item = itemInfo.GetItem();
            if (item != null && item.equipmentSlot != EquipmentSlot.None)
            {
                EquipmentSlotUI matchingSlot = inventoryUI.FindMatchingEquipmentSlot(item.equipmentSlot);
                if (matchingSlot != null)
                {
                    Debug.Log($"[InventorySlot] Double-click equipping item: {item.itemName} (ID: {itemInfo.id}) to {matchingSlot.slotType} from slot {slotIndex}");
                    core.CmdEquipItem(itemInfo, slotIndex, matchingSlot.slotType);
                }
                else
                {
                    Debug.LogWarning($"[InventorySlot] Cannot equip {item.itemName} on double-click: no matching slot for {item.equipmentSlot}");
                }
            }
        }
    }

    private string GetComponentsOnPointerEnter(GameObject go)
    {
        if (go == null) return "null";
        var components = go.GetComponents<Component>();
        return string.Join(", ", System.Linq.Enumerable.Select(components, c => c.GetType().Name));
    }

    private IEnumerator DelayedHideTooltip()
    {
        yield return new WaitForSeconds(0.2f);
        if (inventoryUI != null)
            inventoryUI.HideTooltip();
        tooltipCoroutine = null;
    }
}