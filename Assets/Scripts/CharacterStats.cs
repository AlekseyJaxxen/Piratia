using UnityEngine;
using Mirror;

public enum CharacterClass
{
    Warrior,
    Mage,
    Archer
}

public enum DamageType
{
    Physical,
    Magic
}

public static class CombatConstants
{
    public const float MIN_PHYSICAL_DAMAGE = 15f;
}

public class CharacterStats : NetworkBehaviour
{
    [Header("Character Class")]
    [SyncVar]
    public CharacterClass characterClass = CharacterClass.Warrior;

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

    public override void OnStartServer()
    {
        base.OnStartServer();
        CalculateDerivedStats();
        currentMana = maxMana;
    }

    [Server]
    public void CalculateDerivedStats()
    {
        maxHealth = 1000 + (constitution * 10);
        GetComponent<Health>().MaxHealth = maxHealth;
        GetComponent<Health>().SetHealth(maxHealth);

        movementSpeed = 8f + (agility * 0.1f);
        GetComponent<PlayerMovement>().moveSpeed = movementSpeed;

        dodgeChance = 5.0f + (agility * 0.5f);
        hitChance = 80.0f + (accuracy * 1.0f);
        criticalHitChance = 15.0f + (agility * 0.2f);

        maxMana = 100 + (intelligence * 10);
        armor = constitution * 2;
        physicalResistance = Mathf.Min(constitution * 0.5f, 80f);

        currentMana = Mathf.Min(currentMana, maxMana);

        switch (characterClass)
        {
            case CharacterClass.Warrior:
                minAttack = 5 + (strength * 2);
                maxAttack = 5 + (strength * 3);
                break;
            case CharacterClass.Mage:
                minAttack = 5 + (spirit * 2);
                maxAttack = 5 + (spirit * 3);
                break;
            case CharacterClass.Archer:
                minAttack = 5 + (accuracy * 2);
                maxAttack = 5 + (accuracy * 3);
                break;
        }
    }

    // Клиентская проверка для UI
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

    private void OnManaChanged(int oldMana, int newMana)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Mana changed: {oldMana} -> {newMana}");
        }
    }

    [Server]
    public bool TryCriticalHit()
    {
        float randomValue = Random.Range(0f, 100f);
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