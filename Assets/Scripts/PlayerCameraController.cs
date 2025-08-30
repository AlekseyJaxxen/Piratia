using UnityEngine;
using Mirror;

public class PlayerCameraController : MonoBehaviour
{
    public Camera CameraInstance { get; private set; }

    [Header("Orbit Settings")]
    [SerializeField] private float minRadius = 5f; // Close zoom distance
    [SerializeField] private float maxRadius = 30f; // Far zoom distance
    [SerializeField] private float minElevation = 20f; // Min vertical angle (close, low tilt)
    [SerializeField] private float maxElevation = 80f; // Max vertical angle (far, near top-down)
    [SerializeField] private float zoomSpeed = 0.1f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 3f;

    private float zoomFactor = 0.5f; // 0 close, 1 far
    private float azimuth = 0f; // Horizontal angle
    private Transform _target;
    private PlayerCore _core;

    public void Init(PlayerCore core)
    {
        _core = core;
        if (!_core.isLocalPlayer) return;
        CameraInstance = Camera.main;
        if (CameraInstance == null) return;
        _target = core.transform;
        CameraInstance.orthographic = false;
    }

    void LateUpdate()
    {
        if (_core == null || !_core.isLocalPlayer || CameraInstance == null) return;

        HandleZoom();
        HandleRotation();

        float radius = Mathf.Lerp(minRadius, maxRadius, zoomFactor);
        float elevation = Mathf.Lerp(minElevation, maxElevation, zoomFactor);

        Quaternion rotation = Quaternion.Euler(elevation, azimuth, 0);
        Vector3 position = _target.position + rotation * Vector3.back * radius;

        CameraInstance.transform.position = position;
        CameraInstance.transform.LookAt(_target.position);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) zoomFactor = Mathf.Clamp(zoomFactor - scroll * zoomSpeed, 0f, 1f);
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (mouseX != 0) azimuth += mouseX * rotationSpeed;
        }
    }
}