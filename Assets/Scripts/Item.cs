using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
    public int id = -1; // ”никальный ID, задаЄтс€ в ItemDatabase
    public Sprite icon;
    public ItemType itemType = ItemType.Consumable;
    public EquipmentSlot equipmentSlot = EquipmentSlot.None;

    [Header("Flags")]
    public int maxStack = 1;
    public bool canDrop = true;
    public bool canSell = true;
    public bool canUse = false;
    public bool canHotbar = false;

    [Header("Stats Modifiers")]
    public int strengthMod;
    public int agilityMod;
    public int spiritMod;
    public int constitutionMod;
    public int accuracyMod;
    public int intelligenceMod;

    private void OnEnable()
    {
        if (id < 0)
        {
            Debug.LogWarning($"[Item] ID not set for {itemName}, defaulting to -1");
        }
    }

    public virtual void Use(PlayerCore player)
    {
        if (canUse)
        {
            Debug.Log($"Used {itemName}");
            if (player.Health != null)
            {
                player.Health.Heal(100);
            }
        }
    }
}

public enum ItemType { Normal, Consumable }
public enum EquipmentSlot { None, Head, Body, Legs, RightHand, LeftHand }