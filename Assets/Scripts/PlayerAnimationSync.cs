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

    // Оптимизация: кэширование для предотвращения избыточных сетевых вызовов
    private string _lastSentAnimation = "";
    private bool _lastSentIsPlaying = false;
    private float _lastAnimationCheck = 0f;
    private const float ANIMATION_CHECK_INTERVAL = 0.1f; // Проверяем анимации каждые 100мс

    void Awake()
    {
        animationComponent = GetComponent<Animation>();
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            // Оптимизация: проверяем анимации не каждый кадр, а с интервалом
            if (Time.time - _lastAnimationCheck < ANIMATION_CHECK_INTERVAL)
                return;
            _lastAnimationCheck = Time.time;
            
            // Отслеживаем текущую анимацию
            string currentPlayingAnimation = "";
            bool currentlyPlaying = false;
            
            foreach (AnimationState state in animationComponent)
            {
                if (animationComponent.IsPlaying(state.name))
                {
                    currentPlayingAnimation = state.name;
                    currentlyPlaying = true;
                    break;
                }
            }
            
            // Оптимизация: отправляем сетевые вызовы только при реальных изменениях
            if (currentPlayingAnimation != _lastSentAnimation)
            {
                CmdSetCurrentAnimation(currentPlayingAnimation);
                _lastSentAnimation = currentPlayingAnimation;
            }
            
            if (currentlyPlaying != _lastSentIsPlaying)
            {
                CmdSetAnimationState(currentlyPlaying);
                _lastSentIsPlaying = currentlyPlaying;
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