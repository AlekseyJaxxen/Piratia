using UnityEngine;
using Mirror;
using System.Linq;

public class PlayerEquipmentVisuals : NetworkBehaviour
{
    private PlayerCore playerCore;
    private GameObject[] instantiatedObjects;

    public void Init(PlayerCore core)
    {
        playerCore = core;
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        instantiatedObjects = new GameObject[allTransforms.Length];
        Debug.Log($"[PlayerEquipmentVisuals] Initialized with {allTransforms.Length} transforms: {string.Join(", ", allTransforms.Where(t => t != null).Select(t => t.name))}");
        bool hasRightHand = allTransforms.Any(t => t != null && t.name.ToLower() == "righthandweapon");
        bool hasLeftHand = allTransforms.Any(t => t != null && t.name.ToLower() == "lefthandweapon");
        Debug.Log($"[PlayerEquipmentVisuals] RightHandWeapon found: {hasRightHand}, LeftHandWeapon found: {hasLeftHand}");
    }

    public void UpdateEquipmentVisual(EquipmentSlot slot, ItemInfo itemInfo)
    {
        if (slot == EquipmentSlot.Head)
        {
            Debug.Log($"[PlayerEquipmentVisuals] Skipping visual update for Head slot (not implemented)");
            return;
        }

        // Очистка визуала для текущего слота
        ClearVisualForSlot(slot);

        Item item = itemInfo.GetItem();
        if (item == null || itemInfo.id == 0)
        {
            Debug.Log($"[PlayerEquipmentVisuals] No item or invalid item ID for slot {slot}, visual cleared");
            return;
        }

        // Для двуручного оружия используем primaryDisplaySlot
        string boneName;
        if (item.isTwoHanded)
        {
            boneName = item.GetBoneNameForSlot(item.primaryDisplaySlot);
        }
        else
        {
            boneName = item.GetBoneNameForSlot(slot);
        }

        Transform bone = FindBone(boneName);
        if (bone == null)
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] No bone ({boneName}) for slot {slot}, skipping visual for {item.itemName}");
            return;
        }

        // Экипировка модели
        if (item.isTwoHanded)
        {
            if (!string.IsNullOrEmpty(boneName))
            {
                EquipModel(item, bone, slot);
            }
        }
        else
        {
            EquipModel(item, bone, slot);
        }

        Debug.Log($"[PlayerEquipmentVisuals] Equipped model for {item.itemName} on {boneName} for slot {slot}");
    }

    private Transform FindBone(string boneName)
    {
        if (string.IsNullOrEmpty(boneName))
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] Bone name is empty or null");
            return null;
        }

        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform transform in allTransforms)
        {
            if (transform != null && transform.name.ToLower() == boneName.ToLower())
            {
                Debug.Log($"[PlayerEquipmentVisuals] Found bone {boneName}, scale: {transform.localScale}, position: {transform.position}");
                return transform;
            }
        }
        Debug.LogWarning($"[PlayerEquipmentVisuals] Bone {boneName} not found in {allTransforms.Length} transforms");
        return null;
    }

    private void EquipModel(Item item, Transform bone, EquipmentSlot slot)
    {
        GameObject modelPrefab = item.GetEquipModelPrefab();
        if (modelPrefab == null)
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] No equip model prefab for item {item.itemName} at path {item.model1}");
            return;
        }

        GameObject model = Instantiate(modelPrefab, bone);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = item.modelRotation; // Без корректировки поворота
        model.transform.localScale = item.modelScale;

        int transformIndex = System.Array.IndexOf(GetComponentsInChildren<Transform>(), bone);
        if (transformIndex >= 0 && transformIndex < instantiatedObjects.Length)
        {
            if (instantiatedObjects[transformIndex] != null)
            {
                Debug.LogWarning($"[PlayerEquipmentVisuals] Overwriting existing model at index {transformIndex} for bone {bone.name}");
                Destroy(instantiatedObjects[transformIndex]);
            }
            instantiatedObjects[transformIndex] = model;
        }
        else
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] Invalid transform index {transformIndex} for bone {bone.name}");
        }

        Debug.Log($"[PlayerEquipmentVisuals] Equipped model for {item.itemName} on {bone.name} for slot {slot}, model scale: {model.transform.localScale}, rotation: {model.transform.localRotation}");
    }

    private void ClearVisualForSlot(EquipmentSlot slot)
    {
        if (slot == EquipmentSlot.Head)
        {
            Debug.Log($"[PlayerEquipmentVisuals] Skipping clear visual for Head slot (not implemented)");
            return;
        }

        // Очистка только для кости, соответствующей текущему слоту
        string boneName = slot == EquipmentSlot.RightHand ? "RightHandWeapon" : "LeftHandWeapon";
        Transform bone = FindBone(boneName);
        Transform[] allTransforms = GetComponentsInChildren<Transform>();

        if (bone != null)
        {
            // Уничтожение всех дочерних объектов на кости
            foreach (Transform child in bone)
            {
                Debug.Log($"[PlayerEquipmentVisuals] Destroying child object {child.name} on bone {boneName} for slot {slot}");
                Destroy(child.gameObject);
            }

            // Очистка в instantiatedObjects
            int transformIndex = System.Array.IndexOf(allTransforms, bone);
            if (transformIndex >= 0 && transformIndex < instantiatedObjects.Length && instantiatedObjects[transformIndex] != null)
            {
                Debug.Log($"[PlayerEquipmentVisuals] Destroying model for slot {slot} on bone {boneName} at index {transformIndex}");
                Destroy(instantiatedObjects[transformIndex]);
                instantiatedObjects[transformIndex] = null;
            }
        }

        // Очистка другого слота, если предмет двуручный
        EquipmentSlot otherSlot = slot == EquipmentSlot.RightHand ? EquipmentSlot.LeftHand : EquipmentSlot.RightHand;
        ItemInfo otherItem = playerCore.Inventory.GetEquipped(otherSlot);
        if (otherItem.id > 0)
        {
            Item item = otherItem.GetItem();
            if (item != null && item.isTwoHanded)
            {
                string otherBoneName = item.GetBoneNameForSlot(item.primaryDisplaySlot);
                if (!string.IsNullOrEmpty(otherBoneName))
                {
                    Transform otherBone = FindBone(otherBoneName);
                    if (otherBone != null)
                    {
                        foreach (Transform child in otherBone)
                        {
                            Debug.Log($"[PlayerEquipmentVisuals] Destroying child object {child.name} on bone {otherBoneName} for other slot {otherSlot}");
                            Destroy(child.gameObject);
                        }

                        int transformIndex = System.Array.IndexOf(allTransforms, otherBone);
                        if (transformIndex >= 0 && transformIndex < instantiatedObjects.Length && instantiatedObjects[transformIndex] != null)
                        {
                            Debug.Log($"[PlayerEquipmentVisuals] Destroying model for other slot {otherSlot} on bone {otherBoneName} at index {transformIndex}");
                            Destroy(instantiatedObjects[transformIndex]);
                            instantiatedObjects[transformIndex] = null;
                        }
                    }
                }
            }
        }
    }
}