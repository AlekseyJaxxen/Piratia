# 📁 Папки для сохранения ItemGenerator ScriptableObjects

## 🎯 **Ответ:**

ItemGenerator создает ScriptableObjects в папку, которая настраивается в **настройках генератора**.

## 📂 **Папка по умолчанию:**

```
Assets/Resources/Items/Generated/
```

## ⚙️ **Где настроить папку:**

### **1️⃣ Через Editor Window:**
1. **Откройте** `Tools → Item Generator` в меню Unity
2. **Найдите секцию** "Generation Settings"
3. **Измените поле** "Output Path"
4. **По умолчанию:** `Resources/Items/Generated/`

### **2️⃣ Через Inspector:**
1. **Найдите ItemGenerator.asset** в проекте
2. **Откройте в Inspector**
3. **Измените поле** "Output Path"

### **3️⃣ В коде:**
```csharp
// В ItemGenerator.cs, строка 16:
public string outputPath = "Resources/Items/Generated/";
```

## 🔧 **Как работает сохранение:**

### **Метод SaveItemsToResources():**
```csharp
private void SaveItemsToResources(List<Item> items)
{
    #if UNITY_EDITOR
    string fullPath = "Assets/" + outputPath;  // ✅ "Assets/Resources/Items/Generated/"
    if (!System.IO.Directory.Exists(fullPath))
    {
        System.IO.Directory.CreateDirectory(fullPath);  // ✅ Создает папку если нет
    }
    
    foreach (Item item in items)
    {
        string fileName = $"{item.itemName.Replace(" ", "_")}_Lv{item.requiredLevel}.asset";
        string assetPath = fullPath + fileName;  // ✅ Полный путь к файлу
        
        UnityEditor.AssetDatabase.CreateAsset(item, assetPath);  // ✅ Создает .asset файл
    }
    
    UnityEditor.AssetDatabase.SaveAssets();
    UnityEditor.AssetDatabase.Refresh();
    #endif
}
```

## 📋 **Пример имен файлов:**

При генерации предметов создаются файлы с именами:
```
Enhanced_Sword_Lv10.asset
Strong_Helmet_Lv25.asset
Colossus_Boots_Lv50.asset
```

## 🎮 **Настройки генератора:**

### **В ItemGenerator есть следующие настройки:**

| Параметр | Значение по умолчанию | Описание |
|----------|----------------------|----------|
| `generateToResources` | `true` | Сохранять ли в Resources папку |
| `outputPath` | `"Resources/Items/Generated/"` | Путь для сохранения |
| `addToItemDatabase` | `true` | Добавлять ли в ItemDatabase |
| `startId` | `1000` | Начальный ID для новых предметов |

## 🔍 **Где найти сгенерированные предметы:**

### **В проекте Unity:**
```
Assets/
├── Resources/
│   └── Items/
│       └── Generated/          ← ✅ ВОТ ЗДЕСЬ!
│           ├── Enhanced_Sword_Lv10.asset
│           ├── Strong_Helmet_Lv25.asset
│           └── Colossus_Boots_Lv50.asset
```

### **В файловой системе Windows:**
```
C:\Piratia2.0\Assets\Resources\Items\Generated\
```

## ⚡ **Быстрые действия:**

### **Изменить папку на другую:**
1. **Откройте** `Tools → Item Generator`
2. **Измените** "Output Path" на `"MyItems/Weapons/"`
3. **Результат:** предметы будут в `Assets/MyItems/Weapons/`

### **Отключить сохранение в файлы:**
1. **Снимите галочку** "Save to Resources"
2. **Предметы будут только в ItemDatabase**, но не сохранятся как .asset файлы

### **Найти существующий ItemGenerator:**
```csharp
// В коде:
ItemGenerator generator = Resources.Load<ItemGenerator>("ItemGenerator");

// Или через поиск:
string[] guids = AssetDatabase.FindAssets("t:ItemGenerator");
```

## 🎯 **Итог:**

**ItemGenerator создает ScriptableObjects в папку:**
```
Assets/Resources/Items/Generated/
```

**Эту папку можно изменить** в настройках генератора через `outputPath`.

**Файлы создаются** только при нажатии кнопок "Generate Single Item" или "Generate All Items" в Editor Window.
