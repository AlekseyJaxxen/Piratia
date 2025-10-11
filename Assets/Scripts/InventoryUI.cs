using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;
using System.Linq;

public class InventoryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static InventoryUI Instance { get; private set; }
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private Button closeInventoryButton;
    [SerializeField] private Button closeCharacterButton;
    [SerializeField] private InventorySlot[] inventorySlots;
    [SerializeField] private EquipmentSlotUI headSlotUI;
    [SerializeField] private EquipmentSlotUI bodySlotUI;
    [SerializeField] private EquipmentSlotUI legsSlotUI;
    [SerializeField] private EquipmentSlotUI rightHandSlotUI;
    [SerializeField] private EquipmentSlotUI leftHandSlotUI;
    [SerializeField] private EquipmentSlotUI ringSlotUI;
    [SerializeField] private EquipmentSlotUI necklaceSlotUI;
    [SerializeField] private EquipmentSlotUI bootsSlotUI;
    [SerializeField] private EquipmentSlotUI glovesSlotUI;
    [SerializeField] private EquipmentSlotUI weaponSlotUI;
    [SerializeField] private EquipmentSlotUI offHandSlotUI;
    [SerializeField] private EquipmentSlotUI[] equipmentSlots;
    [SerializeField] private GameObject itemTooltip;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private float tooltipOffsetX = 25f;
    [SerializeField] private float tooltipOffsetY = 25f;
    [SerializeField] private TextMeshProUGUI goldText;
    private PlayerCore core;
    private Inventory inventory;
    private RectTransform inventoryPanelRect;
    private RectTransform characterPanelRect;
    private Vector2 dragOffset;
    public InventorySlot draggedSlot;
    public bool isTooltipActive;

    private void Awake()
    {
        Instance = this;
        if (inventoryPanel != null) inventoryPanelRect = inventoryPanel.GetComponent<RectTransform>();
        if (characterPanel != null) characterPanelRect = characterPanel.GetComponent<RectTransform>();
        inventoryPanel.SetActive(false);
        characterPanel.SetActive(false);
        itemTooltip.SetActive(false);
        
        // Инициализируем equipmentSlots
        if (equipmentSlots == null || equipmentSlots.Length == 0)
        {
            equipmentSlots = GetComponentsInChildren<EquipmentSlotUI>(true);
            Debug.Log($"[InventoryUI] Awake: Auto-initialized equipmentSlots, found {equipmentSlots?.Length ?? 0} EquipmentSlotUI components");
            if (equipmentSlots != null && equipmentSlots.Length > 0)
            {
                for (int i = 0; i < equipmentSlots.Length; i++)
                {
                    Debug.Log($"[InventoryUI] Awake: equipmentSlots[{i}] = {equipmentSlots[i]?.name ?? "null"} (slotType: {equipmentSlots[i]?.slotType})");
                }
            }
        }
        else
        {
            Debug.Log($"[InventoryUI] Awake: Using serialized equipmentSlots, found {equipmentSlots.Length} EquipmentSlotUI components");
        }
    }

    private void Start()
    {
        core = GetComponentInParent<PlayerCore>();
        if (core == null || !core.isLocalPlayer)
        {
            gameObject.SetActive(false);
            return;
        }
        inventory = core.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("[InventoryUI] Inventory component not found!");
            gameObject.SetActive(false);
            return;
        }
        if (inventory != null)
        {
            inventory.OnInventoryChanged.AddListener(UpdateInventoryUI);
            inventory.OnGoldChanged.AddListener(OnGoldUIChanged);
            inventory.OnEquipmentChanged.AddListener(UpdateEquipmentUI);
        }
        if (closeInventoryButton != null)
            closeInventoryButton.onClick.AddListener(() => { inventoryPanel.SetActive(false); characterPanel.SetActive(false); });
        if (goldText != null)
            goldText.text = $"Gold: {inventory.gold}";
        UpdateInventoryUI();
        UpdateEquipmentUI();
        StartCoroutine(WaitForSyncAndUpdate());
    }

    private void Update()
    {
        // Обрабатываем ввод только для локального игрока
        if (!core.isLocalPlayer || core.isDead) return;
        
        // Проверяем stun, но разрешаем открытие инвентаря во время торговли
        TradeSystem tradeSystem = core.GetComponent<TradeSystem>();
        bool canOpenInventory = !core.isStunned || (tradeSystem != null && tradeSystem.IsTradeActive());
        
        if (!canOpenInventory) return;
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool newState = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(newState);
            characterPanel.SetActive(newState);
            UpdateInventoryUI();
            UpdateEquipmentUI();
            // Panels state updated
        }
    }

    public void UpdateInventoryUI()
    {
        if (inventorySlots == null || inventory == null) return;
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;
            if (i < inventory.items.Count && inventory.items[i].id > 0)
            {
                inventorySlots[i].slotIndex = i;
                inventorySlots[i].SetItem(inventory.items[i]);
            }
            else
            {
                inventorySlots[i].Clear();
            }
        }
        
        // Обновляем визуальное состояние предметов в торговле
        UpdateTradeItemVisuals();
    }
    
    private void UpdateTradeItemVisuals()
    {
        if (core == null) return;
        
        TradeSystem tradeSystem = core.GetComponent<TradeSystem>();
        
        // Проверяем каждый слот инвентаря на предметы в торговле
        for (int i = 0; i < inventorySlots.Length && i < inventory.items.Count; i++)
        {
            if (inventorySlots[i] == null) continue;
            
            ItemInfo inventoryItem = inventory.items[i];
            if (inventoryItem.id == 0) 
            {
                // Очищаем состояние для пустых слотов
                inventorySlots[i].SetTradeLocked(false);
                continue;
            }
            
            // Если торговля не активна, убираем все блокировки
            if (tradeSystem == null || !tradeSystem.IsTradeActive())
            {
                inventorySlots[i].SetTradeLocked(false);
                continue;
            }
            
            // Проверяем, находится ли этот предмет в торговых слотах
            // Исправленная логика: проверяем точное совпадение ID и количества
            bool isInTrade = false;
            for (int j = 0; j < tradeSystem.tradeItems.Count; j++)
            {
                ItemInfo tradeItem = tradeSystem.tradeItems[j];
                if (tradeItem.id == inventoryItem.id && tradeItem.quantity == inventoryItem.quantity)
                {
                    isInTrade = true;
                    break;
                }
            }
            
            // Устанавливаем визуальное состояние
            inventorySlots[i].SetTradeLocked(isInTrade);
        }
    }

    public void UpdateEquipmentUI()
    {
        if (core == null || core.Inventory == null) return;
        
        // Используем автоматически найденные слоты из equipmentSlots
        EquipmentSlotUI[] slots = GetEquipmentSlots();
        if (slots != null && slots.Length > 0)
        {
            foreach (EquipmentSlotUI slot in slots)
            {
                if (slot != null)
                {
                    slot.SetItem(core.Inventory.GetEquipped(slot.slotType));
                }
            }
        }
        else
        {
            Debug.LogWarning("[InventoryUI] No equipment slots found!");
        }
        
        // Также обновляем отдельные слоты, если они назначены (для обратной совместимости)
        if (headSlotUI != null) headSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Head));
        if (bodySlotUI != null) bodySlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Body));
        if (legsSlotUI != null) legsSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Legs));
        if (rightHandSlotUI != null) rightHandSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.RightHand));
        if (leftHandSlotUI != null) leftHandSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.LeftHand));
        if (ringSlotUI != null) ringSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Ring));
        if (necklaceSlotUI != null) necklaceSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Necklace));
        if (bootsSlotUI != null) bootsSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Boots));
        if (glovesSlotUI != null) glovesSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Gloves));
        if (weaponSlotUI != null) weaponSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Weapon));
        if (offHandSlotUI != null) offHandSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.OffHand));
        
        // Equipment UI updated
    }

    public void ShowTooltip(Item item, Vector3 position)
    {
        if (item == null || isTooltipActive) return;
        
        bool canEquip = item.IsEquipable(core.Stats.level, core.Stats.characterClass);
        string rarityColor = GetRarityColor(item.rarity);
        
        StringBuilder sb = new StringBuilder();
        
        // Имя предмета с усилением (если есть)
        string itemName = item.itemName;
        // if (item.enhancementLevel > 0)
        // {
        //     itemName += $" +{item.enhancementLevel}";
        // }
        sb.AppendLine($"<size=16><b><color={rarityColor}>{itemName}</color></b></size>");
        
        // Основные характеристики предмета
        if (item.constantDefence != 0 || item.physicalResistBonus != 0 || item.minAttackConstantBonus != 0 || item.maxAttackConstantBonus != 0)
        {
            string stats = "";
            if (item.minAttackConstantBonus != 0 || item.maxAttackConstantBonus != 0)
            {
                stats += $"Attack [{item.minAttackConstantBonus}/{item.maxAttackConstantBonus}]";
            }
            if (item.constantDefence != 0)
            {
                if (stats != "") stats += "\n";
                stats += $"Defense [{item.constantDefence}]";
            }
            if (item.physicalResistBonus != 0)
            {
                if (stats != "") stats += "\n";
                stats += $"Physical Resistance [{item.physicalResistBonus}%]";
            }
            sb.AppendLine($"<size=11><color=#FFFFFF>{stats}</color></size>");
        }
        
        // Прочность
        if (item.durability > 0)
        {
            sb.AppendLine($"<size=11><color=#FFFFFF>Durability [{item.durability}/{item.durability}]</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Требования к предмету
        if (item.requiredLevel > 0)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Level Requirement: {item.requiredLevel}</color></size>");
        }
        
        if (item.characterClass != CharacterClass.None)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Class Requirement: {item.characterClass}</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Бонусы к характеристикам (синим цветом)
        bool hasBonuses = false;
        if (item.strengthBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Strength Bonus: +{item.strengthBonus}</color></size>");
            hasBonuses = true;
        }
        if (item.agilityBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Agility Bonus: +{item.agilityBonus}</color></size>");
            hasBonuses = true;
        }
        if (item.spiritBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Spirit Bonus: +{item.spiritBonus}</color></size>");
            hasBonuses = true;
        }
        if (item.accuracyBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Accuracy Bonus: +{item.accuracyBonus}</color></size>");
            hasBonuses = true;
        }
        if (item.constitutionBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Constitution Bonus: +{item.constitutionBonus}</color></size>");
            hasBonuses = true;
        }
        
        // Бонусы к HP/SP
        if (item.maxHpConstantBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Maximum HP Bonus: +{item.maxHpConstantBonus}</color></size>");
            hasBonuses = true;
        }
        if (item.maxSpConstantBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Maximum SP Bonus: +{item.maxSpConstantBonus}</color></size>");
            hasBonuses = true;
        }
        
        // Процентные бонусы
        // if (item.maxHpPercentBonus != 0)
        // {
        //     sb.AppendLine($"<size=10><color=#4A9EFF>Maximum HP Bonus: +{item.maxHpPercentBonus:F1}%</color></size>");
        //     hasBonuses = true;
        // }
        // if (item.maxSpPercentBonus != 0)
        // {
        //     sb.AppendLine($"<size=10><color=#4A9EFF>Maximum SP Bonus: +{item.maxSpPercentBonus:F1}%</color></size>");
        //     hasBonuses = true;
        // }
        // if (item.spRecoveryRateBonus != 0)
        // {
        //     sb.AppendLine($"<size=10><color=#4A9EFF>SP Recovery Rate Bonus: +{item.spRecoveryRateBonus:F1}%</color></size>");
        //     hasBonuses = true;
        // }
        
        // Критический шанс
        if (item.crtConstantBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Critical Chance Bonus: +{item.crtConstantBonus}%</color></size>");
            hasBonuses = true;
        }
        
        // Скорость атаки
        if (item.attackSpeedBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Attack Speed Bonus: +{item.attackSpeedBonus:F2}</color></size>");
            hasBonuses = true;
        }
        if (item.attackSpeedPercentBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Attack Speed Bonus: +{item.attackSpeedPercentBonus:F1}%</color></size>");
            hasBonuses = true;
        }
        
        // Скорость движения
        if (item.mspdConstantBonus != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Movement Speed Bonus: +{Mathf.RoundToInt(item.mspdConstantBonus * 100)}</color></size>");
            hasBonuses = true;
        }
        
        // Пустая строка после бонусов
        if (hasBonuses)
        {
            sb.AppendLine("");
        }
        
        // Сокеты (если есть)
        // if (item.socketCount > 0)
        // {
        //     sb.AppendLine($"<size=10><color=#FFFFFF>Socket(s): {item.socketCount}</color></size>");
        //     // Здесь можно добавить отображение вставленных камней
        //     sb.AppendLine("");
        // }
        
        // Торговая ценность
        if (item.price > 0)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Trade Value: {item.price:N0}</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Интерактивные элементы
        sb.AppendLine($"<size=10><color=#00FF00>Trade</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Delete</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Throw</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Количество в стопке
        sb.AppendLine($"<size=10><color=#FFFFFF>Stack: 1</color></size>");
        
        // Ошибки экипировки
        if (!canEquip)
        {
            sb.AppendLine($"<size=10><color=#FF0000>Cannot equip: level or class mismatch</color></size>");
        }
        
        // Показываем tooltip
        itemTooltip.SetActive(true);
        tooltipText.text = sb.ToString().TrimEnd('\n');
        
        // Настройка параметров фона в коде
        if (itemTooltip != null)
        {
            RectTransform tooltipRect = itemTooltip.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.pivot = new Vector2(0, 1);
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
            }
            
            // Настройка фона - более темный полупрозрачный фон
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                backgroundImage.raycastTarget = false;
            }
        }
        
        // Настройка параметров Text в коде
        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0, 1);
            }
        }
        
        // Позиционирование левым верхним углом на 25px от курсора
        itemTooltip.transform.position = position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f);
        
        isTooltipActive = true;
        
        // Debug.Log($"[InventoryUI] Showing enhanced tooltip for {item.itemName} at position {position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f)}");
    }
    
    public void ShowTooltip(ItemInfo itemInfo, Vector3 position)
    {
        if (itemInfo.id <= 0 || isTooltipActive) return;
        
        Item item = itemInfo.GetItem();
        if (item == null) return;
        
        bool canEquip = item.IsEquipable(core.Stats.level, core.Stats.characterClass);
        string rarityColor = GetRarityColor(itemInfo.GetItemRarity());
        
        StringBuilder sb = new StringBuilder();
        
        // Имя предмета с усилением (если есть)
        string itemName = itemInfo.GetItemName();
        // if (itemInfo.enhancementLevel > 0)
        // {
        //     itemName += $" +{itemInfo.enhancementLevel}";
        // }
        sb.AppendLine($"<size=16><b><color={rarityColor}>{itemName}</color></b></size>");
        
        // Основные характеристики предмета (с динамическими статами)
        int totalConstantDefence = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.ConstantDefence));
        int totalPhysicalResistance = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.PhysicalResistance));
        int totalMinAttack = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MinAttack));
        int totalMaxAttack = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxAttack));
        
        if (totalConstantDefence != 0 || totalPhysicalResistance != 0 || totalMinAttack != 0 || totalMaxAttack != 0)
        {
            string stats = "";
            if (totalMinAttack != 0 || totalMaxAttack != 0)
            {
                stats += $"Attack [{totalMinAttack}/{totalMaxAttack}]";
            }
            if (totalConstantDefence != 0)
            {
                if (stats != "") stats += "\n";
                stats += $"Defense [{totalConstantDefence}]";
            }
            if (totalPhysicalResistance != 0)
            {
                if (stats != "") stats += "\n";
                stats += $"Physical Resistance [{totalPhysicalResistance}%]";
            }
            sb.AppendLine($"<size=11><color=#FFFFFF>{stats}</color></size>");
        }
        
        // Прочность
        if (item.durability > 0)
        {
            sb.AppendLine($"<size=11><color=#FFFFFF>Durability [{item.durability}/{item.durability}]</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Требования к предмету
        if (item.requiredLevel > 0)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Level Requirement: {item.requiredLevel}</color></size>");
        }
        
        if (item.characterClass != CharacterClass.None)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Class Requirement: {item.characterClass}</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Бонусы к характеристикам (синим цветом)
        bool hasBonuses = false;
        
        // Основные статы (с динамическими статами)
        int totalStrength = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Strength));
        int totalAgility = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Agility));
        int totalSpirit = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Spirit));
        int totalConstitution = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Constitution));
        int totalAccuracy = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Accuracy));
        
        if (totalStrength != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Strength Bonus: +{totalStrength}</color></size>");
            hasBonuses = true;
        }
        if (totalAgility != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Agility Bonus: +{totalAgility}</color></size>");
            hasBonuses = true;
        }
        if (totalSpirit != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Spirit Bonus: +{totalSpirit}</color></size>");
            hasBonuses = true;
        }
        if (totalConstitution != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Constitution Bonus: +{totalConstitution}</color></size>");
            hasBonuses = true;
        }
        if (totalAccuracy != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Accuracy Bonus: +{totalAccuracy}</color></size>");
            hasBonuses = true;
        }
        
        // Другие статы (с динамическими статами)
        int totalMaxHP = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxHP));
        int totalMaxMP = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxMP));
        int totalMovementSpeed = itemInfo.GetDisplayValue(ItemInfo.StatType.MovementSpeed);
        float totalAttackSpeed = itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackSpeed);
        float totalAttackSpeedPercent = itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackSpeedPercent);
        int totalCritical = Mathf.RoundToInt(itemInfo.GetTotalStatBonus(ItemInfo.StatType.Critical));
        
        if (totalMaxHP != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Maximum HP Bonus: +{totalMaxHP}</color></size>");
            hasBonuses = true;
        }
        if (totalMaxMP != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Maximum SP Bonus: +{totalMaxMP}</color></size>");
            hasBonuses = true;
        }
        if (totalMovementSpeed != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Movement Speed Bonus: +{totalMovementSpeed}</color></size>");
            hasBonuses = true;
        }
        if (totalAttackSpeed != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Attack Speed Bonus: +{totalAttackSpeed:F2}</color></size>");
            hasBonuses = true;
        }
        if (totalAttackSpeedPercent != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Attack Speed Bonus: +{totalAttackSpeedPercent:F1}%</color></size>");
            hasBonuses = true;
        }
        if (totalCritical != 0)
        {
            sb.AppendLine($"<size=10><color=#4A9EFF>Critical Chance Bonus: +{totalCritical}%</color></size>");
            hasBonuses = true;
        }
        
        // Пустая строка после бонусов
        if (hasBonuses)
        {
            sb.AppendLine("");
        }
        
        // Сокеты (если есть)
        // if (item.socketCount > 0)
        // {
        //     sb.AppendLine($"<size=10><color=#FFFFFF>Socket(s): {item.socketCount}</color></size>");
        //     // Здесь можно добавить отображение вставленных камней
        //     sb.AppendLine("");
        // }
        
        // Торговая ценность
        if (item.price > 0)
        {
            sb.AppendLine($"<size=10><color=#FFFFFF>Trade Value: {item.price:N0}</color></size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Интерактивные элементы
        sb.AppendLine($"<size=10><color=#00FF00>Trade</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Delete</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Throw</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Количество в стопке
        sb.AppendLine($"<size=10><color=#FFFFFF>Stack: {itemInfo.quantity}</color></size>");
        
        // Ошибки экипировки
        if (!canEquip)
        {
            sb.AppendLine($"<size=10><color=#FF0000>Cannot equip: level or class mismatch</color></size>");
        }
        
        // Показываем tooltip
        itemTooltip.SetActive(true);
        tooltipText.text = sb.ToString().TrimEnd('\n');
        
        // Настройка параметров фона в коде
        if (itemTooltip != null)
        {
            RectTransform tooltipRect = itemTooltip.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.pivot = new Vector2(0, 1);
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
            }
            
            // Настройка фона - более темный полупрозрачный фон
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                backgroundImage.raycastTarget = false;
            }
        }
        
        // Настройка параметров Text в коде
        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0, 1);
            }
        }
        
        // Позиционирование левым верхним углом на 25px от курсора
        itemTooltip.transform.position = position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f);
        
        isTooltipActive = true;
        
        // Debug.Log($"[InventoryUI] Showing enhanced tooltip for {itemInfo.GetItemName()} at position {position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f)}");
    }

    public void ShowSkillTooltip(SkillBase skill, Vector3 position)
    {
        if (skill == null || isTooltipActive) return;
        
        StringBuilder sb = new StringBuilder();
        
        // Название навыка
        sb.AppendLine($"<size=16><b><color=#FFD700>{skill.SkillName}</color></b></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Описание навыка
        sb.AppendLine($"<size=11><color=#FFFFFF>{skill.Description}</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Характеристики навыка (синим цветом)
        sb.AppendLine($"<size=10><color=#4A9EFF>Mana Cost: {skill.ManaCost}</color></size>");
        sb.AppendLine($"<size=10><color=#4A9EFF>Cooldown: {skill.Cooldown}s</color></size>");
        sb.AppendLine($"<size=10><color=#4A9EFF>Range: {skill.Range}</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Интерактивные элементы
        sb.AppendLine($"<size=10><color=#00FF00>Use</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Learn</color></size>");
        
        itemTooltip.SetActive(true);
        tooltipText.text = sb.ToString().TrimEnd('\n');
        
        // Настройка параметров фона в коде
        if (itemTooltip != null)
        {
            RectTransform tooltipRect = itemTooltip.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.pivot = new Vector2(0, 1);
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
            }
            
            // Настройка фона - более темный полупрозрачный фон
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                backgroundImage.raycastTarget = false;
            }
        }
        
        // Настройка параметров Text в коде
        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0, 1);
                // Размеры задаются статично в префабе, не изменяем их в коде
            }
            
            // Настройки TextMeshProUGUI задаются в префабе, не перезаписываем их в коде
        }
        
        // Позиционирование левым верхним углом на 25px от курсора
        itemTooltip.transform.position = position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f);
        
        isTooltipActive = true;
        // Debug.Log($"[InventoryUI] Showing enhanced tooltip for skill {skill.SkillName} at position {position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f)}");
    }

    public void HideTooltip()
    {
        if (!isTooltipActive) return;
        itemTooltip.SetActive(false);
        isTooltipActive = false;
        // Debug.Log($"[InventoryUI] Hiding tooltip, caller={new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");
    }
    
    public EquipmentSlotUI[] GetEquipmentSlots()
    {
        if (equipmentSlots == null || equipmentSlots.Length == 0)
        {
            equipmentSlots = GetComponentsInChildren<EquipmentSlotUI>(true);
            Debug.Log($"[InventoryUI] GetEquipmentSlots: Re-initialized equipmentSlots, found {equipmentSlots?.Length ?? 0} components");
        }
        return equipmentSlots;
    }
    
    public EquipmentSlotUI FindMatchingEquipmentSlot(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryUI] FindMatchingEquipmentSlot: Item is null");
            return null;
        }
        
        // Получаем equipmentSlots
        EquipmentSlotUI[] slots = GetEquipmentSlots();
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError($"[InventoryUI] FindMatchingEquipmentSlot: Failed to get EquipmentSlotUI components, found: {(slots?.Length ?? 0)}");
            return null;
        }
        Debug.Log($"[InventoryUI] FindMatchingEquipmentSlot: Got {slots.Length} equipmentSlots");
        Debug.Log($"[InventoryUI] Available slots: {string.Join(", ", slots.Select(s => $"{s.name}({s.slotType})"))}");
        Debug.Log($"[InventoryUI] Looking for item: {item.itemName} (equipmentSlot={item.equipmentSlot}, alternativeSlot={item.alternativeSlot})");
        
        // Специальная логика для двуручного оружия
        if (item.isTwoHanded)
        {
            EquipmentSlotUI leftHandSlot = slots.FirstOrDefault(s => s.slotType == EquipmentSlot.LeftHand);
            if (leftHandSlot != null)
            {
                Debug.Log($"[InventoryUI] Found LeftHand slot for two-handed weapon: {item.itemName}");
                return leftHandSlot;
            }
            else
            {
                Debug.LogWarning($"[InventoryUI] No LeftHand slot found for two-handed weapon: {item.itemName}. Available slots: {string.Join(", ", slots.Select(s => s.slotType.ToString()))}");
                return null;
            }
        }
        
        // Для одноручного оружия ищем свободную руку, для остального - основной слот
        if (item.itemType == ItemType.Weapon && !item.isTwoHanded)
        {
            // Одноручное оружие - ищем свободную руку
            EquipmentSlotUI rightHandSlot = slots.FirstOrDefault(s => s.slotType == EquipmentSlot.RightHand);
            EquipmentSlotUI leftHandSlot = slots.FirstOrDefault(s => s.slotType == EquipmentSlot.LeftHand);
            
            // Проверяем, заняты ли руки
            bool rightHandOccupied = rightHandSlot != null && rightHandSlot.itemInfo.id > 0;
            bool leftHandOccupied = leftHandSlot != null && leftHandSlot.itemInfo.id > 0;
            
            if (!rightHandOccupied && rightHandSlot != null)
            {
                Debug.Log($"[InventoryUI] Found free right hand slot for one-handed weapon: {item.itemName}");
                return rightHandSlot;
            }
            else if (!leftHandOccupied && leftHandSlot != null)
            {
                Debug.Log($"[InventoryUI] Found free left hand slot for one-handed weapon: {item.itemName}");
                return leftHandSlot;
            }
            else if (rightHandSlot != null)
            {
                Debug.Log($"[InventoryUI] Both hands occupied, using right hand slot for one-handed weapon: {item.itemName} (will swap)");
                return rightHandSlot;
            }
        }
        
        // Проверяем основной слот (принудительно, своп если занят)
        EquipmentSlotUI mainSlot = slots.FirstOrDefault(s => s.slotType == item.equipmentSlot);
        if (mainSlot != null)
        {
            Debug.Log($"[InventoryUI] Found main slot: {item.equipmentSlot} for {item.itemName} (will swap if occupied)");
            return mainSlot;
        }
        else
        {
            Debug.LogWarning($"[InventoryUI] No EquipmentSlotUI found for {item.equipmentSlot} in equipmentSlots");
        }
        // Проверяем альтернативный слот (принудительно)
        if (item.alternativeSlot != EquipmentSlot.None)
        {
            EquipmentSlotUI altSlot = slots.FirstOrDefault(s => s.slotType == item.alternativeSlot);
            if (altSlot != null)
            {
                Debug.Log($"[InventoryUI] Found alternative slot: {item.alternativeSlot} for {item.itemName} (will swap if occupied)");
                return altSlot;
            }
            else
            {
                Debug.LogWarning($"[InventoryUI] No EquipmentSlotUI found for {item.alternativeSlot} in equipmentSlots");
            }
        }
        Debug.LogWarning($"[InventoryUI] No slots found for {item.itemName} (equipmentSlot: {item.equipmentSlot}, alternativeSlot: {item.alternativeSlot})");
        return null;
    }
    
    public void ShowEmptySlotTooltip(EquipmentSlot slotType, Vector3 position)
    {
        if (isTooltipActive) return;
        
        string slotName = GetSlotDisplayName(slotType);
        string slotDescription = GetSlotDescription(slotType);
        
        StringBuilder sb = new StringBuilder();
        
        // Название слота
        sb.AppendLine($"<size=16><b><color=#FFFFFF>{slotName}</color></b></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Описание слота
        sb.AppendLine($"<size=11><color=#CCCCCC>{slotDescription}</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Инструкция
        sb.AppendLine($"<size=10><color=#AAAAAA>Drag an item here to equip it</color></size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Интерактивные элементы
        sb.AppendLine($"<size=10><color=#00FF00>Equip</color></size>");
        sb.AppendLine($"<size=10><color=#00FF00>Auto-Equip</color></size>");
        
        // Показываем тултип
        itemTooltip.SetActive(true);
        tooltipText.text = sb.ToString().TrimEnd('\n');
        
        // Настройка параметров фона в коде
        if (itemTooltip != null)
        {
            RectTransform tooltipRect = itemTooltip.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.pivot = new Vector2(0, 1);
                tooltipRect.anchorMin = new Vector2(0, 1);
                tooltipRect.anchorMax = new Vector2(0, 1);
            }
            
            // Настройка фона - более темный полупрозрачный фон
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                backgroundImage.raycastTarget = false;
            }
        }
        
        // Настройка параметров Text в коде
        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0, 1);
                // Размеры задаются статично в префабе, не изменяем их в коде
            }
            
            // Настройки TextMeshProUGUI задаются в префабе, не перезаписываем их в коде
        }
        
        // Позиционирование левым верхним углом на 25px от курсора
        itemTooltip.transform.position = position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f);
        
        isTooltipActive = true;
    }
    
    private string GetSlotDescription(EquipmentSlot slotType)
    {
        switch (slotType)
        {
            case EquipmentSlot.Head:
                return "Head armor slot";
            case EquipmentSlot.Body:
                return "Body armor slot";
            case EquipmentSlot.Boots:
                return "Boots slot";
            case EquipmentSlot.Gloves:
                return "Gloves slot";
            case EquipmentSlot.Necklace:
                return "Necklace slot";
            case EquipmentSlot.RightHand:
                return "Right hand weapon slot";
            case EquipmentSlot.LeftHand:
                return "Left hand weapon slot";
            case EquipmentSlot.Ring:
                return "Ring slot";
            default:
                return "Equipment slot";
        }
    }
    
    private string GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return "#FFFFFF"; // Белый
            case Rarity.Uncommon:
                return "#00FF00"; // Зеленый
            case Rarity.Rare:
                return "#0080FF"; // Синий
            case Rarity.Epic:
                return "#8000FF"; // Фиолетовый
            case Rarity.Legendary:
                return "#FF8000"; // Оранжевый
            default:
                return "#FFFFFF"; // Белый по умолчанию
        }
    }
    
    private string GetSlotDisplayName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Head: return "Head";
            case EquipmentSlot.Body: return "Body";
            case EquipmentSlot.Legs: return "Legs";
            case EquipmentSlot.Boots: return "Boots";
            case EquipmentSlot.Gloves: return "Gloves";
            case EquipmentSlot.RightHand: return "Right Hand";
            case EquipmentSlot.LeftHand: return "Left Hand";
            case EquipmentSlot.OffHand: return "Off Hand";
            case EquipmentSlot.Ring: return "Ring";
            case EquipmentSlot.Necklace: return "Necklace";
            case EquipmentSlot.Weapon: return "Weapon";
            default: return slot.ToString();
        }
    }
    

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryPanel.activeSelf && IsPointerOverRect(eventData, inventoryPanelRect))
        {
            dragOffset = inventoryPanelRect.position - (Vector3)eventData.position;
            Debug.Log("[InventoryUI] Begin drag on InventoryPanel");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryPanel.activeSelf && IsPointerOverRect(eventData, inventoryPanelRect))
        {
            inventoryPanelRect.position = eventData.position + dragOffset;
            if (characterPanel.activeSelf)
            {
                characterPanelRect.position = inventoryPanelRect.position + (characterPanelRect.position - inventoryPanelRect.position);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryPanel.activeSelf && IsPointerOverRect(eventData, inventoryPanelRect))
        {
            Debug.Log("[InventoryUI] End drag on InventoryPanel");
        }
    }

    private bool IsPointerOverRect(PointerEventData eventData, RectTransform rectTransform)
    {
        if (rectTransform == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, eventData.pressEventCamera);
    }

    private void RpcUpdateInventoryUI()
    {
        if (Instance != null && Instance.inventory != null)
        {
            Instance.UpdateInventoryUI();
        }
        else
        {
            StartCoroutine(DelayedUpdateInventoryUI());
        }
    }

    private IEnumerator DelayedUpdateInventoryUI()
    {
        yield return new WaitForSeconds(0.1f);
        if (Instance != null && Instance.inventory != null)
        {
            Instance.UpdateInventoryUI();
        }
    }

    public void ShowInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        if (characterPanel != null)
        {
            characterPanel.SetActive(true);
        }
        
        UpdateInventoryUI();
        UpdateEquipmentUI();
    }
    
    public void HideInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        if (characterPanel != null)
        {
            characterPanel.SetActive(false);
        }
    }

    public void UpdateEquipmentSlot(EquipmentSlot slot, ItemInfo itemInfo)
    {
        switch (slot)
        {
            case EquipmentSlot.Head:
                if (headSlotUI != null) headSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] headSlotUI is not assigned!");
                break;
            case EquipmentSlot.Body:
                if (bodySlotUI != null) bodySlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] bodySlotUI is not assigned!");
                break;
            case EquipmentSlot.Legs:
                if (legsSlotUI != null) legsSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] legsSlotUI is not assigned!");
                break;
            case EquipmentSlot.RightHand:
                if (rightHandSlotUI != null) rightHandSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] rightHandSlotUI is not assigned!");
                break;
            case EquipmentSlot.LeftHand:
                if (leftHandSlotUI != null) leftHandSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] leftHandSlotUI is not assigned!");
                break;
            case EquipmentSlot.Ring:
                if (ringSlotUI != null) ringSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] ringSlotUI is not assigned!");
                break;
            case EquipmentSlot.Necklace:
                if (necklaceSlotUI != null) necklaceSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] necklaceSlotUI is not assigned!");
                break;
            case EquipmentSlot.Boots:
                if (bootsSlotUI != null) bootsSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] bootsSlotUI is not assigned!");
                break;
            case EquipmentSlot.Gloves:
                if (glovesSlotUI != null) glovesSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] glovesSlotUI is not assigned!");
                break;
            case EquipmentSlot.Weapon:
                if (weaponSlotUI != null) weaponSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] weaponSlotUI is not assigned!");
                break;
            case EquipmentSlot.OffHand:
                if (offHandSlotUI != null) offHandSlotUI.SetItem(itemInfo);
                else Debug.LogWarning("[InventoryUI] offHandSlotUI is not assigned!");
                break;
        }
        Debug.Log($"[InventoryUI] Updated equipment slot {slot}");
    }

    public EquipmentSlotUI FindMatchingEquipmentSlot(EquipmentSlot slotType)
    {
        switch (slotType)
        {
            case EquipmentSlot.Head: return headSlotUI != null ? headSlotUI : null;
            case EquipmentSlot.Body: return bodySlotUI != null ? bodySlotUI : null;
            case EquipmentSlot.Legs: return legsSlotUI != null ? legsSlotUI : null;
            case EquipmentSlot.RightHand: return rightHandSlotUI != null ? rightHandSlotUI : null;
            case EquipmentSlot.LeftHand: return leftHandSlotUI != null ? leftHandSlotUI : null;
            case EquipmentSlot.Ring: return ringSlotUI != null ? ringSlotUI : null;
            case EquipmentSlot.Necklace: return necklaceSlotUI != null ? necklaceSlotUI : null;
            case EquipmentSlot.Boots: return bootsSlotUI != null ? bootsSlotUI : null;
            case EquipmentSlot.Gloves: return glovesSlotUI != null ? glovesSlotUI : null;
            case EquipmentSlot.Weapon: return weaponSlotUI != null ? weaponSlotUI : null;
            case EquipmentSlot.OffHand: return offHandSlotUI != null ? offHandSlotUI : null;
            default: return null;
        }
    }

    private IEnumerator WaitForSyncAndUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateInventoryUI();
        UpdateEquipmentUI();
        Debug.Log("[InventoryUI] Forced UI update after sync");
    }

    private void OnGoldUIChanged()
    {
        if (goldText != null) goldText.text = $"Gold: {inventory.gold}";
    }
}