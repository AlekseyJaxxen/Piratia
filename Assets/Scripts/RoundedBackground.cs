using UnityEngine;
using UnityEngine.UI;

public class RoundedBackground : MonoBehaviour
{
    [Header("Background Settings")]
    public Color backgroundColor = new Color(0, 0, 0, 0.7f);
    public float cornerRadius = 5f;
    public int resolution = 32;
    
    private Image backgroundImage;
    
    void Start()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
        
        CreateRoundedBackground();
    }
    
    void CreateRoundedBackground()
    {
        if (backgroundImage == null) return;
        
        // Создаем текстуру с скругленными углами
        Texture2D texture = CreateRoundedTexture(resolution, resolution, cornerRadius);
        
        // Создаем спрайт
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
        
        // Применяем спрайт
        backgroundImage.sprite = sprite;
        backgroundImage.color = backgroundColor;
        backgroundImage.type = Image.Type.Sliced;
    }
    
    Texture2D CreateRoundedTexture(int width, int height, float radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Нормализуем радиус
        float normalizedRadius = radius / (width * 0.5f);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Нормализуем координаты
                float normalizedX = (x - width * 0.5f) / (width * 0.5f);
                float normalizedY = (y - height * 0.5f) / (height * 0.5f);
                
                // Вычисляем расстояние от центра
                float distance = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                
                // Определяем альфу
                float alpha = 1f;
                if (distance > 1f - normalizedRadius)
                {
                    if (distance > 1f)
                    {
                        alpha = 0f;
                    }
                    else
                    {
                        // Плавный переход
                        float t = (distance - (1f - normalizedRadius)) / normalizedRadius;
                        alpha = 1f - t;
                    }
                }
                
                // Устанавливаем цвет пикселя
                Color pixelColor = new Color(1f, 1f, 1f, alpha);
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }
    }
    
    public void SetCornerRadius(float radius)
    {
        cornerRadius = radius;
        CreateRoundedBackground();
    }
}
