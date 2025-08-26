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

    [SyncVar]
    private SyncDictionary<string, float> _skillLastUseTimes = new SyncDictionary<string, float>();

    public bool IsSkillSelected => _activeSkill != null;
    public ISkill ActiveSkill => _activeSkill;

    private void Start()
    {
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

        if (stats.characterClass == null)
        {
            Debug.LogError("[PlayerSkills] CharacterStats.characterClass is null or not set!");
            yield break;
        }

        skills = SkillManager.Instance.GetSkillsForClass(stats.characterClass);
        Debug.Log($"[PlayerSkills] Loaded {skills.Count} skills for class {stats.characterClass}: {string.Join(", ", skills.Select(s => s != null ? s.SkillName : "null"))}");

        foreach (var skill in skills)
        {
            if (skill == null)
            {
                Debug.LogError("[PlayerSkills] Skill in skills list is null!");
                continue;
            }
            skill.Init(_core);
            _skillLastUseTimes[skill.SkillName] = 0f; // Инициализация времени использования
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

    public void HandleSkills()
    {
        if (!isLocalPlayer || _isCasting || _core.isDead || _core.isStunned)
        {
            if (!isLocalPlayer) Debug.Log("[PlayerSkills] Input ignored: not local player");
            if (_isCasting) Debug.Log("[PlayerSkills] Input ignored: is casting");
            if (_core.isDead) Debug.Log("[PlayerSkills] Input ignored: is dead");
            if (_core.isStunned) Debug.Log("[PlayerSkills] Input ignored: is stunned");
            return;
        }

        foreach (var skill in skills)
        {
            if (Input.GetKeyDown(skill.Hotkey) && !IsSkillOnCooldown(skill))
            {
                Debug.Log($"[PlayerSkills] Skill {skill.SkillName} selected with hotkey {skill.Hotkey}");
                CancelAllSkillSelections();
                _activeSkill = skill;
                _activeSkill.SetIndicatorVisibility(true);
            }
        }

        if (_activeSkill != null)
        {
            UpdateTargetIndicator();

            if (Input.GetMouseButtonDown(1))
            {
                CancelSkillSelection();
                Debug.Log("[PlayerSkills] Skill selection cancelled via right-click");
            }
        }
        else
        {
            UpdateCursor();
        }

        if (_activeSkill != null && Input.GetMouseButtonDown(0))
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                GameObject hitObject = hit.collider.gameObject;
                Debug.Log($"[PlayerSkills] Left click detected, hit object: {hitObject.name}, tag: {hitObject.tag}");
            }
        }
    }

    private bool IsSkillOnCooldown(SkillBase skill)
    {
        if (_skillLastUseTimes.TryGetValue(skill.SkillName, out float lastUseTime))
        {
            Debug.Log($"[PlayerSkills] Checking cooldown for {skill.SkillName}, lastUseTime={lastUseTime}, currentTime={Time.time}, cooldown={skill.Cooldown}");
            return Time.time - lastUseTime < skill.Cooldown;
        }
        return false;
    }

    [Command]
    public void CmdExecuteSkill(PlayerCore caster, Vector3? targetPosition, uint targetNetId, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill == null)
        {
            Debug.LogWarning($"[PlayerSkills] Skill {skillName} not found");
            return;
        }

        if (IsSkillOnCooldown(skill))
        {
            Debug.LogWarning($"[PlayerSkills] Skill {skillName} is on cooldown. Ignoring.");
            return;
        }

        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats != null && !stats.HasEnoughMana(skill.ManaCost))
        {
            Debug.LogWarning($"[PlayerSkills] Not enough mana for {skillName}: {stats.currentMana}/{skill.ManaCost}");
            return;
        }

        NetworkIdentity targetIdentity = null;
        if (targetNetId != 0 && NetworkServer.spawned.ContainsKey(targetNetId))
        {
            targetIdentity = NetworkServer.spawned[targetNetId];
        }

        if (skill is ProjectileDamageSkill || skill is SlowSkill || skill is TargetedStunSkill)
        {
            if (targetIdentity == null)
            {
                Debug.LogWarning($"[PlayerSkills] Target netId {targetNetId} not found for {skillName}");
                return;
            }

            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            if (targetCore == null || caster.team == targetCore.team)
            {
                Debug.LogWarning($"[PlayerSkills] Invalid target or same team for {skillName}");
                return;
            }

            if (skill is ProjectileDamageSkill projectileSkill)
            {
                Debug.Log($"[PlayerSkills] Spawning projectile for {skillName} targeting {targetIdentity.gameObject.name}");
                RpcSpawnProjectile(caster.transform.position, targetIdentity.transform.position, skillName);
                Health targetHealth = targetIdentity.GetComponent<Health>();
                if (targetHealth != null)
                {
                    float critChance = stats != null ? stats.criticalHitChance : 0f;
                    bool isCritical = Random.value < critChance;
                    int damage = isCritical ? Mathf.RoundToInt(projectileSkill.damageAmount * 1.5f) : projectileSkill.damageAmount;
                    targetHealth.TakeDamage(damage, skill.SkillDamageType, isCritical);
                }
            }
            else if (skill is SlowSkill slowSkill)
            {
                Debug.Log($"[PlayerSkills] Applying slow to {targetCore.playerName} for {slowSkill.slowDuration}s");
                targetCore.ApplySlow(slowSkill.slowPercentage, slowSkill.slowDuration);
                RpcApplySlowEffect(targetNetId, slowSkill.slowDuration, skillName);
            }
            else if (skill is TargetedStunSkill targetedStunSkill)
            {
                Debug.Log($"[PlayerSkills] Applying stun to {targetCore.playerName} for {targetedStunSkill.stunDuration}s");
                targetCore.ApplyControlEffect(ControlEffectType.Stun, targetedStunSkill.stunDuration);
                RpcPlayTargetedStun(targetNetId, skillName);
            }
        }
        else if (skill is HealingSkill healingSkill)
        {
            if (targetIdentity == null)
            {
                Debug.LogWarning($"[PlayerSkills] Target netId {targetNetId} not found for {skillName}");
                return;
            }

            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
            if (targetHealth != null && targetCore != null && casterCore != null && casterCore.team == targetCore.team)
            {
                Debug.Log($"[PlayerSkills] Healing {targetCore.playerName} for {healingSkill.healAmount}");
                targetHealth.Heal(healingSkill.healAmount);
                RpcPlayHealingSkill(targetNetId, skillName);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkills] Heal ignored: invalid target or different team for {skillName}");
            }
        }
        else if (skill is AreaOfEffectStunSkill || skill is AreaOfEffectHealSkill)
        {
            if (!targetPosition.HasValue)
            {
                Debug.LogWarning($"[PlayerSkills] Target position is null for {skillName}");
                return;
            }

            Collider[] hits = Physics.OverlapSphere(targetPosition.Value, skill.Range, caster.interactableLayers);
            foreach (Collider hit in hits)
            {
                NetworkIdentity identity = hit.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                PlayerCore targetCore = hit.GetComponent<PlayerCore>();
                if (targetCore == null) continue;

                PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
                if (skill is AreaOfEffectStunSkill aoeStunSkill && casterCore.team != targetCore.team)
                {
                    Debug.Log($"[PlayerSkills] Applying AOE stun to {targetCore.playerName} for {aoeStunSkill.stunDuration}s");
                    targetCore.ApplyControlEffect(ControlEffectType.Stun, aoeStunSkill.stunDuration);
                }
                else if (skill is AreaOfEffectHealSkill aoeHealSkill && casterCore.team == targetCore.team)
                {
                    Health targetHealth = hit.GetComponent<Health>();
                    if (targetHealth != null)
                    {
                        Debug.Log($"[PlayerSkills] Healing {targetCore.playerName} for {aoeHealSkill.healAmount}");
                        targetHealth.Heal(aoeHealSkill.healAmount);
                    }
                }
            }

            if (skill is AreaOfEffectStunSkill)
                RpcPlayAreaOfEffectStun(targetPosition.Value, skillName);
            else if (skill is AreaOfEffectHealSkill)
                RpcPlayAreaOfEffectHeal(targetPosition.Value, skillName);
        }

        if (stats != null)
        {
            stats.ConsumeMana(skill.ManaCost);
        }
        _skillLastUseTimes[skill.SkillName] = Time.time;
        skill.StartCooldown();
    }

    [ClientRpc]
    private void RpcPlayBasicAttackVFX(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, bool isCritical, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is BasicAttackSkill basicAttackSkill)
        {
            basicAttackSkill.PlayVFX(startPosition, startRotation, endPosition, isCritical);
        }
    }

    [ClientRpc]
    private void RpcPlayAreaOfEffectStun(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AreaOfEffectStunSkill aoeStunSkill)
        {
            aoeStunSkill.PlayEffect(position);
        }
    }

    [ClientRpc]
    private void RpcPlayAreaOfEffectHeal(Vector3 position, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is AreaOfEffectHealSkill aoeHealSkill)
        {
            aoeHealSkill.PlayEffect(position);
        }
    }

    [ClientRpc]
    private void RpcSpawnProjectile(Vector3 startPos, Vector3 targetPos, string skillName)
    {
        SkillBase skill = skills.Find(s => s.SkillName == skillName);
        if (skill is ProjectileDamageSkill projectileSkill)
        {
            projectileSkill.SpawnProjectile(startPos, targetPos);
        }
        else if (skill is SlowSkill slowSkill)
        {
            slowSkill.SpawnProjectile(startPos, targetPos);
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
                slowSkill.ApplySlowEffect(targetIdentity.gameObject, duration);
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
                targetedStunSkill.PlayEffect(targetIdentity.gameObject);
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

    private void Update()
    {
        if (isLocalPlayer) HandleSkills();
    }

    public void CancelAllSkillSelections()
    {
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
            _activeSkill = null;
            Debug.Log("[PlayerSkills] All skill selections cancelled");
        }
    }

    public void CancelSkillSelection()
    {
        if (_activeSkill != null)
        {
            _activeSkill.SetIndicatorVisibility(false);
            _activeSkill = null;
            Debug.Log("[PlayerSkills] Skill selection fully cancelled");
        }
    }

    private void UpdateTargetIndicator()
    {
        // Реализация обновления индикатора цели
    }

    private void UpdateCursor()
    {
        if (Time.time - _lastCursorUpdate > cursorUpdateInterval)
        {
            SetCursor(defaultCursor);
            _lastCursorUpdate = Time.time;
        }
    }

    private void SetCursor(Texture2D cursor)
    {
        Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
        Debug.Log($"[PlayerSkills] Cursor set to: {cursor?.name ?? "null"}");
    }
}