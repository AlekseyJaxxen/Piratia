using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

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
            Debug.Log($"[InventoryUI] Panels set to {newState}");
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
        headSlotUI.SetItem(inventory.headSlot);
        bodySlotUI.SetItem(inventory.bodySlot);
        legsSlotUI.SetItem(inventory.legsSlot);
        rightHandSlotUI.SetItem(inventory.rightHandSlot);
        leftHandSlotUI.SetItem(inventory.leftHandSlot);
    }

    public void ShowTooltip(Item item, Vector3 position)
    {
        if (item == null || isTooltipActive) return;
        string newText = $"<b>{item.itemName ?? "Unknown Item"}</b>\n" +
                         $"Type: {item.itemType.ToString()}\n"; // ”брано ?. и ?? "None"
        if (item.equipmentSlot != EquipmentSlot.None) newText += $"Slot: {item.equipmentSlot}\n";
        if (item.strengthMod != 0) newText += $"<color=#FFD700>Strength: {item.strengthMod}</color>\n";
        if (item.agilityMod != 0) newText += $"<color=#00FF00>Agility: {item.agilityMod}</color>\n";
        if (item.spiritMod != 0) newText += $"<color=#00FFFF>Spirit: {item.spiritMod}</color>\n";
        if (item.constitutionMod != 0) newText += $"<color=#FF4500>Constitution: {item.constitutionMod}</color>\n";
        if (item.accuracyMod != 0) newText += $"<color=#FFFF00>Accuracy: {item.accuracyMod}</color>\n";
        if (item.intelligenceMod != 0) newText += $"<color=#FF00FF>Intelligence: {item.intelligenceMod}</color>\n";
        itemTooltip.SetActive(true);
        tooltipText.text = newText.TrimEnd('\n');
        itemTooltip.transform.position = position + new Vector3(100f, 0f, 0f);
        isTooltipActive = true;
        Debug.Log($"[InventoryUI] Showing tooltip for {item.itemName ?? "null"} at position {position}, Mods: S:{item.strengthMod}, A:{item.agilityMod}, Sp:{item.spiritMod}, C:{item.constitutionMod}, Ac:{item.accuracyMod}, I:{item.intelligenceMod}");
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
            case EquipmentSlot.Head: headSlotUI.SetItem(itemInfo); break;
            case EquipmentSlot.Body: bodySlotUI.SetItem(itemInfo); break;
            case EquipmentSlot.Legs: legsSlotUI.SetItem(itemInfo); break;
            case EquipmentSlot.RightHand: rightHandSlotUI.SetItem(itemInfo); break;
            case EquipmentSlot.LeftHand: leftHandSlotUI.SetItem(itemInfo); break;
        }
        Debug.Log($"[InventoryUI] Updated equipment slot {slot}");
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

    public EquipmentSlotUI FindMatchingEquipmentSlot(EquipmentSlot slotType)
    {
        switch (slotType)
        {
            case EquipmentSlot.Head: return headSlotUI;
            case EquipmentSlot.Body: return bodySlotUI;
            case EquipmentSlot.Legs: return legsSlotUI;
            case EquipmentSlot.RightHand: return rightHandSlotUI;
            case EquipmentSlot.LeftHand: return leftHandSlotUI;
            default: return null;
        }
    }
}