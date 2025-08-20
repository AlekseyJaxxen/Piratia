using Mirror;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [Header("Animation Clips")]
    public AnimationClip idleAnimation;
    public AnimationClip runAnimation;
    public AnimationClip attackAnimation;
    public AnimationClip skillCastAnimation;
    public AnimationClip deathAnimation;
    public float transitionSpeed = 0.15f;

    [SyncVar(hook = nameof(OnAnimationStateChanged))]
    private string _currentAnimation = "Idle";

    private Animation _animation;
    private PlayerCore _core;
    private bool _deathAnimationPlayed;

    public void Init(PlayerCore core)
    {
        _core = core;
        _animation = GetComponent<Animation>();

        if (idleAnimation != null) _animation.AddClip(idleAnimation, "Idle");
        if (runAnimation != null) _animation.AddClip(runAnimation, "Run");
        if (attackAnimation != null) _animation.AddClip(attackAnimation, "Attack");
        if (skillCastAnimation != null) _animation.AddClip(skillCastAnimation, "SkillCast");
        if (deathAnimation != null) _animation.AddClip(deathAnimation, "Death");

        _animation.Play("Idle");
    }

    private void OnAnimationStateChanged(string oldState, string newState)
    {
        if (_animation != null && !string.IsNullOrEmpty(newState))
        {
            _animation.CrossFade(newState, transitionSpeed);
        }
    }

    public void UpdateAnimations()
    {
        if (_core.isDead) return;

        if (_core.Movement.IsMoving)
        {
            if (isLocal)
                _currentAnimation = "Run";
            
        }
        else
        {
            if (isLocal)
                _currentAnimation = "Idle";
            
        }
    }

    public void PlayAttackAnimation()
    {
        if (!isLocal) return;
        _currentAnimation = "Attack";
        _animation.Stop();
        _animation.Play("Attack");
    }

    public void PlaySkillCastAnimation()
    {
        if (!isLocal) return;
        _currentAnimation = "SkillCast";
        _animation.Stop();
        _animation.Play("SkillCast");
    }

    [Client]
    public void PlayDeathAnimation()
    {
        if (!_deathAnimationPlayed && _animation != null && deathAnimation != null)
        {
            if (isLocal)
                _currentAnimation = "Death";
            _animation.Stop();
            _animation.Play("Death");
            _deathAnimationPlayed = true;
        }

    }

    public void ResetDeathAnimation()
    {
        _deathAnimationPlayed = false;
    }
}