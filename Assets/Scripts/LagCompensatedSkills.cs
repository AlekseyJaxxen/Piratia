using UnityEngine;
using Mirror;

/// <summary>
/// Расширение PlayerSkills с поддержкой lag compensation и prediction
/// Минимальная и безопасная реализация
/// </summary>
public partial class PlayerSkills : NetworkBehaviour
{
    private SkillPredictionManager _predictionManager;
    
    /// <summary>
    /// Инициализация системы предсказания
    /// </summary>
    private void InitPredictionSystem()
    {
        _predictionManager = GetComponent<SkillPredictionManager>();
        if (_predictionManager == null)
        {
            _predictionManager = gameObject.AddComponent<SkillPredictionManager>();
        }
    }
    
    /// <summary>
    /// Lag-compensated команда выполнения скилла
    /// </summary>
    [Command]
    public void CmdExecuteSkillWithCompensation(PlayerCore caster, Vector3? targetPosition, uint targetNetId, 
                                               string skillName, int weight, double clientTimestamp)
    {
        // ВАЛИДАЦИЯ: Проверяем временную метку
        if (!NetworkPrediction.ValidateTimestamp(clientTimestamp))
        {
            Debug.LogWarning($"[PlayerSkills] Invalid timestamp from {caster.name}: {clientTimestamp}");
            return;
        }
        
        // ОСНОВНЫЕ ПРОВЕРКИ:
        if (!caster.CanCastSkill(caster.Skills.skills.Find(s => s.SkillName == skillName)))
        {
            RejectCommand(targetNetId, skillName, clientTimestamp, "Cannot cast skill");
            return;
        }
        
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null)
        {
            Debug.LogWarning($"[PlayerSkills] Skill {skillName} not found on {gameObject.name}");
            RejectCommand(targetNetId, skillName, clientTimestamp, "Skill not found");
            return;
        }
        
        if (GetRemainingCooldown(skillName) > 0)
        {
            RejectCommand(targetNetId, skillName, clientTimestamp, "Skill on cooldown");
            return;
        }
        
        // Global cooldown check removed - players can cast spells quickly
        
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(skill.ManaCost))
        {
            RejectCommand(targetNetId, skillName, clientTimestamp, "Not enough mana");
            return;
        }
        
        // LAG COMPENSATION: Корректируем проверки на момент клика
        GameObject targetObject = null;
        if (targetNetId != 0 && NetworkServer.spawned.ContainsKey(targetNetId))
        {
            targetObject = NetworkServer.spawned[targetNetId].gameObject;
            
            // КОМПЕНСАЦИЯ ЛАГА: Проверяем дистанцию на момент клика
            if (!ValidateTargetDistanceAtTime(caster, targetObject, skillName, clientTimestamp))
            {
                RejectCommand(targetNetId, skillName, clientTimestamp, "Target out of skill range (lag compensation)");
                return;
            }
        }
        
        // ИНВИЗИБИЛИТИ: Прерываем невидимость при касте
        if (toggleBuffStates.ContainsKey("Invisibility") && toggleBuffStates["Invisibility"])
        {
            Debug.Log($"[PlayerSkills] Interrupting invisibility due to skill cast: {skillName} on {gameObject.name}");
            SetToggleBuff("Invisibility", false);
        }
        
        // ВЫПОЛНЯЕМ СКИЛЛ:
        if (stats != null)
        {
            stats.SpendMana(skill.ManaCost);
        }
        
        skill.ExecuteOnServer(caster, targetPosition, targetObject, weight);
        
        StartSkillCooldown(skillName);
        // Global cooldown removed
        
        // Сбрасываем систему anti-orb walking после выполнения скилла
        AntiOrbWalkingSystem antiOrbSystem = caster.GetComponent<AntiOrbWalkingSystem>();
        if (antiOrbSystem != null)
        {
            antiOrbSystem.ResetAfterSkillCast();
        }
        
        // ПОДТВЕРЖДАЕМ КЛИЕНТАМ:
        ConfirmCommand(targetNetId, skillName, clientTimestamp);
        
        RpcCancelSkillSelection();
        RpcConsumeItemFromSkill(skillName);
    }
    
    /// <summary>
    /// Validate target distance at the time when clicked
    /// </summary>
    private bool ValidateTargetDistanceAtTime(PlayerCore caster, GameObject target, string skillName, double clientTimestamp)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null) return false;
        
        // Простая компенсация: учитываем движение за время лага
        float compensationDelay = NetworkPrediction.GetCompensationDelay();
        GameObject casterObj = caster.gameObject;
        GameObject targetObj = target;
        
        Vector3 casterPos = casterObj.transform.position;
        Vector3 targetPos = targetObj.transform.position;
        
        // Простая оценка движения за время лага
        float distanceNow = Vector3.Distance(casterPos, targetPos);
        float skillRange = skill.SkillRangeOrDefault();
        
        // Если сейчас цели нет в радиусе, возможно она была в прошлом
        if (distanceNow > skillRange)
        {
            // Проверяем приближение за время компенсации
            float maxMovement = compensationDelay * 0.01f * skillRange; // Предполагаем ограниченное движение
            return distanceNow <= skillRange + maxMovement;
        }
        
        return true; // Цель в радиусе
    }
    
    private void RejectCommand(uint targetNetId, string skillName, double clientTimestamp, string reason)
    {
        if (_predictionManager != null)
        {
            _predictionManager.RpcRejectPrediction(skillName, targetNetId, clientTimestamp, reason);
        }
    }
    
    private void ConfirmCommand(uint targetNetId, string skillName, double clientTimestamp)
    {
        if (_predictionManager != null)
        {
            _predictionManager.RpcConfirmPrediction(skillName, targetNetId, clientTimestamp);
        }
    }
}

/// <summary>
/// Extension methods for Skills
/// </summary>
public static class SkillExtensions
{
    public static float SkillRangeOrDefault(this SkillBase skill)
    {
        // Безопасное получение радиуса скилла
        var targetedStunSkill = skill as TargetedStunSkill;
        if (targetedStunSkill != null)
        {
            return targetedStunSkill.GetSkillRangeOrDefault();
        }
        
        // Дефолтный радиус
        return 20f;
    }
}

