using UnityEngine;

[CreateAssetMenu(fileName = "NewRingItem", menuName = "Inventory/RingItem")]
public class RingItem : Item
{
    private void OnEnable()
    {
        base.OnEnable();
        // Автоматически устанавливаем значения только если они не заданы
        if (itemType == ItemType.Normal || itemType == ItemType.Consumable)
            itemType = ItemType.Accessory;
        
        if (equipmentSlot == EquipmentSlot.None)
            equipmentSlot = EquipmentSlot.Ring;
            
        if (primaryDisplaySlot == EquipmentSlot.None)
            primaryDisplaySlot = EquipmentSlot.Ring;
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
                        Debug.Log($"[RingItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[RingItem] No matching equipment slot for {itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[RingItem] Cannot equip {itemName}: level {player.Stats.level} or class {player.Stats.characterClass} does not match required level {requiredLevel} or class {characterClass}");
            }
        }
        else
        {
            base.Use(player);
        }
    }
}
