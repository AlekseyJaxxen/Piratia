using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Elements")]
    public Image healthBar;
    public Image manaBar;
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

    private CharacterStats stats;
    private PlayerCore core;
    private RectTransform attributesPanelRect; // Для перетаскивания
    private Vector2 dragOffset; // Смещение при перетаскивании

    private void Start()
    {
        stats = GetComponentInParent<CharacterStats>();
        core = GetComponentInParent<PlayerCore>();
        if (stats != null && core != null && core.isLocalPlayer)
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
            stats.OnMinAttackChangedEvent += (oldValue, newValue) => UpdateAttribute("minAttack", newValue);
            stats.OnMaxAttackChangedEvent += (oldValue, newValue) => UpdateAttribute("maxAttack", newValue);

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
            Debug.LogError("CharacterStats, PlayerCore not found or not local player!");
        }

        // Инициализация для перетаскивания
        if (attributesPanel != null)
        {
            attributesPanelRect = attributesPanel.GetComponent<RectTransform>();
            attributesPanel.SetActive(false);
        }

        // Гарантируем, что курсор всегда видим
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log($"AttributesPanel set to {newState}. Children: {attributesPanel.transform.childCount}");
                UpdateAttributesPanel();
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
            stats.OnMinAttackChangedEvent -= (oldValue, newValue) => UpdateAttribute("minAttack", newValue);
            stats.OnMaxAttackChangedEvent -= (oldValue, newValue) => UpdateAttribute("maxAttack", newValue);
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (!core.isLocalPlayer) return;
        if (healthBar != null)
        {
            float fillAmount = (float)currentHealth / maxHealth;
            healthBar.fillAmount = fillAmount;
            //healthBar.color = Color.Lerp(Color.red, Color.green, fillAmount);
        }
    }

    public void UpdateManaBar(int currentMana, int maxMana)
    {
        if (!core.isLocalPlayer) return;
        if (manaBar != null)
        {
            float fillAmount = (float)currentMana / maxMana;
            manaBar.fillAmount = fillAmount;
            manaBar.color = Color.Lerp(new Color(0, 0, 0.5f), Color.blue, fillAmount);
        }
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
            Debug.Log($"StrengthButton active: {hasPoints}");
        }
        if (agilityButton != null)
        {
            agilityButton.gameObject.SetActive(hasPoints);
            Debug.Log($"AgilityButton active: {hasPoints}");
        }
        if (spiritButton != null)
        {
            spiritButton.gameObject.SetActive(hasPoints);
            Debug.Log($"SpiritButton active: {hasPoints}");
        }
        if (constitutionButton != null)
        {
            constitutionButton.gameObject.SetActive(hasPoints);
            Debug.Log($"ConstitutionButton active: {hasPoints}");
        }
        if (accuracyButton != null)
        {
            accuracyButton.gameObject.SetActive(hasPoints);
            Debug.Log($"AccuracyButton active: {hasPoints}");
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

    // Реализация интерфейсов для перетаскивания
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (attributesPanelRect != null)
        {
            // Сохраняем смещение между позицией курсора и панелью
            dragOffset = attributesPanelRect.position - (Vector3)eventData.position;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (attributesPanelRect != null)
        {
            // Перемещаем панель, учитывая смещение
            attributesPanelRect.position = eventData.position + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Ничего не делаем после завершения перетаскивания
    }
}