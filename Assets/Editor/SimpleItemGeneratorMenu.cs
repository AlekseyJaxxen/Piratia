using UnityEngine;
using UnityEditor;

public class SimpleItemGeneratorMenu
{
    [MenuItem("Tools/Create Advanced Item Generator")]
    public static void CreateAdvancedItemGenerator()
    {
        // Простое решение - создание через System.Type
        System.Type generatorType = System.Type.GetType("AdvancedItemGenerator");
        
        if (generatorType == null)
        {
            EditorUtility.DisplayDialog("Ошибка", 
                "AdvancedItemGenerator не найден!\n\n" +
                "Попробуйте:\n" +
                "1. Right-click в Project папке\n" +
                "2. Create → Item Generator → Smart Generator", "OK");
            return;
        }
        
        // Создаем новый генератор
        Object generator = ScriptableObject.CreateInstance(generatorType);
        
        // Сохраняем в папку Scripts
        string path = "Assets/Scripts/AdvancedItemGenerator.asset";
        
        AssetDatabase.CreateAsset(generator, path);
        AssetDatabase.SaveAssets();
        
        // Выделяем созданный файл в Project
        Selection.activeObject = generator;
        EditorGUIUtility.PingObject(generator);
        
        Debug.Log($"[SimpleItemGeneratorMenu] Создан Advanced Item Generator по пути: {path}");
        
        EditorUtility.DisplayDialog("Успех!", 
            "Advanced Item Generator создан!\n\n" +
            "Следующие шаги:\n" +
            "1. Right-click на генераторе → Context Menu\n" +
            "2. Выберите Create Templates (Sword, Armor, etc.)\n" +
            "3. Для генерации: Right-click → Generate Items", "OK");
    }
    
    [MenuItem("Tools/Open Advanced Item Generator")]
    public static void OpenAdvancedItemGenerator()
    {
        // Находим существующий генератор
        string[] guids = AssetDatabase.FindAssets("t:AdvancedItemGenerator");
        
        if (guids.Length == 0)
        {
            bool createNew = EditorUtility.DisplayDialog("Generator не найден", 
                "Advanced Item Generator не найден в проекте.\n\nСоздать новый генератор?", "Да", "Отмена");
            
            if (createNew)
            {
                CreateAdvancedItemGenerator();
            }
            return;
        }
        
        // Загружаем первый найденный генератор
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        Object generator = AssetDatabase.LoadAssetAtPath<Object>(path);
        
        // Выделяем его в Inspector
        Selection.activeObject = generator;
        EditorGUIUtility.PingObject(generator);
        
        Debug.Log($"[SimpleItemGeneratorMenu] Открыт Advanced Item Generator: {generator.name}");
        
        EditorUtility.DisplayDialog("Generator найден!", 
            $"Найден Advanced Item Generator: {generator.name}\n\n" +
            "Для настройки используйте Inspector:\n" +
            "• Выберите базовый предмет в availableTemplates\n" +
            "• Right-click на генераторе → Context Menu\n" +
            "• Create Templates → Generate Items", "OK");
    }
    
    [MenuItem("Tools/Help - Item Generator Guide")]
    public static void ShowItemGeneratorHelp()
    {
        EditorUtility.DisplayDialog("Руководство по Item Generator", 
            "🔧 СОЗДАНИЕ ГЕНЕРАТОРА:\n" +
            "1. Tools → Create Advanced Item Generator\n" +
            "2. Или Right-click → Create → Item Generator → Smart Generator\n\n" +
            
            "⚙️ НАСТРОЙКА:\n" +
            "1. Выберите AdvancedItemGenerator.asset в Project\n" +
            "2. В Inspector поле 'availableTemplates'\n" +
            "3. Выберите любой Item (SwordItem, ArmorItem, etc.)\n\n" +
            
            "📦 СОЗДАНИЕ ШАБЛОНОВ:\n" +
            "Right-click на генераторе → Context Menu:\n" +
            "• Create Sword Template\n" +
            "• Create Armor Template\n" +
            "• Create Gloves Template\n" +
            "• Create Boots Template\n" +
            "• Create ALL Templates\n\n" +
            
            "⚡ ГЕНЕРАЦИЯ:\n" +
            "• Generate Single Item - тест\n" +
            "• Generate All Items - все предметы\n\n" +
            
            "💾 РЕЗУЛЬТАТ:\n" +
            "Предметы сохраняются в:\n" +
            "Assets/Resources/Items/Generated/", "OK");
    }
}