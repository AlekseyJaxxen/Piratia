using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;

public class HotbarUI : NetworkBehaviour
{
    [System.Serializable]
    public class SkillSlotUI
    {
        public Image skillIcon;
        public Image cooldownOverlay;
        public TMP_Text cooldownText;
    }

    // Ссылка на корневой UI-объект, например, Canvas или Panel
    [SerializeField]
    private GameObject hotbarRoot;

    private PlayerSkills playerSkills;
    public SkillSlotUI[] skillSlots;

    private void Awake()
    {
        playerSkills = GetComponent<PlayerSkills>();
        if (playerSkills == null)
        {
            Debug.LogError("HotbarUI requires a PlayerSkills component on the same GameObject.");
        }

        if (hotbarRoot == null)
        {
            Debug.LogError("HotbarUI requires a reference to the root UI GameObject.");
        }
    }

    public override void OnStartLocalPlayer()
    {
        // Этот метод вызывается только для локального игрока.
        // Здесь мы включаем UI.
        if (hotbarRoot != null)
        {
            hotbarRoot.SetActive(true);
        }
    }

    private void Update()
    {
        // Проверяем, являемся ли мы локальным игроком, прежде чем обновлять UI.
        // Это необходимо, так как OnStartLocalPlayer не отключает скрипт для других игроков.
        if (isLocalPlayer)
        {
            UpdateCooldowns();
        }
    }

    private void UpdateCooldowns()
    {
        if (playerSkills == null || playerSkills.skills.Count == 0 || skillSlots.Length == 0) return;

        for (int i = 0; i < playerSkills.skills.Count && i < skillSlots.Length; i++)
        {
            ISkill skill = playerSkills.skills[i];
            SkillSlotUI slot = skillSlots[i];

            if (skill == null) continue;

            if (skill.IsOnCooldown())
            {
                slot.cooldownOverlay.fillAmount = skill.CooldownProgressNormalized;
                slot.cooldownText.text = Mathf.Ceil(skill.RemainingCooldown).ToString();
            }
            else
            {
                slot.cooldownOverlay.fillAmount = 0;
                slot.cooldownText.text = "";
            }
        }
    }
}