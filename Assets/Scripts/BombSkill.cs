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
        
        // Создаем бомбу на сервере
        GameObject bombObject = Instantiate(bombPrefab, targetPosition.Value, Quaternion.identity);
        BombObject bombScript = bombObject.GetComponent<BombObject>();
        
        if (bombScript != null)
        {
            // Настраиваем параметры бомбы
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
        
        // Спавним бомбу в сети
        NetworkServer.Spawn(bombObject);
        
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
        GameObject bombObject = Instantiate(bombPrefab, targetPosition, Quaternion.identity);
        BombObject bombScript = bombObject.GetComponent<BombObject>();
        
        if (bombScript != null)
        {
            // Настраиваем параметры бомбы для монстра
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
        
        // Спавним бомбу в сети
        NetworkServer.Spawn(bombObject);
        
        // Monster bomb placed
    }
}
