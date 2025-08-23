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
    // Добавьте сюда другие типы контроля
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
    public LayerMask groundLayer;

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

    // ОБНОВЛЕНО: Используем SyncVar с хуком для isStunned
    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;

    // НОВЫЕ ПЕРЕМЕННЫЕ ДЛЯ СИСТЕМЫ КОНТРОЛЯ
    [SyncVar]
    private ControlEffectType currentControlEffect = ControlEffectType.None;
    [SyncVar]
    private float controlEffectEndTime = 0f;

    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float timeOfDeath;

    // ИСПРАВЛЕНО: _initialSpawnPosition теперь SyncVar
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
        localPlayerCoreInstance = this;

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

        // ИСПРАВЛЕНО: Установка начальной позиции на сервере
        _initialSpawnPosition = transform.position;

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

    // ИСПРАВЛЕНО: Добавлен метод OnHealthZero, который будет вызываться из компонента Health
    public void OnHealthZero()
    {
        // Только локальный игрок может отправлять команды
        if (isLocalPlayer)
        {
            CmdDie();
        }
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

        // Убрана логика возрождения с клиента
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

        // ИСПРАВЛЕНО: Таймер возрождения и вызов команды теперь на сервере
        if (isDead)
        {
            if (Time.time - timeOfDeath >= respawnTime)
            {
                RpcRespawnPlayer(_initialSpawnPosition);
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
            // Добавьте логику для других типов контроля, например:
            // if (currentControlEffect == ControlEffectType.Silence) { SetSilenceState(true); }

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
        // Добавьте логику для снятия других эффектов

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

    // ИСПРАВЛЕНО: Это команда, которая запускает процесс смерти на сервере.
    [Command]
    private void CmdDie()
    {
        SetDeathState(true);
    }

    // ИСПРАВЛЕНО: RPC-метод для синхронизации возрождения.
    [ClientRpc]
    private void RpcRespawnPlayer(Vector3 newPosition)
    {
        // Эта логика должна выполняться на всех клиентах, чтобы телепортировать игрока
        // и сбросить его состояние.
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