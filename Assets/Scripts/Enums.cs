using UnityEngine;

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