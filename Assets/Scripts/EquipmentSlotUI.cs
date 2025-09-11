using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] public EquipmentSlot slotType;
    public Inventory.ItemInstance itemInstance;
    private PlayerCore core; // Добавляем поле
    private InventoryUI inventoryUI;

    private void Awake()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();
        core = GetComponentInParent<PlayerCore>(); // Инициализируем core
    }

    public void SetItem(Inventory.ItemInstance instance)
    {
        itemInstance = instance;
        if (instance.item != null)
        {
            itemIcon.sprite = instance.item.icon;
            itemIcon.enabled = true;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
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
        if (itemInstance.item != null)
        {
            core.CmdUnequipItem(slotType);
        }
    }
}