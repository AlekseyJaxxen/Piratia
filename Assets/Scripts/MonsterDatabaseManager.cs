using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Система автоматического добавления монстров в MonsterDatabase
/// </summary>
public class MonsterDatabaseManager : MonoBehaviour
{
    [Header("Database Settings")]
    public MonsterDatabase monsterDatabase;
    public string databasePath = "Assets/Resources/MonsterData/MonsterDatabase.asset";
    public string generatedMonstersFolder = "Assets/Resources/MonsterData/Generated";
    
    [Header("Auto Assignment Settings")]
    public bool autoAssignIds = true;
    public bool autoUpdateDatabase = true;
    public int startId = 1; // ID с которого начинать присваивание
    
    /// <summary>
    /// Автоматически добавляет все MonsterInfo из папки в MonsterDatabase
    /// </summary>
    [ContextMenu("Add All Generated Monsters to Database")]
    public void AddAllGeneratedMonstersToDatabase()
    {
        if (monsterDatabase == null)
        {
            LoadOrCreateDatabase();
        }
        
        if (monsterDatabase == null)
        {
            Debug.LogError("MonsterDatabase not found and could not be created!");
            return;
        }
        
        Debug.Log($"=== ДОБАВЛЕНИЕ МОНСТРОВ В БАЗУ ДАННЫХ ===");
        Debug.Log($"Текущее количество монстров в базе: {monsterDatabase.monsters.Count}");
        
        // Получаем все MonsterInfo файлы из папки
        List<MonsterInfo> generatedMonsters = LoadGeneratedMonsters();
        
        if (generatedMonsters.Count == 0)
        {
            Debug.LogWarning("Не найдено сгенерированных MonsterInfo файлов!");
            return;
        }
        
        Debug.Log($"Найдено сгенерированных монстров: {generatedMonsters.Count}");
        
        int addedCount = 0;
        int updatedCount = 0;
        
        foreach (MonsterInfo monsterInfo in generatedMonsters)
        {
            if (AddMonsterToDatabase(monsterInfo))
            {
                addedCount++;
            }
            else
            {
                updatedCount++;
            }
        }
        
        // Сохраняем базу данных
        if (autoUpdateDatabase)
        {
            SaveDatabase();
        }
        
        Debug.Log($"=== РЕЗУЛЬТАТ ===");
        Debug.Log($"Добавлено новых монстров: {addedCount}");
        Debug.Log($"Обновлено существующих: {updatedCount}");
        Debug.Log($"Итого монстров в базе: {monsterDatabase.monsters.Count}");
    }
    
    /// <summary>
    /// Загружает или создает MonsterDatabase
    /// </summary>
    void LoadOrCreateDatabase()
    {
        #if UNITY_EDITOR
        // Пытаемся загрузить существующую базу
        monsterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterDatabase>(databasePath);
        
        if (monsterDatabase == null)
        {
            Debug.Log($"MonsterDatabase не найден по пути {databasePath}. Создаем новую...");
            
            // Создаем папку если не существует
            string folderPath = Path.GetDirectoryName(databasePath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            // Создаем новую базу данных
            monsterDatabase = ScriptableObject.CreateInstance<MonsterDatabase>();
            UnityEditor.AssetDatabase.CreateAsset(monsterDatabase, databasePath);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"Создана новая MonsterDatabase: {databasePath}");
        }
        else
        {
            Debug.Log($"Загружена существующая MonsterDatabase: {databasePath}");
        }
        #else
        // В билде загружаем из Resources
        monsterDatabase = Resources.Load<MonsterDatabase>("MonsterData/MonsterDatabase");
        if (monsterDatabase == null)
        {
            Debug.LogError("MonsterDatabase не найден в Resources!");
        }
        #endif
    }
    
