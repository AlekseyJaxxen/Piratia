using UnityEngine;
using Mirror;

public class PlayerCameraController : MonoBehaviour
{
    public Camera CameraInstance { get; private set; }

    [Header("Orbit Settings")]
    public float minRadius = 5f; // Close zoom distance
    public float maxRadius = 30f; // Far zoom distance
    public float minElevation = 20f; // Min vertical angle (close, low tilt)
    public float maxElevation = 80f; // Max vertical angle (far, near top-down)
    public float zoomSpeed = 0.1f;

    [Header("Camera Follow")]
    [SerializeField] private float cameraDelay = 0.1f; // Задержка следования (в секундах)

    [Header("Rotation")]
    public float rotationSpeed = 3f;

    private float zoomFactor = 0.5f; // 0 close, 1 far
    private float azimuth = 0f; // Horizontal angle
    private float elevation; // Vertical angle

    private Transform _target;
    private PlayerCore _core;
    private Vector3 cameraVelocity = Vector3.zero; // Для SmoothDamp
    private Transform pivot; // Ссылка на наш Camera Pivot

    public void Init(PlayerCore core)
    {
        _core = core;
        if (!_core.isLocalPlayer) return;

        CameraInstance = Camera.main;
        if (CameraInstance == null) return;

        _target = core.transform;
        CameraInstance.orthographic = false;

        // Создаем Pivot, если его нет.
        if (CameraInstance.transform.parent == null || CameraInstance.transform.parent.name != "Camera Pivot")
        {
            GameObject pivotGO = new GameObject("Camera Pivot");
            CameraInstance.transform.SetParent(pivotGO.transform);
            pivot = pivotGO.transform;
        }
        else
        {
            pivot = CameraInstance.transform.parent;
        }

        // Перемещаем Pivot к персонажу и сбрасываем локальные координаты камеры
        pivot.position = _target.position;
        // Устанавливаем начальное вращение, чтобы камера смотрела на игрока.
        pivot.rotation = Quaternion.Euler(minElevation, _target.rotation.eulerAngles.y, 0);
    }

    void LateUpdate()
    {
        if (_core == null || !_core.isLocalPlayer || CameraInstance == null) return;

        HandleInput();

        // Обновляем позицию Pivot
        pivot.position = Vector3.SmoothDamp(
            pivot.position,
            _target.position,
            ref cameraVelocity,
            cameraDelay
        );

        // Обновляем вращение Pivot.
        // Используем Slerp, чтобы вращение было плавным.
        Quaternion targetPivotRotation = Quaternion.Euler(elevation, azimuth, 0);
        pivot.rotation = targetPivotRotation;

        // Обновляем локальную позицию камеры для зума.
        float radius = Mathf.Lerp(minRadius, maxRadius, zoomFactor);
        CameraInstance.transform.localPosition = new Vector3(0, 0, -radius);

        // Убеждаемся, что камера всегда смотрит на игрока (на локальные координаты 0,0,0 Pivot'а)
        // Это и есть недостающая часть.
        CameraInstance.transform.LookAt(pivot.position);
    }

    private void HandleInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) zoomFactor = Mathf.Clamp(zoomFactor - scroll * zoomSpeed, 0f, 1f);

        elevation = Mathf.Lerp(minElevation, maxElevation, zoomFactor);

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            azimuth += mouseX * rotationSpeed;
        }
    }
}