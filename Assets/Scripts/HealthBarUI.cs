using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;
    public TextMeshProUGUI hpText; // �����������
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2f, 0); // ������ ��� �������

    private int previousHealth = int.MaxValue;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate() // ����������� LateUpdate ��� ������ ������� ����� ��������
    {
        if (target != null && mainCamera != null)
        {
            Vector3 worldPos = target.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0) // ����� ������ ���� ����� �������
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

    public void UpdateHP(int current, int max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = (float)current / max;
            // fillImage.color = Color.Lerp(Color.red, Color.green, fillImage.fillAmount); // ������
        }
        if (hpText != null)
        {
            hpText.text = $"{current}/{max}";
        }
        if (current < previousHealth)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = current;
    }

    private IEnumerator FlashHealthBar()
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

    void OnDestroy()
    {
        // �������, ���� �����
    }
}