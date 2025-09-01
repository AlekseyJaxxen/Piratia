using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MonsterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float flashDuration = 0.3f;
    public Transform target;
    private Camera mainCamera;
    private int previousHealth = int.MaxValue;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(mainCamera.transform);
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        }
    }

    public void UpdateName(string monsterName)
    {
        if (nameText != null)
        {
            nameText.text = monsterName;
            nameText.color = Color.red; // Всегда красный для монстра
        }
    }

    public void UpdateHP(int current, int max)
    {
        if (!gameObject.activeSelf && current > 0)
        {
            gameObject.SetActive(true); // Активируем UI, если здоровье > 0
        }
        if (fillImage != null) fillImage.fillAmount = (float)current / max;
        if (hpText != null) hpText.text = $"{current}/{max}";
        if (current < previousHealth && gameObject.activeSelf)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = current;
    }

    private IEnumerator FlashHealthBar()
    {
        if (fillImage == null || !gameObject.activeSelf) yield break;
        Color originalColor = fillImage.color;
        fillImage.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        fillImage.color = originalColor;
    }
}