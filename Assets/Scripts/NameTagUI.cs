using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class NameTagUI : MonoBehaviour
{
    public TextMeshProUGUI nameText; public TextMeshProUGUI teamText; public Transform target; private Camera mainCamera; public Vector3 offset = new Vector3(0, 2.5f, 0);
    void Start()
    {
        mainCamera = Camera.main;
        Debug.Log($"[NameTagUI] Initialized for {gameObject.name}, nameText: {(nameText != null)}, teamText: {(teamText != null)}");
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
            Debug.Log($"[NameTagUI] Updating for {nameText?.text}, active: {gameObject.activeSelf}");
        }
    }

    public void UpdateNameAndTeam(string playerName, PlayerTeam playerTeam, PlayerTeam localTeam)
    {
        if (nameText != null) nameText.text = playerName;
        if (teamText != null) teamText.text = playerTeam.ToString();

        Color color = (localTeam != PlayerTeam.None && playerTeam == localTeam) ? Color.green : Color.red;
        if (nameText != null) nameText.color = color;
        if (teamText != null) teamText.color = color;

        Debug.Log($"[NameTagUI] Updated: Name={playerName}, Team={playerTeam}, LocalTeam={localTeam}, Color={(nameText != null ? nameText.color : Color.black)}");
    }

    void OnDestroy()
    {
        Debug.Log($"[NameTagUI] Destroyed for {nameText?.text}");
    }

}