using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewBuffStatSkill", menuName = "Skills/BuffStatSkill")]
public class BuffStatSkill : SkillBase
{
    [System.Serializable]
    public struct BuffEffect
    {
        [Tooltip("���� ��� �����")]
        public StatType stat;
        [Tooltip("��������� ����� (��������, 1.3 ��� ���������� �� 30%, 0 ��� �������������)")]
        public float multiplier;
        [Tooltip("������������� �������� ����� (��������, +50, 0 ��� �������������)")]
        public float rawValue;
        [Tooltip("������������ ����� � ��������")]
        public float duration;
        [Tooltip("VFX-������ ��� �����")]
        public GameObject vfxPrefab;
        [Tooltip("�������� VFX ������������ ���������")]
        public Vector3 vfxOffset;
    }

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
    [Tooltip("������ �������� �����")]
    private BuffEffect[] buffEffects = new BuffEffect[1] { new BuffEffect { stat = StatType.Agility, multiplier = 1.3f, rawValue = 0f, duration = 10f, vfxPrefab = null, vfxOffset = Vector3.up } };

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        skills.CmdExecuteSkill(caster, null, targetObject.GetComponent<NetworkIdentity>().netId, _skillName, Weight);
        // Кулдаун уже устанавливается в CmdExecuteSkill, дублировать не нужно
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        CharacterStats stats = targetObject.GetComponent<CharacterStats>();
        if (stats != null)
        {
            Debug.Log($"[BuffStatsSkill] {_skillName} applying buffs to {targetObject.name}. Current attackSpeed before: {stats.attackSpeed:F2}");
            
            foreach (var effect in buffEffects)
            {
                string statName = effect.stat.ToString().ToLower();
                Debug.Log($"[BuffStatsSkill] Applying {effect.stat} buff: multiplier={effect.multiplier:F2}, rawValue={effect.rawValue:F2}, duration={effect.duration:F2}s");
                stats.ApplyBuff(statName, effect.multiplier, effect.rawValue, effect.duration, effect.vfxPrefab, effect.vfxOffset);
            }
            
            Debug.Log($"[BuffStatsSkill] {_skillName} finished. AttackSpeed after: {stats.attackSpeed:F2}");
        }
    }
}