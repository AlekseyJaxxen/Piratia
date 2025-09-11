using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
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

    public virtual void Use(PlayerCore player)
    {
        if (canUse)
        {
            Debug.Log($"Used {itemName}");
            if (player.Health != null)
            {
                player.Health.Heal(100); // Используем метод Heal из Health.cs
            }
        }
    }
}

public enum ItemType { Normal, Consumable }
public enum EquipmentSlot { None, Head, Body, Legs, RightHand, LeftHand }