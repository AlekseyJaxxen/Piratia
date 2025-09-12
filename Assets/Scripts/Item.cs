using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
    public int id = -1;
    public Sprite icon;
    [SerializeField] private GameObject dropModelPrefab;
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
    [Header("Skill Effect (Optional)")]
    public SkillBase skillEffect;
    public float castRange = 5f;

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
            if (skillEffect != null)
            {
                skillEffect.Init(player);
                Ray ray = player.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition); // ิ่๊๑
                Vector3? targetPos = null;
                if (Physics.Raycast(ray, out RaycastHit hit, castRange, LayerMask.GetMask("Ground")))
                {
                    targetPos = hit.point;
                }
                else
                {
                    targetPos = player.transform.position + player.transform.forward * castRange;
                }
                skillEffect.Execute(player, targetPos, null);
            }
        }
    }

    public GameObject GetDropModelPrefab()
    {
        return dropModelPrefab;
    }
}

public enum ItemType { Normal, Consumable }
public enum EquipmentSlot { None, Head, Body, Legs, RightHand, LeftHand }