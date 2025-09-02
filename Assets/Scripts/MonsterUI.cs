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
    [SyncVar(hook = nameof(OnMaxHealthChanged))]
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
            Debug.Log($"[MonsterUI] Name updated: {newName}, isClient={isClient}");
        }
    }

    private void OnHPChanged(int _, int newHP)
    {
        UpdateUI(newHP, maxHealth);
    }

    private void OnMaxHealthChanged(int _, int newMaxHP)
    {
        UpdateUI(currentHealth, newMaxHP);
    }

    private void UpdateUI(int hp, int maxHP)
    {
        if (!gameObject.activeSelf && hp > 0)
        {
            gameObject.SetActive(true);
        }
        else if (hp <= 0)
        {
            gameObject.SetActive(false);
        }
        if (fillImage != null) fillImage.fillAmount = maxHP > 0 ? (float)hp / maxHP : 0f;
        if (hpText != null) hpText.text = $"{hp}/{maxHP}";
        if (hp < previousHealth && gameObject.activeSelf)
        {
            StartCoroutine(FlashHealthBar());
        }
        previousHealth = hp;
        Debug.Log($"[MonsterUI] UI updated: {hp}/{maxHP}, isClient={isClient}");
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