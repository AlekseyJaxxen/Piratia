using UnityEngine;

public class PrefabCameraViewer : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float minRadius = 5f;
    public float maxRadius = 30f;
    public float minElevation = 20f;
    public float maxElevation = 80f;
    public float zoomSpeed = 0.1f;
    [Header("Rotation")]
    public float rotationSpeed = 3f;
    private float zoomFactor = 0.5f;
    private float azimuth = 0f;
    public Transform target; // Назначь в инспекторе на префаб монстра
    private Camera cam;

    void Start()
    {
        cam = Camera.main ?? GetComponent<Camera>();
        if (cam == null) { cam = gameObject.AddComponent<Camera>(); }
        cam.orthographic = false;
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;
        HandleZoom();
        HandleRotation();
        float radius = Mathf.Lerp(minRadius, maxRadius, zoomFactor);
        float elevation = Mathf.Lerp(minElevation, maxElevation, zoomFactor);
        Quaternion rotation = Quaternion.Euler(elevation, azimuth, 0);
        Vector3 targetPosition = target.position + rotation * Vector3.back * radius;
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, 0.1f);
        cam.transform.LookAt(target.position);
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