    /// <summary>
    /// Загружает все сгенерированные MonsterInfo файлы
    /// </summary>
    List<MonsterInfo> LoadGeneratedMonsters()
    {
        List<MonsterInfo> monsters = new List<MonsterInfo>();
        
        #if UNITY_EDITOR
        if (!Directory.Exists(generatedMonstersFolder))
        {
            Debug.LogWarning($"Папка {generatedMonstersFolder} не существует!");
            return monsters;
        }
        
        // Получаем все .asset файлы из папки
        string[] assetFiles = Directory.GetFiles(generatedMonstersFolder, "*.asset", SearchOption.AllDirectories);
        
        foreach (string assetFile in assetFiles)
        {
            MonsterInfo monsterInfo = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterInfo>(assetFile);
            if (monsterInfo != null)
            {
                monsters.Add(monsterInfo);
            }
        }
        
        // Сортируем по имени для консистентности
        monsters.Sort((a, b) => a.monsterName.CompareTo(b.monsterName));
        #else
        // В билде загружаем из Resources
        MonsterInfo[] allMonsters = Resources.LoadAll<MonsterInfo>("MonsterData/Generated");
        monsters.AddRange(allMonsters);
        monsters.Sort((a, b) => a.monsterName.CompareTo(b.monsterName));
        #endif
        
        return monsters;
    }
    
    /// <summary>
    /// Добавляет монстра в базу данных
    /// </summary>
    bool AddMonsterToDatabase(MonsterInfo monsterInfo)
    {
        if (monsterInfo == null)
        {
            Debug.LogWarning("MonsterInfo is null, пропускаем...");
            return false;
        }
        
        // Проверяем, есть ли уже такой монстр в базе
        MonsterInfo existingMonster = FindMonsterInDatabase(monsterInfo.monsterName);
        
        if (existingMonster != null)
        {
            // Обновляем существующего монстра
            int index = monsterDatabase.monsters.IndexOf(existingMonster);
            monsterDatabase.monsters[index] = monsterInfo;
            
            // Присваиваем ID если нужно
            if (autoAssignIds)
            {
                monsterInfo.monsterId = index + 1; // ID = индекс + 1
            }
            
            Debug.Log($"Обновлен монстр: {monsterInfo.monsterName} (ID: {monsterInfo.monsterId})");
            return false; // Не новый монстр
        }
        else
        {
            // Добавляем нового монстра
            monsterDatabase.monsters.Add(monsterInfo);
            
            // Присваиваем ID если нужно
            if (autoAssignIds)
            {
                monsterInfo.monsterId = monsterDatabase.monsters.Count; // ID = позиция в списке
            }
            
            Debug.Log($"Добавлен новый монстр: {monsterInfo.monsterName} (ID: {monsterInfo.monsterId})");
            return true; // Новый монстр
        }
    }
    
