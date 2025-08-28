using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour, IDropHandler
{
    public KeyCode hotkey;
    private SkillBase assignedSkill;
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            SkillDragHandler dragHandler = eventData.pointerDrag.GetComponent<SkillDragHandler>();
            if (dragHandler != null)
            {
                // Assume dragged object has SkillButton component with SkillBase reference
                SkillButton skillButton = eventData.pointerDrag.GetComponent<SkillButton>();
                if (skillButton != null && skillButton.skill != null)
                {
                    AssignSkill(skillButton.skill);
                }
            }
        }
    }
    public void AssignSkill(SkillBase skill)
    {
        assignedSkill = skill;
        skill.Hotkey = hotkey; // Ensure Hotkey is settable in SkillBase
        GetComponent<Image>().sprite = skill.Icon;
    }
}