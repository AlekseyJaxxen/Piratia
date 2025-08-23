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

public class PlayerCore : NetworkBehaviour
{
    [Header("Core Components")]
    public PlayerMovement Movement;
    public PlayerCombat Combat;
    public PlayerSkills Skills;
    public PlayerActionSystem ActionSystem;
    public PlayerCameraController Camera;
    public Health Health;

    [Header("Dependencies")]
    public LayerMask interactableLayers;

    [Header("Visuals")]
    [SerializeField]
    private GameObject[] playerModels;
    [SyncVar(hook = nameof(OnModelChanged))]
    private int _modelIndex = -1;
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
    private int stunPriority = 0;
    [SyncVar]
    private float stunTimer = 0f;

    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float timeOfDeath;

    private GameObject _teamIndicator;
    private TextMeshProUGUI _nameText;
    private PlayerUI_Team _playerUI_Team;

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        Skills = GetComponent<PlayerSkills>();
        ActionSystem = GetComponent<PlayerActionSystem>();
        Camera = GetComponent<PlayerCameraController>();
        Health = GetComponent<Health>();
    }

    public override void OnStartLocalPlayer()
    {
        if (Camera != null)
        {
            Camera.Init(this);
        }
        _playerUI_Team = FindObjectOfType<PlayerUI_Team>();
        if (_playerUI_Team != null)
        {
            _playerUI_Team.ShowTeamSelectionUI();
        }
    }

    public override void OnStartServer()
    {
        isDead = false;
        isStunned = false;

        if (playerModels != null && playerModels.Length > 0)
        {
            _modelIndex = Random.Range(0, playerModels.Length);
        }
        else
        {
            Debug.LogError("Player models array is not assigned or is empty!");
        }

        if (Health != null) Health.Init();
        playerName = $"Player_{connectionToClient.connectionId}";
    }

    private void Start()
    {
        InitComponents();
        _teamIndicator = transform.Find("TeamIndicator")?.gameObject;
        _nameText = GetComponentInChildren<TextMeshProUGUI>();
        OnTeamChanged(PlayerTeam.None, team);
        OnNameChanged("Player", playerName);
    }

    private void InitComponents()
    {
        if (Movement != null) Movement.Init(this);
        if (Combat != null) Combat.Init(this);
        if (Skills != null) Skills.Init(this);
        if (ActionSystem != null) ActionSystem.Init(this);
    }

    private void Update()
    {
        ServerUpdate();

        if (!isLocalPlayer || isStunned) return;

        if (isDead)
        {
            if (Time.time - timeOfDeath >= respawnTime)
            {
                CmdRespawnPlayer();
            }
            return;
        }

        Skills.HandleSkills();
        Combat.HandleCombat();
        Movement.HandleMovement();
    }

    // Этот метод теперь будет вызываться только на сервере благодаря атрибуту
    [ServerCallback]
    private void ServerUpdate()
    {
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0)
            {
                SetStunState(false);
                stunPriority = 0;
                stunTimer = 0f;
            }
        }
    }

    #region State Management

    [Server]
    public void TryApplyStun(int newPriority, float duration)
    {
        if (newPriority > stunPriority)
        {
            stunPriority = newPriority;
            stunTimer = duration;
            SetStunState(true);
            Debug.Log($"Applied stun with priority {newPriority} and duration {duration}.");
        }
        else
        {
            Debug.Log($"Ignored stun with priority {newPriority}. Current priority is {stunPriority}.");
        }
    }

    [Server]
    public void SetDeathState(bool state)
    {
        isDead = state;
        if (state)
        {
            timeOfDeath = Time.time;
            Movement.StopMovement();
            Combat.StopAttacking();
            ActionSystem.CompleteAction();
            RpcSetDeathState(true);
        }
        else
        {
            RpcSetDeathState(false);
        }
    }

    [ClientRpc]
    private void RpcSetDeathState(bool state)
    {
        if (Movement != null)
        {
            Movement.StopMovement();
            Movement.enabled = !state;
        }
        if (Combat != null) Combat.enabled = !state;
        if (Skills != null) Skills.enabled = !state;
        if (ActionSystem != null) ActionSystem.enabled = !state;

        if (playerModels != null && _modelIndex >= 0 && _modelIndex < playerModels.Length)
        {
            if (playerModels[_modelIndex] != null) playerModels[_modelIndex].SetActive(!state);
        }

        if (state && deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }
    }

    [Server]
    public void SetStunState(bool state)
    {
        isStunned = state;
        if (state)
        {
            ActionSystem.CompleteAction();
            RpcSetStunState(true);
        }
        else
        {
            RpcSetStunState(false);
        }
    }

    [ClientRpc]
    private void RpcSetStunState(bool state)
    {
        if (Movement != null) Movement.enabled = !state;
        if (Combat != null) Combat.enabled = !state;
        if (Skills != null) Skills.enabled = !state;
        if (ActionSystem != null) ActionSystem.enabled = !state;
    }

    [Command]
    public void CmdRespawnPlayer()
    {
        SetDeathState(false);
        if (Health != null)
        {
            Health.SetHealth(Health.MaxHealth);
        }
    }

    [Command]
    public void CmdSetPlayerInfo(string newName, PlayerTeam newTeam)
    {
        playerName = newName;
        team = newTeam;
    }

    private void OnTeamChanged(PlayerTeam oldTeam, PlayerTeam newTeam)
    {
        UpdateTeamIndicatorColor();
    }

    private void OnNameChanged(string oldName, string newName)
    {
        if (_nameText != null)
        {
            _nameText.text = newName;
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
            PlayerCore localPlayerCore = FindObjectOfType<PlayerCore>();
            if (localPlayerCore != null && localPlayerCore.team == team)
            {
                rend.material = allyMaterial;
            }
            else
            {
                rend.material = enemyMaterial;
            }
        }
    }

    private void OnModelChanged(int oldIndex, int newIndex)
    {
        if (playerModels == null || newIndex < 0 || newIndex >= playerModels.Length) return;
        if (oldIndex >= 0 && oldIndex < playerModels.Length && playerModels[oldIndex] != null) playerModels[oldIndex].SetActive(false);
        if (playerModels[newIndex] != null) playerModels[newIndex].SetActive(true);
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue) { Combat.enabled = false; Skills.enabled = false; Movement.enabled = false; }
        else { Combat.enabled = true; Skills.enabled = true; Movement.enabled = true; }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
    }
    #endregion
}