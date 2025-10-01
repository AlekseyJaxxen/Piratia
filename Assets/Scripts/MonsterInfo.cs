using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "MonsterInfo", menuName = "Monster/MonsterInfo", order = 1)]
public class MonsterInfo : ScriptableObject
{
    [Header("Monster Settings")]
    public string monsterName = "Monster";
    public float moveSpeed = 5f;
    public float attackCooldown = 2f;
    public GameObject deathVFXPrefab;
    public bool canMove = true;
    public bool canAttack = true;
    public GameObject slowEffectPrefab;
    [Header("Aggro & Experience")]
    public int experienceReward = 50;
    [Header("Drop Settings")]
    public List<DropEntry> dropTable = new List<DropEntry>();
    public GameObject droppedItemPrefab;
    [Header("Health Settings")] // ���������
    public int maxHealth = 100;
    public MonsterBasicAttackSkill basicAttackSkill; // Радиус атаки берется из этого скилла
    
    [Header("Combat Stats")]
    public int hitRate = 10; // Hit rate for this monster
    public int dodge = 10; // Dodge for this monster
    
    [Header("Physics Settings")]
    public GameObject physicsModel;
    public Vector3 minForce = new Vector3(-5f, 2f, -5f);
    public Vector3 maxForce = new Vector3(5f, 5f, 0f);
    public int monsterId;
    public string aiType = "AI2"; // "AI2" ��� "AI3"
    public GameObject modelPrefab;
    [Header("Combined Settings")]
    public bool isCombined = false;
    public MonsterInfo legsInfo;
    [Header("Combined Attack Ranges")]
    public float headAttackRange = 3f;  // Радиус атаки головы (выше)
    public float legsAttackRange = 2f;  // Радиус атаки ног (ниже)
    
    [Header("AI Settings")]
    public float patrolRadius = 10f;
    public float chaseTimeout = 30f;
    public float detectionRange = 10f;
    // attackRange удален - теперь берется из basicAttackSkill.Range
    public LayerMask playerLayer = -1;
    
    [Header("Monster Skills")]
    public List<MonsterSkillEntry> monsterSkills = new List<MonsterSkillEntry>();
}

[System.Serializable]
public class MonsterSkillEntry
{
    public SkillBase skill;
    public float cooldown = 5f;
    public float useChance = 0.3f; // 30% chance to use this skill
    public float minHealthPercentage = 0.5f; // Use when health is below 50%
    public float maxHealthPercentage = 1f; // Use when health is above 0%
    public float minDistance = 0f; // Minimum distance to target
    public float maxDistance = 10f; // Maximum distance to target
    public bool requiresTarget = true; // Does this skill need a target?
}