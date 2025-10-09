using UnityEngine;

[CreateAssetMenu(fileName = "NewUniversalConsumable", menuName = "Items/Consumable/UniversalConsumable")]
public class UniversalConsumableItem : Item
{
    [Header("Consumable Type")]
    [Tooltip("Тип consumable предмета")]
    public ConsumableType consumableTypeSetting = ConsumableType.Heal;
    
    [Header("Instant Effects")]
    [Tooltip("Количество лечения")]
    public int healAmountSetting = 0;
    
    [Tooltip("Количество восстановления маны")]
    public int manaAmountSetting = 0;
    
    [Header("Buff Settings")]
    [Tooltip("Стат для баффа")]
    public BuffStatType buffStatType = BuffStatType.Strength;
    
    [Tooltip("Значение баффа")]
    public float buffValue = 1f;
    
    [Tooltip("Длительность баффа в секундах")]
    public float buffDuration = 60f;
    
    [Tooltip("Является ли бафф процентным")]
    public bool isPercentageBuff = false;
    
    [Tooltip("Вес баффа: 1 - не стакается, 2 - заменяет эффект")]
    public int buffWeight = 1;
    
    [Header("Item Settings")]
    [Tooltip("Максимальный стак предмета")]
    public int maxStackSize = 5;
    
    [Tooltip("Кулдаун предмета в секундах")]
    public float itemCooldown = 1f;

    public enum BuffStatType
    {
        Strength,
        Agility,
        Spirit,
        Constitution,
        Accuracy,
        MaxHealth,
        MaxMana,
        MovementSpeed,
        Armor,
        MinAttack,
        MaxAttack,
        AttackSpeed,
        DodgeChance,
        HitChance,
        CriticalHitChance,
        CriticalHitMultiplier,
        PhysicalResistance,
        MagicDamageMultiplier
    }

    private void OnEnable()
    {
        base.OnEnable();
        
        // Автоматически настраиваем базовые параметры
        if (itemType == ItemType.Normal)
            itemType = ItemType.Consumable;
        
        if (consumableType == ConsumableType.None)
            consumableType = consumableTypeSetting;
        
        if (canUse == false)
            canUse = true;
        
        if (canHotbar == false)
            canHotbar = true;
        
        if (stackable == false)
            stackable = true;
        
        if (maxStack <= 1)
            maxStack = maxStackSize;
        
        if (cooldown == 1f)
            cooldown = itemCooldown;
        
        if (instantUse == false)
            instantUse = true;
        
        // Настраиваем мгновенные эффекты
        if (healAmountSetting > 0)
            this.healAmount = healAmountSetting;
        
        if (manaAmountSetting > 0)
            this.manaAmount = manaAmountSetting;
        
        // Создаем временный бафф если это бафф-предмет
        if (consumableTypeSetting == ConsumableType.Buff)
        {
            if (temporaryBuffs == null || temporaryBuffs.Length == 0)
            {
                temporaryBuffs = new TemporaryBuff[1];
                temporaryBuffs[0] = new TemporaryBuff
                {
                    statName = GetStatNameFromType(buffStatType),
                    value = buffValue,
                    duration = buffDuration,
                    isPercentage = isPercentageBuff,
                    weight = buffWeight
                };
            }
            else
            {
                // Обновляем существующий бафф
                temporaryBuffs[0].statName = GetStatNameFromType(buffStatType);
                temporaryBuffs[0].value = buffValue;
                temporaryBuffs[0].duration = buffDuration;
                temporaryBuffs[0].isPercentage = isPercentageBuff;
                temporaryBuffs[0].weight = buffWeight;
            }
        }
        
        // Обновляем описание
        UpdateDescription();
    }

    private string GetStatNameFromType(BuffStatType statType)
    {
        switch (statType)
        {
            case BuffStatType.Strength: return "strength";
            case BuffStatType.Agility: return "agility";
            case BuffStatType.Spirit: return "spirit";
            case BuffStatType.Constitution: return "constitution";
            case BuffStatType.Accuracy: return "accuracy";
            case BuffStatType.MaxHealth: return "maxhealth";
            case BuffStatType.MaxMana: return "maxmana";
            case BuffStatType.MovementSpeed: return "movementspeed";
            case BuffStatType.Armor: return "armor";
            case BuffStatType.MinAttack: return "minattack";
            case BuffStatType.MaxAttack: return "maxattack";
            case BuffStatType.AttackSpeed: return "attackspeed";
            case BuffStatType.DodgeChance: return "dodgechance";
            case BuffStatType.HitChance: return "hitchance";
            case BuffStatType.CriticalHitChance: return "criticalhitchance";
            case BuffStatType.CriticalHitMultiplier: return "criticalhitmultiplier";
            case BuffStatType.PhysicalResistance: return "physicalresistance";
            case BuffStatType.MagicDamageMultiplier: return "magicdamagemultiplier";
            default: return "strength";
        }
    }

    public void UpdateDescription()
    {
        switch (consumableTypeSetting)
        {
            case ConsumableType.Heal:
                description = $"A consumable that restores {healAmountSetting} HP instantly. Cooldown: {itemCooldown}s.";
                break;
                
            case ConsumableType.Mana:
                description = $"A consumable that restores {manaAmountSetting} mana instantly. Cooldown: {itemCooldown}s.";
                break;
                
            case ConsumableType.Buff:
                string statName = GetStatNameFromType(buffStatType);
                string valueText = isPercentageBuff ? $"{buffValue * 100:F0}%" : $"{buffValue:F0}";
                string durationText = buffDuration >= 60f ? $"{buffDuration / 60f:F0} min" : $"{buffDuration:F0}s";
                string weightText = buffWeight == 1 ? "Cannot stack" : "Replaces weaker buffs";
                description = $"A magical consumable that increases {statName} by {valueText} for {durationText}. {weightText}. Cooldown: {itemCooldown}s.";
                break;
                
            default:
                description = $"A consumable item. Cooldown: {itemCooldown}s.";
                break;
        }
    }

    private void OnValidate()
    {
        // Обновляем параметры при изменении в инспекторе
        maxStack = maxStackSize;
        cooldown = itemCooldown;
        
        if (healAmountSetting > 0)
            this.healAmount = healAmountSetting;
        
        if (manaAmountSetting > 0)
            this.manaAmount = manaAmountSetting;
        
        if (consumableTypeSetting == ConsumableType.Buff && temporaryBuffs != null && temporaryBuffs.Length > 0)
        {
            temporaryBuffs[0].statName = GetStatNameFromType(buffStatType);
            temporaryBuffs[0].value = buffValue;
            temporaryBuffs[0].duration = buffDuration;
            temporaryBuffs[0].isPercentage = isPercentageBuff;
            temporaryBuffs[0].weight = buffWeight;
        }
        
        UpdateDescription();
    }
}
