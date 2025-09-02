using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Mirror;

public class MonsterUI : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float flashDuration = 0.3f;
    public Transform target;
    private Camera mainCamera;
    private int previousHealth = int.MaxValue;

    [SyncVar(hook = nameof(OnNameChanged))]
    private string monsterName;
    [SyncVar(hook = nameof(OnHPChanged))]
    private int currentHealth;
    [SyncVar]
    private int maxHealth;

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

    private void OnNameChanged(string _, string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
            nameText.color = Color.red;
        }
    }

    private void OnHPChanged(int _, int newHP)
    {
        if (!gameObject.activeSelf && newHP > 0)
        {
            gameObject.SetActive(true);
        }
        else if (newHP <= 0)
        {
            gameObject.SetActive(false);
        }
        if (fillImage != null) fillImage.fillAmount = (float)newHP / maxHealth;
        if (hpText != null) hpText.text = $"{newHP}/{maxHealth}";
        if (newHP < previousHealth && gameObject.activeSelf)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = newHP;
    }

    [Server]
    public void SetData(string name, int currentHP, int maxHP)
    {
        monsterName = name;
        currentHealth = currentHP;
        maxHealth = maxHP;
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