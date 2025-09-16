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
    [Header("MMO Properties")]
    public Rarity rarity = Rarity.Common;
    public int requiredLevel = 1;
    [Header("Skill Effect (Optional)")]
    public SkillBase skillEffect;
    public float castRange = 5f;
    [Header("Additional Item Properties")]
    public string model1;
    public string model2;
    public string model3;
    public string model4;
    public string model5;
    public string shipSymbol;
    public int shipSize;
    public int number;
    public string obtain;
    public string prefix;
    public float rate;
    public int setId;
    public int forgingLevel;
    public int stableValue;
    public bool onlyId;
    public bool trade;
    public bool picked;
    public bool discard;
    public bool confirmToDelete;
    public bool stackable;
    public bool isInstantiation;
    public int price;
    public int size;
    public int characterLevel;
    public string characterClass;
    public string characterNick;
    public int characterReputation;
    public bool itemCanEquip;
    public string location;
    public string itemSwitchLocation;
    public string itemObtainIntoLocation;
    public int strModulusBonus;
    public int agiModulusBonus;
    public int dexModulusBonus;
    public int conModulusBonus;
    public int sprModulusBonus;
    public int lukModulusBonus;
    public int hitRateModulusBonus;
    public int minAttackModulusBonus;
    public int maxAttackModulusBonus;
    public int defenseModulusBonus;
    public int maxHpModulusBonus;
    public int maxSpModulusBonus;
    public int fleeModulusBonus;
    public int hitModulusBonus;
    public int crtModulusBonus;
    public int mfModulusBonus;
    public int hrecModulusBonus;
    public int srecModulusBonus;
    public int mspdModulusBonus;
    public int colModulusBonus;
    public int strConstantBonus;
    public int agiConstantBonus;
    public int dexConstantBonus;
    public int conConstantBonus;
    public int staConstantBonus;
    public int lukConstantBonus;
    public int attackRangeConstantBonus;
    public int minAttackConstantBonus;
    public int maxAttackConstantBonus;
    public int maxHpConstantBonus;
    public int maxSpConstantBonus;
    public int fleeConstantBonus;
    public int hitConstantBonus;
    public int crtConstantBonus;
    public int mfConstantBonus;
    public int hrecConstantBonus;
    public int srecConstantBonus;
    public int mspdConstantBonus;
    public int colConstantBonus;
    public int physicalResist;
    public string itemLeftHandExertIdentifier;
    public int itemEnergy;
    public int durability;
    public int maxInstantiation;
    public int holeValue;
    public int shipDurabilityRecovered;
    public int canContainCannonQuantity;
    public int shipMemberCount;
    public string memberLabel;
    public int cargoCapacity;
    public int fuelConsumption;
    public int cannonballPathOfFlightSpeed;
    public int shipMovementSpeed;
    public string usageEffect;
    public string displayEffect;
    public string itemBindEffect;
    public string itemBindEffectDummy;
    public string displayItemEffect;
    public string itemDropModelEffect;
    public string itemUsageEffect;
    public string description;
    public int itemLevel;
    public string remark;

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

    public bool IsEquipable(int playerLevel)
    {
        return equipmentSlot != EquipmentSlot.None && playerLevel >= requiredLevel;
    }

    public GameObject GetDropModelPrefab()
    {
        return dropModelPrefab;
    }
}

public enum ItemType { Normal, Consumable, Weapon, Armor, Accessory, QuestItem, Material }
public enum EquipmentSlot { None, Head, Body, Legs, RightHand, LeftHand, Ring, Necklace, Boots, Gloves, Weapon, OffHand }
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }