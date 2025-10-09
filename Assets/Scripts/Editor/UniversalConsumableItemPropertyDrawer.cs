using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UniversalConsumableItem))]
public class UniversalConsumableItemPropertyDrawer : Editor
{
    private readonly string[] statNames = {
        "strength", "agility", "spirit", "constitution", "accuracy",
        "maxhealth", "maxmana", "movementspeed", "armor", "minattack", "maxattack",
        "attackspeed", "dodgechance", "hitchance", "criticalhitchance",
        "criticalhitmultiplier", "physicalresistance", "magicdamagemultiplier"
    };

    public override void OnInspectorGUI()
    {
        UniversalConsumableItem item = (UniversalConsumableItem)target;
        
        // Отображаем только нужные поля из базового класса
        EditorGUILayout.LabelField("Basic Item Properties", EditorStyles.boldLabel);
        item.itemName = EditorGUILayout.TextField("Item Name", item.itemName);
        item.id = EditorGUILayout.IntField("ID", item.id);
        item.description = EditorGUILayout.TextArea(item.description, GUILayout.Height(60));
        item.icon = (Sprite)EditorGUILayout.ObjectField("Icon", item.icon, typeof(Sprite), false);
        item.itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", item.itemType);
        item.rarity = (Rarity)EditorGUILayout.EnumPopup("Rarity", item.rarity);
        item.canUse = EditorGUILayout.Toggle("Can Use", item.canUse);
        item.canHotbar = EditorGUILayout.Toggle("Can Hotbar", item.canHotbar);
        item.stackable = EditorGUILayout.Toggle("Stackable", item.stackable);
        item.maxStack = EditorGUILayout.IntField("Max Stack", item.maxStack);
        item.cooldown = EditorGUILayout.FloatField("Cooldown", item.cooldown);
        item.instantUse = EditorGUILayout.Toggle("Instant Use", item.instantUse);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Universal Consumable Settings", EditorStyles.boldLabel);
        
        // Отображаем поля UniversalConsumableItem
        EditorGUILayout.PropertyField(serializedObject.FindProperty("consumableTypeSetting"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("healAmountSetting"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("manaAmountSetting"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buffStatType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buffValue"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buffDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isPercentageBuff"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buffWeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStackSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemCooldown"));
        
        // Отображаем temporaryBuffs с кастомным PropertyDrawer
        EditorGUILayout.PropertyField(serializedObject.FindProperty("temporaryBuffs"));
        
        serializedObject.ApplyModifiedProperties();
        
        // Добавляем кнопку для обновления описания
        EditorGUILayout.Space();
        if (GUILayout.Button("Update Description"))
        {
            item.UpdateDescription();
            EditorUtility.SetDirty(item);
        }
        
        // Показываем информацию о текущих настройках
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Settings:", EditorStyles.boldLabel);
        
        switch (item.consumableTypeSetting)
        {
            case ConsumableType.Heal:
                EditorGUILayout.LabelField($"Type: Heal ({item.healAmountSetting} HP)");
                break;
            case ConsumableType.Mana:
                EditorGUILayout.LabelField($"Type: Mana ({item.manaAmountSetting} MP)");
                break;
            case ConsumableType.Buff:
                string statName = GetStatNameFromType(item.buffStatType);
                string valueText = item.isPercentageBuff ? $"{item.buffValue * 100:F0}%" : $"{item.buffValue:F0}";
                string durationText = item.buffDuration >= 60f ? $"{item.buffDuration / 60f:F0} min" : $"{item.buffDuration:F0}s";
                string weightText = item.buffWeight == 1 ? "No Stack" : "Replace";
                EditorGUILayout.LabelField($"Type: Buff ({statName} +{valueText} for {durationText}, {weightText})");
                break;
        }
    }

    private string GetStatNameFromType(UniversalConsumableItem.BuffStatType statType)
    {
        switch (statType)
        {
            case UniversalConsumableItem.BuffStatType.Strength: return "strength";
            case UniversalConsumableItem.BuffStatType.Agility: return "agility";
            case UniversalConsumableItem.BuffStatType.Spirit: return "spirit";
            case UniversalConsumableItem.BuffStatType.Constitution: return "constitution";
            case UniversalConsumableItem.BuffStatType.Accuracy: return "accuracy";
            case UniversalConsumableItem.BuffStatType.MaxHealth: return "maxhealth";
            case UniversalConsumableItem.BuffStatType.MaxMana: return "maxmana";
            case UniversalConsumableItem.BuffStatType.MovementSpeed: return "movementspeed";
            case UniversalConsumableItem.BuffStatType.Armor: return "armor";
            case UniversalConsumableItem.BuffStatType.MinAttack: return "minattack";
            case UniversalConsumableItem.BuffStatType.MaxAttack: return "maxattack";
            case UniversalConsumableItem.BuffStatType.AttackSpeed: return "attackspeed";
            case UniversalConsumableItem.BuffStatType.DodgeChance: return "dodgechance";
            case UniversalConsumableItem.BuffStatType.HitChance: return "hitchance";
            case UniversalConsumableItem.BuffStatType.CriticalHitChance: return "criticalhitchance";
            case UniversalConsumableItem.BuffStatType.CriticalHitMultiplier: return "criticalhitmultiplier";
            case UniversalConsumableItem.BuffStatType.PhysicalResistance: return "physicalresistance";
            case UniversalConsumableItem.BuffStatType.MagicDamageMultiplier: return "magicdamagemultiplier";
            default: return "strength";
        }
    }
}
