using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;
using static SkillBase;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int buttonIndex;
    public SkillBase skill;
    public Item item;
    public int itemSlotIndex = -1;
    private PlayerSkills skillsComponent;
    private PlayerCore core;
    private InventoryUI inventoryUI;
    private Image iconImage;
    private GameObject dragIcon;
    private Coroutine tooltipCoroutine;
    [SerializeField] private GameObject buffIndicator;

    private void Awake()
    {
        inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
        iconImage = GetComponentInChildren<Image>();
        buffIndicator = transform.Find("BuffIndicator")?.gameObject;
        if (buffIndicator == null)
        {
            Debug.LogWarning($"[SkillButton] BuffIndicator not found for button {buttonIndex}");
        }
    }

    public void Initialize(PlayerSkills skills, PlayerCore playerCore, int index)
    {
        skillsComponent = skills;
        core = playerCore;
        buttonIndex = index;
        itemSlotIndex = -1;
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        if (skills != null && skillsComponent != null)
        {
            skillsComponent.OnToggleBuffChanged.AddListener(UpdateBuffIndicator);
            if (skill != null && skill.SkillCastType == CastType.ToggleBuff)
            {
                bool isActive = skillsComponent.toggleBuffStates.ContainsKey(skill.SkillName) && skillsComponent.toggleBuffStates[skill.SkillName];
                UpdateBuffIndicator(skill.SkillName, isActive);
            }
        }
    }

    private void OnDestroy()
    {
        if (skillsComponent != null)
        {
            skillsComponent.OnToggleBuffChanged.RemoveListener(UpdateBuffIndicator);
        }
    }

    private void Update()
    {
        if (skillsComponent != null && skill != null && skill.SkillCastType == CastType.ToggleBuff)
        {
            bool isActive = skillsComponent.toggleBuffStates.ContainsKey(skill.SkillName) && skillsComponent.toggleBuffStates[skill.SkillName];
            if (buffIndicator != null && buffIndicator.activeSelf != isActive)
            {
                UpdateBuffIndicator(skill.SkillName, isActive);
                Debug.Log($"[SkillButton] Client-side UpdateBuffIndicator: {skill.SkillName} set to {isActive} for button {buttonIndex}");
            }
        }
    }

    public void OnButtonClicked()
    {
        if (core == null)
        {
            Debug.LogError("[SkillButton] PlayerCore is null!");
            return;
        }
        if (item != null)
        {
            if (core.isDead || core.isStunned)
            {
                Debug.Log($"[SkillButton] Cannot use item {item.itemName}: Player is dead or stunned");
                return;
            }
            core.CmdSelectItem(item.id, itemSlotIndex);
            Debug.Log($"[SkillButton] Item used: {item.itemName} (ID: {item.id}), slot: {itemSlotIndex}, index: {buttonIndex}");
        }
        else if (skill != null)
        {
            if (core.isDead || core.isStunned || (core.isSilenced && !(skill is BasicAttackSkill)))
            {
                Debug.Log($"[SkillButton] Cannot select skill {skill.SkillName}: Player is dead, stunned, or silenced");
                return;
            }
            if (skillsComponent != null)
            {
                if (skill.SkillCastType == SkillBase.CastType.SelfBuff)
                {
                    // SelfBuff скиллы теперь идут через PlayerActionSystem для правильного прерывания действий
                    core.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, core.gameObject, skill);
                    Debug.Log($"[SkillButton] SelfBuff through ActionSystem: {skill.SkillName}, index: {buttonIndex}");
                }
                else if (skill.SkillCastType == SkillBase.CastType.ToggleBuff)
                {
                    bool isActive = skillsComponent.toggleBuffStates.ContainsKey(skill.SkillName) && skillsComponent.toggleBuffStates[skill.SkillName];
                    bool targetState = !isActive;
                    if (isActive == targetState)
                    {
                        Debug.Log($"[SkillButton] ToggleBuff {skill.SkillName} already in state {isActive}, skipping CmdToggleBuff, index: {buttonIndex}");
                        return;
                    }
                    // Проверяем кулдаун для активации ToggleBuff скиллов
                    if (targetState && skillsComponent.GetRemainingCooldown(skill.SkillName) > 0)
                    {
                        Debug.LogWarning($"[SkillButton] Cannot activate ToggleBuff {skill.SkillName}: on cooldown ({skillsComponent.GetRemainingCooldown(skill.SkillName):F2}s remaining), index: {buttonIndex}");
                        return;
                    }
                    
                    if (!targetState && skillsComponent.GetRemainingCooldown(skill.SkillName) > 0)
                    {
                        Debug.Log($"[SkillButton] Deactivating {skill.SkillName} during cooldown, index: {buttonIndex}");
                    }
                    core.Skills.CmdToggleBuff(skill.SkillName, targetState);
                    // ������: ��������� ��� layer
                    int targetLayer = targetState ? LayerMask.NameToLayer("Ignore Raycast") : skillsComponent._originalLayer;
                    core.gameObject.layer = targetLayer;
                    Debug.Log($"[SkillButton] Local layer set to {targetLayer} for {skill.SkillName}");
                }
                else
                {
                    skillsComponent.SelectSkill(skill);
                    Debug.Log($"[SkillButton] Skill {skill.SkillName} selected, index: {buttonIndex}");
                }
            }
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
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        tooltipCoroutine = StartCoroutine(DelayedHideTooltip());
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
                    PlayerUI.Instance.AssignItemToHotbar(targetItem, this, targetSlot.slotIndex);
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
                itemSlotIndex = -1;
                iconImage.sprite = PlayerUI.Instance.GetDefaultEmptySprite();
                Debug.Log($"[SkillButton] Dropped item from hotbar button {buttonIndex}");
            }
        }
        else
        {
            PlayerUI.Instance.SwapSkillsOrItems(this, null);
            Debug.Log($"[SkillButton] Cleared hotbar button {buttonIndex} (no target)");
        }
        Destroy(dragIcon);
    }

    private string GetComponentsOnPointerEnter(GameObject go)
    {
        if (go == null) return "null";
        var components = go.GetComponents<Component>();
        return string.Join(", ", Enumerable.Select(components, c => c.GetType().Name));
    }

    private IEnumerator DelayedHideTooltip()
    {
        yield return new WaitForSeconds(0.2f);
        if (inventoryUI != null)
            inventoryUI.HideTooltip();
        tooltipCoroutine = null;
    }

    public void UpdateBuffIndicator(string skillName, bool isActive)
    {
        if (buffIndicator != null && skill != null && skill.SkillCastType == CastType.ToggleBuff && skill.SkillName == skillName)
        {
            buffIndicator.SetActive(isActive);
            Debug.Log($"[SkillButton] UpdateBuffIndicator: {skill.SkillName} set to {isActive} on button {buttonIndex}, layer={core.gameObject.layer}");
        }
    }
}