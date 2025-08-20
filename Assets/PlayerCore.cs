using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerSkills))]
//[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(PlayerCameraController))]
[RequireComponent(typeof(PlayerActionSystem))]
public class PlayerCore : NetworkBehaviour
{
    [Header("References")]
    public Transform modelPivot;
    public LayerMask interactableLayers;

    [Header("Player Circles")]
    public GameObject circleAlly;
    public GameObject circleEnemy;

    [Header("Player Visuals")]
    [SerializeField] private GameObject[] playerModels;

    [Header("Player State")]
    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;

    [SyncVar(hook = nameof(OnDeathStateChanged))]
    public bool isDead = false;

    [SyncVar(hook = nameof(OnModelChanged))]
    private int _modelIndex = -1;

    public PlayerMovement Movement { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerSkills Skills { get; private set; }
    //public PlayerAnimation Animation { get; private set; }
    public PlayerCameraController Camera { get; private set; }
    public PlayerActionSystem ActionSystem { get; private set; }

    void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        Skills = GetComponent<PlayerSkills>();
        // Animation = GetComponent<PlayerAnimation>();
        Camera = GetComponent<PlayerCameraController>();
        ActionSystem = GetComponent<PlayerActionSystem>();
    }

    void Start()
    {
        Camera.Init(this);
        Movement.Init(this);
        Combat.Init(this);
        Skills.Init(this);
        // Animation.Init(this);
        ActionSystem.Init(this);

        if (isLocalPlayer)
        {
            circleAlly.SetActive(true);
            circleEnemy.SetActive(false);
        }
        else
        {
            circleAlly.SetActive(false);
            circleEnemy.SetActive(true);
        }

        if (!isLocalPlayer) return;
    }

    void Update()
    {
        if (isLocalPlayer && !isDead && !isStunned)
        {
            // 🚨 ИЗМЕНЕНО: Удален вызов Movement.HandleMovement()
            Combat.HandleCombat();
            Skills.HandleSkills();
            // Animation.UpdateAnimations();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (playerModels != null && playerModels.Length > 0)
        {
            _modelIndex = Random.Range(0, playerModels.Length);
        }
        else
        {
            Debug.LogError("Player models array is not assigned or is empty!");
        }
    }

    private void OnModelChanged(int oldIndex, int newIndex)
    {
        if (playerModels == null || newIndex < 0 || newIndex >= playerModels.Length)
        {
            return;
        }

        if (oldIndex >= 0 && oldIndex < playerModels.Length)
        {
            playerModels[oldIndex].SetActive(false);
        }

        playerModels[newIndex].SetActive(true);
    }

    void OnStunStateChanged(bool oldValue, bool newValue)
    {
        Skills.HandleStunEffect(newValue);
    }

    void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            HandleDeath();
        }
        else
        {
            enabled = true;
        }
    }

    [Server]
    public void SetStunState(bool state)
    {
        isStunned = state;
    }

    [Server]
    public void SetDeathState(bool state)
    {
        isDead = state;
        Debug.Log($"[Server] SetDeathState: isDead = {state} for {gameObject.name}");
    }

    private void HandleDeath()
    {
        ActionSystem.CompleteAction();
        Movement.StopMovement();
        Combat.ClearTarget();
        Skills.CancelSkillSelection();
        enabled = false;
    }
}