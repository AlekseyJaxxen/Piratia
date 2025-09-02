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

    public void SetData(string name, int currentHP, int maxHP)
    {
        if (nameText != null)
        {
            nameText.text = name;
            nameText.color = Color.red;
        }

        if (currentHP > 0)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }

        UpdateUI(currentHP, maxHP);

        Debug.Log($"[MonsterUI] UI updated with data: {currentHP}/{maxHP}. Active status: {gameObject.activeSelf}");
    }

    private void UpdateUI(int hp, int maxHP)
    {
        if (fillImage != null) fillImage.fillAmount = maxHP > 0 ? (float)hp / maxHP : 0f;
        if (hpText != null) hpText.text = $"{hp}/{maxHP}";
        if (hp < previousHealth && gameObject.activeSelf)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = hp;
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