    /// <summary>
    /// Ищет монстра в базе данных по имени
    /// </summary>
    MonsterInfo FindMonsterInDatabase(string monsterName)
    {
        foreach (MonsterInfo monster in monsterDatabase.monsters)
        {
            if (monster != null && monster.monsterName == monsterName)
            {
                return monster;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Сохраняет базу данных
    /// </summary>
    void SaveDatabase()
    {
        if (monsterDatabase != null)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(monsterDatabase);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
            Debug.Log("MonsterDatabase сохранена!");
        }
    }
    
    /// <summary>
    /// Присваивает ID всем монстрам в базе данных
    /// </summary>
    [ContextMenu("Assign IDs to All Monsters")]
    public void AssignIdsToAllMonsters()
    {
        if (monsterDatabase == null)
        {
            LoadOrCreateDatabase();
        }
        
        if (monsterDatabase == null)
        {
            Debug.LogError("MonsterDatabase not found!");
            return;
        }
        
        Debug.Log("=== ПРИСВАИВАНИЕ ID ВСЕМ МОНСТРАМ ===");
        
        for (int i = 0; i < monsterDatabase.monsters.Count; i++)
        {
            if (monsterDatabase.monsters[i] != null)
            {
                int newId = i + 1; // ID = индекс + 1
                monsterDatabase.monsters[i].monsterId = newId;
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(monsterDatabase.monsters[i]);
                #endif
                Debug.Log($"Монстр {i + 1}: {monsterDatabase.monsters[i].monsterName} → ID: {newId}");
            }
        }
        
        SaveDatabase();
        Debug.Log("=== ID ПРИСВАИВАНИЕ ЗАВЕРШЕНО ===");
    }
    
    /// <summary>
    /// Очищает базу данных
    /// </summary>
    [ContextMenu("Clear Database")]
    public void ClearDatabase()
    {
        if (monsterDatabase == null)
        {
            LoadOrCreateDatabase();
        }
        
        #if UNITY_EDITOR
        if (UnityEditor.EditorUtility.DisplayDialog("Очистить базу данных", 
            $"Вы уверены, что хотите удалить всех монстров из базы данных?\nТекущее количество: {monsterDatabase.monsters.Count}", 
            "Да", "Нет"))
        {
            monsterDatabase.monsters.Clear();
            SaveDatabase();
            Debug.Log("База данных очищена!");
        }
        #else
        // В билде просто очищаем без диалога
        monsterDatabase.monsters.Clear();
        Debug.Log("База данных очищена!");
        #endif
    }
    
    /// <summary>
    /// Показывает статистику базы данных
    /// </summary>
    [ContextMenu("Show Database Statistics")]
    public void ShowDatabaseStatistics()
    {
        if (monsterDatabase == null)
        {
            LoadOrCreateDatabase();
        }
        
        Debug.Log("=== СТАТИСТИКА MONSTER DATABASE ===");
        Debug.Log($"Общее количество монстров: {monsterDatabase.monsters.Count}");
        
        // Группируем по типам
        Dictionary<string, int> typeCount = new Dictionary<string, int>();
        Dictionary<int, int> levelCount = new Dictionary<int, int>();
        
        foreach (MonsterInfo monster in monsterDatabase.monsters)
        {
            if (monster != null)
            {
                // Подсчитываем по типам (извлекаем из имени)
                string type = ExtractTypeFromName(monster.monsterName);
                if (!typeCount.ContainsKey(type))
                    typeCount[type] = 0;
                typeCount[type]++;
                
                // Подсчитываем по уровням (извлекаем из имени)
                int level = ExtractLevelFromName(monster.monsterName);
                if (level > 0)
                {
                    if (!levelCount.ContainsKey(level))
                        levelCount[level] = 0;
                    levelCount[level]++;
                }
            }
        }
        
        Debug.Log("По типам:");
        foreach (var kvp in typeCount)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }
        
        Debug.Log("По уровням:");
        foreach (var kvp in levelCount)
        {
            Debug.Log($"  Level {kvp.Key}: {kvp.Value}");
        }
        
        Debug.Log("=== КОНЕЦ СТАТИСТИКИ ===");
    }
    
    /// <summary>
    /// Извлекает тип монстра из имени
    /// </summary>
    string ExtractTypeFromName(string monsterName)
    {
        if (string.IsNullOrEmpty(monsterName))
            return "Unknown";
        
        // Ищем паттерн типа в имени (например, "TankMonster_Lv5" → "Tank")
        if (monsterName.Contains("Tank"))
            return "Tank";
        if (monsterName.Contains("Fast"))
            return "Fast";
        if (monsterName.Contains("Magic"))
            return "Magic";
        if (monsterName.Contains("Ranged"))
            return "Ranged";
        if (monsterName.Contains("Mushroom"))
            return "Mushroom";
        
        return "Other";
    }
    
    /// <summary>
    /// Извлекает уровень монстра из имени
    /// </summary>
    int ExtractLevelFromName(string monsterName)
    {
        if (string.IsNullOrEmpty(monsterName))
            return 0;
        
        // Ищем паттерн уровня в имени (например, "TankMonster_Lv5" → 5)
        int lvIndex = monsterName.IndexOf("Lv");
        if (lvIndex >= 0)
        {
            string levelStr = monsterName.Substring(lvIndex + 2);
            if (int.TryParse(levelStr, out int level))
            {
                return level;
            }
        }
        
        return 0;
    }
    
    /// <summary>
    /// Интегрируется с MonsterInfoGenerator для автоматического добавления
    /// </summary>
    public void OnMonsterInfoGenerated(MonsterInfo monsterInfo)
    {
        if (autoUpdateDatabase && monsterDatabase != null)
        {
            AddMonsterToDatabase(monsterInfo);
            SaveDatabase();
        }
    }
}
