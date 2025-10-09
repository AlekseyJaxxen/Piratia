using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;

public class PartyUIPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private Transform partyMembersContainer;
    [SerializeField] private GameObject partyMemberSlotPrefab;
    
    [Header("Styling")]
    [SerializeField] private Color leaderColor = Color.yellow;
    [SerializeField] private Color memberColor = Color.white;
    [SerializeField] private Color offlineColor = Color.gray;
    
    private Dictionary<uint, GameObject> partyMemberSlots = new Dictionary<uint, GameObject>();
    private PlayerCore localPlayer;
    
    public static PartyUIPanel Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        Debug.Log("[PartyUIPanel] Start() called");
        localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer != null)
        {
            Debug.Log($"[PartyUIPanel] Local player found: {localPlayer.playerName}");
            UpdatePartyUI();
        }
        else
        {
            Debug.LogWarning("[PartyUIPanel] Local player not found in Start()");
        }
        
        // Скрываем панель по умолчанию
        if (partyPanel != null)
        {
            partyPanel.SetActive(false);
            Debug.Log("[PartyUIPanel] Party panel hidden by default");
        }
        else
        {
            Debug.LogWarning("[PartyUIPanel] Party panel is null in Start()");
        }
    }
    
    private void Update()
    {
        // Обновляем UI каждые 0.2 секунды для более быстрого обновления данных
        if (Time.frameCount % 12 == 0) // ~0.2 секунды при 60 FPS
        {
            UpdatePartyUI();
        }
        
        // Запрашиваем состояние здоровья каждые 2 секунды для актуальных данных
        if (Time.frameCount % 120 == 0) // ~2 секунды при 60 FPS
        {
            RequestPartyHealthStatus();
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("[PartyUIPanel] OnEnable() called");
    }
    
    private void OnDisable()
    {
        Debug.Log("[PartyUIPanel] OnDisable() called");
    }
    
    /// <summary>
    /// Обновляет UI группы на основе текущих участников
    /// </summary>
    public void UpdatePartyUI()
    {
        Debug.Log("[PartyUIPanel] UpdatePartyUI() called");
        
        if (localPlayer == null)
        {
            localPlayer = PlayerCore.localPlayerCoreInstance;
            if (localPlayer == null) 
            {
                Debug.LogWarning("[PartyUIPanel] Local player is null in UpdatePartyUI()");
                return;
            }
        }
        
        Debug.Log($"[PartyUIPanel] Local player: {localPlayer.playerName}, partyId: '{localPlayer.partyId}'");
        
        // Если игрок не в группе, скрываем панель
        if (string.IsNullOrEmpty(localPlayer.partyId))
        {
            Debug.Log("[PartyUIPanel] Player not in party, hiding panel");
            HidePartyPanel();
            return;
        }
        
        // Показываем панель
        Debug.Log("[PartyUIPanel] Player in party, showing panel");
        ShowPartyPanel();
        
        // Находим всех участников группы
        List<PlayerCore> partyMembers = GetPartyMembers();
        Debug.Log($"[PartyUIPanel] Found {partyMembers.Count} party members");
        
        // Обновляем слоты участников
        UpdatePartyMemberSlots(partyMembers);
    }
    
    /// <summary>
    /// Находит всех участников текущей группы
    /// </summary>
    private List<PlayerCore> GetPartyMembers()
    {
        List<PlayerCore> members = new List<PlayerCore>();
        
        if (string.IsNullOrEmpty(localPlayer.partyId)) 
        {
            Debug.Log("[PartyUIPanel] Local player has no partyId");
            return members;
        }
        
        // Находим всех игроков в той же группе
        PlayerCore[] allPlayers = FindObjectsOfType<PlayerCore>();
        Debug.Log($"[PartyUIPanel] Found {allPlayers.Length} total players");
        
        foreach (PlayerCore player in allPlayers)
        {
            Debug.Log($"[PartyUIPanel] Checking player: {player.playerName}, partyId: '{player.partyId}'");
            if (!string.IsNullOrEmpty(player.partyId) && player.partyId == localPlayer.partyId)
            {
                members.Add(player);
                Debug.Log($"[PartyUIPanel] Added to party members: {player.playerName}");
            }
        }
        
        Debug.Log($"[PartyUIPanel] Total party members found: {members.Count}");
        return members;
    }
    
    /// <summary>
    /// Обновляет слоты участников группы
    /// </summary>
    private void UpdatePartyMemberSlots(List<PlayerCore> partyMembers)
    {
        // Удаляем старые слоты для игроков, которых больше нет в группе
        List<uint> slotsToRemove = new List<uint>();
        foreach (var kvp in partyMemberSlots)
        {
            bool stillInParty = false;
            foreach (PlayerCore member in partyMembers)
            {
                if (member.netId == kvp.Key)
                {
                    stillInParty = true;
                    break;
                }
            }
            
            if (!stillInParty)
            {
                slotsToRemove.Add(kvp.Key);
            }
        }
        
        // Удаляем слоты
        foreach (uint netId in slotsToRemove)
        {
            if (partyMemberSlots.ContainsKey(netId))
            {
                Destroy(partyMemberSlots[netId]);
                partyMemberSlots.Remove(netId);
            }
        }
        
        // Создаем или обновляем слоты для текущих участников
        foreach (PlayerCore member in partyMembers)
        {
            if (partyMemberSlots.ContainsKey(member.netId))
            {
                // Обновляем существующий слот
                UpdatePartyMemberSlot(member, partyMemberSlots[member.netId]);
            }
            else
            {
                // Создаем новый слот
                CreatePartyMemberSlot(member);
            }
        }
    }
    
    /// <summary>
    /// Создает новый слот для участника группы
    /// </summary>
    private void CreatePartyMemberSlot(PlayerCore member)
    {
        Debug.Log($"[PartyUIPanel] CreatePartyMemberSlot() called for: {member.playerName}");
        
        if (partyMemberSlotPrefab == null)
        {
            Debug.LogError("[PartyUIPanel] Party member slot prefab is null!");
            return;
        }
        
        if (partyMembersContainer == null)
        {
            Debug.LogError("[PartyUIPanel] Party members container is null!");
            return;
        }
        
        GameObject slot = Instantiate(partyMemberSlotPrefab, partyMembersContainer);
        slot.name = $"PartyMember_{member.playerName}_{member.netId}";
        slot.SetActive(true);
        
        // Сохраняем ссылку на слот
        partyMemberSlots[member.netId] = slot;
        
        // Настраиваем слот
        SetupPartyMemberSlot(member, slot);
        
        Debug.Log($"[PartyUIPanel] Created slot for party member: {member.playerName}");
    }
    
    /// <summary>
    /// Настраивает слот участника группы
    /// </summary>
    private void SetupPartyMemberSlot(PlayerCore member, GameObject slot)
    {
        Debug.Log($"[PartyUIPanel] SetupPartyMemberSlot() called for: {member.playerName}");
        
        // Находим компоненты UI
        TextMeshProUGUI nameText = slot.transform.Find("ContentContainer/TextContainer/NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI levelText = slot.transform.Find("ContentContainer/TextContainer/LevelText")?.GetComponent<TextMeshProUGUI>();
        Slider healthBar = slot.transform.Find("ContentContainer/HealthBar")?.GetComponent<Slider>();
        Image leaderIcon = slot.transform.Find("ContentContainer/LeaderIcon")?.GetComponent<Image>();
        Button clickableButton = slot.GetComponent<Button>();
        
        Debug.Log($"[PartyUIPanel] Found components - NameText: {nameText != null}, LevelText: {levelText != null}, HealthBar: {healthBar != null}, LeaderIcon: {leaderIcon != null}, Button: {clickableButton != null}");
        
        // Логируем структуру слота для отладки
        Debug.Log($"[PartyUIPanel] Slot structure for {member.playerName}:");
        LogTransformStructure(slot.transform, 0);
        
        // Настраиваем имя
        if (nameText != null)
        {
            nameText.text = member.playerName;
            nameText.color = member.isPartyLeader ? leaderColor : memberColor;
            Debug.Log($"[PartyUIPanel] Set name text: {member.playerName}, color: {nameText.color}");
        }
        else
        {
            Debug.LogError("[PartyUIPanel] NameText component not found!");
        }
        
        // Настраиваем уровень (если есть CharacterStats)
        if (levelText != null)
        {
            CharacterStats stats = member.GetComponent<CharacterStats>();
            if (stats != null)
            {
                levelText.text = $"Lv.{stats.level}";
            }
            else
            {
                levelText.text = "Lv.1";
            }
            Debug.Log($"[PartyUIPanel] Set level text: {levelText.text}");
        }
        else
        {
            Debug.LogError("[PartyUIPanel] LevelText component not found!");
        }
        
        // Настраиваем health bar
        if (healthBar != null)
        {
            Health health = member.GetComponent<Health>();
            if (health != null)
            {
                healthBar.value = (float)health.CurrentHealth / health.MaxHealth;
                Debug.Log($"[PartyUIPanel] Set health bar: {health.CurrentHealth}/{health.MaxHealth} = {healthBar.value}");
            }
            else
            {
                Debug.LogWarning($"[PartyUIPanel] Health component not found on {member.playerName}");
            }
        }
        else
        {
            Debug.LogError("[PartyUIPanel] HealthBar component not found!");
        }
        
        // Настраиваем иконку лидера
        if (leaderIcon != null)
        {
            leaderIcon.gameObject.SetActive(member.isPartyLeader);
            Debug.Log($"[PartyUIPanel] Set leader icon: {member.isPartyLeader}");
        }
        else
        {
            Debug.LogError("[PartyUIPanel] LeaderIcon component not found!");
        }
        
        // Настраиваем кликабельность
        if (clickableButton != null)
        {
            clickableButton.onClick.RemoveAllListeners();
            clickableButton.onClick.AddListener(() => OnPartyMemberClicked(member));
            Debug.Log($"[PartyUIPanel] Set up click listener for {member.playerName}");
        }
        else
        {
            Debug.LogError("[PartyUIPanel] Button component not found!");
        }
        
        // Добавляем компонент для обработки кликов
        PartyMemberSlot slotComponent = slot.GetComponent<PartyMemberSlot>();
        if (slotComponent == null)
        {
            slotComponent = slot.AddComponent<PartyMemberSlot>();
        }
        slotComponent.Initialize(member, this);
        
        Debug.Log($"[PartyUIPanel] Setup completed for {member.playerName}");
    }
    
    /// <summary>
    /// Обновляет существующий слот участника группы
    /// </summary>
    private void UpdatePartyMemberSlot(PlayerCore member, GameObject slot)
    {
        Debug.Log($"[PartyUIPanel] UpdatePartyMemberSlot() called for: {member.playerName}");
        
        // Обновляем health bar
        Slider healthBar = slot.transform.Find("ContentContainer/HealthBar")?.GetComponent<Slider>();
        if (healthBar != null)
        {
            Health health = member.GetComponent<Health>();
            if (health != null)
            {
                float healthPercent = (float)health.CurrentHealth / health.MaxHealth;
                healthBar.value = healthPercent;
                Debug.Log($"[PartyUIPanel] Updated health bar for {member.playerName}: {health.CurrentHealth}/{health.MaxHealth} = {healthPercent}");
            }
        }
        else
        {
            Debug.LogWarning($"[PartyUIPanel] HealthBar not found for {member.playerName}");
        }
        
        // Обновляем иконку лидера
        Image leaderIcon = slot.transform.Find("ContentContainer/LeaderIcon")?.GetComponent<Image>();
        if (leaderIcon != null)
        {
            leaderIcon.gameObject.SetActive(member.isPartyLeader);
            Debug.Log($"[PartyUIPanel] Updated leader icon for {member.playerName}: {member.isPartyLeader}");
        }
        else
        {
            Debug.LogWarning($"[PartyUIPanel] LeaderIcon not found for {member.playerName}");
        }
        
        // Обновляем имя и уровень
        TextMeshProUGUI nameText = slot.transform.Find("ContentContainer/TextContainer/NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = member.playerName;
            nameText.color = member.isPartyLeader ? leaderColor : memberColor;
            Debug.Log($"[PartyUIPanel] Updated name for {member.playerName}: {member.playerName}, leader: {member.isPartyLeader}");
        }
        else
        {
            Debug.LogWarning($"[PartyUIPanel] NameText not found for {member.playerName}");
        }
        
        TextMeshProUGUI levelText = slot.transform.Find("ContentContainer/TextContainer/LevelText")?.GetComponent<TextMeshProUGUI>();
        if (levelText != null)
        {
            int level = member.Stats != null ? member.Stats.level : 1;
            levelText.text = $"Lv.{level}";
            Debug.Log($"[PartyUIPanel] Updated level for {member.playerName}: Lv.{level}");
        }
        else
        {
            Debug.LogWarning($"[PartyUIPanel] LevelText not found for {member.playerName}");
        }
    }
    
    /// <summary>
    /// Обрабатывает клик по участнику группы
    /// </summary>
    public void OnPartyMemberClicked(PlayerCore member)
    {
        Debug.Log($"[PartyUIPanel] Party member clicked: {member.playerName}");
        
        // Проверяем, выбран ли скилл
        if (localPlayer != null && localPlayer.Skills != null && localPlayer.Skills.IsSkillSelected)
        {
            var skill = (SkillBase)localPlayer.Skills.ActiveSkill;
            Debug.Log($"[PartyUIPanel] Selected skill: {skill.SkillName}, type: {skill.SkillCastType}");
            
            // Проверяем, что это TargetedAlly скилл
            if (skill.SkillCastType == SkillBase.CastType.TargetedAlly)
            {
                // Применяем скилл на выбранного участника группы
                Debug.Log($"[PartyUIPanel] Attempting to apply {skill.SkillName} to {member.playerName}");
                localPlayer.ActionSystem.TryStartAction(PlayerAction.SkillCast, null, member.gameObject, skill);
                localPlayer.Skills.CancelSkillSelection();
                
                Debug.Log($"[PartyUIPanel] Applied {skill.SkillName} to {member.playerName}");
            }
            else
            {
                Debug.LogWarning($"[PartyUIPanel] Cannot apply {skill.SkillName} to party member - not a TargetedAlly skill (type: {skill.SkillCastType})");
            }
        }
        else
        {
            Debug.Log($"[PartyUIPanel] No skill selected, cannot apply to {member.playerName}");
        }
    }
    
    /// <summary>
    /// Показывает панель группы
    /// </summary>
    private void ShowPartyPanel()
    {
        if (partyPanel != null)
        {
            partyPanel.SetActive(true);
            Debug.Log("[PartyUIPanel] Party panel shown");
        }
        else
        {
            Debug.LogWarning("[PartyUIPanel] Party panel is null, cannot show");
        }
    }
    
    /// <summary>
    /// Скрывает панель группы
    /// </summary>
    private void HidePartyPanel()
    {
        if (partyPanel != null)
        {
            partyPanel.SetActive(false);
            Debug.Log("[PartyUIPanel] Party panel hidden");
        }
        
        // Очищаем все слоты
        Debug.Log($"[PartyUIPanel] Clearing {partyMemberSlots.Count} party member slots");
        foreach (var kvp in partyMemberSlots)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        partyMemberSlots.Clear();
    }
    
    /// <summary>
    /// Принудительно обновляет UI группы
    /// </summary>
    public void ForceUpdatePartyUI()
    {
        UpdatePartyUI();
    }
    
    /// <summary>
    /// Обновляет здоровье конкретного участника группы
    /// </summary>
    public void UpdatePartyMemberHealth(uint memberNetId, int currentHealth, int maxHealth)
    {
        Debug.Log($"[PartyUIPanel] UpdatePartyMemberHealth() called for netId {memberNetId}: {currentHealth}/{maxHealth}");
        
        if (partyMemberSlots.ContainsKey(memberNetId))
        {
            GameObject slot = partyMemberSlots[memberNetId];
            Slider healthBar = slot.transform.Find("ContentContainer/HealthBar")?.GetComponent<Slider>();
            
            if (healthBar != null)
            {
                float healthPercent = (float)currentHealth / maxHealth;
                healthBar.value = healthPercent;
                Debug.Log($"[PartyUIPanel] Updated health bar for netId {memberNetId}: {currentHealth}/{maxHealth} = {healthPercent}");
            }
            else
            {
                Debug.LogWarning($"[PartyUIPanel] HealthBar not found for netId {memberNetId}");
            }
        }
        else
        {
            Debug.LogWarning($"[PartyUIPanel] Party member slot not found for netId {memberNetId}");
        }
    }
    
    /// <summary>
    /// Запрашивает актуальное состояние здоровья участников группы
    /// </summary>
    public void RequestPartyHealthStatus()
    {
        if (localPlayer != null && !string.IsNullOrEmpty(localPlayer.partyId))
        {
            Debug.Log("[PartyUIPanel] Requesting party health status from server");
            localPlayer.CmdRequestPartyHealthStatus();
        }
    }
    
    /// <summary>
    /// Логирует структуру Transform для отладки
    /// </summary>
    private void LogTransformStructure(Transform transform, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}- {transform.name} (active: {transform.gameObject.activeInHierarchy})");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            LogTransformStructure(transform.GetChild(i), depth + 1);
        }
    }
}
