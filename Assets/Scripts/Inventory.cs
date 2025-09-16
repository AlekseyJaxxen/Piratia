using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.Events;

public class Inventory : NetworkBehaviour
{
    public PlayerCore playerCore;
    public int inventorySize = 20;
    public readonly SyncList<ItemInfo> items = new SyncList<ItemInfo>();
    [SyncVar(hook = nameof(OnHeadChanged))] public ItemInfo headSlot;
    [SyncVar(hook = nameof(OnBodyChanged))] public ItemInfo bodySlot;
    [SyncVar(hook = nameof(OnLegsChanged))] public ItemInfo legsSlot;
    [SyncVar(hook = nameof(OnRightHandChanged))] public ItemInfo rightHandSlot;
    [SyncVar(hook = nameof(OnLeftHandChanged))] public ItemInfo leftHandSlot;
    [SyncVar(hook = nameof(OnRingChanged))] public ItemInfo ringSlot;
    [SyncVar(hook = nameof(OnNecklaceChanged))] public ItemInfo necklaceSlot;
    [SyncVar(hook = nameof(OnBootsChanged))] public ItemInfo bootsSlot;
    [SyncVar(hook = nameof(OnGlovesChanged))] public ItemInfo glovesSlot;
    [SyncVar(hook = nameof(OnWeaponChanged))] public ItemInfo weaponSlot;
    [SyncVar(hook = nameof(OnOffHandChanged))] public ItemInfo offHandSlot;
    [SyncVar(hook = nameof(OnInventoryGoldChanged))] public int gold = 0;
    [HideInInspector] public UnityEvent OnInventoryChanged = new UnityEvent();
    [HideInInspector] public UnityEvent OnGoldChanged = new UnityEvent();
    [HideInInspector] public UnityEvent OnEquipmentChanged = new UnityEvent();

    public void OnItemsListChanged(SyncList<ItemInfo>.Operation op, int index, ItemInfo oldItem, ItemInfo newItem)
    {
        Debug.Log($"[Inventory] Items list changed: op={op}, index={index}");
        OnInventoryChanged.Invoke();
    }

