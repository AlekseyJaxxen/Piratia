using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ItemGeneratorWindow : EditorWindow
{
    private ItemGenerator generator;
    private Vector2 scrollPosition;
    private int selectedLevelConfigIndex = -1;
    private bool showLevelConfigs = true;
    private bool showGenerationSettings = true;
    private bool showSaveSettings = true;
    
    [MenuItem("Tools/Item Generator")]
    public static void ShowWindow()
    {
        GetWindow<ItemGeneratorWindow>("Item Generator");
    }
    
    private void OnEnable()
    {
        // Загружаем или создаем генератор
        string[] guids = AssetDatabase.FindAssets("t:ItemGenerator");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            generator = AssetDatabase.LoadAssetAtPath<ItemGenerator>(path);
        }
        
        if (generator == null)
        {
            generator = CreateInstance<ItemGenerator>();
        }
    }
    
    private void OnGUI()
    {
        if (generator == null)
        {
            EditorGUILayout.HelpBox("ItemGenerator not found. Creating new one...", MessageType.Warning);
            if (GUILayout.Button("Create New Generator"))
            {
                generator = CreateInstance<ItemGenerator>();
            }
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Item Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Базовый предмет
        EditorGUILayout.LabelField("Base Item Template", EditorStyles.boldLabel);
        generator.baseItem = (Item)EditorGUILayout.ObjectField("Base Item", generator.baseItem, typeof(Item), false);
        if (generator.baseItem == null)
        {
            EditorGUILayout.HelpBox("Select a base item to use as template for generation", MessageType.Warning);
        }
        EditorGUILayout.Space();
        
        // Настройки генерации
        showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings, "Generation Settings", true);
        if (showGenerationSettings)
        {
            EditorGUI.indentLevel++;
            generator.generateToResources = EditorGUILayout.Toggle("Save to Resources", generator.generateToResources);
            generator.outputPath = EditorGUILayout.TextField("Output Path", generator.outputPath);
            generator.addToItemDatabase = EditorGUILayout.Toggle("Add to ItemDatabase", generator.addToItemDatabase);
            generator.startId = EditorGUILayout.IntField("Start ID", generator.startId);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        
        // Управление конфигурациями уровней
        showLevelConfigs = EditorGUILayout.Foldout(showLevelConfigs, $"Level Configurations ({generator.levelConfigs.Count})", true);
        if (showLevelConfigs)
        {
            EditorGUI.indentLevel++;
            
            // Кнопки управления
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Level Config"))
            {
                generator.levelConfigs.Add(new ItemGenerator.LevelConfig { level = 1 });
            }
            if (GUILayout.Button("Generate Sample Config"))
            {
                generator.GenerateSampleConfiguration();
            }
            EditorGUILayout.EndHorizontal();
            
            // Список конфигураций
            for (int i = 0; i < generator.levelConfigs.Count; i++)
            {
                DrawLevelConfig(i);
            }
            
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        
        // Кнопки действий
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Single Item") && generator.levelConfigs.Count > 0)
        {
            generator.GenerateSingleItem();
        }
        if (GUILayout.Button("Generate All Items") && generator.levelConfigs.Count > 0)
        {
            generator.GenerateItems();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
        
        // Сохраняем изменения
        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
        }
    }
    
    private void DrawLevelConfig(int index)
    {
        var config = generator.levelConfigs[index];
        bool isSelected = selectedLevelConfigIndex == index;
        
        EditorGUILayout.BeginVertical("box");
        
        // Заголовок конфигурации
        EditorGUILayout.BeginHorizontal();
        bool foldout = EditorGUILayout.Foldout(isSelected, $"Level {config.level} Configuration", true);
        if (foldout != isSelected)
        {
            selectedLevelConfigIndex = foldout ? index : -1;
        }
        
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            generator.levelConfigs.RemoveAt(index);
            if (selectedLevelConfigIndex == index)
                selectedLevelConfigIndex = -1;
            return;
        }
        EditorGUILayout.EndHorizontal();
        
        if (isSelected)
        {
            EditorGUI.indentLevel++;
            
            // Основные настройки уровня
            config.level = EditorGUILayout.IntField("Level", config.level);
            config.statChance = EditorGUILayout.Slider("Stat Chance", config.statChance, 0f, 1f);
            
            EditorGUILayout.Space();
            
            // Диапазоны урона
            EditorGUILayout.LabelField("Damage Ranges", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min Damage:", GUILayout.Width(80));
            config.minDamageRange.x = EditorGUILayout.IntField(config.minDamageRange.x, GUILayout.Width(50));
            EditorGUILayout.LabelField("-", GUILayout.Width(10));
            config.minDamageRange.y = EditorGUILayout.IntField(config.minDamageRange.y, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Damage:", GUILayout.Width(80));
            config.maxDamageRange.x = EditorGUILayout.IntField(config.maxDamageRange.x, GUILayout.Width(50));
            EditorGUILayout.LabelField("-", GUILayout.Width(10));
            config.maxDamageRange.y = EditorGUILayout.IntField(config.maxDamageRange.y, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Диапазоны статов
            EditorGUILayout.LabelField("Stat Ranges", EditorStyles.boldLabel);
            DrawStatRange("Strength", ref config.strengthRange);
            DrawStatRange("Agility", ref config.agilityRange);
            DrawStatRange("Spirit", ref config.spiritRange);
            DrawStatRange("Constitution", ref config.constitutionRange);
            DrawStatRange("Accuracy", ref config.accuracyRange);
            DrawStatRange("Health", ref config.healthRange);
            DrawStatRange("Mana", ref config.manaRange);
            DrawStatRange("Defense", ref config.defenseRange);
            DrawStatRange("Critical", ref config.criticalRange);
            DrawFloatStatRange("Movement Speed", ref config.movementSpeedRange);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStatRange(string label, ref Vector2Int range)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(100));
        range.x = EditorGUILayout.IntField(range.x, GUILayout.Width(50));
        EditorGUILayout.LabelField("-", GUILayout.Width(10));
        range.y = EditorGUILayout.IntField(range.y, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawFloatStatRange(string label, ref Vector2 range)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(100));
        range.x = EditorGUILayout.FloatField(range.x, GUILayout.Width(50));
        EditorGUILayout.LabelField("-", GUILayout.Width(10));
        range.y = EditorGUILayout.FloatField(range.y, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
    }
}