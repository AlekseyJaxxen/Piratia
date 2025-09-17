using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class PlayerEquipmentVisuals : NetworkBehaviour
{
    private Dictionary<EquipmentSlot, GameObject> equippedModels = new Dictionary<EquipmentSlot, GameObject>();
    private Transform characterModel;
    private Animator animator;
    private PlayerAnimationSystem animationSystem;

    public void Init(PlayerCore player)
    {
        animationSystem = player.GetComponent<PlayerAnimationSystem>();
        if (animationSystem == null)
        {
            Debug.LogError($"[PlayerEquipmentVisuals] PlayerAnimationSystem not found on {player.gameObject.name}!");
            return;
        }

        characterModel = GetActiveCharacterModel();
        if (characterModel == null)
        {
            Debug.LogError($"[PlayerEquipmentVisuals] No active model found for {player.gameObject.name}! Waiting for PlayerAnimationSystem to initialize.");
            StartCoroutine(WaitForModelInitialization(player));
            return;
        }

        animator = characterModel.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[PlayerEquipmentVisuals] Animator not found on character model: {characterModel.name}");
            return;
        }
        Debug.Log($"[PlayerEquipmentVisuals] Initialized with character model: {characterModel.name}, scale: {characterModel.localScale}");
    }

    private Transform GetActiveCharacterModel()
    {
        if (animationSystem != null)
        {
            GameObject activeModel = animationSystem.GetActiveModel();
            if (activeModel != null)
            {
                return activeModel.transform;
            }
        }
        return null;
    }

    private IEnumerator WaitForModelInitialization(PlayerCore player)
    {
        yield return new WaitForSeconds(1f);
        characterModel = GetActiveCharacterModel();
        if (characterModel == null)
        {
            Debug.LogError($"[PlayerEquipmentVisuals] Failed to find active model after retry for {player.gameObject.name}!");
            yield break;
        }
        animator = characterModel.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[PlayerEquipmentVisuals] Animator not found on character model: {characterModel.name}");
            yield break;
        }
        Debug.Log($"[PlayerEquipmentVisuals] Successfully initialized with character model: {characterModel.name}, scale: {characterModel.localScale} after retry");
    }

    [Client]
    public void UpdateEquipmentVisual(EquipmentSlot slot, ItemInfo itemInfo)
    {
        if (characterModel == null || animator == null)
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] Cannot update equipment visual for slot {slot}: characterModel or animator is null.");
            return;
        }

        // Удаляем старую модель, если она есть
        if (equippedModels.ContainsKey(slot))
        {
            if (equippedModels[slot] != null)
            {
                NetworkServer.Destroy(equippedModels[slot]);
            }
            equippedModels.Remove(slot);
        }

        // Добавляем новую модель, если предмет экипирован
        if (itemInfo.id > 0)
        {
            Item item = itemInfo.GetItem();
            if (item != null && !string.IsNullOrEmpty(item.boneName))
            {
                GameObject prefab = item.GetEquipModelPrefab();
                if (prefab != null)
                {
                    Transform bone = FindBone(item.boneName);
                    if (bone != null)
                    {
                        GameObject model = Instantiate(prefab, bone);
                        // Корректируем масштаб относительно масштаба characterModel
                        Vector3 modelScale = characterModel.localScale;
                        Vector3 inverseModelScale = new Vector3(1f / modelScale.x, 1f / modelScale.y, 1f / modelScale.z);
                        model.transform.localScale = Vector3.Scale(prefab.transform.localScale, inverseModelScale);
                        model.transform.localPosition = Vector3.zero;
                        model.transform.localRotation = Quaternion.identity;
                        NetworkServer.Spawn(model, connectionToClient);
                        equippedModels[slot] = model;
                        Debug.Log($"[PlayerEquipmentVisuals] Equipped model for {item.itemName} on {item.boneName} for slot {slot}, model scale: {modelScale}, adjusted model scale: {model.transform.localScale}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerEquipmentVisuals] Bone {item.boneName} not found for {item.itemName}");
                    }
                }
            }
        }
    }

    private Transform FindBone(string boneName)
    {
        if (animator == null) return null;
        Transform[] bones = animator.GetComponentsInChildren<Transform>();
        foreach (Transform bone in bones)
        {
            if (bone.name == boneName)
            {
                Debug.Log($"[PlayerEquipmentVisuals] Found bone {boneName}, scale: {bone.localScale}");
                return bone;
            }
        }
        Debug.LogWarning($"[PlayerEquipmentVisuals] Bone {boneName} not found in animator hierarchy.");
        return null;
    }

    [Client]
    public void ClearAllEquipmentVisuals()
    {
        foreach (var model in equippedModels.Values)
        {
            if (model != null)
            {
                NetworkServer.Destroy(model);
            }
        }
        equippedModels.Clear();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        ClearAllEquipmentVisuals();
    }
}