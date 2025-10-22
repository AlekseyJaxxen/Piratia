using UnityEngine;
using Mirror;
using System.Collections;
using System.Linq;

/// <summary>
/// Структура для хранения параметров атаки
/// </summary>
[System.Serializable]
public struct AttackParams
{
    public float firstAttackTime;
    public float baseAttackTime;
    public float antiOrbWalkingCooldown;
    public float fastAttackResetTime;
}

[CreateAssetMenu(fileName = "NewBasicAttackSkill", menuName = "Skills/BasicAttackSkill")]
public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public GameObject vfxPrefab;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    [Header("VFX Position Settings")]
    public Vector3 vfxStartOffset = new Vector3(0f, 1.5f, 0f); // �������� ��������� ������� ��� �������
    public Vector3 vfxTargetOffset = new Vector3(0f, 1.0f, 0f); // �������� ������� VFX �� ����
    [Header("Critical Hit Settings")]
    public GameObject criticalHitVfxPrefab;
    public GameObject impactEffectPrefab;
    public Color criticalHitColor = Color.yellow;
    
    [Header("First Attack Speed Settings")]
    [Tooltip("Множитель времени первой атаки (0.1 = очень быстро, 1.0 = как обычная атака)")]
    public float firstAttackTime = 0.1f; // Множитель времени первой атаки
    [Tooltip("Множитель базового времени атаки (1.0 = стандартное время, 0.5 = в 2 раза быстрее)")]
    public float baseAttackTime = 1.0f; // Множитель базового времени атаки
    [Tooltip("Множитель для anti-orb walking cooldown (1.0 = равен реальному времени атаки, 0.8 = на 20% меньше)")]
    public float antiOrbWalkingCooldownMultiplier = 1.0f; // Множитель для защиты от orb walking
    [Tooltip("Время бездействия для ресета быстрой атаки (секунды)")]
    public float fastAttackResetTime = 1.0f; // Время для ресета возможности быстрой атаки
    
    /// <summary>
    /// Структура для хранения параметров атаки (для совместимости с AntiOrbWalkingSystem)
    /// </summary>
    public AttackParams GetAttackParams(PlayerCore caster)
    {
        float realAttackTime = GetBaseAttackTime(caster);
        return new AttackParams
        {
            firstAttackTime = this.firstAttackTime,
            baseAttackTime = this.baseAttackTime,
            antiOrbWalkingCooldown = realAttackTime * this.antiOrbWalkingCooldownMultiplier,
            fastAttackResetTime = this.fastAttackResetTime
        };
    }
    
    /// <summary>
    /// Получить время первой атаки с учетом скорости атаки персонажа
    /// </summary>
    public float GetFirstAttackTime(PlayerCore caster)
    {
        if (caster?.Stats == null) return firstAttackTime;
        
        float attackSpeed = caster.Stats.attackSpeed;
        // Используем стандартную формулу: время между атаками = 1.0 / скорость атаки
        // firstAttackTime теперь служит как множитель для настройки баланса
        return (1.0f / attackSpeed) * firstAttackTime;
    }
    
    /// <summary>
    /// Получить базовое время атаки с учетом скорости атаки персонажа
    /// </summary>
    public float GetBaseAttackTime(PlayerCore caster)
    {
        if (caster?.Stats == null) return baseAttackTime;
        
        float attackSpeed = caster.Stats.attackSpeed;
        // Используем стандартную формулу: время между атаками = 1.0 / скорость атаки
        // baseAttackTime теперь служит как множитель для настройки баланса
        return (1.0f / attackSpeed) * baseAttackTime;
    }
    
    /// <summary>
    /// Получить время атаки с учетом того, является ли это первой атакой
    /// </summary>
    public float GetAttackTime(PlayerCore caster, bool isFirstAttack)
    {
        return isFirstAttack ? GetFirstAttackTime(caster) : GetBaseAttackTime(caster);
    }
    
    /// <summary>
    /// Проверить, является ли атака быстрой (первой после периода бездействия)
    /// </summary>
    public bool IsFastAttack(PlayerCore caster)
    {
        if (caster?.Combat == null) return false;
        
        float timeSinceLastAttack = Time.time - caster.Combat._lastAttackTime;
        return timeSinceLastAttack >= fastAttackResetTime;
    }

    /// <summary>
    /// Переопределяем Range чтобы использовать attackRange из CharacterStats
    /// </summary>
    public override float Range
    {
        get
        {
            if (_player != null)
            {
                CharacterStats stats = _player.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    return stats.attackRange;
                }
            }
            return base.Range; // Fallback на базовую дальность скилла
        }
    }


    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] Target object is null for skill {_skillName}");
            return;
        }
        
        // Проверяем AntiOrbWalkingSystem для защиты от abuse
        AntiOrbWalkingSystem antiOrbSystem = caster.GetComponent<AntiOrbWalkingSystem>();
        if (antiOrbSystem != null && !antiOrbSystem.CanStartFastFirstAttack())
        {
            Debug.LogWarning($"[BasicAttackSkill] Fast first attack blocked by AntiOrbWalkingSystem for {caster.name}");
            return;
        }
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if ((targetCore == null || targetCore.team == caster.team) && targetMonster == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] Invalid target for {_skillName}");
            return;
        }
        float distance = Vector3.Distance(caster.transform.position, targetObject.transform.position); // Full distance with Y
        if (distance > Range)
        {
            Debug.LogWarning($"[BasicAttackSkill] Target {targetObject.name} is out of range: {distance} > {Range}");
            return;
        }
        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] Target {targetObject.name} has no NetworkIdentity for skill {_skillName}");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[BasicAttackSkill] Not enough mana for skill {_skillName}: {stats.currentMana}/{ManaCost}");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning($"[BasicAttackSkill] PlayerSkills component missing on caster for skill {_skillName}");
            return;
        }
        // Client requesting attack
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, 0);
        
        // Логируем скорость атаки и кулдаун BasicAttackSkill
        CharacterStats statsForLog = caster.GetComponent<CharacterStats>();
        if (statsForLog != null)
        {
            Debug.Log($"[BasicAttackSkill] AttackSpeed: {statsForLog.attackSpeed:F2}, Cooldown: {Cooldown:F3}s, ignoreGlobalCooldown: {ignoreGlobalCooldown} (caster: {caster.name})");
        }
        
        // УБРАНО: Двойной кулдаун - PlayerActionSystem уже ждет полный кулдаун
        // caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        // Управление циклом атаки через AntiOrbWalkingSystem
        AntiOrbWalkingSystem antiOrbSystem = caster.GetComponent<AntiOrbWalkingSystem>();
        if (antiOrbSystem != null)
        {
            antiOrbSystem.StartAttackCycle();
        }
        
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        int damage = Random.Range(stats.minAttack, stats.maxAttack + 1);
        bool isCritical = stats.TryCriticalHit();
        // Try HealthMonster first for monsters, then fall back to Health
        HealthMonster targetHealthMonster = targetObject.GetComponent<HealthMonster>();
        if (targetHealthMonster != null)
        {
            targetHealthMonster.TakeDamage(damage, SkillDamageType, isCritical, caster.netIdentity, true);
        }
        else
        {
            Health targetHealth = targetObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, SkillDamageType, isCritical, caster.netIdentity, 1f, true);
            }
        }
        Vector3 startPos = caster.transform.position + vfxStartOffset;
        Quaternion startRot = caster.transform.rotation;
        Vector3 targetPos = targetObject.transform.position + vfxTargetOffset;
        caster.GetComponent<PlayerSkills>().RpcPlayBasicAttackVFX(startPos, startRot, targetPos, isCritical, _skillName);
        
        // Завершаем цикл атаки через AntiOrbWalkingSystem
        if (antiOrbSystem != null)
        {
            antiOrbSystem.EndAttackCycle();
        }
    }

    public void PlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical, PlayerSkills playerSkills)
    {
        if (vfxPrefab != null)
        {
            // VFX ���������� �� ����
            GameObject vfxInstance = Object.Instantiate(vfxPrefab, endPosition, Quaternion.identity);
            if (isCritical && vfxInstance.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = criticalHitColor;
            }
            Object.Destroy(vfxInstance, 0.2f);
            // VFX spawned at target position
        }
        if (isCritical && criticalHitVfxPrefab != null)
        {
            // ����������� VFX ����� �� ����
            GameObject critVfx = Object.Instantiate(criticalHitVfxPrefab, endPosition, Quaternion.identity);
            Object.Destroy(critVfx, 1f);
            // Critical VFX spawned
        }
        if (projectilePrefab != null)
        {
            // ������ �������� �� ��������� � ����
            GameObject projectileInstance = Object.Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            if (isCritical && projectileInstance.TryGetComponent<Renderer>(out var projectileRenderer))
            {
                projectileRenderer.material.color = criticalHitColor;
                projectileInstance.transform.localScale *= 1.3f;
            }
            playerSkills.StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition, isCritical));
        }
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 start, Vector3 end, bool isCritical)
    {
        float actualSpeed = isCritical ? projectileSpeed * 1.5f : projectileSpeed;
        while (projectile != null && Vector3.Distance(projectile.transform.position, end) > 0.1f)
        {
            projectile.transform.position = Vector3.MoveTowards(
                projectile.transform.position,
                end,
                actualSpeed * Time.deltaTime
            );
            yield return null;
        }
        if (projectile != null)
        {
            if (impactEffectPrefab != null)
            {
                // ������ ��������� �� ����
                GameObject impact = Object.Instantiate(impactEffectPrefab, end, Quaternion.identity);
                if (isCritical && impact.TryGetComponent<Renderer>(out var impactRenderer))
                {
                    impactRenderer.material.color = criticalHitColor;
                    impact.transform.localScale *= 1.5f;
                }
                Object.Destroy(impact, isCritical ? 2f : 1f);
                Debug.Log($"[BasicAttackSkill] Spawned impact effect at target position: {end}");
            }
            Object.Destroy(projectile);
        }
    }
    
    /// <summary>
    /// Переопределяем SetIndicatorVisibility для использования эффективной дальности
    /// </summary>
    public override void SetIndicatorVisibility(bool isVisible)
    {
        if (_player != null)
        {
            if (isVisible)
            {
                if (castRangeIndicator == null && castRangePrefab != null)
                {
                    castRangeIndicator = Object.Instantiate(castRangePrefab, _player.transform);
                    castRangeIndicator.transform.localPosition = Vector3.up * 0.01f;
                    castRangeIndicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    castRangeIndicator.name = $"{SkillName} Cast Range";
                }
                if (castRangeIndicator != null)
                {
                    // Используем Range (который теперь возвращает attackRange из CharacterStats)
                    castRangeIndicator.transform.localScale = new Vector3(Range * 2, 0.1f, Range * 2);
                    castRangeIndicator.SetActive(true);
                }
                if (effectRadiusIndicator == null && effectRadiusPrefab != null && EffectRadius > 0)
                {
                    effectRadiusIndicator = Object.Instantiate(effectRadiusPrefab);
                    effectRadiusIndicator.transform.localScale = new Vector3(EffectRadius * 2, 0.1f, EffectRadius * 2);
                    effectRadiusIndicator.name = $"{SkillName} Effect Radius";
                }
                if (effectRadiusIndicator != null) effectRadiusIndicator.SetActive(true);
            }
            else
            {
                if (castRangeIndicator != null) castRangeIndicator.SetActive(false);
                if (effectRadiusIndicator != null) effectRadiusIndicator.SetActive(false);
            }
        }
    }
}