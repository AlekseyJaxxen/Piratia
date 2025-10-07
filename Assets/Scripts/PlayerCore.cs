using UnityEngine;
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
    public void CmdLeaveParty()
    {
        if (isDead) return;
        string oldPartyId = partyId;
        partyId = "";
        Debug.Log($"[PlayerCore] {playerName} left party: {oldPartyId}");
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

}