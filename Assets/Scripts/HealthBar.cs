using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image _healthBarImage;
    [SerializeField] private Image _damageFlashImage;
    [SerializeField] private Vector3 _offset = new Vector3(0, 2f, 0);
    private Health _health;
    private float _flashDuration = 0.5f;
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
            // Инициализируем полоску сразу
            UpdateHealthBar(_health.CurrentHealth, _health.MaxHealth);
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
            // Всегда используем актуальные значения из Health компонента
            float currentRatio = (float)currentHealth / _health.MaxHealth;
            float previousRatio = _healthBarImage.fillAmount;

            _healthBarImage.fillAmount = currentRatio;

            if (currentRatio < previousRatio)
            {
                _damageFlashImage.fillAmount = previousRatio;
                _flashTimer = _flashDuration;
                _damageFlashImage.color = new Color(1f, 1f, 1f, 1f);
            }
            else if (currentRatio > previousRatio)
            {
                // Если здоровье увеличилось (хил), сразу обновляем flash
                _damageFlashImage.fillAmount = currentRatio;
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