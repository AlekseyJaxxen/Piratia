using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlayerSkills : NetworkBehaviour
{
    [Header("Skills")]
    public List<SkillBase> skills = new List<SkillBase>();

    [Header("Stun Effect")]
    public GameObject stunEffectPrefab;
    private GameObject _stunEffectInstance;

    [Header("Cursor Settings")]
    public Texture2D defaultCursor;
    public Texture2D castCursor;
    public Texture2D attackCursor;
    public float cursorUpdateInterval = 0.1f; // Интервал в секундах
    private float _lastCursorUpdate = 0f;

    private PlayerCore _core;
    private bool _isCasting;
    private ISkill _activeSkill;

    public bool IsSkillSelected => _activeSkill != null;
    public ISkill ActiveSkill => _activeSkill;

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
            foreach (var skill in skills)
            {
                skill.Init(core);
            }
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
        if (!isLocalPlayer || _isCasting || _core.isDead || _core.EffectManager.IsStunned) return; // FIX: Changed _core.isStunned

        // Обработка нажатий клавиш для выбора навыка.
        // Это действие не прерывает движение.
        foreach (var skill in skills)
        {
            if (Input.GetKeyDown(skill.Hotkey) && !skill.IsOnCooldown())
            {
                CancelAllSkillSelections();
                _activeSkill = skill;
                _activeSkill.SetIndicatorVisibility(true);
            }
        }

        if (_activeSkill != null)
        {
            UpdateTargetIndicator();

            // Правый клик для отмены
            if (Input.GetMouseButtonDown(1))
            {
                CancelSkillSelection();
            }
        }
        else
        {
            UpdateCursor();
        }
    }

    private void UpdateCursor()
    {
        if (Time.time < _lastCursorUpdate + cursorUpdateInterval) return;

        _lastCursorUpdate = Time.time;

        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _core.interactableLayers))
        {
            if (hit.collider.CompareTag("Enemy") || (hit.collider.CompareTag("Player") && hit.collider.GetComponent<PlayerCore>()?.team != _core.team))
            {
                SetCursor(attackCursor);
            }
            else
            {
                SetCursor(defaultCursor);
            }
        }
        else
        {
            SetCursor(defaultCursor);
        }
    }


    private void UpdateTargetIndicator()
    {
        GameObject targetIndicator = _activeSkill.TargetIndicator;
        if (targetIndicator == null) return;

        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // ОБЩАЯ ЛОГИКА СМЕНЫ КУРСОРА:
        // Курсор меняется, если активен любой скилл, у которого задан CastCursor.
        if (_activeSkill.CastCursor != null)
        {
            SetCursor(_activeSkill.CastCursor);
        }
        else
        {
            SetCursor(defaultCursor);
        }

        // Логика для таргетных умений
        if (_activeSkill is ProjectileDamageSkill || _activeSkill is TargetedStunSkill || _activeSkill is SlowSkill)
        {
            // В этой части мы только управляем индикатором, а не курсором.
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _core.interactableLayers))
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    targetIndicator.SetActive(true);
                    targetIndicator.transform.position = hit.collider.gameObject.transform.position + Vector3.up * 1f;
                    targetIndicator.transform.localScale = Vector3.one;

                    if (_activeSkill.RangeIndicator != null)
                    {
                        _activeSkill.RangeIndicator.SetActive(false);
                    }
                }
                else
                {
                    targetIndicator.SetActive(false);
                    if (_activeSkill.RangeIndicator != null)
                    {
                        _activeSkill.RangeIndicator.SetActive(true);
                    }
                }
            }
            else
            {
                targetIndicator.SetActive(false);
                if (_activeSkill.RangeIndicator != null)
                {
                    _activeSkill.RangeIndicator.SetActive(true);
                }
            }
        }
        else // Логика для AoE-умений
        {
            // Логика индикатора для AoE
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _core.groundLayer))
            {
                targetIndicator.SetActive(true);
                targetIndicator.transform.position = hit.point + Vector3.up * 0.1f;
                targetIndicator.transform.localScale = new Vector3(_activeSkill.Range, 0.1f, _activeSkill.Range);

                if (_activeSkill.RangeIndicator != null)
                {
                    _activeSkill.RangeIndicator.SetActive(false);
                }
            }
            else
            {
                targetIndicator.SetActive(false);
                if (_activeSkill.RangeIndicator != null)
                {
                    _activeSkill.RangeIndicator.SetActive(true);
                }
            }
        }
    }

    public IEnumerator CastSkill(Vector3? targetPosition, GameObject targetObject, ISkill skillToCast)
    {
        if (skillToCast == null || _core.isDead || _core.EffectManager.IsStunned) // FIX: Changed _core.isStunned
        {
            _isCasting = false;
            _core.ActionSystem.CompleteAction();
            yield break;
        }

        _isCasting = true;

        // Логика для целевых (targeted) скиллов (например, Проектные, Стан)
        if (targetObject != null)
        {
            float distance;

            while (true)
            {
                if (_core.isDead || _core.EffectManager.IsStunned || targetObject == null) // FIX: Changed _core.isStunned
                {
                    Debug.Log("PlayerSkills: Target lost or player state invalid. Cancelling skill cast.");
                    _core.ActionSystem.CompleteAction();
                    yield break;
                }

                distance = Vector3.Distance(transform.position, targetObject.transform.position);

                if (distance <= skillToCast.Range)
                {
                    Debug.Log($"PlayerSkills: Target is in range. Casting immediately. Distance: {distance:F2}, Range: {skillToCast.Range:F2}");
                    _core.Movement.StopMovement();
                    _core.Movement.RotateTo(targetObject.transform.position - transform.position);
                    break;
                }
                else
                {
                    Debug.Log($"PlayerSkills: Target is out of range. Moving towards target. Distance: {distance:F2}, Range: {skillToCast.Range:F2}");
                    _core.Movement.MoveTo(targetObject.transform.position);
                }

                yield return null;
            }
        }
        // Логика для нецелевых (AoE) скиллов (например, каст на землю)
        else if (targetPosition.HasValue)
        {
            float distance = Vector3.Distance(transform.position, targetPosition.Value);
            if (distance > skillToCast.Range)
            {
                Debug.Log($"PlayerSkills: Target position is out of range. Moving to cast position. Distance: {distance:F2}, Range: {skillToCast.Range:F2}");
                _core.Movement.MoveTo(targetPosition.Value);
                yield return new WaitUntil(() => !_core.Movement.Agent.pathPending && _core.Movement.Agent.remainingDistance <= _core.Movement.Agent.stoppingDistance);

                // Повторная проверка расстояния
                distance = Vector3.Distance(transform.position, targetPosition.Value);
                if (distance > skillToCast.Range)
                {
                    Debug.Log("PlayerSkills: Arrived at destination, but position is still out of range. Cancelling skill cast.");
                    _isCasting = false;
                    _core.ActionSystem.CompleteAction();
                    yield break;
                }
            }

            Debug.Log($"PlayerSkills: Target position is in range. Casting immediately. Distance: {distance:F2}, Range: {skillToCast.Range:F2}");
            _core.Movement.StopMovement();
        }
        else
        {
            _isCasting = false;
            _core.ActionSystem.CompleteAction();
            yield break;
        }

        // Если персонаж не умер и все еще выполняет каст
        if (!_core.isDead && _core.ActionSystem.CurrentAction == PlayerAction.SkillCast)
        {
            yield return new WaitForSeconds(skillToCast.CastTime);
            skillToCast.Execute(_core, targetPosition, targetObject);
            skillToCast.StartCooldown();
        }

        _isCasting = false;
        _core.ActionSystem.CompleteAction();
        CancelAllSkillSelections();
    }

    public void CancelSkillSelection()
    {
        CancelAllSkillSelections();
        _isCasting = false;
    }

    private void CancelAllSkillSelections()
    {
        foreach (var skill in skills)
        {
            if (skill.TargetIndicator != null)
            {
                skill.TargetIndicator.transform.localScale = Vector3.one;
            }
            skill.SetIndicatorVisibility(false);
        }
        _activeSkill = null;
    }

    public void HandleSilenceEffect(bool isSilenced)
    {
        if (isSilenced)
        {
            CancelSkillSelection();
            foreach (var skill in skills)
            {
                skill.SetIndicatorVisibility(false);
            }
        }
    }

    private void SetCursor(Texture2D cursorTexture)
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }
}