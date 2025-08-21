using UnityEngine;

public class BillBoardSprite : MonoBehaviour
{
    [Header("Settings")]
    public bool freezeXZRotation = true; // Фиксировать наклон по осям X/Z
    public bool reverseFacing = false;   // Развернуть спрайт на 180°

    private Camera mainCamera;

    void Start()
    {
        // Находим главную камеру автоматически
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("BillboardSprite: Main camera not found!");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Получаем направление от спрайта к камере
        Vector3 lookDirection = mainCamera.transform.position - transform.position;

        if (freezeXZRotation)
        {
            // Обнуляем Y-составляющую для 2D-подобного эффекта
            lookDirection.y = 0;
        }

        // Разворот при необходимости
        if (reverseFacing)
        {
            lookDirection = -lookDirection;
        }

        // Применяем поворот
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}