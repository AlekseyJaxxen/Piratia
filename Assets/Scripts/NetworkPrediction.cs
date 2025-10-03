using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Минимальная система предсказания и компенсации лага для Mirror.
/// Безопасная реализация с возможностью отката.
/// </summary>
public static class NetworkPrediction
{
    // Максимальная задержка для компенсации (мс)
    private const float MAX_COMPENSATION_MS = 200f;
    
    /// <summary>
    /// Получить задержку для компенсации между клиентом и сервером
    /// </summary>
    public static float GetCompensationDelay()
    {
        if (!NetworkClient.isConnected) return 0f;
        
        // Простая оценка RTT через NetworkTime
        float rtt = (float)NetworkTime.rtt * 1000f; // Convert to ms
        return Mathf.Min(rtt * 0.5f, MAX_COMPENSATION_MS); // Используем половину RTT
    }
    
    /// <summary>
    /// Проверить корректность временной метки от клиента
    /// </summary>
    public static bool ValidateTimestamp(double clientTimestamp)
    {
        double serverTime = NetworkTime.time;
        double deltaSeconds = serverTime - clientTimestamp;
        
        // Только если команда не старше 500ms и не из будущего
        return deltaSeconds >= -0.1f && deltaSeconds <= 0.5f;
    }
    
    /// <summary>
    /// Вычислить время для rollback'а
    /// </summary>
    public static double GetRollbackTime(double clientTimestamp, uint targetNetId)
    {
        // Базовая компенсация лага
        float compensationDelay = GetCompensationDelay();
        return clientTimestamp - (compensationDelay / 1000f);
    }
}

/// <summary>
/// Структура для хранения предсказанного состояния скилла
/// </summary>
[System.Serializable]
public struct PredictedSkillState
{
    public string skillName;
    public double timestamp;
    public uint targetNetId;
    public int skillWeight;
    public bool wasExecuted;
}

/// <summary>
/// Компонент для отслеживания предсказанных скиллов
/// </summary>
public class SkillPredictionManager : NetworkBehaviour
{
    private Queue<PredictedSkillState> _predictionQueue = new Queue<PredictedSkillState>();
    
    /// <summary>
    /// Предсказать выполнение скилла на клиенте
    /// </summary>
    public void PredictSkillExecution(string skillName, uint targetNetId, int weight)
    {
        var prediction = new PredictedSkillState
        {
            skillName = skillName,
            timestamp = NetworkTime.time,
            targetNetId = targetNetId,
            skillWeight = weight,
            wasExecuted = true
        };
        
        _predictionQueue.Enqueue(prediction);
        
        // Показываем моментальную обратную связь
        ShowPredictedEffect(skillName, targetNetId);
    }
    
    /// <summary>
    /// Подтвердить предсказание с сервера
    /// </summary>
    [ClientRpc]
    public void RpcConfirmPrediction(string skillName, uint targetNetId, double timestamp)
    {
        // Ищем и убираем подтвержденное предсказание
        var predictions = _predictionQueue.ToArray();
        _predictionQueue.Clear();
        
        foreach (var pred in predictions)
        {
            if (pred.skillName == skillName && pred.targetNetId == targetNetId && 
                Mathf.Abs((float)(pred.timestamp - timestamp)) < 0.1f)
            {
                // Предсказание подтверждено, удаляем
                continue;
            }
            
            _predictionQueue.Enqueue(pred);
        }
    }
    
    /// <summary>
    /// Отклонить предсказание и выполнить rollback
    /// </summary>
    [ClientRpc]
    public void RpcRejectPrediction(string skillName, uint targetNetId, double timestamp, string reason)
    {
        // Удаляем отклоненное предсказание
        var predictions = _predictionQueue.ToArray();
        _predictionQueue.Clear();
        
        foreach (var pred in predictions)
        {
            if (pred.skillName == skillName && pred.targetNetId == targetNetId && 
                Mathf.Abs((float)(pred.timestamp - timestamp)) < 0.1f)
            {
                // Откатываем предсказание
                RollbackPredictedEffect(skillName, targetNetId, reason);
                continue;
            }
            
            _predictionQueue.Enqueue(pred);
        }
    }
    
    private void ShowPredictedEffect(string skillName, uint targetNetId)
    {
        switch (skillName)
        {
            case "Stun":
                // Показываем предсказанную анимацию стана
                if (NetworkClient.spawned.ContainsKey(targetNetId))
                {
                    var target = NetworkClient.spawned[targetNetId].gameObject;
                    // ВОТ ТУТ МОЖНО ПОКАЗАТЬ ПРЕДСКАЗАННЫЙ VFX СРАЗУ
                    Debug.Log($"[SkillPredictionManager] Predicted {skillName} on {target.name}");
                }
                break;
        }
    }
    
    private void RollbackPredictedEffect(string skillName, uint targetNetId, string reason)
    {
        Debug.LogWarning($"[SkillPredictionManager] Prediction rejected: {skillName} -> {reason}");
        
        // ВОТ ТУТ МОЖНО УБРАТЬ ПРЕДСКАЗАННЫЙ VFX ИЛИ ОТМЕНИТЬ ЭФФЕКТ
        switch (skillName)
        {
            case "Stun":
                if (NetworkClient.spawned.ContainsKey(targetNetId))
                {
                    var target = NetworkClient.spawned[targetNetId].gameObject;
                    // Убираем предсказанный эффект стана
                    Debug.Log($"[SkillPredictionManager] Rolled back {skillName} prediction on {target.name}");
                }
                break;
        }
        
        // Восстанавливаем ресурсы
        if (isLocalPlayer)
        {
            var skills = GetComponent<PlayerSkills>();
            if (skills != null)
            {
                // Восстанавливаем ману и очищаем кулдаун
                var skill = skills.skills.Find(s => s.SkillName == skillName);
                if (skill != null)
                {
                    var stats = GetComponent<CharacterStats>();
                    if (stats != null)
                    {
                        stats.RestoreMana(skill.ManaCost);
                    }
                    skills.ClearSkillCooldown(skillName);
                }
            }
        }
    }
}
