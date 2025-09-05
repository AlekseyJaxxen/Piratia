using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewTargetedStunSkill", menuName = "Skills/TargetedStunSkill")]
public class TargetedStunSkill : SkillBase
{
    [Header("Stun Skill Specifics")]
    public int baseDamage = 0; // Если нужно добавить урон, иначе 0
    public float damageMultiplier = 1f;
    public float stunDuration = 2f;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[TargetedStunSkill] Target object is null");
            return;
        }
        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        Monster targetMonster = targetObject.GetComponent<Monster>();
        if ((targetCore == null || targetCore.team == caster.team) && targetMonster == null)
        {
            Debug.LogWarning("[TargetedStunSkill] Invalid target: not enemy");
            return;
        }
        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[TargetedStunSkill] Target {targetObject.name} has no NetworkIdentity");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[TargetedStunSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        Debug.Log($"[TargetedStunSkill] Attempting to stun target: {targetObject.name}, weight: {Weight}");
        skills.CmdExecuteSkill(caster, null, targetIdentity.netId, _skillName, Weight);
        caster.GetComponent<PlayerSkills>().StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (targetObject == null) return;
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats == null) return;

        int finalDamage = 0;
        if (baseDamage > 0)
        {
            if (SkillDamageType == DamageType.Physical)
            {
                int randomAttack = Random.Range(stats.minAttack, stats.maxAttack + 1);
                finalDamage = Mathf.RoundToInt((baseDamage + randomAttack) * damageMultiplier);
            }
            else
            {
                finalDamage = baseDamage + Mathf.RoundToInt(stats.spirit * damageMultiplier);
            }
        }

        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null && finalDamage > 0)
        {
            targetHealth.TakeDamage(finalDamage, SkillDamageType, false, caster.netIdentity);
        }

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