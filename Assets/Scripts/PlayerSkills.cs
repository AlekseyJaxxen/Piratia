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

    private PlayerCore _core;
    private bool _isCasting;
    private ISkill _activeSkill;

    public bool IsSkillSelected => _activeSkill != null;

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
        if (!isLocalPlayer || _isCasting || _core.isDead || _core.isStunned || _core.ActionSystem.IsPerformingAction) return;

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

            if (Input.GetMouseButtonDown(0))
            {
                TryCastActiveSkill();
            }
            if (Input.GetMouseButtonDown(1))
            {
                CancelSkillSelection();
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

        // Этот блок отвечает за целевые навыки, такие как ProjectileDamageSkill и TargetedStunSkill.
        if (_activeSkill is ProjectileDamageSkill || _activeSkill is TargetedStunSkill)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    targetIndicator.SetActive(true);
                    targetIndicator.transform.position = hit.collider.gameObject.transform.position + Vector3.up * 1f;
                    SetCursor(_activeSkill.CastCursor);
                    targetIndicator.transform.localScale = Vector3.one;

                    // Добавлена проверка на существование индикатора радиуса
                    if (_activeSkill.RangeIndicator != null)
                    {
                        _activeSkill.RangeIndicator.SetActive(false);
                    }
                }
                else
                {
                    targetIndicator.SetActive(false);
                    SetCursor(defaultCursor);

                    // Добавлена проверка на существование индикатора радиуса
                    if (_activeSkill.RangeIndicator != null)
                    {
                        _activeSkill.RangeIndicator.SetActive(true);
                    }
                }
            }
            else
            {
                targetIndicator.SetActive(false);
                SetCursor(defaultCursor);

                // Добавлена проверка на существование индикатора радиуса
                if (_activeSkill.RangeIndicator != null)
                {
                    _activeSkill.RangeIndicator.SetActive(true);
                }
            }
        }
        // Этот блок отвечает за навыки с областью действия, такие как AreaOfEffectHealSkill.
        else
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                targetIndicator.SetActive(true);
                targetIndicator.transform.position = hit.point + Vector3.up * 0.1f;
                targetIndicator.transform.localScale = new Vector3(_activeSkill.Range * 2, 0.1f, _activeSkill.Range * 2);

                if (_activeSkill.RangeIndicator != null)
                {
                    _activeSkill.RangeIndicator.SetActive(false);
                }
                // ИСПРАВЛЕНО: Заменено прямое управление видимостью на вызов SetCursor().
                SetCursor(_activeSkill.CastCursor);
            }
            else
            {
                targetIndicator.SetActive(false);

                if (_activeSkill.RangeIndicator != null)
                {
                    _activeSkill.RangeIndicator.SetActive(true);
                }
                // ИСПРАВЛЕНО: Заменено прямое управление видимостью на вызов SetCursor().
                SetCursor(defaultCursor);
            }
        }
    }

    private void TryCastActiveSkill()
    {
        if (_activeSkill == null || _core.isStunned)
        {
            CancelSkillSelection();
            return;
        }

        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
        {
            if (_activeSkill is ProjectileDamageSkill || _activeSkill is TargetedStunSkill)
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.collider.gameObject.transform.position, hit.collider.gameObject, _activeSkill);
                }
            }
            else
            {
                _core.ActionSystem.TryStartAction(PlayerAction.SkillCast, hit.point, null, _activeSkill);
            }
        }
    }

    public IEnumerator CastSkill(Vector3? targetPosition, GameObject targetObject, ISkill skillToCast)
    {
        if (skillToCast == null || _core.isDead || _core.isStunned)
        {
            _isCasting = false;
            _core.ActionSystem.CompleteAction();
            yield break;
        }

        _isCasting = true;
        _core.Movement.StopMovement();

        if (targetPosition.HasValue)
        {
            float distance = Vector3.Distance(transform.position, targetPosition.Value);
            float currentRange = skillToCast.Range;
            if (distance > currentRange)
            {
                _core.Movement.MoveTo(targetPosition.Value);
                yield return new WaitUntil(() => Vector3.Distance(transform.position, targetPosition.Value) <= currentRange || _core.isDead);
            }
        }

        yield return new WaitForSeconds(skillToCast.CastTime);

        skillToCast.Execute(_core, targetPosition, targetObject);
        skillToCast.StartCooldown();

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

    private void SetCursor(Texture2D cursorTexture)
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }
}