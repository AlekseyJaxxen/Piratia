using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor для MonsterDatabaseManager
/// </summary>
[CustomEditor(typeof(MonsterDatabaseManager))]
public class MonsterDatabaseManagerEditor : Editor
{
    private MonsterDatabaseManager manager;
    
    void OnEnable()
    {
        manager = (MonsterDatabaseManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Database Management", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Add All Monsters"))
        {
            manager.AddAllGeneratedMonstersToDatabase();
        }
        
        if (GUILayout.Button("Assign IDs"))
        {
            manager.AssignIdsToAllMonsters();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Show Statistics"))
        {
            manager.ShowDatabaseStatistics();
        }
        
        if (GUILayout.Button("Clear Database"))
        {
            manager.ClearDatabase();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Показываем информацию о базе данных
        if (manager.monsterDatabase != null)
        {
            EditorGUILayout.LabelField("Database Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Monsters Count: {manager.monsterDatabase.monsters.Count}");
            EditorGUILayout.LabelField($"Database Path: {manager.databasePath}");
        }
        else
        {
            EditorGUILayout.HelpBox("MonsterDatabase not assigned!", MessageType.Warning);
        }
    }
}

/// <summary>
/// Окно для управления MonsterDatabase
/// </summary>
public class MonsterDatabaseWindow : EditorWindow
{
    private MonsterDatabaseManager manager;
    private Vector2 scrollPosition;
    private bool autoAssignIds = true;
    private bool autoUpdateDatabase = true;
    
    [MenuItem("Tools/Monster Database Manager")]
    public static void ShowWindow()
    {
        GetWindow<MonsterDatabaseWindow>("Monster Database Manager");
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Monster Database Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Выбор Manager
        manager = (MonsterDatabaseManager)EditorGUILayout.ObjectField("Database Manager:", manager, typeof(MonsterDatabaseManager), true);
        
        if (manager == null)
        {
            EditorGUILayout.HelpBox("Please assign a MonsterDatabaseManager component.", MessageType.Warning);
            
            if (GUILayout.Button("Create Database Manager"))
            {
                CreateDatabaseManager();
            }
            return;
        }
        
        EditorGUILayout.Space();
        
        // Настройки
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        autoAssignIds = EditorGUILayout.Toggle("Auto Assign IDs", autoAssignIds);
        autoUpdateDatabase = EditorGUILayout.Toggle("Auto Update Database", autoUpdateDatabase);
        
        manager.autoAssignIds = autoAssignIds;
        manager.autoUpdateDatabase = autoUpdateDatabase;
        
        EditorGUILayout.Space();
        
        // Основные операции
        EditorGUILayout.LabelField("Database Operations", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add All Generated Monsters to Database"))
        {
            manager.AddAllGeneratedMonstersToDatabase();
        }
        
        if (GUILayout.Button("Assign IDs to All Monsters"))
        {
            manager.AssignIdsToAllMonsters();
        }
        
        EditorGUILayout.Space();
        
        // Информация
        EditorGUILayout.LabelField("Database Information", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Show Database Statistics"))
        {
            manager.ShowDatabaseStatistics();
        }
        
        EditorGUILayout.Space();
        
        // Опасные операции
        EditorGUILayout.LabelField("Dangerous Operations", EditorStyles.boldLabel);
        
        GUI.color = Color.red;
        if (GUILayout.Button("Clear Database"))
        {
            manager.ClearDatabase();
        }
        GUI.color = Color.white;
        
        EditorGUILayout.Space();
        
        // Показываем информацию о базе данных
        if (manager.monsterDatabase != null)
        {
            EditorGUILayout.LabelField("Current Database Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Monsters Count: {manager.monsterDatabase.monsters.Count}");
            EditorGUILayout.LabelField($"Database Path: {manager.databasePath}");
            
            // Показываем список монстров
            if (manager.monsterDatabase.monsters.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Monsters in Database:", EditorStyles.boldLabel);
                
                for (int i = 0; i < manager.monsterDatabase.monsters.Count; i++)
                {
                    MonsterInfo monster = manager.monsterDatabase.monsters[i];
                    if (monster != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"ID {monster.monsterId}: {monster.monsterName}", GUILayout.Width(300));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = monster;
                            EditorGUIUtility.PingObject(monster);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"ID {i + 1}: [NULL]", EditorStyles.miniLabel);
                    }
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void CreateDatabaseManager()
    {
        GameObject managerObject = new GameObject("MonsterDatabaseManager");
        manager = managerObject.AddComponent<MonsterDatabaseManager>();
        Selection.activeObject = managerObject;
    }
}
