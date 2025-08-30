using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections;

public class MonsterHealthBarUI : NetworkBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float flashDuration = 0.3f;
    private Monster monster;
    private int previousHealth = int.MaxValue;

    private void Awake()
    {
        monster = GetComponentInParent<Monster>();
        if (monster == null)
        {
            Debug.LogError($"[MonsterHealthBarUI] Monster component missing on {gameObject.name}");
            gameObject.SetActive(false);
            return;
        }

        // Ensure Canvas is properly configured
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UI"; // Ensure this is a high-priority layer
            canvas.sortingOrder = 100; // High order to render above other objects
        }
    }

    private void Start()
    {
        UpdateHP(monster.currentHealth, monster.maxHealth);
    }

    private void LateUpdate()
    {
        if (monster != null)
        {
            transform.position = monster.transform.position + offset;
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
        }
    }

    public void UpdateHP(int current, int max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = (float)current / max;
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
        if (fillImage == null) yield break;
        Color originalColor = fillImage.color;
        fillImage.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        fillImage.color = originalColor;
    }

    private void OnDestroy()
    {
        // Cleanup if needed
    }
}