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
        // Проверяем, что UI объекты не уничтожены
        if (itemIcon == null)
        {
            Debug.LogWarning($"[EquipmentSlotUI] itemIcon is null for slot {slotType}, skipping SetItem");
            return;
        }
        
        itemInfo = info;
        Item item = info.GetItem();
        Debug.Log($"[EquipmentSlotUI] SetItem for {slotType}: ID={info.id}, quantity={info.quantity}, item={item?.itemName ?? "null"}");
        if (item != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
            Debug.Log($"[EquipmentSlotUI] Item set in slot {slotType}: {item.itemName}");
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            Debug.Log($"[EquipmentSlotUI] Slot {slotType} cleared");
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipCoroutine == null)
        {
            if (itemInfo.id > 0)
            {
                // Показываем тултип предмета
                tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(itemInfo, eventData.position));
            }
            else
            {
                // Показываем тултип пустого слота
                tooltipCoroutine = StartCoroutine(ShowEmptySlotTooltipAfterDelay(eventData.position));
            }
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
    
    private IEnumerator ShowEmptySlotTooltipAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(0.3f);
        if (inventoryUI != null && !inventoryUI.isTooltipActive)
        {
            // Позиционируем tooltip левым верхним углом на 25px от курсора
            Vector3 tooltipPosition = position + new Vector3(25f, 25f, 0f);
            inventoryUI.ShowEmptySlotTooltip(slotType, tooltipPosition);
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
        // Begin drag
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
            // Unequipping item
            core.CmdUnequipItem(slotType);
        }
        else if (item.canDrop)
        {
            // Dropping item
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
            string expectedSlots = item.isTwoHanded ? "LeftHand only" : $"{item.equipmentSlot} or {item.alternativeSlot}";
            Debug.LogWarning($"[EquipmentSlotUI] Cannot equip {item.itemName}: slot {slotType} does not match expected slots ({expectedSlots})");
            inventoryUI.draggedSlot = null;
            return;
        }
        if (!item.IsEquipable(core.Stats.level, core.Stats.characterClass))
        {
            Debug.LogWarning($"[EquipmentSlotUI] Cannot equip {item.itemName}: player level {core.Stats.level} or class {core.Stats.characterClass} does not match required level {item.requiredLevel} or class {item.characterClass}");
            inventoryUI.draggedSlot = null;
            return;
        }
        // ����-����: ���� ���� �����, unequip �������
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