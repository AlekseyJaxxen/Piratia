using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public static ItemDatabase Instance { get; private set; }
    [SerializeField] private Item[] items;
    private Dictionary<int, Item> itemMap;

    private void OnEnable()
    {
        Instance = this;
        itemMap = new Dictionary<int, Item>();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                if (items[i].id < 0)
                {
                    Debug.LogWarning($"[ItemDatabase] Item {items[i].itemName} has invalid ID: {items[i].id}");
                    continue;
                }
                if (itemMap.ContainsKey(items[i].id))
                {
                    Debug.LogWarning($"[ItemDatabase] Duplicate ID {items[i].id} for item {items[i].itemName}");
                    continue;
                }
                itemMap[items[i].id] = items[i];
                Debug.Log($"[ItemDatabase] Registered item: {items[i].itemName} (ID: {items[i].id})");
            }
        }
    }

    public Item GetItem(int id)
    {
        if (id <= 0) return null;
        if (itemMap == null)
        {
            Debug.LogError("[ItemDatabase] Item map not initialized!");
            return null;
        }
        if (itemMap.TryGetValue(id, out Item item))
        {
            return item;
        }
        Debug.LogError($"[ItemDatabase] Item with ID {id} not found");
        return null;
    }

    public Item[] GetAllItems()
    {
        return items;
    }

    public Item GetItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        if (items == null) return null;
        
        return items.FirstOrDefault(item => item != null && item.itemName == itemName);
    }
}