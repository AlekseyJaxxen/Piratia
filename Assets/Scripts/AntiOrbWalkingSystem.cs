using UnityEngine;
using Mirror;

/// <summary>
/// Система защиты от злоупотребления orb walking (быстрыми первыми атаками)
/// </summary>
public class AntiOrbWalkingSystem : NetworkBehaviour
{
    [Header("Anti Orb Walking")]
    [SyncVar] private float lastBasicAttackTime = 0f;
    [SyncVar] private bool isInAttackCycle = false;
    [SyncVar] private float attackCycleStartTime = 0f;
    
    private PlayerCore _core;
    private PlayerSkills _skills;
    
    private void Start()
    {
        _core = GetComponent<PlayerCore>();
        _skills = GetComponent<PlayerSkills>();
    }
    
    /// <summary>
    /// Проверить, можно ли начать быструю первую атаку
    /// </summary>
    public bool CanStartFastFirstAttack()
    {
        // Если мы в цикле атаки - нельзя
        if (isInAttackCycle) return false;
        
        // Проверяем через BasicAttackSkill, является ли это быстрой атакой
        if (_skills?.skills != null && _skills.skills.Count > 0 && _core != null)
        {
            var basicAttack = _skills.skills.Find(s => s is BasicAttackSkill) as BasicAttackSkill;
            if (basicAttack != null)
            {
                return basicAttack.IsFastAttack(_core);
            }
        }
        
        // Fallback: если прошло достаточно времени с последней атаки
        float timeSinceLastAttack = Time.time - lastBasicAttackTime;
        return timeSinceLastAttack >= GetAntiOrbWalkingCooldown();
    }
    
    /// <summary>
    /// Начать цикл атаки (блокирует быстрые атаки)
    /// </summary>
    [Server]
    public void StartAttackCycle()
    {
        isInAttackCycle = true;
        attackCycleStartTime = Time.time;
        lastBasicAttackTime = Time.time;
        
        Debug.Log($"[AntiOrbWalking] Attack cycle started on {gameObject.name}");
    }
    
    /// <summary>
    /// Завершить цикл атаки
    /// </summary>
    [Server]
    public void EndAttackCycle()
    {
        isInAttackCycle = false;
        Debug.Log($"[AntiOrbWalking] Attack cycle ended on {gameObject.name}");
    }
    
    /// <summary>
    /// Сбросить систему после каста скилла
    /// </summary>
    [Server]
    public void ResetAfterSkillCast()
    {
        isInAttackCycle = false;
        Debug.Log($"[AntiOrbWalking] System reset after skill cast on {gameObject.name}");
    }
    
    /// <summary>
    /// Получить время кулдауна anti-orb walking (равно реальному времени атаки)
    /// </summary>
    private float GetAntiOrbWalkingCooldown()
    {
        if (_skills?.skills != null && _skills.skills.Count > 0)
        {
            // Берем параметры из первого скилла (BasicAttack)
            var basicAttack = _skills.skills.Find(s => s is BasicAttackSkill) as BasicAttackSkill;
            if (basicAttack != null && _core != null)
            {
                var attackParams = basicAttack.GetAttackParams(_core);
                Debug.Log($"[AntiOrbWalkingSystem] Orb walking cooldown: {attackParams.antiOrbWalkingCooldown:F2}s (real attack time * {basicAttack.antiOrbWalkingCooldownMultiplier:F2})");
                return attackParams.antiOrbWalkingCooldown;
            }
        }
        
        // Fallback: используем реальное время атаки
        float realAttackInterval = GetAttackInterval();
        Debug.Log($"[AntiOrbWalkingSystem] Orb walking cooldown (fallback): {realAttackInterval:F2}s");
        return realAttackInterval;
    }
    
    /// <summary>
    /// Получить время первой атаки
    /// </summary>
    public float GetFirstAttackTime()
    {
        if (_skills?.skills == null || _skills.skills.Count == 0 || _core == null) 
            return 0.1f; // Дефолтное значение
        
        var basicAttack = _skills.skills.Find(s => s is BasicAttackSkill) as BasicAttackSkill;
        if (basicAttack != null)
        {
            return basicAttack.GetFirstAttackTime(_core);
        }
        
        return 0.1f; // Дефолтное значение
    }
    
    /// <summary>
    /// Получить базовое время атаки
    /// </summary>
    public float GetBaseAttackTime()
    {
        if (_skills?.skills == null || _skills.skills.Count == 0 || _core == null) 
            return 1.0f; // Дефолтное значение
        
        var basicAttack = _skills.skills.Find(s => s is BasicAttackSkill) as BasicAttackSkill;
        if (basicAttack != null)
        {
            return basicAttack.GetBaseAttackTime(_core);
        }
        
        return 1.0f; // Дефолтное значение
    }
    
    /// <summary>
    /// Получить время атаки с учетом скорости атаки персонажа
    /// </summary>
    public float GetAttackInterval()
    {
        float baseTime = GetBaseAttackTime();
        float attackSpeed = GetPlayerAttackSpeed();
        
        // Время между атаками = Базовое время / Скорость атаки
        return baseTime / attackSpeed;
    }
    
    /// <summary>
    /// Получить скорость атаки персонажа
    /// </summary>
    private float GetPlayerAttackSpeed()
    {
        if (_core?.Stats == null) return 1.0f;
        
        // Получаем скорость атаки из CharacterStats
        return _core.Stats.attackSpeed;
    }
    
    /// <summary>
    /// Проверить, находимся ли мы в цикле атаки
    /// </summary>
    public bool IsInAttackCycle()
    {
        return isInAttackCycle;
    }
    
    /// <summary>
    /// Получить оставшееся время до следующей возможной быстрой атаки
    /// </summary>
    public float GetTimeUntilNextFastAttack()
    {
        if (!isInAttackCycle) return 0f;
        
        float timeSinceCycleStart = Time.time - attackCycleStartTime;
        float attackInterval = GetAttackInterval(); // Используем время с учетом скорости атаки
        
        return Mathf.Max(0f, attackInterval - timeSinceCycleStart);
    }
    
    /// <summary>
    /// Получить оставшееся время до сброса anti-orb walking
    /// </summary>
    public float GetTimeUntilAntiOrbReset()
    {
        float timeSinceLastAttack = Time.time - lastBasicAttackTime;
        float cooldown = GetAntiOrbWalkingCooldown();
        
        return Mathf.Max(0f, cooldown - timeSinceLastAttack);
    }
    
    /// <summary>
    /// Получить информацию о состоянии системы (для отладки)
    /// </summary>
    public string GetSystemStatus()
    {
        return $"InCycle: {isInAttackCycle}, " +
               $"TimeUntilFast: {GetTimeUntilNextFastAttack():F2}s, " +
               $"TimeUntilReset: {GetTimeUntilAntiOrbReset():F2}s, " +
               $"LastAttack: {Time.time - lastBasicAttackTime:F2}s ago";
    }
}
