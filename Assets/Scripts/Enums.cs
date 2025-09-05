using UnityEngine;

public enum CharacterClass
{
    Warrior,
    Mage,
    Archer,
    Monster
}

public enum DamageType
{
    Physical,
    Magic
}

public enum PlayerTeam
{
    None,
    Red,
    Blue
}

public enum MonsterRace
{
    None, Undead, Demon, Beast, Humanoid, Plant, Insect, Dragon
}

public enum MonsterElement
{
    Neutral, Fire, Water, Earth, Wind, Poison, Holy, Dark, Ghost, Undead
}

public enum PlayerAction
{
    None,
    Move,
    Attack,
    SkillCast
}

public enum ControlEffectType
{
    None = 0,
    Stun = 1,
    Silence = 2,
    FbStun = 3,
    Slow = 4,
    
}

public static class CombatConstants
{
    public const float MIN_PHYSICAL_DAMAGE = 15f;
}