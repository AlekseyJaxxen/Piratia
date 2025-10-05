using UnityEngine;

[CreateAssetMenu(fileName = "NewBowItem", menuName = "Inventory/BowItem")]
public class BowItem : Item
{

    private void OnEnable()
    {
        // Устанавливаем тип оружия как лук при создании предмета
        base.OnEnable();
        weaponType = WeaponType.Bow;
        
        // Устанавливаем бонус к дальности атаки для луков
        attackRangeBonus = 6f;
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
                        Debug.Log($"[BowItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BowItem] No matching equipment slot for {itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[BowItem] Cannot equip {itemName}: level {player.Stats.level} or class {player.Stats.characterClass} does not match required level {requiredLevel} or class {characterClass}");
            }
        }
        else
        {
            base.Use(player);
        }
    }

}
