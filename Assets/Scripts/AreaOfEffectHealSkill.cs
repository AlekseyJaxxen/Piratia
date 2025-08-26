using UnityEngine;
using System.Collections;

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

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        Debug.Log($"[AreaOfEffectHealSkill] Attempting to AOE heal at position: {targetPosition.Value}");
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName);
    }

    public void PlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
}