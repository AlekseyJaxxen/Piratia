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
        if (!core.isLocalPlayer || core.isDead || core.isStunned) return;
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
    }

    public void UpdateEquipmentUI()
    {
        if (core == null || core.Inventory == null) return;
        if (headSlotUI != null) headSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Head));
        if (bodySlotUI != null) bodySlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Body));
        if (legsSlotUI != null) legsSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Legs));
        if (rightHandSlotUI != null) rightHandSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.RightHand));
        if (leftHandSlotUI != null) leftHandSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.LeftHand));
        if (ringSlotUI != null) ringSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Ring));
        else Debug.LogWarning("[InventoryUI] ringSlotUI is not assigned!");
        if (necklaceSlotUI != null) necklaceSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Necklace));
        else Debug.LogWarning("[InventoryUI] necklaceSlotUI is not assigned!");
        if (bootsSlotUI != null) bootsSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Boots));
        else Debug.LogWarning("[InventoryUI] bootsSlotUI is not assigned!");
        if (glovesSlotUI != null) glovesSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Gloves));
        else Debug.LogWarning("[InventoryUI] glovesSlotUI is not assigned!");
        if (weaponSlotUI != null) weaponSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.Weapon));
        else Debug.LogWarning("[InventoryUI] weaponSlotUI is not assigned!");
        if (offHandSlotUI != null) offHandSlotUI.SetItem(core.Inventory.GetEquipped(EquipmentSlot.OffHand));
        else Debug.LogWarning("[InventoryUI] offHandSlotUI is not assigned!");
        // Equipment UI updated
    }

    public void ShowTooltip(Item item, Vector3 position)
    {
        if (item == null || isTooltipActive) return;
        
        bool canEquip = item.IsEquipable(core.Stats.level, core.Stats.characterClass);
        string color = canEquip ? "#FFFFFF" : "#FF0000";
        string rarityColor = GetRarityColor(item.rarity);
        
        StringBuilder sb = new StringBuilder();
        
        // Имя предмета
        sb.AppendLine($"<size=16><b><color={rarityColor}>{item.itemName}</color></b></size>");
        
        // Защита / урон
        if (item.physicalResist != 0 || item.minAttackConstantBonus != 0 || item.maxAttackConstantBonus != 0)
        {
            string defenseDamage = "";
            if (item.physicalResist != 0)
            {
                defenseDamage += $"Defense (+{item.physicalResist})";
            }
            if (item.minAttackConstantBonus != 0 || item.maxAttackConstantBonus != 0)
            {
                if (defenseDamage != "") defenseDamage += " / ";
                defenseDamage += $"Damage ({item.minAttackConstantBonus}-{item.maxAttackConstantBonus})";
            }
            sb.AppendLine($"<size=11>{defenseDamage}</size>");
        }
        
        // Прочность
        if (item.durability > 0)
        {
            sb.AppendLine($"<size=11>Durability ({item.durability}/{item.durability})</size>");
        }
        
        // Шанс урона / уворот
        if (item.crtConstantBonus != 0)
        {
            sb.AppendLine($"<size=11>Critical Chance (+{item.crtConstantBonus}%)</size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Уровень
        sb.AppendLine($"<size=10>Level: {item.requiredLevel}</size>");
        
        // Класс
        if (item.characterClass != CharacterClass.None)
        {
            sb.AppendLine($"<size=10>Class: {item.characterClass}</size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Основные характеристики
        if (item.strengthBonus != 0)
            sb.AppendLine($"<size=10>Strength: +{item.strengthBonus}</size>");
        if (item.agilityBonus != 0)
            sb.AppendLine($"<size=10>Agility: +{item.agilityBonus}</size>");
        if (item.spiritBonus != 0)
            sb.AppendLine($"<size=10>Spirit: +{item.spiritBonus}</size>");
        if (item.accuracyBonus != 0)
            sb.AppendLine($"<size=10>Accuracy: +{item.accuracyBonus}</size>");
        if (item.constitutionBonus != 0)
            sb.AppendLine($"<size=10>Constitution: +{item.constitutionBonus}</size>");
        
        // Остальные параметры
        if (item.maxHpConstantBonus != 0)
            sb.AppendLine($"<size=10>Health: +{item.maxHpConstantBonus}</size>");
        if (item.maxSpConstantBonus != 0)
            sb.AppendLine($"<size=10>Mana: +{item.maxSpConstantBonus}</size>");
        if (item.mspdConstantBonus != 0)
            sb.AppendLine($"<size=10>Movement Speed: +{item.mspdConstantBonus}</size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Цена продажи
        if (item.price > 0)
        {
            sb.AppendLine($"<size=10>Sell Price: {item.price}</size>");
        }
        
        // Ошибки экипировки
        if (!canEquip)
            sb.AppendLine($"<size=10>Cannot equip: level or class mismatch</size>");
        
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
            
            
            // Настройка фона
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, 0.8f);
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
        
        // Debug.Log($"[InventoryUI] Showing enhanced tooltip for {item.itemName} at position {position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f)}");
    }
    
    public void ShowTooltip(ItemInfo itemInfo, Vector3 position)
    {
        if (itemInfo.id <= 0 || isTooltipActive) return;
        
        Item item = itemInfo.GetItem();
        if (item == null) return;
        
        bool canEquip = item.IsEquipable(core.Stats.level, core.Stats.characterClass);
        string color = canEquip ? "#FFFFFF" : "#FF0000";
        string rarityColor = GetRarityColor(itemInfo.GetItemRarity());
        
        StringBuilder sb = new StringBuilder();
        
        // Имя предмета с динамическими статами
        sb.AppendLine($"<size=16><b><color={rarityColor}>{itemInfo.GetItemName()}</color></b></size>");
        
        // Защита / урон (с динамическими статами)
        int totalPhysicalResist = itemInfo.GetTotalStatBonus(ItemInfo.StatType.PhysicalResist);
        int totalMinAttack = itemInfo.GetTotalStatBonus(ItemInfo.StatType.MinAttack);
        int totalMaxAttack = itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxAttack);
        
        if (totalPhysicalResist != 0 || totalMinAttack != 0 || totalMaxAttack != 0)
        {
            string defenseDamage = "";
            if (totalPhysicalResist != 0)
            {
                defenseDamage += $"Defense (+{totalPhysicalResist})";
            }
            if (totalMinAttack != 0 || totalMaxAttack != 0)
            {
                if (defenseDamage != "") defenseDamage += " / ";
                defenseDamage += $"Damage ({totalMinAttack}-{totalMaxAttack})";
            }
            sb.AppendLine($"<size=11>{defenseDamage}</size>");
        }
        
        // Прочность
        if (item.durability > 0)
        {
            sb.AppendLine($"<size=11>Durability ({item.durability}/{item.durability})</size>");
        }
        
        // Шанс урона / уворот (с динамическими статами)
        int totalCritical = itemInfo.GetTotalStatBonus(ItemInfo.StatType.Critical);
        if (totalCritical != 0)
        {
            sb.AppendLine($"<size=11>Critical Chance (+{totalCritical}%)</size>");
        }
        
        // Пустая строка
        sb.AppendLine("");
        
        // Уровень
        sb.AppendLine($"<size=11>Level: {item.requiredLevel}</size>");
        
        // Класс
        sb.AppendLine($"<size=11>Class: {item.characterClass}</size>");
        
        // Пустая строка
        sb.AppendLine("");
        
        // Основные статы (с динамическими статами)
        int totalStrength = itemInfo.GetTotalStatBonus(ItemInfo.StatType.Strength);
        int totalAgility = itemInfo.GetTotalStatBonus(ItemInfo.StatType.Agility);
        int totalSpirit = itemInfo.GetTotalStatBonus(ItemInfo.StatType.Spirit);
        int totalConstitution = itemInfo.GetTotalStatBonus(ItemInfo.StatType.Constitution);
        int totalAccuracy = itemInfo.GetTotalStatBonus(ItemInfo.StatType.Accuracy);
        
        if (totalStrength != 0 || totalAgility != 0 || totalSpirit != 0 || totalConstitution != 0 || totalAccuracy != 0)
        {
            sb.AppendLine("<size=11><b>Primary Stats:</b></size>");
            if (totalStrength != 0) sb.AppendLine($"<size=11>Strength: +{totalStrength}</size>");
            if (totalAgility != 0) sb.AppendLine($"<size=11>Agility: +{totalAgility}</size>");
            if (totalSpirit != 0) sb.AppendLine($"<size=11>Spirit: +{totalSpirit}</size>");
            if (totalConstitution != 0) sb.AppendLine($"<size=11>Constitution: +{totalConstitution}</size>");
            if (totalAccuracy != 0) sb.AppendLine($"<size=11>Accuracy: +{totalAccuracy}</size>");
            sb.AppendLine("");
        }
        
        // Другие статы (с динамическими статами)
        int totalMaxHP = itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxHP);
        int totalMaxMP = itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxMP);
        int totalMovementSpeed = itemInfo.GetTotalStatBonus(ItemInfo.StatType.MovementSpeed);
        
        if (totalMaxHP != 0 || totalMaxMP != 0 || totalMovementSpeed != 0)
        {
            sb.AppendLine("<size=11><b>Other Stats:</b></size>");
            if (totalMaxHP != 0) sb.AppendLine($"<size=11>Health: +{totalMaxHP}</size>");
            if (totalMaxMP != 0) sb.AppendLine($"<size=11>Mana: +{totalMaxMP}</size>");
            if (totalMovementSpeed != 0) sb.AppendLine($"<size=11>Movement Speed: +{totalMovementSpeed}</size>");
            sb.AppendLine("");
        }
        
        // Цена продажи
        if (item.price > 0)
        {
            sb.AppendLine($"<size=11>Sell Price: {item.price} gold</size>");
        }
        
        tooltipText.text = sb.ToString();
        itemTooltip.SetActive(true);
        
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
            
            // Настройка фона
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, 0.8f);
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
        
        // Debug.Log($"[InventoryUI] Showing enhanced tooltip for {itemInfo.GetItemName()} at position {position + new Vector3(tooltipOffsetX, tooltipOffsetY, 0f)}");
    }

    public void ShowSkillTooltip(SkillBase skill, Vector3 position)
    {
        if (skill == null || isTooltipActive) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<size=16><b>{skill.SkillName}</b></size>");
        sb.AppendLine("─────────────────");
        sb.AppendLine($"<size=11>{skill.Description}</size>");
        sb.AppendLine("─────────────────");
        sb.AppendLine($"<size=10>Mana: {skill.ManaCost}</size>");
        sb.AppendLine($"<size=10>Cooldown: {skill.Cooldown}s</size>");
        sb.AppendLine($"<size=10>Range: {skill.Range}</size>");
        
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
            
            
            // Настройка фона
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, 0.8f);
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
        sb.AppendLine(""); // Пустая строка
        
        // Описание слота
        sb.AppendLine($"<size=11><color=#CCCCCC>{slotDescription}</color></size>");
        sb.AppendLine(""); // Пустая строка
        
        // Инструкция
        sb.AppendLine($"<size=10><color=#AAAAAA>Drag an item here to equip it</color></size>");
        
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
            
            // Настройка фонового изображения
            Image backgroundImage = itemTooltip.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, 0.8f);
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