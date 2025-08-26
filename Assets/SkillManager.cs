using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private List<SkillBase> warriorSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> mageSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> archerSkills = new List<SkillBase>();

    public static SkillManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Debug.Log($"[SkillManager] Warrior skills: {string.Join(", ", warriorSkills.Select(s => s != null ? s.SkillName : "null"))}");
        Debug.Log($"[SkillManager] Mage skills: {string.Join(", ", mageSkills.Select(s => s != null ? s.SkillName : "null"))}");
        Debug.Log($"[SkillManager] Archer skills: {string.Join(", ", archerSkills.Select(s => s != null ? s.SkillName : "null"))}");
    }

    public List<SkillBase> GetSkillsForClass(CharacterClass characterClass)
    {
        List<SkillBase> selectedSkills = new List<SkillBase>();
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                selectedSkills = warriorSkills;
                break;
            case CharacterClass.Mage:
                selectedSkills = mageSkills;
                break;
            case CharacterClass.Archer:
                selectedSkills = archerSkills;
                break;
            default:
                Debug.LogError($"[SkillManager] No skills defined for class {characterClass}");
                return new List<SkillBase>();
        }
        Debug.Log($"[SkillManager] Returning {selectedSkills.Count} skills for class {characterClass}: {string.Join(", ", selectedSkills.Select(s => s != null ? s.SkillName : "null"))}");
        return selectedSkills;
    }
}