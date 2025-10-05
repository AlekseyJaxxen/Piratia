using UnityEngine;

/// <summary>
/// Утилита для создания материалов совместимых с URP
/// </summary>
public static class URPMaterialHelper
{
    /// <summary>
    /// Создает материал с URP шейдером
    /// </summary>
    public static Material CreateURPMaterial(Color color, float metallic = 0.3f, float smoothness = 0.7f)
    {
        // Список URP шейдеров в порядке приоритета
        string[] urpShaders = {
            "Universal Render Pipeline/Lit",
            "URP/Lit", 
            "Shader Graphs/URP Lit",
            "Universal Render Pipeline/Simple Lit",
            "URP/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "URP/Unlit"
        };
        
        Shader selectedShader = null;
        
        // Ищем первый доступный URP шейдер
        foreach (string shaderName in urpShaders)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                selectedShader = shader;
                Debug.Log($"[URPMaterialHelper] Found URP shader: {shaderName}");
                break;
            }
        }
        
        // Fallback на стандартный шейдер
        if (selectedShader == null)
        {
            selectedShader = Shader.Find("Standard");
            Debug.LogWarning("[URPMaterialHelper] No URP shaders found, using Standard shader as fallback");
        }
        
        Material material = new Material(selectedShader);
        material.color = color;
        
        // Настраиваем параметры в зависимости от шейдера
        ConfigureMaterialProperties(material, selectedShader, metallic, smoothness);
        
        // Проверяем что материал создался корректно
        if (material == null || material.shader == null)
        {
            Debug.LogError($"[URPMaterialHelper] Failed to create material! Shader: {selectedShader?.name}");
            return null;
        }
        
        Debug.Log($"[URPMaterialHelper] Successfully created material with shader: {material.shader.name}");
        return material;
    }
    
    /// <summary>
    /// Настраивает свойства материала в зависимости от шейдера
    /// </summary>
    private static void ConfigureMaterialProperties(Material material, Shader shader, float metallic, float smoothness)
    {
        string shaderName = shader.name.ToLower();
        
        if (shaderName.Contains("universal render pipeline") || shaderName.Contains("urp"))
        {
            // URP шейдеры
            if (shaderName.Contains("lit"))
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat("_WorkflowMode", 1f); // Metallic workflow
                
                // Дополнительные URP параметры
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 0f); // Opaque
                }
                if (material.HasProperty("_Blend"))
                {
                    material.SetFloat("_Blend", 0f); // Alpha
                }
                if (material.HasProperty("_AlphaClip"))
                {
                    material.SetFloat("_AlphaClip", 0f); // No alpha clipping
                }
            }
            else if (shaderName.Contains("simple lit"))
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
            }
            else if (shaderName.Contains("unlit"))
            {
                // Unlit шейдеры не имеют metallic/smoothness
                // Только цвет
            }
        }
        else if (shaderName.Contains("standard"))
        {
            // Стандартный шейдер Unity
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
        }
    }
    
    /// <summary>
    /// Создает материал для временных box монстров
    /// </summary>
    public static Material CreateMonsterBoxMaterial(Color monsterColor, MonsterBoxType boxType)
    {
        float metallic = 0.3f;
        float smoothness = 0.7f;
        
        // Настраиваем параметры в зависимости от типа монстра
        switch (boxType)
        {
            case MonsterBoxType.Tank:
                metallic = 0.5f;  // Более металлический
                smoothness = 0.8f; // Более гладкий
                break;
            case MonsterBoxType.Fast:
                metallic = 0.1f;  // Менее металлический
                smoothness = 0.5f; // Менее гладкий
                break;
            case MonsterBoxType.Magic:
                metallic = 0.2f;  // Средний металлик
                smoothness = 0.9f; // Очень гладкий (магический)
                break;
            case MonsterBoxType.Ranged:
                metallic = 0.4f;  // Средний металлик
                smoothness = 0.6f; // Средняя гладкость
                break;
        }
        
        return CreateURPMaterial(monsterColor, metallic, smoothness);
    }
    
    /// <summary>
    /// Проверяет доступность URP шейдеров
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogAvailableShaders()
    {
        Debug.Log("=== Available Shaders ===");
        
        string[] shadersToCheck = {
            "Universal Render Pipeline/Lit",
            "URP/Lit",
            "Shader Graphs/URP Lit", 
            "Universal Render Pipeline/Simple Lit",
            "URP/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "URP/Unlit",
            "Standard"
        };
        
        foreach (string shaderName in shadersToCheck)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                Debug.Log($"✅ {shaderName}");
            }
            else
            {
                Debug.LogWarning($"❌ {shaderName} - Not found");
            }
        }
    }
}
