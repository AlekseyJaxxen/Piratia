using UnityEngine;
using Mirror;
using System.Collections;

[CreateAssetMenu(fileName = "NewAoeDebuffGroundSkill", menuName = "Skills/AoeDebuffGroundSkill")]
public class AoeDebuffGroundSkill : SkillBase
{
    public float slowPercentage = 0.3f;
    public float duration = 10f;
    public float aoeRadius = 5f;
    public GameObject groundEffectPrefab; // Префаб с NetworkBehaviour для эффекта

    protected override void ExecuteSkillImplementation(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue) return;
        PlayerSkills skills = caster.GetComponent<PlayerSkills>();
        skills.CmdExecuteSkill(caster, targetPosition, 0, _skillName, Weight);
        skills.StartLocalCooldown(_skillName, Cooldown, !ignoreGlobalCooldown);
    }

    public override void ExecuteOnServer(PlayerCore caster, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        GameObject groundEffect = Instantiate(groundEffectPrefab, targetPosition.Value, Quaternion.identity);
        NetworkServer.Spawn(groundEffect);
        groundEffect.GetComponent<GroundEffect>().Init(slowPercentage, duration, aoeRadius, caster.team);
    }
}

// GroundEffect.cs (отдельный скрипт)
public class GroundEffect : NetworkBehaviour
{
    private float slowPercent;
    private float dur;
    private float rad;
    private PlayerTeam team;

    public void Init(float slow, float duration, float radius, PlayerTeam ownerTeam)
    {
        slowPercent = slow;
        dur = duration;
        rad = radius;
        team = ownerTeam;
        StartCoroutine(DestroyAfter(duration));
    }

    private void Update()
    {
        if (!isServer) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, rad);
        foreach (Collider col in hits)
        {
            PlayerCore player = col.GetComponent<PlayerCore>();
            if (player != null && player.team != team)
            {
                player.ApplySlow(slowPercent, 1f, 1); // Короткий тик
            }
        }
    }

    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }
}