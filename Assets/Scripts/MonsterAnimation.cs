using UnityEngine;
using Mirror;
using System.Collections;

public class MonsterAnimation : NetworkBehaviour
{
    private Monster _monster;
    private Renderer modelRenderer;
    private Color originalColor;
    [SerializeField] private Transform modelTransform;
    private Coroutine damageFlashCoroutine;

    private void Awake()
    {
        _monster = GetComponent<Monster>();
        if (modelTransform == null)
        {
            Debug.LogError("[MonsterAnimation] modelTransform not assigned!");
            return;
        }

        modelRenderer = modelTransform.GetComponent<Renderer>();
        if (modelRenderer != null)
        {
            originalColor = modelRenderer.material.color;
        }
        else
        {
            Debug.LogError("[MonsterAnimation] No Renderer found on modelTransform!");
        }
    }

    public void PlayDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
        Debug.Log($"[MonsterAnimation] Playing damage flash for {gameObject.name}");
    }

    private IEnumerator DamageFlashCoroutine()
    {
        if (modelRenderer != null)
        {
            // Flash to red
            modelRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            
            // Return to original color
            modelRenderer.material.color = originalColor;
        }
    }

    public void PlayShake(float duration = 0.5f, float strength = 0.5f)
    {
        // DoTween shake removed - no more jerky movement
        Debug.Log($"[MonsterAnimation] Shake disabled for {gameObject.name}");
    }

    private void OnDisable()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
    }
}