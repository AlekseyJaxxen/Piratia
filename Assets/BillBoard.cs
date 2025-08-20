using UnityEngine;

public class BillBoard : MonoBehaviour
{
    // Ссылка на камеру, за которой должен следовать объект
    // Если поле не заполнено, скрипт автоматически найдет главную камеру.
    public Transform targetCamera;

    void Start()
    {
        // Находим главную камеру, если она не задана вручную.
        if (targetCamera == null)
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogError("Main Camera not found! Please tag your camera as 'MainCamera'.");
            }
        }
    }

    void Update()
    {
        // Проверяем, существует ли цель (камера).
        if (targetCamera != null)
        {
            // Направляем объект на камеру, но только по оси Y.
            // Это предотвращает наклон объекта и делает его всегда вертикальным.
            transform.LookAt(transform.position + targetCamera.forward);
        }
    }
}