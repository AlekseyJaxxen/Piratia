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
    // 🚨 ДОБАВЛЕНО: SyncVar для синхронизации состояния стана
    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;

    // 🚨 ИЗМЕНЕНО: Добавлен хук для обработки состояния смерти
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
        // Инициализация всех компонентов происходит на всех экземплярах.
        // Это необходимо, чтобы ссылки были доступны как на сервере, так и на клиентах.
        Camera.Init(this);
        Movement.Init(this);
        Combat.Init(this);
        Skills.Init(this);
        // Animation.Init(this);
        ActionSystem.Init(this);

        // Логика активации кругов в зависимости от isLocalPlayer
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

        // Весь остальной код, который должен работать только
        // для локального игрока, вынесен в эту проверку.
        if (!isLocalPlayer) return;

        // Здесь может быть другой код, который вы хотите выполнять только для локального игрока.
    }

    void Update()
    {
        // 🚨 ИЗМЕНЕНО: Теперь проверка isStunned идет напрямую.
        if (isLocalPlayer && !isDead && !isStunned)
        {
            Movement.HandleMovement();
            Combat.HandleCombat();
            Skills.HandleSkills();
            // Animation.UpdateAnimations();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Случайный выбор модели
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
        // Убедимся, что массив моделей существует и новый индекс валиден.
        if (playerModels == null || newIndex < 0 || newIndex >= playerModels.Length)
        {
            return;
        }

        // Выключаем старую модель, если она была выбрана
        if (oldIndex >= 0 && oldIndex < playerModels.Length)
        {
            playerModels[oldIndex].SetActive(false);
        }

        // Включаем новую модель
        playerModels[newIndex].SetActive(true);
    }

    // 🚨 НОВЫЙ МЕТОД: Хук для обработки состояния стана.
    // Вызывается автоматически на всех клиентах при изменении SyncVar.
    void OnStunStateChanged(bool oldValue, bool newValue)
    {
        // Вызываем метод в PlayerSkills для управления визуальным эффектом.
        Skills.HandleStunEffect(newValue);
    }

    // 🚨 ИЗМЕНЕНО: Хук для обработки состояния смерти.
    // Теперь он вызывается на всех клиентах, когда isDead меняется.
    void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            HandleDeath();
        }
        else
        {
            // Возвращаем управление игроку, когда он возрождается.
            enabled = true;
        }
    }

    // 🚨 НОВЫЙ МЕТОД: Серверный метод для установки статуса стана
    [Server]
    public void SetStunState(bool state)
    {
        // isStunned - это SyncVar, Mirror сам синхронизирует его на клиенты.
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