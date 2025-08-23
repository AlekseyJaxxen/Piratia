using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ����� ��� ���������� ����� ������ �� ������ ������
[System.Serializable]
public class SkillSlotUI
{
    public Image skillIcon;
    public Image cooldownImage;
    public TextMeshProUGUI cooldownText;
    [HideInInspector]
    public float cooldownDuration;
    [HideInInspector]
    public float currentCooldown;
}

public class SkillUIController : MonoBehaviour
{
    public List<SkillSlotUI> skillSlots;
    private PlayerSkills _playerSkills;

    // ���������� ��� ������������� ������
    public void Init(PlayerSkills playerSkills)
    {
        _playerSkills = playerSkills;

        // ���������, ��������� �� ���������� ������ UI � ����������� ������
        if (skillSlots.Count != _playerSkills.skills.Count)
        {
            Debug.LogError("Mismatch between UI slots and actual skills!");
            return;
        }

        // ������������� ������ � �������� �������� �����������
        for (int i = 0; i < skillSlots.Count; i++)
        {
            // ��������������, ��� � ��� ���� ������ ��� ������ � SkillBase
            // skillSlots[i].skillIcon.sprite = _playerSkills.skills[i].SkillIcon;
            skillSlots[i].cooldownImage.fillAmount = 0;
            skillSlots[i].cooldownText.gameObject.SetActive(false);
        }
    }

    // ���� ����� ����� ���������� �� PlayerSkills, ����� ������ ������������
    public void StartCooldown(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillSlots.Count) return;

        skillSlots[skillIndex].cooldownDuration = _playerSkills.skills[skillIndex].Cooldown;
        skillSlots[skillIndex].currentCooldown = skillSlots[skillIndex].cooldownDuration;
        skillSlots[skillIndex].cooldownImage.fillAmount = 1;
        skillSlots[skillIndex].cooldownText.gameObject.SetActive(true);
    }

    private void Update()
    {
        // ��������� ��������� ������� ����� � ������ �����
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].currentCooldown > 0)
            {
                skillSlots[i].currentCooldown -= Time.deltaTime;
                float fill = skillSlots[i].currentCooldown / skillSlots[i].cooldownDuration;
                skillSlots[i].cooldownImage.fillAmount = fill;
                skillSlots[i].cooldownText.text = Mathf.Ceil(skillSlots[i].currentCooldown).ToString();

                if (skillSlots[i].currentCooldown <= 0)
                {
                    skillSlots[i].currentCooldown = 0;
                    skillSlots[i].cooldownImage.fillAmount = 0;
                    skillSlots[i].cooldownText.gameObject.SetActive(false);
                }
            }
        }
    }
}