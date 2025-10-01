using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewBombSkill", menuName = "Skills/BombSkill")]
public class BombSkill : SkillBase
{
    [Header("Bomb Settings")]
    public int baseDamage = 100;
    public float damageMultiplier = 1f;
    public float explosionRadius = 5f;
    public float explosionDelay = 3f; // Время до взрыва в секундах
    
    [Header("Visual Settings")]
    public GameObject bombPrefab; // Префаб бомбы
    public GameObject explosionEffect; // Эффект взрыва
    public GameObject zoneIndicator; // Индикатор зоны поражения
    public Color zoneColor = Color.red;
    public float zoneAlpha = 0.3f;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("[BombSkill] Target position is null");
            return;
        }
        
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[BombSkill] PlayerSkills component missing on caster");
            return;
        }
        
        // Attempting to place bomb
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (!targetPosition.HasValue) return;
        
        Debug.Log($"[BombSkill] ExecuteOnServer called, explosionEffect={explosionEffect != null}, zoneIndicator={zoneIndicator != null}");
        Debug.Log($"[BombSkill] Parameters: baseDamage={baseDamage}, explosionRadius={explosionRadius}, explosionDelay={explosionDelay}");
        if (explosionEffect != null) Debug.Log($"[BombSkill] explosionEffect name: {explosionEffect.name}");
        if (zoneIndicator != null) Debug.Log($"[BombSkill] zoneIndicator name: {zoneIndicator.name}");
        
        // Создаем бомбу на сервере
        // Поднимаем бомбу на 0.2f чтобы избежать z-fighting с terrain
        Vector3 spawnPosition = targetPosition.Value + Vector3.up * 0.2f;
        GameObject bombObject = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
        BombObject bombScript = bombObject.GetComponent<BombObject>();
        
        // Спавним бомбу в сети ПЕРЕД инициализацией
        NetworkServer.Spawn(bombObject);
        
        if (bombScript != null)
        {
            // Настраиваем параметры бомбы ПОСЛЕ спавна
            bombScript.Initialize(
                baseDamage,
                damageMultiplier,
                explosionRadius,
                explosionDelay,
                explosionEffect,
                zoneIndicator,
                zoneColor,
                zoneAlpha,
                (int)caster.team,
                caster.netIdentity
            );
        }
        
        // Bomb placed
    }
    
    // Метод для использования монстрами
    public void ExecuteForMonster(Monster caster, Vector3 targetPosition)
    {
        if (bombPrefab == null)
        {
            Debug.LogError("[BombSkill] BombPrefab not assigned!");
            return;
        }
        
        // Создаем бомбу на сервере
        // Поднимаем бомбу на 0.2f чтобы избежать z-fighting с terrain
        Vector3 spawnPosition = targetPosition + Vector3.up * 0.2f;
        GameObject bombObject = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
        BombObject bombScript = bombObject.GetComponent<BombObject>();
        
        // Спавним бомбу в сети ПЕРЕД инициализацией
        NetworkServer.Spawn(bombObject);
        
        if (bombScript != null)
        {
            // Настраиваем параметры бомбы для монстра ПОСЛЕ спавна
            bombScript.Initialize(
                baseDamage,
                damageMultiplier,
                explosionRadius,
                explosionDelay,
                explosionEffect,
                zoneIndicator,
                zoneColor,
                zoneAlpha,
                (int)PlayerTeam.None, // Team None для монстров
                caster.netIdentity
            );
        }
        
        // Monster bomb placed
    }
}
