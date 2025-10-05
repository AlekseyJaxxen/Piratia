using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewMonsterBasicAttackSkill", menuName = "Skills/MonsterBasicAttackSkill")]
public class MonsterBasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public GameObject vfxPrefab;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;

    [Header("Critical Hit Settings")]
    public GameObject criticalHitVfxPrefab;
    public GameObject impactEffectPrefab;
    public Color criticalHitColor = Color.yellow;

    [Header("Monster Attack Settings")]
    public int baseDamage = 10;
    public float criticalChance = 0.1f; // 10% chance for critical hit
    public float criticalMultiplier = 1.5f;
    
    [Header("Animation Settings")]
    [Tooltip("Использовать универсальную систему ID (рекомендуется)")]
    public bool useUniversalAnimationIds = true;
    
    [Tooltip("Имя анимации атаки. Используется только если useUniversalAnimationIds = false")]
    public string customAttackAnimationName = "";
    
    [Tooltip("ID анимации атаки. Используется только если useUniversalAnimationIds = false")]
    public int customAttackAnimationId = -1;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        // Этот метод оставлен для совместимости с SkillBase, но не используется напрямую
        Debug.LogWarning($"[MonsterBasicAttackSkill] ExecuteSkillImplementation called with PlayerCore, redirecting to Monster logic");
        Monster monster = caster.GetComponent<Monster>();
        if (monster != null)
        {
            Execute(monster, targetPosition, targetObject);
        }
    }

    public void Execute(Monster caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (caster == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Monster component missing on caster for skill {_skillName}");
            return;
        }

        if (targetObject == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Target object is null for skill {_skillName}");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Target {targetObject.name} has no NetworkIdentity for skill {_skillName}");
            return;
        }

        // Воспроизводим анимацию атаки
        PlayAttackAnimation(caster);

        bool isCritical = Random.value < criticalChance;
        
        // ИСПРАВЛЕНИЕ: Используем характеристики монстра вместо baseDamage
        int monsterDamage = CalculateMonsterDamage(caster, isCritical);

        // Monster requesting attack

        // Вызываем метод в Monster для обработки сетевой атаки
        caster.ExecuteAttack(targetIdentity.netId, _skillName, monsterDamage, isCritical);
    }
    
    /// <summary>
    /// Рассчитывает урон монстра на основе его характеристик
    /// </summary>
    private int CalculateMonsterDamage(Monster monster, bool isCritical)
    {
        if (monster.info == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Monster {monster.name} has no MonsterInfo, using baseDamage");
            return isCritical ? Mathf.RoundToInt(baseDamage * criticalMultiplier) : baseDamage;
        }
        
        // Используем характеристики монстра (minAttack и maxAttack из MonsterInfo)
        int minDamage = monster.info.minAttack;
        int maxDamage = monster.info.maxAttack;
        
        // Случайный урон в диапазоне
        int randomDamage = Random.Range(minDamage, maxDamage + 1);
        
        // Применяем критический множитель если нужно
        if (isCritical)
        {
            randomDamage = Mathf.RoundToInt(randomDamage * criticalMultiplier);
        }
        
        Debug.Log($"[MonsterBasicAttackSkill] {monster.name} damage: {minDamage}-{maxDamage} → {randomDamage} (critical: {isCritical})");
        
        return randomDamage;
    }
    
    /// <summary>
    /// Воспроизводит анимацию атаки в зависимости от типа монстра
    /// </summary>
    private void PlayAttackAnimation(Monster caster)
    {
        if (useUniversalAnimationIds)
        {
            // Универсальная система: ID 0 = всегда атака
            PlayUniversalAnimation(caster, UniversalAnimationId.Attack);
        }
        else
        {
            // Старая система с пользовательскими настройками
            if (!string.IsNullOrEmpty(customAttackAnimationName))
            {
                PlayCustomAnimation(caster, customAttackAnimationName);
            }
            else if (customAttackAnimationId >= 0)
            {
                PlayCustomAnimation(caster, customAttackAnimationId);
            }
            else
            {
                Debug.LogWarning($"[MonsterBasicAttackSkill] No animation configured for skill {_skillName}");
            }
        }
    }
    
    /// <summary>
    /// Воспроизводит универсальную анимацию
    /// </summary>
    private void PlayUniversalAnimation(Monster caster, UniversalAnimationId animationId)
    {
        if (caster == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Monster is null for universal animation");
            return;
        }
        
        // Простое логирование - анимации будут обрабатываться в Monster.cs
        Debug.Log($"[MonsterBasicAttackSkill] Would play universal animation {animationId} for {caster.name}");
    }
    
    /// <summary>
    /// Воспроизводит пользовательскую анимацию по имени
    /// </summary>
    private void PlayCustomAnimation(Monster caster, string animationName)
    {
        if (caster == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Monster is null for custom animation");
            return;
        }
        
        // Простое логирование - анимации будут обрабатываться в Monster.cs
        Debug.Log($"[MonsterBasicAttackSkill] Would play custom animation '{animationName}' for {caster.name}");
    }
    
    /// <summary>
    /// Воспроизводит пользовательскую анимацию по ID
    /// </summary>
    private void PlayCustomAnimation(Monster caster, int animationId)
    {
        if (caster == null)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Monster is null for custom animation");
            return;
        }
        
        // Простое логирование - анимации будут обрабатываться в Monster.cs
        Debug.Log($"[MonsterBasicAttackSkill] Would play custom animation ID {animationId} for {caster.name}");
    }
    
    /// <summary>
    /// Воспроизводит VFX для атаки
    /// </summary>
    public void PlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical, Monster caster)
    {
        if (isCritical && criticalHitVfxPrefab != null)
        {
            // Критический удар VFX
            GameObject vfx = Instantiate(criticalHitVfxPrefab, endPosition, startRotation);
            Destroy(vfx, 5f); // Удаляем через 5 секунд
        }
        else if (vfxPrefab != null)
        {
            // Обычный удар VFX
            GameObject vfx = Instantiate(vfxPrefab, endPosition, startRotation);
            Destroy(vfx, 3f); // Удаляем через 3 секунды
        }
        
        // Impact эффект
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, endPosition, Quaternion.identity);
            Destroy(impact, 2f); // Удаляем через 2 секунды
        }
    }
}