using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image _healthBarImage;
    [SerializeField] private Image _damageFlashImage; // Новый Image для эффекта
    [SerializeField] private Vector3 _offset = new Vector3(0, 2f, 0);
    private Health _health;
    private float _flashDuration = 0.5f; // Длительность эффекта
    private float _flashTimer;

    private void Awake()
    {
        if (_healthBarImage == null)
        {
            _healthBarImage = GetComponent<Image>();
            if (_healthBarImage == null)
            {
                Debug.LogError("Image component not found on the GameObject.", this);
            }
        }

        if (_damageFlashImage == null)
        {
            GameObject flashObj = new GameObject("DamageFlash");
            flashObj.transform.SetParent(_healthBarImage.transform, false);
            _damageFlashImage = flashObj.AddComponent<Image>();
            _damageFlashImage.color = Color.white;
            _damageFlashImage.fillAmount = 1f;
            _damageFlashImage.type = Image.Type.Filled;
            _damageFlashImage.fillMethod = Image.FillMethod.Horizontal;
            _damageFlashImage.rectTransform.sizeDelta = _healthBarImage.rectTransform.sizeDelta;
        }

        _health = GetComponentInParent<Health>();
        if (_health == null)
        {
            Debug.LogError("Health component not found on the parent object.", this);
        }
        else
        {
            _health.OnHealthUpdated += UpdateHealthBar;
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthUpdated -= UpdateHealthBar;
        }
    }

    private void Update()
    {
        if (_health != null)
        {
            transform.position = _health.transform.position + _offset;
            UpdateFlashEffect();
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (_health != null && _healthBarImage != null)
        {
            float newHealthRatio = (float)currentHealth / maxHealth;
            float previousHealthRatio = _healthBarImage.fillAmount;
            _healthBarImage.fillAmount = newHealthRatio;

            if (newHealthRatio < previousHealthRatio)
            {
                _damageFlashImage.fillAmount = previousHealthRatio;
                _flashTimer = _flashDuration;
                _damageFlashImage.color = new Color(1f, 1f, 1f, 1f);
            }
        }
    }

    private void UpdateFlashEffect()
    {
        if (_flashTimer > 0)
        {
            _flashTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_flashTimer / _flashDuration);
            _damageFlashImage.color = new Color(1f, 1f, 1f, alpha);
            if (_flashTimer <= 0)
            {
                _damageFlashImage.fillAmount = _healthBarImage.fillAmount;
            }
        }
    }
}