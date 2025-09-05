using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using static SkillBase;

public class PlayerSkills : NetworkBehaviour
{
    [Header("Skills")]
    public List<SkillBase> skills = new List<SkillBase>();
    [Header("Stun Effect")]
    public GameObject stunEffectPrefab;
    private GameObject _stunEffectInstance;
    [Header("Silence Effect")]
    public GameObject silenceEffectPrefab;
    private GameObject _silenceEffectInstance;
    [Header("Cursor Settings")]
    public Texture2D defaultCursor;
    public Texture2D castCursor;
    public Texture2D attackCursor;
    public float cursorUpdateInterval = 0.1f;
    private float _lastCursorUpdate = 0f;
    private PlayerCore _core;
    private bool _isCasting;
    private ISkill _activeSkill;
    private Coroutine _castSkillCoroutine;
    private GameObject rangeIndicator;
    private readonly SyncDictionary<string, float> _skillLastUseTimes = new SyncDictionary<string, float>();
    [SerializeField] private float globalCooldown = 1f;
    [SyncVar(hook = nameof(OnGlobalCooldownChanged))] private float _lastGlobalUseTime;
    public bool IsSkillSelected => _activeSkill != null;
    public ISkill ActiveSkill => _activeSkill;
    private Dictionary<string, float> localCooldowns = new Dictionary<string, float>();
    private float localGlobalCooldownEnd = 0f;

    private void Awake()
    {
        _skillLastUseTimes.OnChange += OnCooldownChanged;
    }

    private void Start()
    {
        _core = GetComponent<PlayerCore>();
        StartCoroutine(InitializeSkills());
    }

    private IEnumerator InitializeSkills()
    {
        yield return new WaitForEndOfFrame();
        if (_core == null)
        {
            _core = GetComponent<PlayerCore>();
            if (_core == null)
            {
                yield break;
            }
        }
        if (stunEffectPrefab != null)
        {
            _stunEffectInstance = Instantiate(stunEffectPrefab, transform);
            _stunEffectInstance.SetActive(false);
        }
        if (silenceEffectPrefab != null)
        {
            _silenceEffectInstance = Instantiate(silenceEffectPrefab, transform);
            _silenceEffectInstance.SetActive(false);
        }
        CharacterStats stats = GetComponent<CharacterStats>();
        if (stats == null)
        {
            yield break;
        }
        int maxWaitFrames = 100;
        int currentFrame = 0;
        while (SkillManager.Instance == null && currentFrame < maxWaitFrames)
        {
            yield return null;
            currentFrame++;
        }
        if (SkillManager.Instance == null)
        {
            yield break;
        }
        skills = SkillManager.Instance.GetSkillsForClass(stats.characterClass).Select(s => Instantiate(s)).ToList();
        foreach (var skill in skills)
        {
            if (skill == null)
            {
                continue;
            }
            skill.Init(_core);
            if (isServer)
            {
                _skillLastUseTimes[skill.SkillName] = 0f;
            }
        }
        if (isLocalPlayer)
        {
            SetCursor(defaultCursor);
        }
    }

