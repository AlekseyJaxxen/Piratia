/// <summary>
/// Универсальные ID анимаций для всех монстров
/// Эта система обеспечивает единообразие анимаций независимо от их названий
/// </summary>
public enum UniversalAnimationId
{
    Attack = 0,     // ID 0 - всегда атака (любое название: "Attack", "Mushroom_attack", "Bite", etc.)
    Idle = 1,       // ID 1 - всегда покой (любое название: "Idle", "Rest", "Stand", etc.)
    Move = 2,       // ID 2 - всегда движение (любое название: "Walk", "Move", "Run", etc.)
    Death = 3,      // ID 3 - всегда смерть (любое название: "Death", "Die", "Destroy", etc.)
    Hit = 4,        // ID 4 - всегда получение урона (любое название: "Hit", "Hurt", "Damage", etc.)
    Spawn = 5       // ID 5 - всегда появление (любое название: "Spawn", "Appear", "Birth", etc.)
}

/// <summary>
/// Утилиты для работы с универсальными ID анимаций
/// </summary>
public static class UniversalAnimationUtils
{
    /// <summary>
    /// Получает описание универсального ID анимации
    /// </summary>
    public static string GetDescription(UniversalAnimationId id)
    {
        switch (id)
        {
            case UniversalAnimationId.Attack: return "Атака";
            case UniversalAnimationId.Idle: return "Покой";
            case UniversalAnimationId.Move: return "Движение";
            case UniversalAnimationId.Death: return "Смерть";
            case UniversalAnimationId.Hit: return "Получение урона";
            case UniversalAnimationId.Spawn: return "Появление";
            default: return "Неизвестная анимация";
        }
    }
    
    /// <summary>
    /// Получает стандартные названия для универсального ID
    /// </summary>
    public static string[] GetStandardNames(UniversalAnimationId id)
    {
        switch (id)
        {
            case UniversalAnimationId.Attack: 
                return new[] { "Attack", "attack", "Bite", "Strike", "Hit" };
            case UniversalAnimationId.Idle: 
                return new[] { "Idle", "idle", "Rest", "Stand", "Wait" };
            case UniversalAnimationId.Move: 
                return new[] { "Move", "move", "Walk", "walk", "Run", "run" };
            case UniversalAnimationId.Death: 
                return new[] { "Death", "death", "Die", "die", "Destroy" };
            case UniversalAnimationId.Hit: 
                return new[] { "Hit", "hit", "Hurt", "hurt", "Damage", "damage" };
            case UniversalAnimationId.Spawn: 
                return new[] { "Spawn", "spawn", "Appear", "appear", "Birth", "birth" };
            default: 
                return new string[0];
        }
    }
}
