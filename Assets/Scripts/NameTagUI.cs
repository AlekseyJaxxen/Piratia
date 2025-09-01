using UnityEngine;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI teamText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    public Transform target;
    private Camera mainCamera;

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

    public void UpdateNameAndTeam(string name, PlayerTeam team, PlayerTeam localTeam)
    {
        if (nameText != null) nameText.text = name;
        if (teamText != null) teamText.text = team.ToString();
        Color color = (localTeam != PlayerTeam.None && team == localTeam) ? Color.green : Color.red;
        if (nameText != null) nameText.color = color;
        if (teamText != null) teamText.color = color;
    }
}