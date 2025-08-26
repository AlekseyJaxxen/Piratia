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
            if (Input.GetKeyDown(skill.Hotkey) && !skill.IsOnCooldown())
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
        if (_activeSkill != null && _activeSkill.TargetIndicator != null)
        {
            Ray ray = _core.Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _core.interactableLayers))
            {
                _activeSkill.TargetIndicator.SetActive(true);
                _activeSkill.TargetIndicator.transform.position = hit.point + Vector3.up * 0.1f;

                float distance = Vector3.Distance(transform.position, hit.point);
                float scale = Mathf.Clamp(distance / _activeSkill.Range, 0.5f, 1f);
                _activeSkill.TargetIndicator.transform.localScale = Vector3.one * scale;
            }
            else
            {
                _activeSkill.TargetIndicator.SetActive(false);
            }
        }
    }

    public void CancelSkillSelection()
    {
        CancelAllSkillSelections();
        _isCasting = false;
        if (_castSkillCoroutine != null)
        {
            StopCoroutine(_castSkillCoroutine);
            _castSkillCoroutine = null;
        }
        Debug.Log("[PlayerSkills] Skill selection fully cancelled");
    }

    private void CancelAllSkillSelections()
    {
        foreach (var skill in skills)
        {
            if (skill.TargetIndicator != null)
            {
                skill.TargetIndicator.transform.localScale = Vector3.one;
                skill.SetIndicatorVisibility(false);
            }
        }
        _activeSkill = null;
        Debug.Log("[PlayerSkills] All skill selections cancelled");
    }

    private void SetCursor(Texture2D cursorTexture)
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
            Debug.Log($"[PlayerSkills] Cursor set to: {cursorTexture.name}");
        }
        else
        {
            Debug.LogWarning("[PlayerSkills] Cursor texture is null");
        }
    }

    public IEnumerator CastSkill(Vector3? targetPosition, GameObject targetObject, ISkill skillToCast)
    {
        _isCasting = true;
        _castSkillCoroutine = StartCoroutine(_core.ActionSystem.TryStartAction(PlayerAction.SkillCast, targetPosition, targetObject, skillToCast) ? WaitForSkillCast(targetPosition, targetObject) : null);

        if (_castSkillCoroutine != null)
        {
            yield return _castSkillCoroutine;
        }

        _isCasting = false;
        _castSkillCoroutine = null;
    }

    private IEnumerator WaitForSkillCast(Vector3? targetPosition, GameObject targetObject)
    {
        if (targetObject != null)
        {
            while (targetObject != null && !_core.isDead && _core.ActionSystem.CurrentAction == PlayerAction.SkillCast)
            {
                yield return null;
            }
        }
        else if (targetPosition.HasValue)
        {
            while (_core.ActionSystem.CurrentAction == PlayerAction.SkillCast)
            {
                yield return null;
            }
        }
    }

    [Command]
    public void CmdExecuteSkill(PlayerCore caster, Vector3? targetPosition, uint targetNetId, string skillName)
    {
        Debug.Log($"[PlayerSkills] CmdExecuteSkill called: skill={skillName}, caster.isOwned={caster.netIdentity.isOwned}, server={netIdentity.isServer}");
        if (!netIdentity.isServer)
        {
            Debug.LogError($"[PlayerSkills] CmdExecuteSkill called on client for skill {skillName}. This should only run on the server.");
            return;
        }

        if (caster == null)
        {
            Debug.LogWarning($"[PlayerSkills] Caster is null in CmdExecuteSkill for skill {skillName}");
            return;
        }

        if (!caster.netIdentity.isOwned && !netIdentity.isServer)
        {
            Debug.LogWarning($"[PlayerSkills] Caster lacks authority for skill {skillName}");
            return;
        }

        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogWarning($"[PlayerSkills] CharacterStats component missing on caster for skill {skillName}");
            return;
        }

        SkillBase skill = this.skills.Find(s => s.SkillName == skillName);
        if (skill == null)
        {
            Debug.LogWarning($"[PlayerSkills] Skill {skillName} not found in skills list");
            return;
        }

        if (!stats.HasEnoughMana(skill.ManaCost))
        {
            Debug.LogWarning($"[PlayerSkills] Not enough mana for skill {skillName}: {stats.currentMana}/{skill.ManaCost}");
            return;
        }

        bool isCritical = false;
        if (skill is BasicAttackSkill)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                Debug.LogWarning($"[PlayerSkills] Target with netId {targetNetId} not found on server for skill {skillName}");
                return;
            }

            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
            if (targetCore != null && casterCore != null && casterCore.team != targetCore.team)
            {
                Health targetHealth = targetIdentity.GetComponent<Health>();
                if (targetHealth != null)
                {
                    isCritical = Random.value <= stats.criticalHitChance;
                    int damage = isCritical ? stats.maxAttack * 2 : Random.Range(stats.minAttack, stats.maxAttack + 1);
                    Debug.Log($"[PlayerSkills] Applying damage: {damage} to {targetCore.playerName}");
                    targetHealth.TakeDamage(damage, skill.SkillDamageType, isCritical);
                }
                RpcPlayBasicAttackVFX(caster.transform.position, caster.transform.rotation, targetIdentity.transform.position, isCritical, skillName);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkills] Attack ignored: invalid target or same team for skill {skillName}");
            }
        }
        else if (skill is ProjectileDamageSkill projectileSkill)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                Debug.LogWarning($"[PlayerSkills] Target with netId {targetNetId} not found on server for skill {skillName}");
                return;
            }

            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
            if (targetCore != null && casterCore != null && casterCore.team != targetCore.team)
            {
                Health targetHealth = targetIdentity.GetComponent<Health>();
                if (targetHealth != null)
                {
                    isCritical = Random.value <= stats.criticalHitChance;
                    int finalDamage = skill.SkillDamageType == DamageType.Magic
                        ? Mathf.RoundToInt(projectileSkill.damageAmount * stats.magicDamageMultiplier)
                        : projectileSkill.damageAmount + (stats.strength * 2);
                    Debug.Log($"[PlayerSkills] Applying damage: {finalDamage} to {targetCore.playerName}");
                    targetHealth.TakeDamage(finalDamage, skill.SkillDamageType, isCritical);
                }
                RpcSpawnProjectile(caster.transform.position, targetIdentity.transform.position, skillName);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkills] Projectile ignored: invalid target or same team for skill {skillName}");
            }
        }
        else if (skill is SlowSkill slowSkill)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                Debug.LogWarning($"[PlayerSkills] Target with netId {targetNetId} not found on server for skill {skillName}");
                return;
            }

            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
            if (targetCore != null && casterCore != null && casterCore.team != targetCore.team)
            {
                Health targetHealth = targetIdentity.GetComponent<Health>();
                if (targetHealth != null)
                {
                    isCritical = Random.value <= stats.criticalHitChance;
                    int finalDamage = skill.SkillDamageType == DamageType.Magic
                        ? Mathf.RoundToInt(slowSkill.baseDamage * stats.magicDamageMultiplier * SlowSkill.DAMAGE_MULTIPLIER)
                        : Mathf.RoundToInt((slowSkill.baseDamage + (stats.strength * 2)) * SlowSkill.DAMAGE_MULTIPLIER);
                    Debug.Log($"[PlayerSkills] Applying damage: {finalDamage} to {targetCore.playerName}");
                    targetHealth.TakeDamage(finalDamage, skill.SkillDamageType, isCritical);
                }
                targetCore.ApplySlow(slowSkill.slowPercentage, slowSkill.slowDuration);
                RpcApplySlowEffect(targetIdentity.netId, slowSkill.slowDuration, skillName);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkills] Slow ignored: invalid target or same team for skill {skillName}");
            }
        }
        else if (skill is TargetedStunSkill targetedStunSkill)
        {
            if (caster.isStunned)
            {
                Debug.LogWarning("[PlayerSkills] Caster is stunned and cannot use TargetedStunSkill");
                return;
            }

            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                Debug.LogWarning($"[PlayerSkills] Target with netId {targetNetId} not found on server for skill {skillName}");
                return;
            }

            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
            if (targetCore != null && casterCore != null && casterCore.team != targetCore.team)
            {
                Debug.Log($"[PlayerSkills] Applying stun to {targetCore.playerName} for {targetedStunSkill.stunDuration}s");
                targetCore.ApplyControlEffect(ControlEffectType.Stun, targetedStunSkill.stunDuration);
                RpcPlayTargetedStun(targetIdentity.netId, skillName);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkills] Stun ignored: invalid target or same team for skill {skillName}");
            }
        }
        else if (skill is HealingSkill healingSkill)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                Debug.LogWarning($"[PlayerSkills] Target with netId {targetNetId} not found on server for skill {skillName}");
                return;
            }

            Health targetHealth = targetIdentity.GetComponent<Health>();
            PlayerCore targetCore = targetIdentity.GetComponent<PlayerCore>();
            PlayerCore casterCore = connectionToClient.identity.GetComponent<PlayerCore>();
            if (targetHealth != null && targetCore != null && casterCore != null && casterCore.team == targetCore.team)
            {
                Debug.Log($"[PlayerSkills] Healing {targetCore.playerName} for {healingSkill.healAmount}");
                targetHealth.Heal(healingSkill.healAmount);
                RpcPlayHealingSkill(targetIdentity.netId, skillName);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkills] Heal ignored: invalid target or different team for skill {skillName}");
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
}