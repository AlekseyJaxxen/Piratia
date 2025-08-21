using UnityEngine;
using Mirror;
using System.Collections;

public class StunTimer : NetworkBehaviour
{
    private PlayerCore _core;
    private Coroutine _stunCoroutine;

    [SyncVar(hook = nameof(OnStunnedStateChanged))]
    private bool _isStunned = false;
    public bool IsStunned => _isStunned;

    public void Init(PlayerCore core)
    {
        _core = core;
    }

    [Server]
    public void ApplyStun(float duration)
    {
        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
        }
        _stunCoroutine = StartCoroutine(StunDuration(duration));
    }

    [Server]
    private IEnumerator StunDuration(float duration)
    {
        _isStunned = true;
        _core.ActionSystem.CompleteAction();
        _core.Skills.HandleStunEffect(true);

        yield return new WaitForSeconds(duration);

        _isStunned = false;
        _core.Skills.HandleStunEffect(false);
    }

    private void OnStunnedStateChanged(bool oldState, bool newState)
    {
        if (isClient)
        {
            if (_core != null && _core.Skills != null)
            {
                _core.Skills.HandleStunEffect(newState);
            }
        }
    }
}