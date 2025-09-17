using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordItem", menuName = "Inventory/SwordItem")]
public class SwordItem : Item
{
    [Header("Sword Properties")]
    public int baseDamage = 10;
    public float attackSpeed = 1f;
    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;

    public override void Use(PlayerCore player)
    {
        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError($"[SwordItem] CharacterStats component not found on PlayerCore for {itemName}!");
            return;
        }

        if (IsEquipable(stats.level))
        {
            Debug.Log($"[SwordItem] Equipping sword {itemName}");
            int slotIndex = player.Inventory.items.FindIndex(i => i.id == id);
            if (slotIndex >= 0)
            {
                player.CmdEquipItem(player.Inventory.items[slotIndex], slotIndex, equipmentSlot);
            }
            else
            {
                Debug.LogWarning($"[SwordItem] Item {itemName} not found in inventory");
            }
        }
        else
        {
            Debug.LogWarning($"[SwordItem] Cannot equip {itemName}. Player level {stats.level} is less than required level {requiredLevel}");
        }
    }
}