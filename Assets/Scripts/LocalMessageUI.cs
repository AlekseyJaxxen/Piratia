using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocalMessageUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI messageText;
    public CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    public float floatSpeed = 1f;
    public float floatHeight = 0.5f;
    public bool useFloatingAnimation = true;
    
    [Header("Position Settings")]
    public float baseHeight = 2.5f; // Базовая высота сообщения
    
    private Vector3 startPosition;
    private float timeElapsed = 0f;
    
    void Start()
    {
        // Устанавливаем базовую высоту
        startPosition = new Vector3(0, baseHeight, 0);
        transform.localPosition = startPosition;
        
        // Настраиваем CanvasGroup если не назначен
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Настраиваем TextMeshPro если не назначен
        if (messageText == null)
        {
            messageText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Настраиваем Screen Space - Overlay Canvas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Поверх других элементов
        
        // Добавляем GraphicRaycaster для Screen Space Canvas
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
    
    void Update()
    {
        if (useFloatingAnimation)
        {
            timeElapsed += Time.deltaTime;
            
            // Плавающая анимация
            float yOffset = Mathf.Sin(timeElapsed * floatSpeed) * floatHeight;
            transform.localPosition = startPosition + Vector3.up * yOffset;
        }
    }
    
    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
    
    public void SetColor(Color color)
    {
        if (messageText != null)
        {
            messageText.color = color;
        }
    }
    
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
    
    public void SetHeight(float height)
    {
        baseHeight = height;
        startPosition = new Vector3(0, baseHeight, 0);
        transform.localPosition = startPosition;
    }
}
