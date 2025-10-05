using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Генератор временных box префабов для монстров
/// </summary>
public class MonsterBoxPrefabGenerator : MonoBehaviour
{
    [Header("Box Prefab Settings")]
    public string prefabOutputFolder = "Assets/Resources/MonsterData/TemporaryBoxes";
    public Material defaultBoxMaterial;
    
    [System.Serializable]
    public class BoxTypeConfig
    {
        public MonsterBoxType boxType;
        public string name;
        public Color color;
        public Vector3 size;
        public string description;
        public float metallic = 0.3f;
        public float smoothness = 0.7f;
        public float lightIntensity = 0.5f;
        public float lightRange = 2f;
    }
    
    [Header("Box Type Configurations")]
    public List<BoxTypeConfig> boxConfigs = new List<BoxTypeConfig>();
    
    void Start()
    {
        InitializeBoxConfigs();
    }
    
    /// <summary>
    /// Инициализирует конфигурации box типов
    /// </summary>
    void InitializeBoxConfigs()
    {
        boxConfigs.Clear();
        
        // Tank Box - синий, широкий
        boxConfigs.Add(new BoxTypeConfig
        {
            boxType = MonsterBoxType.Tank,
            name = "TankBox",
            color = Color.blue,
            size = new Vector3(1.2f, 1.0f, 1.2f),
            description = "Толстый моб с высокой защитой",
            metallic = 0.3f,
            smoothness = 0.7f,
            lightIntensity = 0.5f,
            lightRange = 2f
        });
        
        // Fast Box - красный, маленький
        boxConfigs.Add(new BoxTypeConfig
        {
            boxType = MonsterBoxType.Fast,
            name = "FastBox",
            color = Color.red,
            size = new Vector3(0.8f, 0.8f, 0.8f),
            description = "Быстрый моб с высоким уворотом",
            metallic = 0.2f,
            smoothness = 0.8f,
            lightIntensity = 0.6f,
            lightRange = 1.5f
        });
        
        // Magic Box - фиолетовый, высокий
        boxConfigs.Add(new BoxTypeConfig
        {
            boxType = MonsterBoxType.Magic,
            name = "MagicBox",
            color = Color.magenta,
            size = new Vector3(1.0f, 1.2f, 1.0f),
            description = "Магический моб с высоким уроном",
            metallic = 0.4f,
            smoothness = 0.6f,
            lightIntensity = 0.7f,
            lightRange = 2.5f
        });
        
        // Ranged Box - желтый, очень высокий
        boxConfigs.Add(new BoxTypeConfig
        {
            boxType = MonsterBoxType.Ranged,
            name = "RangedBox",
            color = Color.yellow,
            size = new Vector3(0.9f, 1.4f, 0.9f),
            description = "Дальний моб со сбалансированными характеристиками",
            metallic = 0.2f,
            smoothness = 0.9f,
            lightIntensity = 0.4f,
            lightRange = 3f
        });
    }
    
    /// <summary>
    /// Создает временный box префаб для определенного типа
    /// </summary>
    public GameObject CreateTemporaryBoxPrefab(MonsterBoxType boxType)
    {
        BoxTypeConfig config = GetBoxConfig(boxType);
        if (config == null)
        {
            Debug.LogError($"Box config for type {boxType} not found!");
            return null;
        }
        
        // Создаем GameObject для box
        GameObject boxPrefab = new GameObject($"{config.name}_Prefab");
        
        // Добавляем компоненты
        MeshRenderer renderer = boxPrefab.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = boxPrefab.AddComponent<MeshFilter>();
        BoxCollider boxCollider = boxPrefab.AddComponent<BoxCollider>();
        
        // Создаем куб
        meshFilter.mesh = CreateCubeMesh();
        
        // Настраиваем материал
        Material material = new Material(Shader.Find("Standard"));
        material.color = config.color;
        material.SetFloat("_Metallic", config.metallic);
        material.SetFloat("_Smoothness", config.smoothness);
        renderer.material = material;
        
        // Масштабируем box
        boxPrefab.transform.localScale = config.size;
        
        // Настраиваем коллайдер
        boxCollider.size = Vector3.one;
        boxCollider.center = Vector3.zero;
        
        // Добавляем легкое свечение
        Light light = boxPrefab.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = config.color;
        light.intensity = config.lightIntensity;
        light.range = config.lightRange;
        
        // Добавляем текстовый индикатор
        CreateTextIndicator(boxPrefab, config);
        
        Debug.Log($"Created temporary box prefab: {config.name}");
        return boxPrefab;
    }
    
