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
        inventoryUI = FindObjectOfType<InventoryUI>();
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
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(inventoryUI.GetComponent<Canvas>().transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = iconImage.sprite;
        dragImage.rectTransform.sizeDelta = iconImage.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        SkillButton targetButton = eventData.pointerEnter?.GetComponent<SkillButton>();
        InventorySlot targetSlot = eventData.pointerEnter?.GetComponent<InventorySlot>();

        if (targetButton != null && targetButton != this)
        {
            PlayerUI.Instance.SwapSkillsOrItems(this, targetButton);
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
        Destroy(dragIcon);
    }
}