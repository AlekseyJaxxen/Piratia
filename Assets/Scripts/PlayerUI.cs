using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider healthSlider;
    public Slider manaSlider;
    public TextMeshProUGUI levelText;
    public Slider experienceSlider;
    public TextMeshProUGUI skillPointsText;
    public TextMeshProUGUI characteristicPointsText;

    private CharacterStats stats;

    private void Start()
    {
        stats = GetComponentInParent<CharacterStats>();
        if (stats != null)
        {
            UpdateLevel(stats.level);
            UpdateExperience(stats.currentExperience, stats.level);
            UpdateManaBar(stats.currentMana, stats.maxMana);
            UpdateSkillPoints(stats.skillPoints);
            UpdateCharacteristicPoints(stats.characteristicPoints);

            // Подписываемся на события
            Health health = stats.GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthUpdated += UpdateHealthBar;
            }
            stats.OnManaChangedEvent += UpdateManaBar;
            stats.OnLevelChangedEvent += UpdateLevelAndExperience;
        }
        else
        {
            Debug.LogError("CharacterStats component not found in parent!");
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            Health health = stats.GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthUpdated -= UpdateHealthBar;
            }
            stats.OnManaChangedEvent -= UpdateManaBar;
            stats.OnLevelChangedEvent -= UpdateLevelAndExperience;
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void UpdateManaBar(int currentMana, int maxMana)
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = maxMana;
            manaSlider.value = currentMana;
        }
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Level: {level}";
        }
    }

    public void UpdateExperience(int currentExperience, int level)
    {
        if (experienceSlider != null && level <= 100)
        {
            int expNeeded = 10 + ((level - 1) * (level - 1) * 5);
            experienceSlider.maxValue = expNeeded;
            experienceSlider.value = currentExperience;
        }
    }

    public void UpdateSkillPoints(int skillPoints)
    {
        if (skillPointsText != null)
        {
            skillPointsText.text = $"Skill Points: {skillPoints}";
        }
    }

    public void UpdateCharacteristicPoints(int characteristicPoints)
    {
        if (characteristicPointsText != null)
        {
            characteristicPointsText.text = $"Characteristic Points: {characteristicPoints}";
        }
    }

    private void UpdateLevelAndExperience(int oldLevel, int newLevel)
    {
        UpdateLevel(newLevel);
        UpdateExperience(stats.currentExperience, newLevel);
        UpdateSkillPoints(stats.skillPoints);
        UpdateCharacteristicPoints(stats.characteristicPoints);
    }
}