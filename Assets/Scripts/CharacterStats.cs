using UnityEngine;
using Mirror;

public class CharacterStats : NetworkBehaviour
{
    [Header("Character Class")]
    [SyncVar]
    public CharacterClass characterClass = CharacterClass.Warrior;

    [Header("Level and Experience")]
    [SyncVar(hook = nameof(OnLevelChanged))]
    public int level = 1;
    [SyncVar]
    public int currentExperience = 0;
    [SyncVar]
    public int totalExperience = 0;
    [SyncVar]
    public int skillPoints = 0;
    [SyncVar]
    public int characteristicPoints = 0;

    [Header("Base Attributes")]
    [SyncVar]
    public int strength = 5;
    [SyncVar]
    public int agility = 5;
    [SyncVar]
    public int spirit = 5;
    [SyncVar]
    public int constitution = 5;
    [SyncVar]
    public int accuracy = 5;
    [SyncVar]
    public int intelligence = 5;

    [Header("Combat Stats")]
    [SyncVar]
    public float movementSpeed = 8f;
    [SyncVar]
    public int maxHealth = 1000;
    [SyncVar]
    public int minAttack = 5;
    [SyncVar]
    public int maxAttack = 5;
    [SyncVar]
    public float attackSpeed = 1.0f;
    [SyncVar]
    public float dodgeChance = 5.0f;
    [SyncVar]
    public float hitChance = 80.0f;
    [SyncVar]
    public float criticalHitChance = 15.0f;
    [SyncVar]
    public float criticalHitMultiplier = 2.0f;

    [Header("New Attributes")]
    [SyncVar]
    public int maxMana = 100;
    [SyncVar]
    public int armor = 0;
    [SyncVar]
    public float physicalResistance = 0f;

    [Header("Current Stats")]
    [SyncVar(hook = nameof(OnManaChanged))]
    public int currentMana;

    // События для UI
    public event System.Action<int, int> OnManaChangedEvent;
    public event System.Action<int, int> OnLevelChangedEvent;

    private static readonly int[] ExperiencePerLevel = new int[100];

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeExperienceTable();
        CalculateDerivedStats();
        currentMana = maxMana;
        totalExperience = CalculateTotalExperience();
        skillPoints = level - 1;
        characteristicPoints = CalculateCharacteristicPoints();
    }

    private void InitializeExperienceTable()
    {
        for (int i = 0; i < 100; i++)
        {
            ExperiencePerLevel[i] = 10 + (i * i * 5);
        }
    }

    private int CalculateTotalExperience()
    {
        int total = 0;
        for (int i = 0; i < 100; i++)
        {
            total += ExperiencePerLevel[i];
        }
        return total;
    }

    private int CalculateCharacteristicPoints()
    {
        int points = 0;
        for (int i = 1; i <= level; i++)
        {
            points += (i % 5 == 0) ? 5 : 1;
        }
        return points;
    }

    [Server]
    public void AddExperience(int amount)
    {
        if (level >= 100) return;

        currentExperience += amount;
        while (currentExperience >= ExperiencePerLevel[level - 1] && level < 100)
        {
            currentExperience -= ExperiencePerLevel[level - 1];
            level++;
            skillPoints++;
            characteristicPoints += (level % 5 == 0) ? 5 : 1;
            CalculateDerivedStats();
            Debug.Log($"Player leveled up to {level}! Skill Points: {skillPoints}, Characteristic Points: {characteristicPoints}");
        }
        if (level == 100)
        {
            currentExperience = 0;
        }
    }

    [Server]
    public void CalculateDerivedStats()
    {
        int newMaxHealth = 1000 + (constitution * 10);
        Health healthComponent = GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(newMaxHealth);
        }
        movementSpeed = 8f;
        PlayerMovement movementComponent = GetComponent<PlayerMovement>();
        if (movementComponent != null)
        {
            movementComponent.moveSpeed = movementSpeed;
        }
        attackSpeed = 1.0f + (agility * 0.05f);
        dodgeChance = 5.0f + (agility * 0.5f);
        hitChance = 80.0f + (accuracy * 1.0f);
        criticalHitChance = 15.0f + (agility * 0.2f);
        maxMana = 100 + (intelligence * 10);
        armor = constitution * 2;
        physicalResistance = Mathf.Min(constitution * 0.5f, 80f);
        currentMana = Mathf.Min(currentMana, maxMana);
    }

    [Client]
    public bool HasEnoughMana(int amount)
    {
        return currentMana >= amount;
    }

    [Server]
    public bool ConsumeMana(int amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true;
        }
        return false;
    }

    [Server]
    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
    }

    [Client]
    public void OnManaChanged(int oldMana, int newMana)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Mana changed: {oldMana} -> {newMana}");
        }
        OnManaChangedEvent?.Invoke(oldMana, newMana);
    }

    [Client]
    public void OnLevelChanged(int oldLevel, int newLevel)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Level changed: {oldLevel} -> {newLevel}");
        }
        OnLevelChangedEvent?.Invoke(oldLevel, newLevel);
    }

    [Server]
    public bool TryCriticalHit()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        return randomValue <= criticalHitChance;
    }

    [Server]
    public int CalculateDamageWithCrit(int baseDamage, out bool isCritical)
    {
        isCritical = TryCriticalHit();
        if (isCritical)
        {
            return Mathf.RoundToInt(baseDamage * criticalHitMultiplier);
        }
        return baseDamage;
    }
}