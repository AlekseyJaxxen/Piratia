using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour, IDropHandler
{
    public KeyCode hotkey;
    private SkillBase assignedSkill;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop called on " + gameObject.name);
        if (eventData.pointerDrag != null)
        {
            Debug.Log("pointerDrag: " + eventData.pointerDrag.name);
            SkillDragHandler dragHandler = eventData.pointerDrag.GetComponent<SkillDragHandler>();
            if (dragHandler != null)
            {
                Debug.Log("DragHandler found");
                SkillButton skillButton = eventData.pointerDrag.GetComponent<SkillButton>();
                if (skillButton != null && skillButton.skill != null)
                {
                    Debug.Log("Assigning skill: " + skillButton.skill.SkillName);
                    AssignSkill(skillButton.skill);
                }
                else
                {
                    Debug.Log("No SkillButton or skill null");
                }
            }
            else
            {
                Debug.Log("No DragHandler");
            }
        }
    }

    public void AssignSkill(SkillBase skill)
    {
        assignedSkill = skill;
        Debug.Log("Assigning " + skill.SkillName + " to hotkey " + hotkey);
        skill.Hotkey = hotkey; // Ensure Hotkey is settable in SkillBase
        GetComponent<Image>().sprite = skill.Icon;
    }
}