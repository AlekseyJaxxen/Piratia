using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewAreaOfEffectStunSkill", menuName = "Skills/AreaOfEffectStunSkill")]
public class AreaOfEffectStunSkill : SkillBase
{
    [Header("AOE Stun Skill Specifics")]
    public float stunDuration = 2f;
    public GameObject effectPrefab;
    public float aoeRadius = 5f;

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
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        int aoeLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Enemy"); // Изменено на "Enemy"
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition.Value, aoeRadius, aoeLayerMask);
        foreach (Collider col in hitColliders)
        {
            PlayerCore targetCore = col.GetComponent<PlayerCore>();
            Monster targetMonster = col.GetComponent<Monster>();
            if (targetCore != null && targetCore.team != caster.team)
            {
                targetCore.ApplyControlEffect(ControlEffectType.Stun, stunDuration, weight);
                Debug.Log($"[AreaOfEffectStunSkill] Applied stun to player {targetCore.gameObject.name}, duration={stunDuration}, weight={weight}");
            }
            else if (targetMonster != null && targetMonster.gameObject.CompareTag("Enemy"))
            {
                targetMonster.ReceiveControlEffect(ControlEffectType.Stun, stunDuration, weight);
                Debug.Log($"[AreaOfEffectStunSkill] Applied stun to monster {targetMonster.monsterName}, duration={stunDuration}, weight={weight}");
            }
        }
        caster.GetComponent<PlayerSkills>().RpcPlayAoeStun(targetPosition.Value, _skillName);
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