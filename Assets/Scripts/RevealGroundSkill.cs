using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewRevealGroundSkill", menuName = "Skills/RevealGroundSkill")]
public class RevealGroundSkill : SkillBase
{
    [Header("Reveal Ground Skill Specifics")]
    public float duration = 10f;
    public float aoeRadius = 5f;
    public GameObject revealEffectPrefab; // Префаб зоны с NetworkBehaviour

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue) return;
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        GameObject revealEffect = Instantiate(revealEffectPrefab, targetPosition.Value, Quaternion.identity);
        NetworkServer.Spawn(revealEffect);
        // Передаем маску слоев, включающую "Player" и "Ignore Raycast"
        int aoeLayerMask = LayerMask.GetMask("Player", "Ignore Raycast");
        revealEffect.GetComponent<RevealGroundEffect>().Init(duration, aoeRadius, caster.team, aoeLayerMask);
    }
}