using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewToggleSelfBuffSkill", menuName = "Skills/ToggleSelfBuffSkill")]
public class ToggleSelfBuffSkill : SkillBase
{
    public string buffType = "magicShield"; // e.g. "magicShield" or "invisibility"
    public float damageReduction = 0.3f; // Для shield

    private bool isActive;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        isActive = !isActive;
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        skills.CmdExecuteSkill(caster, null, 0, _skillName, Weight); // Передача состояния
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.ToggleBuff(buffType, damageReduction); // Toggle on/off
        }
    }
}