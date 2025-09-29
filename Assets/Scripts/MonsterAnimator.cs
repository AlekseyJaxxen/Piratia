using UnityEngine;

public class MonsterAnimator : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            Debug.LogWarning("[MonsterAnimator] Animator not found!");
        }
    }

    // Памятка: Анимации вызываются по именам клипов в Animator Controller. Названия: "Death", "Idle", "Move", "Attack", "IdleNoLeg", "LegDeath". Используй Play("Name") для запуска.

    public void PlayDeath()
    {
        if (_animator != null) _animator.Play("Death");
    }

    public void PlayIdle()
    {
        if (_animator != null) _animator.Play("Idle");
    }

    public void PlayMove()
    {
        if (_animator != null) _animator.Play("Move");
    }

    public void PlayAttack()
    {
        if (_animator != null) _animator.Play("Attack");
    }

    public void PlayIdleNoLeg()
    {
        if (_animator != null) _animator.Play("IdleNoLeg");
    }

    public void PlayLegDeath()
    {
        if (_animator != null) _animator.Play("LegDeath");
    }
}