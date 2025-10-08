using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Events;
public partial class PlayerSkills : NetworkBehaviour
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
    // Global cooldown removed - players can cast spells quickly
    public bool IsSkillSelected => _activeSkill != null;
    public ISkill ActiveSkill => _activeSkill;
    private Dictionary<string, float> localCooldowns = new Dictionary<string, float>();
    private float localGlobalCooldownEnd = 0f;
    [SyncVar(hook = nameof(OnInvisibilityChanged))] public bool _isInvisible;
    private Coroutine _invisibilityCoroutine;
    public readonly SyncDictionary<string, bool> toggleBuffStates = new SyncDictionary<string, bool>();
    [HideInInspector] public UnityEvent<string, bool> OnToggleBuffChanged = new UnityEvent<string, bool>();
    [SyncVar(hook = nameof(OnPlayerLayerChanged))] private int _playerLayer;
    [SyncVar] public int _originalLayer;
    private void Awake()
    {
        _skillLastUseTimes.OnChange += OnCooldownChanged;
        toggleBuffStates.OnChange += OnToggleBuffStateChanged;
        InitPredictionSystem(); // Инициализируем систему предсказания
    }
    private void Start()
    {
        _core = GetComponent<PlayerCore>();
        _playerLayer = gameObject.layer;
        _originalLayer = gameObject.layer;
        StartCoroutine(InitializeSkills());
    }
    private void OnPlayerLayerChanged(int oldLayer, int newLayer)
    {
        // Player layer changed
        gameObject.layer = newLayer;
        if (isServer)  // Force-сет на сервере (hook не всегда срабатывает)
        {
            gameObject.layer = newLayer;
            // Server force layer
        }
    }
    private void OnInvisibilityChanged(bool oldValue, bool newValue)
    {
        // Invisibility changed
        SkillBase skill = skills.Find(s => s.SkillName == "Invisibility");
        if (skill != null)
        {
            skill.ApplyInvisibilityEffect(newValue);
        }
    }
    private void OnToggleBuffStateChanged(SyncDictionary<string, bool>.Operation op, string skillName, bool isActive)
    {
        // ToggleBuff changed
        OnToggleBuffChanged.Invoke(skillName, isActive);
        if (skillName == "Invisibility")
        {
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill != null)
            {
                skill.ApplyInvisibilityEffect(isActive);
            }
        }
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
            skill.Hotkey = KeyCode.None;
            if (isServer)
            {
                _skillLastUseTimes[skill.SkillName] = 0f;
            }
        }
        if (isLocalPlayer)
        {
            SetCursor(defaultCursor);
        }

        if (isServer)
        {
            foreach (var skill in skills)
            {
                _skillLastUseTimes[skill.SkillName] = float.NegativeInfinity;
            }
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
        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    skill.CleanupIndicators();
                }
            }
        }
        if (_invisibilityCoroutine != null) StopCoroutine(_invisibilityCoroutine);
        SetInvisible(false);
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
        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    skill.CleanupIndicators();
                }
            }
        }
        if (_invisibilityCoroutine != null) StopCoroutine(_invisibilityCoroutine);
        SetInvisible(false);
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
        if (!caster.CanCastSkill(caster.Skills.skills.Find(s => s.SkillName == skillName)))
        {
            // Cannot cast skill - invalid conditions
            return;
        }
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null)
        {
            Debug.LogWarning($"[PlayerSkills] Skill {skillName} not found on {gameObject.name}");
            return;
        }
        if (GetRemainingCooldown(skillName) > 0)
        {
            // Skill on cooldown
            return;
        }
        // Global cooldown check removed - players can cast spells quickly
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(skill.ManaCost))
        {
            // Not enough mana
            return;
        }
        if (toggleBuffStates.ContainsKey("Invisibility") && toggleBuffStates["Invisibility"])
        {
            Debug.Log($"[PlayerSkills] Interrupting invisibility due to skill cast: {skillName} on {gameObject.name}");
            SetToggleBuff("Invisibility", false);
            RpcSetInvisibilityVisibility(false, _core.team, _originalLayer); // Добавлено: явный вызов для модели
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
                distance = Vector3.Distance(transform.position, targetObject.transform.position); // Full distance with Y
            }
            else if (targetPosition.HasValue)
            {
                distance = Vector3.Distance(transform.position, targetPosition.Value); // Full distance with Y
            }
            if (distance > skill.Range + tolerance)
            {
                Debug.LogWarning($"[PlayerSkills] Skill {skillName} out of range: distance={distance}, range={skill.Range} on {gameObject.name}");
                return;
            }
            if (distance > skill.Range + warningThreshold)
            {
                Debug.LogWarning($"Skill {skillName} used with high tolerance: Distance = {distance}, Range = {skill.Range}, Tolerance = {tolerance} on {gameObject.name}");
            }
        }
        if (stats != null) stats.SpendMana(skill.ManaCost);
        StartSkillCooldown(skillName);
        // Global cooldown removed
        if (skill.CastTime > 0)
        {
            StartCoroutine(CastSkillCoroutine(skill, targetPosition, targetObject, weight));
        }
        else
        {
            // Устанавливаем ссылку на игрока для динамического расчета кулдауна
            skill.SetPlayer(caster);
            skill.ExecuteOnServer(caster, targetPosition, targetObject, weight);
            
            // Устанавливаем кулдаун для мгновенных скиллов (SelfBuff, ToggleBuff и т.д.)
            StartSkillCooldown(skillName);
            // Global cooldown removed
            
            if (!(skill is BasicAttackSkill))
            {
                RpcCancelSkillSelection();
                RpcConsumeItemFromSkill(skillName);
            }
        }
    }




    private IEnumerator CastSkillCoroutine(SkillBase skill, Vector3? targetPosition, GameObject targetObject, int weight)
    {
        _isCasting = true;
        yield return new WaitForSeconds(skill.CastTime);
        _isCasting = false;
        
        // КРИТИЧНО: Проверяем состояние еще раз после каста для предотвращения каста под ставом/молчанием
        if (_core.isStunned)
        {
            Debug.LogWarning($"[PlayerSkills] Skill cast interrupted: {skill.SkillName} canceled due to stun: {_core.name}");
            // Возвращаем ману и очищаем кулдаун если каст был прерван
            CharacterStats stats = _core.GetComponent<CharacterStats>();
            if (stats != null) stats.RestoreMana(skill.ManaCost);
            _skillLastUseTimes.Remove(skill.SkillName); // Очищаем кулдаун для каста
            RpcCancelSkillSelection();
            yield break;
        }
        
        if (_core.isSilenced && !(skill is BasicAttackSkill))
        {
            Debug.LogWarning($"[PlayerSkills] Skill cast interrupted: {skill.SkillName} canceled due to silence: {_core.name}");
            // Возвращаем ману и очищаем кулдаун если каст был прерван
            CharacterStats stats = _core.GetComponent<CharacterStats>();
            if (stats != null) stats.RestoreMana(skill.ManaCost);
            _skillLastUseTimes.Remove(skill.SkillName); // Очищаем кулдаун для каста
            RpcCancelSkillSelection();
            yield break;
        }
        
        // Устанавливаем ссылку на игрока для динамического расчета кулдауна
        skill.SetPlayer(_core);
        skill.ExecuteOnServer(_core, targetPosition, targetObject, weight);
        
        // Кулдаун уже установлен в CmdExecuteSkill, дублировать не нужно
        RpcCancelSkillSelection();
        RpcConsumeItemFromSkill(skill.SkillName);
    }
    [ClientRpc]
    private void RpcConsumeItemFromSkill(string skillName)
    {
        if (!isLocalPlayer) return;
        var ui = GetComponentInChildren<PlayerUI>();
        if (ui != null)
        {
            var hotbarButtons = ui.GetSkillButtons2().Concat(ui.GetSkillButtons3());
            foreach (var btn in hotbarButtons)
            {
                if (btn.item != null && btn.item.skillEffect != null && btn.item.skillEffect.SkillName == skillName)
                {
                    _core.CmdConsumeItem(btn.item.id, btn.itemSlotIndex);
                    Debug.Log($"[PlayerSkills] Consumed item {btn.item.itemName} after {skillName} cast");
                    break;
                }
            }
        }
    }
    public float GetRemainingCooldown(string skillName)
    {
        if (_skillLastUseTimes.ContainsKey(skillName))
        {
            return Mathf.Max(0, skills.Find(s => s.SkillName == skillName).Cooldown - ((float)NetworkTime.time - _skillLastUseTimes[skillName]));
        }
        return 0f;
    }
    [Server]
    public void StartSkillCooldown(string skillName)
    {
        _skillLastUseTimes[skillName] = (float)NetworkTime.time;
    }
    // StartGlobalCooldown method removed - global cooldown disabled
    
    [Server]
    public void ClearSkillCooldown(string skillName)
    {
        _skillLastUseTimes.Remove(skillName);
    }
    private void HandleSkills()
    {
        if (skills == null || skills.Count == 0) return;
        // Removed cast time restriction - players can cast spells quickly
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
        if (!_core.CanCastSkill(skill))
        {
            Debug.LogWarning($"[PlayerSkills] Cannot select skill {((SkillBase)skill).SkillName}: player is dead, stunned, or silenced (and not BasicAttackSkill) on {gameObject.name}");
            return;
        }
        SkillBase s = (SkillBase)skill;
        if (GetRemainingCooldown(s.SkillName) > 0)
        {
            Debug.LogWarning($"[PlayerSkills] Cannot select {s.SkillName}: on cooldown");
            return;
        }
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
        }
        _activeSkill = s;
        s.SetIndicatorVisibility(true);
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
    public void RpcPlayTargetedRecovery(uint targetNetId, string skillName)
    {
        if (NetworkClient.spawned.ContainsKey(targetNetId))
        {
            NetworkIdentity targetIdentity = NetworkClient.spawned[targetNetId];
            SkillBase skill = skills.Find(s => s.SkillName == skillName);
            if (skill is TargetedRecoverySkill targetedRecoverySkill)
            {
                targetedRecoverySkill.PlayEffect(targetIdentity.gameObject, this);
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
        // Global cooldown UI update removed
        if (isLocalPlayer && skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    UpdateSkillUI(skill.SkillName);
                }
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
    // Оптимизация: кэширование для курсора
    private Texture2D _lastCursor = null;
    private Vector3 _lastMousePosition = Vector3.zero;
    private const float MOUSE_MOVEMENT_THRESHOLD = 5f; // Минимальное движение мыши для обновления
    
    private void UpdateCursor()
    {
        // Оптимизация: обновляем курсор только при движении мыши или с интервалом
        Vector3 currentMousePos = Input.mousePosition;
        bool mouseMoved = Vector3.Distance(currentMousePos, _lastMousePosition) > MOUSE_MOVEMENT_THRESHOLD;
        
        if ((float)NetworkTime.time - _lastCursorUpdate > cursorUpdateInterval || mouseMoved)
        {
            if (_core == null || _core.Camera == null || _core.Camera.CameraInstance == null) return;
            
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            Texture2D newCursor = defaultCursor; // По умолчанию
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                GameObject hitObject = hit.collider.gameObject;
                PlayerCore hitCore = hitObject.GetComponent<PlayerCore>();
                Monster hitMonster = hitObject.GetComponent<Monster>();
                
                // Check for dead allies on ReviveLayer
                if (hitObject.layer == LayerMask.NameToLayer("ReviveLayer"))
                {
                    hitCore = hitObject.GetComponentInParent<PlayerCore>();
                }
                
                // Check for enemies
                if ((hitCore != null && IsEnemy(hitCore) && hitCore.Skills != null && !hitCore.Skills._isInvisible && !hitCore.isDead) || hitMonster != null)
                {
                    newCursor = attackCursor;
                }
                // Check for dead allies (for revive)
                else if (hitCore != null && IsAlly(hitCore) && hitCore.isDead && _activeSkill is ReviveSkill)
                {
                    newCursor = castCursor;
                }
                // Check for living allies (for other skills)
                else if (hitCore != null && IsAlly(hitCore) && !hitCore.isDead && _activeSkill != null && _activeSkill is not ReviveSkill)
                {
                    newCursor = castCursor;
                }
            }
            
            // Оптимизация: меняем курсор только если он действительно изменился
            if (_lastCursor != newCursor)
            {
                SetCursor(newCursor);
                _lastCursor = newCursor;
            }
            
            _lastMousePosition = currentMousePos;
            _lastCursorUpdate = (float)NetworkTime.time;
        }
    }
    private void SetCursor(Texture2D cursor)
    {
        if (cursor != null)
        {
            Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
        }
    }
    private void OnCooldownChanged(SyncDictionary<string, float>.Operation op, string key, float item)
    {
        if (isLocalPlayer) UpdateSkillUI(key);
    }
    // OnGlobalCooldownChanged method removed - global cooldown disabled
    private void UpdateSkillUI(string key)
    {
        if (skills == null || PlayerUI.Instance == null) return;
        
        SkillBase skill = skills.Find(s => s != null && s.SkillName == key);
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
    // UpdateGlobalCooldownUI method removed - global cooldown disabled
    [ClientRpc]
    private void RpcCancelSkillSelection()
    {
        CancelSkillSelection();
    }
    public void StartLocalCooldown(string skillName, float cooldown, bool useGlobal)
    {
        localCooldowns[skillName] = (float)NetworkTime.time + cooldown;
        // Global cooldown removed - useGlobal parameter ignored
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
    [ClientRpc]
    private void RpcUpdateBuffIndicator(string skillName, bool isActive)
    {
        if (!isLocalPlayer) return;
        var ui = GetComponentInChildren<PlayerUI>();
        if (ui != null)
        {
            var hotbarButtons = ui.GetSkillButtons2().Concat(ui.GetSkillButtons3());
            foreach (var btn in hotbarButtons)
            {
                if (btn.skill != null && btn.skill.SkillName == skillName && btn.skill.SkillCastType == SkillBase.CastType.ToggleBuff)
                {
                    btn.UpdateBuffIndicator(skillName, isActive);
                    Debug.Log($"[PlayerSkills] RpcUpdateBuffIndicator: {skillName} set to {isActive} for button {btn.buttonIndex}, layer={gameObject.layer}");
                    break;
                }
            }
        }
    }
    [Command]
    public void CmdToggleInvisibility(bool enable, string skillName)
    {
        CmdToggleBuff(skillName, enable);
    }
    [Command]
    public void CmdToggleBuff(string skillName, bool enable)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null || skill.SkillCastType != SkillBase.CastType.ToggleBuff)
        {
            Debug.LogWarning($"[PlayerSkills] Cannot toggle {skillName}: skill not found or not a ToggleBuff on {gameObject.name}");
            return;
        }
        if (enable)
        {
            if (GetRemainingCooldown(skillName) > 0)
            {
                Debug.Log($"[PlayerSkills] Cannot enable {skillName}: on cooldown {GetRemainingCooldown(skillName)}s on {gameObject.name}");
                return;
            }
            CharacterStats stats = GetComponent<CharacterStats>();
            if (stats != null && !stats.HasEnoughMana(skill.ManaCost))
            {
                // Not enough mana
                return;
            }
            if (stats != null) stats.SpendMana(skill.ManaCost);
            StartSkillCooldown(skillName);
            float duration = 10f;
            var durationField = skill.GetType().GetField("duration");
            if (durationField != null)
            {
                duration = (float)durationField.GetValue(skill);
            }
            if (_invisibilityCoroutine != null) StopCoroutine(_invisibilityCoroutine);
            _invisibilityCoroutine = StartCoroutine(ToggleBuffDuration(skillName, duration));
        }
        SetToggleBuff(skillName, enable);
    }
    [Server]
    public void SetToggleBuff(string skillName, bool value)
    {
        toggleBuffStates[skillName] = value;
        Debug.Log($"[PlayerSkills] SetToggleBuff: {skillName} = {value} on {gameObject.name}, toggleBuffStates: {string.Join(", ", toggleBuffStates.Select(kv => $"{kv.Key}: {kv.Value}"))}");
        if (skillName == "Invisibility")
        {
            _isInvisible = value;
            _playerLayer = value ? LayerMask.NameToLayer("Ignore Raycast") : _originalLayer;
            RpcSetInvisibilityState(value);
            RpcSetInvisibilityVisibility(value, _core.team, _originalLayer);
            RpcForceLayer(_playerLayer);

            if (value)  // При входе в невидимость очисти target у врагов
            {
                ClearEnemyTargets();
            }
        }
        RpcUpdateBuffIndicator(skillName, value);
    }
    [ClientRpc]
    public void RpcSetInvisibilityState(bool value)
    {
        _isInvisible = value;
        Debug.Log($"[PlayerSkills] RpcSetInvisibilityState {value} on {gameObject.name}");
    }
    private IEnumerator ToggleBuffDuration(string skillName, float duration)
    {
        yield return new WaitForSeconds(duration);
        SetToggleBuff(skillName, false);
    }
    [Server]
    private void SetInvisible(bool value)
    {
        SetToggleBuff("Invisibility", value);
    }
    [Command]
    public void CmdInterruptInvisibility()
    {
        Debug.Log($"[PlayerSkills] CmdInterruptInvisibility called on {gameObject.name}, _isInvisible={_isInvisible}");
        SetToggleBuff("Invisibility", false);
    }
    [ClientRpc]
    public void RpcRevealPlayer(bool isVisible, int layer)
    {
        PlayerCore localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerCore>();
        bool isAlly = localPlayer != null && IsAllyToPlayer(_core, localPlayer);
        Transform modelsTransform = transform.Find("Models");
        if (modelsTransform != null)
        {
            modelsTransform.gameObject.SetActive(isVisible || isAlly || this.isLocalPlayer);
        }
        else
        {
            Debug.LogWarning($"[PlayerSkills] GameObject 'Models' not found on {gameObject.name}");
        }
        Debug.Log($"[PlayerSkills] RpcRevealPlayer: isVisible={isVisible}, layer={layer}, isAlly={isAlly}, isLocalPlayer={this.isLocalPlayer} on {gameObject.name}");
    }
    [ClientRpc]
    public void RpcSetInvisibilityVisibility(bool isInvisible, PlayerTeam targetTeam, int originalLayer)
    {
        PlayerCore localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerCore>();
        bool isAlly = localPlayer != null && IsAllyToPlayer(_core, localPlayer);
        Transform modelsTransform = transform.Find("Models");
        if (modelsTransform != null)
        {
            bool shouldBeVisible = !isInvisible || isAlly || this.isLocalPlayer;
            modelsTransform.gameObject.SetActive(shouldBeVisible);
            Debug.Log($"[PlayerSkills] RpcSetInvisibilityVisibility: isInvisible={isInvisible}, shouldBeVisible={shouldBeVisible}, isAlly={isAlly}, isLocalPlayer={this.isLocalPlayer}, targetTeam={targetTeam}, localPlayerTeam={(localPlayer != null ? localPlayer.team.ToString() : "null")} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[PlayerSkills] GameObject 'Models' not found on {gameObject.name}");
        }
    }
    [ClientRpc]
    public void RpcForceLayer(int layer)
    {
        gameObject.layer = layer;
        Debug.Log($"[PlayerSkills] RpcForceLayer {layer} on {gameObject.name}");
    }
    // GetGlobalRemainingCooldown method removed - global cooldown disabled
    [Server]
    public void SetPlayerLayer(int layer)
    {
        _playerLayer = layer;
        Debug.Log($"[PlayerSkills] Server set layer: {layer} on {gameObject.name}");
    }
    [Server]
    private void ClearEnemyTargets()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                PlayerCore enemy = conn.identity.GetComponent<PlayerCore>();
                if (enemy != null && IsEnemy(enemy) && enemy.Combat.Target == gameObject)
                {
                    enemy.Combat.ClearTarget();
                    Debug.Log($"[PlayerSkills] Cleared target from {enemy.gameObject.name} due to invis");
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if target player is an enemy
    /// Solo players are enemies to each other
    /// </summary>
    private bool IsEnemy(PlayerCore target)
    {
        if (target == null) return false;
        
        // A player is never an enemy to themselves
        if (_core == target)
        {
            return false;
        }
        
        // Check party membership first (highest priority)
        if (!string.IsNullOrEmpty(_core.partyId) && !string.IsNullOrEmpty(target.partyId) && 
            _core.partyId == target.partyId)
        {
            return false; // Party members are never enemies
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(_core.guildId) && !string.IsNullOrEmpty(target.guildId) && 
            _core.guildId == target.guildId)
        {
            return false; // Guild members are never enemies
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(_core.factionId) && !string.IsNullOrEmpty(target.factionId) && 
            _core.factionId == target.factionId)
        {
            return false; // Faction members are never enemies
        }
        
        // Check basic team logic
        if (_core.team == target.team && _core.team != PlayerTeam.Solo)
        {
            return false; // Same team members are not enemies
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (_core.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return true; // Solo players are enemies to each other
        }
        
        // Different teams are enemies
        return _core.team != target.team;
    }
    
    /// <summary>
    /// Checks if target player is an ally
    /// Supports dynamic teams: guild, party, faction, and basic teams
    /// </summary>
    private bool IsAlly(PlayerCore target)
    {
        if (target == null) return false;
        
        // A player is always an ally to themselves
        if (_core == target)
        {
            return true;
        }
        
        // Check basic team logic first
        if (_core.team == target.team && _core.team != PlayerTeam.Solo)
        {
            return true;
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(_core.guildId) && _core.guildId == target.guildId)
        {
            return true;
        }
        
        // Check party membership
        if (!string.IsNullOrEmpty(_core.partyId) && _core.partyId == target.partyId)
        {
            return true;
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(_core.factionId) && _core.factionId == target.factionId)
        {
            return true;
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (_core.team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return false;
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if target player is an ally to the observer player
    /// Used for invisibility visibility checks
    /// </summary>
    private bool IsAllyToPlayer(PlayerCore target, PlayerCore observer)
    {
        if (target == null || observer == null) return false;
        
        // A player is always an ally to themselves
        if (target == observer)
        {
            return true;
        }
        
        // Check basic team logic first
        if (target.team == observer.team && target.team != PlayerTeam.Solo)
        {
            return true;
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(target.guildId) && target.guildId == observer.guildId)
        {
            return true;
        }
        
        // Check party membership
        if (!string.IsNullOrEmpty(target.partyId) && target.partyId == observer.partyId)
        {
            return true;
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(target.factionId) && target.factionId == observer.factionId)
        {
            return true;
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (target.team == PlayerTeam.Solo && observer.team == PlayerTeam.Solo)
        {
            return false;
        }
        
        return false;
    }

    public void StartJumpCoroutine(Vector3 start, Vector3 end, int weight, float jumpDuration, float heightMultiplier)
    {
        StartCoroutine(PerformJump(_core, start, end, weight, jumpDuration, heightMultiplier));
    }

    private IEnumerator PerformJump(PlayerCore caster, Vector3 start, Vector3 end, int weight, float jumpDuration, float heightMultiplier)
    {
        RpcDisableAgentAndNT();
        caster.Movement.Agent.enabled = false;
        caster.GetComponent<NetworkTransformHybrid>().enabled = false;

        float elapsed = 0f;
        float distance = Vector3.Distance(start, end);
        float heightDiff = end.y - start.y;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y = start.y + t * heightDiff + Mathf.Sin(t * Mathf.PI) * (distance / 2f) * heightMultiplier;

            caster.transform.position = pos;
            RpcSetPosition(pos);

            yield return null;
        }
        caster.transform.position = end;
        RpcSetPosition(end);

        caster.Movement.Agent.enabled = true;
        caster.Movement.Agent.Warp(end);
        caster.GetComponent<NetworkTransformHybrid>().enabled = true;
        RpcEnableAgentAndNT(end);

        caster.ClearStunEffect();
    }

    [ClientRpc]
    private void RpcSetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    [ClientRpc]
    private void RpcDisableAgentAndNT()
    {
        _core.Movement.Agent.enabled = false;
        GetComponent<NetworkTransformHybrid>().enabled = false;
    }

    [ClientRpc]
    private void RpcEnableAgentAndNT(Vector3 pos)
    {
        transform.position = pos;
        _core.Movement.Agent.enabled = true;
        _core.Movement.Agent.Warp(pos);
        GetComponent<NetworkTransformHybrid>().enabled = true;
    }

}