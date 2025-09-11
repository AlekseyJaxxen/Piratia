using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    public Inventory.ItemInstance itemInstance;
    public int slotIndex;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private Canvas canvas;
    private GameObject dragIcon;

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
    }

    public void SetItem(Inventory.ItemInstance instance)
    {
        itemInstance = instance;
        if (instance.item != null)
        {
            itemIcon.sprite = instance.item.icon;
            itemIcon.enabled = true;
            quantityText.text = instance.quantity > 1 ? instance.quantity.ToString() : "";
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        itemInstance = new Inventory.ItemInstance();
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        quantityText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInstance.item != null)
        {
            inventoryUI.ShowTooltip(itemInstance.item, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI.HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemInstance.item == null || canvas == null) return;
        inventoryUI.draggedSlot = this;
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = itemIcon.sprite;
        dragImage.rectTransform.sizeDelta = itemIcon.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = itemIcon.rectTransform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemInstance.item == null || dragIcon == null) return;
        dragIcon.GetComponent<RectTransform>().anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemInstance.item == null)
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
        EquipmentSlotUI targetEquipSlot = eventData.pointerEnter?.GetComponent<EquipmentSlotUI>();
        SkillButton hotbarButton = eventData.pointerEnter?.GetComponent<SkillButton>();

        if (targetSlot != null && targetSlot != this)
        {
            core.CmdSwapInventoryItems(slotIndex, targetSlot.slotIndex);
        }
        else if (targetEquipSlot != null && itemInstance.item.equipmentSlot == targetEquipSlot.slotType)
        {
            core.CmdEquipItem(itemInstance, slotIndex, targetEquipSlot.slotType);
        }
        else if (hotbarButton != null && itemInstance.item.canHotbar && hotbarButton.buttonIndex != 0)
        {
            PlayerUI.Instance.AssignItemToHotbar(itemInstance.item, hotbarButton);
            core.CmdUseItem(itemInstance.item, slotIndex);
        }
        if (dragIcon != null) Destroy(dragIcon);
        inventoryUI.draggedSlot = null;
    }
}