    public void Init(PlayerCore core)
    {
        playerCore = core;
        items.Callback += OnItemsListChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isClient)
        {
            OnInventoryChanged.Invoke();
            OnEquipmentChanged.Invoke();
            OnGoldChanged.Invoke();
        }
    }

    [Server]
    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null || item.id < 0 || quantity <= 0)
        {
            Debug.LogError($"[Inventory] Cannot add item: Item is null or ID is invalid or quantity <=0 ({quantity})");
            return false;
        }
        bool added = false;
        bool isStackable = item.stackable && item.maxStack > 1;
        if (isStackable)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == item.id)
                {
                    int newQuantity = items[i].quantity + quantity;
                    if (newQuantity <= item.maxStack)
                    {
                        ItemInfo updatedItemInfo = items[i];
                        updatedItemInfo.quantity = newQuantity;
                        items[i] = updatedItemInfo;
                        Debug.Log($"[Inventory] Added {quantity} to existing stack of {item.itemName}. New total: {newQuantity}");
                        added = true;
                        break;
                    }
                }
            }
        }
        if (!added)
        {
            int emptyIndex = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == 0)
                {
                    emptyIndex = i;
                    break;
                }
            }
            if (emptyIndex >= 0)
            {
                ItemInfo newInfo = new ItemInfo { id = item.id, quantity = quantity };
                items[emptyIndex] = newInfo;
                Debug.Log($"[Inventory] Added new item: {item.itemName} (ID: {item.id}, quantity: {quantity}) to empty slot {emptyIndex}");
                added = true;
            }
            else if (items.Count < inventorySize)
            {
                items.Add(new ItemInfo { id = item.id, quantity = quantity });
                Debug.Log($"[Inventory] Added new item: {item.itemName} (ID: {item.id}, quantity: {quantity}) to new slot");
                added = true;
            }
            else
            {
                Debug.LogWarning($"[Inventory] Cannot add item: {item.itemName}, inventory full");
            }
        }
        return added;
    }

    [Server]
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged.Invoke();
        Debug.Log($"[Inventory] Added {amount} gold, total: {gold}");
    }

    [Server]
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || gold < amount) return false;
        gold -= amount;
        OnGoldChanged.Invoke();
        Debug.Log($"[Inventory] Spent {amount} gold, remaining: {gold}");
        return true;
    }

    [Server]
    public void EquipItem(ItemInfo itemInfo, EquipmentSlot slot, int slotIndex)
    {
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogError($"[Inventory] Cannot equip item: Item with ID {itemInfo.id} not found");
            return;
        }
        if (item.equipmentSlot != slot)
        {
            Debug.LogError($"[Inventory] Cannot equip item: {item.itemName} slot {item.equipmentSlot} does not match {slot}");
            return;
        }
        if (!item.IsEquipable(playerCore.Stats.level))
        {
            Debug.LogError($"[Inventory] Cannot equip item: {item.itemName}, player level {playerCore.Stats.level} is less than required level {item.requiredLevel}");
            return;
        }
        Debug.Log($"[Inventory] Equipping item: {item.itemName} (ID: {itemInfo.id}) to {slot} from slot {slotIndex}");
        ItemInfo oldItem = GetEquipped(slot);
        if (oldItem.id > 0)
        {
            Item oldItemObj = oldItem.GetItem();
            if (oldItemObj != null)
            {
                Debug.Log($"[Inventory] Unequipping old item: {oldItemObj.itemName} from {slot} to inventory");
                ApplyItemStats(oldItemObj, false);
                AddItem(oldItemObj, oldItem.quantity);
            }
        }
        SetEquipped(slot, itemInfo);
        ApplyItemStats(item, true);
        ClearItemSlot(slotIndex);
    }

    [Server]
    public void UnequipItem(EquipmentSlot slot)
    {
        ItemInfo itemInfo = GetEquipped(slot);
        if (itemInfo.id <= 0 || itemInfo.quantity <= 0)
        {
            SetEquipped(slot, new ItemInfo());
            return;
        }
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogError($"[Inventory] Cannot unequip item: Item with ID {itemInfo.id} not found");
            SetEquipped(slot, new ItemInfo());
            return;
        }
        Debug.Log($"[Inventory] Unequipping item: {item.itemName} from {slot}, quantity: {itemInfo.quantity}");
        ApplyItemStats(item, false);
        AddItem(item, itemInfo.quantity);
        SetEquipped(slot, new ItemInfo());
    }

    private void ApplyItemStats(Item item, bool apply)
    {
        int mod = apply ? 1 : -1;
        CharacterStats stats = playerCore.Stats;
        stats.strength += (item.strengthMod + item.strModulusBonus + item.strConstantBonus) * mod;
        stats.agility += (item.agilityMod + item.agiModulusBonus + item.agiConstantBonus) * mod;
        stats.spirit += (item.spiritMod + item.sprModulusBonus) * mod;
        stats.constitution += (item.constitutionMod + item.conModulusBonus + item.conConstantBonus) * mod;
        stats.accuracy += (item.accuracyMod + item.hitRateModulusBonus + item.hitModulusBonus + item.hitConstantBonus) * mod;
        stats.intelligence += (item.intelligenceMod + item.agiModulusBonus + item.agiConstantBonus) * mod;
        stats.maxHealth += (item.maxHpModulusBonus + item.maxHpConstantBonus) * mod;
        stats.maxMana += (item.maxSpModulusBonus + item.maxSpConstantBonus) * mod;
        stats.armor += (item.defenseModulusBonus + item.physicalResist) * mod;
        stats.criticalHitChance += (item.crtModulusBonus + item.crtConstantBonus) * mod;
        stats.movementSpeed += (item.mspdModulusBonus + item.mspdConstantBonus) * mod;
        stats.CalculateDerivedStats();
        Debug.Log($"[Inventory] Applied stats for {item.itemName} (apply={apply}): strength={item.strengthMod + item.strModulusBonus + item.strConstantBonus}, agility={item.agilityMod + item.agiModulusBonus + item.agiConstantBonus}, spirit={item.spiritMod + item.sprModulusBonus}, constitution={item.constitutionMod + item.conModulusBonus + item.conConstantBonus}, accuracy={item.accuracyMod + item.hitRateModulusBonus + item.hitModulusBonus + item.hitConstantBonus}, intelligence={item.intelligenceMod + item.agiModulusBonus + item.agiConstantBonus}, maxHealth={item.maxHpModulusBonus + item.maxHpConstantBonus}, maxMana={item.maxSpModulusBonus + item.maxSpConstantBonus}, armor={item.defenseModulusBonus + item.physicalResist}, criticalHitChance={item.crtModulusBonus + item.crtConstantBonus}, movementSpeed={item.mspdModulusBonus + item.mspdConstantBonus}");
    }

    private ItemInfo GetEquipped(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Head: return headSlot;
            case EquipmentSlot.Body: return bodySlot;
            case EquipmentSlot.Legs: return legsSlot;
            case EquipmentSlot.RightHand: return rightHandSlot;
            case EquipmentSlot.LeftHand: return leftHandSlot;
            case EquipmentSlot.Ring: return ringSlot;
            case EquipmentSlot.Necklace: return necklaceSlot;
            case EquipmentSlot.Boots: return bootsSlot;
            case EquipmentSlot.Gloves: return glovesSlot;
            case EquipmentSlot.Weapon: return weaponSlot;
            case EquipmentSlot.OffHand: return offHandSlot;
            default: return new ItemInfo();
        }
    }

    private void SetEquipped(EquipmentSlot slot, ItemInfo info)
    {
        Item item = info.GetItem();
        switch (slot)
        {
            case EquipmentSlot.Head: headSlot = info; break;
            case EquipmentSlot.Body: bodySlot = info; break;
            case EquipmentSlot.Legs: legsSlot = info; break;
            case EquipmentSlot.RightHand: rightHandSlot = info; break;
            case EquipmentSlot.LeftHand: leftHandSlot = info; break;
            case EquipmentSlot.Ring: ringSlot = info; break;
            case EquipmentSlot.Necklace: necklaceSlot = info; break;
            case EquipmentSlot.Boots: bootsSlot = info; break;
            case EquipmentSlot.Gloves: glovesSlot = info; break;
            case EquipmentSlot.Weapon: weaponSlot = info; break;
            case EquipmentSlot.OffHand: offHandSlot = info; break;
        }
        OnEquipmentChanged.Invoke();
        Debug.Log($"[Inventory] Set equipped item: {(item != null ? item.itemName : "none")} (ID: {info.id}) to {slot}");
    }

    public Item[] GetEquippedItems()
    {
        List<Item> equippedItems = new List<Item>();
        ItemInfo[] slots = { headSlot, bodySlot, legsSlot, rightHandSlot, leftHandSlot, ringSlot, necklaceSlot, bootsSlot, glovesSlot, weaponSlot, offHandSlot };
        foreach (var slot in slots)
        {
            Item item = slot.GetItem();
            if (item != null)
            {
                equippedItems.Add(item);
            }
        }
        return equippedItems.ToArray();
    }

    private void OnHeadChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnBodyChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnLegsChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnRightHandChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnLeftHandChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnRingChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnNecklaceChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnBootsChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnGlovesChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnWeaponChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnOffHandChanged(ItemInfo oldItem, ItemInfo newItem) { OnEquipmentChanged.Invoke(); }
    private void OnInventoryGoldChanged(int oldGold, int newGold) { OnGoldChanged.Invoke(); }

    [Server]
    public void ClearItemSlot(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            Debug.LogError($"[Inventory] Cannot clear item slot: Index {index} is out of bounds.");
            return;
        }
        items[index] = new ItemInfo { id = 0, quantity = 0 };
        Debug.Log($"[Inventory] Cleared item slot at index: {index}");
    }
}