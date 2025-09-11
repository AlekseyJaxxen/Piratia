using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] public EquipmentSlot slotType;
    public ItemInfo itemInfo;
    private PlayerCore core;
    private InventoryUI inventoryUI;

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
        if (itemInfo.id > 0) // Проверка для непустого слота
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
        Item item = itemInfo.GetItem();
        if (item != null)
        {
            core.CmdUnequipItem(slotType);
        }
    }
}