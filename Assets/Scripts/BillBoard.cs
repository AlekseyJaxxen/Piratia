using UnityEngine;

public class BillBoard : MonoBehaviour
{
    // ������ �� ������, �� ������� ������ ��������� ������
    // ���� ���� �� ���������, ������ ������������� ������ ������� ������.
    public Transform targetCamera;

    void Start()
    {
        // ������� ������� ������, ���� ��� �� ������ �������.
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
        // ���������, ���������� �� ���� (������).
        if (targetCamera != null)
        {
            // ���������� ������ �� ������, �� ������ �� ��� Y.
            // ��� ������������� ������ ������� � ������ ��� ������ ������������.
            transform.LookAt(transform.position + targetCamera.forward);
        }
    }
}