    private void OnDisable()
    {
        if (_castSkillCoroutine != null)
        {
            StopCoroutine(_castSkillCoroutine);
            _castSkillCoroutine = null;
        }
        if (_stunEffectInstance != null)
        {
            Destroy(_stunEffectInstance);
        }
        if (_silenceEffectInstance != null)
        {
            Destroy(_silenceEffectInstance);
        }
        CancelAllSkillSelections();
        foreach (var skill in skills)
        {
            skill.CleanupIndicators();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (_castSkillCoroutine != null)
        {
            StopCoroutine(_castSkillCoroutine);
            _castSkillCoroutine = null;
        }
        if (_stunEffectInstance != null)
        {
            Destroy(_stunEffectInstance);
        }
        if (_silenceEffectInstance != null)
        {
            Destroy(_silenceEffectInstance);
        }
        CancelAllSkillSelections();
        foreach (var skill in skills)
        {
            skill.CleanupIndicators();
        }
    }

    public void HandleStunEffect(bool isStunned)
    {
        if (_stunEffectInstance != null)
        {
            _stunEffectInstance.SetActive(isStunned);
        }
    }

    public void HandleSilenceEffect(bool isSilenced)
    {
        if (_silenceEffectInstance != null)
        {
            _silenceEffectInstance.SetActive(isSilenced);
        }
    }

    private void UpdateTargetIndicator()
    {
        if (_activeSkill == null || ((SkillBase)_activeSkill).effectRadiusPrefab == null) return;
        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.groundLayer))
        {
            ((SkillBase)_activeSkill).SetEffectRadiusPosition(hit.point + Vector3.up * 0.01f);
        }
    }

    [Command]
    public void CmdExecuteSkill(PlayerCore caster, Vector3? targetPosition, uint targetNetId, string skillName, int weight)
    {
        if (!caster.CanCastSkill())
        {
            return;
        }
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null)
        {
            return;
        }
        if (GetRemainingCooldown(skillName) > 0)
        {
            return;
        }
        if (!skill.ignoreGlobalCooldown && GetGlobalRemainingCooldown() > 0)
        {
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(skill.ManaCost))
        {
            return;
        }
        GameObject targetObject = null;
        if (targetNetId != 0 && NetworkServer.spawned.ContainsKey(targetNetId))
        {
            targetObject = NetworkServer.spawned[targetNetId].gameObject;
        }
        const float tolerance = 3f;
        const float warningThreshold = 1.5f;
        if (skill.Range > 0)
        {
            float distance = 0f;
            if (targetObject != null)
            {
                distance = Vector3.Distance(transform.position, targetObject.transform.position);
            }
            else if (targetPosition.HasValue)
            {
                distance = Vector3.Distance(transform.position, targetPosition.Value);
            }
            if (distance > skill.Range + tolerance)
            {
                return;
            }
            if (distance > skill.Range + warningThreshold)
            {
                Debug.LogWarning($"Skill {skillName} used with high tolerance: Distance = {distance}, Range = {skill.Range}, Tolerance = {tolerance}");
            }
        }
        if (stats != null) stats.SpendMana(skill.ManaCost);
        if (skill.CastTime > 0)
        {
            StartCoroutine(CastSkillCoroutine(skill, targetPosition, targetObject, weight));
        }
        else
        {
            skill.ExecuteOnServer(caster, targetPosition, targetObject, weight);
            StartSkillCooldown(skillName);
            if (!skill.ignoreGlobalCooldown) StartGlobalCooldown();
            RpcCancelSkillSelection();
        }
    }

    private IEnumerator CastSkillCoroutine(SkillBase skill, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        _isCasting = true;
        yield return new WaitForSeconds(skill.CastTime);
        _isCasting = false;
        skill.ExecuteOnServer(_core, targetPosition, targetObject, weight);
        StartSkillCooldown(skill.SkillName);
        if (!skill.ignoreGlobalCooldown) StartGlobalCooldown();
        RpcCancelSkillSelection();
    }

    public float GetRemainingCooldown(string skillName)
    {
        if (_skillLastUseTimes.ContainsKey(skillName))
        {
            return Mathf.Max(0, skills.Find(s => s.SkillName == skillName).Cooldown - ((float)NetworkTime.time - _skillLastUseTimes[skillName]));
        }
        return 0f;
    }

    public float GetGlobalRemainingCooldown()
    {
        return Mathf.Max(0, globalCooldown - ((float)NetworkTime.time - _lastGlobalUseTime));
    }

    [Server]
    public void StartSkillCooldown(string skillName)
    {
        _skillLastUseTimes[skillName] = (float)NetworkTime.time;
    }

    [Server]
    public void StartGlobalCooldown()
    {
        _lastGlobalUseTime = (float)NetworkTime.time;
    }

    private void HandleSkills()
    {
        if (skills == null || skills.Count == 0) return;
        foreach (var skill in skills.Where(s => s.Hotkey != KeyCode.None))
        {
            if (Input.GetKeyDown(skill.Hotkey))
            {
                if (_core.isSilenced)
                {
                    Debug.LogWarning($"[PlayerSkills] Cannot select skill {skill.SkillName}: player is silenced");
                    continue;
                }
                if (GetRemainingCooldown(skill.SkillName) > 0 || (!skill.ignoreGlobalCooldown && GetGlobalRemainingCooldown() > 0))
                {
                    continue;
                }
                if (localCooldowns.ContainsKey(skill.SkillName) && (float)NetworkTime.time < localCooldowns[skill.SkillName]) continue;
                if (!skill.ignoreGlobalCooldown && (float)NetworkTime.time < localGlobalCooldownEnd) continue;
                if (skill.SkillCastType == CastType.SelfBuff)
                {
                    skill.Execute(_core, null, _core.gameObject);
                    CancelSkillSelection();
                    return;
                }
                SelectSkill(skill);
                return;
            }
        }
        if (_isCasting) return;
        if (Input.GetMouseButtonDown(1))
        {
            CancelSkillSelection();
        }
        if (_activeSkill != null)
        {
            UpdateTargetIndicator();
        }
        else
        {
            UpdateCursor();
        }
    }

    public void SelectSkill(ISkill skill)
    {
        if (_core.isSilenced)
        {
            Debug.LogWarning("[PlayerSkills] Cannot select skill: player is silenced");
            return;
        }
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
        }
        _activeSkill = skill;
        skill.SetIndicatorVisibility(true);
        SetCursor(castCursor);
    }

    [ClientRpc]
    public void RpcPlayBasicAttackVFX(Vector3 startPos, Quaternion startRot, Vector3 targetPos, bool isCritical, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is BasicAttackSkill basicAttackSkill)
        {
            basicAttackSkill.PlayVFX(startPos, startRot, targetPos, isCritical, this);
        }
    }

    [ClientRpc]
    public void RpcSpawnProjectile(Vector3 startPos, Vector3 targetPos, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is ProjectileDamageSkill projectileSkill)
        {
            projectileSkill.SpawnProjectile(startPos, targetPos, this);
        }
        else if (skill is SlowSkill slowSkill)
        {
            slowSkill.SpawnProjectile(startPos, targetPos, this);
        }
    }

    [ClientRpc]
    public void RpcApplySlowEffect(uint targetNetId, float duration, string skillName)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill is SlowSkill slowSkill)
            {
                slowSkill.ApplySlowEffect(targetIdentity.gameObject, duration, this);
            }
        }
    }

    [ClientRpc]
    public void RpcPlayTargetedStun(uint targetNetId, string skillName)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill is TargetedStunSkill targetedStunSkill)
            {
                targetedStunSkill.PlayEffect(targetIdentity.gameObject, this);
            }
        }
    }

    [ClientRpc]
    public void RpcPlayTargetedSilence(uint targetNetId, string skillName)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill is TargetedSilenceSkill targetedSilenceSkill)
            {
                targetedSilenceSkill.PlayEffect(targetIdentity.gameObject, this);
            }
        }
    }

    [ClientRpc]
    public void RpcPlayHealingSkill(uint targetNetId, string skillName)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill is HealingSkill healingSkill)
            {
                healingSkill.PlayEffect(targetIdentity.gameObject);
            }
        }
    }

    [ClientRpc]
    public void RpcPlayAoeStun(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AreaOfEffectStunSkill aoeStunSkill)
        {
            aoeStunSkill.PlayEffect(position);
        }
    }

    [ClientRpc]
    public void RpcPlayAoeHeal(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AreaOfEffectHealSkill aoeHealSkill)
        {
            aoeHealSkill.PlayEffect(position);
        }
    }

    [ClientRpc]
    public void RpcPlayAoeDamage(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AoeDamageSkill aoeDamageSkill)
        {
            aoeDamageSkill.PlayEffect(position, GetComponent<PlayerCore>());
        }
    }

    private void Update()
    {
        if (isLocalPlayer) HandleSkills();
        if (isLocalPlayer)
        {
            UpdateGlobalCooldownUI();
        }
        if (isLocalPlayer)
        {
            foreach (var skill in skills)
            {
                UpdateSkillUI(skill.SkillName);
            }
        }
    }

    public void CancelAllSkillSelections()
    {
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
            _activeSkill = null;
            SetCursor(defaultCursor);
        }
    }

    public void CancelSkillSelection()
    {
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
            _activeSkill = null;
            SetCursor(defaultCursor);
        }
    }

    private void UpdateCursor()
    {
        if ((float)NetworkTime.time - _lastCursorUpdate > cursorUpdateInterval)
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                GameObject hitObject = hit.collider.gameObject;
                PlayerCore hitCore = hitObject.GetComponent<PlayerCore>();
                Monster hitMonster = hitObject.GetComponent<Monster>();
                if ((hitCore != null && hitCore.team != _core.team) || hitMonster != null)
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
            _lastCursorUpdate = (float)NetworkTime.time;
        }
    }

    private void SetCursor(Texture2D cursor)
    {
        Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
    }

    private void OnCooldownChanged(SyncDictionary<string, float>.Operation op, string key, float item)
    {
        if (isLocalPlayer) UpdateSkillUI(key);
    }

    private void OnGlobalCooldownChanged(float oldVal, float newVal)
    {
        if (isLocalPlayer) UpdateGlobalCooldownUI();
    }

    private void UpdateSkillUI(string key)
    {
        SkillBase skill = skills.Find(s => s.SkillName == key);
        if (skill != null)
        {
            float progress;
            if (localCooldowns.ContainsKey(key))
            {
                float remainingCooldown = localCooldowns[key] - (float)NetworkTime.time;
                progress = Mathf.Clamp01(remainingCooldown / skill.Cooldown);
            }
            else
            {
                progress = 1f - skill.CooldownProgressNormalized;
            }
            PlayerUI.Instance.UpdateSkillCooldown(key, progress);
        }
    }

    private void UpdateGlobalCooldownUI()
    {
        float progress = 1f - Mathf.Max(0, globalCooldown - ((float)NetworkTime.time - _lastGlobalUseTime)) / globalCooldown;
        PlayerUI.Instance.UpdateGlobalCooldown(progress);
    }

    [ClientRpc]
    private void RpcCancelSkillSelection()
    {
        CancelSkillSelection();
    }

    public void StartLocalCooldown(string skillName, float cooldown, bool useGlobal)
    {
        localCooldowns[skillName] = (float)NetworkTime.time + cooldown;
        if (useGlobal) localGlobalCooldownEnd = (float)NetworkTime.time + globalCooldown;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);
        if (skills == null || skills.Count == 0) return;
    }

    [ClientRpc]
    public void RpcPlayReviveVFX(uint targetNetId, string skillName)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill is ReviveSkill reviveSkill)
            {
                reviveSkill.PlayEffect(targetIdentity.gameObject);
            }
        }
    }
}