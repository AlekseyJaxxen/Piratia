// InventoryUI.cs - полная версия, исправил дубликаты хуков + изменил listener для gold
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Mirror;
using System.Collections.Generic;
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
            inventory.OnGoldChanged.AddListener(OnGoldUIChanged); // Используем метод
            inventory.OnEquipmentChanged.AddListener(UpdateEquipmentUI);
        }
        if (closeInventoryButton != null)
            closeInventoryButton.onClick.AddListener(() => inventoryPanel.SetActive(false));
        if (closeCharacterButton != null)
            closeCharacterButton.onClick.AddListener(() => characterPanel.SetActive(false));
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
            UpdateInventoryUI();
            UpdateEquipmentUI();
            inventoryPanel.SetActive(newState);
            UpdateInventoryUI();
            Debug.Log($"[InventoryUI] InventoryPanel set to {newState}");
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            bool newState = !characterPanel.activeSelf;
            characterPanel.SetActive(newState);
            UpdateEquipmentUI();
            Debug.Log($"[InventoryUI] CharacterPanel set to {newState}");
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
        string newText = $"{item.itemName}\n" +
                         $"Type: {item.itemType}\n" +
                         (item.equipmentSlot != EquipmentSlot.None ? $"Slot: {item.equipmentSlot}\n" : "") +
                         $"Strength: {item.strengthMod}\n" +
                         $"Agility: {item.agilityMod}\n" +
                         $"Spirit: {item.spiritMod}\n" +
                         $"Constitution: {item.constitutionMod}\n" +
                         $"Accuracy: {item.accuracyMod}\n" +
                         $"Intelligence: {item.intelligenceMod}";
        itemTooltip.SetActive(true);
        tooltipText.text = newText;
        itemTooltip.transform.position = position;
        isTooltipActive = true;
        Debug.Log($"[InventoryUI] Showing tooltip for {item.itemName} at position {position}");
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
        RectTransform activePanelRect = null;
        if (inventoryPanel.activeSelf && IsPointerOverRect(eventData, inventoryPanelRect))
        {
            activePanelRect = inventoryPanelRect;
        }
        else if (characterPanel.activeSelf && IsPointerOverRect(eventData, characterPanelRect))
        {
            activePanelRect = characterPanelRect;
        }
        if (activePanelRect != null)
        {
            dragOffset = activePanelRect.position - (Vector3)eventData.position;
            Debug.Log($"[InventoryUI] Begin drag on {(activePanelRect == inventoryPanelRect ? "InventoryPanel" : "CharacterPanel")}");
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        RectTransform activePanelRect = null;
        if (inventoryPanel.activeSelf && IsPointerOverRect(eventData, inventoryPanelRect))
        {
            activePanelRect = inventoryPanelRect;
        }
        else if (characterPanel.activeSelf && IsPointerOverRect(eventData, characterPanelRect))
        {
            activePanelRect = characterPanelRect;
        }
        if (activePanelRect != null)
        {
            activePanelRect.position = eventData.position + dragOffset;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        RectTransform activePanelRect = null;
        if (inventoryPanel.activeSelf && IsPointerOverRect(eventData, inventoryPanelRect))
        {
            activePanelRect = inventoryPanelRect;
        }
        else if (characterPanel.activeSelf && IsPointerOverRect(eventData, characterPanelRect))
        {
            activePanelRect = characterPanelRect;
        }
        if (activePanelRect != null)
        {
            Debug.Log($"[InventoryUI] End drag on {(activePanelRect == inventoryPanelRect ? "InventoryPanel" : "CharacterPanel")}");
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
    private void OnGoldUIChanged() // Новый метод для gold
    {
        if (goldText != null) goldText.text = $"Gold: {inventory.gold}";
    }
}