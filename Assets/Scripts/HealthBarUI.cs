using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;
    public TextMeshProUGUI hpText; // Для HP "{current}/{max}"
    // Если нужно имя, добавь: public TextMeshProUGUI nameText; и обновляй в UpdateHP или отдельно
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2f, 0);
    private int previousHealth = int.MaxValue;

    void Start()
    {
        mainCamera = Camera.main;
        // UpdateHP(0, 100); // Инициализация при старте (default values)
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.position = target.position + offset;
            //transform.LookAt(mainCamera.transform);
           // transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f); // Billboard
        }
    }

    public void UpdateHP(int current, int max)
    {
        if (fillImage != null) fillImage.fillAmount = (float)current / max;
        if (hpText != null) hpText.text = $"{current}/{max}";
        if (current < previousHealth)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = current;
    }

    private IEnumerator FlashHealthBar()
    {
        if (fillImage == null) yield break;
        Color originalColor = fillImage.color;
        fillImage.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        fillImage.color = originalColor;
    }
}