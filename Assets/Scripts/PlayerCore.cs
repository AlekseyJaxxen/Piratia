using UnityEngine;
using Mirror;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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

    private readonly SyncList<ControlEffect> activeControlEffects = new SyncList<ControlEffect>(); // Удален [SyncVar], добавлен readonly

    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float timeOfDeath;

    [SyncVar]
    private Vector3 _initialSpawnPosition;

    private GameObject _teamIndicator;
    private TextMeshProUGUI _nameText;
    private PlayerUI_Team _playerUI_Team;
    private float _originalSpeed = 0f;
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

        if (Movement != null)
        {
            _originalSpeed = Movement.GetOriginalSpeed();
        }
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
        for (int i = activeControlEffects.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeControlEffects[i].endTime)
            {
                RemoveControlEffect(activeControlEffects[i].type);
            }
        }

        if (isDead)
        {
            if (Time.time - timeOfDeath >= respawnTime)
            {
                Transform newSpawnPoint = FindObjectOfType<MyNetworkManager>()?.GetTeamSpawnPoint(team);
                RpcRespawnPlayer(newSpawnPoint != null ? newSpawnPoint.position : _initialSpawnPosition);
            }
        }
    }

    #region State Management

    [Server]
    public void ApplyControlEffect(ControlEffectType newEffectType, float duration, float slowPercentage = 0f)
    {
        var existingEffect = activeControlEffects.Find(e => e.type == newEffectType);
        if (existingEffect.type != ControlEffectType.None)
        {
            activeControlEffects.Remove(existingEffect);
            activeControlEffects.Add(new ControlEffect(newEffectType, Time.time + duration, slowPercentage));
        }
        else
        {
            activeControlEffects.Add(new ControlEffect(newEffectType, Time.time + duration, slowPercentage));
        }

        if (newEffectType == ControlEffectType.Stun)
        {
            SetStunState(true);
        }
        else if (newEffectType == ControlEffectType.Slow)
        {
            ApplySlow(slowPercentage);
        }
        else if (newEffectType == ControlEffectType.Silence)
        {
            RpcSetSilenceState(true);
        }

        Debug.Log($"Применен эффект: {newEffectType} на {duration} секунд.");
    }

    [Server]
    private void RemoveControlEffect(ControlEffectType effectType)
    {
        var effect = activeControlEffects.Find(e => e.type == effectType);
        if (effect.type != ControlEffectType.None)
        {
            activeControlEffects.Remove(effect);

            if (effectType == ControlEffectType.Stun)
            {
                if (!activeControlEffects.Any(e => e.type == ControlEffectType.Stun))
                {
                    SetStunState(false);
                }
            }
            else if (effectType == ControlEffectType.Slow)
            {
                var remainingSlow = activeControlEffects.Find(e => e.type == ControlEffectType.Slow);
                if (remainingSlow.type != ControlEffectType.None)
                {
                    ApplySlow(remainingSlow.slowPercentage);
                }
                else
                {
                    Movement.SetMovementSpeed(_originalSpeed);
                    Debug.Log("Эффект замедления снят. Скорость восстановлена.");
                }
            }
            else if (effectType == ControlEffectType.Silence)
            {
                if (!activeControlEffects.Any(e => e.type == ControlEffectType.Silence))
                {
                    RpcSetSilenceState(false);
                }
            }
        }
    }

    [Server]
    public void ClearControlEffect()
    {
        activeControlEffects.Clear();
        SetStunState(false);
        RpcSetSilenceState(false);
        Movement.SetMovementSpeed(_originalSpeed);
        Debug.Log("Все эффекты сняты.");
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

    [Server]
    public void SetStunState(bool state)
    {
        isStunned = state;
        if (state)
        {
            ActionSystem.CompleteAction();
            Movement.StopMovement();
            RpcSetStunState(true);
        }
        else
        {
            RpcSetStunState(false);
        }
    }

    [Server]
    public void ApplySlow(float slowPercentage)
    {
        float maxSlow = activeControlEffects
            .Where(e => e.type == ControlEffectType.Slow)
            .Select(e => e.slowPercentage)
            .DefaultIfEmpty(0f)
            .Max();

        float newSpeed = _originalSpeed * (1f - maxSlow);
        Movement.SetMovementSpeed(newSpeed);
        Debug.Log($"Применено замедление: {maxSlow:P0}. Новая скорость: {newSpeed}");
    }

    [ClientRpc]
    private void RpcSetStunState(bool state)
    {
        if (Movement != null)
        {
            Movement.enabled = !state;
            if (state) Movement.StopMovement();
        }
        if (Combat != null) Combat.enabled = !state;
        if (Skills != null)
        {
            Skills.enabled = !state;
            Skills.HandleStunEffect(state);
        }
        if (ActionSystem != null) ActionSystem.enabled = !state;
    }

    [ClientRpc]
    private void RpcSetSilenceState(bool state)
    {
        if (Skills != null)
        {
            Skills.HandleSilenceEffect(state);
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

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
    }
}
#endregion