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
                Debug.Log($"[SkillManager] Getting Warrior skills: {selectedSkills.Count} skills found");
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
        
        // Если список пустой, попробуем загрузить BasicAttackSkill из ресурсов
        if (selectedSkills.Count == 0 && characterClass == CharacterClass.Warrior)
        {
            Debug.LogWarning($"[SkillManager] No {characterClass} skills configured, attempting to load BasicAttackSkill from resources");
            BasicAttackSkill basicAttack = Resources.Load<BasicAttackSkill>("SO-Skills/NewBasicAttackSkill");
            if (basicAttack != null)
            {
                selectedSkills.Add(basicAttack);
                Debug.Log($"[SkillManager] Loaded BasicAttackSkill from resources: {basicAttack.SkillName}");
            }
            else
            {
                Debug.LogError($"[SkillManager] Failed to load BasicAttackSkill from resources");
            }
        }
        
        // Returning skills for class
        return selectedSkills;
    }
    
    /// <summary>
    /// Получает все скиллы для игрока с множественными классами
    /// </summary>
    public List<SkillBase> GetSkillsForPlayer(CharacterStats playerStats)
    {
        List<SkillBase> allSkills = new List<SkillBase>();
        HashSet<string> addedSkillNames = new HashSet<string>(); // Для избежания дубликатов
        
        // Получаем скиллы для всех классов игрока
        foreach (var playerClass in playerStats.playerClasses)
        {
            List<SkillBase> classSkills = GetSkillsForClass(playerClass);
            foreach (var skill in classSkills)
            {
                // Добавляем скилл только если его еще нет
                if (!addedSkillNames.Contains(skill.SkillName))
                {
                    allSkills.Add(skill);
                    addedSkillNames.Add(skill.SkillName);
                }
            }
        }
        
        Debug.Log($"[SkillManager] Getting skills for player with classes [{string.Join(", ", playerStats.playerClasses)}]: {allSkills.Count} unique skills found");
        return allSkills;
    }
}