using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordItem", menuName = "Inventory/SwordItem")]
public class SwordItem : Item
{
    [Header("Sword Properties")]
    public int baseDamage = 10;
    public float attackSpeed = 1f;
    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;

    private void OnEnable()
    {
        // ”дал€ем автоматическое заполнение полей, полагаемс€ на настройки из инспектора и базовый класс Item
        base.OnEnable();
    }

    public override void Use(PlayerCore player)
    {
        if (!canUse)
        {
            if (IsEquipable(player.Stats.level, player.Stats.characterClass))
            {
                EquipmentSlotUI slotUI = PlayerUI.Instance.FindMatchingEquipmentSlot(this);
                if (slotUI != null)
                {
                    int slotIndex = player.Inventory.items.FindIndex(item => item.id == id);
                    if (slotIndex >= 0)
                    {
                        player.CmdEquipItem(player.Inventory.items[slotIndex], slotIndex, slotUI.slotType);
                        Debug.Log($"[SwordItem] Equipping {itemName} to {slotUI.slotType} from slot {slotIndex}");
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
            base.Use(player);
        }
    }

    public virtual int CalculateDamage()
    {
        bool isCritical = Random.value < criticalChance;
        int damage = baseDamage;
        if (isCritical)
        {
            damage = Mathf.RoundToInt(damage * criticalMultiplier);
            Debug.Log($"[SwordItem] Critical hit with {itemName}! Damage: {damage}");
        }
        return damage;
    }
}