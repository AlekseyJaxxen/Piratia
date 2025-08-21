using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ������ �� ��������� Image ��� ���������� ������� ��������
    [SerializeField] private Image _healthBarImage;

    // �������� ������ �������� �� ������ �������
    [SerializeField] private Vector3 _offset = new Vector3(0, 2f, 0);

    // ������ �� ��������� Health, ����� �������� ������ � ��������
    private Health _health;

    private void Awake()
    {
        // ������� ��������� Image �� ������� �������
        if (_healthBarImage == null)
        {
            _healthBarImage = GetComponent<Image>();
            if (_healthBarImage == null)
            {
                Debug.LogError("Image component not found on the GameObject.", this);
            }
        }

        // ������� ��������� Health �� ������������ �������
        _health = GetComponentInParent<Health>();
        if (_health == null)
        {
            Debug.LogError("Health component not found on the parent object. Please attach this HealthBar to a player or enemy.", this);
        }
    }

    private void Start()
    {
        // ��������� ������ �������� � ������
        UpdateHealthBar();
    }

    private void Update()
    {
        // ������������� ��������� ������ �������� ��� ����������
        transform.position = _health.transform.position + _offset;

     
        // ��������� ������ ��������
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (_health != null && _healthBarImage != null)
        {
            // ��������� ������� ��������
            float healthRatio = (float)_health.CurrentHealth / _health.MaxHealth;
            // ��������� fillAmount �����������
            _healthBarImage.fillAmount = healthRatio;
        }
    }
}