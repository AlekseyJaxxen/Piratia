using UnityEngine;
using Mirror;
public class PlayerCombat : NetworkBehaviour
{
    private PlayerCore _core;
    [SyncVar] private uint _targetNetId;
    [HideInInspector]
    public float _lastAttackTime = -Mathf.Infinity;
    private GameObject _targetCache; // Кэш для локального доступа
    public GameObject Target
    {
        get
        {
            if (_targetCache == null && _targetNetId != 0 && NetworkServer.spawned.ContainsKey(_targetNetId))
            {
                _targetCache = NetworkServer.spawned[_targetNetId].gameObject;
            }
            return _targetCache;
        }
    }
    public void Init(PlayerCore core)
    {
        _core = core;
    }
    public void HandleCombat()
    {
        if (_core.ActionSystem.CurrentAction != PlayerAction.Attack) return;
        if (Target == null) return;
    }
    public void SetCurrentTarget(GameObject target)
    {
        _targetNetId = target ? target.GetComponent<NetworkIdentity>().netId : 0;
        _targetCache = target;
    }
    public void ClearTarget()
    {
        _targetNetId = 0;
        _targetCache = null;
    }
    public void StopAttacking()
    {
        ClearTarget();
    }
}