using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int buttonIndex;
    public SkillBase skill;
    public Item item;
    private PlayerSkills skillsComponent;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private Image iconImage;
    private GameObject dragIcon;

    private void Awake()
    {
        inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
        iconImage = GetComponentInChildren<Image>();
    }

    public void Initialize(PlayerSkills skills, PlayerCore playerCore, int index)
    {
        skillsComponent = skills;
        core = playerCore;
        buttonIndex = index;
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    public void OnButtonClicked()
    {
        if (core == null)
        {
            Debug.LogError("[SkillButton] PlayerCore is null!");
            return;
        }
        if (skill != null)
        {
            if (core.isDead || core.isStunned || (core.isSilenced && !(skill is BasicAttackSkill)))
            {
                Debug.Log($"[SkillButton] Cannot select skill {skill.SkillName}: Player is dead, stunned, or silenced");
                return;
            }
            if (skillsComponent != null)
            {
                skillsComponent.SelectSkill(skill);
                Debug.Log($"[SkillButton] Skill {skill.SkillName} selected, index: {buttonIndex}");
            }
        }
        else if (item != null)
        {
            if (core.isDead || core.isStunned)
            {
                Debug.Log($"[SkillButton] Cannot use item {item.itemName}: Player is dead or stunned");
                return;
            }
            core.CmdUseItem(item.id, -1);
            Debug.Log($"[SkillButton] Item used: {item.itemName} (ID: {item.id}), index: {buttonIndex}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventoryUI == null) return;
        if (skill != null)
        {
            inventoryUI.ShowSkillTooltip(skill, transform.position);
        }
        else if (item != null)
        {
            inventoryUI.ShowTooltip(item, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventoryUI != null)
        {
            inventoryUI.HideTooltip();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if ((skill == null || buttonIndex == 0) && item == null) return;
        if (iconImage == null)
        {
            Debug.LogError("[SkillButton] iconImage is null!");
            return;
        }
        Canvas canvas = inventoryUI.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SkillButton] Canvas not found in parent of InventoryUI!");
            return;
        }
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = iconImage.sprite;
        dragImage.rectTransform.sizeDelta = iconImage.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = eventData.position;
        Debug.Log($"[SkillButton] Begin drag: {(skill != null ? skill.SkillName : item != null ? item.itemName : "null")} (index: {buttonIndex})");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        dragIcon.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        SkillButton targetButton = eventData.pointerEnter?.GetComponent<SkillButton>() ?? eventData.pointerEnter?.GetComponentInParent<SkillButton>();
        InventorySlot targetSlot = eventData.pointerEnter?.GetComponent<InventorySlot>();

        if (targetButton != null && targetButton != this)
        {
            PlayerUI.Instance.SwapSkillsOrItems(this, targetButton);
            Debug.Log($"[SkillButton] Swapped with button {targetButton.buttonIndex}");
        }
        else if (targetSlot != null && item != null)
        {
            if (targetSlot.itemInfo.id >= 0)
            {
                Item targetItem = targetSlot.itemInfo.GetItem();
                if (targetItem?.canHotbar == true)
                {
                    PlayerUI.Instance.AssignItemToHotbar(targetItem, this);
                    core.CmdUseItem(targetSlot.itemInfo.id, targetSlot.slotIndex);
                    Debug.Log($"[SkillButton] Assigned item from slot {targetSlot.slotIndex} to hotbar button {buttonIndex}");
                }
                else
                {
                    Debug.LogWarning($"[SkillButton] Cannot assign item from slot {targetSlot.slotIndex} to hotbar: canHotbar is false or item is null");
                }
            }
            else
            {
                core.CmdDropItem(item.id, -1);
                item = null;
                iconImage.sprite = PlayerUI.Instance.GetDefaultEmptySprite();
                Debug.Log($"[SkillButton] Dropped item from hotbar button {buttonIndex}");
            }
        }
        else
        {
            Debug.LogWarning($"[SkillButton] Drag ended without action: {(skill != null ? skill.SkillName : item != null ? item.itemName : "null")} (index: {buttonIndex}), pointerEnter={eventData.pointerEnter?.name ?? "null"}, components={GetComponentsOnPointerEnter(eventData.pointerEnter)}");
        }
        Destroy(dragIcon);
    }

    private string GetComponentsOnPointerEnter(GameObject go)
    {
        if (go == null) return "null";
        var components = go.GetComponents<Component>();
        return string.Join(", ", System.Linq.Enumerable.Select(components, c => c.GetType().Name));
    }
}