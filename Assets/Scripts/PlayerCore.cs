using UnityEngine;
using Mirror;

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

    [SyncVar(hook = nameof(OnDeathStateChanged))]
    public bool isDead = false;

    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;

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
    }

    private void Start()
    {
        InitComponents();
    }

    // 🚨 ИСПРАВЛЕНО: Убраны аргументы из вызовов Init
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

    private void OnModelChanged(int oldIndex, int newIndex)
    {
        if (playerModels == null || newIndex < 0 || newIndex >= playerModels.Length)
        {
            return;
        }

        if (oldIndex >= 0 && oldIndex < playerModels.Length && playerModels[oldIndex] != null)
        {
            playerModels[oldIndex].SetActive(false);
        }

        if (playerModels[newIndex] != null)
        {
            playerModels[newIndex].SetActive(true);
        }
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Combat.enabled = false;
            Skills.enabled = false;
            Movement.enabled = false;
        }
        else
        {
            Combat.enabled = true;
            Skills.enabled = true;
            Movement.enabled = true;
        }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
    }
    #endregion
}