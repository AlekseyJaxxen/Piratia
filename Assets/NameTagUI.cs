using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI teamText;
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2.5f, 0); // Высота над головой

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            Vector3 worldPos = target.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0)
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

    public void UpdateNameAndTeam(string playerName, PlayerTeam playerTeam, PlayerTeam localTeam)
    {
        if (nameText != null) nameText.text = playerName;
        if (teamText != null) teamText.text = playerTeam.ToString();

        Color color = (playerTeam == localTeam) ? Color.green : Color.red;
        if (nameText != null) nameText.color = color; // Или teamText.color
        if (nameText != null) teamText.color = color; // Или teamText.color
    }

    void OnDestroy()
    {
        // Отписка, если нужно
    }
}