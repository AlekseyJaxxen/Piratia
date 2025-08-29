using UnityEngine;
using Mirror;

public class CharacterStats : NetworkBehaviour
{
    [Header("Character Class")]
    [SyncVar]
    public CharacterClass characterClass = CharacterClass.Warrior; // —сылка на Enums.cs

    [Header("Monster Attributes")]
    //[SyncVar] public MonsterRace race = MonsterRace.None;
    //[SyncVar] public MonsterElement element = MonsterElement.Neutral;

    [Header("Level and Experience")]
    [SyncVar(hook = nameof(OnLevelChanged))]
    public int level = 1;
    [SyncVar]
    public int currentExperience = 0;
    [SyncVar]
    public int totalExperience = 0;
    [SyncVar]
    public int skillPoints = 0;
    [SyncVar(hook = nameof(OnCharacteristicPointsChanged))]
    public int characteristicPoints = 0;

    [Header("Base Attributes")]
    [SyncVar(hook = nameof(OnStrengthChanged))]
    public int strength = 5;
    [SyncVar(hook = nameof(OnAgilityChanged))]
    public int agility = 5;
    [SyncVar(hook = nameof(OnSpiritChanged))]
    public int spirit = 5;
    [SyncVar(hook = nameof(OnConstitutionChanged))]
    public int constitution = 5;
    [SyncVar(hook = nameof(OnAccuracyChanged))]
    public int accuracy = 5;
    [SyncVar]
    public int intelligence = 5;

    [Header("Combat Stats")]
    [SyncVar]
    public float movementSpeed = 8f;
    [SyncVar]
    public int maxHealth = 1000;
    [SyncVar(hook = nameof(OnMinAttackChangedHook))]
    public int minAttack = 5;
    [SyncVar(hook = nameof(OnMaxAttackChangedHook))]
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
    [SyncVar]
    public float magicDamageMultiplier = 1.0f;

    [Header("Current Stats")]
    [SyncVar(hook = nameof(OnManaChanged))]
    public int currentMana;

    public event System.Action<int, int> OnManaChangedEvent;
    public event System.Action<int, int> OnLevelChangedEvent;
    public event System.Action<int, int> OnCharacteristicPointsChangedEvent;
    public event System.Action<int, int> OnStrengthChangedEvent;
    public event System.Action<int, int> OnAgilityChangedEvent;
    public event System.Action<int, int> OnSpiritChangedEvent;
    public event System.Action<int, int> OnConstitutionChangedEvent;
    public event System.Action<int, int> OnAccuracyChangedEvent;
    public event System.Action<int, int> OnMinAttackChangedEvent;
    public event System.Action<int, int> OnMaxAttackChangedEvent;

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
        Debug.Log($"[Server] Character initialized: class={characterClass}, strength={strength}, minAttack={minAttack}, maxAttack={maxAttack}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[Client] Character initialized: class={characterClass}, strength={strength}, minAttack={minAttack}, maxAttack={maxAttack}");
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
        int newMaxHealth = 1000 + (constitution * 20);
        Health healthComponent = GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(newMaxHealth);
        }
       // movementSpeed = 8f + (agility * 0.2f);
        //PlayerMovement movementComponent = GetComponent<PlayerMovement>();
       // if (movementComponent != null)
      //  {
      //     movementComponent.moveSpeed = movementSpeed;
       // }
        attackSpeed = 1.0f + (agility * 0.05f);
        dodgeChance = 5.0f + (agility * 0.5f);
        hitChance = 80.0f + (accuracy * 1.0f);
        criticalHitChance = 15.0f + (agility * 0.2f);
        maxMana = 100 + (intelligence * 5) + (spirit * 5);
        armor = constitution * 3;
        physicalResistance = 0f;
        magicDamageMultiplier = 1.0f + (spirit * 0.05f);
        currentMana = Mathf.Min(currentMana, maxMana);
        if (characterClass == CharacterClass.Warrior)
        {
            minAttack = 5 + (strength * 3);
            maxAttack = 10 + (strength * 3);
        }
        else if (characterClass == CharacterClass.Archer)
        {
            minAttack = 5 + (accuracy * 2);
            maxAttack = 10 + (accuracy * 2);
        }
        else
        {
            minAttack = 5;
            maxAttack = 10;
        }
        Debug.Log($"[Server] CalculateDerivedStats: class={characterClass}, strength={strength}, minAttack={minAttack}, maxAttack={maxAttack}, attackSpeed={attackSpeed}, dodgeChance={dodgeChance}, hitChance={hitChance}");
    }

    [Server]
    public bool IncreaseStat(string statName)
    {
        if (characteristicPoints <= 0) return false;
        characteristicPoints--;
        switch (statName.ToLower())
        {
            case "strength":
                strength++;
                break;
            case "agility":
                agility++;
                break;
            case "spirit":
                spirit++;
                break;
            case "constitution":
                constitution++;
                break;
            case "accuracy":
                accuracy++;
                break;
            default:
                characteristicPoints++;
                return false;
        }
        CalculateDerivedStats();
        Debug.Log($"[Server] Increased {statName} to {GetStatValue(statName)}. minAttack={minAttack}, maxAttack={maxAttack}, characteristicPoints={characteristicPoints}");
        return true;
    }

    [Server]
    private int GetStatValue(string statName)
    {
        switch (statName.ToLower())
        {
            case "strength": return strength;
            case "agility": return agility;
            case "spirit": return spirit;
            case "constitution": return constitution;
            case "accuracy": return accuracy;
            default: return 0;
        }
    }

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

    [Client]
    public void OnCharacteristicPointsChanged(int oldPoints, int newPoints)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Characteristic Points changed: {oldPoints} -> {newPoints}");
        }
        OnCharacteristicPointsChangedEvent?.Invoke(oldPoints, newPoints);
    }

    [Client]
    public void OnStrengthChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Strength changed: {oldValue} -> {newValue}");
        }
        OnStrengthChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnAgilityChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Agility changed: {oldValue} -> {newValue}");
        }
        OnAgilityChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnSpiritChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Spirit changed: {oldValue} -> {newValue}");
        }
        OnSpiritChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnConstitutionChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Constitution changed: {oldValue} -> {newValue}");
        }
        OnConstitutionChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnAccuracyChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Accuracy changed: {oldValue} -> {newValue}");
        }
        OnAccuracyChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    private void OnMinAttackChangedHook(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"[Client] minAttack changed: {oldValue} -> {newValue}");
        }
        OnMinAttackChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    private void OnMaxAttackChangedHook(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"[Client] maxAttack changed: {oldValue} -> {newValue}");
        }
        OnMaxAttackChangedEvent?.Invoke(oldValue, newValue);
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