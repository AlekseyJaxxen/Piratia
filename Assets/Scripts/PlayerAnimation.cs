using UnityEngine;
using DG.Tweening;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    private PlayerAnimationSystem _animationSystem;
    private Sequence damageFlashSequence;
    private Renderer modelRenderer;
    private Color originalColor;

    private void Awake()
    {
        _animationSystem = GetComponent<PlayerAnimationSystem>();
        if (_animationSystem == null)
        {
            Debug.LogError("[PlayerAnimation] PlayerAnimationSystem is null!");
        }
    }

    public void SetupRenderer(GameObject activeModel)
    {
        if (activeModel == null)
        {
            Debug.LogError("[PlayerAnimation] Active model is null!");
            return;
        }

        modelRenderer = activeModel.GetComponent<Renderer>();
        if (modelRenderer != null)
        {
            originalColor = modelRenderer.material.color;
            // Создаём последовательность для эффекта вспышки
            damageFlashSequence = DOTween.Sequence();
            damageFlashSequence.Append(modelRenderer.material.DOColor(Color.red, 0.1f));
            damageFlashSequence.Append(modelRenderer.material.DOColor(originalColor, 0.1f));
            damageFlashSequence.SetAutoKill(false);
            damageFlashSequence.Pause();
        }
        else
        {
            Debug.LogError("[PlayerAnimation] No Renderer found on active model!");
        }
    }

    public void PlayDamageFlash()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Rewind();
            damageFlashSequence.Play();
        }
    }

    private void OnDisable()
    {
        if (damageFlashSequence != null)
        {
            damageFlashSequence.Kill();
        }
    }
}