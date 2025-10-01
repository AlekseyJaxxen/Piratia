using UnityEngine;

public class ScreenSpaceFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 worldOffset = new Vector3(0, 2.5f, 0);
    public float followSpeed = 10f;
    public bool smoothFollow = true;
    
    [Header("Screen Position")]
    public float screenOffsetY = 50f; // Смещение в пикселях вверх
    
    private Camera mainCamera;
    private RectTransform rectTransform;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        rectTransform = GetComponent<RectTransform>();
    }
    
    void Update()
    {
        if (target == null || mainCamera == null || rectTransform == null) return;
        
        // Конвертируем мировую позицию в экранные координаты
        Vector3 worldPosition = target.position + worldOffset;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        
        // Проверяем, находится ли объект перед камерой
        if (screenPosition.z > 0)
        {
            // Добавляем смещение в экранных координатах для позиционирования над персонажем
            screenPosition.y += screenOffsetY;
            
            if (smoothFollow)
            {
                // Плавное следование
                rectTransform.position = Vector3.Lerp(rectTransform.position, screenPosition, followSpeed * Time.deltaTime);
            }
            else
            {
                // Мгновенное следование
                rectTransform.position = screenPosition;
            }
        }
        else
        {
            // Объект за камерой - скрываем
            if (rectTransform.gameObject.activeInHierarchy)
            {
                rectTransform.gameObject.SetActive(false);
            }
        }
    }
    
    public void SetTarget(Transform targetTransform, Vector3 offset)
    {
        target = targetTransform;
        worldOffset = offset;
        
        if (rectTransform != null)
        {
            rectTransform.gameObject.SetActive(true);
        }
    }
    
    public void StopFollowing()
    {
        target = null;
        if (rectTransform != null)
        {
            rectTransform.gameObject.SetActive(false);
        }
    }
}
