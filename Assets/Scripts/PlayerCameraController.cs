using UnityEngine;
using Mirror;

public class PlayerCameraController : MonoBehaviour
{
    public Camera CameraInstance { get; private set; }

    [Header("Isometric Settings")]
    [SerializeField] private float height = 15f;
    [SerializeField] private float distance = 15f;
    [SerializeField] private float angle = 45f;

    [Header("Camera Controls")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minFOV = 15f;
    [SerializeField] private float maxFOV = 60f;

    private Transform _target;
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

        _target = core.transform;

        // Устанавливаем перспективную камеру для зума через Field of View
        CameraInstance.orthographic = false;
        CameraInstance.fieldOfView = maxFOV; // Начальное поле зрения
        CameraInstance.transform.rotation = Quaternion.Euler(angle, 0, 0);

        Debug.Log($"[Client] Main camera configured for {gameObject.name}");
    }

    void LateUpdate()
    {
        if (_core == null || !_core.isLocalPlayer || CameraInstance == null)
        {
            return;
        }

        HandleRotation();
        HandleZoom();

        // Вычисляем смещение и устанавливаем позицию камеры
        Vector3 offset = Quaternion.Euler(0, CameraInstance.transform.eulerAngles.y, 0) * new Vector3(0, height, -distance);
        CameraInstance.transform.position = _target.position + offset;
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            // Изменяем поле зрения (FOV) для эффекта зума
            CameraInstance.fieldOfView = Mathf.Clamp(CameraInstance.fieldOfView - scrollInput * zoomSpeed, minFOV, maxFOV);
        }
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (mouseX != 0)
            {
                // Поворачиваем камеру вокруг оси Y
                CameraInstance.transform.RotateAround(_target.position, Vector3.up, mouseX * rotationSpeed);
            }
        }
    }
}