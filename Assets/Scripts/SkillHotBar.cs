using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class SkillHotBar : MonoBehaviour
{
//   [Header("Skill Slots")]
//   public List<GameObject> skillCooldownObjects;
//   public List<Text> skillKeyTexts;
//   public List<Image> skillIcons;
//
//   private List<Image> skillCooldownImages = new List<Image>();
//   private PlayerSkills playerSkills;
//   private bool isInitialized = false;
//
//   private void Awake()
//   {
//       InitializeComponents();
//   }
//
//   private void Start()
//   {
//       StartCoroutine(WaitForLocalPlayer());
//   }
//
//   private void InitializeComponents()
//   {
//       foreach (var obj in skillCooldownObjects)
//       {
//           if (obj != null)
//           {
//               skillCooldownImages.Add(obj.GetComponent<Image>());
//           }
//           else
//           {
//               skillCooldownImages.Add(null);
//               Debug.LogError("An element in skillCooldownObjects list is not assigned!");
//           }
//       }
//   }
//
//   private IEnumerator WaitForLocalPlayer()
//   {
//       while (playerSkills == null)
//       {
//           foreach (var player in FindObjectsOfType<NetworkBehaviour>())
//           {
//               if (player.isLocalPlayer)
//               {
//                   playerSkills = player.GetComponent<PlayerSkills>();
//                   if (playerSkills != null)
//                   {
//                       InitializeUI();
//                       yield break;
//                   }
//               }
//           }
//           yield return new WaitForSeconds(0.5f);
//       }
//   }
//
//   private void InitializeUI()
//   {
//       if (playerSkills == null) return;
//
//       UpdateKeyTexts();
//       isInitialized = true;
//   }
//
//   private void UpdateKeyTexts()
//   {
//       for (int i = 0; i < playerSkills.skills.Count && i < skillKeyTexts.Count; i++)
//       {
//           if (skillKeyTexts[i] != null)
//           {
//               skillKeyTexts[i].text = playerSkills.skills[i].hotkey.ToString().Replace("Alpha", "");
//           }
//       }
//   }
//
//   private void Update()
//   {
//       if (!isInitialized || playerSkills == null || !playerSkills.isLocalPlayer)
//       {
//           return;
//       }
//
//       // Iterate through all skills and update UI
//       for (int i = 0; i < playerSkills.skills.Count; i++)
//       {
//           if (i < skillCooldownImages.Count && skillCooldownImages[i] != null)
//           {
//               UpdateCooldownUI(i);
//           }
//       }
//   }
//
//   private void UpdateCooldownUI(int skillIndex)
//   {
//       float cooldownProgress = playerSkills.GetCooldownProgress(skillIndex);
//       Image cooldownImage = skillCooldownImages[skillIndex];
//
//       cooldownImage.fillAmount = 1 - cooldownProgress;
//       cooldownImage.color = cooldownProgress < 1f ?
//           new Color(0.5f, 0.5f, 0.5f, 0.7f) :
//           Color.white;
//   }
//
//   private void OnDestroy()
//   {
//       StopAllCoroutines();
//   }
}