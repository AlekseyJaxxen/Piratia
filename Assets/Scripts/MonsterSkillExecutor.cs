using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class MonsterSkillExecutor : NetworkBehaviour
{
    private Monster monster;
    private HealthMonster health;
    private List<MonsterSkillEntry> availableSkills = new List<MonsterSkillEntry>();
    private Dictionary<SkillBase, float> skillCooldowns = new Dictionary<SkillBase, float>();
    
    private void Awake()
    {
        monster = GetComponent<Monster>();
        health = GetComponent<HealthMonster>();
    }
    
    public void InitializeSkills(List<MonsterSkillEntry> skills)
    {
        availableSkills = new List<MonsterSkillEntry>(skills);
        skillCooldowns.Clear();
        
        foreach (var skillEntry in availableSkills)
        {
            if (skillEntry.skill != null)
            {
                skillCooldowns[skillEntry.skill] = 0f;
            }
        }
    }
    
    [Server]
    public bool TryUseSkill(PlayerCore target = null)
    {
        Debug.Log($"[MonsterSkillExecutor] TryUseSkill called for {monster.name}, target: {target?.name}");
        
        if (monster.IsDead || monster.IsStunned || monster.IsSilenced)
        {
            Debug.Log($"[MonsterSkillExecutor] {monster.name} cannot use skill: IsDead={monster.IsDead}, IsStunned={monster.IsStunned}, IsSilenced={monster.IsSilenced}");
            return false;
        }
            
        float currentHealthPercentage = (float)health.CurrentHealth / health.MaxHealth;
        Debug.Log($"[MonsterSkillExecutor] {monster.name} health: {health.CurrentHealth}/{health.MaxHealth} ({currentHealthPercentage:P})");
        
        if (availableSkills.Count == 0)
        {
            Debug.Log($"[MonsterSkillExecutor] {monster.name} has no available skills!");
            return false;
        }
        
        foreach (var skillEntry in availableSkills)
        {
            if (skillEntry.skill == null) 
            {
                Debug.Log($"[MonsterSkillExecutor] {monster.name} skill is null, skipping");
                continue;
            }
            
            Debug.Log($"[MonsterSkillExecutor] {monster.name} checking skill: {skillEntry.skill.name}");
            
            // Check cooldown
            if (Time.time < skillCooldowns[skillEntry.skill])
            {
                Debug.Log($"[MonsterSkillExecutor] {monster.name} skill {skillEntry.skill.name} on cooldown: {skillCooldowns[skillEntry.skill] - Time.time:F1}s left");
                continue;
            }
                
            // Check health percentage
            if (currentHealthPercentage < skillEntry.minHealthPercentage || 
                currentHealthPercentage > skillEntry.maxHealthPercentage)
            {
                Debug.Log($"[MonsterSkillExecutor] {monster.name} skill {skillEntry.skill.name} health check failed: {currentHealthPercentage:P} not in range [{skillEntry.minHealthPercentage:P}-{skillEntry.maxHealthPercentage:P}]");
                continue;
            }
                
            // Check if skill requires target
            if (skillEntry.requiresTarget && target == null)
            {
                Debug.Log($"[MonsterSkillExecutor] {monster.name} skill {skillEntry.skill.name} requires target but none provided");
                continue;
            }
                
            // Check distance to target
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < skillEntry.minDistance || distance > skillEntry.maxDistance)
                {
                    Debug.Log($"[MonsterSkillExecutor] {monster.name} skill {skillEntry.skill.name} distance check failed: {distance:F1} not in range [{skillEntry.minDistance}-{skillEntry.maxDistance}]");
                    continue;
                }
            }
            
            // Check use chance
            float randomValue = Random.value;
            if (randomValue > skillEntry.useChance)
            {
                Debug.Log($"[MonsterSkillExecutor] {monster.name} skill {skillEntry.skill.name} chance failed: {randomValue:F2} > {skillEntry.useChance:F2}");
                continue;
            }
                
            // Execute skill
            Debug.Log($"[MonsterSkillExecutor] {monster.name} USING SKILL: {skillEntry.skill.name}!");
            ExecuteSkill(skillEntry, target);
            skillCooldowns[skillEntry.skill] = Time.time + skillEntry.cooldown;
            return true;
        }
        
        Debug.Log($"[MonsterSkillExecutor] {monster.name} No skills available/all checks failed");
        return false;
    }
    
    [Server]
    private void ExecuteSkill(MonsterSkillEntry skillEntry, PlayerCore target)
    {
        SkillBase skill = skillEntry.skill;
        
        // Using skill
        
        // Handle different skill types
        if (skill is AoeDamageSkill aoeSkill)
        {
            Vector3 targetPosition = target != null ? target.transform.position : transform.position;
            ExecuteAoeDamageSkill(aoeSkill, targetPosition);
        }
        else if (skill is AoeDebuffGroundSkill debuffSkill)
        {
            Vector3 targetPosition = target != null ? target.transform.position : transform.position;
            ExecuteAoeDebuffSkill(debuffSkill, targetPosition);
        }
        else if (skill is BombSkill bombSkill)
        {
            Vector3 targetPosition = target != null ? target.transform.position : transform.position;
            ExecuteBombSkill(bombSkill, targetPosition);
        }
        else if (skill is MonsterBasicAttackSkill basicSkill)
        {
            if (target != null)
            {
                basicSkill.Execute(monster, null, target.gameObject);
            }
        }
        else
        {
            // Generic skill execution
            ExecuteGenericSkill(skill, target);
        }
    }
    
    [Server]
    private void ExecuteAoeDamageSkill(AoeDamageSkill skill, Vector3 targetPosition)
    {
        // Calculate damage (monster doesn't have CharacterStats, so use base damage)
        int totalDamage = skill.baseDamage;
        
        // Find targets in AOE radius
        int aoeLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Monster", "Enemy");
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, skill.aoeRadius, aoeLayerMask);
        
        foreach (Collider col in hitColliders)
        {
            Health targetHealth = col.GetComponent<Health>();
            HealthMonster targetHealthMonster = col.GetComponent<HealthMonster>();
            
            if (targetHealthMonster != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                if (targetCore != null) // Only damage players, not other monsters
                {
                    targetHealthMonster.TakeDamage(totalDamage, skill.SkillDamageType, false, monster.netIdentity, skill.damageMultiplier);
                }
            }
            else if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                if (targetCore != null) // Only damage players, not other monsters
                {
                    targetHealth.TakeDamage(totalDamage, skill.SkillDamageType, false, monster.netIdentity, skill.damageMultiplier);
                }
            }
        }
        
        // Play VFX
        RpcPlayAoeEffect(targetPosition, skill.effectPrefab);
    }
    
    [Server]
    private void ExecuteAoeDebuffSkill(AoeDebuffGroundSkill skill, Vector3 targetPosition)
    {
        GameObject groundEffect = Instantiate(skill.groundEffectPrefab, targetPosition, Quaternion.identity);
        NetworkServer.Spawn(groundEffect);
        
        int aoeLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Enemy");
        groundEffect.GetComponent<GroundEffect>().Init(skill.slowPercentage, skill.duration, skill.aoeRadius, 0, aoeLayerMask); // Team 0 for monsters
    }
    
    [Server]
    private void ExecuteBombSkill(BombSkill skill, Vector3 targetPosition)
    {
        skill.ExecuteForMonster(monster, targetPosition);
    }
    
    [Server]
    private void ExecuteGenericSkill(SkillBase skill, PlayerCore target)
    {
        // For skills that don't have specific monster implementations
        // This is a fallback - you might need to create specific monster versions
        Debug.LogWarning($"[MonsterSkillExecutor] Generic skill execution not implemented for {skill.SkillName}");
    }
    
    [ClientRpc]
    private void RpcPlayAoeEffect(Vector3 position, GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    public bool IsSkillOnCooldown(SkillBase skill)
    {
        if (skillCooldowns.ContainsKey(skill))
        {
            return Time.time < skillCooldowns[skill];
        }
        return false;
    }
    
    public float GetSkillCooldownRemaining(SkillBase skill)
    {
        if (skillCooldowns.ContainsKey(skill))
        {
            float remaining = skillCooldowns[skill] - Time.time;
            return Mathf.Max(0f, remaining);
        }
        return 0f;
    }
}
