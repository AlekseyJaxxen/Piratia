using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Mirror;
using System.Collections.Generic;

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
    [SerializeField] private Button dropButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button hotbarButton;

    private PlayerCore core;
    private Inventory inventory;
    private RectTransform inventoryPanelRect;
    private Vector2 dragOffset;
    public InventorySlot draggedSlot; // Изменено на public

    private void Awake()
    {
        Instance = this;
        if (inventoryPanel != null) inventoryPanelRect = inventoryPanel.GetComponent<RectTransform>();
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

        if (closeInventoryButton != null)
            closeInventoryButton.onClick.AddListener(() => inventoryPanel.SetActive(false));
        if (closeCharacterButton != null)
            closeCharacterButton.onClick.AddListener(() => characterPanel.SetActive(false));
        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropButtonClicked);
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellButtonClicked);
        if (useButton != null)
            useButton.onClick.AddListener(OnUseButtonClicked);
        if (hotbarButton != null)
            hotbarButton.onClick.AddListener(OnHotbarButtonClicked);

        UpdateInventoryUI();
        UpdateEquipmentUI();
    }

    private void Update()
    {
        if (!core.isLocalPlayer || core.isDead || core.isStunned) return;
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool newState = !inventoryPanel.activeSelf;
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
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i < inventory.items.Count)
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
        if (item == null) return;
        itemTooltip.SetActive(true);
        tooltipText.text = $"{item.itemName}\n" +
                          $"Type: {item.itemType}\n" +
                          (item.equipmentSlot != EquipmentSlot.None ? $"Slot: {item.equipmentSlot}\n" : "") +
                          $"Strength: {item.strengthMod}\n" +
                          $"Agility: {item.agilityMod}\n" +
                          $"Spirit: {item.spiritMod}\n" +
                          $"Constitution: {item.constitutionMod}\n" +
                          $"Accuracy: {item.accuracyMod}\n" +
                          $"Intelligence: {item.intelligenceMod}";
        itemTooltip.transform.position = position;
    }

    public void HideTooltip()
    {
        itemTooltip.SetActive(false);
    }

    private void OnDropButtonClicked()
    {
        if (draggedSlot == null || draggedSlot.itemInstance.item == null || !draggedSlot.itemInstance.item.canDrop) return;
        core.CmdDropItem(draggedSlot.itemInstance.item, draggedSlot.slotIndex);
        draggedSlot = null;
    }

    private void OnSellButtonClicked()
    {
        if (draggedSlot == null || draggedSlot.itemInstance.item == null || !draggedSlot.itemInstance.item.canSell) return;
        core.CmdSellItem(draggedSlot.itemInstance.item, draggedSlot.slotIndex);
        draggedSlot = null;
    }

    private void OnUseButtonClicked()
    {
        if (draggedSlot == null || draggedSlot.itemInstance.item == null || !draggedSlot.itemInstance.item.canUse) return;
        core.CmdUseItem(draggedSlot.itemInstance.item, draggedSlot.slotIndex);
        draggedSlot = null;
    }

    private void OnHotbarButtonClicked()
    {
        if (draggedSlot == null || draggedSlot.itemInstance.item == null || !draggedSlot.itemInstance.item.canHotbar) return;
        SkillButton emptyButton = null;
        foreach (var btn in PlayerUI.Instance.GetSkillButtons2()) // Используем метод доступа
        {
            if (btn.skill == null && btn.item == null)
            {
                emptyButton = btn;
                break;
            }
        }
        if (emptyButton == null)
        {
            foreach (var btn in PlayerUI.Instance.GetSkillButtons3())
            {
                if (btn.skill == null && btn.item == null)
                {
                    emptyButton = btn;
                    break;
                }
            }
        }
        if (emptyButton != null)
        {
            PlayerUI.Instance.AssignItemToHotbar(draggedSlot.itemInstance.item, emptyButton);
            core.CmdUseItem(draggedSlot.itemInstance.item, draggedSlot.slotIndex);
        }
        draggedSlot = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryPanelRect != null)
        {
            dragOffset = inventoryPanelRect.position - (Vector3)eventData.position;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryPanelRect != null)
        {
            inventoryPanelRect.position = eventData.position + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
}