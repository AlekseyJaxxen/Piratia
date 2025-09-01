using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MonsterUI : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Перетащи "Fill"
    [SerializeField] private TextMeshProUGUI hpText; // "HPText"
    [SerializeField] private TextMeshProUGUI nameText; // "NameText"
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2f, 0);
    private int previousHealth = int.MaxValue;
    [SerializeField] private float flashDuration = 0.3f;

    void Start()
    {
        mainCamera = Camera.main;
        //UpdateHP(100, 100); // Init HP
        //UpdateName("Monster"); // Init name
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

    public void UpdateName(string monsterName)
    {
        if (nameText != null) nameText.text = monsterName;
    }

    private IEnumerator FlashHealthBar()
    {
        if (fillImage == null) yield break;
        Color originalColor = fillImage.color;
        fillImage.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        fillImage.color = originalColor;
    }
}