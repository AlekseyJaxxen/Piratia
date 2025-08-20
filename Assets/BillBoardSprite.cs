using UnityEngine;

public class BillBoardSprite : MonoBehaviour
{
    [Header("Settings")]
    public bool freezeXZRotation = true; // ����������� ������ �� ���� X/Z
    public bool reverseFacing = false;   // ���������� ������ �� 180�

    private Camera mainCamera;

    void Start()
    {
        // ������� ������� ������ �������������
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

        // �������� ����������� �� ������� � ������
        Vector3 lookDirection = mainCamera.transform.position - transform.position;

        if (freezeXZRotation)
        {
            // �������� Y-������������ ��� 2D-��������� �������
            lookDirection.y = 0;
        }

        // �������� ��� �������������
        if (reverseFacing)
        {
            lookDirection = -lookDirection;
        }

        // ��������� �������
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}