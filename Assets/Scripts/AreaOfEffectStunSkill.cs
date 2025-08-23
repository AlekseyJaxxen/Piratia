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
        if (!isLocalPlayer || !targetPosition.HasValue) return;

        CmdStunArea(targetPosition.Value);
    }

    [Command]
    private void CmdStunArea(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, effectRadius);
        PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();

        foreach (var hit in hits)
        {
            PlayerCore targetCore = hit.GetComponent<PlayerCore>();

            if (targetCore != null && casterCore != null)
            {
                // ѕровер€ем, что цель находитс€ в другой команде
                if (casterCore.team != targetCore.team)
                {
                    // ¬ызываем новый серверный метод дл€ безопасного оглушени€
                    TryStunTarget(targetCore, stunDuration);
                }
                else
                {
                    Debug.Log("Cannot stun a teammate!");
                }
            }
        }
        RpcPlayEffect(position);
    }

    // Ќовый приватный серверный метод дл€ обработки оглушени€
    [Server]
    private void TryStunTarget(PlayerCore target, float duration)
    {
        // ¬ыполн€ем проверку и немедленно устанавливаем состо€ние
        // Ёто предотвращает состо€ние гонки, так как всЄ происходит в одном серверном "кадре"
        if (!target.isStunned)
        {
            Debug.Log($"Stunning target {target.playerName}.");
            target.SetStunState(true);
            target.Movement.StopMovement();
            StartCoroutine(UnstunAfterDelay(target, duration));
        }
        else
        {
            Debug.Log($"Target {target.playerName} is already stunned.");
        }
    }

    [Server]
    private IEnumerator UnstunAfterDelay(PlayerCore core, float duration)
    {
        yield return new WaitForSeconds(duration);
        core.SetStunState(false);
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