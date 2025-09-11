using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class Inventory : NetworkBehaviour
{
    public PlayerCore playerCore;
    public int inventorySize = 20;
    [SyncVar] public List<ItemInfo> items = new List<ItemInfo>();
    [SyncVar] public ItemInfo headSlot;
    [SyncVar] public ItemInfo bodySlot;
    [SyncVar] public ItemInfo legsSlot;
    [SyncVar] public ItemInfo rightHandSlot;
    [SyncVar] public ItemInfo leftHandSlot;

    public void Init(PlayerCore core)
    {
        playerCore = core;
    }

    [Server]
    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null || item.id < 0)
        {
            Debug.LogError("[Inventory] Cannot add item: Item is null or ID is invalid");
            return false;
        }
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].id == item.id && items[i].quantity < item.maxStack)
            {
                var instance = items[i];
                instance.quantity += quantity;
                items[i] = instance;
                Debug.Log($"[Inventory] Added {quantity} {item.itemName} to stack at slot {i}, new quantity: {items[i].quantity}");
                return true;
            }
        }
        if (items.Count < inventorySize)
        {
            items.Add(new ItemInfo { id = item.id, quantity = quantity });
            Debug.Log($"[Inventory] Added new item: {item.itemName} (ID: {item.id}, quantity: {quantity}) to slot {items.Count - 1}");
            return true;
        }
        Debug.LogWarning($"[Inventory] Cannot add item: {item.itemName}, inventory full");
        return false;
    }

    [Server]
    public void EquipItem(ItemInfo itemInfo, EquipmentSlot slot)
    {
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogError($"[Inventory] Cannot equip item: Item with ID {itemInfo.id} not found");
            return;
        }
        Debug.Log($"[Inventory] Equipping item: {item.itemName} (ID: {itemInfo.id}) to {slot}");
        ItemInfo oldItem = GetEquipped(slot);
        if (oldItem.id >= 0)
        {
            Item oldItemObj = oldItem.GetItem();
            if (oldItemObj != null)
            {
                Debug.Log($"[Inventory] Unequipping old item: {oldItemObj.itemName} from {slot}");
                UnequipItem(slot);
            }
        }
        SetEquipped(slot, itemInfo);
        ApplyItemStats(item, true);
    }

    [Server]
    public void UnequipItem(EquipmentSlot slot)
    {
        ItemInfo itemInfo = GetEquipped(slot);
        if (itemInfo.id < 0) return;
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogError($"[Inventory] Cannot unequip item: Item with ID {itemInfo.id} not found");
            return;
        }
        Debug.Log($"[Inventory] Unequipping item: {item.itemName} from {slot}");
        ApplyItemStats(item, false);
        AddItem(item, itemInfo.quantity);
        SetEquipped(slot, new ItemInfo());
    }

    private void ApplyItemStats(Item item, bool apply)
    {
        int mod = apply ? 1 : -1;
        CharacterStats stats = playerCore.Stats;
        stats.strength += item.strengthMod * mod;
        stats.agility += item.agilityMod * mod;
        stats.spirit += item.spiritMod * mod;
        stats.constitution += item.constitutionMod * mod;
        stats.accuracy += item.accuracyMod * mod;
        stats.intelligence += item.intelligenceMod * mod;
        stats.CalculateDerivedStats();
        Debug.Log($"[Inventory] Applied stats for {item.itemName} (apply={apply}): strength={item.strengthMod * mod}, agility={item.agilityMod * mod}, spirit={item.spiritMod * mod}, constitution={item.constitutionMod * mod}, accuracy={item.accuracyMod * mod}, intelligence={item.intelligenceMod * mod}");
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
        }
        Debug.Log($"[Inventory] Set equipped item: {(item != null ? item.itemName : "none")} (ID: {info.id}) to {slot}");
    }
}