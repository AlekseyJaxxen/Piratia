using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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

    [SerializeField] Transform skillPanel;
    [SerializeField] private SkillButton[] skillButtons1;
    [SerializeField] private SkillButton[] skillButtons2;
    [SerializeField] private SkillButton[] skillButtons3;
    [SerializeField] private Sprite defaultEmptySprite;
    [SerializeField] private Button closeButton;

    [Header("Attributes Panel")]
    public GameObject attributesPanel;
    [SerializeField] private Button closeAttributesButton;
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

    public Sprite GetDefaultEmptySprite() => defaultEmptySprite;

    public SkillButton[] GetSkillButtons2() => skillButtons2;

    public SkillButton[] GetSkillButtons3() => skillButtons3;

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
        yield return new WaitForSeconds(2f);

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

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                if (skillPanel != null)
                {
                    skillPanel.gameObject.SetActive(false);
                    Debug.Log("[PlayerUI] Close button clicked, skillPanel hidden");
                }
            });
        }
        else
        {
            Debug.LogWarning("[PlayerUI] CloseButton not assigned in Inspector!");
        }

        if (closeAttributesButton != null)
        {
            closeAttributesButton.onClick.AddListener(() =>
            {
                if (attributesPanel != null)
                {
                    attributesPanel.SetActive(false);
                    Debug.Log("[PlayerUI] CloseAttributesButton clicked, attributesPanel hidden");
                }
            });
        }
        else
        {
            Debug.LogWarning("[PlayerUI] CloseAttributesButton not assigned in Inspector!");
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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
            yield return new WaitUntil(() => skillsComponent.skills.Count > 0);

            skillCooldownEntries.Clear();

            for (int i = 0; i < skillButtons1.Length && i < skillsComponent.skills.Count; i++)
            {
                SkillButton btn = skillButtons1[i];
                SkillBase originalSkill = skillsComponent.skills[i];
                if (originalSkill == null)
                {
                    Debug.LogError($"[PlayerUI] Skill at index {i} is null in skills list for {skillsComponent.gameObject.name}");
                    continue;
                }

                SkillBase skillCopy = Instantiate(originalSkill);
                skillCopy.Init(core);
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
                skillCopy.Hotkey = KeyCode.None;
                Debug.Log($"[PlayerUI] Skill button1 initialized for {skillCopy.SkillName} at index {i}, no hotkey");
            }

            for (int i = 0; i < skillButtons2.Length; i++)
            {
                SkillButton btn = skillButtons2[i];
                Image iconImage = btn.GetComponentInChildren<Image>();
                if (iconImage != null) iconImage.sprite = defaultEmptySprite;

                Image cdImage = btn.transform.Find("CooldownOverlay")?.GetComponent<Image>();
                if (cdImage != null)
                {
                    skillCooldownEntries.Add(new SkillCooldownEntry { skillName = "", cooldownImage = cdImage });
                }

                btn.Initialize(skillsComponent, core, i);
                btn.skill = null;
                Debug.Log($"[PlayerUI] Empty skill button2 at index {i} initialized, hotkey {hotkeys2[i]}");
            }

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
                UpdateAttributesPanel();
                Debug.Log($"[PlayerUI] AttributesPanel set to {newState}. ActiveSelf={attributesPanel.activeSelf}, Children={attributesPanel.transform.childCount}");
            }
            else
            {
                Debug.LogError("[PlayerUI] AttributesPanel is null! Ensure it is assigned in the Inspector.");
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
            else
            {
                Debug.LogError("[PlayerUI] SkillPanel is null! Ensure it is assigned in the Inspector.");
            }
        }

        foreach (var btn in skillButtons2)
        {
            if (btn.skill != null && Input.GetKeyDown(btn.skill.Hotkey))
            {
                Debug.Log($"[PlayerUI] Hotkey pressed for skill: {btn.skill.SkillName} (hotkey {btn.skill.Hotkey})");
                btn.OnButtonClicked();
            }
            else if (btn.item != null && Input.GetKeyDown(GetHotkeyForButton(btn)))
            {
                Debug.Log($"[PlayerUI] Hotkey pressed for item: {btn.item.itemName} (hotkey {GetHotkeyForButton(btn)})");
                btn.OnButtonClicked();
            }
        }

        foreach (var btn in skillButtons3)
        {
            if (btn.skill != null && Input.GetKeyDown(btn.skill.Hotkey))
            {
                Debug.Log($"[PlayerUI] Hotkey pressed for skill: {btn.skill.SkillName} (hotkey {btn.skill.Hotkey})");
                btn.OnButtonClicked();
            }
            else if (btn.item != null && Input.GetKeyDown(GetHotkeyForButton(btn)))
            {
                Debug.Log($"[PlayerUI] Hotkey pressed for item: {btn.item.itemName} (hotkey {GetHotkeyForButton(btn)})");
                btn.OnButtonClicked();
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

        if (strengthText != null) strengthText.text = $"{stats.strength}";
        if (agilityText != null) agilityText.text = $"{stats.agility}";
        if (spiritText != null) spiritText.text = $"{stats.spirit}";
        if (constitutionText != null) constitutionText.text = $"{stats.constitution}";
        if (accuracyText != null) accuracyText.text = $"{stats.accuracy}";
        if (armorText != null) armorText.text = $"{stats.armor}";
        if (physicalResistanceText != null) physicalResistanceText.text = $"{stats.physicalResistance:F1}%";
        if (magicDamageMultiplierText != null) magicDamageMultiplierText.text = $"{stats.magicDamageMultiplier:F2}x";
        if (movementSpeedText != null) movementSpeedText.text = $"{stats.movementSpeed:F1}";
        if (attackSpeedText != null) attackSpeedText.text = $"{stats.attackSpeed:F2}";
        if (dodgeChanceText != null) dodgeChanceText.text = $"{stats.dodgeChance:F1}%";
        if (hitChanceText != null) hitChanceText.text = $"{stats.hitChance:F1}%";
        if (criticalHitChanceText != null) criticalHitChanceText.text = $"{stats.criticalHitChance:F1}%";
        if (criticalHitMultiplierText != null) criticalHitMultiplierText.text = $"{stats.criticalHitMultiplier:F2}x";
        if (minAttackText != null) minAttackText.text = $"{stats.minAttack}";
        if (maxAttackText != null) maxAttackText.text = $"{stats.maxAttack}";

        bool hasPoints = stats.characteristicPoints > 0;

        if (strengthButton != null) { strengthButton.gameObject.SetActive(hasPoints); Debug.Log($"[PlayerUI] StrengthButton active: {hasPoints}"); }
        if (agilityButton != null) { agilityButton.gameObject.SetActive(hasPoints); Debug.Log($"[PlayerUI] AgilityButton active: {hasPoints}"); }
        if (spiritButton != null) { spiritButton.gameObject.SetActive(hasPoints); Debug.Log($"[PlayerUI] SpiritButton active: {hasPoints}"); }
        if (constitutionButton != null) { constitutionButton.gameObject.SetActive(hasPoints); Debug.Log($"[PlayerUI] ConstitutionButton active: {hasPoints}"); }
        if (accuracyButton != null) { accuracyButton.gameObject.SetActive(hasPoints); Debug.Log($"[PlayerUI] AccuracyButton active: {hasPoints}"); }
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
    }

    public void AssignItemToHotbar(Item item, SkillButton hotbarButton, int slotIndex)
    {
        int index2 = Array.IndexOf(skillButtons2, hotbarButton);
        int index3 = Array.IndexOf(skillButtons3, hotbarButton);
        if (index2 == -1 && index3 == -1) return;

        // Очистка скилла, если есть
        string oldSkillName = "";
        if (hotbarButton.skill != null)
        {
            oldSkillName = hotbarButton.skill.SkillName;
            Destroy(hotbarButton.skill);
            hotbarButton.skill = null;
        }

        Image iconImage = hotbarButton.GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
        }

        var cooldownEntry = skillCooldownEntries.Find(e => e.cooldownImage == hotbarButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
        if (cooldownEntry != null)
        {
            cooldownEntry.skillName = item.itemName;
        }

        hotbarButton.item = item;
        hotbarButton.itemSlotIndex = slotIndex;

        Debug.Log($"[PlayerUI] Assigned item {item.itemName} to hotbar slot (index {(index2 != -1 ? index2 : index3)}), slotIndex: {slotIndex}, cleared skill: {oldSkillName}");
    }

    public void ClearHotbarItem(int itemId)
    {
        foreach (var btn in skillButtons2.Concat(skillButtons3))
        {
            if (btn.item != null && btn.item.id == itemId)
            {
                btn.item = null;
                btn.itemSlotIndex = -1;
                Image iconImage = btn.GetComponentInChildren<Image>();
                if (iconImage != null) iconImage.sprite = defaultEmptySprite;
                var entry = skillCooldownEntries.Find(e => e.cooldownImage == btn.transform.Find("CooldownOverlay")?.GetComponent<Image>());
                if (entry != null) entry.skillName = "";
                Debug.Log($"[PlayerUI] Cleared hotbar item {itemId} from button {btn.buttonIndex}");
                break;
            }
        }
    }

    public void SwapSkillsOrItems(SkillButton firstButton, SkillButton secondButton)
    {
        if (firstButton == null || (firstButton.skill == null && firstButton.item == null) || firstButton.buttonIndex == 0)
        {
            Debug.LogError($"[PlayerUI] Cannot swap: firstButton={firstButton}, firstSkill={(firstButton?.skill?.SkillName)}, firstItem={(firstButton?.item?.itemName)}, firstIndex={firstButton?.buttonIndex}, secondIndex={secondButton?.buttonIndex}");
            return;
        }

        // Очистка при drop мимо
        if (secondButton == null)
        {
            string oldSkillName = "";
            if (firstButton.skill != null)
            {
                oldSkillName = firstButton.skill.SkillName;
                Destroy(firstButton.skill);
                firstButton.skill = null;
            }
            if (firstButton.item != null)
            {
                firstButton.item = null;
                firstButton.itemSlotIndex = -1;
            }
            Image iconImage = firstButton.GetComponentInChildren<Image>();
            if (iconImage != null) iconImage.sprite = defaultEmptySprite;
            var entry = skillCooldownEntries.Find(e => e.cooldownImage == firstButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            if (entry != null) entry.skillName = "";
            Debug.Log($"[PlayerUI] Cleared hotbar slot {firstButton.buttonIndex} (skill: {oldSkillName})");
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

        if (isFirstInSpellBook && isSecondInSpellBook)
        {
            Debug.Log("[PlayerUI] Drag inside spell book (panel1) ignored");
            return;
        }
        else if (isSecondInSpellBook)
        {
            Debug.Log("[PlayerUI] Drag to spell book (panel1) ignored");
            return;
        }
        else if (isFirstInSpellBook)
        {
            SkillBase skillCopy = Instantiate(firstButton.skill);
            skillCopy.Init(core);
            skillCopy.Hotkey = secondHotkey; // Set на копии
            if (secondButton.skill != null)
            {
                string oldSecondSkillName = secondButton.skill.SkillName;
                Destroy(secondButton.skill);
                secondButton.skill = null;
            }
            if (secondButton.item != null)
            {
                secondButton.item = null;
                secondButton.itemSlotIndex = -1;
            }
            secondButton.skill = skillCopy;
            secondIcon.sprite = firstIcon.sprite;
            var secondEntry = skillCooldownEntries.Find(e => e.cooldownImage == secondButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            if (secondEntry != null) secondEntry.skillName = skillCopy.SkillName;
            Debug.Log($"[PlayerUI] Copied skill {skillCopy.SkillName} from spell book to slot {secondButton.buttonIndex}, hotkey {secondHotkey}");
        }
        else
        {
            // Swap между hotbar
            SkillBase tempSkill = firstButton.skill;
            Item tempItem = firstButton.item;
            int tempSlotIndex = firstButton.itemSlotIndex;
            Sprite tempSprite = firstIcon.sprite;
            KeyCode tempHotkey = firstButton.skill?.Hotkey ?? KeyCode.None;

            // Assign second to first
            firstButton.skill = secondButton.skill;
            firstButton.item = secondButton.item;
            firstButton.itemSlotIndex = secondButton.itemSlotIndex;
            firstIcon.sprite = secondButton.skill != null ? secondIcon.sprite : (secondButton.item != null ? secondButton.item.icon : defaultEmptySprite);
            if (firstButton.skill != null) firstButton.skill.Hotkey = firstHotkey;

            // Assign first to second
            secondButton.skill = tempSkill;
            secondButton.item = tempItem;
            secondButton.itemSlotIndex = tempSlotIndex;
            secondIcon.sprite = tempSkill != null ? tempSprite : (tempItem != null ? tempItem.icon : defaultEmptySprite);
            if (secondButton.skill != null) secondButton.skill.Hotkey = secondHotkey;

            var firstEntry = skillCooldownEntries.Find(e => e.cooldownImage == firstButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());
            var secondEntry = skillCooldownEntries.Find(e => e.cooldownImage == secondButton.transform.Find("CooldownOverlay")?.GetComponent<Image>());

            if (firstEntry != null) firstEntry.skillName = firstButton.skill != null ? firstButton.skill.SkillName : (firstButton.item != null ? firstButton.item.itemName : "");
            if (secondEntry != null) secondEntry.skillName = secondButton.skill != null ? secondButton.skill.SkillName : (secondButton.item != null ? secondButton.item.itemName : "");

            Debug.Log($"[PlayerUI] Swapped: {(firstButton.skill != null ? firstButton.skill.SkillName : (firstButton.item != null ? firstButton.item.itemName : "empty"))} (hotkey {firstHotkey}) <-> {(secondButton.skill != null ? secondButton.skill.SkillName : (secondButton.item != null ? secondButton.item.itemName : "empty"))} (hotkey {secondHotkey})");
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