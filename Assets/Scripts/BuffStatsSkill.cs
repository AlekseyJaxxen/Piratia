using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewBuffStatSkill", menuName = "Skills/BuffStatSkill")]
public class BuffStatSkill : SkillBase
{
    public string statName = "agility";
    public float multiplier = 1.3f; // Увеличить на 30%
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
            stats.ApplyBuff(statName, multiplier, duration);
        }
    }
}