using UnityEngine;
using Mirror;

public class PlayerHealthSetup : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Transform healthBarPosition; // ������ ������ ��� �������
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
        // ������� WorldCanvas � �����
        Canvas worldCanvas = FindObjectOfType<Canvas>();

        healthBarInstance = Instantiate(healthBarPrefab, worldCanvas.transform);

        // ����������� HealthBarUI
        HealthBarUI healthBarUI = healthBarInstance.GetComponent<HealthBarUI>();
        if (healthBarUI != null)
        {
            healthBarUI.Initialize(playerHealth);
        }

        // ��������� ��������� ��� ���������� �� �������
        HealthBarFollow follow = healthBarInstance.AddComponent<HealthBarFollow>();
        follow.Setup(healthBarPosition, Camera.main);
    }

    private void OnDestroy()
    {
        if (healthBarInstance != null)
            Destroy(healthBarInstance);
    }
}