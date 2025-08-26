using UnityEngine;
using System.Collections;
using Mirror;

public class TargetedStunSkill : SkillBase
{
    [Header("Stun Skill Specifics")]
    public float stunDuration = 2f;
    public GameObject effectPrefab;
    private Coroutine _effectCoroutine;

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[TargetedStunSkill] Target object is null for skill {_skillName}");
            return;
        }

        NetworkIdentity targetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetIdentity == null)
        {
            Debug.LogWarning($"[TargetedStunSkill] Target {targetObject.name} has no NetworkIdentity for skill {_skillName}");
            return;
        }

        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(ManaCost))
        {
            Debug.LogWarning($"[TargetedStunSkill] Not enough mana for skill {_skillName}: {stats.currentMana}/{ManaCost}");
            return;
        }

        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        if (skills == null)
        {
            Debug.LogWarning($"[TargetedStunSkill] PlayerSkills component missing on caster for skill {_skillName}");
            return;
        }

        Debug.Log($"[TargetedStunSkill] Client requesting stun for skill {_skillName} on target: {targetObject.name}, netId: {targetIdentity.netId}");
        skills.CmdExecuteSkill(caster, null, targetIdentity.netId, _skillName);
    }

    public void PlayEffect(GameObject target)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
            _effectCoroutine = StartCoroutine(DestroyEffectAfterDelay(effect, 2f));
        }
    }

    private IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            Destroy(effect);
        }
        _effectCoroutine = null;
    }

    private void OnDisable()
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }
    }
}