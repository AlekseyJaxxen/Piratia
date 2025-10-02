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
    private PlayerEquipmentVisuals visuals;

    public void Init(PlayerCore core)
    {
        playerCore = core;
        items.Callback += OnItemsListChanged;
        while (items.Count < inventorySize)
        {
            items.Add(new ItemInfo { id = 0, quantity = 0 });
        }
        visuals = GetComponent<PlayerEquipmentVisuals>();
        if (visuals == null)
        {
            Debug.LogError("[Inventory] PlayerEquipmentVisuals component not found!");
        }
        else
        {
            visuals.Init(core);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isClient)
        {
            OnInventoryChanged.Invoke();
            OnEquipmentChanged.Invoke();
            OnGoldChanged.Invoke();
            UpdateEquipmentVisuals();
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
        int remaining = quantity;
        if (isStackable)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == item.id && items[i].quantity < item.maxStack)
                {
                    int space = item.maxStack - items[i].quantity;
                    int addAmount = Mathf.Min(remaining, space);
                    ItemInfo updatedItemInfo = items[i];
                    updatedItemInfo.quantity += addAmount;
                    items[i] = updatedItemInfo;
                    remaining -= addAmount;
                    // Added to existing stack
                    added = true;
                    if (remaining <= 0) break;
                }
            }
        }
        while (remaining > 0 && !added)
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
                int addAmount = isStackable ? Mathf.Min(remaining, item.maxStack) : remaining;
                ItemInfo newInfo = new ItemInfo { id = item.id, quantity = addAmount };
                items[emptyIndex] = newInfo;
                remaining -= addAmount;
                // Added new item to empty slot
                added = true;
            }
            else
            {
                Debug.LogWarning($"[Inventory] Cannot add item: {item.itemName}, inventory full");
                break;
            }
        }
        if (added) OnInventoryChanged.Invoke();
        return added && remaining == 0;
    }

    [Server]
    public bool AddItemInfo(ItemInfo itemInfo)
    {
        if (itemInfo.id < 0 || itemInfo.quantity <= 0)
        {
            Debug.LogError($"[Inventory] Cannot add item: ID is invalid or quantity <=0 ({itemInfo.quantity})");
            return false;
        }
        
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogError($"[Inventory] Cannot add item: Item with ID {itemInfo.id} not found");
            return false;
        }
        
        bool isStackable = item.stackable && item.maxStack > 1;
        int remaining = itemInfo.quantity;
        bool added = false;
        
        // Если предмет имеет динамические статы, он НЕ стакается
        if (itemInfo.hasDynamicStats)
        {
            isStackable = false;
        }
        
        if (isStackable)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == item.id && items[i].quantity < item.maxStack && !items[i].hasDynamicStats)
                {
                    int space = item.maxStack - items[i].quantity;
                    int addAmount = Mathf.Min(remaining, space);
                    ItemInfo updatedItemInfo = items[i];
                    updatedItemInfo.quantity += addAmount;
                    items[i] = updatedItemInfo;
                    remaining -= addAmount;
                    added = true;
                    if (remaining <= 0) break;
                }
            }
        }
        
        while (remaining > 0)
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
                int addAmount = isStackable ? Mathf.Min(remaining, item.maxStack) : 1;
                ItemInfo newInfo = itemInfo;
                newInfo.quantity = addAmount;
                items[emptyIndex] = newInfo;
                remaining -= addAmount;
                added = true;
            }
            else
            {
                Debug.LogWarning($"[Inventory] Cannot add item: {item.itemName}, inventory full");
                break;
            }
        }
        if (added) OnInventoryChanged.Invoke();
        return added && remaining == 0;
    }

    [Server]
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged.Invoke();
        // Gold added
    }

    [Server]
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || gold < amount) return false;
        gold -= amount;
        OnGoldChanged.Invoke();
        // Gold spent
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
        if (!item.CanEquipToSlot(slot))
        {
            Debug.LogError($"[Inventory] Cannot equip item: {item.itemName} cannot be equipped to slot {slot}");
            return;
        }
        if (!item.IsEquipable(playerCore.Stats.level, playerCore.Stats.characterClass))
        {
            Debug.LogError($"[Inventory] Cannot equip item: {item.itemName}, player level {playerCore.Stats.level} or class {playerCore.Stats.characterClass} does not match required level {item.requiredLevel} or class {item.characterClass}");
            return;
        }
        if (slotIndex < 0 || slotIndex >= items.Count)
        {
            Debug.LogError($"[Inventory] Cannot equip item: {item.itemName} (ID: {itemInfo.id}), invalid slot index {slotIndex}");
            return;
        }
        ItemInfo slotItem = items[slotIndex];
        if (slotItem.id != itemInfo.id || slotItem.quantity <= 0)
        {
            Debug.LogError($"[Inventory] Cannot equip item: {item.itemName} (ID: {itemInfo.id}), item mismatch (expected ID: {itemInfo.id}, found ID: {slotItem.id}, quantity: {slotItem.quantity}) at slot {slotIndex}");
            Debug.LogError($"[Inventory] Inventory state: items count={items.Count}, slot {slotIndex} has item with ID={slotItem.id}, quantity={slotItem.quantity}");
            
            // Попробуем найти предмет в инвентаре по ID
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == itemInfo.id && items[i].quantity > 0)
                {
                    Debug.LogWarning($"[Inventory] Found item {item.itemName} (ID: {itemInfo.id}) at slot {i} instead of {slotIndex}, attempting to equip from correct slot");
                    EquipItem(itemInfo, slot, i);
                    return;
                }
            }
            
            Debug.LogError($"[Inventory] Item {item.itemName} (ID: {itemInfo.id}) not found in inventory at all!");
            return;
        }
        // Обработка двуручного оружия
        if (item.isTwoHanded)
        {
            // Двуручное оружие всегда экипируется в левую руку и блокирует правую
            if (slot != EquipmentSlot.LeftHand)
            {
                Debug.LogError($"[Inventory] Two-handed weapon {item.itemName} can only be equipped in LeftHand slot");
                return;
            }
            
            // Освобождаем правую руку
            ItemInfo rightHandItem = GetEquipped(EquipmentSlot.RightHand);
            if (rightHandItem.id > 0)
            {
                Item rightHandItemObj = rightHandItem.GetItem();
                if (rightHandItemObj != null)
                {
                    Debug.Log($"[Inventory] Unequipping item from RightHand to inventory due to two-handed item {item.itemName}");
                    ApplyItemStats(rightHandItemObj, false);
                    if (!AddItemInfo(rightHandItem))
                    {
                        Debug.LogWarning($"[Inventory] Failed to add unequipped item {rightHandItem.GetItemName()} from RightHand to inventory");
                        return;
                    }
                    SetEquipped(EquipmentSlot.RightHand, new ItemInfo());
                }
            }
        }
        else if (slot == EquipmentSlot.LeftHand || slot == EquipmentSlot.RightHand)
        {
            // Обычное оружие - проверяем конфликты с двуручным и предпочтения руки
            EquipmentSlot otherSlot = (slot == EquipmentSlot.LeftHand) ? EquipmentSlot.RightHand : EquipmentSlot.LeftHand;
            ItemInfo otherSlotItem = GetEquipped(otherSlot);
            if (otherSlotItem.id > 0)
            {
                Item otherItemObj = otherSlotItem.GetItem();
                if (otherItemObj != null && otherItemObj.isTwoHanded)
                {
                    Debug.Log($"[Inventory] Unequipping two-handed item: {otherSlotItem.GetItemName()} from {otherSlot} to inventory");
                    ApplyItemStats(otherItemObj, false);
                    if (!AddItemInfo(otherSlotItem))
                    {
                        Debug.LogWarning($"[Inventory] Failed to add unequipped item {otherSlotItem.GetItemName()} from {otherSlot} to inventory");
                        return;
                    }
                    SetEquipped(otherSlot, new ItemInfo());
                }
            }
            
            // Проверяем предпочтения руки для одноручного оружия
            if (!item.isTwoHanded && item.itemType == ItemType.Weapon)
            {
                if (!item.preferRightHand && slot == EquipmentSlot.RightHand)
                {
                    Debug.LogError($"[Inventory] Weapon {item.itemName} can only be equipped in LeftHand (preferRightHand=false)");
                    return;
                }
            }
        }
        ItemInfo oldItem = GetEquipped(slot);
        if (oldItem.id > 0)
        {
            Debug.Log($"[Inventory] Unequipping old item: {oldItem.GetItemName()} from {slot} to inventory");
            ApplyItemInfoStats(oldItem, false);
            if (!AddItemInfo(oldItem))
            {
                Debug.LogWarning($"[Inventory] Failed to add unequipped item {oldItem.GetItemName()} back to inventory");
                return;
            }
        }
        Debug.Log($"[Inventory] Equipping item: {item.itemName} (ID: {itemInfo.id}) to {slot} from slot {slotIndex}");
        SetEquipped(slot, itemInfo);
        ApplyItemInfoStats(itemInfo, true);
        ClearItemSlot(slotIndex);
    }

    [Server]
    public void UnequipItem(EquipmentSlot slot)
    {
        ItemInfo itemInfo = GetEquipped(slot);
        if (itemInfo.id <= 0 || itemInfo.quantity <= 0)
        {
            Debug.Log($"[Inventory] No item to unequip in slot {slot}");
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
        Debug.Log($"[Inventory] Unequipping item: {itemInfo.GetItemName()} from {slot}, quantity: {itemInfo.quantity}");
        SetEquipped(slot, new ItemInfo());
        ApplyItemInfoStats(itemInfo, false);
        if (!AddItemInfo(itemInfo))
        {
            Debug.LogWarning($"[Inventory] Failed to add unequipped item {itemInfo.GetItemName()} back to inventory");
            return;
        }
        if (item.isTwoHanded && item.alternativeSlot != EquipmentSlot.None)
        {
            ItemInfo otherSlotItem = GetEquipped(item.alternativeSlot);
            if (otherSlotItem.id > 0)
            {
                Debug.Log($"[Inventory] Unequipping second slot: {otherSlotItem.GetItemName()} from {item.alternativeSlot}");
                ApplyItemInfoStats(otherSlotItem, false);
                if (!AddItemInfo(otherSlotItem))
                {
                    Debug.LogWarning($"[Inventory] Failed to add unequipped item {otherSlotItem.GetItemName()} from {item.alternativeSlot} to inventory");
                }
                SetEquipped(item.alternativeSlot, new ItemInfo());
            }
        }
    }

    [Server]
    public void SwapItems(int slotIndex1, int slotIndex2)
    {
        if (slotIndex1 < 0 || slotIndex2 < 0 || slotIndex1 >= items.Count || slotIndex2 >= items.Count)
        {
            Debug.LogError($"[Inventory] Cannot swap items: Invalid indices {slotIndex1}/{slotIndex2}");
            return;
        }
        ItemInfo temp = items[slotIndex1];
        items[slotIndex1] = items[slotIndex2];
        items[slotIndex2] = temp;
        Debug.Log($"[Inventory] Swapped items: slot {slotIndex1} <-> slot {slotIndex2}");
        OnInventoryChanged.Invoke();
    }

    [Server]
    public void StackItems(int fromIndex, int toIndex, int maxAdd)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= items.Count || toIndex >= items.Count)
        {
            Debug.LogError($"[Inventory] Cannot stack items: Invalid indices {fromIndex}/{toIndex}");
            return;
        }
        ItemInfo fromItem = items[fromIndex];
        ItemInfo toItem = items[toIndex];
        if (fromItem.id != toItem.id || fromItem.id <= 0)
        {
            Debug.LogError($"[Inventory] Cannot stack items: IDs don't match ({fromItem.id} != {toItem.id}) or invalid ID");
            return;
        }
        Item item = fromItem.GetItem();
        if (item == null || !item.stackable)
        {
            Debug.LogError($"[Inventory] Cannot stack item: {item?.itemName ?? "null"} is not stackable or null");
            return;
        }
        int quantityToAdd = Mathf.Min(maxAdd, fromItem.quantity);
        ItemInfo updatedToItem = toItem;
        updatedToItem.quantity += quantityToAdd;
        items[toIndex] = updatedToItem;
        ItemInfo updatedFromItem = fromItem;
        updatedFromItem.quantity -= quantityToAdd;
        if (updatedFromItem.quantity <= 0)
        {
            updatedFromItem = new ItemInfo { id = 0, quantity = 0 };
        }
        items[fromIndex] = updatedFromItem;
        Debug.Log($"[Inventory] Stacked {quantityToAdd} of {item.itemName} from slot {fromIndex} to {toIndex}, new quantity: {items[toIndex].quantity}");
        OnInventoryChanged.Invoke();
    }

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
        OnInventoryChanged.Invoke();
    }

    public void OnItemsListChanged(SyncList<ItemInfo>.Operation op, int index, ItemInfo oldItem, ItemInfo newItem)
    {
        Debug.Log($"[Inventory] Items list changed: op={op}, index={index}");
        OnInventoryChanged.Invoke();
    }

    private void ApplyItemStats(Item item, bool apply)
    {
        // НЕ изменяем базовые статы напрямую - это приводит к двойному учету
        // Статы экипировки уже учитываются в CalculateDerivedStats() через GetEquippedItems()
        CharacterStats stats = playerCore.Stats;
        
        // Только пересчитываем производные статы
        stats.CalculateDerivedStats();
        
        Debug.Log($"[Inventory] Recalculated stats after equipment change: {item.itemName} (apply={apply}). New maxHealth: {stats.maxHealth}, maxMana: {stats.maxMana}");
    }
    
    private void ApplyItemInfoStats(ItemInfo itemInfo, bool apply)
    {
        // НЕ изменяем базовые статы напрямую - это приводит к двойному учету
        // Статы экипировки уже учитываются в CalculateDerivedStats() через GetEquippedItems()
        CharacterStats stats = playerCore.Stats;
        
        // Только пересчитываем производные статы
        stats.CalculateDerivedStats();
        
        Debug.Log($"[Inventory] Recalculated stats after equipment change: {itemInfo.GetItemName()} (apply={apply}). New maxHealth: {stats.maxHealth}, maxMana: {stats.maxMana}");
    }

    public ItemInfo GetEquipped(EquipmentSlot slot)
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
            default: return new ItemInfo { id = 0, quantity = 0 };
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
    
    public ItemInfo[] GetEquippedItemInfos()
    {
        List<ItemInfo> equippedItemInfos = new List<ItemInfo>();
        ItemInfo[] slots = { headSlot, bodySlot, legsSlot, rightHandSlot, leftHandSlot, ringSlot, necklaceSlot, bootsSlot, glovesSlot, weaponSlot, offHandSlot };
        foreach (var slot in slots)
        {
            if (slot.id > 0)
            {
                equippedItemInfos.Add(slot);
            }
        }
        return equippedItemInfos.ToArray();
    }

    private void OnHeadChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Head, newItem);
    }

    private void OnBodyChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Body, newItem);
    }

    private void OnLegsChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Legs, newItem);
    }

    private void OnRightHandChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.RightHand, newItem);
    }

    private void OnLeftHandChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.LeftHand, newItem);
    }

    private void OnRingChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Ring, newItem);
    }

    private void OnNecklaceChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Necklace, newItem);
    }

    private void OnBootsChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Boots, newItem);
    }

    private void OnGlovesChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Gloves, newItem);
    }

    private void OnWeaponChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.Weapon, newItem);
    }

    private void OnOffHandChanged(ItemInfo oldItem, ItemInfo newItem)
    {
        OnEquipmentChanged.Invoke();
        if (isClient) visuals?.UpdateEquipmentVisual(EquipmentSlot.OffHand, newItem);
    }

    private void OnInventoryGoldChanged(int oldGold, int newGold)
    {
        OnGoldChanged.Invoke();
    }

    [ClientRpc]
    private void RpcUpdateEquipmentVisual(EquipmentSlot slot, ItemInfo itemInfo)
    {
        visuals?.UpdateEquipmentVisual(slot, itemInfo);
    }

    [Client]
    private void UpdateEquipmentVisuals()
    {
        if (visuals == null) return;
        visuals.UpdateEquipmentVisual(EquipmentSlot.Head, headSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Body, bodySlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Legs, legsSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.RightHand, rightHandSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.LeftHand, leftHandSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Ring, ringSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Necklace, necklaceSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Boots, bootsSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Gloves, glovesSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.Weapon, weaponSlot);
        visuals.UpdateEquipmentVisual(EquipmentSlot.OffHand, offHandSlot);
        Debug.Log($"[Inventory] Updated visuals for all equipment slots");
    }
}