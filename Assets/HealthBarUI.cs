using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthFillImage;

    private Health targetHealth;

    public void Initialize(Health healthComponent)
    {
        targetHealth = healthComponent;
        targetHealth.HealthChanged += UpdateHealthBar;
        UpdateHealthBar(healthComponent.maxHealth, healthComponent.maxHealth);
    }

    private void UpdateHealthBar(int oldHealth, int newHealth)
    {
        float fillAmount = (float)newHealth / targetHealth.maxHealth;
        healthFillImage.fillAmount = fillAmount;

        // Изменение цвета (опционально)
        //healthFillImage.color = Color.Lerp(Color.red, Color.green, fillAmount);
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
            targetHealth.HealthChanged -= UpdateHealthBar;
    }
}