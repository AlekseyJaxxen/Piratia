using UnityEngine;
using Mirror;
using TMPro;
using System.Collections.Generic;

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
    public ControlEffectManager EffectManager; // Changed from private to public

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
        EffectManager = GetComponent<ControlEffectManager>(); // Assign to the public field
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

        if (!isLocalPlayer || EffectManager.IsStunned || EffectManager.IsSilenced || EffectManager.IsPoisoned) return;

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
        if (isDead)
        {
            if (Time.time - timeOfDeath >= respawnTime)
            {
                Transform newSpawnPoint = FindObjectOfType<MyNetworkManager>()?.GetTeamSpawnPoint(team);
                RpcRespawnPlayer(newSpawnPoint != null ? newSpawnPoint.position : _initialSpawnPosition);
            }
        }
    }

    [Server]
    public void SetDeathState(bool state) // Changed from private to public
    {
        isDead = state;
        if (state)
        {
            timeOfDeath = Time.time;
            Movement.StopMovement();
            Combat.StopAttacking();
            ActionSystem.CompleteAction();
            EffectManager.ClearControlEffect();
            if (TryGetComponent<BoxCollider>(out var boxCollider))
            {
                boxCollider.enabled = false;
            }
            if (TryGetComponent<Renderer>(out var renderer))
            {
                renderer.enabled = false;
            }
            RpcSetDeathState(true);
        }
        else
        {
            if (TryGetComponent<BoxCollider>(out var boxCollider))
            {
                boxCollider.enabled = true;
            }
            if (TryGetComponent<Renderer>(out var renderer))
            {
                renderer.enabled = true;
            }
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

        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui != null)
        {
            ui.gameObject.SetActive(!state);
        }

        if (state && deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }
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
        Debug.Log($"Сервер: Игрок {newName} присоединился к команде {newTeam}.");
    }

    [Command]
    public void CmdChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 20)
        {
            Debug.LogWarning($"Сервер: Неверное имя '{newName}' от клиента.");
            return;
        }
        playerName = newName;
        Debug.Log($"Сервер: Имя игрока изменено на: {newName}");
    }

    [Command]
    public void CmdChangeTeam(PlayerTeam newTeam)
    {
        if (isDead)
        {
            Debug.LogWarning($"Сервер: Нельзя сменить команду во время смерти.");
            return;
        }
        team = newTeam;
        Debug.Log($"Сервер: Команда игрока изменена на: {newTeam}");
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
}