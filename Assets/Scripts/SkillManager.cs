using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private List<SkillBase> warriorSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> mageSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> archerSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> tankSkills = new List<SkillBase>(); // ��������� ������ ��� Tank
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
        // Skills loaded for all classes
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
            case CharacterClass.Tank:
                selectedSkills = tankSkills;
                break;
            case CharacterClass.Monster:
                Debug.LogWarning($"[SkillManager] No skills defined for class {characterClass}");
                return new List<SkillBase>();
            default:
                Debug.LogError($"[SkillManager] No skills defined for class {characterClass}");
                return new List<SkillBase>();
        }
        // Returning skills for class
        return selectedSkills;
    }
}