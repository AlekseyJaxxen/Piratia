using UnityEngine;
using TMPro;

public class NameTagUI : MonoBehaviour
{
    // These fields are public so they can be assigned in the Unity Editor.
    public TextMeshProUGUI nameText;
    // We can remove teamText since the name tag's color will indicate the team.
    // public TextMeshProUGUI teamText; 
    public Transform target;
    private Camera mainCamera;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    /// <summary>
    /// Initializes the main camera reference.
    /// </summary>
    void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Updates the position and rotation of the name tag to face the camera.
    /// Called after all other Update functions have been called.
    /// </summary>
    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            // Position the name tag above the target object.
            transform.position = target.position + offset;

            // Make the name tag face the camera.
            transform.LookAt(mainCamera.transform);

            // To prevent the name tag from tilting, zero out the X and Z rotation.
            // This keeps it upright on the screen regardless of the camera's angle.
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        }
    }

    /// <summary>
    /// Updates the name and color of the name tag based on the player's team.
    /// </summary>
    /// <param name="entityName">The name to display on the tag.</param>
    /// <param name="targetTeam">The team of the entity this tag belongs to.</param>
    /// <param name="localPlayerTeam">The team of the local player.</param>
    public void UpdateNameAndTeam(string entityName, PlayerTeam targetTeam, PlayerTeam localPlayerTeam)
    {
        if (nameText == null)
        {
            Debug.LogError("nameText is not assigned in the NameTagUI script.");
            return;
        }

        // Set the name text.
        nameText.text = entityName;

        // Determine the color based on team affiliation.
        if (targetTeam == PlayerTeam.None)
        {
            // If the team is "None", assume it's a neutral entity like a monster.
            nameText.color = Color.red;
        }
        else if (targetTeam == localPlayerTeam)
        {
            // It's an ally, set the color to blue.
            nameText.color = Color.blue;
        }
        else // targetTeam != localPlayerTeam
        {
            // It's an enemy, set the color to red.
            nameText.color = Color.red;
        }
    }
}