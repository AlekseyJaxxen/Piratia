using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "ReviveSkill", menuName = "Skills/ReviveSkill")]
public class ReviveSkill : SkillBase
{
    [Header("Revive Settings")]
    [SerializeField] private GameObject reviveVFXPrefab;
    [SerializeField] private float reviveHpFraction = 0.5f;
    [SerializeField] private float reviveRange = 5f;

    protected override void ExecuteSkillImplementation(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        Debug.Log($"[ReviveSkill] ExecuteSkillImplementation - targetObject: {targetObject?.name}");
        
        if (targetObject == null)
        {
            Debug.LogWarning("[ReviveSkill] Target object is null");
            return;
        }

        // Get PlayerCore from target or its parent
        PlayerCore targetPlayer = targetObject.GetComponent<PlayerCore>();
        if (targetPlayer == null)
        {
            targetPlayer = targetObject.GetComponentInParent<PlayerCore>();
        }

        if (targetPlayer == null)
        {
            Debug.LogWarning("[ReviveSkill] No PlayerCore found on target");
            return;
        }

        Debug.Log($"[ReviveSkill] Target: {targetPlayer.name}, isDead: {targetPlayer.isDead}, team: {targetPlayer.team}, caster team: {player.team}");

        // Validate target
        if (!targetPlayer.isDead)
        {
            Debug.LogWarning("[ReviveSkill] Target is not dead");
            return;
        }

        // Revive can be used on any dead player, regardless of team
        // Removed team check to allow reviving anyone

        // Check range
        float distance = Vector3.Distance(player.transform.position, targetPlayer.transform.position);
        if (distance > reviveRange)
        {
            Debug.LogWarning($"[ReviveSkill] Target is out of range: {distance} > {reviveRange}");
            return;
        }

        Debug.Log($"[ReviveSkill] Client validation passed for {targetPlayer.name}");
        
        // The actual server execution will be handled by ExecuteOnServer
        // No need to call CmdExecuteSkill again as it would cause recursion
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        Debug.Log($"[ReviveSkill] ExecuteOnServer - targetObject: {targetObject?.name}");
        
        if (targetObject == null)
        {
            Debug.LogWarning("[ReviveSkill] Target object is null in ExecuteOnServer");
            return;
        }

        // Get PlayerCore from target
        PlayerCore targetPlayer = targetObject.GetComponent<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogWarning("[ReviveSkill] No PlayerCore found on target in ExecuteOnServer");
            return;
        }

        Debug.Log($"[ReviveSkill] Server - Target: {targetPlayer.name}, isDead: {targetPlayer.isDead}, team: {targetPlayer.team}, caster team: {caster.team}");

        // Validate target
        if (!targetPlayer.isDead)
        {
            Debug.LogWarning("[ReviveSkill] Server - Target is not dead");
            return;
        }

        // Revive can be used on any dead player, regardless of team
        // Removed team check to allow reviving anyone

        // Check range
        float distance = Vector3.Distance(caster.transform.position, targetPlayer.transform.position);
        if (distance > reviveRange)
        {
            Debug.LogWarning($"[ReviveSkill] Server - Target is out of range: {distance} > {reviveRange}");
            return;
        }

        Debug.Log($"[ReviveSkill] Server - Executing revive on {targetPlayer.name}");
        
        // Set revive parameters
        targetPlayer.pendingReviveHpFraction = reviveHpFraction;
        
        // Show revive request UI to target
        targetPlayer.RpcShowReviveRequest(caster.netId);
        
        // Play VFX
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