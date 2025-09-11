using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class Inventory : NetworkBehaviour
{
    public PlayerCore playerCore;
    public int inventorySize = 20;
    [SyncVar] public List<ItemInstance> items = new List<ItemInstance>();
    [SyncVar] public ItemInstance headSlot;
    [SyncVar] public ItemInstance bodySlot;
    [SyncVar] public ItemInstance legsSlot;
    [SyncVar] public ItemInstance rightHandSlot;
    [SyncVar] public ItemInstance leftHandSlot;

    public void Init(PlayerCore core)
    {
        playerCore = core;
    }

    [Server]
    private void Start()
    {
        if (isServer)
        {
            Item healthPotion = Resources.Load<Item>("Items/HealthPotion");
            if (healthPotion != null) AddItem(healthPotion, 5);
            Item helmet = Resources.Load<Item>("Items/Helmet");
            if (helmet != null) AddItem(helmet, 1);
        }
    }

    [Server]
    public bool AddItem(Item item, int quantity = 1)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item == item && items[i].quantity < item.maxStack)
            {
                var instance = items[i];
                instance.quantity += quantity;
                items[i] = instance; // Обновляем элемент в списке
                return true;
            }
        }
        if (items.Count < inventorySize)
        {
            items.Add(new ItemInstance { item = item, quantity = quantity });
            return true;
        }
        return false;
    }

    [Server]
    public void EquipItem(ItemInstance itemInst, EquipmentSlot slot)
    {
        ItemInstance oldItem = GetEquipped(slot);
        if (oldItem.item != null) UnequipItem(slot);

        SetEquipped(slot, itemInst);
        ApplyItemStats(itemInst.item, true);
    }

    [Server]
    public void UnequipItem(EquipmentSlot slot)
    {
        ItemInstance itemInst = GetEquipped(slot);
        if (itemInst.item == null) return;

        ApplyItemStats(itemInst.item, false);
        AddItem(itemInst.item, itemInst.quantity);
        SetEquipped(slot, new ItemInstance());
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
    }

    private ItemInstance GetEquipped(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Head: return headSlot;
            case EquipmentSlot.Body: return bodySlot;
            case EquipmentSlot.Legs: return legsSlot;
            case EquipmentSlot.RightHand: return rightHandSlot;
            case EquipmentSlot.LeftHand: return leftHandSlot;
            default: return new ItemInstance();
        }
    }

    private void SetEquipped(EquipmentSlot slot, ItemInstance inst)
    {
        switch (slot)
        {
            case EquipmentSlot.Head: headSlot = inst; break;
            case EquipmentSlot.Body: bodySlot = inst; break;
            case EquipmentSlot.Legs: legsSlot = inst; break;
            case EquipmentSlot.RightHand: rightHandSlot = inst; break;
            case EquipmentSlot.LeftHand: leftHandSlot = inst; break;
        }
    }

    [System.Serializable]
    public struct ItemInstance
    {
        public Item item;
        public int quantity;
    }
}