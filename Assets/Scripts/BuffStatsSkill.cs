using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewBuffStatSkill", menuName = "Skills/BuffStatSkill")]
public class BuffStatSkill : SkillBase
{
    [System.Serializable]
    public struct BuffEffect
    {
        [Tooltip("Стат для баффа")]
        public StatType stat;
        [Tooltip("Множитель баффа (например, 1.3 для увеличения на 30%, 0 для игнорирования)")]
        public float multiplier;
        [Tooltip("Фиксированное значение баффа (например, +50, 0 для игнорирования)")]
        public float rawValue;
        [Tooltip("Длительность баффа в секундах")]
        public float duration;
    }

    // Enum для всех статов, включая основные и производные
    public enum StatType
    {
        Strength,
        Agility,
        Spirit,
        Constitution,
        Accuracy,
        Intelligence,
        MaxHealth,
        MaxMana,
        MovementSpeed,
        Armor,
        MinAttack,
        MaxAttack,
        AttackSpeed,
        DodgeChance,
        HitChance,
        CriticalHitChance,
        CriticalHitMultiplier,
        PhysicalResistance,
        MagicDamageMultiplier
    }

    [SerializeField]
    [Tooltip("Список эффектов баффа")]
    private BuffEffect[] buffEffects = new BuffEffect[1] { new BuffEffect { stat = StatType.Agility, multiplier = 1.3f, rawValue = 0f, duration = 10f } };

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        skills.CmdExecuteSkill(caster, null, targetObject.GetComponent<NetworkIdentity>().netId, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        CharacterStats stats = targetObject.GetComponent<CharacterStats>();
        if (stats != null)
        {
            foreach (var effect in buffEffects)
            {
                string statName = effect.stat.ToString().ToLower();
                stats.ApplyBuff(statName, effect.multiplier, effect.rawValue, effect.duration);
            }
        }
    }
}