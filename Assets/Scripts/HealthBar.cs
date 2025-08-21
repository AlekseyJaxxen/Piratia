using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // Ссылка на компонент Image для управления полосой здоровья
    [SerializeField] private Image _healthBarImage;

    // Смещение полосы здоровья от центра объекта
    [SerializeField] private Vector3 _offset = new Vector3(0, 2f, 0);

    // Ссылка на компонент Health, чтобы получать данные о здоровье
    private Health _health;

    private void Awake()
    {
        // Находим компонент Image на текущем объекте
        if (_healthBarImage == null)
        {
            _healthBarImage = GetComponent<Image>();
            if (_healthBarImage == null)
            {
                Debug.LogError("Image component not found on the GameObject.", this);
            }
        }

        // Находим компонент Health на родительском объекте
        _health = GetComponentInParent<Health>();
        if (_health == null)
        {
            Debug.LogError("Health component not found on the parent object. Please attach this HealthBar to a player or enemy.", this);
        }
    }

    private void Start()
    {
        // Обновляем полосу здоровья в начале
        UpdateHealthBar();
    }

    private void Update()
    {
        // Устанавливаем положение полосы здоровья над персонажем
        transform.position = _health.transform.position + _offset;

     
        // Обновляем полосу здоровья
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (_health != null && _healthBarImage != null)
        {
            // Вычисляем процент здоровья
            float healthRatio = (float)_health.CurrentHealth / _health.MaxHealth;
            // Обновляем fillAmount изображения
            _healthBarImage.fillAmount = healthRatio;
        }
    }
}