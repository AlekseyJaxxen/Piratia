using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    private Color originalColor;
    public Transform target;
    private Camera mainCamera;
    private Coroutine damageFlashCoroutine;

    void Start()
    {
        mainCamera = Camera.main;
        if (fillImage != null)
        {
            originalColor = fillImage.color;
            
            // DoTween removed
        }
        else
        {
            Debug.LogError("[HealthBarUI] FillImage not assigned!");
        }
    }

    // Оптимизация: интервалы обновления для UI
    private float _lastUIUpdate = 0f;
    private const float UI_UPDATE_INTERVAL = 0.1f; // Обновляем UI каждые 100мс
    
    void LateUpdate()
    {
        // Оптимизация: обновляем позицию UI с интервалом
        if (Time.time - _lastUIUpdate >= UI_UPDATE_INTERVAL)
        {
            if (target != null && mainCamera != null)
            {
                transform.position = target.position + offset;
                transform.LookAt(mainCamera.transform);
                transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
            }
            _lastUIUpdate = Time.time;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void UpdateHP(int current, int max)
    {
        if (!gameObject.activeSelf && current > 0)
        {
            gameObject.SetActive(true); 
        }
        if (fillImage != null) fillImage.fillAmount = (float)current / max;
        if (hpText != null) hpText.text = $"{current}/{max}";
    }

    public void PlayDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }
    
    private IEnumerator DamageFlashCoroutine()
    {
        if (fillImage != null)
        {
            fillImage.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            fillImage.color = originalColor;
        }
    }

    private void OnDisable()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
    }
}