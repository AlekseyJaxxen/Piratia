using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public SkillBase skill; // Навык
    public Item item; // Предмет (для hotbar)
    private Button button;
    private PlayerSkills playerSkills;
    private PlayerCore playerCore;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private GameObject dragIcon; // Клон для перетаскивания
    private Image originalIcon; // Оригинальная иконка
    public int buttonIndex; // Индекс кнопки в массиве

    public void Initialize(PlayerSkills skills, PlayerCore core, int index)
    {
        playerSkills = skills;
        playerCore = core;
        buttonIndex = index;
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        originalIcon = GetComponentInChildren<Image>();
        if (button != null && playerSkills != null && playerCore != null && canvas != null && originalIcon != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError($"[SkillButton] Initialization failed: Button={button}, PlayerSkills={playerSkills}, PlayerCore={playerCore}, Canvas={canvas}, OriginalIcon={originalIcon}");
        }
    }

    public void OnButtonClicked()
    {
        if (skill != null)
        {
            if (!playerCore.CanCastSkill(skill))
            {
                Debug.LogWarning($"[SkillButton] Cannot cast skill {skill.SkillName}: invalid conditions.");
                return;
            }
            if (playerSkills.GetRemainingCooldown(skill.SkillName) > 0)
            {
                Debug.LogWarning($"[SkillButton] Skill {skill.SkillName} is on cooldown.");
                return;
            }
            if (!skill.ignoreGlobalCooldown && playerSkills.GetGlobalRemainingCooldown() > 0)
            {
                Debug.LogWarning($"[SkillButton] Global cooldown active for {skill.SkillName}.");
                return;
            }
            playerSkills.SelectSkill(skill);
            Debug.Log($"[SkillButton] Skill selected: {skill.SkillName}");
        }
        else if (item != null && item.canUse)
        {
            playerCore.CmdUseItem(item, -1); // -1, так как предмет в hotbar
            Debug.Log($"[SkillButton] Used item: {item.itemName}");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if ((skill == null && item == null) || canvas == null || originalIcon == null || buttonIndex == 0)
        {
            Debug.Log($"[SkillButton] Drag blocked for {(skill != null ? skill.SkillName : item != null ? item.itemName : "empty")} (index {buttonIndex})");
            return;
        }
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.3f;
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = originalIcon.sprite;
        dragImage.rectTransform.sizeDelta = originalIcon.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = originalIcon.rectTransform.position;
        Debug.Log($"[SkillButton] Begin drag: {(skill != null ? skill.SkillName : item != null ? item.itemName : "empty")} (index {buttonIndex})");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if ((skill == null && item == null) || canvas == null || dragIcon == null || buttonIndex == 0) return;
        dragIcon.GetComponent<RectTransform>().anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if ((skill == null && item == null) || canvas == null || buttonIndex == 0)
        {
            if (dragIcon != null)
            {
                Destroy(dragIcon);
                dragIcon = null;
            }
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            Debug.Log($"[SkillButton] End drag blocked for {(skill != null ? skill.SkillName : item != null ? item.itemName : "empty")} (index {buttonIndex})");
            return;
        }
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        SkillButton otherButton = eventData.pointerEnter?.GetComponent<SkillButton>();
        if (otherButton != null && otherButton.buttonIndex != 0)
        {
            PlayerUI.Instance.SwapSkillsOrItems(this, otherButton);
        }
        Debug.Log($"[SkillButton] End drag: {(skill != null ? skill.SkillName : item != null ? item.itemName : "empty")} (index {buttonIndex})");
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (buttonIndex == 0)
        {
            Debug.Log($"[SkillButton] Drop blocked on index {buttonIndex}");
            return;
        }
        SkillButton otherButton = eventData.pointerDrag?.GetComponent<SkillButton>();
        InventorySlot inventorySlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (otherButton != null && otherButton != this && otherButton.buttonIndex != 0)
        {
            PlayerUI.Instance.SwapSkillsOrItems(otherButton, this);
        }
        else if (inventorySlot != null && inventorySlot.itemInstance.item != null && inventorySlot.itemInstance.item.canHotbar)
        {
            PlayerUI.Instance.AssignItemToHotbar(inventorySlot.itemInstance.item, this);
        }
    }

    private void OnDestroy()
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}