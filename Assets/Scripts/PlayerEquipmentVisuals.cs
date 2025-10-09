using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections.Generic;

public class PlayerEquipmentVisuals : NetworkBehaviour
{
    private PlayerCore playerCore;
    private GameObject[] instantiatedObjects;

    public void Init(PlayerCore core)
    {
        playerCore = core;
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        instantiatedObjects = new GameObject[allTransforms.Length];
        
        // Получаем активную модель для проверки костей
        GameObject activeModel = GetActiveModel();
        Transform[] modelTransforms = activeModel != null ? activeModel.GetComponentsInChildren<Transform>() : allTransforms;
        
        // Проверяем наличие всех необходимых костей для экипировки в активной модели
        bool hasRightHand = modelTransforms.Any(t => t != null && t.name.ToLower() == "righthandweapon");
        bool hasLeftHand = modelTransforms.Any(t => t != null && t.name.ToLower() == "lefthandweapon");
        bool hasHelmetSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "helmetslot");
        bool hasChestArmorSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "chestarmorslot");
        bool hasLegArmorSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "legarmorslot");
        bool hasBootSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "bootslot");
        bool hasGloveSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "gloveslot");
        bool hasRingSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "ringslot");
        bool hasNecklaceSlot = modelTransforms.Any(t => t != null && t.name.ToLower() == "necklaceslot");
        
        string modelName = activeModel != null ? activeModel.name : "Full Hierarchy";
        Debug.Log($"[PlayerEquipmentVisuals] Equipment bones found in '{modelName}' - RightHand: {hasRightHand}, LeftHand: {hasLeftHand}, HelmetSlot: {hasHelmetSlot}, ChestArmorSlot: {hasChestArmorSlot}, LegArmorSlot: {hasLegArmorSlot}, BootSlot: {hasBootSlot}, GloveSlot: {hasGloveSlot}, RingSlot: {hasRingSlot}, NecklaceSlot: {hasNecklaceSlot}");
        
        // PlayerEquipmentVisuals initialized
    }

    public void UpdateEquipmentVisual(EquipmentSlot slot, ItemInfo itemInfo)
    {
        // Пропускаем только слоты, для которых действительно нет реализации визуалов
        if (slot == EquipmentSlot.Ring || slot == EquipmentSlot.Necklace || 
            slot == EquipmentSlot.Weapon || slot == EquipmentSlot.OffHand)
        {
            return;
        }

        Item item = itemInfo.GetItem();
        if (item == null || itemInfo.id == 0)
        {
            ClearVisualForSlot(slot);
            return;
        }

        // Определяем слот для отображения
        EquipmentSlot displaySlot = slot;
        if (item.isTwoHanded)
        {
            // Двуручное оружие всегда отображается на левой руке
            displaySlot = EquipmentSlot.LeftHand;
        }
        
        string boneName;
        if (item.isTwoHanded)
        {
            // Для двуручного оружия принудительно используем кость левой руки
            boneName = "LeftHandWeapon";
        }
        else
        {
            // Для всех остальных предметов используем метод GetBoneNameForSlot
            boneName = GetBoneNameForSlot(displaySlot);
        }
        Transform bone = FindBone(boneName);

        if (bone == null)
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] No bone ({boneName}) for display slot {displaySlot}, skipping visual for {item.itemName}");
            return;
        }

        // Очищаем визуалы перед экипировкой
        if (item.isTwoHanded)
        {
            // Для двуручного оружия очищаем оба слота
            ClearVisualForSlot(EquipmentSlot.LeftHand);
            ClearVisualForSlot(EquipmentSlot.RightHand);
        }
        else
        {
            // Для одноручного оружия очищаем только текущий слот
            ClearVisualForSlot(slot);
        }

        // Экипируем модель
        if (!string.IsNullOrEmpty(boneName))
        {
            EquipModel(item, bone, slot);
        }

        // Equipped model
    }

    private string GetBoneNameForSlot(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.RightHand:
                return "RightHandWeapon";
            case EquipmentSlot.LeftHand:
                return "LeftHandWeapon";
            case EquipmentSlot.Head:
                return "HelmetSlot"; // специализированная кость для шлемов
            case EquipmentSlot.Body:
                return "ChestArmorSlot"; // специализированная кость для нагрудников
            case EquipmentSlot.Legs:
                return "LegArmorSlot"; // специализированная кость для поножей
            case EquipmentSlot.Boots:
                return "BootSlot"; // специализированная кость для ботинок
            case EquipmentSlot.Gloves:
                return "GloveSlot"; // специализированная кость для перчаток
            case EquipmentSlot.Ring:
                return "RingSlot"; // кость для кольца
            case EquipmentSlot.Necklace:
                return "NecklaceSlot"; // кость для ожерелья
            case EquipmentSlot.Weapon:
                return "RightHandWeapon"; // основное оружие на правой руке
            case EquipmentSlot.OffHand:
                return "LeftHandWeapon"; // щит или второе оружие на левой руке
            default:
                return null;
        }
    }

    private Transform FindBone(string boneName)
    {
        if (string.IsNullOrEmpty(boneName))
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] Bone name is empty");
            return null;
        }

        // Получаем активную модель из PlayerAnimationSystem
        GameObject activeModel = GetActiveModel();
        if (activeModel == null)
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] Active model is null, falling back to full hierarchy search");
            // Fallback к старому методу если активная модель не найдена
            return FindBoneInFullHierarchy(boneName);
        }

        // Ищем кость только в активной модели
        Transform[] modelTransforms = activeModel.GetComponentsInChildren<Transform>();
        
        foreach (Transform transform in modelTransforms)
        {
            if (transform != null && transform.name.ToLower() == boneName.ToLower())
            {
                return transform;
            }
        }
        
        // Выводим все доступные кости для отладки только при ошибке
        Debug.LogWarning($"[PlayerEquipmentVisuals] Bone '{boneName}' not found in active model '{activeModel.name}'. Available bones:");
        foreach (Transform transform in modelTransforms)
        {
            if (transform != null)
            {
                Debug.LogWarning($"[PlayerEquipmentVisuals] - {transform.name}");
            }
        }
        return null;
    }

    private Transform FindBoneInFullHierarchy(string boneName)
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        
        foreach (Transform transform in allTransforms)
        {
            if (transform != null && transform.name.ToLower() == boneName.ToLower())
            {
                return transform;
            }
        }
        
        // Выводим все доступные кости для отладки только при ошибке
        Debug.LogWarning($"[PlayerEquipmentVisuals] Bone '{boneName}' not found in full hierarchy. Available bones:");
        foreach (Transform transform in allTransforms)
        {
            if (transform != null)
            {
                Debug.LogWarning($"[PlayerEquipmentVisuals] - {transform.name}");
            }
        }
        return null;
    }

    private GameObject GetActiveModel()
    {
        if (playerCore == null) return null;
        
        PlayerAnimationSystem animationSystem = playerCore.GetComponent<PlayerAnimationSystem>();
        if (animationSystem == null) return null;
        
        return animationSystem.GetActiveModel();
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
        model.transform.localRotation = item.modelRotation;
        model.transform.localScale = item.modelScale;

        int transformIndex = System.Array.IndexOf(GetComponentsInChildren<Transform>(), bone);
        if (transformIndex >= 0 && transformIndex < instantiatedObjects.Length)
        {
            if (instantiatedObjects[transformIndex] != null)
            {
                Destroy(instantiatedObjects[transformIndex]);
            }
            instantiatedObjects[transformIndex] = model;
        }
        else
        {
            Debug.LogWarning($"[PlayerEquipmentVisuals] Invalid transform index {transformIndex} for bone {bone.name}");
        }
    }

    private void ClearVisualForSlot(EquipmentSlot slot)
    {
        // Пропускаем только слоты, для которых действительно нет реализации визуалов
        if (slot == EquipmentSlot.Ring || slot == EquipmentSlot.Necklace || 
            slot == EquipmentSlot.Weapon || slot == EquipmentSlot.OffHand)
        {
            return;
        }

        // Определяем правильную кость, соответствующую данному слоту
        string boneName = GetBoneNameForSlot(slot);
        if (string.IsNullOrEmpty(boneName))
        {
            return;
        }
        Transform bone = FindBone(boneName);
        Transform[] allTransforms = GetComponentsInChildren<Transform>();

        if (bone != null)
        {
            // ����������� ���� �������� �������� �� �����
            // Удаляем все дочерние объекты с кости (создаем копию списка для безопасного удаления)
            List<Transform> childrenToDestroy = new List<Transform>();
            foreach (Transform child in bone)
            {
                childrenToDestroy.Add(child);
            }
            
            foreach (Transform child in childrenToDestroy)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            // ������� � instantiatedObjects
            int transformIndex = System.Array.IndexOf(allTransforms, bone);
            if (transformIndex >= 0 && transformIndex < instantiatedObjects.Length && instantiatedObjects[transformIndex] != null)
            {
                Destroy(instantiatedObjects[transformIndex]);
                instantiatedObjects[transformIndex] = null;
            }
        }
        
        // Для двуручного оружия также очищаем правую руку, если оно было экипировано в левую
        if (slot == EquipmentSlot.LeftHand)
        {
            ItemInfo leftHandItem = playerCore.Inventory.GetEquipped(EquipmentSlot.LeftHand);
            if (leftHandItem.id > 0)
            {
                Item leftItem = leftHandItem.GetItem();
                if (leftItem != null && leftItem.isTwoHanded)
                {
                    // Очищаем правую руку для двуручного оружия
                    string rightBoneName = "RightHandWeapon";
                    Transform rightBone = FindBone(rightBoneName);
                    if (rightBone != null)
                    {
                        List<Transform> rightChildrenToDestroy = new List<Transform>();
                        foreach (Transform child in rightBone)
                        {
                            rightChildrenToDestroy.Add(child);
                        }
                        
                        foreach (Transform child in rightChildrenToDestroy)
                        {
                            Debug.Log($"[PlayerEquipmentVisuals] Destroying child object {child.name} on bone {rightBoneName} for two-handed weapon");
                            if (child != null)
                            {
                                Destroy(child.gameObject);
                            }
                        }

                        int rightTransformIndex = System.Array.IndexOf(allTransforms, rightBone);
                        if (rightTransformIndex >= 0 && rightTransformIndex < instantiatedObjects.Length && instantiatedObjects[rightTransformIndex] != null)
                        {
                            Debug.Log($"[PlayerEquipmentVisuals] Destroying model for right hand on bone {rightBoneName} at index {rightTransformIndex}");
                            Destroy(instantiatedObjects[rightTransformIndex]);
                            instantiatedObjects[rightTransformIndex] = null;
                        }
                    }
                }
            }
        }

        // ������� ������� �����, ���� ������� ���������
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