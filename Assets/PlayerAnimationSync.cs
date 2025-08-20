using Mirror;
using UnityEngine;

[RequireComponent(typeof(Animation))]
public class PlayerAnimationSync : NetworkBehaviour
{
    private Animation animationComponent;

    [SyncVar(hook = nameof(OnAnimationChanged))]
    private string currentAnimation;

    [SyncVar(hook = nameof(OnAnimationStateChanged))]
    private bool isPlaying;

    void Awake()
    {
        animationComponent = GetComponent<Animation>();
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            // Синхронизация текущей анимации
            foreach (AnimationState state in animationComponent)
            {
                if (animationComponent.IsPlaying(state.name))
                {
                    if (currentAnimation != state.name)
                    {
                        CmdSetCurrentAnimation(state.name);
                    }
                    if (!isPlaying)
                    {
                        CmdSetAnimationState(true);
                    }
                    return;
                }
            }

            if (isPlaying)
            {
                CmdSetAnimationState(false);
            }
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (isLocalPlayer)
        {
            CmdPlayAnimation(animationName);
        }
    }

    public void StopAnimation(string animationName)
    {
        if (isLocalPlayer)
        {
            CmdStopAnimation(animationName);
        }
    }

    [Command]
    private void CmdSetCurrentAnimation(string animationName)
    {
        currentAnimation = animationName;
    }

    [Command]
    private void CmdSetAnimationState(bool playing)
    {
        isPlaying = playing;
    }

    [Command]
    private void CmdPlayAnimation(string animationName)
    {
        RpcPlayAnimation(animationName);
    }

    [Command]
    private void CmdStopAnimation(string animationName)
    {
        RpcStopAnimation(animationName);
    }

    [ClientRpc]
    private void RpcPlayAnimation(string animationName)
    {
        if (!isLocalPlayer)
        {
            animationComponent.Play(animationName);
        }
    }

    [ClientRpc]
    private void RpcStopAnimation(string animationName)
    {
        if (!isLocalPlayer)
        {
            animationComponent.Stop(animationName);
        }
    }

    private void OnAnimationChanged(string oldAnimation, string newAnimation)
    {
        if (!isLocalPlayer && isPlaying)
        {
            animationComponent.Play(newAnimation);
        }
    }

    private void OnAnimationStateChanged(bool oldState, bool newState)
    {
        if (!isLocalPlayer)
        {
            if (newState && !string.IsNullOrEmpty(currentAnimation))
            {
                animationComponent.Play(currentAnimation);
            }
            else
            {
                animationComponent.Stop();
            }
        }
    }
}