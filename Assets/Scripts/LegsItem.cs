using UnityEngine;

[CreateAssetMenu(fileName = "NewLegsItem", menuName = "Inventory/LegsItem")]
public class LegsItem : Item
{
    private void OnEnable()
    {
        base.OnEnable();
        // Автоматически устанавливаем значения только если они не заданы
        if (itemType == ItemType.Normal || itemType == ItemType.Consumable)
            itemType = ItemType.Armor;
        
        if (equipmentSlot == EquipmentSlot.None)
            equipmentSlot = EquipmentSlot.Legs;
            
        if (primaryDisplaySlot == EquipmentSlot.None)
            primaryDisplaySlot = EquipmentSlot.Legs;
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
                        Debug.Log($"[LegsItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LegsItem] No matching equipment slot for {itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[LegsItem] Cannot equip {itemName}: level {player.Stats.level} or class {player.Stats.characterClass} does not match required level {requiredLevel} or class {characterClass}");
            }
        }
        else
        {
            return base.Use(player);
        }
        return false;
    }
}
