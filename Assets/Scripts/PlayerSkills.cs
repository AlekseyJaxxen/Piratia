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
                Debug.LogError("[PlayerSkills] Init failed: PlayerCore is null");
                yield break;
            }
        }
        if (stunEffectPrefab != null)
        {
            _stunEffectInstance = Instantiate(stunEffectPrefab, transform);
            _stunEffectInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PlayerSkills] stunEffectPrefab is not assigned");
        }
        CharacterStats stats = GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError("[PlayerSkills] CharacterStats component missing!");
            yield break;
        }
        int maxWaitFrames = 100;
        int currentFrame = 0;
        while (SkillManager.Instance == null && currentFrame < maxWaitFrames)
        {
            Debug.Log($"[PlayerSkills] Waiting for SkillManager.Instance (frame {currentFrame}/{maxWaitFrames})");
            yield return null;
            currentFrame++;
        }
        if (SkillManager.Instance == null)
        {
            Debug.LogError("[PlayerSkills] SkillManager.Instance is still null after waiting!");
            yield break;
        }
        skills = SkillManager.Instance.GetSkillsForClass(stats.characterClass).Select(s => Instantiate(s)).ToList();
        Debug.Log($"[PlayerSkills] Loaded {skills.Count} skills for class {stats.characterClass}: {string.Join(", ", skills.Select(s => s != null ? s.SkillName : "null"))}");
        foreach (var skill in skills)
        {
            if (skill == null)
            {
                Debug.LogError("[PlayerSkills] Skill in skills list is null!");
                continue;
            }
            skill.Init(_core);
            if (isServer)
            {
                _skillLastUseTimes[skill.SkillName] = 0f;
                Debug.Log($"[PlayerSkills] Initialized cooldown for {skill.SkillName} to 0 on server");
            }
            Debug.Log($"[PlayerSkills] Initialized skill: {skill.SkillName}");
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
        CancelAllSkillSelections();
        foreach (var skill in skills)
        {
            skill.CleanupIndicators();
        }
        Debug.Log("[PlayerSkills] Cleaned up on disable.");
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
        CancelAllSkillSelections();
        Debug.Log("[PlayerSkills] Cleaned up on client disconnect.");
    }

    public void HandleStunEffect(bool isStunned)
    {
        if (_stunEffectInstance != null)
        {
            _stunEffectInstance.SetActive(isStunned);
        }
    }

    private void UpdateTargetIndicator()
    {
        if (_activeSkill == null || ((SkillBase)_activeSkill).effectRadiusPrefab == null) return;
        Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.groundLayer))
        {
            ((SkillBase)_activeSkill).effectRadiusIndicator.transform.position = hit.point + Vector3.up * 0.01f;
        }
    }

    [Command]
    public void CmdExecuteSkill(PlayerCore caster, Vector3? targetPosition, uint targetNetId, string skillName, int weight)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null)
        {
            Debug.LogWarning($"[PlayerSkills] Skill not found: {skillName}");
            return;
        }
        if (GetRemainingCooldown(skillName) > 0)
        {
            Debug.LogWarning($"[PlayerSkills] Skill on cooldown: {skillName}");
            return;
        }
        if (!skill.ignoreGlobalCooldown && GetGlobalRemainingCooldown() > 0)
        {
            Debug.LogWarning("[PlayerSkills] Global cooldown active");
            return;
        }
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(skill.ManaCost))
        {
            Debug.LogWarning($"[PlayerSkills] Not enough mana for {skillName}: {stats.currentMana}/{skill.ManaCost}");
            return;
        }
        GameObject targetObject = null;
        if (targetNetId != 0 && NetworkServer.spawned.ContainsKey(targetNetId))
        {
            targetObject = NetworkServer.spawned[targetNetId].gameObject;
        }
        if (stats != null) stats.SpendMana(skill.ManaCost);
        if (skill.CastTime > 0)
        {
            StartCoroutine(CastSkillCoroutine(skill, targetPosition, targetObject, weight));
        }
        else
        {
            ExecuteSkill(skill, targetPosition, targetObject, weight);
            RpcCancelSkillSelection();
        }
    }

    private IEnumerator CastSkillCoroutine(SkillBase skill, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        _isCasting = true;
        yield return new WaitForSeconds(skill.CastTime);
        _isCasting = false;
        ExecuteSkill(skill, targetPosition, targetObject, weight);
        RpcCancelSkillSelection();
    }

    private void ExecuteSkill(SkillBase skill, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        if (skill is BasicAttackSkill)
        {
            HandleBasicAttack(skill, targetObject);
        }
        else if (skill is ProjectileDamageSkill)
        {
            HandleProjectileDamage(skill, targetObject);
        }
        else if (skill is SlowSkill)
        {
            HandleSlowSkill(skill, targetObject, weight);
        }
        else if (skill is TargetedStunSkill)
        {
            HandleTargetedStun(skill, targetObject, weight);
        }
        else if (skill is AreaOfEffectStunSkill)
        {
            HandleAreaOfEffectStun(skill, targetPosition.Value, weight);
        }
        else if (skill is AreaOfEffectHealSkill)
        {
            HandleAreaOfEffectHeal(skill, targetPosition.Value);
        }
        else if (skill is HealingSkill)
        {
            HandleHealingSkill(skill, targetObject);
        }
        StartSkillCooldown(skill.SkillName);
        if (!skill.ignoreGlobalCooldown) StartGlobalCooldown();
        CancelSkillSelection();
    }

    private void HandleBasicAttack(SkillBase skill, GameObject targetObject)
    {
        CharacterStats stats = GetComponent<CharacterStats>();
        int damage = Random.Range(stats.minAttack, stats.maxAttack + 1);
        bool isCritical = Random.value < stats.criticalHitChance / 100f;
        if (isCritical) damage = Mathf.RoundToInt(damage * stats.criticalHitMultiplier);

        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, skill.SkillDamageType, netIdentity);
        }

        Vector3 startPos = transform.position + Vector3.up * 1.5f;
        Quaternion startRot = transform.rotation;
        Vector3 targetPos = targetObject.transform.position + Vector3.up * 1.5f;
        RpcPlayBasicAttackVFX(startPos, startRot, targetPos, isCritical, skill.SkillName);
    }

    // Здесь вставьте остальные методы Handle... (они были обрезаны, но для компиляции они нужны; предположим, они есть как в предыдущих сообщениях)

    private void HandleProjectileDamage(SkillBase skill, GameObject targetObject)
    {
        ProjectileDamageSkill projectileSkill = skill as ProjectileDamageSkill;
        int damage = projectileSkill.damageAmount;

        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, skill.SkillDamageType, netIdentity);
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetObject.transform.position;
        RpcSpawnProjectile(startPos, targetPos, skill.SkillName);
    }

    private void HandleSlowSkill(SkillBase skill, GameObject targetObject, int weight)
    {
        SlowSkill slowSkill = skill as SlowSkill;
        int damage = Mathf.RoundToInt(slowSkill.baseDamage * SlowSkill.DAMAGE_MULTIPLIER);

        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, skill.SkillDamageType, netIdentity);
        }

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        if (targetCore != null)
        {
            targetCore.ApplySlow(slowSkill.slowPercentage, slowSkill.slowDuration, weight);
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetObject.transform.position;
        RpcSpawnProjectile(startPos, targetPos, skill.SkillName);

        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        RpcApplySlowEffect(targetNetId, slowSkill.slowDuration, skill.SkillName);
    }

    private void HandleTargetedStun(SkillBase skill, GameObject targetObject, int weight)
    {
        TargetedStunSkill stunSkill = skill as TargetedStunSkill;

        PlayerCore targetCore = targetObject.GetComponent<PlayerCore>();
        if (targetCore != null)
        {
            targetCore.ApplyControlEffect(ControlEffectType.Stun, stunSkill.stunDuration, weight);
        }

        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        RpcPlayTargetedStun(targetNetId, skill.SkillName);
    }

    private void HandleAreaOfEffectStun(SkillBase skill, Vector3 position, int weight)
    {
        AreaOfEffectStunSkill aoeStun = skill as AreaOfEffectStunSkill;
        Collider[] hitColliders = Physics.OverlapSphere(position, skill.EffectRadius, _core.interactableLayers);
        foreach (Collider col in hitColliders)
        {
            PlayerCore targetCore = col.GetComponent<PlayerCore>();
            if (targetCore != null && targetCore.team != _core.team)
            {
                targetCore.ApplyControlEffect(ControlEffectType.Stun, aoeStun.stunDuration, weight);
            }
        }
        RpcPlayAoeStun(position, skill.SkillName);
    }

    private void HandleAreaOfEffectHeal(SkillBase skill, Vector3 position)
    {
        AreaOfEffectHealSkill aoeHeal = skill as AreaOfEffectHealSkill;
        Collider[] hitColliders = Physics.OverlapSphere(position, skill.EffectRadius, _core.interactableLayers);
        foreach (Collider col in hitColliders)
        {
            Health targetHealth = col.GetComponent<Health>();
            if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                if (targetCore != null && targetCore.team == _core.team)
                {
                    targetHealth.Heal(aoeHeal.healAmount);
                }
            }
        }
        RpcPlayAoeHeal(position, skill.SkillName);
    }

    private void HandleHealingSkill(SkillBase skill, GameObject targetObject)
    {
        HealingSkill healingSkill = skill as HealingSkill;
        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.Heal(healingSkill.healAmount);
        }
        uint targetNetId = targetObject.GetComponent<NetworkIdentity>().netId;
        RpcPlayHealingSkill(targetNetId, skill.SkillName);
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
        foreach (var skill in skills.Where(s => s.Hotkey != KeyCode.None))
        {
            if (Input.GetKeyDown(skill.Hotkey))
            {
                if (GetRemainingCooldown(skill.SkillName) > 0 || (!skill.ignoreGlobalCooldown && GetGlobalRemainingCooldown() > 0))
                {
                    continue;
                }

                if (localCooldowns.ContainsKey(skill.SkillName) && (float)NetworkTime.time < localCooldowns[skill.SkillName]) continue;
                if (!skill.ignoreGlobalCooldown && (float)NetworkTime.time < localGlobalCooldownEnd) continue;
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
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
        }
        _activeSkill = skill;
        skill.SetIndicatorVisibility(true);
        SetCursor(castCursor);
    }

    [ClientRpc]
    private void RpcPlayBasicAttackVFX(Vector3 startPos, Quaternion startRot, Vector3 targetPos, bool isCritical, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is BasicAttackSkill basicAttackSkill)
        {
            basicAttackSkill.PlayVFX(startPos, startRot, targetPos, isCritical, this);
        }
    }

    [ClientRpc]
    private void RpcSpawnProjectile(Vector3 startPos, Vector3 targetPos, string skillName)
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
    private void RpcApplySlowEffect(uint targetNetId, float duration, string skillName)
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
    private void RpcPlayTargetedStun(uint targetNetId, string skillName)
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
    private void RpcPlayHealingSkill(uint targetNetId, string skillName)
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
    private void RpcPlayAoeStun(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AreaOfEffectStunSkill aoeStunSkill)
        {
            aoeStunSkill.PlayEffect(position);
        }
    }

    [ClientRpc]
    private void RpcPlayAoeHeal(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AreaOfEffectHealSkill aoeHealSkill)
        {
            aoeHealSkill.PlayEffect(position);
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
            Debug.Log("[PlayerSkills] All skill selections cancelled");
        }
    }

    public void CancelSkillSelection()
    {
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
            _activeSkill = null;
            SetCursor(defaultCursor);
            Debug.Log("[PlayerSkills] Skill selection fully cancelled");
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
        Debug.Log($"[PlayerSkills] Cursor set to: {cursor?.name ?? "null"}");
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
                progress = Mathf.Clamp01(1 - ((float)NetworkTime.time - (localCooldowns[key] - skill.Cooldown)) / skill.Cooldown);
            }
            else
            {
                progress = skill.CooldownProgressNormalized;
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
}