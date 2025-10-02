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
        int damage = isCritical ? Mathf.RoundToInt(baseDamage * criticalMultiplier) : baseDamage;

        // Monster requesting attack

        // Вызываем метод в Monster для обработки сетевой атаки
        caster.ExecuteAttack(targetIdentity.netId, _skillName, damage, isCritical);
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
            PlayCustomAnimation(caster);
        }
    }
    
    /// <summary>
    /// Воспроизводит анимацию по универсальному ID
    /// </summary>
    private void PlayUniversalAnimation(Monster caster, UniversalAnimationId universalId)
    {
        int animationId = (int)universalId;
        int animationCount = caster.GetAnimationCount();
        
        if (animationCount == 0)
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] No animations available for {caster.monsterName}. Cannot play {universalId} animation.");
            return;
        }
        
        // Проверяем, есть ли анимация с нужным ID
        if (animationId < animationCount)
        {
            string animName = caster.GetAnimationName(animationId);
            
            // Проверяем, что имя анимации не пустое
            if (string.IsNullOrEmpty(animName))
            {
                Debug.LogWarning($"[MonsterBasicAttackSkill] Animation at ID {animationId} has empty name for {caster.monsterName}. Trying fallback.");
                // Пробуем найти первую анимацию с непустым именем
                for (int i = 0; i < animationCount; i++)
                {
                    string fallbackName = caster.GetAnimationName(i);
                    if (!string.IsNullOrEmpty(fallbackName))
                    {
                        Debug.Log($"[MonsterBasicAttackSkill] Using fallback animation (ID: {i}): '{fallbackName}' for {caster.monsterName}");
                        caster.PlayAnimationById(i);
                        return;
                    }
                }
                Debug.LogError($"[MonsterBasicAttackSkill] All animations have empty names for {caster.monsterName}. Cannot play any animation.");
                return;
            }
            
            Debug.Log($"[MonsterBasicAttackSkill] Playing universal {universalId} animation (ID: {animationId}): '{animName}' for {caster.monsterName}");
            caster.PlayAnimationById(animationId);
        }
        else
        {
            // Fallback: используем первую доступную анимацию
            string firstAnimName = caster.GetAnimationName(0);
            if (string.IsNullOrEmpty(firstAnimName))
            {
                Debug.LogError($"[MonsterBasicAttackSkill] First animation has empty name for {caster.monsterName}. Cannot play fallback animation.");
                return;
            }
            
            Debug.LogWarning($"[MonsterBasicAttackSkill] Universal {universalId} ID {animationId} not available for {caster.monsterName} (only {animationCount} animations). Using first animation: '{firstAnimName}' (ID: 0)");
            caster.PlayAnimationById(0);
        }
    }
    
    /// <summary>
    /// Воспроизводит анимацию по пользовательским настройкам (старая система)
    /// </summary>
    private void PlayCustomAnimation(Monster caster)
    {
        // Приоритет 1: Пользовательская настройка по ID
        if (customAttackAnimationId >= 0)
        {
            if (customAttackAnimationId < caster.GetAnimationCount())
            {
                string animName = caster.GetAnimationName(customAttackAnimationId);
                Debug.Log($"[MonsterBasicAttackSkill] Playing custom animation by ID {customAttackAnimationId}: '{animName}' for {caster.monsterName}");
                caster.PlayAnimationById(customAttackAnimationId);
                return;
            }
            else
            {
                Debug.LogWarning($"[MonsterBasicAttackSkill] Custom animation ID {customAttackAnimationId} is out of range (0-{caster.GetAnimationCount() - 1}) for {caster.monsterName}");
            }
        }
        
        // Приоритет 2: Пользовательская настройка по имени
        if (!string.IsNullOrEmpty(customAttackAnimationName))
        {
            if (caster.HasAnimation(customAttackAnimationName))
            {
                Debug.Log($"[MonsterBasicAttackSkill] Playing custom animation by name '{customAttackAnimationName}' for {caster.monsterName}");
                caster.PlayAnimation(customAttackAnimationName);
                return;
            }
            else
            {
                Debug.LogWarning($"[MonsterBasicAttackSkill] Custom animation '{customAttackAnimationName}' not found for {caster.monsterName}");
            }
        }
        
        // Приоритет 3: Автоматическое определение
        if (caster.IsHumanoidMonster())
        {
            // Для гуманоидных монстров используем стандартную анимацию "Attack"
            Debug.Log($"[MonsterBasicAttackSkill] Playing humanoid attack animation for {caster.monsterName}");
            caster.PlayAnimation("Attack");
        }
        else if (caster.IsNonHumanoidMonster())
        {
            // Для не-гуманоидных монстров пробуем найти специфичную анимацию
            string attackAnimName = GetAttackAnimationName(caster);
            
            if (caster.HasAnimation(attackAnimName))
            {
                Debug.Log($"[MonsterBasicAttackSkill] Playing non-humanoid attack animation '{attackAnimName}' for {caster.monsterName}");
                caster.PlayAnimation(attackAnimName);
            }
            else if (caster.HasAnimation("Attack"))
            {
                // Fallback на стандартную анимацию "Attack"
                Debug.Log($"[MonsterBasicAttackSkill] Playing fallback attack animation 'Attack' for {caster.monsterName}");
                caster.PlayAnimation("Attack");
            }
            else
            {
                // Если есть только одна анимация, используем её
                int animCount = caster.GetAnimationCount();
                if (animCount > 0)
                {
                    string firstAnimName = caster.GetAnimationName(0);
                    Debug.Log($"[MonsterBasicAttackSkill] Playing first available animation '{firstAnimName}' (ID: 0) for {caster.monsterName}");
                    caster.PlayAnimationById(0);
                }
                else
                {
                    Debug.LogWarning($"[MonsterBasicAttackSkill] No animations available for {caster.monsterName}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[MonsterBasicAttackSkill] Unknown monster type for {caster.monsterName}, cannot play attack animation");
        }
    }
    
    /// <summary>
    /// Определяет имя анимации атаки на основе имени монстра
    /// </summary>
    private string GetAttackAnimationName(Monster caster)
    {
        // Пробуем найти анимацию по паттерну: {MonsterName}_attack
        string monsterName = caster.monsterName.Replace(" ", "").ToLower();
        string[] possibleNames = {
            $"{monsterName}_attack",     // mushroom_attack
            $"{caster.monsterName}_attack", // Mushroom_attack
            "attack",                    // attack
            "Attack"                     // Attack
        };
        
        foreach (string animName in possibleNames)
        {
            if (caster.HasAnimation(animName))
            {
                return animName;
            }
        }
        
        return "Attack"; // Fallback
    }

    public void PlayVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical, Monster monster)
    {
        if (vfxPrefab != null)
        {
            Quaternion xRotation = Quaternion.Euler(0, 0, 0);
            Quaternion finalRotation = startRotation * xRotation;
            GameObject vfxInstance = Object.Instantiate(vfxPrefab, startPosition, finalRotation);
            if (isCritical && vfxInstance.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = criticalHitColor;
            }
            Object.Destroy(vfxInstance, 0.2f);
        }

        if (isCritical && criticalHitVfxPrefab != null)
        {
            GameObject critVfx = Object.Instantiate(criticalHitVfxPrefab, startPosition, startRotation);
            Object.Destroy(critVfx, 1f);
        }

        if (projectilePrefab != null)
        {
            GameObject projectileInstance = Object.Instantiate(projectilePrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            if (isCritical && projectileInstance.TryGetComponent<Renderer>(out var projectileRenderer))
            {
                projectileRenderer.material.color = criticalHitColor;
                projectileInstance.transform.localScale *= 1.3f;
            }
            monster.StartCoroutine(MoveProjectile(projectileInstance, startPosition, endPosition, isCritical));
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
                GameObject impact = Object.Instantiate(impactEffectPrefab, projectile.transform.position, Quaternion.identity);
                if (isCritical && impact.TryGetComponent<Renderer>(out var impactRenderer))
                {
                    impactRenderer.material.color = criticalHitColor;
                    impact.transform.localScale *= 1.5f;
                }
                Object.Destroy(impact, isCritical ? 2f : 1f);
            }
            Object.Destroy(projectile);
        }
    }
}