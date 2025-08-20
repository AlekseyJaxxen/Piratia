using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerUI : NetworkBehaviour
{
    [Header("UI References")]
    public Canvas worldCanvas;
    public Image healthBarFill;
    public Text nameText;
    public Vector3 worldOffset = new Vector3(0, 2.2f, 0);

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";

    private Health _health;
    private Transform _cameraTransform;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _cameraTransform = Camera.main.transform;

        // ������������� ������� Canvas ���� �� ��������
        if (worldCanvas == null) worldCanvas = GetComponentInChildren<Canvas>();
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            worldCanvas.enabled = false;
            return;
        }

        nameText.text = playerName;
        _health.HealthChanged += UpdateHealthBar;
    }

    private void LateUpdate()
    {
        if (isLocalPlayer) return;

        // ������� ��� �������
        worldCanvas.transform.position = transform.position + worldOffset;

        // ������� ������ � ������ (Billboard ������)
        worldCanvas.transform.forward = _cameraTransform.forward;
    }

    private void OnNameChanged(string oldName, string newName)
    {
        nameText.text = newName;
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        healthBarFill.fillAmount = (float)currentHealth / maxHealth;

        // �������� �� �������� � ��������
        healthBarFill.color = Color.Lerp(
            new Color(0.8f, 0.1f, 0.1f),
            new Color(0.2f, 0.8f, 0.2f),
            healthBarFill.fillAmount
        );
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.HealthChanged -= UpdateHealthBar;
    }
}