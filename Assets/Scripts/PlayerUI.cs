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

    [Header("Attributes Panel")]
    public GameObject attributesPanel;
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI agilityText;
    public TextMeshProUGUI spiritText;
    public TextMeshProUGUI constitutionText;
    public TextMeshProUGUI accuracyText;
    public Button strengthButton;
    public Button agilityButton;
    public Button spiritButton;
    public Button constitutionButton;
    public Button accuracyButton;

    private CharacterStats stats;
    private PlayerCore core;

    private void Start()
    {
        stats = GetComponentInParent<CharacterStats>();
        core = GetComponentInParent<PlayerCore>();
        if (stats != null && core != null)
        {
            UpdateLevel(stats.level);
            UpdateExperience(stats.currentExperience, stats.level);
            UpdateManaBar(stats.currentMana, stats.maxMana);
            UpdateSkillPoints(stats.skillPoints);
            UpdateCharacteristicPoints(0, stats.characteristicPoints);
            UpdateAttributesPanel();

            // Подписываемся на события
            Health health = stats.GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthUpdated += UpdateHealthBar;
            }
            stats.OnManaChangedEvent += UpdateManaBar;
            stats.OnLevelChangedEvent += UpdateLevelAndExperience;
            stats.OnCharacteristicPointsChangedEvent += (oldPoints, newPoints) => UpdateCharacteristicPoints(oldPoints, newPoints);
            stats.OnStrengthChangedEvent += (oldValue, newValue) => UpdateAttribute("strength", newValue);
            stats.OnAgilityChangedEvent += (oldValue, newValue) => UpdateAttribute("agility", newValue);
            stats.OnSpiritChangedEvent += (oldValue, newValue) => UpdateAttribute("spirit", newValue);
            stats.OnConstitutionChangedEvent += (oldValue, newValue) => UpdateAttribute("constitution", newValue);
            stats.OnAccuracyChangedEvent += (oldValue, newValue) => UpdateAttribute("accuracy", newValue);

            // Настраиваем кнопки
            if (strengthButton != null)
                strengthButton.onClick.AddListener(() => core.CmdIncreaseStat("strength"));
            if (agilityButton != null)
                agilityButton.onClick.AddListener(() => core.CmdIncreaseStat("agility"));
            if (spiritButton != null)
                spiritButton.onClick.AddListener(() => core.CmdIncreaseStat("spirit"));
            if (constitutionButton != null)
                constitutionButton.onClick.AddListener(() => core.CmdIncreaseStat("constitution"));
            if (accuracyButton != null)
                accuracyButton.onClick.AddListener(() => core.CmdIncreaseStat("accuracy"));
        }
        else
        {
            Debug.LogError("CharacterStats or PlayerCore component not found in parent!");
        }

        // Изначально скрываем панель характеристик
        if (attributesPanel != null)
            attributesPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && core.isLocalPlayer)
        {
            if (attributesPanel != null)
            {
                attributesPanel.SetActive(!attributesPanel.activeSelf);
                Cursor.lockState = attributesPanel.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = attributesPanel.activeSelf;
            }
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
            stats.OnCharacteristicPointsChangedEvent -= (oldPoints, newPoints) => UpdateCharacteristicPoints(oldPoints, newPoints);
            stats.OnStrengthChangedEvent -= (oldValue, newValue) => UpdateAttribute("strength", newValue);
            stats.OnAgilityChangedEvent -= (oldValue, newValue) => UpdateAttribute("agility", newValue);
            stats.OnSpiritChangedEvent -= (oldValue, newValue) => UpdateAttribute("spirit", newValue);
            stats.OnConstitutionChangedEvent -= (oldValue, newValue) => UpdateAttribute("constitution", newValue);
            stats.OnAccuracyChangedEvent -= (oldValue, newValue) => UpdateAttribute("accuracy", newValue);
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

    public void UpdateCharacteristicPoints(int oldPoints, int newPoints)
    {
        if (characteristicPointsText != null)
        {
            characteristicPointsText.text = $"Characteristic Points: {newPoints}";
        }
        UpdateAttributesPanel();
    }

    private void UpdateLevelAndExperience(int oldLevel, int newLevel)
    {
        UpdateLevel(newLevel);
        UpdateExperience(stats.currentExperience, newLevel);
        UpdateSkillPoints(stats.skillPoints);
        UpdateCharacteristicPoints(0, stats.characteristicPoints);
    }

    private void UpdateAttributesPanel()
    {
        if (strengthText != null)
            strengthText.text = $"Strength: {stats.strength}";
        if (agilityText != null)
            agilityText.text = $"Agility: {stats.agility}";
        if (spiritText != null)
            spiritText.text = $"Spirit: {stats.spirit}";
        if (constitutionText != null)
            constitutionText.text = $"Constitution: {stats.constitution}";
        if (accuracyText != null)
            accuracyText.text = $"Accuracy: {stats.accuracy}";

        bool hasPoints = stats.characteristicPoints > 0;
        if (strengthButton != null)
            strengthButton.gameObject.SetActive(hasPoints);
        if (agilityButton != null)
            agilityButton.gameObject.SetActive(hasPoints);
        if (spiritButton != null)
            spiritButton.gameObject.SetActive(hasPoints);
        if (constitutionButton != null)
            constitutionButton.gameObject.SetActive(hasPoints);
        if (accuracyButton != null)
            accuracyButton.gameObject.SetActive(hasPoints);
    }

    private void UpdateAttribute(string statName, int value)
    {
        switch (statName.ToLower())
        {
            case "strength":
                if (strengthText != null) strengthText.text = $"Strength: {value}";
                break;
            case "agility":
                if (agilityText != null) agilityText.text = $"Agility: {value}";
                break;
            case "spirit":
                if (spiritText != null) spiritText.text = $"Spirit: {value}";
                break;
            case "constitution":
                if (constitutionText != null) constitutionText.text = $"Constitution: {value}";
                break;
            case "accuracy":
                if (accuracyText != null) accuracyText.text = $"Accuracy: {value}";
                break;
        }
    }
}