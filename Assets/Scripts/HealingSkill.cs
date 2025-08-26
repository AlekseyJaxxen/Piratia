using UnityEngine;
using System.Collections;
using Mirror;

public class HealingSkill : SkillBase
{
    [Header("Heal Settings")]
    public int healAmount = 20;
    public GameObject effectPrefab;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[HealingSkill] Target object is null");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[HealingSkill] Target {targetObject.name} has no NetworkIdentity");
            return;
        }

        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[HealingSkill] Not enough mana: {stats.currentMana}/{ManaCost}");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        Debug.Log($"[HealingSkill] Attempting to heal target: {targetObject.name}, netId: {targetIdentity.netId}");
        skills.CmdExecuteSkill(caster, targetPosition, targetIdentity.netId, _skillName, 0); // Некотрольный скилл, weight = 0
    }

    public void PlayEffect(GameObject target)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
}