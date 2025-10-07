using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections;
using System.Linq;

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
    [HideInInspector] public Inventory Inventory;
    private HealthBarUI healthBarUI;
    [HideInInspector] public NameTagUI nameTagUI;
    [Header("Respawn")]
    public float respawnTime = 5.0f;
    private float _timeOfDeath;
    [Header("UI References")]
    [SerializeField] private DeathScreenUI deathScreenUI;
    [SerializeField] private Canvas mainCanvasReference;
    [Header("Dependencies")]
    public LayerMask interactableLayers;
    public LayerMask groundLayer;
    [Header("Visuals")]
    public Material localPlayerMaterial;
    public Material allyMaterial;
    public Material enemyMaterial;
    public GameObject deathVFXPrefab;
    [SerializeField] public Transform modelTransform;
    private Quaternion initialModelRotation;
    [SerializeField] private BoxCollider boxCollider;
    [Header("Indicators")]
    [SerializeField] private GameObject targetIndicatorPrefab;
    [SerializeField] private GameObject moveIndicatorPrefab;
    [SyncVar(hook = nameof(OnTeamChanged))]
    public PlayerTeam team = PlayerTeam.None;
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";
    
    [Header("Dynamic Teams")]
    [SyncVar(hook = nameof(OnGuildChanged))]
    public string guildId = "";
    [SyncVar(hook = nameof(OnPartyChanged))]
    public string partyId = "";
    [SyncVar(hook = nameof(OnPartyLeaderChanged))]
    public bool isPartyLeader = false;
    [SyncVar(hook = nameof(OnFactionChanged))]
    public string factionId = "";
    [SyncVar(hook = nameof(OnDeathStateChanged))]
    public bool isDead = false;
    [SyncVar(hook = nameof(OnStunStateChanged))]
    public bool isStunned = false;
    [SyncVar(hook = nameof(OnSilenceStateChanged))]
    public bool isSilenced = false;
    [SyncVar]
    public float stunEffectEndTime = 0f;
    [SyncVar]
    private int stunEffectWeight = 0;
    [SyncVar]
    public float silenceEffectEndTime = 0f;
    [SyncVar]
    private int silenceEffectWeight = 0;
    [Header("Mana Regeneration")]
    public float manaRegenInterval = 1f;
    public int manaRegenAmount = 5;
    private float _lastManaRegenTime;
    [SyncVar]
    protected Vector3 _initialSpawnPosition;
    private GameObject _teamIndicator;
    private TextMeshProUGUI _nameText;
    private PlayerUI_Team _playerUI_Team;
    public static PlayerCore localPlayerCoreInstance;
    [SerializeField] private BoxCollider reviveCollider;
    [SyncVar] public Vector3 deathPosition;
    [SerializeField] private ReviveRequestUI reviveRequestUI;
    [SyncVar] public float pendingReviveHpFraction = 0f;
    [Header("Dropped Items")]
    [SerializeField] private GameObject droppedItemPrefab;
    private PlayerEquipmentVisuals equipmentVisuals;

    protected virtual void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        Skills = GetComponent<PlayerSkills>();
        ActionSystem = GetComponent<PlayerActionSystem>();
        Camera = GetComponent<PlayerCameraController>();
        Health = GetComponent<Health>();
        Stats = GetComponent<CharacterStats>();
        Inventory = GetComponent<Inventory>();
        equipmentVisuals = GetComponent<PlayerEquipmentVisuals>();
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] Inventory component not found on this GameObject!");
        }
        else
        {
            Inventory.Init(this);
        }
        if (equipmentVisuals == null)
        {
            Debug.LogError("[PlayerCore] PlayerEquipmentVisuals component not found!");
        }
        if (Movement != null) Movement.Init(this);
        if (Combat != null) Combat.Init(this);
        if (ActionSystem != null) ActionSystem.Init(this);
        if (Camera != null) Camera.Init(this);
        if (modelTransform != null)
        {
            initialModelRotation = modelTransform.localRotation;
        }
        boxCollider = GetComponent<BoxCollider>();
        reviveCollider = transform.Find("ReviveCollider")?.GetComponent<BoxCollider>();
        if (reviveCollider != null) reviveCollider.enabled = false;
        reviveRequestUI = GetComponentInChildren<ReviveRequestUI>();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
        }
        if (NetworkServer.active) ServerUpdate();
    }

    [Server]
    protected virtual void ServerUpdate()
    {
        if (isStunned && Time.time >= stunEffectEndTime)
        {
            ClearStunEffect();
        }
        if (isSilenced && Time.time >= silenceEffectEndTime)
        {
            ClearSilenceEffect();
        }
        if (Time.time >= _lastManaRegenTime + manaRegenInterval)
        {
            Stats.RestoreMana(manaRegenAmount);
            _lastManaRegenTime = Time.time;
        }
    }

    public override void OnStartLocalPlayer()
    {
        localPlayerCoreInstance = this;
        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui != null && !ui.gameObject.activeSelf)
        {
            ui.gameObject.SetActive(true);
        }
        
        // Создаем ContextMenuUI для локального игрока
        CreateContextMenuUI();
        
        // Создаем PartyInviteUI для локального игрока
        CreatePartyInviteUI();
        
        // Создаем PartyUIPanel для локального игрока
        CreatePartyUIPanel();
        
        if (Camera != null)
        {
            Camera.Init(this);
        }
        base.OnStartLocalPlayer();
        int localPlayerLayer = LayerMask.NameToLayer("Player");
        if (localPlayerLayer != -1)
        {
            gameObject.layer = localPlayerLayer;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = localPlayerLayer;
                if (reviveCollider != null) reviveCollider.gameObject.layer = LayerMask.NameToLayer("ReviveLayer");
            }
        }
        if (team == PlayerTeam.None)
        {
            CmdRequestTeamAssignment();
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        reviveRequestUI = GetComponentInChildren<ReviveRequestUI>();
        StartCoroutine(DelayedInventorySync());
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
        healthBarUI = GetComponentInChildren<HealthBarUI>();
        nameTagUI = GetComponentInChildren<NameTagUI>();
        if (healthBarUI != null)
        {
            healthBarUI.target = transform;
            if (Health != null)
            {
                Health.OnHealthUpdated += healthBarUI.UpdateHP;
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
            }
        }
        reviveRequestUI = GetComponentInChildren<ReviveRequestUI>(true);
        if (nameTagUI != null)
        {
            nameTagUI.target = transform;
            nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None, isLocalPlayer);
        }
        StartCoroutine(InitializeUIWithRetry());
        StartCoroutine(DelayedUIUpdate());
        PlayerUI ui = GetComponentInChildren<PlayerUI>();
        if (ui != null && !isLocalPlayer)
        {
            ui.gameObject.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (nameTagUI != null)
        {
            nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None, isLocalPlayer);
        }
    }
    
    /// <summary>
    /// Updates name tags for all players to reflect party/guild/faction changes
    /// </summary>
    private void UpdateAllPlayerNameTags()
    {
        if (!isLocalPlayer) return; // Only local player should update all name tags
        
        // Find all PlayerCore instances and update their name tags
        PlayerCore[] allPlayers = FindObjectsOfType<PlayerCore>();
        foreach (PlayerCore player in allPlayers)
        {
            if (player.nameTagUI != null)
            {
                player.nameTagUI.UpdateNameAndTeam(
                    player.playerName, 
                    player.team, 
                    localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None, 
                    player.isLocalPlayer
                );
            }
        }
        
        Debug.Log($"[PlayerCore] Updated name tags for {allPlayers.Length} players");
        
        // Обновляем PartyUIPanel
        if (PartyUIPanel.Instance != null)
        {
            PartyUIPanel.Instance.ForceUpdatePartyUI();
        }
    }

    private IEnumerator DelayedUIUpdate()
    {
        float delay = Random.Range(2f, 3f);
        yield return new WaitForSeconds(delay);
        if (!isServer)
        {
            UpdateUI();
        }
    }

    private IEnumerator InitializeUIWithRetry()
    {
        int maxRetries = 5;
        int retryCount = 0;
        while (retryCount < maxRetries)
        {
            if (nameTagUI != null && healthBarUI != null && playerName != "Player" && team != PlayerTeam.None && Health != null && Health.CurrentHealth > 0)
            {
                nameTagUI.UpdateNameAndTeam(playerName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None, isLocalPlayer);
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
                yield break;
            }
            retryCount++;
            yield return new WaitForSeconds(2f);
        }
        Debug.LogWarning($"[PlayerCore] UI initialization failed after {maxRetries} retries");
    }

    [Server]
    public void ServerRespawnPlayer(Vector3 newPosition, float hpFraction = 1f)
    {
        SetDeathState(false);
        isStunned = false;
        isSilenced = false;
        ClearStunEffect();
        ClearSilenceEffect();
        if (Movement != null) Movement.SetMovementSpeed(Stats.movementSpeed);
        if (Health != null)
        {
            Health.SetHealth(Mathf.RoundToInt(Stats.maxHealth * hpFraction));
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
            if (ActionSystem != null)
            {
                ActionSystem.enabled = true;
                ActionSystem.Init(this);
            }
            deathScreenUI.HideDeathScreen();
            if (reviveRequestUI != null) reviveRequestUI.Hide();
            if (healthBarUI != null && Health != null)
            {
                healthBarUI.gameObject.SetActive(Health.CurrentHealth > 0);
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
            }
            GetComponent<PlayerAnimationSystem>()?.ResetAnimations();
        }
    }

    [Command]
    private void CmdRequestTeamAssignment()
    {
        PlayerUI_Team.PlayerInfo uiInfo = PlayerUI_Team.GetTempPlayerInfo();
        PlayerTeam newTeam = uiInfo.team != PlayerTeam.None ? uiInfo.team : PlayerTeam.Solo;
        team = newTeam;
        playerName = uiInfo.name;
    }

    [Command]
    public void CmdChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 20)
        {
            return;
        }
        playerName = newName;
    }

    [Command]
    public void CmdChangeTeam(PlayerTeam newTeam)
    {
        if (isDead)
        {
            return;
        }
        team = newTeam;
    }

    [Command]
    public void CmdJoinGuild(string newGuildId)
    {
        if (isDead) return;
        guildId = newGuildId;
        Debug.Log($"[PlayerCore] {playerName} joined guild: {newGuildId}");
    }

    [Command]
    public void CmdLeaveGuild()
    {
        if (isDead) return;
        string oldGuildId = guildId;
        guildId = "";
        Debug.Log($"[PlayerCore] {playerName} left guild: {oldGuildId}");
    }

    [Command]
    public void CmdJoinParty(string newPartyId)
    {
        if (isDead) return;
        partyId = newPartyId;
        Debug.Log($"[PlayerCore] {playerName} joined party: {newPartyId}");
    }


    [Command]
    public void CmdJoinFaction(string newFactionId)
    {
        if (isDead) return;
        factionId = newFactionId;
        Debug.Log($"[PlayerCore] {playerName} joined faction: {newFactionId}");
    }

    [Command]
    public void CmdLeaveFaction()
    {
        if (isDead) return;
        string oldFactionId = factionId;
        factionId = "";
        Debug.Log($"[PlayerCore] {playerName} left faction: {oldFactionId}");
    }
    
    [Command]
    public void CmdInviteToParty(uint targetNetId)
    {
        if (isDead) return;
        
        // Проверяем, что цель существует
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Target player {targetNetId} not found for party invite");
            return;
        }
        
        PlayerCore targetPlayer = targetIdentity.GetComponent<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Target player {targetNetId} has no PlayerCore component");
            return;
        }
        
        // Проверяем, что цель не в группе
        if (!string.IsNullOrEmpty(targetPlayer.partyId))
        {
            Debug.Log($"[PlayerCore] Target player {targetPlayer.playerName} is already in a party");
            return;
        }
        
        // Отправляем приглашение (группа создается только после принятия)
        targetPlayer.RpcShowPartyInvite(playerName, netId);
        Debug.Log($"[PlayerCore] {playerName} invited {targetPlayer.playerName} to party");
    }
    
    [Command]
    public void CmdAcceptPartyInvite(uint inviterNetId)
    {
        if (isDead) return;
        
        // Проверяем, что приглашающий существует
        if (!NetworkServer.spawned.TryGetValue(inviterNetId, out NetworkIdentity inviterIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Inviter player {inviterNetId} not found");
            return;
        }
        
        PlayerCore inviterPlayer = inviterIdentity.GetComponent<PlayerCore>();
        if (inviterPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Inviter player {inviterNetId} has no PlayerCore component");
            return;
        }
        
        // Создаем группу, если приглашающий не в группе
        if (string.IsNullOrEmpty(inviterPlayer.partyId))
        {
            // Создаем новую группу
            string newPartyId = System.Guid.NewGuid().ToString();
            inviterPlayer.partyId = newPartyId;
            inviterPlayer.isPartyLeader = true; // Приглашающий становится лидером
            partyId = newPartyId;
            isPartyLeader = false; // Принимающий не лидер
            Debug.Log($"[PlayerCore] Created new party {newPartyId} with {inviterPlayer.playerName} as leader and {playerName}");
        }
        else
        {
            // Присоединяемся к существующей группе приглашающего
            partyId = inviterPlayer.partyId;
            isPartyLeader = false; // Присоединяющийся не лидер
            Debug.Log($"[PlayerCore] {playerName} joined existing party {partyId} with {inviterPlayer.playerName}");
        }
    }
    
    [Command]
    public void CmdDeclinePartyInvite(uint inviterNetId)
    {
        if (isDead) return;
        
        // Проверяем, что приглашающий существует
        if (!NetworkServer.spawned.TryGetValue(inviterNetId, out NetworkIdentity inviterIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Inviter player {inviterNetId} not found");
            return;
        }
        
        PlayerCore inviterPlayer = inviterIdentity.GetComponent<PlayerCore>();
        if (inviterPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Inviter player {inviterNetId} has no PlayerCore component");
            return;
        }
        
        Debug.Log($"[PlayerCore] {playerName} declined party invite from {inviterPlayer.playerName}");
    }
    
    [Command]
    public void CmdLeaveParty()
    {
        if (isDead) return;
        
        if (string.IsNullOrEmpty(partyId))
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to leave party but is not in any party");
            return;
        }
        
        string oldPartyId = partyId;
        partyId = "";
        Debug.Log($"[PlayerCore] {playerName} left party {oldPartyId}");
        
        // Проверяем, сколько игроков осталось в группе
        CheckAndDisbandPartyIfNeeded(oldPartyId);
    }
    
    [Command]
    public void CmdRequestToJoinParty(uint targetNetId)
    {
        if (isDead) return;
        
        // Проверяем, что целевой игрок существует
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Target player {targetNetId} not found");
            return;
        }
        
        PlayerCore targetPlayer = targetIdentity.GetComponent<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Target player {targetNetId} has no PlayerCore component");
            return;
        }
        
        // Проверяем, что целевой игрок в группе
        if (string.IsNullOrEmpty(targetPlayer.partyId))
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to request join party but {targetPlayer.playerName} is not in any party");
            return;
        }
        
        // Проверяем, что мы не в группе
        if (!string.IsNullOrEmpty(partyId))
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to request join party but is already in party {partyId}");
            return;
        }
        
        // Отправляем запрос на присоединение
        targetPlayer.RpcShowJoinPartyRequest(playerName, netId);
        Debug.Log($"[PlayerCore] {playerName} requested to join party of {targetPlayer.playerName}");
    }
    
    [Command]
    public void CmdAcceptJoinRequest(uint requesterNetId)
    {
        if (isDead) return;
        
        // Проверяем, что запрашивающий существует
        if (!NetworkServer.spawned.TryGetValue(requesterNetId, out NetworkIdentity requesterIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Requester player {requesterNetId} not found");
            return;
        }
        
        PlayerCore requesterPlayer = requesterIdentity.GetComponent<PlayerCore>();
        if (requesterPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Requester player {requesterNetId} has no PlayerCore component");
            return;
        }
        
        // Проверяем, что мы в группе
        if (string.IsNullOrEmpty(partyId))
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to accept join request but is not in any party");
            return;
        }
        
        // Проверяем, что запрашивающий не в группе
        if (!string.IsNullOrEmpty(requesterPlayer.partyId))
        {
            Debug.LogWarning($"[PlayerCore] {requesterPlayer.playerName} tried to join party but is already in party {requesterPlayer.partyId}");
            return;
        }
        
        // Присоединяем запрашивающего к нашей группе
        requesterPlayer.partyId = partyId;
        requesterPlayer.isPartyLeader = false; // Присоединяющийся не лидер
        Debug.Log($"[PlayerCore] {playerName} accepted join request from {requesterPlayer.playerName}, added to party {partyId}");
    }
    
    [Command]
    public void CmdDeclineJoinRequest(uint requesterNetId)
    {
        if (isDead) return;
        
        // Проверяем, что запрашивающий существует
        if (!NetworkServer.spawned.TryGetValue(requesterNetId, out NetworkIdentity requesterIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Requester player {requesterNetId} not found");
            return;
        }
        
        PlayerCore requesterPlayer = requesterIdentity.GetComponent<PlayerCore>();
        if (requesterPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Requester player {requesterNetId} has no PlayerCore component");
            return;
        }
        
        Debug.Log($"[PlayerCore] {playerName} declined join request from {requesterPlayer.playerName}");
    }
    
    [Command]
    public void CmdKickFromParty(uint targetNetId)
    {
        if (isDead) return;
        
        // Проверяем, что мы лидер группы
        if (!isPartyLeader)
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to kick from party but is not party leader");
            return;
        }
        
        // Проверяем, что целевой игрок существует
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            Debug.LogWarning($"[PlayerCore] Target player {targetNetId} not found");
            return;
        }
        
        PlayerCore targetPlayer = targetIdentity.GetComponent<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogWarning($"[PlayerCore] Target player {targetNetId} has no PlayerCore component");
            return;
        }
        
        // Проверяем, что целевой игрок в нашей группе
        if (string.IsNullOrEmpty(targetPlayer.partyId) || targetPlayer.partyId != partyId)
        {
            Debug.LogWarning($"[PlayerCore] {targetPlayer.playerName} is not in the same party as {playerName}");
            return;
        }
        
        // Проверяем, что мы не исключаем сами себя
        if (targetPlayer.netId == netId)
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to kick themselves from party");
            return;
        }
        
        // Исключаем игрока из группы
        string oldPartyId = targetPlayer.partyId;
        targetPlayer.partyId = "";
        targetPlayer.isPartyLeader = false; // Снимаем статус лидера (на всякий случай)
        
        Debug.Log($"[PlayerCore] {playerName} kicked {targetPlayer.playerName} from party {oldPartyId}");
        
        // Проверяем, нужно ли распустить группу или назначить нового лидера
        CheckAndDisbandPartyIfNeeded(oldPartyId);
    }
    
    /// <summary>
    /// Тестовый метод для симуляции отключения игрока (только для отладки)
    /// </summary>
    [Command]
    public void CmdTestDisconnect()
    {
        if (!isLocalPlayer) return; // Только локальный игрок может вызвать тест
        
        Debug.Log($"[PlayerCore] Testing disconnect scenario for {playerName}");
        OnDestroy();
    }
    
    /// <summary>
    /// Запрашивает состояние здоровья участников группы
    /// </summary>
    [Command]
    public void CmdRequestPartyHealthStatus()
    {
        if (isDead) return;
        
        if (string.IsNullOrEmpty(partyId))
        {
            Debug.LogWarning($"[PlayerCore] {playerName} tried to request party health but is not in any party");
            return;
        }
        
        Debug.Log($"[PlayerCore] {playerName} requested party health status for party {partyId}");
        
        // Находим всех участников группы
        PlayerCore[] allPlayers = FindObjectsOfType<PlayerCore>();
        foreach (PlayerCore player in allPlayers)
        {
            if (!string.IsNullOrEmpty(player.partyId) && player.partyId == partyId)
            {
                // Отправляем состояние здоровья каждого участника
                RpcReceivePartyMemberHealth(player.netId, player.playerName, player.Health.CurrentHealth, player.Health.MaxHealth);
            }
        }
    }
    
    /// <summary>
    /// Получает состояние здоровья участника группы
    /// </summary>
    [ClientRpc]
    public void RpcReceivePartyMemberHealth(uint memberNetId, string memberName, int currentHealth, int maxHealth)
    {
        Debug.Log($"[PlayerCore] Received health status for {memberName}: {currentHealth}/{maxHealth}");
        
        // Обновляем PartyUIPanel если она существует
        if (PartyUIPanel.Instance != null)
        {
            PartyUIPanel.Instance.UpdatePartyMemberHealth(memberNetId, currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// Проверяет количество участников в группе и распускает группу, если остается только один игрок
    /// </summary>
    private void CheckAndDisbandPartyIfNeeded(string partyIdToCheck)
    {
        if (string.IsNullOrEmpty(partyIdToCheck)) return;
        
        // Находим всех игроков в этой группе (только активных/подключенных)
        PlayerCore[] allPlayers = FindObjectsOfType<PlayerCore>();
        int playersInParty = 0;
        PlayerCore lastPlayerInParty = null;
        
        foreach (PlayerCore player in allPlayers)
        {
            if (!string.IsNullOrEmpty(player.partyId) && player.partyId == partyIdToCheck)
            {
                playersInParty++;
                lastPlayerInParty = player;
            }
        }
        
        Debug.Log($"[PlayerCore] Party {partyIdToCheck} has {playersInParty} active members");
        
        // Если в группе остался только один игрок, распускаем группу
        if (playersInParty == 1 && lastPlayerInParty != null)
        {
            lastPlayerInParty.partyId = "";
            lastPlayerInParty.isPartyLeader = false; // Снимаем статус лидера
            Debug.Log($"[PlayerCore] Party {partyIdToCheck} disbanded - only {lastPlayerInParty.playerName} remained (disconnect scenario)");
            
            // Принудительно обновляем UI для игрока, который остался один
            lastPlayerInParty.UpdateUI();
            if (lastPlayerInParty.isLocalPlayer)
            {
                lastPlayerInParty.UpdateAllPlayerNameTags();
            }
        }
        // Если в группе больше одного игрока, но нет лидера, назначаем нового лидера
        else if (playersInParty > 1)
        {
            bool hasLeader = false;
            PlayerCore newLeader = null;
            
            foreach (PlayerCore player in allPlayers)
            {
                if (!string.IsNullOrEmpty(player.partyId) && player.partyId == partyIdToCheck)
                {
                    if (player.isPartyLeader)
                    {
                        hasLeader = true;
                        break;
                    }
                    if (newLeader == null)
                    {
                        newLeader = player; // Первый найденный игрок становится кандидатом в лидеры
                    }
                }
            }
            
            // Если нет лидера, назначаем нового
            if (!hasLeader && newLeader != null)
            {
                newLeader.isPartyLeader = true;
                Debug.Log($"[PlayerCore] Party {partyIdToCheck} - {newLeader.playerName} became the new leader (disconnect scenario)");
                
                // Принудительно обновляем UI для нового лидера
                newLeader.UpdateUI();
                if (newLeader.isLocalPlayer)
                {
                    newLeader.UpdateAllPlayerNameTags();
                }
            }
        }
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

    [Command]
    public void CmdRequestRespawn()
    {
        if (isDead)
        {
            ServerRespawnPlayer(GetTeamSpawnPoint(team).position);
        }
    }

    [Command]
    public void CmdSetClass(CharacterClass newClass)
    {
        if (Stats != null)
        {
            Stats.CmdSetClass(newClass);
        }
    }

    private void OnTeamChanged(PlayerTeam oldTeam, PlayerTeam newTeam)
    {
        UpdateTeamIndicatorColor();
        if (nameTagUI != null)
        {
            nameTagUI.UpdateNameAndTeam(playerName, newTeam, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None);
        }
    }

    private void OnNameChanged(string oldName, string newName)
    {
        if (_nameText != null)
        {
            _nameText.text = newName;
        }
        if (nameTagUI != null)
        {
            nameTagUI.UpdateNameAndTeam(newName, team, localPlayerCoreInstance != null ? localPlayerCoreInstance.team : PlayerTeam.None, isLocalPlayer);
        }
    }

    private void OnGuildChanged(string oldGuildId, string newGuildId)
    {
        Debug.Log($"[PlayerCore] {playerName} guild changed: {oldGuildId} -> {newGuildId}");
        UpdateUI();
    }

    private void OnPartyChanged(string oldPartyId, string newPartyId)
    {
        Debug.Log($"[PlayerCore] {playerName} party changed: {oldPartyId} -> {newPartyId}");
        UpdateUI();
        // Обновляем UI всех игроков, чтобы отразить изменения в party
        UpdateAllPlayerNameTags();
    }
    
    private void OnPartyLeaderChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[PlayerCore] {playerName} party leader status changed: {oldValue} -> {newValue}");
        UpdateUI();
        // Обновляем UI всех игроков, чтобы отразить изменения в лидерстве
        UpdateAllPlayerNameTags();
    }

    private void OnFactionChanged(string oldFactionId, string newFactionId)
    {
        Debug.Log($"[PlayerCore] {playerName} faction changed: {oldFactionId} -> {newFactionId}");
        UpdateUI();
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
            if (IsAlly(localPlayerCoreInstance))
            {
                rend.material = allyMaterial;
            }
            else
            {
                rend.material = enemyMaterial;
            }
        }
    }
    
    /// <summary>
    /// Checks if target player is an ally
    /// Supports dynamic teams: guild, party, faction, and basic teams
    /// </summary>
    private bool IsAlly(PlayerCore target)
    {
        if (target == null) return false;
        
        // A player is always an ally to themselves
        if (this == target)
        {
            return true;
        }
        
        // Check basic team logic first
        if (team == target.team && team != PlayerTeam.Solo)
        {
            return true;
        }
        
        // Check guild membership
        if (!string.IsNullOrEmpty(guildId) && guildId == target.guildId)
        {
            return true;
        }
        
        // Check party membership
        if (!string.IsNullOrEmpty(partyId) && partyId == target.partyId)
        {
            return true;
        }
        
        // Check faction membership
        if (!string.IsNullOrEmpty(factionId) && factionId == target.factionId)
        {
            return true;
        }
        
        // Solo players are enemies to each other (if not in same dynamic team)
        if (team == PlayerTeam.Solo && target.team == PlayerTeam.Solo)
        {
            return false;
        }
        
        return false;
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
            if (boxCollider != null) boxCollider.enabled = false;
            if (reviveCollider != null) reviveCollider.enabled = true;
            if (healthBarUI != null) healthBarUI.gameObject.SetActive(false);
        }
        else
        {
            if (Combat != null) Combat.enabled = true;
            if (Skills != null) Skills.enabled = true;
            if (Movement != null) Movement.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;
            if (reviveCollider != null) reviveCollider.enabled = false;
            if (healthBarUI != null && Health != null)
            {
                healthBarUI.gameObject.SetActive(Health.CurrentHealth > 0);
                healthBarUI.UpdateHP(Health.CurrentHealth, Health.MaxHealth);
            }
        }
    }

    private void OnStunStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleStunEffect(newValue);
        if (newValue && ActionSystem != null) ActionSystem.CompleteAction();
    }

    private void OnSilenceStateChanged(bool oldValue, bool newValue)
    {
        if (Skills != null) Skills.HandleSilenceEffect(newValue);
    }

    [Server]
    public void ApplyControlEffect(ControlEffectType effectType, float duration, int skillWeight, float slowPercentage = 0f)
    {
        if (effectType == ControlEffectType.Stun)
        {
            if (isStunned && Time.time < stunEffectEndTime && skillWeight <= stunEffectWeight)
            {
                return;
            }
            ClearStunEffect();
            isStunned = true;
            stunEffectEndTime = Time.time + duration;
            stunEffectWeight = skillWeight;
        }
        else if (effectType == ControlEffectType.Slow)
        {
            Stats.ApplySlow(slowPercentage, duration, "ControlEffect");
        }
        else if (effectType == ControlEffectType.Silence)
        {
            if (isSilenced && Time.time < silenceEffectEndTime && skillWeight <= silenceEffectWeight)
            {
                return;
            }
            ClearSilenceEffect();
            isSilenced = true;
            silenceEffectEndTime = Time.time + duration;
            silenceEffectWeight = skillWeight;
        }
    }

    [Server]
    public void ApplySlow(float percentage, float duration, int skillWeight)
    {
        Stats.ApplySlow(percentage, duration, "ApplySlow");
    }

    [Server]
    public void ClearStunEffect()
    {
        if (isStunned)
        {
            isStunned = false;
            stunEffectEndTime = 0f;
            stunEffectWeight = 0;
        }
    }

    [Server]
    private void ClearSlowEffect()
    {
    }

    [Server]
    private void ClearSilenceEffect()
    {
        if (isSilenced)
        {
            isSilenced = false;
            silenceEffectEndTime = 0f;
            silenceEffectWeight = 0;
        }
    }

    [Server]
    public void ClearNegativeEffectsExceptStun()
    {
        ClearSilenceEffect();
        if (Stats != null)
        {
            Stats.ClearSlowEffects();
        }
    }

    [Command]
    private void CmdDie()
    {
        SetDeathState(true);
    }

    [Server]
    public void SetDeathState(bool state)
    {
        isDead = state;
        if (state) deathPosition = transform.position;
    }

    public override void OnStopClient()
    {
        if (healthBarUI != null) Destroy(healthBarUI.gameObject);
        if (nameTagUI != null) Destroy(nameTagUI.gameObject);
    }

    public override void OnStopLocalPlayer()
    {
        if (localPlayerCoreInstance == this)
        {
            localPlayerCoreInstance = null;
        }
        base.OnStopLocalPlayer();
    }

    public void OnDestroy()
    {
        // Обрабатываем отключение игрока из группы
        if (!string.IsNullOrEmpty(partyId))
        {
            Debug.Log($"[PlayerCore] Player {playerName} disconnected from party {partyId} (was leader: {isPartyLeader})");
            
            // Проверяем, нужно ли распустить группу или назначить нового лидера
            CheckAndDisbandPartyIfNeeded(partyId);
        }
        
        if (healthBarUI != null) Destroy(healthBarUI.gameObject);
        if (nameTagUI != null) Destroy(nameTagUI.gameObject);
    }

    public GameObject GetHealthBarPrefab() { return null; }

    public void SetHealthBarUI(HealthBarUI ui) { healthBarUI = ui; }

    public HealthBarUI GetHealthBarUI() { return healthBarUI; }

    public int GetCurrentHealth() { return Health != null ? Health.CurrentHealth : 0; }

    public int GetMaxHealth() { return Health != null ? Health.MaxHealth : 0; }

    public NameTagUI GetNameTagUI() { return nameTagUI; }

    public bool CanCastSkill(ISkill skill = null)
    {
        if (skill != null && skill is BasicAttackSkill)
        {
            return !isDead && !isStunned;
        }
        
        // Allow revive skills to be cast even when caster is alive (for reviving dead teammates)
        if (skill != null && skill is ReviveSkill)
        {
            return !isStunned && !isSilenced;
        }
        
        return !isDead && !isStunned && !isSilenced;
    }

    [Command]
    public void CmdRequestRevive(uint targetNetId)
    {
        NetworkIdentity targetIdentity;
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out targetIdentity)) return;
        PlayerCore target = targetIdentity.GetComponent<PlayerCore>();
        if (target == null || !target.isDead) return; // Removed team check - can revive anyone
        target.RpcShowReviveRequest(netId);
    }

    [ClientRpc]
    public void RpcShowReviveRequest(uint casterNetId)
    {
        if (!isLocalPlayer || reviveRequestUI == null) return;
        NetworkIdentity casterIdentity;
        string casterName = "";
        if (NetworkClient.spawned.TryGetValue(casterNetId, out casterIdentity))
        {
            PlayerCore caster = casterIdentity.GetComponent<PlayerCore>();
            if (caster != null)
            {
                casterName = caster.playerName;
            }
        }
        reviveRequestUI.Show(casterName);
    }
    
    [ClientRpc]
    public void RpcShowPartyInvite(string inviterName, uint inviterNetId)
    {
        if (!isLocalPlayer) return;
        
        // Показываем приглашение через PartyInviteUI
        if (PartyInviteUI.Instance != null)
        {
            PartyInviteUI.Instance.ShowInvite(inviterName, inviterNetId);
        }
        else
        {
            Debug.LogWarning("[PlayerCore] PartyInviteUI.Instance is null, cannot show party invite");
        }
    }
    
    [ClientRpc]
    public void RpcShowJoinPartyRequest(string requesterName, uint requesterNetId)
    {
        if (!isLocalPlayer) return;
        
        // Показываем запрос на присоединение к группе
        if (PartyInviteUI.Instance != null)
        {
            PartyInviteUI.Instance.ShowJoinRequest(requesterName, requesterNetId);
        }
        else
        {
            Debug.LogWarning("[PlayerCore] PartyInviteUI.Instance is null, cannot show join party request");
        }
    }

    [Command]
    public void CmdAcceptRevive()
    {
        if (!isDead) return;
        ServerRespawnPlayer(deathPosition, pendingReviveHpFraction);
        pendingReviveHpFraction = 0f;
    }

    [Command]
    public void CmdDropItem(int itemID, int slotIndex)
    {
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] CmdDropItem failed: Inventory is null!");
            return;
        }
        Item item = Resources.Load<ItemDatabase>("ItemDatabase")?.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[PlayerCore] CmdDropItem failed: Item with ID {itemID} not found");
            return;
        }
        if (slotIndex >= 0 && slotIndex < this.Inventory.items.Count && this.Inventory.items[slotIndex].id == itemID && item.canDrop)
        {
            var instance = this.Inventory.items[slotIndex];
            int dropQuantity = instance.quantity;
            if (dropQuantity <= 0) return;
            this.Inventory.ClearItemSlot(slotIndex);
            RpcUpdateInventoryUI();
            RpcClearHotbarItem(itemID);
            // Item dropped - передаем ItemInfo с динамическими статами
            SpawnDroppedItemWithItemInfo(instance, dropQuantity);
        }
    }

    [Server]
    private void SpawnDroppedItem(int itemID, int quantity)
    {
        if (quantity <= 0 || droppedItemPrefab == null)
        {
            Debug.LogError($"[PlayerCore] Cannot spawn dropped item: quantity {quantity} or prefab null");
            return;
        }
        GameObject droppedItem = Instantiate(droppedItemPrefab, transform.position + Random.insideUnitSphere * 1f + Vector3.up * 0.5f, Quaternion.identity);
        DroppedItem droppedScript = droppedItem.GetComponent<DroppedItem>();
        if (droppedScript != null)
        {
            droppedScript.itemID = itemID;
            droppedScript.quantity = quantity;
        }
        NetworkServer.Spawn(droppedItem);
        // Dropped item spawned
    }
    
    [Server]
    private void SpawnDroppedItemWithItemInfo(ItemInfo itemInfo, int quantity)
    {
        if (quantity <= 0 || droppedItemPrefab == null)
        {
            Debug.LogError($"[PlayerCore] Cannot spawn dropped item: quantity {quantity} or prefab null");
            return;
        }
        GameObject droppedItem = Instantiate(droppedItemPrefab, transform.position + Random.insideUnitSphere * 1f + Vector3.up * 0.5f, Quaternion.identity);
        DroppedItem droppedScript = droppedItem.GetComponent<DroppedItem>();
        if (droppedScript != null)
        {
            // Создаем ItemInfo с правильным количеством
            ItemInfo dropItemInfo = itemInfo;
            dropItemInfo.quantity = quantity;
            
            // Инициализируем дропнутый предмет с ItemInfo
            droppedScript.InitializeWithDynamicItemInfo(dropItemInfo);
            droppedScript.ownerNetId = netId;
            droppedScript.dropTime = Time.time;
        }
        NetworkServer.Spawn(droppedItem);
        Debug.Log($"[PlayerCore] Dropped item with dynamic stats: {itemInfo.GetItemName()} (ID: {itemInfo.id}, quantity: {quantity})");
    }

    [Command]
    public void CmdSellItem(int itemID, int slotIndex)
    {
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] CmdSellItem failed: Inventory is null!");
            return;
        }
        Item item = Resources.Load<ItemDatabase>("ItemDatabase")?.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[PlayerCore] CmdSellItem failed: Item with ID {itemID} not found");
            return;
        }
        if (slotIndex >= 0 && slotIndex < this.Inventory.items.Count && this.Inventory.items[slotIndex].id == itemID && item.canSell)
        {
            var instance = this.Inventory.items[slotIndex];
            instance.quantity--;
            this.Inventory.items[slotIndex] = instance;
            if (this.Inventory.items[slotIndex].quantity <= 0)
                this.Inventory.ClearItemSlot(slotIndex);
            RpcUpdateInventoryUI();
            // Item sold
        }
    }

    [Command]
    public void CmdSelectItem(int itemID, int slotIndex)
    {
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] CmdSelectItem failed: Inventory is null!");
            return;
        }
        Item item = Resources.Load<ItemDatabase>("ItemDatabase")?.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[PlayerCore] CmdSelectItem failed: Item with ID {itemID} not found");
            return;
        }
        if (slotIndex >= -1 && (slotIndex == -1 || (slotIndex < this.Inventory.items.Count && this.Inventory.items[slotIndex].id == itemID)) && item.canUse)
        {
            if (isDead || isStunned)
            {
                Debug.LogWarning($"[PlayerCore] Cannot select item {item.itemName}: player is dead or stunned");
                return;
            }
            if (item.skillEffect != null)
            {
                RpcSelectItemSkill(itemID, slotIndex);
                // Selected skill from item
                return;
            }
            item.Use(this);
            ConsumeItem(slotIndex, itemID);
            // Item used
        }
    }

    [Command]
    public void CmdConsumeItem(int itemID, int slotIndex)
    {
        if (slotIndex >= 0)
        {
            var instance = this.Inventory.items[slotIndex];
            if (instance.id != itemID) return;
            instance.quantity--;
            this.Inventory.items[slotIndex] = instance;
            if (this.Inventory.items[slotIndex].quantity <= 0)
            {
                this.Inventory.ClearItemSlot(slotIndex);
                RpcClearHotbarItem(itemID);
            }
            RpcUpdateInventoryUI();
        }
        // Item consumed
    }

    [Server]
    private void ConsumeItem(int slotIndex, int itemID)
    {
        if (slotIndex >= 0)
        {
            var instance = this.Inventory.items[slotIndex];
            if (instance.id != itemID) return;
            instance.quantity--;
            this.Inventory.items[slotIndex] = instance;
            if (this.Inventory.items[slotIndex].quantity <= 0)
            {
                this.Inventory.ClearItemSlot(slotIndex);
                RpcClearHotbarItem(itemID);
            }
            RpcUpdateInventoryUI();
        }
    }

    [Command]
    public void CmdSwapInventoryItems(int slotIndex1, int slotIndex2)
    {
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] CmdSwapInventoryItems failed: Inventory is null!");
            return;
        }
        while (Inventory.items.Count < Mathf.Max(slotIndex1, slotIndex2) + 1)
        {
            if (Inventory.items.Count < Inventory.inventorySize)
            {
                Inventory.items.Add(new ItemInfo { id = 0 });
            }
            else
            {
                Debug.LogError($"[PlayerCore] CmdSwapInventoryItems failed: max slots reached");
                return;
            }
        }
        if (slotIndex1 >= Inventory.items.Count || slotIndex2 >= Inventory.items.Count)
        {
            Debug.LogError($"[PlayerCore] Invalid slot indices: {slotIndex1}/{slotIndex2}, count={Inventory.items.Count}");
            return;
        }
        var temp = Inventory.items[slotIndex1];
        Inventory.items[slotIndex1] = Inventory.items[slotIndex2];
        Inventory.items[slotIndex2] = temp;
        RpcUpdateInventoryUI();
        // Slots swapped
    }

    [Command]
    public void CmdEquipItem(ItemInfo itemInfo, int slotIndex, EquipmentSlot slot)
    {
        if (!isServer) return;
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] Inventory component not found!");
            return;
        }
        Item item = itemInfo.GetItem();
        if (item == null)
        {
            Debug.LogError($"[PlayerCore] Cannot equip item: Item with ID {itemInfo.id} not found");
            return;
        }
        if (!item.IsEquipable(Stats.level, Stats.characterClass))
        {
            Debug.LogError($"[PlayerCore] Cannot equip item: {item.itemName}, player level {Stats.level} or class {Stats.characterClass} does not match required level {item.requiredLevel} or class {item.characterClass}");
            return;
        }
        if (!item.CanEquipToSlot(slot))
        {
            Debug.LogError($"[PlayerCore] Cannot equip item: {item.itemName} cannot be equipped to slot {slot}");
            return;
        }
        Debug.Log($"[PlayerCore] Equipping item: {item.itemName} (ID: {itemInfo.id}) to {slot} from slot {slotIndex}");
        Inventory.EquipItem(itemInfo, slot, slotIndex);
        
        // Принудительно обновляем визуалы экипировки на всех клиентах
        Inventory.RpcForceUpdateAllEquipmentVisuals();
    }

    [Command]
    public void CmdUnequipItem(EquipmentSlot slotType)
    {
        if (Inventory == null)
        {
            Debug.LogError("[PlayerCore] CmdUnequipItem failed: Inventory is null!");
            return;
        }
        this.Inventory.UnequipItem(slotType);
        RpcUpdateInventoryUI();
        RpcUpdateEquipmentUI();
        
        // Принудительно обновляем визуалы экипировки на всех клиентах
        Inventory.RpcForceUpdateAllEquipmentVisuals();
        Debug.Log($"[PlayerCore] Unequipped item from slot: {slotType}");
    }

    [Command]
    public void CmdPickupDroppedItem(uint droppedItemNetId)
    {
        if (!NetworkServer.spawned.ContainsKey(droppedItemNetId)) return;
        DroppedItem droppedItem = NetworkServer.spawned[droppedItemNetId].GetComponent<DroppedItem>();
        if (droppedItem == null) return;
        float distance = Vector3.Distance(transform.position, droppedItem.transform.position);
        if (distance > droppedItem.pickupDistance) return;
        droppedItem.Pickup(this);
    }

    [ClientRpc]
    private void RpcUpdateInventoryUI()
    {
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.UpdateInventoryUI();
    }

    [ClientRpc]
    private void RpcUpdateEquipmentUI()
    {
        if (InventoryUI.Instance != null) InventoryUI.Instance.UpdateEquipmentUI();
    }

    [ClientRpc]
    private void RpcClearHotbarItem(int itemId)
    {
        if (PlayerUI.Instance != null)
            PlayerUI.Instance.ClearHotbarItem(itemId);
    }

    public GameObject GetTargetIndicatorPrefab() => targetIndicatorPrefab;

    public GameObject GetMoveIndicatorPrefab() => moveIndicatorPrefab;

    private IEnumerator DelayedInventorySync()
    {
        yield return new WaitForEndOfFrame();
        if (isServer)
        {
            RpcUpdateInventoryUI();
        }
    }

    [ClientRpc]
    private void RpcSelectItemSkill(int itemID, int slotIndex)
    {
        if (!isLocalPlayer) return;
        Item item = Resources.Load<ItemDatabase>("ItemDatabase")?.GetItem(itemID);
        if (item != null && item.skillEffect != null)
        {
            item.skillEffect.Init(this);
            if (Skills != null)
            {
                if (item.skillEffect.CastTime > 0)
                {
                    Skills.SelectSkill(item.skillEffect);
                    Debug.Log($"[PlayerCore] Selected skill {item.skillEffect.SkillName} for casting from item {item.itemName}");
                }
                else
                {
                    Ray ray = Camera.CameraInstance.ScreenPointToRay(Input.mousePosition);
                    Vector3? targetPos = null;
                    if (Physics.Raycast(ray, out RaycastHit hit, item.castRange, LayerMask.GetMask("Ground")))
                    {
                        targetPos = hit.point;
                    }
                    else
                    {
                        targetPos = transform.position + transform.forward * item.castRange;
                    }
                    Skills.CmdExecuteSkill(this, targetPos, 0, item.skillEffect.SkillName, 0);
                    if (slotIndex >= 0)
                    {
                        CmdConsumeItem(itemID, slotIndex);
                    }
                }
            }
        }
    }

    [Command]
    public void CmdStackItems(int fromSlot, int toSlot, int maxTransfer)
    {
        if (fromSlot < 0 || toSlot < 0 || fromSlot >= Inventory.items.Count || toSlot >= Inventory.items.Count) return;
        var fromItem = Inventory.items[fromSlot];
        var toItem = Inventory.items[toSlot];
        if (fromItem.id != toItem.id || fromItem.id <= 0 || toItem.quantity >= toItem.GetItem().maxStack) return;
        Item item = toItem.GetItem();
        int transfer = Mathf.Min(fromItem.quantity, maxTransfer);
        toItem.quantity += transfer;
        fromItem.quantity -= transfer;
        Inventory.items[toSlot] = toItem;
        if (fromItem.quantity <= 0)
        {
            this.Inventory.ClearItemSlot(fromSlot);
            RpcClearHotbarItem(fromItem.id);
        }
        else
        {
            Inventory.items[fromSlot] = fromItem;
        }
        RpcUpdateInventoryUI();
        Debug.Log($"[PlayerCore] Stacked {transfer} from slot {fromSlot} to {toSlot}");
    }

    [ClientRpc]
    private void RpcUpdateHotbarSlotIndices(int removedSlot)
    {
        if (PlayerUI.Instance == null) return;
        var hotbarButtons = PlayerUI.Instance.GetSkillButtons2().Concat(PlayerUI.Instance.GetSkillButtons3());
        foreach (var btn in hotbarButtons)
        {
            if (btn.item != null && btn.itemSlotIndex > removedSlot)
            {
                btn.itemSlotIndex--;
                Debug.Log($"[PlayerCore] Updated hotbar {btn.buttonIndex} slotIndex: {btn.itemSlotIndex + 1} -> {btn.itemSlotIndex}");
            }
        }
    }

    private Transform GetTeamSpawnPoint(PlayerTeam team)
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        foreach (GameObject spawnPoint in spawnPoints)
        {
            TeamSpawnPoint teamSpawn = spawnPoint.GetComponent<TeamSpawnPoint>();
            if (teamSpawn != null && teamSpawn.team == team)
            {
                return spawnPoint.transform;
            }
        }
        if (spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
        }
        Debug.LogWarning("No spawn points for team " + team);
        return transform;
    }

    [ClientRpc]
    public void RpcDisableNT()
    {
        GetComponent<NetworkTransformHybrid>().enabled = false;
    }

    [ClientRpc]
    public void RpcEnableNT()
    {
        GetComponent<NetworkTransformHybrid>().enabled = true;
    }
    
    private void CreateContextMenuUI()
    {
        // Находим Canvas в PlayerUI
        PlayerUI playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError("[PlayerCore] PlayerUI not found for ContextMenuUI creation!");
            return;
        }
        
        Canvas playerCanvas = playerUI.GetComponentInParent<Canvas>();
        if (playerCanvas == null)
        {
            Debug.LogError("[PlayerCore] Canvas not found in PlayerUI for ContextMenuUI creation!");
            return;
        }
        
        Debug.Log($"[PlayerCore] Found Canvas: {playerCanvas.name}, Render Mode: {playerCanvas.renderMode}");
        
        // Создаем GameObject для ContextMenuUI
        GameObject contextMenuObject = new GameObject("ContextMenuUI");
        contextMenuObject.transform.SetParent(playerCanvas.transform, false);
        
        // Убеждаемся, что Canvas имеет высокий Sort Order для отображения поверх других UI
        if (playerCanvas.sortingOrder < 100)
        {
            playerCanvas.sortingOrder = 100;
            Debug.Log($"[PlayerCore] Set Canvas sort order to: {playerCanvas.sortingOrder}");
        }
        
        // Добавляем RectTransform если его нет
        RectTransform rectTransform = contextMenuObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = contextMenuObject.AddComponent<RectTransform>();
        }
        
        // Настраиваем RectTransform
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Добавляем ContextMenuUI компонент
        ContextMenuUI contextMenuUI = contextMenuObject.AddComponent<ContextMenuUI>();
        
        Debug.Log("[PlayerCore] ContextMenuUI created successfully for local player");
    }
    
    private void CreatePartyInviteUI()
    {
        // Находим Canvas в PlayerUI
        PlayerUI playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError("[PlayerCore] PlayerUI not found for PartyInviteUI creation!");
            return;
        }
        
        Canvas playerCanvas = playerUI.GetComponentInParent<Canvas>();
        if (playerCanvas == null)
        {
            Debug.LogError("[PlayerCore] Canvas not found in PlayerUI for PartyInviteUI creation!");
            return;
        }
        
        Debug.Log($"[PlayerCore] Found Canvas for PartyInviteUI: {playerCanvas.name}, Render Mode: {playerCanvas.renderMode}");
        
        // Создаем GameObject для PartyInviteUI
        GameObject partyInviteObject = new GameObject("PartyInviteUI");
        partyInviteObject.transform.SetParent(playerCanvas.transform, false);
        
        // Добавляем RectTransform если его нет
        RectTransform rectTransform = partyInviteObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = partyInviteObject.AddComponent<RectTransform>();
        }
        
        // Настраиваем RectTransform
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Добавляем PartyInviteUI компонент
        PartyInviteUI partyInviteUI = partyInviteObject.AddComponent<PartyInviteUI>();
        
        Debug.Log("[PlayerCore] PartyInviteUI created successfully for local player");
    }
    
    private void CreatePartyUIPanel()
    {
        Debug.Log("[PlayerCore] CreatePartyUIPanel() called");
        
        // Проверяем, не создана ли уже панель
        if (PartyUIPanel.Instance != null)
        {
            Debug.LogWarning("[PlayerCore] PartyUIPanel already exists, skipping creation");
            return;
        }
        
        // Находим Canvas в PlayerUI
        PlayerUI playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError("[PlayerCore] PlayerUI not found for PartyUIPanel creation!");
            return;
        }
        
        Canvas playerCanvas = playerUI.GetComponentInParent<Canvas>();
        if (playerCanvas == null)
        {
            Debug.LogError("[PlayerCore] Canvas not found in PlayerUI for PartyUIPanel creation!");
            return;
        }
        
        Debug.Log($"[PlayerCore] Found Canvas for PartyUIPanel: {playerCanvas.name}, Render Mode: {playerCanvas.renderMode}");
        
        // Проверяем, нет ли уже PartyUIPanel в Canvas
        PartyUIPanel existingPanel = playerCanvas.GetComponentInChildren<PartyUIPanel>();
        if (existingPanel != null)
        {
            Debug.LogWarning("[PlayerCore] PartyUIPanel already exists in Canvas, skipping creation");
            return;
        }
        
        // Создаем GameObject для PartyUIPanel
        GameObject partyUIObject = new GameObject("PartyUIPanel");
        partyUIObject.transform.SetParent(playerCanvas.transform, false);
        partyUIObject.SetActive(true);
        
        // Настраиваем RectTransform для левой части экрана
        RectTransform partyUIRect = partyUIObject.AddComponent<RectTransform>();
        partyUIRect.anchorMin = new Vector2(0, 0.5f); // Левая сторона, по центру по вертикали
        partyUIRect.anchorMax = new Vector2(0, 0.5f);
        partyUIRect.pivot = new Vector2(0, 0.5f); // Левый центр
        partyUIRect.sizeDelta = new Vector2(200f, 300f); // Размер панели
        partyUIRect.anchoredPosition = new Vector2(10f, 0f); // Отступ от левого края
        
        // Добавляем PartyUIPanel компонент
        PartyUIPanel partyUIPanel = partyUIObject.AddComponent<PartyUIPanel>();
        
        // Создаем основную панель группы
        CreatePartyPanelStructure(partyUIObject, partyUIPanel);
        
        Debug.Log("[PlayerCore] PartyUIPanel created successfully for local player");
    }
    
    private void CreatePartyPanelStructure(GameObject parent, PartyUIPanel partyUIPanel)
    {
        Debug.Log("[PlayerCore] CreatePartyPanelStructure() called");
        
        // Создаем основную панель
        GameObject panel = new GameObject("PartyPanel");
        panel.transform.SetParent(parent.transform, false);
        panel.SetActive(true);
        
        // Настраиваем RectTransform панели
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Добавляем фон панели
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        panelImage.raycastTarget = false; // Отключаем raycast у фона, чтобы не было hover эффектов
        
        // Создаем контейнер для участников группы
        GameObject membersContainer = new GameObject("PartyMembersContainer");
        membersContainer.transform.SetParent(panel.transform, false);
        membersContainer.SetActive(true);
        
        // Настраиваем RectTransform контейнера
        RectTransform containerRect = membersContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(5f, 5f);
        containerRect.offsetMax = new Vector2(-5f, -5f);
        
        // Добавляем VerticalLayoutGroup для автоматического расположения
        VerticalLayoutGroup layoutGroup = membersContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 5f;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        
        // Добавляем Image для блокировки кликов через контейнер
        Image containerImage = membersContainer.AddComponent<Image>();
        containerImage.color = Color.clear; // Прозрачный, но блокирует клики
        containerImage.raycastTarget = true;
        
        // Создаем префаб слота участника группы (НЕ добавляем в контейнер)
        GameObject slotPrefab = CreatePartyMemberSlotPrefab();
        
        // Устанавливаем ссылки в PartyUIPanel
        var partyUIPanelType = typeof(PartyUIPanel);
        var panelField = partyUIPanelType.GetField("partyPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var containerField = partyUIPanelType.GetField("partyMembersContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var prefabField = partyUIPanelType.GetField("partyMemberSlotPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        panelField?.SetValue(partyUIPanel, panel);
        containerField?.SetValue(partyUIPanel, membersContainer.transform);
        prefabField?.SetValue(partyUIPanel, slotPrefab);
        
        // Создаем ClickBlocker ПОСЛЕ всех слотов, чтобы он был внизу иерархии
        // и не блокировал клики по слотам участников
        GameObject clickBlocker = new GameObject("ClickBlocker");
        clickBlocker.transform.SetParent(panel.transform, false);
        clickBlocker.SetActive(true);
        
        RectTransform blockerRect = clickBlocker.AddComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        
        Image blockerImage = clickBlocker.AddComponent<Image>();
        blockerImage.color = Color.clear; // Полностью прозрачный
        blockerImage.raycastTarget = true; // Блокирует клики только в пустых областях
        
        Debug.Log("[PlayerCore] Party panel structure created successfully");
    }
    
    private GameObject CreatePartyMemberSlotPrefab()
    {
        Debug.Log("[PlayerCore] CreatePartyMemberSlotPrefab() called");
        
        // Создаем префаб слота участника группы
        GameObject slotPrefab = new GameObject("PartyMemberSlotPrefab");
        slotPrefab.SetActive(true);
        
        // Настраиваем RectTransform слота
        RectTransform slotRect = slotPrefab.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(0f, 60f); // Высота слота
        
        // Добавляем фон слота
        Image slotImage = slotPrefab.AddComponent<Image>();
        slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Добавляем Button для кликабельности
        Button slotButton = slotPrefab.AddComponent<Button>();
        slotButton.targetGraphic = slotImage;
        
        // Создаем горизонтальный layout для содержимого слота
        GameObject contentContainer = new GameObject("ContentContainer");
        contentContainer.transform.SetParent(slotPrefab.transform, false);
        contentContainer.SetActive(true);
        
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        HorizontalLayoutGroup contentLayout = contentContainer.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 5f;
        contentLayout.padding = new RectOffset(5, 5, 5, 5);
        contentLayout.childControlWidth = false;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        
        // Создаем иконку лидера
        GameObject leaderIcon = new GameObject("LeaderIcon");
        leaderIcon.transform.SetParent(contentContainer.transform, false);
        leaderIcon.SetActive(false); // Скрыта по умолчанию
        
        RectTransform leaderRect = leaderIcon.AddComponent<RectTransform>();
        leaderRect.sizeDelta = new Vector2(20f, 20f);
        
        Image leaderImage = leaderIcon.AddComponent<Image>();
        leaderImage.color = Color.yellow;
        
        // Создаем вертикальный контейнер для текста
        GameObject textContainer = new GameObject("TextContainer");
        textContainer.transform.SetParent(contentContainer.transform, false);
        textContainer.SetActive(true);
        
        RectTransform textRect = textContainer.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(100f, 0f);
        
        VerticalLayoutGroup textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = false;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        
        // Создаем текст имени
        GameObject nameText = new GameObject("NameText");
        nameText.transform.SetParent(textContainer.transform, false);
        nameText.SetActive(true);
        
        RectTransform nameRect = nameText.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(0f, 20f);
        
        TMPro.TextMeshProUGUI nameTextComponent = nameText.AddComponent<TMPro.TextMeshProUGUI>();
        nameTextComponent.text = "Player Name";
        nameTextComponent.fontSize = 12f;
        nameTextComponent.color = Color.white;
        nameTextComponent.alignment = TMPro.TextAlignmentOptions.Left;
        
        // Создаем текст уровня
        GameObject levelText = new GameObject("LevelText");
        levelText.transform.SetParent(textContainer.transform, false);
        levelText.SetActive(true);
        
        RectTransform levelRect = levelText.AddComponent<RectTransform>();
        levelRect.sizeDelta = new Vector2(0f, 15f);
        
        TMPro.TextMeshProUGUI levelTextComponent = levelText.AddComponent<TMPro.TextMeshProUGUI>();
        levelTextComponent.text = "Lv.1";
        levelTextComponent.fontSize = 10f;
        levelTextComponent.color = Color.gray;
        levelTextComponent.alignment = TMPro.TextAlignmentOptions.Left;
        
        // Создаем health bar
        GameObject healthBar = new GameObject("HealthBar");
        healthBar.transform.SetParent(contentContainer.transform, false);
        healthBar.SetActive(true);
        
        RectTransform healthRect = healthBar.AddComponent<RectTransform>();
        healthRect.sizeDelta = new Vector2(60f, 15f);
        
        Slider healthSlider = healthBar.AddComponent<Slider>();
        healthSlider.value = 1f;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        
        // Настраиваем фон health bar
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBar.transform, false);
        background.SetActive(true);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = Color.red;
        healthSlider.targetGraphic = bgImage;
        
        // Настраиваем fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(healthBar.transform, false);
        fillArea.SetActive(true);
        
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fill.SetActive(true);
        
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        healthSlider.fillRect = fillRect;
        
        // Делаем Slider неинтерактивным (только для отображения)
        healthSlider.interactable = false;
        
        Debug.Log("[PlayerCore] Party member slot prefab created successfully");
        return slotPrefab;
    }

}