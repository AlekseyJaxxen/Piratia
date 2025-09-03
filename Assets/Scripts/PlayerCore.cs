using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;

public class PlayerCore : NetworkBehaviour
{
    [Header("Core Components")]
    public PlayerMovement Movement;
    public PlayerCombat Combat;
    public PlayerSkills Skills;
    public PlayerActionSystem ActionSystem;
    public PlayerCameraController Camera;
    public Health Health;
    public CharacterStats Stats;
    private HealthBarUI healthBarUI;
    [HideInInspector] public NameTagUI nameTagUI;
    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float _timeOfDeath;
    [Header("UI References")]
    [SerializeField]
    private DeathScreenUI deathScreenUI;
    [SerializeField]
    private Canvas mainCanvasReference;
    [Header("Dependencies")]
    public LayerMask interactableLayers;
    public LayerMask groundLayer;
    [Header("Visuals")]
    public Material localPlayerMaterial;
    public Material allyMaterial;
    public Material enemyMaterial;
    public GameObject deathVFXPrefab;
    [SerializeField] private Transform modelTransform;
    private Quaternion initialModelRotation;
    [SerializeField] private BoxCollider boxCollider;
    [SyncVar(hook = nameof(OnTeamChanged))]
    public PlayerTeam team = PlayerTeam.None;
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";
    [SyncVar(hook = nameof(OnDeathStateChanged))]
    public bool isDead = false;
    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;
    [SyncVar]
    private ControlEffectType currentControlEffect = ControlEffectType.None;
    [SyncVar]
    private float controlEffectEndTime = 0f;
    [SyncVar]
    private int currentEffectWeight = 0;
    [SyncVar]
    private float _slowPercentage = 0f;
    private float _originalSpeed = 0f;
    [Header("Mana Regeneration")]
    public float manaRegenInterval = 1f;
    public int manaRegenAmount = 5;
    private float _lastManaRegenTime;
    [SyncVar]
    protected Vector3 _initialSpawnPosition;
    private GameObject _teamIndicator;
    private TextMeshProUGUI _nameText;
    private PlayerUI_Team _playerUI_Team;
    public static PlayerCore localPlayerCoreInstance;
    [SerializeField] private BoxCollider reviveCollider;

    [SyncVar] public Vector3 deathPosition;

    [SerializeField] private ReviveRequestUI reviveRequestUI;

    [SyncVar] public float pendingReviveHpFraction = 0f;

