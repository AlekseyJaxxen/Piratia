using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public SkillBase skill;
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
        buttonIndex = index; // Сохраняем индекс
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        originalIcon = GetComponentInChildren<Image>();
        if (button != null && playerSkills != null && playerCore != null && canvas != null && originalIcon != null)
        {
            button.onClick.AddListener(OnSkillButtonClicked);
        }
        else
        {
            Debug.LogError($"[SkillButton] Initialization failed: Button={button}, PlayerSkills={playerSkills}, PlayerCore={playerCore}, Canvas={canvas}, OriginalIcon={originalIcon}");
        }
    }

    public void OnSkillButtonClicked() // Изменено на public
    {
        if (skill == null) return; // Игнорируем клик по пустой кнопке
        if (playerSkills != null && playerCore != null)
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
        else
        {
            Debug.LogError($"[SkillButton] PlayerSkills={playerSkills}, PlayerCore={playerCore} is null!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skill == null || canvas == null || originalIcon == null || buttonIndex == 0) // Запрет для первого слота
        {
            Debug.Log($"[SkillButton] Drag blocked for {skill?.SkillName} (index {buttonIndex})");
            return;
        }
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.3f; // Слегка прозрачная кнопка
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = originalIcon.sprite;
        dragImage.rectTransform.sizeDelta = originalIcon.rectTransform.sizeDelta;
        dragImage.raycastTarget = false;
        dragImage.rectTransform.position = originalIcon.rectTransform.position;
        Debug.Log($"[SkillButton] Begin drag: {skill.SkillName} (index {buttonIndex})");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (skill == null || canvas == null || dragIcon == null || buttonIndex == 0) return;
        dragIcon.GetComponent<RectTransform>().anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (skill == null || canvas == null || buttonIndex == 0) return;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        Debug.Log($"[SkillButton] End drag: {skill.SkillName} (index {buttonIndex})");
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (buttonIndex == 0) // Запрет для первого слота
        {
            Debug.Log($"[SkillButton] Drop blocked on index {buttonIndex}");
            return;
        }
        SkillButton otherButton = eventData.pointerDrag?.GetComponent<SkillButton>();
        if (otherButton != null && otherButton.skill != null && otherButton != this && otherButton.buttonIndex != 0)
        {
            Debug.Log($"[SkillButton] Dropped {otherButton.skill.SkillName} (index {otherButton.buttonIndex}) onto {(skill != null ? skill.SkillName : "empty")} (index {buttonIndex})");
            PlayerUI.Instance.SwapSkills(otherButton, this);
        }
    }

    private void OnDestroy()
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
    }
}