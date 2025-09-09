using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Collections.Generic;

public class PlayerUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static PlayerUI Instance { get; private set; }
    [Header("UI Elements")]
    public Image healthBar;
    public Image manaBar;
    public TextMeshProUGUI levelText;
    public Slider experienceSlider;
    public TextMeshProUGUI skillPointsText;
    public TextMeshProUGUI characteristicPointsText;
    [SerializeField] Transform skillPanel; // Родитель для кнопок навыков в Canvas.
    [SerializeField] SkillButton[] skillButtons; // Массив кнопок навыков, заданный в инспекторе
    [Header("Attributes Panel")]
    public GameObject attributesPanel;
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI agilityText;
    public TextMeshProUGUI spiritText;
    public TextMeshProUGUI constitutionText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI physicalResistanceText;
    public TextMeshProUGUI magicDamageMultiplierText;
    public TextMeshProUGUI movementSpeedText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI dodgeChanceText;
    public TextMeshProUGUI hitChanceText;
    public TextMeshProUGUI criticalHitChanceText;
    public TextMeshProUGUI criticalHitMultiplierText;
    public TextMeshProUGUI minAttackText;
    public TextMeshProUGUI maxAttackText;
    public Button strengthButton;
    public Button agilityButton;
    public Button spiritButton;
    public Button constitutionButton;
    public Button accuracyButton;
    [Header("Cooldown UI")]
    public Image globalCooldownImage;
    [System.Serializable]
    public class SkillCooldownEntry
    {
        public string skillName;
        public Image cooldownImage;
    }
    [SerializeField] private List<SkillCooldownEntry> skillCooldownEntries = new List<SkillCooldownEntry>();
    private CharacterStats stats;
    private PlayerCore core;
    private RectTransform attributesPanelRect;
    private Vector2 dragOffset;

    private void Start()
    {
        core = GetComponentInParent<PlayerCore>();
        if (core == null)
        {
            Debug.LogError("[PlayerUI] PlayerCore not found!");
            gameObject.SetActive(false);
            return;
        }
        if (!core.isLocalPlayer)
        {
            Debug.Log("[PlayerUI] Not local player, disabling UI.");
            gameObject.SetActive(false);
            return;
        }
        Instance = this;
        stats = core.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError("[PlayerUI] CharacterStats not found!");
            gameObject.SetActive(false);
            return;
        }
        StartCoroutine(InitializeUI());
    }

    private IEnumerator InitializeUI()
    {
        yield return new WaitForSeconds(2f); // Increased delay for network sync
        if (!core.isLocalPlayer || !core.isClient)
        {
            Debug.Log("[PlayerUI] Waiting for client sync...");
            yield return new WaitUntil(() => core.isLocalPlayer && core.isClient);
        }
        if (core.Health != null)
        {
            core.Health.OnHealthUpdated += UpdateHealthBar;
            UpdateHealthBar(core.Health.CurrentHealth, core.Health.MaxHealth);
            Debug.Log($"[PlayerUI] Initial health: {core.Health.CurrentHealth}/{core.Health.MaxHealth}");
        }
        else
        {
            Debug.LogError("[PlayerUI] Health component not found!");
        }
        UpdateLevel(stats.level);
        UpdateExperience(stats.currentExperience, stats.level);
        UpdateManaBar(stats.currentMana, stats.maxMana);
        UpdateSkillPoints(stats.skillPoints);
        UpdateCharacteristicPoints(0, stats.characteristicPoints);
        UpdateAttributesPanel();
        stats.OnManaChangedEvent += UpdateManaBar;
        stats.OnLevelChangedEvent += UpdateLevelAndExperience;
        stats.OnCharacteristicPointsChangedEvent += UpdateCharacteristicPoints;
        stats.OnStrengthChangedEvent += (oldValue, newValue) => UpdateAttribute("strength", newValue);
        stats.OnAgilityChangedEvent += (oldValue, newValue) => UpdateAttribute("agility", newValue);
        stats.OnSpiritChangedEvent += (oldValue, newValue) => UpdateAttribute("spirit", newValue);
        stats.OnConstitutionChangedEvent += (oldValue, newValue) => UpdateAttribute("constitution", newValue);
        stats.OnAccuracyChangedEvent += (oldValue, newValue) => UpdateAttribute("accuracy", newValue);
        stats.OnMinAttackChangedEvent += (oldValue, newValue) => UpdateAttribute("minAttack", newValue);
        stats.OnMaxAttackChangedEvent += (oldValue, newValue) => UpdateAttribute("maxAttack", newValue);
        if (strengthButton != null)
        {
            strengthButton.onClick.AddListener(() => { core.CmdIncreaseStat("strength"); Debug.Log("[PlayerUI] Strength button clicked"); });
        }
        if (agilityButton != null)
        {
            agilityButton.onClick.AddListener(() => { core.CmdIncreaseStat("agility"); Debug.Log("[PlayerUI] Agility button clicked"); });
        }
        if (spiritButton != null)
        {
            spiritButton.onClick.AddListener(() => { core.CmdIncreaseStat("spirit"); Debug.Log("[PlayerUI] Spirit button clicked"); });
        }
        if (constitutionButton != null)
        {
            constitutionButton.onClick.AddListener(() => { core.CmdIncreaseStat("constitution"); Debug.Log("[PlayerUI] Constitution button clicked"); });
        }
        if (accuracyButton != null)
        {
            accuracyButton.onClick.AddListener(() => { core.CmdIncreaseStat("accuracy"); Debug.Log("[PlayerUI] Accuracy button clicked"); });
        }
        if (attributesPanel != null)
        {
            attributesPanelRect = attributesPanel.GetComponent<RectTransform>();
            attributesPanel.SetActive(false);
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Ensure only one EventSystem
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Destroy(eventSystems[i].gameObject);
                Debug.LogWarning("[PlayerUI] Destroyed duplicate EventSystem to prevent input conflicts.");
            }
        }
        PlayerSkills skillsComponent = core.GetComponent<PlayerSkills>();
        if (skillsComponent != null)
        {
            yield return new WaitUntil(() => skillsComponent.skills.Count > 0); // Ждать загрузки skills.
            skillCooldownEntries.Clear();
            for (int i = 0; i < skillButtons.Length && i < skillsComponent.skills.Count; i++)
            {
                SkillBase skill = skillsComponent.skills[i];
                if (skill == null)
                {
                    Debug.LogError($"[PlayerUI] Skill at index {i} is null in skills list for {skillsComponent.gameObject.name}");
                    continue;
                }
                SkillButton btn = skillButtons[i];
                Image iconImage = btn.GetComponentInChildren<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = skill.Icon;
                }
                else
                {
                    Debug.LogError($"[PlayerUI] Icon Image not found in skill button at index {i} for skill {skill.SkillName}");
                }
                Image cdImage = btn.transform.Find("CooldownOverlay")?.GetComponent<Image>();
                if (cdImage != null)
                {
                    skillCooldownEntries.Add(new SkillCooldownEntry { skillName = skill.SkillName, cooldownImage = cdImage });
                }
                else
                {
                    Debug.LogError($"[PlayerUI] CooldownOverlay Image not found for skill {skill.SkillName}");
                }
                btn.Initialize(skillsComponent, core, i); // Передаем индекс
                btn.skill = skill;
                Debug.Log($"[PlayerUI] Skill button initialized for {skill.SkillName} at index {i}");
            }
        }
        else
        {
            Debug.LogError("[PlayerUI] PlayerSkills component not found!");
        }
    }

    private void Update()
    {
        if (!core.isLocalPlayer || core.isDead || core.isStunned) return;
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (attributesPanel != null)
            {
                bool newState = !attributesPanel.activeSelf;
                attributesPanel.SetActive(newState);
                Debug.Log($"[PlayerUI] AttributesPanel set to {newState}. Children: {attributesPanel.transform.childCount}");
                UpdateAttributesPanel();
            }
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (skillPanel != null)
            {
                bool newState = !skillPanel.gameObject.activeSelf;
                skillPanel.gameObject.SetActive(newState);
                Debug.Log($"[PlayerUI] SkillPanel set to {newState}. Children: {skillPanel.transform.childCount}");
            }
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            if (core != null && core.Health != null)
            {
                core.Health.OnHealthUpdated -= UpdateHealthBar;
            }
            stats.OnManaChangedEvent -= UpdateManaBar;
            stats.OnLevelChangedEvent -= UpdateLevelAndExperience;
            stats.OnCharacteristicPointsChangedEvent -= UpdateCharacteristicPoints;
            stats.OnStrengthChangedEvent -= (oldValue, newValue) => UpdateAttribute("strength", newValue);
            stats.OnAgilityChangedEvent -= (oldValue, newValue) => UpdateAttribute("agility", newValue);
            stats.OnSpiritChangedEvent -= (oldValue, newValue) => UpdateAttribute("spirit", newValue);
            stats.OnConstitutionChangedEvent -= (oldValue, newValue) => UpdateAttribute("constitution", newValue);
            stats.OnAccuracyChangedEvent += (oldValue, newValue) => UpdateAttribute("accuracy", newValue);
            stats.OnMinAttackChangedEvent -= (oldValue, newValue) => UpdateAttribute("minAttack", newValue);
            stats.OnMaxAttackChangedEvent -= (oldValue, newValue) => UpdateAttribute("maxAttack", newValue);
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (!core.isLocalPlayer) return;
        if (healthBar == null)
        {
            Debug.LogError("[PlayerUI] healthBar is null! Ensure it is assigned in the Inspector.");
            return;
        }
        float fillAmount = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        healthBar.fillAmount = fillAmount;
        // healthBar.color = Color.Lerp(Color.red, Color.green, fillAmount);
        Debug.Log($"[PlayerUI] Health bar updated: {currentHealth}/{maxHealth}, fillAmount={fillAmount}");
    }

    public void UpdateManaBar(int currentMana, int maxMana)
    {
        if (!core.isLocalPlayer) return;
        if (manaBar == null)
        {
            Debug.LogError("[PlayerUI] manaBar is null! Ensure it is assigned in the Inspector.");
            return;
        }
        float fillAmount = maxMana > 0 ? (float)currentMana / maxMana : 0f;
        manaBar.fillAmount = fillAmount;
        manaBar.color = Color.Lerp(new Color(0, 0, 0.5f), Color.blue, fillAmount);
        Debug.Log($"[PlayerUI] Mana bar updated: {currentMana}/{maxMana}, fillAmount={fillAmount}");
    }

    public void UpdateLevel(int level)
    {
        if (!core.isLocalPlayer) return;
        if (levelText != null)
        {
            levelText.text = $"{level}";
        }
    }

    public void UpdateExperience(int currentExperience, int level)
    {
        if (!core.isLocalPlayer) return;
        if (experienceSlider != null && level <= 100)
        {
            int expNeeded = 10 + ((level - 1) * (level - 1) * 5);
            experienceSlider.maxValue = expNeeded;
            experienceSlider.value = currentExperience;
        }
    }

    public void UpdateSkillPoints(int skillPoints)
    {
        if (!core.isLocalPlayer) return;
        if (skillPointsText != null)
        {
            skillPointsText.text = $"{skillPoints}";
        }
    }

    public void UpdateCharacteristicPoints(int oldPoints, int newPoints)
    {
        if (!core.isLocalPlayer) return;
        if (characteristicPointsText != null)
        {
            characteristicPointsText.text = $"{newPoints}";
        }
        UpdateAttributesPanel();
    }

    private void UpdateLevelAndExperience(int oldLevel, int newLevel)
    {
        if (!core.isLocalPlayer) return;
        UpdateLevel(newLevel);
        UpdateExperience(stats.currentExperience, newLevel);
        UpdateSkillPoints(stats.skillPoints);
        UpdateCharacteristicPoints(0, stats.characteristicPoints);
    }

    private void UpdateAttributesPanel()
    {
        if (!core.isLocalPlayer || stats == null) return;
        if (strengthText != null)
            strengthText.text = $"{stats.strength}";
        if (agilityText != null)
            agilityText.text = $"{stats.agility}";
        if (spiritText != null)
            spiritText.text = $"{stats.spirit}";
        if (constitutionText != null)
            constitutionText.text = $"{stats.constitution}";
        if (accuracyText != null)
            accuracyText.text = $"{stats.accuracy}";
        if (armorText != null)
            armorText.text = $"{stats.armor}";
        if (physicalResistanceText != null)
            physicalResistanceText.text = $"{stats.physicalResistance:F1}%";
        if (magicDamageMultiplierText != null)
            magicDamageMultiplierText.text = $"{stats.magicDamageMultiplier:F2}x";
        if (movementSpeedText != null)
            movementSpeedText.text = $"{stats.movementSpeed:F1}";
        if (attackSpeedText != null)
            attackSpeedText.text = $"{stats.attackSpeed:F2}";
        if (dodgeChanceText != null)
            dodgeChanceText.text = $"{stats.dodgeChance:F1}%";
        if (hitChanceText != null)
            hitChanceText.text = $"{stats.hitChance:F1}%";
        if (criticalHitChanceText != null)
            criticalHitChanceText.text = $"{stats.criticalHitChance:F1}%";
        if (criticalHitMultiplierText != null)
            criticalHitMultiplierText.text = $"{stats.criticalHitMultiplier:F2}x";
        if (minAttackText != null)
            minAttackText.text = $"{stats.minAttack}";
        if (maxAttackText != null)
            maxAttackText.text = $"{stats.maxAttack}";
        bool hasPoints = stats.characteristicPoints > 0;
        if (strengthButton != null)
        {
            strengthButton.gameObject.SetActive(hasPoints);
            Debug.Log($"[PlayerUI] StrengthButton active: {hasPoints}");
        }
        if (agilityButton != null)
        {
            agilityButton.gameObject.SetActive(hasPoints);
            Debug.Log($"[PlayerUI] AgilityButton active: {hasPoints}");
        }
        if (spiritButton != null)
        {
            spiritButton.gameObject.SetActive(hasPoints);
            Debug.Log($"[PlayerUI] SpiritButton active: {hasPoints}");
        }
        if (constitutionButton != null)
        {
            constitutionButton.gameObject.SetActive(hasPoints);
            Debug.Log($"[PlayerUI] ConstitutionButton active: {hasPoints}");
        }
        if (accuracyButton != null)
        {
            accuracyButton.gameObject.SetActive(hasPoints);
            Debug.Log($"[PlayerUI] AccuracyButton active: {hasPoints}");
        }
    }

    private void UpdateAttribute(string statName, int value)
    {
        if (!core.isLocalPlayer) return;
        switch (statName.ToLower())
        {
            case "strength":
                if (strengthText != null) strengthText.text = $"{value}";
                break;
            case "agility":
                if (agilityText != null) agilityText.text = $"{value}";
                break;
            case "spirit":
                if (spiritText != null) spiritText.text = $"{value}";
                break;
            case "constitution":
                if (constitutionText != null) constitutionText.text = $"{value}";
                break;
            case "accuracy":
                if (accuracyText != null) accuracyText.text = $"{value}";
                break;
            case "minattack":
                if (minAttackText != null) minAttackText.text = $"{value}";
                break;
            case "maxattack":
                if (maxAttackText != null) maxAttackText.text = $"{value}";
                break;
        }
    }

    public void UpdateSkillCooldown(string skillName, float progress)
    {
        var entry = skillCooldownEntries.Find(e => e.skillName == skillName);
        if (entry != null && entry.cooldownImage != null)
        {
            entry.cooldownImage.fillAmount = progress;
            Image skillIcon = entry.cooldownImage.GetComponentInParent<Image>();
            if (skillIcon != null)
            {
                skillIcon.color = progress > 0 ? Color.gray : Color.white;
            }
        }
    }

    public void UpdateGlobalCooldown(float progress)
    {
        if (globalCooldownImage != null)
        {
            globalCooldownImage.fillAmount = 1f - progress;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (attributesPanelRect != null)
        {
            dragOffset = attributesPanelRect.position - (Vector3)eventData.position;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (attributesPanelRect != null)
        {
            attributesPanelRect.position = eventData.position + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Nothing needed here
    }

    public void SwapSkills(SkillButton firstButton, SkillButton secondButton)
    {
        if (firstButton == null || secondButton == null || firstButton.skill == null || secondButton.skill == null)
        {
            Debug.LogError("[PlayerUI] Cannot swap skills: one of the buttons or skills is null!");
            return;
        }
        // Обмен иконок
        Image firstIcon = firstButton.GetComponentInChildren<Image>();
        Image secondIcon = secondButton.GetComponentInChildren<Image>();
        if (firstIcon != null && secondIcon != null)
        {
            Sprite tempSprite = firstIcon.sprite;
            firstIcon.sprite = secondIcon.sprite;
            secondIcon.sprite = tempSprite;
        }
        else
        {
            Debug.LogError("[PlayerUI] Icon Image not found on one of the buttons!");
        }
        // Обмен в skillCooldownEntries
        var firstEntry = skillCooldownEntries.Find(e => e.skillName == firstButton.skill.SkillName);
        var secondEntry = skillCooldownEntries.Find(e => e.skillName == secondButton.skill.SkillName);
        if (firstEntry != null && secondEntry != null)
        {
            string tempName = firstEntry.skillName;
            firstEntry.skillName = secondEntry.skillName;
            secondEntry.skillName = tempName;
            Debug.Log($"[PlayerUI] Swapped cooldown entries for {firstEntry.skillName} and {secondEntry.skillName}");
        }
        else
        {
            Debug.LogError("[PlayerUI] Could not find cooldown entries for swap!");
        }
        // Обмен SkillBase в кнопках
        SkillBase tempSkill = firstButton.skill;
        firstButton.skill = secondButton.skill;
        secondButton.skill = tempSkill;
        // Обмен Hotkey в SkillBase
        KeyCode tempHotkey = firstButton.skill.Hotkey;
        firstButton.skill.Hotkey = secondButton.skill.Hotkey;
        secondButton.skill.Hotkey = tempHotkey;
        // Обмен в списке skills в PlayerSkills
        PlayerSkills skillsComponent = core.GetComponent<PlayerSkills>();
        if (skillsComponent != null)
        {
            int firstIndex = skillsComponent.skills.IndexOf(secondButton.skill); // secondButton.skill после обмена содержит tempSkill
            int secondIndex = skillsComponent.skills.IndexOf(firstButton.skill);
            if (firstIndex != -1 && secondIndex != -1)
            {
                skillsComponent.skills[firstIndex] = firstButton.skill;
                skillsComponent.skills[secondIndex] = secondButton.skill;
                Debug.Log($"[PlayerUI] Swapped skills in list: {firstButton.skill.SkillName} (index {firstIndex}) <-> {secondButton.skill.SkillName} (index {secondIndex})");
                Debug.Log($"[PlayerUI] Swapped hotkeys: {firstButton.skill.SkillName} ({firstButton.skill.Hotkey}) <-> {secondButton.skill.SkillName} ({secondButton.skill.Hotkey})");
            }
            else
            {
                Debug.LogError($"[PlayerUI] Failed to swap skills in list: indices not found (firstIndex={firstIndex}, secondIndex={secondIndex})");
            }
        }
        else
        {
            Debug.LogError("[PlayerUI] PlayerSkills component not found!");
        }
    }
}