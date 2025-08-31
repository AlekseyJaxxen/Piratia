using UnityEngine;
[CreateAssetMenu(fileName = "NewAreaOfEffectHealSkill", menuName = "Skills/AreaOfEffectHealSkill")]
public class AreaOfEffectHealSkill : SkillBase
{
    [Header("Heal Settings")]
    public int healAmount = 20;
    public GameObject effectPrefab;
    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("[AreaOfEffectHealSkill] Target position is null");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[AreaOfEffectHealSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        Debug.Log($"[AreaOfEffectHealSkill] Attempting to AOE heal at position: {targetPosition.Value}");
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, 0); // Некотрольный скилл, weight = 0
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }
    public void PlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Object.Destroy(effect, 2f);
        }
    }
}