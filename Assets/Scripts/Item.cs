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
            if (skillEffect == null)
            {
                // Heal убран как тест, оставляем заглушку
                Debug.Log($"[Item] No skill effect for {itemName}, no default action");
            }
            else
            {
                skillEffect.Init(player);
                PlayerSkills skills = player.GetComponent<PlayerSkills>();
                if (skills != null)
                {
                    if (skillEffect.CastTime > 0)
                    {
                        skills.SelectSkill(skillEffect);
                        Debug.Log($"[Item] Selected skill {skillEffect.SkillName} for casting from item {itemName}");
                    }
                    else
                    {
                        Ray ray = player.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
                        Vector3? targetPos = null;
                        if (Physics.Raycast(ray, out RaycastHit hit, castRange, LayerMask.GetMask("Ground")))
                        {
                            targetPos = hit.point;
                        }
                        else
                        {
                            targetPos = player.transform.position + player.transform.forward * castRange;
                        }
                        skills.CmdExecuteSkill(player, targetPos, 0, skillEffect.SkillName, 0);
                    }
                }
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