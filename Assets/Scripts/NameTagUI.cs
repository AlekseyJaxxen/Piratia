using UnityEngine;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI teamText;
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    void Start()
    {
        mainCamera = Camera.main;
       // UpdateNameAndTeam("", PlayerTeam.None, PlayerTeam.None); // Инициализация при старте
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(mainCamera.transform);
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f); // Billboard
        }
    }

    public void UpdateNameAndTeam(string playerName, PlayerTeam playerTeam, PlayerTeam localTeam)
    {
        if (nameText != null) nameText.text = playerName;
        if (teamText != null) teamText.text = playerTeam.ToString();
        Color color = (localTeam != PlayerTeam.None && playerTeam == localTeam) ? Color.green : Color.red;
        if (nameText != null) nameText.color = color;
        if (teamText != null) teamText.color = color;
    }
}