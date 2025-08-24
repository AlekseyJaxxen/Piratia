// PlayerStatusEffectManager.cs
using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PlayerStatusEffectManager : NetworkBehaviour
{
    // Структура для хранения информации об эффекте
    [System.Serializable]
    public struct ActiveEffect
    {
        public ControlEffectType Type;
        public float EndTime;
        public float Value;
    }

    public readonly SyncList<ActiveEffect> activeEffects = new SyncList<ActiveEffect>();

    private PlayerCore _playerCore;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _playerCore = GetComponent<PlayerCore>();
        if (_playerCore == null)
        {
            Debug.LogError("PlayerStatusEffectManager requires a PlayerCore component on the same GameObject.");
        }
    }

    [Server]
    private void Update()
    {
        // Проверяем и удаляем истекшие эффекты
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeEffects[i].EndTime)
            {
                RemoveControlEffect(activeEffects[i].Type);
            }
        }
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, float value = 0f)
    {
        int existingEffectIndex = -1;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].Type == effectType)
            {
                existingEffectIndex = i;
                break;
            }
        }

        float newEndTime = Time.time + duration;

        if (existingEffectIndex != -1)
        {
            ActiveEffect existingEffect = activeEffects[existingEffectIndex];
            if (newEndTime > existingEffect.EndTime)
            {
                existingEffect.EndTime = newEndTime;
                existingEffect.Value = value;
                activeEffects[existingEffectIndex] = existingEffect;
                Debug.Log($"Обновлена длительность эффекта: {effectType}.");
            }
        }
        else
        {
            activeEffects.Add(new ActiveEffect
            {
                Type = effectType,
                EndTime = newEndTime,
                Value = value
            });
            Debug.Log($"Применен новый эффект: {effectType} на {duration} секунд.");
        }

        // Отдельно обрабатываем логику при применении эффекта
        if (effectType == ControlEffectType.Stun)
        {
            _playerCore.Movement.StopMovement();
            _playerCore.Skills.HandleStunEffect(true);
        }
        if (effectType == ControlEffectType.Slow)
        {
            _playerCore.Movement.SetMovementSpeed(_playerCore.Movement.GetOriginalSpeed() * (1f - value));
        }
    }

    [Server]
    public void RemoveControlEffect(ControlEffectType effectType)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Type == effectType)
            {
                activeEffects.RemoveAt(i);
                Debug.Log($"Эффект {effectType} снят.");

                // Отдельно обрабатываем логику при снятии эффекта
                if (effectType == ControlEffectType.Stun)
                {
                    _playerCore.Skills.HandleStunEffect(HasControlEffect(ControlEffectType.Stun));
                }
                if (effectType == ControlEffectType.Slow)
                {
                    _playerCore.Movement.SetMovementSpeed(_playerCore.Movement.GetOriginalSpeed());
                }
                return;
            }
        }
    }

    public bool HasControlEffect(ControlEffectType effectType)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Type == effectType)
            {
                return true;
            }
        }
        return false;
    }
}