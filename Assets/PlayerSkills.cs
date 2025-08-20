using UnityEngine;
using System.Collections;
using Mirror;

public class PlayerSkills : NetworkBehaviour
{
    [Header("Skill 1 Settings")]
    public KeyCode skillHotkey = KeyCode.F1;
    public float skillRange = 5f;
    public float skillCooldown = 3f;
    public float castTime = 1f;
    public int skillDamage = 15;
    public GameObject skillEffectPrefab;
    public GameObject skillProjectilePrefab;
    public GameObject skillRangeIndicator;
    public GameObject skillTargetIndicatorPrefab;

    [Header("Stun Effect")]
    public GameObject stunEffectPrefab;
    private GameObject _stunEffectInstance;

    [Header("Skill 2 Settings")]
    public KeyCode skill2Hotkey = KeyCode.F2;
    public float skill2Range = 7f;
    public float skill2Cooldown = 5f;
    public float skill2CastTime = 1.5f;
    public float skill2StunDuration = 2f;
    public GameObject skill2EffectPrefab;
    public GameObject skill2RangeIndicator;
    public GameObject skill2TargetIndicatorPrefab;

    [Header("Skill 3 Settings")]
    public KeyCode skill3Hotkey = KeyCode.F3;
    public float skill3Range = 4f;
    public float skill3Cooldown = 4f;
    public float skill3CastTime = 0.8f;
    public int skill3Damage = 20;
    public GameObject skill3EffectPrefab;
    public GameObject skill3RangeIndicator;
    public GameObject skill3TargetIndicatorPrefab;

    [Header("Skill 4 Settings")]
    public KeyCode skill4Hotkey = KeyCode.F4;
    public float skill4Range = 8f;
    public float skill4Cooldown = 8f;
    public float skill4CastTime = 2f;
    public float skill4StunDuration = 3f;
    public GameObject skill4EffectPrefab;
    public GameObject skill4RangeIndicator;
    public GameObject skill4TargetIndicatorPrefab;

    [Header("Cursor Settings")]
    public Texture2D defaultCursor;
    public Texture2D castCursor;

    private float _lastSkillTime;
    private float _lastSkill2Time;
    private float _lastSkill3Time;
    private float _lastSkill4Time;
    private PlayerCore _core;
    private GameObject _targetIndicator;
    private GameObject _targetIndicator2;
    private GameObject _targetIndicator3;
    private GameObject _targetIndicator4;
    private bool _isCasting;
    private int _currentSkillIndex;
    private GameObject _currentTarget;

    public void Init(PlayerCore core)
    {
        _core = core;

        if (stunEffectPrefab != null)
        {
            _stunEffectInstance = Instantiate(stunEffectPrefab, transform);
            _stunEffectInstance.SetActive(false);
        }

        if (isLocalPlayer)
        {
            _targetIndicator = Instantiate(skillTargetIndicatorPrefab);
            _targetIndicator.SetActive(false);
            skillRangeIndicator.SetActive(false);

            _targetIndicator2 = Instantiate(skill2TargetIndicatorPrefab);
            _targetIndicator2.SetActive(false);
            skill2RangeIndicator.SetActive(false);

            _targetIndicator3 = Instantiate(skill3TargetIndicatorPrefab);
            _targetIndicator3.SetActive(false);
            skill3RangeIndicator.SetActive(false);

            _targetIndicator4 = Instantiate(skill4TargetIndicatorPrefab);
            _targetIndicator4.SetActive(false);
            skill4RangeIndicator.SetActive(false);

            SetCursor(defaultCursor);
        }
    }

    public void HandleStunEffect(bool isStunned)
    {
        if (_stunEffectInstance != null)
        {
            _stunEffectInstance.SetActive(isStunned);
        }
    }

    public void HandleSkills()
    {
        if (!isLocalPlayer || _isCasting || _core.isDead || _core.isStunned || !_core.ActionSystem.CanStartNewAction) return;

        HandleSkill(skillHotkey, ref _lastSkillTime, skillCooldown, skillRangeIndicator, 1);
        HandleSkill(skill2Hotkey, ref _lastSkill2Time, skill2Cooldown, skill2RangeIndicator, 2);
        HandleSkill(skill3Hotkey, ref _lastSkill3Time, skill3Cooldown, skill3RangeIndicator, 3);
        HandleSkill(skill4Hotkey, ref _lastSkill4Time, skill4Cooldown, skill4RangeIndicator, 4);

        if (AnySkillRangeActive())
        {
            UpdateTargetIndicator();
            if (_currentSkillIndex == 1 || _currentSkillIndex == 4)
            {
                SetCursor(castCursor);
            }
            else
            {
                Cursor.visible = false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryCastSkill();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                CancelSkillSelection();
            }
        }
        else
        {
            SetCursor(defaultCursor);
            Cursor.visible = true;
        }
    }

