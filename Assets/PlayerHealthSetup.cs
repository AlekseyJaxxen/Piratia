using UnityEngine;
using Mirror;

public class PlayerHealthSetup : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Transform healthBarPosition; // Пустой объект над головой
    [SerializeField] private Health playerHealth;

    private GameObject healthBarInstance;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (healthBarPrefab != null)
        {
            CreateHealthBar();
        }
    }

    private void CreateHealthBar()
    {
        // Находим WorldCanvas в сцене
        Canvas worldCanvas = FindObjectOfType<Canvas>();

        healthBarInstance = Instantiate(healthBarPrefab, worldCanvas.transform);

        // Настраиваем HealthBarUI
        HealthBarUI healthBarUI = healthBarInstance.GetComponent<HealthBarUI>();
        if (healthBarUI != null)
        {
            healthBarUI.Initialize(playerHealth);
        }

        // Добавляем компонент для следования за игроком
        HealthBarFollow follow = healthBarInstance.AddComponent<HealthBarFollow>();
        follow.Setup(healthBarPosition, Camera.main);
    }

    private void OnDestroy()
    {
        if (healthBarInstance != null)
            Destroy(healthBarInstance);
    }
}