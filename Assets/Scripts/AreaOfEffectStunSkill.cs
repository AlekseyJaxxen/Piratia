using UnityEngine;

[CreateAssetMenu(fileName = "NewAreaOfEffectStunSkill", menuName = "Skills/AreaOfEffectStunSkill")]
public class AreaOfEffectStunSkill : SkillBase
{
    [Header("AOE Stun Skill Specifics")]
    public float stunDuration = 2f;
    public GameObject effectPrefab;
    public float aoeRadius = 5f; // Радиус действия

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("[AreaOfEffectStunSkill] Target position is null");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[AreaOfEffectStunSkill] PlayerSkills component missing on caster");
            return;
        }

        Debug.Log($"[AreaOfEffectStunSkill] Attempting to AOE stun at position: {targetPosition.Value}, weight: {Weight}");

        // Находим монстров в радиусе
        Collider[] colliders = Physics.OverlapSphere(targetPosition.Value, aoeRadius, LayerMask.GetMask("Monster"));
        foreach (Collider col in colliders)
        {
            Monster monster = col.GetComponent<Monster>();
            if (monster != null && monster != this)
            {
                // Отправляем серверную команду для каждого монстра
                skills.CmdApplyAreaEffect(monster.netId, ControlEffectType.Stun, stunDuration, Weight);
            }
        }

        // Выполняем общую серверную команду для скилла
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
        PlayEffect(targetPosition.Value);
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