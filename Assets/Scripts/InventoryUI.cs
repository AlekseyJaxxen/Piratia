using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;

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
    [SerializeField] private GameObject itemTooltip;
    [SerializeField] private TextMeshProUGUI tooltipText;
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
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>{item.itemName}</b>");
        sb.AppendLine($"Type: {item.itemType}");
        sb.AppendLine($"Rarity: {item.rarity}");
        sb.AppendLine($"<color={color}>Required Level: {item.requiredLevel}</color>");
        if (item.characterClass != CharacterClass.None)
            sb.AppendLine($"Class: {item.characterClass}");
        if (item.equipmentSlot != EquipmentSlot.None)
            sb.AppendLine($"Slot: {item.equipmentSlot}");
        if (item.isTwoHanded)
            sb.AppendLine($"Two-Handed: Occupies {item.equipmentSlot} and {item.alternativeSlot}");
        if (item.strengthMod != 0 || item.strModulusBonus != 0 || item.strConstantBonus != 0)
            sb.AppendLine($"<color=#FFD700>Strength: +{item.strengthMod + item.strModulusBonus + item.strConstantBonus}</color>");
        if (item.agilityMod != 0 || item.agiModulusBonus != 0 || item.agiConstantBonus != 0)
            sb.AppendLine($"<color=#00FF00>Agility: +{item.agilityMod + item.agiModulusBonus + item.agiConstantBonus}</color>");
        if (item.spiritMod != 0 || item.sprModulusBonus != 0)
            sb.AppendLine($"<color=#00FFFF>Spirit: +{item.spiritMod + item.sprModulusBonus}</color>");
        if (item.constitutionMod != 0 || item.conModulusBonus != 0 || item.conConstantBonus != 0)
            sb.AppendLine($"<color=#FF4500>Constitution: +{item.constitutionMod + item.conModulusBonus + item.conConstantBonus}</color>");
        if (item.accuracyMod != 0 || item.hitRateModulusBonus != 0 || item.hitModulusBonus != 0 || item.hitConstantBonus != 0)
            sb.AppendLine($"<color=#FFFF00>Accuracy: +{item.accuracyMod + item.hitRateModulusBonus + item.hitModulusBonus + item.hitConstantBonus}</color>");
        if (item.intelligenceMod != 0 || item.agiModulusBonus != 0 || item.agiConstantBonus != 0)
            sb.AppendLine($"<color=#FF00FF>Intelligence: +{item.intelligenceMod + item.agiModulusBonus + item.agiConstantBonus}</color>");
        if (item.minAttackConstantBonus != 0)
            sb.AppendLine($"Min Attack: {item.minAttackConstantBonus}");
        if (item.maxAttackConstantBonus != 0)
            sb.AppendLine($"Max Attack: {item.maxAttackConstantBonus}");
        if (item.maxHpModulusBonus != 0 || item.maxHpConstantBonus != 0)
            sb.AppendLine($"Max Health: +{item.maxHpModulusBonus + item.maxHpConstantBonus}");
        if (item.maxSpModulusBonus != 0 || item.maxSpConstantBonus != 0)
            sb.AppendLine($"Max Mana: +{item.maxSpModulusBonus + item.maxSpConstantBonus}");
        if (item.defenseModulusBonus != 0 || item.physicalResist != 0)
            sb.AppendLine($"Defense: +{item.defenseModulusBonus + item.physicalResist}");
        if (item.crtModulusBonus != 0 || item.crtConstantBonus != 0)
            sb.AppendLine($"Critical Chance: +{item.crtModulusBonus + item.crtConstantBonus}%");
        if (item.mspdModulusBonus != 0 || item.mspdConstantBonus != 0)
            sb.AppendLine($"Movement Speed: +{item.mspdModulusBonus + item.mspdConstantBonus}");
        if (item.durability > 0)
            sb.AppendLine($"Durability: {item.durability}");
        if (item.description != "0")
            sb.AppendLine($"\n{item.description}");
        if (!canEquip)
            sb.AppendLine($"<color=#FF0000>Cannot equip: Level or class mismatch</color>");
        itemTooltip.SetActive(true);
        tooltipText.text = sb.ToString().TrimEnd('\n');
        itemTooltip.transform.position = position + new Vector3(100f, 0f, 0f);
        isTooltipActive = true;
        Debug.Log($"[InventoryUI] Showing tooltip for {item.itemName} at position {position}, Rarity: {item.rarity}, Level: {item.requiredLevel}, Stats: {sb}");
    }

    public void ShowSkillTooltip(SkillBase skill, Vector3 position)
    {
        if (skill == null || isTooltipActive) return;
        string newText = $"{skill.SkillName}\n" +
                         $"Description: {skill.Description}\n" +
                         $"Mana Cost: {skill.ManaCost}\n" +
                         $"Cooldown: {skill.Cooldown}\n" +
                         $"Range: {skill.Range}";
        itemTooltip.SetActive(true);
        tooltipText.text = newText;
        itemTooltip.transform.position = position + new Vector3(100f, 0f, 0f);
        isTooltipActive = true;
        Debug.Log($"[InventoryUI] Showing tooltip for skill {skill.SkillName} at position {position}");
    }

    public void HideTooltip()
    {
        if (!isTooltipActive) return;
        itemTooltip.SetActive(false);
        isTooltipActive = false;
        Debug.Log($"[InventoryUI] Hiding tooltip, caller={new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");
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