using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ControlEffectManager : NetworkBehaviour
{
    [SerializeField] private PlayerCore playerCore;
    private readonly SyncList<ControlEffect> activeControlEffects = new SyncList<ControlEffect>();
    private float originalSpeed = 0f;

    [SyncVar(hook = nameof(OnStunStateChanged))]
    private bool isStunned = false;

    [SyncVar(hook = nameof(OnSilenceStateChanged))]
    private bool isSilenced = false;

    [SyncVar(hook = nameof(OnPoisonStateChanged))]
    private bool isPoisoned = false;

    private void Awake()
    {
        playerCore = GetComponent<PlayerCore>();
        if (playerCore.Movement != null)
        {
            originalSpeed = playerCore.Movement.GetOriginalSpeed();
        }
    }

    [ServerCallback]
    private void Update()
    {
        for (int i = activeControlEffects.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeControlEffects[i].endTime)
            {
                RemoveControlEffect(activeControlEffects[i].type);
            }
        }
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType newEffectType, float duration, float value = 0f)
    {
        var existingEffect = activeControlEffects.Find(e => e.type == newEffectType);
        if (existingEffect.type != ControlEffectType.None)
        {
            activeControlEffects.Remove(existingEffect);
            activeControlEffects.Add(new ControlEffect(newEffectType, Time.time + duration, value));
        }
        else
        {
            activeControlEffects.Add(new ControlEffect(newEffectType, Time.time + duration, value));
        }

        switch (newEffectType)
        {
            case ControlEffectType.Stun:
                SetStunState(true);
                break;
            case ControlEffectType.Slow:
                ApplySlow(value);
                break;
            case ControlEffectType.Silence:
                SetSilenceState(true);
                break;
            case ControlEffectType.Poison:
                SetPoisonState(true);
                break;
        }

        Debug.Log($"Применен эффект: {newEffectType} на {duration} секунд.");
    }

    [Server]
    private void RemoveControlEffect(ControlEffectType effectType)
    {
        var effect = activeControlEffects.Find(e => e.type == effectType);
        if (effect.type != ControlEffectType.None)
        {
            activeControlEffects.Remove(effect);

            switch (effectType)
            {
                case ControlEffectType.Stun:
                    if (!activeControlEffects.Any(e => e.type == ControlEffectType.Stun))
                    {
                        SetStunState(false);
                    }
                    break;
                case ControlEffectType.Slow:
                    var remainingSlow = activeControlEffects.Find(e => e.type == ControlEffectType.Slow);
                    if (remainingSlow.type != ControlEffectType.None)
                    {
                        ApplySlow(remainingSlow.slowPercentage);
                    }
                    else
                    {
                        playerCore.Movement.SetMovementSpeed(originalSpeed);
                        Debug.Log("Эффект замедления снят. Скорость восстановлена.");
                    }
                    break;
                case ControlEffectType.Silence:
                    if (!activeControlEffects.Any(e => e.type == ControlEffectType.Silence))
                    {
                        SetSilenceState(false);
                    }
                    break;
                case ControlEffectType.Poison:
                    if (!activeControlEffects.Any(e => e.type == ControlEffectType.Poison))
                    {
                        SetPoisonState(false);
                    }
                    break;
            }
        }
    }

    [Server]
    public void ClearControlEffect()
    {
        activeControlEffects.Clear();
        SetStunState(false);
        SetSilenceState(false);
        SetPoisonState(false);
        playerCore.Movement.SetMovementSpeed(originalSpeed);
        Debug.Log("Все эффекты сняты.");
    }

    [Server]
    private void SetStunState(bool state)
    {
        isStunned = state;
        if (state)
        {
            playerCore.Movement.StopMovement();
            playerCore.ActionSystem.CompleteAction();
        }
    }

    [Server]
    private void SetSilenceState(bool state)
    {
        isSilenced = state;
        RpcSetSilenceState(state);
    }

    [Server]
    private void SetPoisonState(bool state)
    {
        isPoisoned = state;
        if (state)
        {
            StartCoroutine(ApplyPoisonDamage());
        }
    }

    [Server]
    private void ApplySlow(float slowPercentage)
    {
        float maxSlow = activeControlEffects
            .Where(e => e.type == ControlEffectType.Slow)
            .Select(e => e.slowPercentage)
            .DefaultIfEmpty(0f)
            .Max();

        float newSpeed = originalSpeed * (1f - maxSlow);
        playerCore.Movement.SetMovementSpeed(newSpeed);
        Debug.Log($"Применено замедление: {maxSlow:P0}. Новая скорость: {newSpeed}");
    }

    [Server]
    private IEnumerator ApplyPoisonDamage()
    {
        float duration = activeControlEffects.Find(e => e.type == ControlEffectType.Poison).endTime - Time.time;
        while (isPoisoned && duration > 0)
        {
            playerCore.Health.TakeDamage(5); // 5 урона в секунду
            yield return new WaitForSeconds(1f);
            duration -= 1f;
        }
    }

    [ClientRpc]
    private void RpcSetSilenceState(bool state)
    {
        if (playerCore.Skills != null)
        {
            playerCore.Skills.HandleSilenceEffect(state);
        }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (playerCore.Movement != null)
        {
            playerCore.Movement.enabled = !newValue;
            if (newValue) playerCore.Movement.StopMovement();
        }
        if (playerCore.Combat != null) playerCore.Combat.enabled = !newValue;
        if (playerCore.Skills != null)
        {
            playerCore.Skills.enabled = !newValue;
            playerCore.Skills.HandleStunEffect(newValue);
        }
        if (playerCore.ActionSystem != null) playerCore.ActionSystem.enabled = !newValue;
    }

    private void OnSilenceStateChanged(bool oldValue, bool newValue)
    {
        if (playerCore.Skills != null)
        {
            playerCore.Skills.HandleSilenceEffect(newValue);
        }
    }

    private void OnPoisonStateChanged(bool oldValue, bool newValue)
    {
        // Визуальные эффекты на клиенте, если нужно
    }

    public bool IsStunned => isStunned;
    public bool IsSilenced => isSilenced;
    public bool IsPoisoned => isPoisoned;
}