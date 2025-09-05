using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [SerializeField] private int targetFPS = 90;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0; // Отключаем VSync
        Application.targetFrameRate = targetFPS; // Устанавливаем целевой FPS
        DontDestroyOnLoad(gameObject); // Сохраняем объект между сценами
        Debug.Log($"[FPSLimiter] Target FPS set to {targetFPS}");
    }
}