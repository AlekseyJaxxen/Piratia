using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Item.TemporaryBuff))]
public class TemporaryBuffPropertyDrawer : PropertyDrawer
{
    private readonly string[] statNames = {
        "strength", "agility", "spirit", "constitution", "accuracy",
        "maxhealth", "maxmana", "movementspeed", "armor", "minattack", "maxattack",
        "attackspeed", "dodgechance", "hitchance", "criticalhitchance",
        "criticalhitmultiplier", "physicalresistance", "magicdamagemultiplier"
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Получаем свойства
        SerializedProperty statNameProp = property.FindPropertyRelative("statName");
        SerializedProperty valueProp = property.FindPropertyRelative("value");
        SerializedProperty durationProp = property.FindPropertyRelative("duration");
        SerializedProperty isPercentageProp = property.FindPropertyRelative("isPercentage");
        SerializedProperty weightProp = property.FindPropertyRelative("weight");

        // Находим индекс текущего стата
        int currentIndex = System.Array.IndexOf(statNames, statNameProp.stringValue);
        if (currentIndex == -1) currentIndex = 0;

        // Высота одной строки
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Поле для выбора стата
        Rect statRect = new Rect(position.x, position.y, position.width, lineHeight);
        int newIndex = EditorGUI.Popup(statRect, "Stat Name", currentIndex, statNames);
        if (newIndex != currentIndex)
        {
            statNameProp.stringValue = statNames[newIndex];
        }

        // Поле для значения
        Rect valueRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
        EditorGUI.PropertyField(valueRect, valueProp, new GUIContent("Value"));

        // Поле для длительности
        Rect durationRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
        EditorGUI.PropertyField(durationRect, durationProp, new GUIContent("Duration"));

        // Чекбокс для процентного баффа
        Rect percentageRect = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
        EditorGUI.PropertyField(percentageRect, isPercentageProp, new GUIContent("Is Percentage"));

        // Поле для веса
        Rect weightRect = new Rect(position.x, position.y + (lineHeight + spacing) * 4, position.width, lineHeight);
        EditorGUI.PropertyField(weightRect, weightProp, new GUIContent("Weight"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return (lineHeight + spacing) * 5 - spacing; // 5 полей
    }
}
