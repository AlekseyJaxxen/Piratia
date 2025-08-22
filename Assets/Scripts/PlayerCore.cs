using UnityEngine;
using Mirror;
using TMPro;

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

    [SyncVar(hook = nameof(OnTeamChanged))]
    public PlayerTeam team = PlayerTeam.None;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";

    [SyncVar(hook = nameof(OnDeathStateChanged))]
    public bool isDead = false;

    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;

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
        if (!isLocalPlayer || isDead || isStunned) return;

        Skills.HandleSkills();
        Combat.HandleCombat();
        Movement.HandleMovement();
    }

    #region State Management
    [Server]
    public void SetDeathState(bool state)
    {
        isDead = state;
        if (state)
        {
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
            rend.material.color = (team == PlayerTeam.Red) ? Color.red : Color.blue;
        }
        else
        {
            PlayerCore localPlayerCore = FindObjectOfType<PlayerCore>();
            if (localPlayerCore != null && localPlayerCore.team == team)
            {
                rend.material.color = Color.green;
            }
            else
            {
                rend.material.color = Color.red;
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