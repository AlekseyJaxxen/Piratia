using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [SerializeField] private int targetFPS = 90;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0; // ��������� VSync
        Application.targetFrameRate = targetFPS; // ������������� ������� FPS
        DontDestroyOnLoad(gameObject); // ��������� ������ ����� �������
        Debug.Log($"[FPSLimiter] Target FPS set to {targetFPS}");
    }
}