    /// <summary>
    /// Создает текстовый индикатор для box
    /// </summary>
    void CreateTextIndicator(GameObject parent, BoxTypeConfig config)
    {
        // Создаем объект для текста
        GameObject textObject = new GameObject("TextIndicator");
        textObject.transform.SetParent(parent.transform);
        textObject.transform.localPosition = Vector3.up * (config.size.y + 0.5f);
        
        // Создаем простой куб как индикатор
        GameObject textMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        textMesh.transform.SetParent(textObject.transform);
        textMesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        textMesh.name = "TextCube";
        
        // Настраиваем цвет текста
        MeshRenderer textRenderer = textMesh.GetComponent<MeshRenderer>();
        Material textMaterial = new Material(Shader.Find("Standard"));
        textMaterial.color = Color.white;
        textMaterial.SetFloat("_Metallic", 0f);
        textMaterial.SetFloat("_Smoothness", 0f);
        textRenderer.material = textMaterial;
        
        // Удаляем коллайдер у текста
        Collider textCollider = textMesh.GetComponent<Collider>();
        if (textCollider != null)
        {
            DestroyImmediate(textCollider);
        }
    }
    
    /// <summary>
    /// Создает простую сетку куба
    /// </summary>
    Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        
        // Вершины куба
        Vector3[] vertices = new Vector3[]
        {
            // Передняя грань
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            
            // Задняя грань
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f)
        };
        
        // Треугольники
        int[] triangles = new int[]
        {
            // Передняя грань
            0, 1, 2, 0, 2, 3,
            // Задняя грань
            4, 6, 5, 4, 7, 6,
            // Левая грань
            4, 0, 3, 4, 3, 7,
            // Правая грань
            1, 5, 6, 1, 6, 2,
            // Верхняя грань
            3, 2, 6, 3, 6, 7,
            // Нижняя грань
            4, 1, 0, 4, 5, 1
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    /// <summary>
    /// Получает конфигурацию box по типу
    /// </summary>
    BoxTypeConfig GetBoxConfig(MonsterBoxType boxType)
    {
        foreach (var config in boxConfigs)
        {
            if (config.boxType == boxType)
                return config;
        }
        return null;
    }
    
    /// <summary>
    /// Создает все временные box префабы
    /// </summary>
    public void CreateAllTemporaryBoxPrefabs()
    {
        Debug.Log("Creating all temporary box prefabs...");
        
        foreach (MonsterBoxType boxType in System.Enum.GetValues(typeof(MonsterBoxType)))
        {
            GameObject boxPrefab = CreateTemporaryBoxPrefab(boxType);
            if (boxPrefab != null)
            {
                // Сохраняем как префаб (только в Editor)
                #if UNITY_EDITOR
                SaveBoxPrefab(boxPrefab, boxType);
                #endif
            }
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Сохраняет box как префаб (только в Editor)
    /// </summary>
    void SaveBoxPrefab(GameObject boxPrefab, MonsterBoxType boxType)
    {
        // Создаем папку если не существует
        if (!Directory.Exists(prefabOutputFolder))
        {
            Directory.CreateDirectory(prefabOutputFolder);
        }
        
        // Создаем имя файла
        string fileName = $"{boxType}Box_Prefab";
        string prefabPath = $"{prefabOutputFolder}/{fileName}.prefab";
        
        // Создаем префаб
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(boxPrefab, prefabPath);
        
        // Удаляем временный объект
        DestroyImmediate(boxPrefab);
        
        Debug.Log($"Saved box prefab: {prefabPath}");
    }
    #endif
    
    /// <summary>
    /// Создает временный box для MonsterInfo
    /// </summary>
    public GameObject CreateTemporaryBoxForMonsterInfo(MonsterInfo monsterInfo)
    {
        if (!monsterInfo.useTemporaryBox)
        {
            Debug.LogWarning($"MonsterInfo {monsterInfo.monsterName} doesn't use temporary box");
            return null;
        }
        
        // Создаем box на основе настроек MonsterInfo
        GameObject boxPrefab = new GameObject($"{monsterInfo.monsterName}_TemporaryBox");
        
        // Добавляем компоненты
        MeshRenderer renderer = boxPrefab.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = boxPrefab.AddComponent<MeshFilter>();
        BoxCollider boxCollider = boxPrefab.AddComponent<BoxCollider>();
        
        // Создаем куб
        meshFilter.mesh = CreateCubeMesh();
        
        // Настраиваем материал
        Material material = new Material(Shader.Find("Standard"));
        material.color = monsterInfo.boxColor;
        material.SetFloat("_Metallic", 0.3f);
        material.SetFloat("_Smoothness", 0.7f);
        renderer.material = material;
        
        // Масштабируем box
        boxPrefab.transform.localScale = monsterInfo.boxSize;
        
        // Настраиваем коллайдер
        boxCollider.size = Vector3.one;
        boxCollider.center = Vector3.zero;
        
        // Добавляем легкое свечение
        Light light = boxPrefab.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = monsterInfo.boxColor;
        light.intensity = 0.5f;
        light.range = 2f;
        
        // Добавляем текстовый индикатор
        CreateCustomTextIndicator(boxPrefab, monsterInfo);
        
        Debug.Log($"Created temporary box for {monsterInfo.monsterName}");
        return boxPrefab;
    }
    
    /// <summary>
    /// Создает кастомный текстовый индикатор для MonsterInfo
    /// </summary>
    void CreateCustomTextIndicator(GameObject parent, MonsterInfo monsterInfo)
    {
        // Создаем объект для текста
        GameObject textObject = new GameObject("MonsterInfoIndicator");
        textObject.transform.SetParent(parent.transform);
        textObject.transform.localPosition = Vector3.up * (monsterInfo.boxSize.y + 0.5f);
        
        // Создаем простой куб как индикатор
        GameObject textMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        textMesh.transform.SetParent(textObject.transform);
        textMesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        textMesh.name = "InfoCube";
        
        // Настраиваем цвет текста
        MeshRenderer textRenderer = textMesh.GetComponent<MeshRenderer>();
        Material textMaterial = new Material(Shader.Find("Standard"));
        textMaterial.color = Color.white;
        textMaterial.SetFloat("_Metallic", 0f);
        textMaterial.SetFloat("_Smoothness", 0f);
        textRenderer.material = textMaterial;
        
        // Удаляем коллайдер у текста
        Collider textCollider = textMesh.GetComponent<Collider>();
        if (textCollider != null)
        {
            DestroyImmediate(textCollider);
        }
    }
    
    // Контекстное меню для тестирования
    [ContextMenu("Create All Box Prefabs")]
    void CreateAllBoxPrefabs()
    {
        CreateAllTemporaryBoxPrefabs();
    }
    
    [ContextMenu("Create Tank Box")]
    void CreateTankBox()
    {
        GameObject boxPrefab = CreateTemporaryBoxPrefab(MonsterBoxType.Tank);
        if (boxPrefab != null)
        {
            #if UNITY_EDITOR
            SaveBoxPrefab(boxPrefab, MonsterBoxType.Tank);
            #endif
        }
    }
    
    [ContextMenu("Create Fast Box")]
    void CreateFastBox()
    {
        GameObject boxPrefab = CreateTemporaryBoxPrefab(MonsterBoxType.Fast);
        if (boxPrefab != null)
        {
            #if UNITY_EDITOR
            SaveBoxPrefab(boxPrefab, MonsterBoxType.Fast);
            #endif
        }
    }
    
    [ContextMenu("Create Magic Box")]
    void CreateMagicBox()
    {
        GameObject boxPrefab = CreateTemporaryBoxPrefab(MonsterBoxType.Magic);
        if (boxPrefab != null)
        {
            #if UNITY_EDITOR
            SaveBoxPrefab(boxPrefab, MonsterBoxType.Magic);
            #endif
        }
    }
    
    [ContextMenu("Create Ranged Box")]
    void CreateRangedBox()
    {
        GameObject boxPrefab = CreateTemporaryBoxPrefab(MonsterBoxType.Ranged);
        if (boxPrefab != null)
        {
            #if UNITY_EDITOR
            SaveBoxPrefab(boxPrefab, MonsterBoxType.Ranged);
            #endif
        }
    }
}
