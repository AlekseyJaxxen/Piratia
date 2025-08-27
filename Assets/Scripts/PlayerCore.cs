using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;

public enum PlayerAction
{
    None,
    Move,
    Attack,
    SkillCast
}

public enum ControlEffectType
{
    None = 0,
    Stun = 1,
    Silence = 2,
    FbStun = 3,
    Slow = 4,
}

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

    [SerializeField] private GameObject healthBarPrefab; // Назначьте prefab в инспекторе
    private HealthBarUI healthBarUI;
    [SerializeField] private GameObject nameTagPrefab; // Назначьте prefab в инспекторе
    private NameTagUI nameTagUI;



    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float _timeOfDeath;

    [Header("UI References")]
    [SerializeField]
    private DeathScreenUI deathScreenUI;

    [Header("Dependencies")]
    public LayerMask interactableLayers;
    public LayerMask groundLayer;

    [Header("Visuals")]
    public Material localPlayerMaterial;
    public Material allyMaterial;
    public Material enemyMaterial;

    public GameObject deathVFXPrefab;

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
    private Vector3 _initialSpawnPosition;

    private GameObject _teamIndicator;
    private TextMeshProUGUI _nameText;
    private PlayerUI_Team _playerUI_Team;
    public static PlayerCore localPlayerCoreInstance;

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        Skills = GetComponent<PlayerSkills>();
        ActionSystem = GetComponent<PlayerActionSystem>();
        Camera = GetComponent<PlayerCameraController>();
        Health = GetComponent<Health>();
        Stats = GetComponent<CharacterStats>();

        if (Movement == null) Debug.LogError("[PlayerCore] PlayerMovement component missing!");
        if (Combat == null) Debug.LogError("[PlayerCore] PlayerCombat component missing!");
        if (Skills == null) Debug.LogError("[PlayerCore] PlayerSkills component missing!");
        if (ActionSystem == null) Debug.LogError("[PlayerCore] PlayerActionSystem component missing!");
        if (Camera == null) Debug.LogError("[PlayerCore] PlayerCameraController component missing!");
        if (Health == null) Debug.LogError("[PlayerCore] Health component missing!");
        if (Stats == null) Debug.LogError("[PlayerCore] CharacterStats component missing!");

        if (Movement != null) Movement.Init(this);
        if (Combat != null) Combat.Init(this);
        if (ActionSystem != null) ActionSystem.Init(this);
        if (Camera != null) Camera.Init(this);
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            Debug.Log($"[PlayerCore] Update: isDead={isDead}, isStunned={isStunned}, Movement.enabled={Movement != null && Movement.enabled}, team={team}, name={playerName}");
        }
        if (NetworkServer.active) ServerUpdate();
    }

    [Server]
    private void ServerUpdate()
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
        Debug.Log($"[PlayerCore] OnStartLocalPlayer invoked. isOwned: {netIdentity.isOwned}, isDead: {isDead}, isStunned: {isStunned}, team: {team}, name: {playerName}");
        localPlayerCoreInstance = this;
        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui == null)
        {
            Debug.LogWarning("[PlayerCore] PlayerUI not found in player prefab!");
        }
        if (Camera != null)
        {
            Camera.Init(this);
        }
        else
        {
            Debug.LogError("[PlayerCore] Camera component is null!");
        }
        base.OnStartLocalPlayer();
        int localPlayerLayer = LayerMask.NameToLayer("Player");
        if (localPlayerLayer != -1)
        {
            gameObject.layer = localPlayerLayer;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = localPlayerLayer;
            }
        }
        else
        {
            Debug.LogError("[PlayerCore] Layer 'Player' not found!");
        }
        if (team == PlayerTeam.None)
        {
            Debug.LogWarning($"[PlayerCore] Team is None for player {playerName}. Requesting team assignment.");
            CmdRequestTeamAssignment();
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
        OnTeamChanged(team, team);

        if (healthBarPrefab != null)
        {
            Canvas mainCanvas = MainCanvas.Instance;
            if (mainCanvas != null)
            {
                GameObject barInstance = Instantiate(healthBarPrefab, mainCanvas.transform);
                healthBarUI = barInstance.GetComponent<HealthBarUI>();
                if (healthBarUI != null)
                {
                    healthBarUI.target = transform;
                    // Подписка на события здоровья
                    if (Health != null)
                    {
                        Health.OnHealthUpdated += healthBarUI.UpdateHP;
                        // Инициализация текущим значением
                        healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
                    }
                }
            }
        }

        if (nameTagPrefab != null)
        {
            Canvas mainCanvas = MainCanvas.Instance;
            if (mainCanvas != null)
            {
                GameObject tagInstance = Instantiate(nameTagPrefab, mainCanvas.transform);
                nameTagUI = tagInstance.GetComponent<NameTagUI>();
                if (nameTagUI != null)
                {
                    nameTagUI.target = transform;
                    nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance.team);
                }
            }
        }
    }

    public override void OnStartServer()
    {
        isDead = false;
        isStunned = false;
        _lastManaRegenTime = Time.time;
        if (Health != null) Health.Init();
        if (Stats != null) Stats.CalculateDerivedStats();
        _initialSpawnPosition = transform.position;
        Debug.Log($"[PlayerCore] OnStartServer: Initialized player {playerName}, team={team}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (isLocalPlayer)
        {
            if (Skills != null) Skills.CancelSkillSelection();
            if (ActionSystem != null) ActionSystem.CompleteAction();
            Debug.Log("[PlayerCore] Cleaned up on client disconnect.");
        }

        if (healthBarUI != null)
        {
            if (Health != null)
            {
                Health.OnHealthUpdated -= healthBarUI.UpdateHP;
            }
            Destroy(healthBarUI.gameObject);
        }

        if (nameTagUI != null) Destroy(nameTagUI.gameObject);
    }

    private void OnDisable()
    {
        if (Skills != null) Skills.CancelSkillSelection();
        if (ActionSystem != null) ActionSystem.CompleteAction();
        Debug.Log("[PlayerCore] Cleaned up on disable.");
    }

    [Server]
    public void SetDeathState(bool state)
    {
        isDead = state;
        if (state)
        {
            if (ActionSystem != null) ActionSystem.CompleteAction();
            if (Skills != null) Skills.CancelSkillSelection();
            RpcPlayDeathVFX();
        }
    }

    [ClientRpc]
    private void RpcPlayDeathVFX()
    {
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
    }

    [Command]
    public void CmdRequestRespawn()
    {
        if (!isDead)
        {
            Debug.Log("[PlayerCore] Player is not dead, cannot respawn.");
            return;
        }
        Transform spawnPoint = FindObjectOfType<MyNetworkManager>()?.GetTeamSpawnPoint(team);
        Vector3 respawnPosition = spawnPoint != null ? spawnPoint.position : _initialSpawnPosition;
        ServerRespawnPlayer(respawnPosition);
    }

    [Server]
    public void ServerRespawnPlayer(Vector3 newPosition)
    {
        SetDeathState(false);
        isStunned = false;
        currentControlEffect = ControlEffectType.None;
        currentEffectWeight = 0;
        _slowPercentage = 0f;
        if (Movement != null) Movement.SetMovementSpeed(Stats.movementSpeed);
        if (Health != null)
        {
            Health.SetHealth(Health.MaxHealth);
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
            Debug.Log("[PlayerCore] Respawned, components re-enabled.");
        }
    }

    [Command]
    private void CmdRequestTeamAssignment()
    {
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();
        PlayerTeam newTeam = uiInfo.team != PlayerTeam.None ? uiInfo.team : PlayerTeam.Red;
        team = newTeam;
        playerName = uiInfo.name;
        Debug.Log($"[PlayerCore] Server: Assigned team {newTeam} and name {playerName} for player via CmdRequestTeamAssignment");
    }

    [Command]
    public void CmdChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 20)
        {
            Debug.LogWarning($"[PlayerCore] Server: Invalid name '{newName}' received from client.");
            return;
        }
        playerName = newName;
        Debug.Log($"[PlayerCore] Server: Player name changed to: {newName}");
    }

    [Command]
    public void CmdChangeTeam(PlayerTeam newTeam)
    {
        if (isDead)
        {
            Debug.LogWarning($"[PlayerCore] Server: Cannot change team while dead.");
            return;
        }
        team = newTeam;
        Debug.Log($"[PlayerCore] Server: Player team changed to: {newTeam}");
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

    private void OnTeamChanged(PlayerTeam oldTeam, PlayerTeam newTeam)
    {
        UpdateTeamIndicatorColor();
        Debug.Log($"[PlayerCore] Team changed: {oldTeam} -> {newTeam}");

        if (nameTagUI != null) nameTagUI.UpdateNameAndTeam(playerName, newTeam, localPlayerCoreInstance.team);
    }

    private void OnNameChanged(string oldName, string newName)
    {
        if (_nameText != null)
        {
            _nameText.text = newName;
        }
        Debug.Log($"[PlayerCore] Name changed: {oldName} -> {newName}");

        if (nameTagUI != null) nameTagUI.UpdateNameAndTeam(newName, team, localPlayerCoreInstance.team);
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
        }
        else
        {
            if (Combat != null) Combat.enabled = true;
            if (Skills != null) Skills.enabled = true;
            if (Movement != null) Movement.enabled = true;
        }
        Debug.Log($"[PlayerCore] Death state changed: {oldValue} -> {newValue}, Movement.enabled={Movement != null && Movement.enabled}");
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
        if (newValue && ActionSystem != null) ActionSystem.CompleteAction();
        Debug.Log($"[PlayerCore] Stun state changed: {oldValue} -> {newValue}");
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, int skillWeight)
    {
        if (currentControlEffect != ControlEffectType.None && Time.time < controlEffectEndTime && skillWeight <= currentEffectWeight)
        {
            Debug.Log($"[PlayerCore] Cannot apply {effectType} (weight {skillWeight}): {currentControlEffect} (weight {currentEffectWeight}) is active until {controlEffectEndTime}");
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
            Debug.Log($"[PlayerCore] Applied stun effect, weight={skillWeight}, duration={duration}");
        }
        else if (effectType == ControlEffectType.Slow)
        {
            _slowPercentage = duration; // Предполагается, что duration используется как процент замедления
            _originalSpeed = Stats.movementSpeed;
            if (Movement != null) Movement.SetMovementSpeed(Stats.movementSpeed * (1f - _slowPercentage));
            Debug.Log($"[PlayerCore] Applied slow effect, weight={skillWeight}, percentage={_slowPercentage}, duration={duration}");
        }
    }

    [Server]
    public void ApplySlow(float percentage, float duration, int skillWeight)
    {
        if (currentControlEffect != ControlEffectType.None && Time.time < controlEffectEndTime && skillWeight <= currentEffectWeight)
        {
            Debug.Log($"[PlayerCore] Cannot apply slow (weight {skillWeight}): {currentControlEffect} (weight {currentEffectWeight}) is active until {controlEffectEndTime}");
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
        Debug.Log($"[PlayerCore] Applied slow: percentage={percentage}, duration={duration}, weight={skillWeight}");
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
        Debug.Log("[PlayerCore] Cleared control effect");
    }

    [Command]
    private void CmdDie()
    {
        SetDeathState(true);
    }
}