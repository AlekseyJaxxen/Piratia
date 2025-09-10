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
    [SerializeField] SkillButton[] skillButtons1; // Книга заклинаний, без хоткеев
    [SerializeField] SkillButton[] skillButtons2; // Пустой массив, 1-12
    [SerializeField] SkillButton[] skillButtons3; // Пустой массив, Q-W-E-R и т.д.
    [SerializeField] Sprite defaultEmptySprite; // Дефолтный спрайт для пустых слотов
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
    private readonly KeyCode[] hotkeys1 = { KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None };
    private readonly KeyCode[] hotkeys2 = { KeyCode.None, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };
    private readonly KeyCode[] hotkeys3 = { KeyCode.None, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket };

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
        yield return new WaitForSeconds(2f); // Задержка для сетевой синхронизации
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
        // Убедимся, что есть только один EventSystem
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
            yield return new WaitUntil(() => skillsComponent.skills.Count > 0); // Ждать загрузки skills
            skillCooldownEntries.Clear();
            // Заполнение skillButtons1 (книга заклинаний, без хоткеев)
            for (int i = 0; i < skillButtons1.Length && i < skillsComponent.skills.Count; i++)
            {
                SkillButton btn = skillButtons1[i];
                SkillBase originalSkill = skillsComponent.skills[i];
                if (originalSkill == null)
                {
                    Debug.LogError($"[PlayerUI] Skill at index {i} is null in skills list for {skillsComponent.gameObject.name}");
                    continue;
                }
                // Создаём копию навыка для книги заклинаний
                SkillBase skillCopy = Instantiate(originalSkill);
                skillCopy.Init(core); // Инициализируем копию
                btn.skill = skillCopy;
                Image iconImage = btn.GetComponentInChildren<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = skillCopy.Icon;
                }
                else
                {
                    Debug.LogError($"[PlayerUI] Icon Image not found in skill button at index {i} for skill {skillCopy.SkillName}");
                }
                Image cdImage = btn.transform.Find("CooldownOverlay")?.GetComponent<Image>();
                if (cdImage != null)
                {
                    skillCooldownEntries.Add(new SkillCooldownEntry { skillName = skillCopy.SkillName, cooldownImage = cdImage });
                }
                else
                {
                    Debug.LogError($"[PlayerUI] CooldownOverlay Image not found for skill {skillCopy.SkillName}");
                }
                btn.Initialize(skillsComponent, core, i);
                skillCopy.Hotkey = KeyCode.None; // Без хоткея для книги заклинаний
                Debug.Log($"[PlayerUI] Skill button1 initialized for {skillCopy.SkillName} at index {i}, no hotkey");
            }
            // Инициализация пустых skillButtons2 (1-12)
            for (int i = 0; i < skillButtons2.Length; i++)
            {
                SkillButton btn = skillButtons2[i];
                Image iconImage = btn.GetComponentInChildren<Image>();
                if (iconImage != null) iconImage.sprite = defaultEmptySprite; // Пустая иконка
                Image cdImage = btn.transform.Find("CooldownOverlay")?.GetComponent<Image>();
                if (cdImage != null)
                {
                    skillCooldownEntries.Add(new SkillCooldownEntry { skillName = "", cooldownImage = cdImage });
                }
                btn.Initialize(skillsComponent, core, i);
                btn.skill = null;
                Debug.Log($"[PlayerUI] Empty skill button2 at index {i} initialized, hotkey {hotkeys2[i]}");
            }
            // Инициализация пустых skillButtons3 (Q-W-E-R и т.д.)
            for (int i = 0; i < skillButtons3.Length; i++)
            {
                SkillButton btn = skillButtons3[i];
                Image iconImage = btn.GetComponentInChildren<Image>();
                if (iconImage != null) iconImage.sprite = defaultEmptySprite;
                Image cdImage = btn.transform.Find("CooldownOverlay")?.GetComponent<Image>();
                if (cdImage != null)
                {
                    skillCooldownEntries.Add(new SkillCooldownEntry { skillName = "", cooldownImage = cdImage });
                }
                btn.Initialize(skillsComponent, core, i);
                btn.skill = null;
                Debug.Log($"[PlayerUI] Empty skill button3 at index {i} initialized, hotkey {hotkeys3[i]}");
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
        // Активация хоткеев для panel2 и panel3
        foreach (var btn in skillButtons2)
        {
            if (btn.skill != null && Input.GetKeyDown(btn.skill.Hotkey))
            {
                btn.OnSkillButtonClicked();
            }
        }
        foreach (var btn in skillButtons3)
        {
            if (btn.skill != null && Input.GetKeyDown(btn.skill.Hotkey))
            {
                btn.OnSkillButtonClicked();
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
            stats.OnAccuracyChangedEvent -= (oldValue, newValue) => UpdateAttribute("accuracy", newValue);
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
        var entries = skillCooldownEntries.FindAll(e => e.skillName == skillName);
        foreach (var entry in entries)
        {
            if (entry.cooldownImage != null)
            {
                entry.cooldownImage.fillAmount = progress;
                Image skillIcon = entry.cooldownImage.GetComponentInParent<Image>();
                if (skillIcon != null)
                {
                    skillIcon.color = progress > 0 ? Color.gray : Color.white;
                }
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
        // Ничего не требуется
    }

    public void SwapSkills(SkillButton firstButton, SkillButton secondButton)
    {
        if (firstButton == null || secondButton == null || firstButton.skill == null || firstButton.buttonIndex == 0 || secondButton.buttonIndex == 0)
        {
            Debug.LogError($"[PlayerUI] Cannot swap/assign skills: firstButton={firstButton}, secondButton={secondButton}, firstSkill={(firstButton?.skill?.SkillName)}, firstIndex={firstButton?.buttonIndex}, secondIndex={secondButton?.buttonIndex}");
            return;
        }

        KeyCode firstHotkey = GetHotkeyForButton(firstButton);
        KeyCode secondHotkey = GetHotkeyForButton(secondButton);
        Image firstIcon = firstButton.GetComponentInChildren<Image>();
        Image secondIcon = secondButton.GetComponentInChildren<Image>();
        if (firstIcon == null || secondIcon == null)
        {
            Debug.LogError("[PlayerUI] Icon Image not found on one of the buttons!");
            return;
        }

        bool isFirstInSpellBook = Array.IndexOf(skillButtons1, firstButton) != -1;
        bool isSecondInSpellBook = Array.IndexOf(skillButtons1, secondButton) != -1;
        bool isSamePanel = (Array.IndexOf(skillButtons2, firstButton) != -1 && Array.IndexOf(skillButtons2, secondButton) != -1) ||
                           (Array.IndexOf(skillButtons3, firstButton) != -1 && Array.IndexOf(skillButtons3, secondButton) != -1);

        PlayerSkills skillsComponent = core.GetComponent<PlayerSkills>();

        if (isFirstInSpellBook && isSecondInSpellBook)
        {
            // Перетаскивание внутри книги заклинаний (panel1) запрещено
            Debug.Log("[PlayerUI] Drag inside spell book (panel1) ignored");
            return;
        }
        else if (isSecondInSpellBook)
        {
            // Перетаскивание в книгу заклинаний (panel1) запрещено
            Debug.Log("[PlayerUI] Drag to spell book (panel1) ignored");
            return;
        }
        else if (isFirstInSpellBook)
        {
            // Копирование из книги заклинаний (panel1) в panel2 или panel3
            SkillBase skillCopy = Instantiate(firstButton.skill);
            skillCopy.Init(core);
            if (secondButton.skill != null)
            {
                Destroy(secondButton.skill);
            }
            secondButton.skill = skillCopy;
            secondIcon.sprite = firstIcon.sprite;
            var secondEntry = skillCooldownEntries.Find(e => e.cooldownImage == secondButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            if (secondEntry != null) secondEntry.skillName = skillCopy.SkillName;
            if (skillsComponent != null)
            {
                skillsComponent.CmdSetHotkey(skillCopy.SkillName, secondHotkey);
            }
            Debug.Log($"[PlayerUI] Copied skill {skillCopy.SkillName} from spell book (index {firstButton.buttonIndex}) to slot (index {secondButton.buttonIndex}, hotkey {secondHotkey})");
        }
        else if (isSamePanel)
        {
            // Обмен внутри panel2 или panel3
            SkillBase tempSkill = firstButton.skill;
            Sprite tempSprite = firstIcon.sprite;
            firstButton.skill = secondButton.skill;
            firstIcon.sprite = secondButton.skill != null ? secondIcon.sprite : defaultEmptySprite;
            secondButton.skill = tempSkill;
            secondIcon.sprite = tempSkill != null ? tempSprite : defaultEmptySprite;

            // Обновление skillCooldownEntries
            var firstEntry = skillCooldownEntries.Find(e => e.cooldownImage == firstButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            var secondEntry = skillCooldownEntries.Find(e => e.cooldownImage == secondButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            if (firstEntry != null) firstEntry.skillName = firstButton.skill != null ? firstButton.skill.SkillName : "";
            if (secondEntry != null) secondEntry.skillName = secondButton.skill != null ? secondButton.skill.SkillName : "";

            if (skillsComponent != null)
            {
                if (firstButton.skill != null)
                    skillsComponent.CmdSetHotkey(firstButton.skill.SkillName, firstHotkey);
                if (secondButton.skill != null)
                    skillsComponent.CmdSetHotkey(secondButton.skill.SkillName, secondHotkey);
            }
            Debug.Log($"[PlayerUI] Swapped skills inside panel: {(firstButton.skill != null ? firstButton.skill.SkillName : "empty")} (hotkey {firstHotkey}, index {firstButton.buttonIndex}) <-> {(secondButton.skill != null ? secondButton.skill.SkillName : "empty")} (hotkey {secondHotkey}, index {secondButton.buttonIndex})");
        }
        else
        {
            // Перетаскивание между panel2 и panel3
            SkillBase skillCopy = Instantiate(firstButton.skill);
            skillCopy.Init(core);
            if (secondButton.skill != null)
            {
                Destroy(secondButton.skill);
            }
            secondButton.skill = skillCopy;
            secondIcon.sprite = firstIcon.sprite;
            firstIcon.sprite = defaultEmptySprite;
            firstButton.skill = null;

            var firstEntry = skillCooldownEntries.Find(e => e.cooldownImage == firstButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            var secondEntry = skillCooldownEntries.Find(e => e.cooldownImage == secondButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            if (firstEntry != null) firstEntry.skillName = "";
            if (secondEntry != null) secondEntry.skillName = skillCopy.SkillName;

            if (skillsComponent != null)
            {
                skillsComponent.CmdSetHotkey(skillCopy.SkillName, secondHotkey);
            }
            Debug.Log($"[PlayerUI] Moved skill {skillCopy.SkillName} from index {firstButton.buttonIndex} to index {secondButton.buttonIndex}, hotkey {secondHotkey}");
        }
    }

    private KeyCode GetHotkeyForButton(SkillButton button)
    {
        int index = Array.IndexOf(skillButtons1, button);
        if (index != -1) return hotkeys1[index];
        index = Array.IndexOf(skillButtons2, button);
        if (index != -1) return hotkeys2[index];
        index = Array.IndexOf(skillButtons3, button);
        if (index != -1) return hotkeys3[index];
        return KeyCode.None;
    }
}