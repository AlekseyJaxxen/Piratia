using UnityEngine;
using DG.Tweening;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    private PlayerCore _core;
    private Sequence walkSequence;
    private Tween idleTween;
    private Tween stunTween;
    private Sequence deathSequence;
    private Sequence damageFlashSequence;
    private Sequence attackSequence;
    private Vector3 originalLocalPos;
    private Vector3 previousPosition;
    private float velocityMagnitude;
    [SerializeField] private Transform modelTransform;
    private Renderer modelRenderer;
    private Color originalColor;

    private void Awake()
    {
        _core = GetComponent<PlayerCore>();
        if (modelTransform == null) Debug.LogError("[PlayerAnimation] modelTransform not assigned!");
        originalLocalPos = modelTransform.localPosition;
        modelRenderer = modelTransform.GetComponent<Renderer>();
        if (modelRenderer != null)
        {
            originalColor = modelRenderer.material.color;
        }
        else
        {
            Debug.LogError("[PlayerAnimation] No Renderer found on modelTransform!");
        }
        // Pre-create walk sequence
        walkSequence = DOTween.Sequence();
        walkSequence.Append(modelTransform.DOLocalMoveY(originalLocalPos.y + 0.1f, 0.5f).SetLoops(-1, LoopType.Yoyo));
        walkSequence.Join(modelTransform.DOLocalRotate(new Vector3(0, 0, 5), 0.25f).SetLoops(-1, LoopType.Yoyo).From(new Vector3(0, 0, -5)));
        walkSequence.SetAutoKill(false);
        walkSequence.Pause();
        // Pre-create idle tween
        idleTween = modelTransform.DOLocalMoveY(originalLocalPos.y + 0.05f, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        idleTween.SetAutoKill(false);
        idleTween.Pause();
        // Pre-create stun tween
        stunTween = modelTransform.DOLocalRotate(new Vector3(0, 10, 0), 0.5f).SetLoops(-1, LoopType.Yoyo).From(new Vector3(0, -10, 0));
        stunTween.SetAutoKill(false);
        stunTween.Pause();
        // Pre-create death sequence (plays once)
        deathSequence = DOTween.Sequence();
        deathSequence.Append(modelTransform.DOLocalMoveY(originalLocalPos.y - 1f, 1f).SetEase(Ease.InOutSine));
        deathSequence.Join(modelTransform.DOLocalRotate(new Vector3(90, 0, 0), 1f).SetEase(Ease.InOutSine));
        deathSequence.SetAutoKill(false);
        deathSequence.SetLoops(1);
        deathSequence.Pause();
        // Pre-create damage flash sequence
        damageFlashSequence = DOTween.Sequence();
        damageFlashSequence.Append(modelRenderer.material.DOColor(Color.red, 0.1f));
        damageFlashSequence.Append(modelRenderer.material.DOColor(originalColor, 0.1f));
        damageFlashSequence.SetAutoKill(false);
        damageFlashSequence.Pause();
        // Pre-create attack sequence
        attackSequence = DOTween.Sequence();
        attackSequence.Append(modelTransform.DOLocalMove(new Vector3(originalLocalPos.x - 0.1f, originalLocalPos.y + 0.2f, originalLocalPos.z), 0.05f).SetEase(Ease.InOutFlash));
        attackSequence.Append(modelTransform.DOLocalMove(originalLocalPos, 0.1f).SetEase(Ease.InOutFlash));
        attackSequence.SetAutoKill(false);
        attackSequence.Pause();
    }

    private void Update()
    {
        if (_core == null) return;
        Vector3 currentPosition = transform.position;
        Vector3 velocity = (currentPosition - previousPosition) / Time.deltaTime;
        velocityMagnitude = velocity.magnitude;
        previousPosition = currentPosition;
        if (_core.isDead)
        {
            walkSequence.Pause();
            walkSequence.Rewind();
            idleTween.Pause();
            idleTween.Rewind();
            stunTween.Pause();
            stunTween.Rewind();
            damageFlashSequence.Pause();
            damageFlashSequence.Rewind();
            attackSequence.Pause();
            attackSequence.Rewind();
            deathSequence.Play();
        }
        else if (_core.isStunned)
        {
            walkSequence.Pause();
            walkSequence.Rewind();
            idleTween.Pause();
            idleTween.Rewind();
            deathSequence.Pause();
            deathSequence.Rewind();
            damageFlashSequence.Pause();
            damageFlashSequence.Rewind();
            attackSequence.Pause();
            attackSequence.Rewind();
            stunTween.Play();
        }
        else
        {
            stunTween.Pause();
            stunTween.Rewind();
            deathSequence.Pause();
            deathSequence.Rewind();
            if (velocityMagnitude > 0.1f)
            {
                idleTween.Pause();
                idleTween.Rewind();
                walkSequence.Play();
            }
            else
            {
                walkSequence.Pause();
                walkSequence.Rewind();
                idleTween.Play();
            }
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

    public void PlayAttackAnimation()
    {
        if (attackSequence != null)
        {
            attackSequence.Rewind();
            attackSequence.Play();
        }
    }

    private void OnDisable()
    {
        walkSequence.Kill();
        idleTween.Kill();
        stunTween.Kill();
        deathSequence.Kill();
        damageFlashSequence.Kill();
        attackSequence.Kill();
    }
}