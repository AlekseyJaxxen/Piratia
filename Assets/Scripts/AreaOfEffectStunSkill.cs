using UnityEngine;
using Mirror;
using System.Collections;

public class AreaOfEffectStunSkill : SkillBase
{
    [Header("Area Stun Skill Specifics")]
    public float stunDuration = 2f;
    public float effectRadius = 2f;
    public GameObject effectPrefab;

    public override void Execute(PlayerCore player, Vector3? targetPosition, GameObject targetObject)
    {
        if (!targetPosition.HasValue) return;
        CmdStunArea(targetPosition.Value);
    }

    [Command]
    private void CmdStunArea(Vector3 position)
    {
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        // ÄÎÁÀÂËÅÍÎ: Ïğîâåğêà, íàõîäèòñÿ ëè êàñòåğ ïîä ñòàíîì
        if (casterCore.isStunned)
        {
            Debug.Log("Caster is stunned and cannot use this skill.");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(position, effectRadius);

        foreach (var hit in hits)
        {
            PlayerCore targetCore = hit.GetComponent<PlayerCore>();
            if (targetCore != null && casterCore != null)
            {
                if (casterCore.team != targetCore.team)
                {
                    // ÈÑÏÎËÜÇÓÅÌ ÍÎÂÛÉ ÌÅÒÎÄ ÄËß ÏĞÈÌÅÍÅÍÈß İÔÔÅÊÒÀ ÊÎÍÒĞÎËß
                    targetCore.ApplyControlEffect(ControlEffectType.Stun, stunDuration);
                }
                else
                {
                    Debug.Log("Cannot stun a teammate!");
                }
            }
        }
        RpcPlayEffect(position);
    }

    [ClientRpc]
    private void RpcPlayEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 3f;
            StartCoroutine(DecreaseScaleOverTime(effect.transform, 1f, 2f));
        }
    }

    private IEnumerator DecreaseScaleOverTime(Transform targetTransform, float duration, float destroyTime)
    {
        float elapsed = 0f;
        Vector3 originalScale = targetTransform.localScale;
        Vector3 targetScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            targetTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        targetTransform.localScale = Vector3.zero;
        yield return new WaitForSeconds(destroyTime - duration);
        Destroy(targetTransform.gameObject);
    }
}