    protected virtual void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        Skills = GetComponent<PlayerSkills>();
        ActionSystem = GetComponent<PlayerActionSystem>();
        Camera = GetComponent<PlayerCameraController>();
        Health = GetComponent<Health>();
        Stats = GetComponent<CharacterStats>();
        if (Movement != null) Movement.Init(this);
        if (Combat != null) Combat.Init(this);
        if (ActionSystem != null) ActionSystem.Init(this);
        if (Camera != null) Camera.Init(this);
        if (modelTransform != null)
        {
            initialModelRotation = modelTransform.localRotation;
        }
        boxCollider = GetComponent<BoxCollider>();
        reviveCollider = transform.Find("ReviveCollider")?.GetComponent<BoxCollider>();
        if (reviveCollider != null) reviveCollider.enabled = false;
        reviveCollider.enabled = false;
        reviveRequestUI = GetComponentInChildren<ReviveRequestUI>();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
        }
        if (NetworkServer.active) ServerUpdate();
    }

    [Server]
    protected virtual void ServerUpdate()
    {
        if (currentControlEffect != ControlEffectType.None && Time.time >= controlEffectEndTime)
        {
            ClearControlEffect();
        }
        if (Time.time >= _lastManaRegenTime + manaRegenInterval)
        {
            Stats.RestoreMana(manaRegenAmount);
            _lastManaRegenTime = Time.time;
        }
    }

    public override void OnStartLocalPlayer()
    {
        localPlayerCoreInstance = this;
        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui != null && !ui.gameObject.activeSelf)
        {
            ui.gameObject.SetActive(true);
        }
        if (Camera != null)
        {
            Camera.Init(this);
        }
        base.OnStartLocalPlayer();
        int localPlayerLayer = LayerMask.NameToLayer("Player");
        if (localPlayerLayer != -1)
        {
            gameObject.layer = localPlayerLayer;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = localPlayerLayer;
                if (reviveCollider != null) reviveCollider.gameObject.layer = LayerMask.NameToLayer("ReviveLayer");
            }
        }
        if (team == PlayerTeam.None)
        {
            CmdRequestTeamAssignment();
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        reviveRequestUI = GetComponentInChildren<ReviveRequestUI>();

    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _nameText = GetComponentInChildren<TextMeshProUGUI>();
        _teamIndicator = transform.Find("TeamIndicator")?.gameObject;
        _playerUI_Team = GetComponentInChildren<PlayerUI_Team>();
        if (_nameText != null)
        {
            _nameText.text = playerName;
        }
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        nameTagUI = GetComponentInChildren<NameTagUI>();
        if (healthBarUI != null)
        {
            healthBarUI.target = transform;
            if (Health != null)
            {
                Health.OnHealthUpdated += healthBarUI.UpdateHP;
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
            }
        }

        reviveRequestUI = GetComponentInChildren<ReviveRequestUI>(true);

        if (nameTagUI != null)
        {
            nameTagUI.target = transform;
            nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None);
        }
        StartCoroutine(InitializeUIWithRetry());
        StartCoroutine(DelayedUIUpdate());
        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui != null && !isLocalPlayer)
        {
            ui.gameObject.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (nameTagUI != null)
        {
            nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None);
        }
    }

    private IEnumerator DelayedUIUpdate()
    {
        float delay = Random.Range(2f, 3f);
        yield return new WaitForSeconds(delay);
        if (!isServer)
        {
            UpdateUI();
        }
    }

    private IEnumerator InitializeUIWithRetry()
    {
        int maxRetries = 5;
        int retryCount = 0;
        while (retryCount < maxRetries)
        {
            if (nameTagUI != null && healthBarUI != null && playerName != "Player" && team != PlayerTeam.None && Health != null && Health.CurrentHealth > 0)
            {
                nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None);
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
                yield break;
            }
            retryCount++;
            yield return new WaitForSeconds(2f);
        }
        Debug.LogWarning($"[PlayerCore] UI initialization failed after {maxRetries} retries");
    }

    [Server]
    public void ServerRespawnPlayer(Vector3 newPosition, float hpFraction = 1f)
    {
        SetDeathState(false);
        isStunned = false;
        currentControlEffect = ControlEffectType.None;
        currentEffectWeight = 0;
        _slowPercentage = 0f;
        if (Movement != null) Movement.SetMovementSpeed(Stats.movementSpeed);
        if (Health != null)
        {
            Health.SetHealth(Mathf.RoundToInt(Stats.maxHealth * hpFraction));
        }
        transform.position = newPosition;
        RpcOnRespawned(newPosition);
    }

    [ClientRpc]
    private void RpcOnRespawned(Vector3 newPosition)
    {
        transform.position = newPosition;
        if (isLocalPlayer)
        {
            if (Movement != null) Movement.enabled = true;
            if (Combat != null) Combat.enabled = true;
            if (Skills != null) Skills.enabled = true;
            if (ActionSystem != null) ActionSystem.enabled = true;
            deathScreenUI.HideDeathScreen();
            if (reviveRequestUI != null) reviveRequestUI.Hide();
            if (healthBarUI != null && Health != null)
            {
                healthBarUI.gameObject.SetActive(Health.CurrentHealth > 0);
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
            }
        }
    }

    [Command]
    private void CmdRequestTeamAssignment()
    {
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();
        PlayerTeam newTeam = uiInfo.team != PlayerTeam.None ? uiInfo.team : PlayerTeam.Red;
        team = newTeam;
        playerName = uiInfo.name;
    }

    [Command]
    public void CmdChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 20)
        {
            return;
        }
        playerName = newName;
    }

    [Command]
    public void CmdChangeTeam(PlayerTeam newTeam)
    {
        if (isDead)
        {
            return;
        }
        team = newTeam;
    }

    [Command]
    public void CmdAddExperience(int amount)
    {
        if (Stats != null)
        {
            Stats.AddExperience(amount);
        }
    }

    [Command]
    public void CmdIncreaseStat(string statName)
    {
        if (Stats != null)
        {
            Stats.IncreaseStat(statName);
        }
    }

    [Command]
    public void CmdRequestRespawn()
    {
        if (isDead)
        {
            ServerRespawnPlayer(_initialSpawnPosition);
        }
    }

    [Command]
    public void CmdSetClass(CharacterClass newClass)
    {
        if (Stats != null)
        {
            Stats.CmdSetClass(newClass);
        }
    }

    private void OnTeamChanged(PlayerTeam oldTeam, PlayerTeam newTeam)
    {
        UpdateTeamIndicatorColor();
        if (nameTagUI != null)
        {
            nameTagUI.UpdateNameAndTeam(playerName, newTeam, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None);
        }
    }

    private void OnNameChanged(string oldName, string newName)
    {
        if (_nameText != null)
        {
            _nameText.text = newName;
        }
        if (nameTagUI != null)
        {
            nameTagUI.UpdateNameAndTeam(newName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None);
        }
    }

    private void UpdateTeamIndicatorColor()
    {
        if (_teamIndicator == null) return;
        Renderer rend = _teamIndicator.GetComponent<Renderer>();
        if (rend == null) return;
        if (isLocalPlayer)
        {
            rend.material = localPlayerMaterial;
        }
        else
        {
            if (localPlayerCoreInstance != null && localPlayerCoreInstance.team == team)
            {
                rend.material = allyMaterial;
            }
            else
            {
                rend.material = enemyMaterial;
            }
        }
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            if (Combat != null) Combat.enabled = false;
            if (Skills != null) Skills.enabled = false;
            if (Movement != null) Movement.enabled = false;
            if (ActionSystem != null) ActionSystem.CompleteAction();
            if (Skills != null) Skills.CancelSkillSelection();
            if (isLocalPlayer) deathScreenUI.ShowDeathScreen();
            if (boxCollider != null) boxCollider.enabled = false;
            if (reviveCollider != null) reviveCollider.enabled = true;
            if (healthBarUI != null) healthBarUI.gameObject.SetActive(false);
        }
        else
        {
            if (Combat != null) Combat.enabled = true;
            if (Skills != null) Skills.enabled = true;
            if (Movement != null) Movement.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;
            if (reviveCollider != null) reviveCollider.enabled = false;
            if (healthBarUI != null && Health != null)
            {
                healthBarUI.gameObject.SetActive(Health.CurrentHealth > 0);
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
            }
        }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
        if (newValue && ActionSystem != null) ActionSystem.CompleteAction();
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        if (currentControlEffect != ControlEffectType.None && Time.time < controlEffectEndTime && skillWeight <= currentEffectWeight)
        {
            return;
        }
        if (currentControlEffect != ControlEffectType.None)
        {
            ClearControlEffect();
        }
        currentControlEffect = effectType;
        currentEffectWeight = skillWeight;
        controlEffectEndTime = Time.time + duration;
        if (effectType == ControlEffectType.Stun)
        {
            isStunned = true;
        }
        else if (effectType == ControlEffectType.Slow)
        {
            _slowPercentage = duration;
            _originalSpeed = Stats.movementSpeed;
            if (Movement != null) Movement.SetMovementSpeed(Stats.movementSpeed * (1f - _slowPercentage));
        }
    }

    [Server]
    public void ApplySlow(float percentage, float duration, int skillWeight)
    {
        if (currentControlEffect != ControlEffectType.None && Time.time < controlEffectEndTime && skillWeight <= currentEffectWeight)
        {
            return;
        }
        if (currentControlEffect != ControlEffectType.None)
        {
            ClearControlEffect();
        }
        currentControlEffect = ControlEffectType.Slow;
        currentEffectWeight = skillWeight;
        _slowPercentage = percentage;
        _originalSpeed = Stats.movementSpeed;
        if (Movement != null) Movement.SetMovementSpeed(Stats.movementSpeed * (1f - _slowPercentage));
        controlEffectEndTime = Time.time + duration;
    }

    [Server]
    private void ClearControlEffect()
    {
        if (currentControlEffect == ControlEffectType.Stun)
        {
            isStunned = false;
        }
        else if (currentControlEffect == ControlEffectType.Slow && _originalSpeed > 0f)
        {
            if (Movement != null) Movement.SetMovementSpeed(_originalSpeed);
            _slowPercentage = 0f;
            _originalSpeed = 0f;
        }
        currentControlEffect = ControlEffectType.None;
        currentEffectWeight = 0;
        controlEffectEndTime = 0f;
    }

    [Command]
    private void CmdDie()
    {
        SetDeathState(true);
    }

    [Server]
    public void SetDeathState(bool state)
    {
        isDead = state;
        if (state) deathPosition = transform.position;
    }

    public override void OnStopClient()
    {
        if (healthBarUI != null) Destroy(healthBarUI.gameObject);
        if (nameTagUI != null) Destroy(nameTagUI.gameObject);
    }

    public void OnDestroy()
    {
        if (healthBarUI != null) Destroy(healthBarUI.gameObject);
        if (nameTagUI != null) Destroy(nameTagUI.gameObject);
    }

    public GameObject GetHealthBarPrefab() { return null; }
    public void SetHealthBarUI(HealthBarUI ui) { healthBarUI = ui; }
    public HealthBarUI GetHealthBarUI() { return healthBarUI; }
    public int GetCurrentHealth() { return Health != null ? Health.CurrentHealth : 0; }
    public int GetMaxHealth() { return Health != null ? Health.MaxHealth : 0; }
    public NameTagUI GetNameTagUI() { return nameTagUI; }
    public bool CanCastSkill()
    {
        return !isDead && !isStunned;
    }

    [Command]
    public void CmdRequestRevive(uint targetNetId)
    {
        NetworkIdentity targetIdentity;
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out targetIdentity)) return;
        PlayerCore target = targetIdentity.GetComponent<PlayerCore>();
        if (target == null || !target.isDead || target.team != team) return;
        target.RpcShowReviveRequest(netId);
    }

    [ClientRpc]
    public void RpcShowReviveRequest(uint casterNetId)
    {
        if (!isLocalPlayer || reviveRequestUI == null) return;
        NetworkIdentity casterIdentity;
        string casterName = "";
        if (NetworkClient.spawned.TryGetValue(casterNetId, out casterIdentity))
        {
            PlayerCore caster = casterIdentity.GetComponent<PlayerCore>();
            if (caster != null)
            {
                casterName = caster.playerName;
            }
        }
        reviveRequestUI.Show(casterName);
    }

    [Command]
    public void CmdAcceptRevive()
    {
        if (!isDead) return;
        ServerRespawnPlayer(deathPosition, pendingReviveHpFraction);
        pendingReviveHpFraction = 0f;
    }
}