using UnityEngine;
using Mirror;

public class BasicAttackSkill : SkillBase
{
    [Header("Basic Attack Settings")]
    public int damageAmount = 10;

    // Добавлено: Префаб визуального эффекта
    public GameObject vfxPrefab;

    // Этот метод вызывается на клиенте
    public override void Execute(PlayerCore caster, Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject == null || !isOwned) return;

        // Отправляем команду на сервер для выполнения атаки
        CmdPerformAttack(caster.transform.position, targetObject.GetComponent<NetworkIdentity>().netId);
    }

    // [Command]-метод, который выполняется на сервере
    [Command(requiresAuthority = false)]
    private void CmdPerformAttack(Vector3 casterPosition, uint targetNetId)
    {
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore attackerCore = connectionToClient.identity.GetComponent<PlayerCore>();

            if (targetHealth != null && targetCore != null && attackerCore != null)
            {
                if (attackerCore.team != targetCore.team)
                {
                    targetHealth.TakeDamage(damageAmount);
                    // Добавлено: Вызываем Rpc метод для воспроизведения VFX на всех клиентах
                    RpcPlayVFX(casterPosition, targetIdentity.transform.position);
                }
                else
                {
                    Debug.Log("Cannot attack a teammate!");
                }
            }
        }
    }

    // Добавлено: [ClientRpc]-метод, который выполняется на всех клиентах
    [ClientRpc]
    private void RpcPlayVFX(Vector3 startPosition, Vector3 endPosition)
    {
        if (vfxPrefab != null)
        {
            // Здесь вы можете создать эффект. 
            // Для примера, создаем его на начальной позиции и уничтожаем через 2 секунды.
            GameObject vfxInstance = Instantiate(vfxPrefab, startPosition, Quaternion.identity);

            // Если VFX должен перемещаться к цели, здесь нужно добавить логику движения.
            // Например, можно добавить компонент, который будет перемещать эффект от startPosition к endPosition.

            Destroy(vfxInstance, 0.2f);
        }
    }
}