using UnityEngine;

/// <summary>
/// Тест для проверки URP материалов
/// </summary>
public class URPMaterialTest : MonoBehaviour
{
    [ContextMenu("Test URP Materials")]
    void TestURPMaterials()
    {
        Debug.Log("=== Testing URP Materials ===");
        
        // Проверяем доступные шейдеры
        URPMaterialHelper.LogAvailableShaders();
        
        // Создаем тестовые материалы
        TestCreateMaterials();
        
        // Создаем тестовые box материалы
        TestCreateBoxMaterials();
    }
    
    void TestCreateMaterials()
    {
        Debug.Log("=== Testing Basic Material Creation ===");
        
        Color[] testColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };
        
        foreach (Color color in testColors)
        {
            Material material = URPMaterialHelper.CreateURPMaterial(color);
            Debug.Log($"Created material with color {color} using shader: {material.shader.name}");
            
            // Проверяем свойства материала
            if (material.HasProperty("_Metallic"))
            {
                Debug.Log($"  - Metallic: {material.GetFloat("_Metallic")}");
            }
            if (material.HasProperty("_Smoothness"))
            {
                Debug.Log($"  - Smoothness: {material.GetFloat("_Smoothness")}");
            }
        }
    }
    
    void TestCreateBoxMaterials()
    {
        Debug.Log("=== Testing Monster Box Materials ===");
        
        MonsterBoxType[] boxTypes = {
            MonsterBoxType.Tank,
            MonsterBoxType.Fast,
            MonsterBoxType.Magic,
            MonsterBoxType.Ranged
        };
        
        Color[] boxColors = { Color.blue, Color.red, Color.magenta, Color.yellow };
        
        for (int i = 0; i < boxTypes.Length; i++)
        {
            Material material = URPMaterialHelper.CreateMonsterBoxMaterial(boxColors[i], boxTypes[i]);
            Debug.Log($"Created {boxTypes[i]} box material with color {boxColors[i]} using shader: {material.shader.name}");
            
            if (material.HasProperty("_Metallic"))
            {
                Debug.Log($"  - Metallic: {material.GetFloat("_Metallic")}");
            }
            if (material.HasProperty("_Smoothness"))
            {
                Debug.Log($"  - Smoothness: {material.GetFloat("_Smoothness")}");
            }
        }
    }
    
    [ContextMenu("Create Test Boxes")]
    void CreateTestBoxes()
    {
        Debug.Log("=== Creating Test Boxes ===");
        
        MonsterBoxType[] boxTypes = {
            MonsterBoxType.Tank,
            MonsterBoxType.Fast,
            MonsterBoxType.Magic,
            MonsterBoxType.Ranged
        };
        
        Color[] boxColors = { Color.blue, Color.red, Color.magenta, Color.yellow };
        Vector3[] boxSizes = {
            new Vector3(2f, 1f, 2f),    // Tank - широкий
            new Vector3(0.8f, 0.8f, 0.8f), // Fast - маленький
            new Vector3(1.2f, 2f, 1.2f),   // Magic - высокий
            new Vector3(1f, 2.5f, 1f)      // Ranged - очень высокий
        };
        
        for (int i = 0; i < boxTypes.Length; i++)
        {
            // Создаем GameObject
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"TestBox_{boxTypes[i]}";
            box.transform.position = new Vector3(i * 3f, 0f, 0f);
            box.transform.localScale = boxSizes[i];
            
            // Создаем материал
            Material material = URPMaterialHelper.CreateMonsterBoxMaterial(boxColors[i], boxTypes[i]);
            box.GetComponent<Renderer>().material = material;
            
            // Добавляем легкое свечение
            Light light = box.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = boxColors[i];
            light.intensity = 0.5f;
            light.range = 2f;
            
            Debug.Log($"Created test box: {boxTypes[i]} at position {box.transform.position}");
        }
    }
    
    [ContextMenu("Clean Test Boxes")]
    void CleanTestBoxes()
    {
        Debug.Log("=== Cleaning Test Boxes ===");
        
        GameObject[] testBoxes = GameObject.FindGameObjectsWithTag("Untagged");
        int cleaned = 0;
        
        foreach (GameObject obj in testBoxes)
        {
            if (obj.name.StartsWith("TestBox_"))
            {
                DestroyImmediate(obj);
                cleaned++;
            }
        }
        
        Debug.Log($"Cleaned {cleaned} test boxes");
    }
}
