using UnityEngine;

[CreateAssetMenu(fileName = "NewGlovesItem", menuName = "Inventory/GlovesItem")]
public class GlovesItem : Item
{
    private void OnEnable()
    {
        base.OnEnable();
        // Автоматически устанавливаем значения только если они не заданы
        if (itemType == ItemType.Normal || itemType == ItemType.Consumable)
            itemType = ItemType.Armor;
        
        if (equipmentSlot == EquipmentSlot.None)
            equipmentSlot = EquipmentSlot.Gloves;
            
        if (primaryDisplaySlot == EquipmentSlot.None)
            primaryDisplaySlot = EquipmentSlot.Gloves;
    }

    public override bool Use(PlayerCore player)
    {
        if (!canUse)
        {
            if (IsEquipable(player.Stats.level, player.Stats.characterClass))
            {
                EquipmentSlotUI slotUI = InventoryUI.Instance.FindMatchingEquipmentSlot(this);
                if (slotUI != null)
                {
                    int slotIndex = player.Inventory.items.FindIndex(item => item.id == id);
                    if (slotIndex >= 0)
                    {
                        player.CmdEquipItem(player.Inventory.items[slotIndex], slotIndex, slotUI.slotType);
                        Debug.Log($"[GlovesItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[GlovesItem] No matching equipment slot for {itemName}");
                }

            }
            else
            {
                Debug.LogWarning($"[GlovesItem] Cannot equip {itemName}: level {player.Stats.level} or class {player.Stats.characterClass} does not match required level {requiredLevel} or class {characterClass}");
            }
        }
        else
        {
            return base.Use(player);
        }
        return false;
    }
}
