using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewDebuffStatSkill", menuName = "Skills/DebuffStatSkill")]
public class DebuffStatSkill : SkillBase
{
    public string statName = "strength"; // e.g. "strength"
    public float multiplier = 0.7f; // Уменьшить на 30%
    public float duration = 10f;

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
            stats.ApplyDebuff(statName, multiplier, duration);
        }
    }
}