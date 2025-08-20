using UnityEngine;
using Mirror;

public class PlayerCameraController : MonoBehaviour
{
    public Camera CameraInstance { get; private set; }

    [Header("Isometric Settings")]
    [SerializeField] private float height = 15f;
    [SerializeField] private float distance = 15f;
    [SerializeField] private float angle = 45f;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Camera Controls")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    private Transform _target;
    private Vector3 _offset;
    private PlayerCore _core;

    public void Init(PlayerCore core)
    {
        _core = core;

        if (!_core.isLocalPlayer)
        {
            return;
        }

        CameraInstance = Camera.main;

        if (CameraInstance == null)
        {
            Debug.LogError("[Client] Main Camera not found in scene!");
            return;
        }

        CameraInstance.orthographic = true;
        CameraInstance.orthographicSize = 10f;
        CameraInstance.nearClipPlane = 0.3f;
        CameraInstance.farClipPlane = 100f;

        _target = core.transform;

        // 🚨 УЛУЧШЕНО: Расчет смещения перенесен в LateUpdate, чтобы можно было его изменять
        UpdateOffset();

        CameraInstance.transform.position = _target.position + _offset;
        CameraInstance.transform.rotation = Quaternion.Euler(angle, 0, 0);

        Debug.Log($"[Client] Main camera configured for {gameObject.name}");
    }

    void LateUpdate()
    {
        if (!_core.isLocalPlayer || _target == null || CameraInstance == null)
        {
            return;
        }

        // 🚨 НОВОЕ: Обработка зума
        HandleZoom();

        // 🚨 НОВОЕ: Обработка поворота
        HandleRotation();

        // Плавное перемещение камеры к новой позиции
        Vector3 desiredPosition = _target.position + _offset;
        CameraInstance.transform.position = Vector3.Lerp(
            CameraInstance.transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            // Уменьшаем/увеличиваем расстояние до цели
            distance = Mathf.Clamp(distance - scrollInput * zoomSpeed, minZoom, maxZoom);
            // Обновляем смещение камеры
            UpdateOffset();
        }
    }

    private void HandleRotation()
    {
        // Поворачиваем камеру, только если зажата левая кнопка мыши
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (mouseX != 0)
            {
                // Поворачиваем смещение вокруг оси Y
                _offset = Quaternion.Euler(0, mouseX * rotationSpeed, 0) * _offset;

                // Чтобы избежать "крена" камеры, фиксируем ее вращение по осям X и Z
                // Это сохранит изометрический вид.
                CameraInstance.transform.rotation = Quaternion.Euler(angle, CameraInstance.transform.eulerAngles.y + mouseX * rotationSpeed, 0);
            }
        }
    }

    // Метод для обновления смещения, чтобы его можно было вызывать из других мест
    private void UpdateOffset()
    {
        _offset = Quaternion.Euler(angle, 0, 0) * Vector3.back * distance;
        _offset.y = height;
    }
}