using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;
    public TextMeshProUGUI hpText; // Опционально
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2f, 0); // Высота над головой
    private int previousHealth = int.MaxValue;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate() // Используем LateUpdate для точной позиции после движения
    {
        if (target != null && mainCamera != null && gameObject.activeInHierarchy)
        {
            Vector3 worldPos = target.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0) // Видим только если перед камерой
            {
                transform.position = screenPos;
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void Initialize(PlayerCore player)
    {
        target = player.transform;
    }

    public void UpdateHP(int current, int max)
    {
        if (fillImage != null && gameObject.activeInHierarchy)
        {
            fillImage.fillAmount = (float)current / max;
        }
        if (hpText != null && gameObject.activeInHierarchy)
        {
            hpText.text = $"{current}/{max}";
        }
        if (current < previousHealth && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = current;
    }

    private IEnumerator FlashHealthBar()
    {
        if (fillImage != null && gameObject.activeInHierarchy)
        {
            Color originalColor = fillImage.color;
            for (int i = 0; i < 3; i++)
            {
                fillImage.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                fillImage.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void OnDestroy()
    {
        // Отписка, если нужно
    }
}