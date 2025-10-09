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
                        Debug.Log($"[SwordItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
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
            return base.Use(player);
        }
        return false;
    }

}