using Mirror;
using UnityEngine;

public class DamageFlash : NetworkBehaviour
{
    [SerializeField] private float flashDuration = 0.3f;
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isFlashing = false;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    [ClientRpc]
    public void RpcFlashDamage()
    {
        if (isFlashing) return;

        StartCoroutine(FlashCoroutine());
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        isFlashing = true;

        // Красный цвет
        foreach (Renderer rend in renderers)
        {
            rend.material.color = Color.red;
        }

        yield return new WaitForSeconds(flashDuration);

        // Возврат исходного цвета
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }

        isFlashing = false;
    }
}