using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewDebuffStatSkill", menuName = "Skills/DebuffStatSkill")]
public class DebuffStatSkill : SkillBase
{
    [System.Serializable]
    public struct DebuffEffect
    {
        [Tooltip("Стат для дебаффа")]
        public StatType stat;
        [Tooltip("Множитель дебаффа (например, 0.7 для уменьшения на 30%, 0 для игнорирования)")]
        public float multiplier;
        [Tooltip("Фиксированное значение дебаффа (например, -50, 0 для игнорирования)")]
        public float rawValue;
        [Tooltip("Длительность дебаффа в секундах")]
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
    [Tooltip("Список эффектов дебаффа")]
    private DebuffEffect[] debuffEffects = new DebuffEffect[1] { new DebuffEffect { stat = StatType.Strength, multiplier = 0.7f, rawValue = 0f, duration = 10f } };

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
            foreach (var effect in debuffEffects)
            {
                string statName = effect.stat.ToString().ToLower();
                stats.ApplyDebuff(statName, effect.multiplier, effect.rawValue, effect.duration);
            }
        }
    }
}