    private void HandleSkill(KeyCode hotkey, ref float lastSkillTime, float cooldown, GameObject rangeIndicator, int skillIndex)
    {
        if (Input.GetKeyDown(hotkey) && Time.time - lastSkillTime >= cooldown)
        {
            CancelAllSkillSelections();
            _currentSkillIndex = skillIndex;
            ShowSkillRange(rangeIndicator);
        }
    }

    public bool AnySkillRangeActive()
    {
        return (skillRangeIndicator != null && skillRangeIndicator.activeSelf) ||
                (skill2RangeIndicator != null && skill2RangeIndicator.activeSelf) ||
                (skill3RangeIndicator != null && skill3RangeIndicator.activeSelf) ||
                (skill4RangeIndicator != null && skill4RangeIndicator.activeSelf);
    }

    private void ShowSkillRange(GameObject rangeIndicator)
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
            rangeIndicator.transform.position = transform.position;
            rangeIndicator.transform.localScale = new Vector3(GetCurrentSkillRange() * 2, 0.1f, GetCurrentSkillRange() * 2);
        }
    }

    private void UpdateTargetIndicator()
    {
        GameObject currentTargetIndicator = GetCurrentTargetIndicator();
        if (currentTargetIndicator == null) return;

        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (_currentSkillIndex == 1 || _currentSkillIndex == 4)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    _currentTarget = hit.collider.gameObject;
                    currentTargetIndicator.SetActive(true);
                    currentTargetIndicator.transform.position = _currentTarget.transform.position + Vector3.up * 1f;
                }
                else
                {
                    _currentTarget = null;
                    currentTargetIndicator.SetActive(false);
                }
            }
            else
            {
                _currentTarget = null;
                currentTargetIndicator.SetActive(false);
            }
        }
        else
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                currentTargetIndicator.SetActive(true);
                currentTargetIndicator.transform.position = hit.point + Vector3.up * 0.1f;
                currentTargetIndicator.transform.localScale = new Vector3(GetCurrentSkillRadius() * 2, 0.1f, GetCurrentSkillRadius() * 2);
            }
            else
            {
                currentTargetIndicator.SetActive(false);
            }
        }
    }

    private void TryCastSkill()
    {
        if (_core.isStunned)
        {
            CancelSkillSelection();
            return;
        }

        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
        {
            if (_currentSkillIndex == 1 || _currentSkillIndex == 4)
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    CancelAllSkillSelections();
                    _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.collider.gameObject.transform.position);
                    _currentTarget = hit.collider.gameObject;
                }
            }
            else
            {
                CancelAllSkillSelections();
                _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point);
            }
        }
    }

    public IEnumerator CastSkill(Vector3 targetPosition)
    {
        if (_core.isDead || _core.isStunned)
        {
            _isCasting = false;
            _core.ActionSystem.CompleteAction();
            yield break;
        }
        _isCasting = true;
        _core.Movement.StopMovement();

        float distance = Vector3.Distance(transform.position, targetPosition);
        float currentRange = GetCurrentSkillRange();
        if (distance > currentRange)
        {
            _core.Movement.MoveTo(targetPosition);
            yield return new WaitUntil(() => Vector3.Distance(transform.position, targetPosition) <= currentRange || _core.isDead);
        }

        yield return new WaitForSeconds(GetCurrentCastTime());

        CmdSpawnSkillEffect(targetPosition, _currentSkillIndex);
        if (_currentSkillIndex == 1)
        {
            if (_currentTarget != null)
            {
                CmdSpawnProjectileEffect(transform.position, _currentTarget.transform.position);
                Health targetHealth = _currentTarget.GetComponent<Health>();
                if (targetHealth != null)
                {
                    CmdApplyDamage(targetHealth.netId, skillDamage);
                }
            }
        }
        else if (_currentSkillIndex == 2)
        {
            ApplySkill2Effect(targetPosition);
        }
        else if (_currentSkillIndex == 3)
        {
            ApplySkill3Damage(targetPosition);
        }
        else if (_currentSkillIndex == 4)
        {
            if (_currentTarget != null)
            {
                CmdApplyStun(_currentTarget, skill4StunDuration);
            }
        }

        UpdateLastSkillTime();
        _isCasting = false;
        _core.ActionSystem.CompleteAction();
    }

    [Command]
    private void CmdSpawnSkillEffect(Vector3 position, int skillIndex)
    {
        RpcSpawnSkillEffect(position, skillIndex);
    }

    [ClientRpc]
    private void RpcSpawnSkillEffect(Vector3 position, int skillIndex)
    {
        GameObject effectPrefab = GetEffectPrefab(skillIndex);
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    [Command]
    private void CmdSpawnProjectileEffect(Vector3 startPos, Vector3 targetPos)
    {
        RpcSpawnProjectileEffect(startPos + Vector3.up * 1f, targetPos + Vector3.up * 1f);
    }

    [ClientRpc]
    private void RpcSpawnProjectileEffect(Vector3 startPos, Vector3 targetPos)
    {
        if (skillProjectilePrefab != null)
        {
            GameObject projectile = Instantiate(skillProjectilePrefab, startPos, Quaternion.identity);
            StartCoroutine(MoveProjectile(projectile, targetPos));
        }
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 targetPos)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = projectile.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            projectile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Destroy(projectile);
    }

    private void ApplySkill2Effect(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Player"))
            {
                NetworkIdentity networkIdentity = hit.GetComponent<NetworkIdentity>();
                if (networkIdentity != null && networkIdentity.netId != _core.netId)
                {
                    CmdApplyStun(hit.gameObject, skill2StunDuration);
                }
            }
        }
    }

    [Command]
    private void CmdApplyStun(GameObject target, float duration)
    {
        if (target == null) return;

        PlayerCore targetCore = target.GetComponent<PlayerCore>();
        if (targetCore != null)
        {
            targetCore.StartCoroutine(StunRoutine(targetCore, duration));
        }
    }

    [Server]
    private IEnumerator StunRoutine(PlayerCore core, float duration)
    {
        core.SetStunState(true);

        core.Movement.StopMovement();

        yield return new WaitForSeconds(duration);

        core.SetStunState(false);
    }

    private void ApplySkill3Damage(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Player"))
            {
                Health targetHealth = hit.GetComponent<Health>();
                if (targetHealth != null)
                {
                    CmdApplyDamage(targetHealth.netId, skill3Damage);
                }
            }
        }
    }

    [Command]
    private void CmdApplyDamage(uint targetNetId, int damage)
    {
        if (NetworkServer.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
            targetIdentity.GetComponent<Health>()?.TakeDamage(damage);
        }
    }

    private void CompleteAttack()
    {
        // ... (current code) ...
    }

    public void CancelSkillSelection()
    {
        CancelAllSkillSelections();
        _isCasting = false;
        _core.ActionSystem.CompleteAction();
    }

    private void CancelAllSkillSelections()
    {
        skillRangeIndicator.SetActive(false);
        skill2RangeIndicator.SetActive(false);
        skill3RangeIndicator.SetActive(false);
        skill4RangeIndicator.SetActive(false);

        if (_targetIndicator != null) _targetIndicator.SetActive(false);
        if (_targetIndicator2 != null) _targetIndicator2.SetActive(false);
        if (_targetIndicator3 != null) _targetIndicator3.SetActive(false);
        if (_targetIndicator4 != null) _targetIndicator4.SetActive(false);

        SetCursor(defaultCursor);
        Cursor.visible = true;
    }

    private void SetCursor(Texture2D cursorTexture)
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    #region Helper Methods
    private float GetCurrentSkillRange()
    {
        return _currentSkillIndex switch
        {
            1 => skillRange,
            2 => skill2Range,
            3 => skill3Range,
            4 => skill4Range,
            _ => skillRange
        };
    }

    private float GetCurrentCastTime()
    {
        return _currentSkillIndex switch
        {
            1 => castTime,
            2 => skill2CastTime,
            3 => skill3CastTime,
            4 => skill4CastTime,
            _ => castTime
        };
    }

    private GameObject GetEffectPrefab(int skillIndex)
    {
        return skillIndex switch
        {
            1 => skillEffectPrefab,
            2 => skill2EffectPrefab,
            3 => skill3EffectPrefab,
            4 => skill4EffectPrefab,
            _ => skillEffectPrefab
        };
    }

    private GameObject GetCurrentTargetIndicator()
    {
        return _currentSkillIndex switch
        {
            1 => _targetIndicator,
            2 => _targetIndicator2,
            3 => _targetIndicator3,
            4 => _targetIndicator4,
            _ => _targetIndicator
        };
    }

    private void UpdateLastSkillTime()
    {
        switch (_currentSkillIndex)
        {
            case 1: _lastSkillTime = Time.time; break;
            case 2: _lastSkill2Time = Time.time; break;
            case 3: _lastSkill3Time = Time.time; break;
            case 4: _lastSkill4Time = Time.time; break;
        }
    }

    private float GetCurrentSkillRadius()
    {
        return _currentSkillIndex switch
        {
            2 => 2f,
            3 => 2f,
            _ => 1f
        };
    }
    #endregion

    public float LastSkill1Time => _lastSkillTime;
    public float LastSkill2Time => _lastSkill2Time;
    public float LastSkill3Time => _lastSkill3Time;
    public float LastSkill4Time => _lastSkill4Time;

    public float Skill1Cooldown => skillCooldown;
    public float Skill2Cooldown => skill2Cooldown;
    public float Skill3Cooldown => skill3Cooldown;
    public float Skill4Cooldown => skill4Cooldown;
}