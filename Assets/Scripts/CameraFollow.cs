using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float searchInterval = 1f; // Интервал поиска игрока

    [Header("Camera Rotation")]
    [SerializeField] private bool rotateWithTarget = true;
    [SerializeField] private float rotationSpeed = 3f;

    private Coroutine searchCoroutine;

    private void Start()
    {
        InitializeCamera();
        StartSearchRoutine();
    }

    private void InitializeCamera()
    {
        if (target == null)
        {
            FindPlayerImmediate();
        }

        if (target != null)
        {
            transform.position = target.position + offset;
            if (rotateWithTarget)
            {
                transform.LookAt(target);
            }
        }
    }

    private void StartSearchRoutine()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
        }
        searchCoroutine = StartCoroutine(SearchPlayerRoutine());
    }

    private IEnumerator SearchPlayerRoutine()
    {
        while (target == null)
        {
            FindPlayerImmediate();
            yield return new WaitForSeconds(searchInterval);
        }
    }

    private void FindPlayerImmediate()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            Debug.Log("CameraFollow: Player found and assigned as target");
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraPosition();
        UpdateCameraRotation();
    }

    private void UpdateCameraPosition()
    {
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    private void UpdateCameraRotation()
    {
        if (!rotateWithTarget) return;

        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null && searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
        }
    }
}