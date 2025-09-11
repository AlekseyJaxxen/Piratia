using UnityEngine;

[System.Serializable]
public struct ItemInfo
{
    public int id; // ”никальный ID предмета
    public int quantity;

    public Item GetItem()
    {
        if (id < 0) return null;
        ItemDatabase database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            Debug.LogError("[ItemInfo] ItemDatabase not found in Resources!");
            return null;
        }
        Item item = database.GetItem(id);
        if (item == null)
        {
            Debug.LogError($"[ItemInfo] Failed to load Item with ID: {id}");
        }
        return item;
    }
}