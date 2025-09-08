using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CharacterStats : NetworkBehaviour
{
    [Header("Character Class")]
    [SerializeField] private ClassData classData;
    [SyncVar(hook = nameof(OnCharacterClassChanged))]
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
    public float movementSpeed;
    [SyncVar]
    public int maxHealth;
    [SyncVar(hook = nameof(OnMinAttackChangedHook))]
    public int minAttack;
    [SyncVar(hook = nameof(OnMaxAttackChangedHook))]
    public int maxAttack;
    [SyncVar]
    public float attackSpeed;
    [SyncVar]
    public float dodgeChance;
    [SyncVar]
    public float hitChance;
    [SyncVar]
    public float criticalHitChance;
    [SyncVar]
    public float criticalHitMultiplier = 2.0f;
    [Header("New Attributes")]
    [SyncVar]
    public int maxMana;
    [SyncVar]
    public int armor;
    [SyncVar]
    public float physicalResistance;
    [SyncVar]
    public float magicDamageMultiplier;
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
    public event System.Action<CharacterClass, CharacterClass> OnCharacterClassChangedEvent;
    private static readonly int[] ExperiencePerLevel = new int[100];
    private bool isClassSet = false;
    private Health healthComponent;
    public readonly List<SlowEffect> activeSlowEffects = new List<SlowEffect>();
    public readonly List<StatEffect> activeStatEffects = new List<StatEffect>();
    public struct SlowEffect
    {
        public float Percentage;
        public float Duration;
        public int SkillWeight;
        public float EndTime;
        public string Source;
    }
    public struct StatEffect
    {
        public string Stat;
        public float Value;
        public float OriginalValue;
        public float EndTime;
        public bool IsToggle;
        public GameObject VFXPrefab;
        public Vector3 VFXOffset;
        public bool IsActive => IsToggle || EndTime > Time.time; // Добавлено свойство IsActive
    }

    private void Awake()
    {
        healthComponent = GetComponent<Health>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeExperienceTable();
        StartCoroutine(WaitForClassInitialization());
    }

    private IEnumerator WaitForClassInitialization()
    {
        float timeout = 10f;
        yield return new WaitUntil(() => isClassSet || (float)NetworkTime.time > timeout);
        if (!isClassSet)
        {
            Debug.LogWarning($"[CharacterStats] Class not set within {timeout} seconds, using default class: {characterClass}");
            LoadClassData();
        }
        CalculateDerivedStats();
        currentMana = maxMana;
        totalExperience = CalculateTotalExperience();
        skillPoints = level - 1;
        characteristicPoints = CalculateCharacteristicPoints();
        StartCoroutine(InitializeSkills());
        Debug.Log($"[Server] Character initialized: class={characterClass}, strength={strength}, minAttack={minAttack}, maxAttack={maxAttack}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    [Server]
    public void LoadClassData()
    {
        classData = Resources.Load<ClassData>($"ClassData/{characterClass}");
        if (classData == null)
        {
            Debug.LogWarning($"[CharacterStats] ClassData is null for {characterClass}");
            return;
        }
        strength = classData.strength;
        agility = classData.agility;
        constitution = classData.constitution;
        spirit = classData.spirit;
        accuracy = classData.accuracy;
        intelligence = classData.intelligence;
        Debug.Log($"[CharacterStats] Loaded ClassData: class={characterClass}, strength={strength}, maxHealth={maxHealth}, maxMana={maxMana}");
    }

    private IEnumerator InitializeSkills()
    {
        PlayerSkills skills = GetComponent<PlayerSkills>();
        if (skills != null)
        {
            yield return skills.StartCoroutine("InitializeSkills");
            Debug.Log($"[CharacterStats] Skills initialized for class={characterClass}");
        }
        else
        {
            Debug.LogWarning("[CharacterStats] PlayerSkills component not found");
        }
    }

    private void OnCharacterClassChanged(CharacterClass oldClass, CharacterClass newClass)
    {
        Debug.Log($"[CharacterStats] Class changed via SyncVar: {oldClass} -> {newClass}");
        characterClass = newClass;
        LoadClassData();
        CalculateDerivedStats();
        StartCoroutine(InitializeSkills());
        OnCharacterClassChangedEvent?.Invoke(oldClass, newClass);
    }

    [Command]
    public void CmdSetClass(CharacterClass newClass)
    {
        Debug.Log($"[CharacterStats] Attempting to load ClassData for {newClass} from Resources/ClassData/{newClass}");
        var resources = Resources.LoadAll("ClassData", typeof(ClassData));
        Debug.Log($"[CharacterStats] Available ClassData files: {string.Join(", ", resources.Select(r => r.name))}");
        ClassData newClassData = Resources.Load<ClassData>($"ClassData/{newClass}");
        if (newClassData == null)
        {
            Debug.LogError($"[CharacterStats] Failed to load ClassData for {newClass}. Path: Resources/ClassData/{newClass}");
            return;
        }
        classData = newClassData;
        characterClass = newClass;
        isClassSet = true;
        strength = classData.strength;
        agility = classData.agility;
        constitution = classData.constitution;
        spirit = classData.spirit;
        accuracy = classData.accuracy;
        intelligence = classData.intelligence;
        CalculateDerivedStats();
        StartCoroutine(InitializeSkills());
        RpcSyncSkills(newClass);
        Debug.Log($"[CharacterStats] Server set class: {newClass}, strength={strength}, maxHealth={maxHealth}, maxMana={maxMana}");
    }

    [ClientRpc]
    private void RpcSyncSkills(CharacterClass newClass)
    {
        StartCoroutine(InitializeSkills());
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
        if (classData == null)
        {
            Debug.LogWarning($"[CharacterStats] ClassData is null, loading for {characterClass}");
            LoadClassData();
            if (classData == null) return;
        }
        maxHealth = classData.baseHealth + Mathf.RoundToInt(constitution * 20 * classData.constitutionMultiplier);
        maxMana = classData.baseMana + Mathf.RoundToInt(spirit * 10 * classData.spiritMultiplier + intelligence * 5 * classData.intelligenceMultiplier);
        float attackValue = classData.attackAttribute == AttackAttributeType.Strength ? strength : accuracy;
        float attackMultiplier = classData.attackAttribute == AttackAttributeType.Strength ? classData.strengthMultiplier : classData.accuracyMultiplier;
        minAttack = Mathf.RoundToInt(classData.baseMinAttack + attackValue * 2 * attackMultiplier);
        maxAttack = Mathf.RoundToInt(classData.baseMaxAttack + attackValue * 3 * attackMultiplier);
        armor = Mathf.RoundToInt(classData.baseDef + strength * 1 * classData.strengthMultiplier);
        float baseMovementSpeed = classData.baseMovementSpeed * classData.agilityMultiplier;
        float slowMultiplier = CalculateSlowMultiplier();
        movementSpeed = baseMovementSpeed * slowMultiplier;
        attackSpeed = 1.0f + (agility * 0.05f * classData.agilityMultiplier);
        dodgeChance = 5.0f + (agility * 0.5f * classData.agilityMultiplier);
        hitChance = 80.0f + (accuracy * 1.0f * classData.accuracyMultiplier);
        criticalHitChance = 15.0f + (agility * 0.2f * classData.agilityMultiplier);
        physicalResistance = classData.basePhysicalResistance;
        magicDamageMultiplier = 1.0f + (spirit * 0.05f * classData.spiritMultiplier);
        currentMana = Mathf.Min(currentMana, maxMana);
        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(maxHealth);
        }
        PlayerMovement movementComponent = GetComponent<PlayerMovement>();
        if (movementComponent != null)
        {
            movementComponent.SetMovementSpeed(movementSpeed);
        }
        Debug.Log($"[Server] CalculateDerivedStats: class={characterClass}, strength={strength}, minAttack={minAttack}, maxAttack={maxAttack}, maxHealth={maxHealth}, maxMana={maxMana}, armor={armor}, movementSpeed={movementSpeed}, attackSpeed={attackSpeed}");
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
            case "intelligence":
                intelligence++;
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
    private float GetStatValue(string statName)
    {
        switch (statName.ToLower())
        {
            case "strength": return strength;
            case "agility": return agility;
            case "spirit": return spirit;
            case "constitution": return constitution;
            case "accuracy": return accuracy;
            case "intelligence": return intelligence;
            case "maxhealth": return maxHealth;
            case "maxmana": return maxMana;
            case "movementspeed": return movementSpeed;
            case "armor": return armor;
            case "minattack": return minAttack;
            case "maxattack": return maxAttack;
            case "attackspeed": return attackSpeed;
            case "dodgechance": return dodgeChance;
            case "hitchance": return hitChance;
            case "criticalhitchance": return criticalHitChance;
            case "criticalhitmultiplier": return criticalHitMultiplier;
            case "physicalresistance": return physicalResistance;
            case "magicdamagemultiplier": return magicDamageMultiplier;
            default:
                Debug.LogWarning($"[CharacterStats] Unknown stat: {statName}");
                return 0;
        }
    }

    [Server]
    private void SetStat(string stat, int value)
    {
        switch (stat.ToLower())
        {
            case "strength": strength = value; break;
            case "agility": agility = value; break;
            case "spirit": spirit = value; break;
            case "constitution": constitution = value; break;
            case "accuracy": accuracy = value; break;
            case "intelligence": intelligence = value; break;
            case "maxhealth":
                maxHealth = value;
                if (healthComponent != null) healthComponent.SetMaxHealth(maxHealth);
                break;
            case "maxmana": maxMana = value; currentMana = Mathf.Min(currentMana, maxMana); break;
            case "armor": armor = value; break;
            case "minattack": minAttack = value; break;
            case "maxattack": maxAttack = value; break;
            default: Debug.LogWarning($"[CharacterStats] Cannot set int for stat: {stat}"); break;
        }
    }

    [Server]
    private void SetStat(string stat, float value)
    {
        switch (stat.ToLower())
        {
            case "movementspeed":
                movementSpeed = value;
                PlayerMovement movement = GetComponent<PlayerMovement>();
                if (movement != null) movement.SetMovementSpeed(movementSpeed);
                break;
            case "attackspeed": attackSpeed = value; break;
            case "dodgechance": dodgeChance = value; break;
            case "hitchance": hitChance = value; break;
            case "criticalhitchance": criticalHitChance = value; break;
            case "criticalhitmultiplier": criticalHitMultiplier = value; break;
            case "physicalresistance": physicalResistance = value; break;
            case "magicdamagemultiplier": magicDamageMultiplier = value; break;
            default: Debug.LogWarning($"[CharacterStats] Cannot set float for stat: {stat}"); break;
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

    public void SpendMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        OnManaChangedEvent?.Invoke(currentMana, maxMana);
    }

    [Server]
    public void ApplyBuff(string stat, float mult, float rawValue, float dur, GameObject vfxPrefab = null, Vector3 vfxOffset = default)
    {
        float original = GetStatValue(stat);
        float newValue = original * (mult == 0 ? 1f : mult) + rawValue;
        if (IsFloatStat(stat))
        {
            SetStat(stat, newValue);
        }
        else
        {
            SetStat(stat, Mathf.RoundToInt(newValue));
        }
        activeStatEffects.Add(new StatEffect { Stat = stat, Value = newValue, OriginalValue = original, EndTime = Time.time + dur, IsToggle = false, VFXPrefab = vfxPrefab, VFXOffset = vfxOffset });
        StartCoroutine(RemoveBuff(stat, original, dur));
    }

    [Server]
    public void ApplyDebuff(string stat, float mult, float rawValue, float dur, GameObject vfxPrefab = null, Vector3 vfxOffset = default)
    {
        float original = GetStatValue(stat);
        float newValue = original * (mult == 0 ? 1f : mult) + rawValue;
        if (IsFloatStat(stat))
        {
            SetStat(stat, newValue);
        }
        else
        {
            SetStat(stat, Mathf.RoundToInt(newValue));
        }
        activeStatEffects.Add(new StatEffect { Stat = stat, Value = newValue, OriginalValue = original, EndTime = Time.time + dur, IsToggle = false, VFXPrefab = vfxPrefab, VFXOffset = vfxOffset });
        StartCoroutine(RemoveBuff(stat, original, dur));
    }

    [Server]
    public void ToggleBuff(string stat, float value)
    {
        if (classData == null)
        {
            Debug.LogWarning($"[CharacterStats] Cannot toggle buff for {stat}: ClassData is null");
            return;
        }
        float current = GetStatValue(stat);
        float baseValue;
        switch (stat.ToLower())
        {
            case "strength": baseValue = classData.strength; break;
            case "agility": baseValue = classData.agility; break;
            case "spirit": baseValue = classData.spirit; break;
            case "constitution": baseValue = classData.constitution; break;
            case "accuracy": baseValue = classData.accuracy; break;
            case "intelligence": baseValue = classData.intelligence; break;
            case "maxhealth": baseValue = classData.baseHealth; break;
            case "maxmana": baseValue = classData.baseMana; break;
            case "movementspeed": baseValue = classData.baseMovementSpeed; break;
            case "armor": baseValue = classData.baseDef; break;
            case "minattack": baseValue = classData.baseMinAttack; break;
            case "maxattack": baseValue = classData.baseMaxAttack; break;
            case "attackspeed": baseValue = 1.0f + (classData.agility * 0.05f * classData.agilityMultiplier); break;
            case "dodgechance": baseValue = 5.0f + (classData.agility * 0.5f * classData.agilityMultiplier); break;
            case "hitchance": baseValue = 80.0f + (classData.accuracy * 1.0f * classData.accuracyMultiplier); break;
            case "criticalhitchance": baseValue = 15.0f + (classData.agility * 0.2f * classData.agilityMultiplier); break;
            case "criticalhitmultiplier": baseValue = criticalHitMultiplier; break;
            case "physicalresistance": baseValue = classData.basePhysicalResistance; break;
            case "magicdamagemultiplier": baseValue = 1.0f + (classData.spirit * 0.05f * classData.spiritMultiplier); break;
            default:
                Debug.LogWarning($"[CharacterStats] Cannot toggle unknown stat: {stat}");
                return;
        }
        if (Mathf.Approximately(current, value))
        {
            activeStatEffects.RemoveAll(e => e.Stat == stat && e.IsToggle);
            if (IsFloatStat(stat))
            {
                SetStat(stat, baseValue);
            }
            else
            {
                SetStat(stat, Mathf.RoundToInt(baseValue));
            }
            Debug.Log($"[CharacterStats] ToggleBuff: Removed buff for {stat}, restored to {baseValue}");
        }
        else
        {
            activeStatEffects.Add(new StatEffect { Stat = stat, Value = value, OriginalValue = baseValue, EndTime = -1f, IsToggle = true });
            if (IsFloatStat(stat))
            {
                SetStat(stat, value);
            }
            else
            {
                SetStat(stat, Mathf.RoundToInt(value));
            }
            Debug.Log($"[CharacterStats] ToggleBuff: Applied buff for {stat}, set to {value}");
        }
        CalculateDerivedStats();
    }

    private IEnumerator RemoveBuff(string stat, float original, float dur)
    {
        yield return new WaitForSeconds(dur);
        activeStatEffects.RemoveAll(e => e.Stat == stat && !e.IsToggle && e.EndTime <= Time.time);
        if (IsFloatStat(stat))
        {
            SetStat(stat, original);
        }
        else
        {
            SetStat(stat, Mathf.RoundToInt(original));
        }
        CalculateDerivedStats();
    }

    private bool IsFloatStat(string stat)
    {
        switch (stat.ToLower())
        {
            case "movementspeed":
            case "attackspeed":
            case "dodgechance":
            case "hitchance":
            case "criticalhitchance":
            case "criticalhitmultiplier":
            case "physicalresistance":
            case "magicdamagemultiplier":
                return true;
            default:
                return false;
        }
    }

    [Server]
    public void ApplySlow(float slowPercentage, float duration, string source = "Unknown")
    {
        if (classData == null)
        {
            Debug.LogWarning($"[CharacterStats] Cannot apply slow: ClassData is null");
            return;
        }
        var existingEffect = activeSlowEffects.Find(e => e.Source == source);
        if (existingEffect.Percentage > 0)
        {
            activeSlowEffects.Remove(existingEffect);
        }
        SlowEffect effect = new SlowEffect
        {
            Percentage = Mathf.Clamp(slowPercentage, 0f, 0.9f),
            Duration = duration,
            SkillWeight = 0,
            EndTime = Time.time + duration,
            Source = source
        };
        activeSlowEffects.Add(effect);
        float slowMultiplier = CalculateSlowMultiplier();
        float baseMovementSpeed = classData.baseMovementSpeed * classData.agilityMultiplier;
        movementSpeed = baseMovementSpeed * slowMultiplier;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetMovementSpeed(movementSpeed);
        }
        StartCoroutine(RemoveSlow(effect));
        Debug.Log($"[CharacterStats] Applied slow from {source}: percentage={slowPercentage}, duration={duration}, total slowMultiplier={slowMultiplier}, new movementSpeed={movementSpeed}");
    }

    private float CalculateSlowMultiplier()
    {
        activeSlowEffects.RemoveAll(effect => Time.time >= effect.EndTime);
        float slowMultiplier = 1f;
        foreach (var effect in activeSlowEffects)
        {
            slowMultiplier *= Mathf.Max(0.1f, 1f - effect.Percentage);
        }
        return Mathf.Max(0.1f, slowMultiplier);
    }

    private IEnumerator RemoveSlow(SlowEffect effect)
    {
        yield return new WaitForSeconds(effect.Duration);
        activeSlowEffects.Remove(effect);
        float slowMultiplier = CalculateSlowMultiplier();
        float baseMovementSpeed = classData.baseMovementSpeed * classData.agilityMultiplier;
        movementSpeed = baseMovementSpeed * slowMultiplier;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetMovementSpeed(movementSpeed);
        }
        Debug.Log($"[CharacterStats] Slow removed from {effect.Source}: total slowMultiplier={slowMultiplier}, movementSpeed restored to {movementSpeed}");
    }

    [Server]
    public void ClearSlowEffects()
    {
        activeSlowEffects.Clear();
        float baseMovementSpeed = classData.baseMovementSpeed * classData.agilityMultiplier;
        movementSpeed = baseMovementSpeed;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetMovementSpeed(movementSpeed);
        }
        Debug.Log($"[CharacterStats] All slow effects cleared, movementSpeed restored to {movementSpeed}");
    }
}