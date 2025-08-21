using UnityEngine;
using Mirror;

public class PlayerColorManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(UpdateColor))]
    public Color playerColor = Color.white;

    private Renderer[] playerRenderers;

    void Start()
    {
        playerRenderers = GetComponentsInChildren<Renderer>(true);
        if (playerRenderers.Length == 0)
        {
            Debug.LogWarning($"[Client] No renderers found in children of {gameObject.name}");
        }

        if (isLocal)
        {
            playerColor = new Color(
                Random.Range(0.4f, 1f),
                Random.Range(0.4f, 1f),
                Random.Range(0.4f, 1f)
            );
            Debug.Log($"[Server] Set playerColor to {playerColor} for {gameObject.name}");
        }
    }

    void UpdateColor(Color oldColor, Color newColor)
    {
        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            Debug.LogWarning($"[Client] No renderers found for color update on {gameObject.name}");
            return;
        }

        foreach (Renderer rend in playerRenderers)
        {
            if (rend != null)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat != null && mat.HasProperty("_Color"))
                    {
                        mat.color = newColor;
                        Debug.Log($"[Client] Applied color {newColor} to {rend.name} on {gameObject.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Client] Material on {rend.name} has no _Color property");
                    }
                }
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateColor(playerColor, playerColor);
    }
}