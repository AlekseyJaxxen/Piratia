using UnityEngine;

public class HealthBarFollow : MonoBehaviour
{
    private Transform target;
    private Camera mainCamera;
    private RectTransform rectTransform;

    public void Setup(Transform followTarget, Camera camera)
    {
        target = followTarget;
        mainCamera = camera;
        rectTransform = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(target.position);
            rectTransform.position = screenPoint;
        }
    }
}