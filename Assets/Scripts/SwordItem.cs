using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordItem", menuName = "Inventory/SwordItem")]
public class SwordItem : Item
{

    private void OnEnable()
    {
        // ������� �������������� ���������� �����, ���������� �� ��������� �� ���������� � ������� ����� Item
        base.OnEnable();
        weaponType = isTwoHanded ? WeaponType.TwoHandedSword : WeaponType.OneHandedSword;
    }

    public override bool Use(PlayerCore player, int slotIndex = -1)
    {
        if (!canUse)
        {
            if (IsEquipable(player.Stats.level, player.Stats.characterClass))
            {
                EquipmentSlotUI slotUI = InventoryUI.Instance.FindMatchingEquipmentSlot(this);
                if (slotUI != null)
                {
                    int foundSlotIndex = player.Inventory.items.FindIndex(item => item.id == id);
                    if (foundSlotIndex >= 0)
                    {
                        player.CmdEquipItem(player.Inventory.items[foundSlotIndex], foundSlotIndex, slotUI.slotType);
                        Debug.Log($"[SwordItem] Equipping {itemName} to {slotUI.slotType} from slot {foundSlotIndex}");
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[SwordItem] No matching equipment slot for {itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[SwordItem] Cannot equip {itemName}: level {player.Stats.level} or class {player.Stats.characterClass} does not match required level {requiredLevel} or class {characterClass}");
            }
        }
        else
        {
            return base.Use(player, slotIndex);
        }
        return false;
    }

}