using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor для MonsterInfoGenerator - предоставляет удобный интерфейс в Unity Editor
/// </summary>
[CustomEditor(typeof(MonsterInfoGenerator))]
public class MonsterInfoGeneratorEditor : Editor
{
    private MonsterInfoGenerator generator;
    private int selectedLevel = 5;
    private MonsterInfoGenerator.MonsterCategory selectedCategory = MonsterInfoGenerator.MonsterCategory.Tank;
    
    void OnEnable()
    {
        generator = (MonsterInfoGenerator)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Monster Info Generation", EditorStyles.boldLabel);
        
        // Выбор категории и уровня
        EditorGUILayout.BeginHorizontal();
        selectedCategory = (MonsterInfoGenerator.MonsterCategory)EditorGUILayout.EnumPopup("Category:", selectedCategory);
        selectedLevel = EditorGUILayout.IntField("Level:", selectedLevel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Кнопки генерации
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Generate Single"))
        {
            GenerateSingleMonsterInfo();
        }
        
        if (GUILayout.Button("Generate Range"))
        {
            GenerateRangeMonsterInfo();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Быстрые кнопки
        EditorGUILayout.LabelField("Quick Generation", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("All Types Lv1-10"))
        {
            GenerateAllTypesLevel1To10();
        }
        
        if (GUILayout.Button("MushroomMob Lv1-5"))
        {
            GenerateMushroomMobLevel1To5();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Информация о шаблонах
        EditorGUILayout.LabelField("Available Templates", EditorStyles.boldLabel);
        
        if (generator.monsterTemplates != null)
        {
            foreach (var template in generator.monsterTemplates)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{template.name} ({template.category})", GUILayout.Width(200));
                EditorGUILayout.LabelField($"Lv{template.minLevel}-{template.maxLevel}", GUILayout.Width(80));
                EditorGUILayout.LabelField(template.description, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.Space();
        
        // Настройки папки
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Output Folder"))
        {
            CreateOutputFolder();
        }
        
        if (GUILayout.Button("Open Output Folder"))
        {
            OpenOutputFolder();
        }
    }
    
    void GenerateSingleMonsterInfo()
    {
        MonsterInfo monsterInfo = generator.GenerateMonsterInfo(selectedCategory, selectedLevel);
        if (monsterInfo != null)
        {
            SaveMonsterInfoAsset(monsterInfo, selectedCategory, selectedLevel);
            EditorUtility.DisplayDialog("Success", $"Generated {selectedCategory} Level {selectedLevel}", "OK");
        }
    }
    
    void GenerateRangeMonsterInfo()
    {
        int startLevel = EditorUtility.DisplayDialogComplex("Generate Range", "Select level range:", "1-10", "1-20", "1-50") switch
        {
            0 => 1, // 1-10
            1 => 1, // 1-20
            2 => 1, // 1-50
            _ => 1
        };
        
        int endLevel = EditorUtility.DisplayDialogComplex("Generate Range", "Select level range:", "1-10", "1-20", "1-50") switch
        {
            0 => 10, // 1-10
            1 => 20, // 1-20
            2 => 50, // 1-50
            _ => 10
        };
        
        generator.GenerateMonsterInfoRange(selectedCategory, startLevel, endLevel);
        EditorUtility.DisplayDialog("Success", $"Generated {selectedCategory} levels {startLevel}-{endLevel}", "OK");
    }
    
    void GenerateAllTypesLevel1To10()
    {
        if (EditorUtility.DisplayDialog("Generate All Types", "Generate all monster types for levels 1-10?", "Yes", "No"))
        {
            generator.GenerateMonsterInfoRange(MonsterInfoGenerator.MonsterCategory.Tank, 1, 10);
            generator.GenerateMonsterInfoRange(MonsterInfoGenerator.MonsterCategory.Fast, 1, 10);
            generator.GenerateMonsterInfoRange(MonsterInfoGenerator.MonsterCategory.Magic, 1, 10);
            generator.GenerateMonsterInfoRange(MonsterInfoGenerator.MonsterCategory.Ranged, 1, 10);
            EditorUtility.DisplayDialog("Success", "Generated all monster types for levels 1-10", "OK");
        }
    }
    
    void GenerateMushroomMobLevel1To5()
    {
        if (EditorUtility.DisplayDialog("Generate MushroomMob", "Generate MushroomMob for levels 1-5?", "Yes", "No"))
        {
            generator.GenerateMonsterInfoRange(MonsterInfoGenerator.MonsterCategory.Tank, 1, 5);
            EditorUtility.DisplayDialog("Success", "Generated MushroomMob for levels 1-5", "OK");
        }
    }
    
    void SaveMonsterInfoAsset(MonsterInfo monsterInfo, MonsterInfoGenerator.MonsterCategory category, int level)
    {
        // Создаем папку если не существует
        string outputFolder = generator.outputFolder;
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        
        // Создаем имя файла
        string fileName = $"{category}_{level}";
        string assetPath = $"{outputFolder}/{fileName}.asset";
        
        // Сохраняем asset
        AssetDatabase.CreateAsset(monsterInfo, assetPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Saved MonsterInfo: {assetPath}");
    }
    
    void CreateOutputFolder()
    {
        string outputFolder = generator.outputFolder;
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Created folder: {outputFolder}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", $"Folder already exists: {outputFolder}", "OK");
        }
    }
    
    void OpenOutputFolder()
    {
        string outputFolder = generator.outputFolder;
        if (Directory.Exists(outputFolder))
        {
            EditorUtility.RevealInFinder(outputFolder);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", $"Folder does not exist: {outputFolder}", "OK");
        }
    }
}

/// <summary>
/// Окно для массовой генерации MonsterInfo
/// </summary>
public class MonsterInfoGeneratorWindow : EditorWindow
{
    private MonsterInfoGenerator generator;
    private Vector2 scrollPosition;
    private bool[] selectedCategories = new bool[4];
    private int startLevel = 1;
    private int endLevel = 10;
    private string outputFolder = "Assets/Resources/MonsterData/Generated";
    
    [MenuItem("Tools/Monster Info Generator")]
    public static void ShowWindow()
    {
        GetWindow<MonsterInfoGeneratorWindow>("Monster Info Generator");
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Monster Info Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Выбор генератора
        generator = (MonsterInfoGenerator)EditorGUILayout.ObjectField("Generator:", generator, typeof(MonsterInfoGenerator), true);
        
        if (generator == null)
        {
            EditorGUILayout.HelpBox("Please assign a MonsterInfoGenerator component.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space();
        
        // Настройки папки
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        
        EditorGUILayout.Space();
        
        // Выбор категорий
        EditorGUILayout.LabelField("Monster Categories", EditorStyles.boldLabel);
        
        string[] categoryNames = { "Tank", "Fast", "Magic", "Ranged" };
        for (int i = 0; i < selectedCategories.Length; i++)
        {
            selectedCategories[i] = EditorGUILayout.Toggle(categoryNames[i], selectedCategories[i]);
        }
        
        EditorGUILayout.Space();
        
        // Настройки уровней
        EditorGUILayout.LabelField("Level Range", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        startLevel = EditorGUILayout.IntField("Start Level:", startLevel);
        endLevel = EditorGUILayout.IntField("End Level:", endLevel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Кнопки генерации
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Selected Categories"))
        {
            GenerateSelectedCategories();
        }
        
        if (GUILayout.Button("Generate All Categories"))
        {
            GenerateAllCategories();
        }
        
        EditorGUILayout.Space();
        
        // Быстрые кнопки
        EditorGUILayout.LabelField("Quick Generation", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Levels 1-10"))
        {
            GenerateQuickRange(1, 10);
        }
        
        if (GUILayout.Button("Levels 1-20"))
        {
            GenerateQuickRange(1, 20);
        }
        
        if (GUILayout.Button("Levels 1-50"))
        {
            GenerateQuickRange(1, 50);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
    }
    
    void GenerateSelectedCategories()
    {
        if (startLevel > endLevel)
        {
            EditorUtility.DisplayDialog("Error", "Start level cannot be greater than end level!", "OK");
            return;
        }
        
        int generatedCount = 0;
        
        for (int i = 0; i < selectedCategories.Length; i++)
        {
            if (selectedCategories[i])
            {
                MonsterInfoGenerator.MonsterCategory category = (MonsterInfoGenerator.MonsterCategory)i;
                generator.GenerateMonsterInfoRange(category, startLevel, endLevel);
                generatedCount += (endLevel - startLevel + 1);
            }
        }
        
        EditorUtility.DisplayDialog("Success", $"Generated {generatedCount} MonsterInfo assets!", "OK");
    }
    
    void GenerateAllCategories()
    {
        if (EditorUtility.DisplayDialog("Generate All", $"Generate all categories for levels {startLevel}-{endLevel}?", "Yes", "No"))
        {
            int generatedCount = 0;
            
            for (int i = 0; i < 4; i++)
            {
                MonsterInfoGenerator.MonsterCategory category = (MonsterInfoGenerator.MonsterCategory)i;
                generator.GenerateMonsterInfoRange(category, startLevel, endLevel);
                generatedCount += (endLevel - startLevel + 1);
            }
            
            EditorUtility.DisplayDialog("Success", $"Generated {generatedCount} MonsterInfo assets!", "OK");
        }
    }
    
    void GenerateQuickRange(int start, int end)
    {
        if (EditorUtility.DisplayDialog("Quick Generation", $"Generate all categories for levels {start}-{end}?", "Yes", "No"))
        {
            int generatedCount = 0;
            
            for (int i = 0; i < 4; i++)
            {
                MonsterInfoGenerator.MonsterCategory category = (MonsterInfoGenerator.MonsterCategory)i;
                generator.GenerateMonsterInfoRange(category, start, end);
                generatedCount += (end - start + 1);
            }
            
            EditorUtility.DisplayDialog("Success", $"Generated {generatedCount} MonsterInfo assets!", "OK");
        }
    }
}
