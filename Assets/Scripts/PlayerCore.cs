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
}

//public enum PlayerTeam
//{
   // None,
  //  Red,
 //   Blue
//}

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

    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float timeOfDeath;

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
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("OnStartLocalPlayer вызван для локального игрока. Логика инициализации компонента запускается.");
        localPlayerCoreInstance = this;

        if (Camera != null)
        {
            Camera.Init(this);
        }

        base.OnStartLocalPlayer();

        int localPlayerLayer = LayerMask.NameToLayer("LocalPlayer");

        if (localPlayerLayer != -1)
        {
            gameObject.layer = localPlayerLayer;

            foreach (Transform child in transform)
            {
                child.gameObject.layer = localPlayerLayer;
            }
        }
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
    }

    public override void OnStartServer()
    {
        isDead = false;
        isStunned = false;

        if (Health != null) Health.Init();
    }

    public void OnHealthZero()
    {
        if (isLocalPlayer)
        {
            CmdDie();
        }
    }

    private void Start()
    {
        InitComponents();
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
            return;
        }

        Skills.HandleSkills();
        Combat.HandleCombat();
        Movement.HandleMovement();
    }

    [ServerCallback]
    private void ServerUpdate()
    {
        if (currentControlEffect != ControlEffectType.None && Time.time >= controlEffectEndTime)
        {
            ClearControlEffect();
        }

        if (isDead)
        {
            if (Time.time - timeOfDeath >= respawnTime)
            {
                Transform newSpawnPoint = FindObjectOfType<MyNetworkManager>()?.GetTeamSpawnPoint(team);

                if (newSpawnPoint != null)
                {
                    RpcRespawnPlayer(newSpawnPoint.position);
                }
                else
                {
                    Debug.LogError($"No spawn points found for team {team}! Respawning at default location.");
                    RpcRespawnPlayer(_initialSpawnPosition);
                }
            }
        }
    }

    #region State Management

    [Server]
    public void ApplyControlEffect(ControlEffectType newEffectType, float duration)
    {
        if (newEffectType > currentControlEffect)
        {
            currentControlEffect = newEffectType;
            controlEffectEndTime = Time.time + duration;

            if (currentControlEffect == ControlEffectType.Stun)
            {
                SetStunState(true);
            }

            Debug.Log($"Applied new control effect: {currentControlEffect} for {duration} seconds.");
        }
        else
        {
            Debug.Log($"Ignored control effect: {newEffectType}. Current effect is {currentControlEffect}.");
        }
    }

    [Server]
    public void ClearControlEffect()
    {
        if (currentControlEffect == ControlEffectType.Stun)
        {
            SetStunState(false);
        }

        currentControlEffect = ControlEffectType.None;
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
            ClearControlEffect();
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
    private void CmdDie()
    {
        SetDeathState(true);
    }

    [ClientRpc]
    private void RpcRespawnPlayer(Vector3 newPosition)
    {
        SetDeathState(false);
        if (Health != null)
        {
            Health.SetHealth(Health.MaxHealth);
        }
        transform.position = newPosition;
    }

    [Command]
    public void CmdSetPlayerInfo(string newName, PlayerTeam newTeam)
    {
        playerName = newName;
        team = newTeam;
        Debug.Log($"Server: Player {newName} has joined team {newTeam}.");
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
        if (newValue) { Combat.enabled = false; Skills.enabled = false; Movement.enabled = false; }
        else { Combat.enabled = true; Skills.enabled = true; Movement.enabled = true; }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
    }
    #endregion
}