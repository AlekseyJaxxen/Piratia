using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewTargetedStunSkill", menuName = "Skills/TargetedStunSkill")]
public class TargetedStunSkill : SkillBase
{
    [Header("Stun Skill Specifics")]
    public float stunDuration = 2f;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[TargetedStunSkill] Target object is null");
            return;
        }

        if (Vector3.Distance(caster.transform.position, targetObject.transform.position) > Range)
        {
            Debug.LogWarning("[TargetedStunSkill] Target out of range");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning("[TargetedStunSkill] PlayerSkills component missing on caster");
            return;
        }

        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        Debug.Log($"[TargetedStunSkill] Attempting to stun target: {targetObject.name}, weight: {Weight}");
        skills.CmdExecuteSkill(caster, null, targetNetId, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (targetObject == null) return;

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();

        if (targetCore != null && targetCore.team != caster.team)
        {
            targetCore.ApplyControlEffect(ControlEffectType.Stun, stunDuration, weight);
        }
        else if (targetMonster != null)
        {
            targetMonster.ReceiveControlEffect(ControlEffectType.Stun, stunDuration, weight);
        }

        caster.GetComponent<PlayerSkills>().RpcPlayTargetedStun(targetObject.GetComponent<NetworkIdentity>().netId, _skillName);
    }

    public void PlayEffect(GameObject target, PlayerSkills playerSkills)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
            playerSkills.StartCoroutine(DestroyEffectAfterDelay(effect, 2f));
        }
    }

    private IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            Object.Destroy(effect);
        }
    }
}