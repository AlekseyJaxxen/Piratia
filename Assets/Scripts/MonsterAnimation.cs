using UnityEngine;
using DG.Tweening;
using Mirror;

public class MonsterAnimation : NetworkBehaviour
{
    private Monster _monster;
    private Sequence damageFlashSequence;
    private Renderer modelRenderer;
    private Color originalColor;
    [SerializeField] private Transform modelTransform;

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

        // Pre-create damage flash sequence
        damageFlashSequence = DOTween.Sequence();
        damageFlashSequence.Append(modelRenderer.material.DOColor(Color.red, 0.1f));
        damageFlashSequence.Append(modelRenderer.material.DOColor(originalColor, 0.1f));
        damageFlashSequence.SetAutoKill(false);
        damageFlashSequence.Pause();
    }

    public void PlayDamageFlash()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Rewind();
            damageFlashSequence.Play();
            Debug.Log($"[MonsterAnimation] Playing damage flash for {gameObject.name}");
        }
    }

    public void PlayShake(float duration = 0.5f, float strength = 0.5f)
    {
        if (modelTransform != null)
        {
            modelTransform.DOShakePosition(duration, strength);
        }
    }

    private void OnDisable()
    {
        damageFlashSequence?.Kill();
    }
}