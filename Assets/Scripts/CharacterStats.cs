using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CharacterStats : NetworkBehaviour
{
    [Header("Character Class")]
    [SerializeField] public ClassData classData;
    [SyncVar(hook = nameof(OnCharacterClassChanged))]
    public CharacterClass characterClass = CharacterClass.Warrior;
    
    [Header("Multiple Classes Support")]
    [SyncVar(hook = nameof(OnPlayerClassesChanged))]
    public List<CharacterClass> playerClasses = new List<CharacterClass>();
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
    [Header("VFX")]
    [SerializeField] private GameObject levelUpVFXPrefab;
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
    [SyncVar(hook = nameof(OnIntelligenceChanged))]
    public int intelligence = 5;
    [SyncVar(hook = nameof(OnLuckChanged))]
    public int luck = 5;
    [Header("Combat Stats")]
    [SyncVar(hook = nameof(OnMovementSpeedChanged))]
    public float movementSpeed;
    [SyncVar(hook = nameof(OnMaxHealthChanged))]
    public int maxHealth;
    [SyncVar(hook = nameof(OnMinAttackChanged))]
    public int minAttack;
    [SyncVar(hook = nameof(OnMaxAttackChanged))]
    public int maxAttack;
    [SyncVar]
    public float attackSpeed;
    [SyncVar]
    public float dodgeChance;
    [SyncVar]
    public float hitChance;
    [SyncVar(hook = nameof(OnCriticalHitChanceChanged))]
    public float criticalHitChance;
    [SyncVar]
    public float criticalHitMultiplier = 2.0f;
    [Header("Total Attributes (Base + Equipment)")]
    [SyncVar] public int totalStrength;
    [SyncVar] public int totalAgility;
    [SyncVar] public int totalSpirit;
    [SyncVar] public int totalConstitution;
    [SyncVar] public int totalAccuracy;
    
    [Header("New Attributes")]
    [SyncVar(hook = nameof(OnMaxManaChanged))]
    public int maxMana;
    [SyncVar(hook = nameof(OnArmorChanged))]
    public int armor;
    [SyncVar(hook = nameof(OnPhysicalResistanceChanged))]
    public float physicalResistance;
    [SyncVar(hook = nameof(OnAttackRangeChanged))]
    public float attackRange;
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
    public event System.Action<int, int> OnIntelligenceChangedEvent;
    public event System.Action<int, int> OnLuckChangedEvent;
    public event System.Action<int, int> OnMinAttackChangedEvent;
    public event System.Action<int, int> OnMaxAttackChangedEvent;
    public event System.Action<float, float> OnMovementSpeedChangedEvent;
    public event System.Action<int, int> OnMaxHealthChangedEvent;
    public event System.Action<int, int> OnMaxManaChangedEvent;
    public event System.Action<int, int> OnArmorChangedEvent;
    public event System.Action<float, float> OnPhysicalResistanceChangedEvent;
    public event System.Action<float, float> OnAttackRangeChangedEvent;
    public event System.Action<float, float> OnCriticalHitChanceChangedEvent;
    public event System.Action<CharacterClass, CharacterClass> OnCharacterClassChangedEvent;
    private static readonly int[] ExperiencePerLevel = new int[100];
    private bool isClassSet = false;
    private Health healthComponent;
    private Inventory inventory;
    public readonly List<SlowEffect> activeSlowEffects = new List<SlowEffect>();
    [SyncVar(hook = nameof(OnActiveStatEffectsChanged))]
    public string activeStatEffectsData = "";
    public readonly List<StatEffect> activeStatEffects = new List<StatEffect>();
    public struct SlowEffect
    {
        public float Percentage;
        public float Duration;
        public int SkillWeight;
        public float EndTime;
        public string Source;
    }
    [System.Serializable]
    public struct StatEffect
    {
        public string Stat;
        public float Value;
        public float OriginalValue;
        public float EndTime;
        public bool IsToggle;
        public string VFXPrefabName; // Имя префаба для синхронизации
        public Vector3 VFXOffset;
        public int SkillWeight;
        public bool IsActive => IsToggle || EndTime > (float)NetworkTime.time;
        
        // Для локального использования
        [System.NonSerialized]
        public GameObject VFXPrefab;
    }

    private void Awake()
    {
        healthComponent = GetComponent<Health>();
        inventory = GetComponent<Inventory>();
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
        // Character initialized on server
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    [Client]
    private void OnActiveStatEffectsChanged(string oldData, string newData)
    {
        if (string.IsNullOrEmpty(newData)) 
        {
            activeStatEffects.Clear();
            return;
        }
        
        try
        {
            // Десериализуем данные эффектов
            var effects = JsonUtility.FromJson<StatEffectList>(newData);
            activeStatEffects.Clear();
            activeStatEffects.AddRange(effects.effects);
            
            // Очищаем истекшие эффекты на клиенте
            CleanupExpiredEffects();
            
        // Принудительно обновляем VFX при изменении эффектов
        BuffVFXController vfxController = GetComponent<BuffVFXController>();
        if (vfxController != null)
        {
            vfxController.ForceUpdateVFX();
        }
        
        // Пересчитываем статы на клиенте при изменении эффектов
        if (isClient)
        {
            CalculateDerivedStats();
        }
            
            Debug.Log($"[CharacterStats] Synced {effects.effects.Length} stat effects to client on {gameObject.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterStats] Failed to deserialize stat effects: {e.Message}");
        }
    }
    
    [Client]
    private void CleanupExpiredEffects()
    {
        var expiredEffects = activeStatEffects.Where(e => !e.IsToggle && e.EndTime <= (float)NetworkTime.time).ToList();
        foreach (var effect in expiredEffects)
        {
            activeStatEffects.Remove(effect);
            Debug.Log($"[CharacterStats] Removed expired effect {effect.Stat} on {gameObject.name}");
        }
    }

    [System.Serializable]
    private class StatEffectList
    {
        public StatEffect[] effects;
    }

    [Server]
    private void SyncStatEffects()
    {
        var effectList = new StatEffectList { effects = activeStatEffects.ToArray() };
        activeStatEffectsData = JsonUtility.ToJson(effectList);
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
        luck = classData.luck;
        // ClassData loaded
    }

    private IEnumerator InitializeSkills()
    {
        PlayerSkills skills = GetComponent<PlayerSkills>();
        if (skills != null)
        {
            yield return skills.StartCoroutine("InitializeSkills");
            // Skills initialized
        }
        else
        {
            Debug.LogWarning("[CharacterStats] PlayerSkills component not found");
        }
    }

    private void OnCharacterClassChanged(CharacterClass oldClass, CharacterClass newClass)
    {
        // Class changed via SyncVar
        characterClass = newClass;
        LoadClassData();
        CalculateDerivedStats();
        StartCoroutine(InitializeSkills());
        OnCharacterClassChangedEvent?.Invoke(oldClass, newClass);
    }

    [Command]
    public void CmdSetClass(CharacterClass newClass)
    {
        // Attempting to load ClassData
        var resources = Resources.LoadAll("ClassData", typeof(ClassData));
        // Available ClassData files listed
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
        luck = classData.luck;
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

    /// <summary>
    /// Устанавливает класс игрока
    /// </summary>
    [Server]
    public void SetPlayerClass(CharacterClass newClass)
    {
        characterClass = newClass;
        playerClasses.Clear();
        playerClasses.Add(newClass);
        
        Debug.Log($"[CharacterStats] Player class set to: {characterClass}");
    }

    /// <summary>
    /// Проверяет, имеет ли игрок определенный класс
    /// </summary>
    public bool HasClass(CharacterClass classToCheck)
    {
        return playerClasses.Contains(classToCheck);
    }

    /// <summary>
    /// Добавляет новый класс игроку
    /// </summary>
    [Command]
    public void CmdAddClass(CharacterClass newClass)
    {
        if (!playerClasses.Contains(newClass))
        {
            playerClasses.Add(newClass);
            Debug.Log($"[CharacterStats] Added class {newClass} to player. Current classes: {string.Join(", ", playerClasses)}");
        }
    }

    /// <summary>
    /// Переключает активный класс игрока
    /// </summary>
    [Command]
    public void CmdSwitchClass(CharacterClass newActiveClass)
    {
        if (playerClasses.Contains(newActiveClass))
        {
            characterClass = newActiveClass;
            LoadClassData();
            CalculateDerivedStats();
            StartCoroutine(InitializeSkills());
            Debug.Log($"[CharacterStats] Switched to class {newActiveClass}");
        }
        else
        {
            Debug.LogWarning($"[CharacterStats] Cannot switch to class {newActiveClass} - player doesn't have this class");
        }
    }

    /// <summary>
    /// Обработчик изменения списка классов игрока
    /// </summary>
    private void OnPlayerClassesChanged(List<CharacterClass> oldClasses, List<CharacterClass> newClasses)
    {
        playerClasses = newClasses ?? new List<CharacterClass>();
        Debug.Log($"[CharacterStats] Player classes updated: {string.Join(", ", playerClasses)}");
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

    /// <summary>
    /// Рассчитывает бонус HP от Constitution (табличный подход)
    /// </summary>
    private int CalculateConstitutionHpBonus(int constitutionValue)
    {
        if (constitutionValue <= 0) return 0;
        
        // Табличные данные для точного расчета
        int[] conValues = { 0, 1, 2, 3, 4, 5, 6, 7, 13, 18, 19, 20, 40, 60 };
        int[] hpBonuses = { 0, 15, 30, 45, 60, 75, 90, 112, 202, 298, 313, 363, 852, 1467 };
        
        // Если значение точно в таблице
        for (int i = 0; i < conValues.Length; i++)
        {
            if (constitutionValue == conValues[i])
            {
                return hpBonuses[i];
            }
        }
        
        // Интерполяция между значениями
        for (int i = 0; i < conValues.Length - 1; i++)
        {
            if (constitutionValue > conValues[i] && constitutionValue < conValues[i + 1])
            {
                // Линейная интерполяция
                float t = (float)(constitutionValue - conValues[i]) / (conValues[i + 1] - conValues[i]);
                return Mathf.RoundToInt(Mathf.Lerp(hpBonuses[i], hpBonuses[i + 1], t));
            }
        }
        
        // Если значение больше максимального в таблице, используем экстраполяцию
        if (constitutionValue > conValues[conValues.Length - 1])
        {
            int lastIndex = conValues.Length - 1;
            int lastCon = conValues[lastIndex];
            int lastHp = hpBonuses[lastIndex];
            
            // Экстраполяция на основе последних двух точек
            int prevIndex = lastIndex - 1;
            int prevCon = conValues[prevIndex];
            int prevHp = hpBonuses[prevIndex];
            
            float slope = (float)(lastHp - prevHp) / (lastCon - prevCon);
            int extraCon = constitutionValue - lastCon;
            return lastHp + Mathf.RoundToInt(extraCon * slope);
        }
        
        return 0;
    }
    
    /// <summary>
    /// Рассчитывает прогрессивный бонус каждые 5 пунктов характеристики
    /// Пример: 5 силы = 7 урона, 10 силы = 16 урона, 100 силы = 310 урона, 1000 силы = 17500 урона
    /// </summary>
    private int CalculateProgressiveBonus(int statValue)
    {
        if (statValue <= 0) return 0;
        
        int bonus = 0;
        int remaining = statValue;
        
        while (remaining > 0)
        {
            int currentGroup = Mathf.Min(remaining, 4); // Группы по 1-4 пункта (линейный рост)
            bonus += currentGroup;
            remaining -= currentGroup;
            
            if (remaining > 0)
            {
                // Каждый 5-й пункт дает увеличенный бонус
                int multiplier = (statValue - remaining) / 5 + 1; // Множитель растет с каждым циклом
                bonus += multiplier;
                remaining--;
            }
        }
        
        return bonus;
    }
    
    /// <summary>
    /// Рассчитывает бонус HP от уровня персонажа
    /// Пример: лв1 = 140 HP, лв50 = 1365 HP, лв100 = 2615 HP
    /// </summary>
    private int CalculateLevelHpBonus()
    {
        if (level <= 1) return 0;
        
        // Формула: каждый уровень дает увеличивающийся бонус
        // Базовый рост: ~25 HP за уровень в начале, увеличивается до ~50 HP за уровень
        int totalBonus = 0;
        for (int i = 2; i <= level; i++)
        {
            int levelBonus = Mathf.RoundToInt(20 + (i - 1) * 0.5f); // 20 + (level-1) * 0.5
            totalBonus += levelBonus;
        }
        
        return totalBonus;
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
        
        // Сначала сбрасываем характеристики к базовым значениям (без бонусов экипировки)
        int baseStrength = strength;
        int baseAgility = agility;
        int baseSpirit = spirit;
        int baseConstitution = constitution;
        int baseAccuracy = accuracy;
        
        // Вычисляем бонусы от экипировки
        int equipmentStrengthBonus = 0;
        int equipmentAgilityBonus = 0;
        int equipmentSpiritBonus = 0;
        int equipmentConstitutionBonus = 0;
        int equipmentAccuracyBonus = 0;
        
        if (inventory != null)
        {
            var equippedItemInfos = inventory.GetEquippedItemInfos();
            equipmentStrengthBonus = Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Strength)));
            equipmentAgilityBonus = Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Agility)));
            equipmentSpiritBonus = Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Spirit)));
            equipmentConstitutionBonus = Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Constitution)));
            equipmentAccuracyBonus = Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Accuracy)));
        }
        
        // Вычисляем итоговые характеристики (базовые + бонусы экипировки)
        int calculatedTotalStrength = baseStrength + equipmentStrengthBonus;
        int calculatedTotalAgility = baseAgility + equipmentAgilityBonus;
        int calculatedTotalSpirit = baseSpirit + equipmentSpiritBonus;
        int calculatedTotalConstitution = baseConstitution + equipmentConstitutionBonus;
        int calculatedTotalAccuracy = baseAccuracy + equipmentAccuracyBonus;
        
        // Сохраняем итоговые характеристики для отображения в UI
        totalStrength = calculatedTotalStrength;
        totalAgility = calculatedTotalAgility;
        totalSpirit = calculatedTotalSpirit;
        totalConstitution = calculatedTotalConstitution;
        totalAccuracy = calculatedTotalAccuracy;
        
        // Вычисляем производные статы с учетом итоговых характеристик
        // НОВАЯ СИСТЕМА: Прогрессивный рост каждые 5 пунктов
        int progressiveConstitutionBonus = CalculateProgressiveBonus(calculatedTotalConstitution);
        int progressiveSpiritBonus = CalculateProgressiveBonus(calculatedTotalSpirit);
        int progressiveStrengthBonus = CalculateProgressiveBonus(calculatedTotalStrength);
        int progressiveAccuracyBonus = CalculateProgressiveBonus(calculatedTotalAccuracy);
        
        // HP с ростом уровня + бонус от Constitution (экспоненциальная прогрессия)
        int levelHpBonus = CalculateLevelHpBonus();
        int constitutionHpBonus = CalculateConstitutionHpBonus(calculatedTotalConstitution);
        maxHealth = classData.baseHealth + levelHpBonus + constitutionHpBonus;
        
        // Mana с прогрессивным бонусом от Spirit
        maxMana = classData.baseMana + Mathf.RoundToInt(progressiveSpiritBonus * classData.spiritMultiplier + intelligence * 5 * classData.intelligenceMultiplier);
        
        // Урон с прогрессивным бонусом от Strength/Accuracy
        float attackValue = classData.attackAttribute == AttackAttributeType.Strength ? progressiveStrengthBonus : progressiveAccuracyBonus;
        float attackMultiplier = classData.attackAttribute == AttackAttributeType.Strength ? classData.strengthMultiplier : classData.accuracyMultiplier;
        minAttack = Mathf.RoundToInt(classData.baseMinAttack + attackValue * 1.0f * attackMultiplier);
        maxAttack = Mathf.RoundToInt(classData.baseMaxAttack + attackValue * 1.0f * attackMultiplier);
        
        // Armor рассчитывается от Constitution
        armor = Mathf.RoundToInt(classData.baseDef + calculatedTotalConstitution * 1 * classData.constitutionMultiplier);
        Debug.Log($"[CharacterStats] Armor calculated: {armor} (base: {classData.baseDef}, constitution: {calculatedTotalConstitution}, multiplier: {classData.constitutionMultiplier})");
        // Скорость движения не зависит от ловкости, только от базового значения класса
        float baseMovementSpeed = classData.baseMovementSpeed;
        float slowMultiplier = CalculateSlowMultiplier();
        movementSpeed = baseMovementSpeed * slowMultiplier;
        // Calculate base attack speed only if no buffs are active
        if (!activeStatEffects.Any(e => e.Stat.ToLower() == "attackspeed" && e.IsActive))
        {
            attackSpeed = classData.baseAttackSpeed + (calculatedTotalAgility * 0.01f * classData.agilityMultiplier);
        }
        else
        {
            // If buffs are active, recalculate from clean baseline
            float baseAttackSpeed = classData.baseAttackSpeed + (calculatedTotalAgility * 0.01f * classData.agilityMultiplier);
            Debug.Log($"[CharacterStats] Base attackSpeed calculated: {baseAttackSpeed:F3} (base: {classData.baseAttackSpeed:F3}, agility: {calculatedTotalAgility:F1})");
        }
        dodgeChance = 10 + level * 2 + calculatedTotalAgility * 0.6f;
        hitChance = 10 + level * 2 + calculatedTotalAccuracy * 0.6f;
        criticalHitChance = 15.0f + (luck * 0.1f);
        physicalResistance = classData.basePhysicalResistance;
        attackRange = classData.baseAttackRange;
        magicDamageMultiplier = 1.0f + (calculatedTotalSpirit * 0.05f * classData.spiritMultiplier);
        
        // Добавляем прямые бонусы от экипировки
        if (inventory != null)
        {
            var equippedItemInfos = inventory.GetEquippedItemInfos();
        maxHealth += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxHP)));
        maxMana += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxMP)));
        minAttack += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.MinAttack)));
        maxAttack += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxAttack)));
            // Обратная совместимость: старый PhysicalResist влияет на оба стата
            armor += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.PhysicalResist)));
            physicalResistance += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.PhysicalResist)));
            
            // Новые отдельные статы
            armor += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Armor)));
            physicalResistance += Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.PhysicalResistance)));
            
            criticalHitChance += equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.Critical));
            movementSpeed += equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.MovementSpeed));
            attackRange += equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackRange));
            float attackSpeedBonus = equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackSpeed));
            float attackSpeedPercentBonus = equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.AttackSpeedPercent));
            
            // Only apply equipment bonus if no skill buffs are active
            if (!activeStatEffects.Any(e => e.Stat.ToLower() == "attackspeed" && e.IsActive))
            {
                // Применяем плоский бонус
                attackSpeed += attackSpeedBonus;
                
                // Применяем процентный бонус
                if (attackSpeedPercentBonus > 0)
                {
                    attackSpeed *= (1.0f + attackSpeedPercentBonus);
                }
            }
            Debug.Log($"[CharacterStats] AttackSpeed after equipment bonuses: {attackSpeed:F3} (+{attackSpeedBonus:F3} flat, +{attackSpeedPercentBonus * 100:F1}% from {equippedItemInfos.Length} items)");
            
            Debug.Log($"[CharacterStats] Equipment stats calculated: {equippedItemInfos.Length} items, Str+{equipmentStrengthBonus}, Agi+{equipmentAgilityBonus}, maxHealth bonus: {Mathf.RoundToInt(equippedItemInfos.Sum(itemInfo => itemInfo.GetTotalStatBonus(ItemInfo.StatType.MaxHP)))}");
        }
        
        
        maxHealth = Mathf.Max(1, maxHealth);
        maxMana = Mathf.Max(0, maxMana);
        minAttack = Mathf.Max(0, minAttack);
        maxAttack = Mathf.Max(minAttack, maxAttack);
        armor = Mathf.Max(0, armor);
        movementSpeed = Mathf.Max(0.1f, movementSpeed);
        criticalHitChance = Mathf.Clamp(criticalHitChance, 0f, 100f);
        physicalResistance = Mathf.Clamp(physicalResistance, 0f, 100f);
        currentMana = Mathf.Min(currentMana, maxMana);
        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(maxHealth);
        }
        
        // Обновляем UI характеристик на клиенте
        if (isClient && isLocalPlayer)
        {
            UpdateCharacterStatsUI();
        }
        PlayerMovement movementComponent = GetComponent<PlayerMovement>();
        if (movementComponent != null)
        {
            movementComponent.SetMovementSpeed(movementSpeed);
        }
        Debug.Log($"[Server] CalculateDerivedStats: class={characterClass}, strength={strength}, constitution={constitution}, minAttack={minAttack}, maxAttack={maxAttack}, maxHealth={maxHealth}, maxMana={maxMana}, armor={armor}, movementSpeed={movementSpeed}, attackSpeed={attackSpeed}, criticalHitChance={criticalHitChance}, physicalResistance={physicalResistance}");
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
            case "luck":
                luck++;
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
            case "luck": return luck;
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
            case "luck": luck = value; break;
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
            case "attackrange": attackRange = value; break;
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
            // Debug.Log($"Mana changed: {oldMana} -> {newMana}");
        }
        OnManaChangedEvent?.Invoke(oldMana, newMana);
    }

    [Client]
    public void OnLevelChanged(int oldLevel, int newLevel)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Level changed: {oldLevel} -> {newLevel}");
        }
        OnLevelChangedEvent?.Invoke(oldLevel, newLevel);
        if (newLevel > oldLevel && levelUpVFXPrefab != null)
        {
            if (isServer)
            {
                RpcSpawnLevelUpVFX();
            }
        }
    }

    [ClientRpc]
    private void RpcSpawnLevelUpVFX()
    {
        if (levelUpVFXPrefab != null)
        {
            GameObject vfx = Instantiate(levelUpVFXPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(vfx, 2f);
            // Debug.Log($"[CharacterStats] Spawned level up VFX for {gameObject.name}");
        }
    }

    [Client]
    public void OnCharacteristicPointsChanged(int oldPoints, int newPoints)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Characteristic Points changed: {oldPoints} -> {newPoints}");
        }
        OnCharacteristicPointsChangedEvent?.Invoke(oldPoints, newPoints);
    }

    [Client]
    public void OnStrengthChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Strength changed: {oldValue} -> {newValue}");
        }
        OnStrengthChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnAgilityChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Agility changed: {oldValue} -> {newValue}");
        }
        OnAgilityChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnSpiritChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Spirit changed: {oldValue} -> {newValue}");
        }
        OnSpiritChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnConstitutionChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Constitution changed: {oldValue} -> {newValue}");
        }
        OnConstitutionChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnAccuracyChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Accuracy changed: {oldValue} -> {newValue}");
        }
        OnAccuracyChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnIntelligenceChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Intelligence changed: {oldValue} -> {newValue}");
        }
        OnIntelligenceChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnLuckChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Luck changed: {oldValue} -> {newValue}");
        }
        OnLuckChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnMovementSpeedChanged(float oldValue, float newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"MovementSpeed changed: {oldValue} -> {newValue}");
        }
        OnMovementSpeedChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnMaxHealthChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"MaxHealth changed: {oldValue} -> {newValue}");
        }
        OnMaxHealthChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnMaxManaChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"MaxMANA changed: {oldValue} -> {newValue}");
        }
        OnMaxManaChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnArmorChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"Armor changed: {oldValue} -> {newValue}");
        }
        OnArmorChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnPhysicalResistanceChanged(float oldValue, float newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"PhysicalResistance changed: {oldValue} -> {newValue}");
        }
        OnPhysicalResistanceChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnAttackRangeChanged(float oldValue, float newValue)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"[CharacterStats] AttackRange changed: {oldValue} -> {newValue}");
        }
        OnAttackRangeChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    public void OnCriticalHitChanceChanged(float oldValue, float newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"CriticalHitChance changed: {oldValue} -> {newValue}");
        }
        OnCriticalHitChanceChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    private void OnMinAttackChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"[Client] minAttack changed: {oldValue} -> {newValue}");
        }
        OnMinAttackChangedEvent?.Invoke(oldValue, newValue);
    }

    [Client]
    private void OnMaxAttackChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            // Debug.Log($"[Client] maxAttack changed: {oldValue} -> {newValue}");
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
    public void ApplyBuff(string stat, float mult, float rawValue, float dur, GameObject vfxPrefab = null, Vector3 vfxOffset = default, int skillWeight = 0)
    {
        StatEffect existingEffect = activeStatEffects.FirstOrDefault(e => e.Stat == stat && !e.IsToggle);
        if (existingEffect.IsActive)
        {
            Debug.Log($"[CharacterStats] Buff for {stat} not applied: an active buff already exists");
            return;
        }
        float original = GetStatValue(stat);
        float newValue = original * (mult == 0 ? 1f : mult) + rawValue;
        
        // Подробное логирование для отладки скорости атаки
        if (stat.ToLower() == "attackspeed")
        {
            Debug.Log($"[CharacterStats] APPLYING ATTACK SPEED BUFF:");
            Debug.Log($"  - Stat: {stat}");
            Debug.Log($"  - Original value: {original:F3}");
            Debug.Log($"  - Multiplier: {mult:F3} (0 means ignore)");
            Debug.Log($"  - Raw value: {rawValue:F3}");
            Debug.Log($"  - Formula: {original:F3} * {(mult == 0 ? 1f : mult)} + {rawValue:F3} = {newValue:F3}");
            Debug.Log($"  - Duration: {dur:F2}s");
            Debug.Log($"  - Current attackSpeed from CharacterStats: {attackSpeed:F3}");
        }
        
        if (IsFloatStat(stat))
        {
            SetStat(stat, newValue);
        }
        else
        {
            SetStat(stat, Mathf.RoundToInt(newValue));
        }
        activeStatEffects.Add(new StatEffect
        {
            Stat = stat,
            Value = newValue,
            OriginalValue = original,
            EndTime = (float)NetworkTime.time + dur,
            IsToggle = false,
            VFXPrefab = vfxPrefab,
            VFXPrefabName = vfxPrefab != null ? vfxPrefab.name : "",
            VFXOffset = vfxOffset,
            SkillWeight = skillWeight
        });
        SyncStatEffects(); // Синхронизируем с клиентами
        StartCoroutine(RemoveBuff(stat, original, dur));
        Debug.Log($"[CharacterStats] Applied buff for {stat}, value={newValue}, duration={dur}, weight={skillWeight}");
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
        activeStatEffects.Add(new StatEffect { 
            Stat = stat, 
            Value = newValue, 
            OriginalValue = original, 
            EndTime = (float)NetworkTime.time + dur, 
            IsToggle = false, 
            VFXPrefab = vfxPrefab, 
            VFXPrefabName = vfxPrefab != null ? vfxPrefab.name : "",
            VFXOffset = vfxOffset 
        });
        SyncStatEffects(); // Синхронизируем с клиентами
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
            case "luck": baseValue = classData.luck; break;
            case "maxhealth": baseValue = classData.baseHealth; break;
            case "maxmana": baseValue = classData.baseMana; break;
            case "movementspeed": baseValue = classData.baseMovementSpeed; break;
            case "armor": baseValue = classData.baseDef; break;
            case "minattack": baseValue = classData.baseMinAttack; break;
            case "maxattack": baseValue = classData.baseMaxAttack; break;
            case "attackspeed": baseValue = GetCleanBaseAttackSpeed(); break;
            case "dodgechance": baseValue = 10 + level * 2 + classData.agility * 0.6f; break;
            case "hitchance": baseValue = 10 + level * 2 + classData.accuracy * 0.6f; break;
            case "criticalhitchance": baseValue = 15.0f + (classData.luck * 0.1f); break;
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
            activeStatEffects.Add(new StatEffect { 
                Stat = stat, 
                Value = value, 
                OriginalValue = baseValue, 
                EndTime = -1f, 
                IsToggle = true,
                VFXPrefabName = ""
            });
            if (IsFloatStat(stat))
            {
                SetStat(stat, value);
            }
            else
            {
                SetStat(stat, Mathf.RoundToInt(value));
            }
            Debug.Log($"[CharacterStats] ToggleBuff: Applied buff for {stat}, set to {value} (baseValue was {baseValue})");
        }
        SyncStatEffects(); // Синхронизируем с клиентами
        CalculateDerivedStats();
    }

    private IEnumerator RemoveBuff(string stat, float original, float dur)
    {
        yield return new WaitForSeconds(dur);
        activeStatEffects.RemoveAll(e => e.Stat == stat && !e.IsToggle && e.EndTime <= (float)NetworkTime.time);
        if (IsFloatStat(stat))
        {
            SetStat(stat, original);
        }
        else
        {
            SetStat(stat, Mathf.RoundToInt(original));
        }
        SyncStatEffects(); // Синхронизируем с клиентами
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
        float baseMovementSpeed = classData.baseMovementSpeed;
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
        float baseMovementSpeed = classData.baseMovementSpeed;
        movementSpeed = baseMovementSpeed * slowMultiplier;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetMovementSpeed(movementSpeed);
        }
        Debug.Log($"[CharacterStats] Slow removed from {effect.Source}: total slowMultiplier={slowMultiplier}, movementSpeed restored to {movementSpeed}");
    }

    [Client]
    private void UpdateCharacterStatsUI()
    {
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.UpdateAttributesPanel();
        }
    }

    [Server]
    public void ClearSlowEffects()
    {
        activeSlowEffects.Clear();
        float baseMovementSpeed = classData.baseMovementSpeed;
        movementSpeed = baseMovementSpeed;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetMovementSpeed(movementSpeed);
        }
        Debug.Log($"[CharacterStats] All slow effects cleared, movementSpeed restored to {movementSpeed}");
    }
    
    /// <summary>
    /// Получить чистую базовую скорость атаки без эффектов и предметов
    /// </summary>
    private float GetCleanBaseAttackSpeed()
    {
        if (classData == null) return 1.0f;
        
        // Рассчитываем базовую скорость атаки без эффектов и предметов
        var originalActiveStatEffects = activeStatEffects.ToList(); // Сохраняем текущие эффекты
        var originalInventory = inventory; // Сохраняем ссылку на инвентарь
        
        activeStatEffects.Clear(); // Убираем все буффы
        inventory = null; // Убираем инвентарь временно
        
        float cleanSpeed = classData.baseAttackSpeed + (classData.agility * 0.01f * classData.agilityMultiplier);
        
        // Восстанавливаем состояние
        activeStatEffects.AddRange(originalActiveStatEffects);
        inventory = originalInventory;
        
        return cleanSpeed;
    }
    
    /// <summary>
    /// Тестовая функция для проверки правильности расчетов прогрессивного бонуса
    /// </summary>
    [ContextMenu("Test Progressive Bonus")]
    public void TestProgressiveBonus()
    {
        Debug.Log("=== ТЕСТ ПРОГРЕССИВНОГО БОНУСА ===");
        Debug.Log($"5 силы = {CalculateProgressiveBonus(5)} урона (ожидается 7)");
        Debug.Log($"6 силы = {CalculateProgressiveBonus(6)} урона (ожидается 9)");
        Debug.Log($"7 силы = {CalculateProgressiveBonus(7)} урона (ожидается 10)");
        Debug.Log($"8 силы = {CalculateProgressiveBonus(8)} урона (ожидается 12)");
        Debug.Log($"9 силы = {CalculateProgressiveBonus(9)} урона (ожидается 13)");
        Debug.Log($"10 силы = {CalculateProgressiveBonus(10)} урона (ожидается 16)");
        Debug.Log($"100 силы = {CalculateProgressiveBonus(100)} урона (ожидается 310)");
        Debug.Log($"1000 силы = {CalculateProgressiveBonus(1000)} урона (ожидается 17500)");
        
        Debug.Log("=== ТЕСТ РОСТА HP С УРОВНЕМ ===");
        Debug.Log($"Лв1 HP бонус = {CalculateLevelHpBonus()} (ожидается 0)");
        // Симулируем уровень 50
        int tempLevel = level;
        level = 50;
        Debug.Log($"Лв50 HP бонус = {CalculateLevelHpBonus()} (ожидается ~1225)");
        level = tempLevel;
    }
    
    /// <summary>
    /// Рассчитывает урон для конкретного значения силы
    /// </summary>
    [ContextMenu("Calculate 76 Strength Damage")]
    public void Calculate76StrengthDamage()
    {
        int strengthValue = 76;
        int progressiveBonus = CalculateProgressiveBonus(strengthValue);
        
        Debug.Log($"=== РАСЧЕТ УРОНА ДЛЯ {strengthValue} СИЛЫ ===");
        Debug.Log($"Прогрессивный бонус от {strengthValue} силы = {progressiveBonus}");
        
        // Предполагаем базовые значения класса (можно настроить)
        int baseMinAttack = 10; // Примерное базовое значение
        int baseMaxAttack = 15;  // Примерное базовое значение
        float strengthMultiplier = 1.0f; // Множитель класса
        
        int minAttack = Mathf.RoundToInt(baseMinAttack + progressiveBonus * 1.0f * strengthMultiplier);
        int maxAttack = Mathf.RoundToInt(baseMaxAttack + progressiveBonus * 1.0f * strengthMultiplier);
        
        Debug.Log($"Базовый min урон: {baseMinAttack}");
        Debug.Log($"Базовый max урон: {baseMaxAttack}");
        Debug.Log($"Итоговый min урон: {minAttack}");
        Debug.Log($"Итоговый max урон: {maxAttack}");
        Debug.Log($"Диапазон урона: {minAttack}-{maxAttack}");
    }
    
    /// <summary>
    /// Рассчитывает урон для Archer с 1000 силы
    /// </summary>
    [ContextMenu("Calculate Archer 1000 Strength Damage")]
    public void CalculateArcher1000StrengthDamage()
    {
        Debug.Log($"=== ПРАВИЛЬНЫЙ РАСЧЕТ УРОНА ДЛЯ ARCHER ===");
        Debug.Log($"Archer использует Accuracy для атаки, НЕ Strength!");
        
        // Сценарий 1: Archer с 1000 силы, но базовый Accuracy (5)
        int strengthValue = 1000;
        int accuracyValue = 5; // Базовый Accuracy для Archer
        
        int strengthProgressiveBonus = CalculateProgressiveBonus(strengthValue);
        int accuracyProgressiveBonus = CalculateProgressiveBonus(accuracyValue);
        
        Debug.Log($"=== СЦЕНАРИЙ 1: ARCHER С 1000 СИЛЫ, 5 ACCURACY ===");
        Debug.Log($"Strength прогрессивный бонус: {strengthProgressiveBonus} (НЕ ИСПОЛЬЗУЕТСЯ)");
        Debug.Log($"Accuracy прогрессивный бонус: {accuracyProgressiveBonus}");
        
        int baseMinAttack = 15;
        int baseMaxAttack = 20;
        float accuracyMultiplier = 1.0f;
        
        int minAttack = Mathf.RoundToInt(baseMinAttack + accuracyProgressiveBonus * 1.0f * accuracyMultiplier);
        int maxAttack = Mathf.RoundToInt(baseMaxAttack + accuracyProgressiveBonus * 1.0f * accuracyMultiplier);
        
        Debug.Log($"Урон Archer с 1000 силы, 5 Accuracy: {minAttack}-{maxAttack}");
        
        // Сценарий 2: Archer с 1000 Accuracy
        int accuracyValue1000 = 1000;
        int accuracyProgressiveBonus1000 = CalculateProgressiveBonus(accuracyValue1000);
        
        int minAttack1000 = Mathf.RoundToInt(baseMinAttack + accuracyProgressiveBonus1000 * 1.0f * accuracyMultiplier);
        int maxAttack1000 = Mathf.RoundToInt(baseMaxAttack + accuracyProgressiveBonus1000 * 1.0f * accuracyMultiplier);
        
        Debug.Log($"=== СЦЕНАРИЙ 2: ARCHER С 1000 ACCURACY ===");
        Debug.Log($"Урон Archer с 1000 Accuracy: {minAttack1000}-{maxAttack1000}");
        
        Debug.Log($"=== ВЫВОД ===");
        Debug.Log($"Archer с 1000 силы имеет низкий урон, потому что использует Accuracy для атаки!");
        Debug.Log($"Чтобы получить высокий урон, нужно повышать Accuracy, а не Strength.");
    }
    
    /// <summary>
    /// Рассчитывает характеристики для Warrior с 100 силы и 50 Constitution
    /// </summary>
    [ContextMenu("Calculate Warrior 100 Str 50 Con")]
    public void CalculateWarrior100Str50Con()
    {
        Debug.Log($"=== РАСЧЕТ ХАРАКТЕРИСТИК ДЛЯ WARRIOR ===");
        Debug.Log($"Warrior: 100 Strength, 50 Constitution");
        
        // Рассчитываем прогрессивные бонусы
        int strengthValue = 100;
        int constitutionValue = 50;
        
        int strengthProgressiveBonus = CalculateProgressiveBonus(strengthValue);
        int constitutionProgressiveBonus = CalculateProgressiveBonus(constitutionValue);
        
        Debug.Log($"=== ПРОГРЕССИВНЫЕ БОНУСЫ ===");
        Debug.Log($"Strength прогрессивный бонус от {strengthValue}: {strengthProgressiveBonus}");
        Debug.Log($"Constitution прогрессивный бонус от {constitutionValue}: {constitutionProgressiveBonus}");
        
        // Базовые значения Warrior (предполагаемые)
        int baseMinAttack = 15;
        int baseMaxAttack = 20;
        int baseHealth = 1000;
        float strengthMultiplier = 1.0f;
        float constitutionMultiplier = 1.0f;
        
        // Warrior использует Strength для атаки
        int minAttack = Mathf.RoundToInt(baseMinAttack + strengthProgressiveBonus * 1.0f * strengthMultiplier);
        int maxAttack = Mathf.RoundToInt(baseMaxAttack + strengthProgressiveBonus * 1.0f * strengthMultiplier);
        
        // HP с ростом уровня + бонус от Constitution (экспоненциальная прогрессия)
        int levelHpBonus = CalculateLevelHpBonus();
        int constitutionHpBonus = CalculateConstitutionHpBonus(constitutionValue);
        int maxHealth = baseHealth + levelHpBonus + constitutionHpBonus;
        
        Debug.Log($"=== ИТОГОВЫЕ ХАРАКТЕРИСТИКИ WARRIOR ===");
        Debug.Log($"Базовый min урон: {baseMinAttack}");
        Debug.Log($"Базовый max урон: {baseMaxAttack}");
        Debug.Log($"Итоговый min урон: {minAttack}");
        Debug.Log($"Итоговый max урон: {maxAttack}");
        Debug.Log($"Диапазон урона: {minAttack}-{maxAttack}");
        
        Debug.Log($"=== HP РАСЧЕТ ===");
        Debug.Log($"Базовый HP: {baseHealth}");
        Debug.Log($"Бонус HP от уровня: {levelHpBonus}");
        Debug.Log($"Бонус HP от Constitution (экспоненциальный): {constitutionHpBonus}");
        Debug.Log($"Итоговый HP: {maxHealth}");
        
        Debug.Log($"=== СРАВНЕНИЕ С ПРИМЕРАМИ ===");
        Debug.Log($"100 силы должно давать ~310 урона (как в примере)");
        Debug.Log($"50 Constitution должно давать значительный бонус к HP");
        
        Debug.Log($"=== ПРОВЕРКА НОВОЙ ФОРМУЛЫ CONSTITUTION ===");
        Debug.Log($"Новая формула: 50 Constitution = {constitutionHpBonus} HP");
        Debug.Log($"Ожидается: 50 Constitution = ~1273 HP (1338 - 65)");
        Debug.Log($"Разница: {1273 - constitutionHpBonus} HP");
    }
    
    /// <summary>
    /// Тестирует точную формулу Constitution
    /// </summary>
    [ContextMenu("Test Constitution Formula")]
    public void TestConstitutionFormula()
    {
        Debug.Log($"=== ТЕСТ ТОЧНОЙ ФОРМУЛЫ CONSTITUTION ===");
        
        // Точные данные из примера
        int[] testValues = { 0, 1, 2, 3, 4, 5, 6, 7, 13, 18, 19, 20, 40, 60 };
        int[] expectedHp = { 65, 80, 95, 110, 125, 140, 155, 177, 267, 363, 378, 428, 917, 1532 };
        
        for (int i = 0; i < testValues.Length; i++)
        {
            int constitutionValue = testValues[i];
            int calculatedHp = 65 + CalculateConstitutionHpBonus(constitutionValue); // Base HP = 65
            int expectedHpTotal = expectedHp[i];
            int difference = calculatedHp - expectedHpTotal;
            
            Debug.Log($"Constitution {constitutionValue}: рассчитано {calculatedHp}, ожидается {expectedHpTotal}, разница {difference}");
        }
    }
}