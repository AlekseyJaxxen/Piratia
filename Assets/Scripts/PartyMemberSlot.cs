using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PartyMemberSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.yellow;
    
    private PlayerCore partyMember;
    private PartyUIPanel partyUIPanel;
    private Image backgroundImage;
    private bool isHovered = false;
    
    /// <summary>
    /// Инициализирует слот участника группы
    /// </summary>
    public void Initialize(PlayerCore member, PartyUIPanel panel)
    {
        partyMember = member;
        partyUIPanel = panel;
        
        // Получаем компонент Image для изменения цвета
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = normalColor;
        }
    }
    
    /// <summary>
    /// Обрабатывает клик по слоту
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (partyMember != null && partyUIPanel != null)
        {
            partyUIPanel.OnPartyMemberClicked(partyMember);
        }
    }
    
    /// <summary>
    /// Обрабатывает наведение мыши на слот
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState();
        
        // Показываем tooltip или другую информацию
        ShowTooltip();
    }
    
    /// <summary>
    /// Обрабатывает уход мыши со слота
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState();
        
        // Скрываем tooltip
        HideTooltip();
    }
    
    /// <summary>
    /// Обновляет визуальное состояние слота
    /// </summary>
    private void UpdateVisualState()
    {
        if (backgroundImage == null) return;
        
        // Проверяем, выбран ли скилл
        bool isSkillSelected = false;
        if (PlayerCore.localPlayerCoreInstance != null && 
            PlayerCore.localPlayerCoreInstance.Skills != null)
        {
            isSkillSelected = PlayerCore.localPlayerCoreInstance.Skills.IsSkillSelected;
        }
        
        if (isSkillSelected)
        {
            backgroundImage.color = isHovered ? selectedColor : normalColor;
        }
        else
        {
            backgroundImage.color = isHovered ? hoverColor : normalColor;
        }
    }
    
    /// <summary>
    /// Показывает tooltip с информацией об участнике группы
    /// </summary>
    private void ShowTooltip()
    {
        if (partyMember == null) return;
        
        // Здесь можно показать tooltip с информацией:
        // - Имя игрока
        // - Уровень
        // - Текущее здоровье / Максимальное здоровье
        // - Статус (лидер/участник)
        // - Класс персонажа
        
        Debug.Log($"[PartyMemberSlot] Hovering over: {partyMember.playerName} (Level: {GetPlayerLevel()}, HP: {GetCurrentHealth()}/{GetMaxHealth()})");
    }
    
    /// <summary>
    /// Скрывает tooltip
    /// </summary>
    private void HideTooltip()
    {
        // Здесь можно скрыть tooltip
    }
    
    /// <summary>
    /// Получает уровень игрока
    /// </summary>
    private int GetPlayerLevel()
    {
        if (partyMember != null)
        {
            CharacterStats stats = partyMember.GetComponent<CharacterStats>();
            if (stats != null)
            {
                return stats.level;
            }
        }
        return 1;
    }
    
    /// <summary>
    /// Получает текущее здоровье игрока
    /// </summary>
    private int GetCurrentHealth()
    {
        if (partyMember != null)
        {
            Health health = partyMember.GetComponent<Health>();
            if (health != null)
            {
                return health.CurrentHealth;
            }
        }
        return 100;
    }
    
    /// <summary>
    /// Получает максимальное здоровье игрока
    /// </summary>
    private int GetMaxHealth()
    {
        if (partyMember != null)
        {
            Health health = partyMember.GetComponent<Health>();
            if (health != null)
            {
                return health.MaxHealth;
            }
        }
        return 100;
    }
    
    /// <summary>
    /// Обновляет визуальное состояние при изменении состояния скилла
    /// </summary>
    public void OnSkillSelectionChanged()
    {
        UpdateVisualState();
    }
}
