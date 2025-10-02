using UnityEngine;

[CreateAssetMenu(fileName = "NewBootsItem", menuName = "Inventory/BootsItem")]
public class BootsItem : Item
{
    private void OnEnable()
    {
        base.OnEnable();
        // Автоматически устанавливаем значения только если они не заданы
        if (itemType == ItemType.Normal || itemType == ItemType.Consumable)
            itemType = ItemType.Armor;
        
        if (equipmentSlot == EquipmentSlot.None)
            equipmentSlot = EquipmentSlot.Boots;
            
        if (primaryDisplaySlot == EquipmentSlot.None)
            primaryDisplaySlot = EquipmentSlot.Boots;
    }

    public override void Use(PlayerCore player)
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
                        Debug.Log($"[BootsItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BootsItem] No matching equipment slot for {itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[BootsItem] Cannot equip {itemName}: level {player.Stats.level} or class {player.Stats.characterClass} does not match required level {requiredLevel} or class {characterClass}");
            }
        }
        else
        {
            base.Use(player);
        }
    }
}
