using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "ReviveSkill", menuName = "Skills/ReviveSkill")]
public class ReviveSkill : SkillBase
{
    [SerializeField] private GameObject reviveVFXPrefab;
    [SerializeField] private float reviveHpFraction = 0.5f;

    protected override void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null) return;
        PlayerCore targetPlayer = targetObject.GetComponentInParent<PlayerCore>();
        if (targetPlayer == null || !targetPlayer.isDead || targetPlayer.team != player.team) return;
        player.Skills.CmdExecuteSkill(player, null, targetPlayer.netId, SkillName, Weight);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (targetObject == null) return;
        PlayerCore targetPlayer = targetObject.GetComponent<PlayerCore>();
        if (targetPlayer == null || !targetPlayer.isDead || targetPlayer.team != caster.team) return;
        targetPlayer.pendingReviveHpFraction = reviveHpFraction;
        targetPlayer.RpcShowReviveRequest(caster.netId);
        caster.Skills.RpcPlayReviveVFX(targetPlayer.netId, SkillName);
    }

    public void PlayEffect(GameObject target)
    {
        if (reviveVFXPrefab != null)
        {
            Instantiate(reviveVFXPrefab, target.transform.position, Quaternion.identity);
        }
    }
}