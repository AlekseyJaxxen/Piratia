using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    private Sequence damageFlashSequence;
    private Color originalColor;
    public Transform target;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (fillImage != null)
        {
            originalColor = fillImage.color;
            // Создаём последовательность для эффекта вспышки
            damageFlashSequence = DOTween.Sequence();
            damageFlashSequence.Append(fillImage.DOColor(Color.red, 0.1f));
            damageFlashSequence.Append(fillImage.DOColor(originalColor, 0.1f));
            damageFlashSequence.SetAutoKill(false);
            damageFlashSequence.Pause();
        }
        else
        {
            Debug.LogError("[HealthBarUI] FillImage not assigned!");
        }
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(mainCamera.transform);
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
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
            gameObject.SetActive(true); // Активируем UI, если здоровье > 0
        }
        if (fillImage != null) fillImage.fillAmount = (float)current / max;
        if (hpText != null) hpText.text = $"{current}/{max}";
    }

    public void PlayDamageFlash()
    {
        if (damageFlashSequence != null && gameObject.activeSelf)
        {
            damageFlashSequence.Rewind();
            damageFlashSequence.Play();
        }
    }

    private void OnDisable()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Kill();
        }
    }
}