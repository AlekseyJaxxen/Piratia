// DroppedItem.cs - ������, collider ������� + UI ����
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using DG.Tweening;
using UnityEngine.EventSystems;
public class DroppedItem : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // + IPointerClickHandler
{
    [Header("Settings")]
    [SyncVar] public int itemID;
    [SyncVar] public int quantity;
    [SyncVar] public uint ownerNetId = 0;
    [SyncVar] public float dropTime = 0f;
    
    [Header("Dynamic Stats")]
    [SyncVar] public int minAttackConstantBonus = 0;
    [SyncVar] public int maxAttackConstantBonus = 0;
    [SyncVar] public int strengthBonus = 0;
    [SyncVar] public int agilityBonus = 0;
    [SyncVar] public int spiritBonus = 0;
    [SyncVar] public int constitutionBonus = 0;
    [SyncVar] public int accuracyBonus = 0;
    [SyncVar] public int maxHpConstantBonus = 0;
    [SyncVar] public int maxSpConstantBonus = 0;
    [SyncVar] public int physicalResist = 0; // УСТАРЕЛО
    [SyncVar] public int armorBonus = 0; // Плоская броня
    [SyncVar] public int physicalResistBonus = 0; // Процентное сопротивление
    [SyncVar] public int crtConstantBonus = 0;
    [SyncVar] public float mspdConstantBonus = 0.0f;
    [SyncVar] public string dynamicItemName = "";
    [SyncVar] public Rarity dynamicRarity = Rarity.Common;
    [SerializeField] private Transform modelParent;
    [SerializeField] private GameObject defaultModelPrefab;
    [SerializeField] private Canvas nameCanvas;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] public float pickupDistance = 2f;
    [SerializeField] private float despawnTime = 300f;
    [SerializeField] private Texture2D pickupCursor;
    private Item item;
    private GameObject modelInstance;
    private Sequence tweenSequence;
    
    /// <summary>
    /// Инициализирует дропнутый предмет с динамическими статами
    /// </summary>
    public void InitializeWithDynamicStats(Item baseItem, Item generatedItem)
    {
        Debug.Log($"[DroppedItem] Initializing with dynamic stats - Base: {baseItem.itemName} (ID: {baseItem.id}), Generated: {generatedItem.itemName} (ID: {generatedItem.id})");
        
        itemID = baseItem.id;
        minAttackConstantBonus = generatedItem.minAttackConstantBonus;
        maxAttackConstantBonus = generatedItem.maxAttackConstantBonus;
        strengthBonus = generatedItem.strengthBonus;
        agilityBonus = generatedItem.agilityBonus;
        spiritBonus = generatedItem.spiritBonus;
        constitutionBonus = generatedItem.constitutionBonus;
        accuracyBonus = generatedItem.accuracyBonus;
        maxHpConstantBonus = generatedItem.maxHpConstantBonus;
        maxSpConstantBonus = generatedItem.maxSpConstantBonus;
        physicalResist = generatedItem.physicalResist;
        armorBonus = generatedItem.armorBonus;
        physicalResistBonus = generatedItem.physicalResistBonus;
        crtConstantBonus = generatedItem.crtConstantBonus;
        mspdConstantBonus = generatedItem.mspdConstantBonus;
        dynamicItemName = generatedItem.itemName;
        
        Debug.Log($"[DroppedItem] Dynamic stats applied - Name: {dynamicItemName}, Damage: {minAttackConstantBonus}-{maxAttackConstantBonus}, Stats: STR+{strengthBonus}, AGI+{agilityBonus}, CON+{constitutionBonus}");
    }
    
    /// <summary>
    /// Инициализирует дропнутый предмет с ItemInfo
    /// </summary>
    public void InitializeWithDynamicItemInfo(ItemInfo itemInfo)
    {
        Debug.Log($"[DroppedItem] Initializing with ItemInfo - ID: {itemInfo.id}, Name: {itemInfo.dynamicItemName}, Quantity: {itemInfo.quantity}");
        
        itemID = itemInfo.id;
        quantity = itemInfo.quantity; // ИСПРАВЛЕНИЕ: устанавливаем quantity
        minAttackConstantBonus = itemInfo.minAttackConstantBonus;
        maxAttackConstantBonus = itemInfo.maxAttackConstantBonus;
        strengthBonus = itemInfo.strengthBonus;
        agilityBonus = itemInfo.agilityBonus;
        spiritBonus = itemInfo.spiritBonus;
        constitutionBonus = itemInfo.constitutionBonus;
        accuracyBonus = itemInfo.accuracyBonus;
        maxHpConstantBonus = itemInfo.maxHpConstantBonus;
        maxSpConstantBonus = itemInfo.maxSpConstantBonus;
        physicalResist = itemInfo.physicalResist;
        armorBonus = itemInfo.armorBonus;
        physicalResistBonus = itemInfo.physicalResistBonus;
        crtConstantBonus = itemInfo.crtConstantBonus;
        mspdConstantBonus = itemInfo.mspdConstantBonus;
        dynamicItemName = itemInfo.dynamicItemName;
        dynamicRarity = itemInfo.dynamicRarity;
        
        Debug.Log($"[DroppedItem] ItemInfo stats applied - Name: {dynamicItemName}, Rarity: {dynamicRarity}, Damage: {minAttackConstantBonus}-{maxAttackConstantBonus}, Stats: STR+{strengthBonus}, AGI+{agilityBonus}, CON+{constitutionBonus}");
    }
    private void Awake()
    {
        if (modelParent == null)
        {
            modelParent = transform.Find("Empty");
            if (modelParent == null)
            {
                modelParent = transform;
                Debug.LogWarning($"[DroppedItem] modelParent not assigned, using transform of {gameObject.name}");
            }
        }
        if (nameCanvas != null)
        {
            // Add a Horizontal Layout Group to the nameCanvas
            HorizontalLayoutGroup layoutGroup = nameCanvas.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 5, 5); // Add some padding
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            // Add a Content Size Fitter to the nameCanvas
            ContentSizeFitter contentFitter = nameCanvas.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Add the Image component for the background
            Image bg = nameCanvas.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);
            bg.raycastTarget = true; // ����: Raycast Target ��� UI �����
            // Ensure the text is a child of the canvas and on top of the background
            nameText.transform.SetParent(nameCanvas.transform);
            nameText.raycastTarget = true; // ����: Raycast Target ��� ������
        }
    }
    private void Start()
    {
        if (isServer)
        {
            Invoke(nameof(DestroySelf), despawnTime);
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[DroppedItem] OnStartClient - ItemID: {itemID}, Quantity: {quantity}, DynamicName: {dynamicItemName}");
        
        ItemDatabase db = Resources.Load<ItemDatabase>("ItemDatabase");
        if (db == null)
        {
            Debug.LogError($"[DroppedItem] ItemDatabase not found at Resources/ItemDatabase");
            return;
        }
        item = db.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[DroppedItem] Item with ID {itemID} not found");
            return;
        }
        
        Debug.Log($"[DroppedItem] Found base item: {item.itemName} (ID: {item.id})");
        
        // КРИТИЧЕСКАЯ ЗАЩИТА: Создаем копию предмета с сохранением типа (Item, SwordItem, etc.)
        Item originalItem = item;
        item = ScriptableObject.CreateInstance(originalItem.GetType()) as Item;
        CopyItemProperties(originalItem, item);
        Debug.Log($"[DroppedItem] Created copy of item to avoid modifying original SO");
        
        // Применяем динамические статы если они есть
        if (!string.IsNullOrEmpty(dynamicItemName))
        {
            string originalName = item.itemName;
            item.itemName = dynamicItemName;
            Debug.Log($"[DroppedItem] Applied dynamic name: '{originalName}' -> '{dynamicItemName}'");
        }
        int appliedStats = 0;
        if (minAttackConstantBonus > 0) { item.minAttackConstantBonus = minAttackConstantBonus; appliedStats++; }
        if (maxAttackConstantBonus > 0) { item.maxAttackConstantBonus = maxAttackConstantBonus; appliedStats++; }
        if (strengthBonus > 0) { item.strengthBonus = strengthBonus; appliedStats++; }
        if (agilityBonus > 0) { item.agilityBonus = agilityBonus; appliedStats++; }
        if (spiritBonus > 0) { item.spiritBonus = spiritBonus; appliedStats++; }
        if (constitutionBonus > 0) { item.constitutionBonus = constitutionBonus; appliedStats++; }
        if (accuracyBonus > 0) { item.accuracyBonus = accuracyBonus; appliedStats++; }
        if (maxHpConstantBonus > 0) { item.maxHpConstantBonus = maxHpConstantBonus; appliedStats++; }
        if (maxSpConstantBonus > 0) { item.maxSpConstantBonus = maxSpConstantBonus; appliedStats++; }
        if (physicalResist > 0) { item.physicalResist = physicalResist; appliedStats++; }
        if (armorBonus > 0) { item.armorBonus = armorBonus; appliedStats++; }
        if (physicalResistBonus > 0) { item.physicalResistBonus = physicalResistBonus; appliedStats++; }
        if (crtConstantBonus > 0) { item.crtConstantBonus = crtConstantBonus; appliedStats++; }
        if (mspdConstantBonus > 0) { item.mspdConstantBonus = mspdConstantBonus; appliedStats++; }
        
        Debug.Log($"[DroppedItem] Applied {appliedStats} dynamic stats to item");
        
        // Устанавливаем редкость предмета если она была передана
        if (dynamicRarity != Rarity.Common || !string.IsNullOrEmpty(dynamicItemName))
        {
            item.rarity = dynamicRarity;
            Debug.Log($"[DroppedItem] Set item rarity to: {dynamicRarity}");
        }
        
        quantity = Mathf.Max(1, quantity); // ����: min 1
        SpawnModel();
        UpdateNameText();
        AnimateDrop();
        
        Debug.Log($"[DroppedItem] Client initialization complete for item: {item.itemName} x{quantity}");
    }
    
    /// <summary>
    /// Копирует все свойства из исходного предмета в целевой
    /// </summary>
    private void CopyItemProperties(Item source, Item target)
    {
        // Копируем все основные свойства
        target.itemName = source.itemName;
        target.originalName = !string.IsNullOrEmpty(source.originalName) ? source.originalName : source.itemName;
        target.id = source.id;
        target.icon = source.icon;
        target.itemType = source.itemType;
        target.equipmentSlot = source.equipmentSlot;
        target.alternativeSlot = source.alternativeSlot;
        target.primaryDisplaySlot = source.primaryDisplaySlot;
        target.maxStack = source.maxStack;
        target.canDrop = source.canDrop;
        target.canSell = source.canSell;
        target.canUse = source.canUse;
        target.canHotbar = source.canHotbar;
        target.isTwoHanded = source.isTwoHanded;
        target.preferRightHand = source.preferRightHand;
        target.rarity = source.rarity;
        target.requiredLevel = source.requiredLevel;
        target.characterClass = source.characterClass;
        target.skillEffect = source.skillEffect;
        target.castRange = source.castRange;
        target.model1 = source.model1;
        target.boneName = source.boneName;
        target.alternativeBoneName = source.alternativeBoneName;
        target.modelRotation = source.modelRotation;
        target.modelScale = source.modelScale;
        target.price = source.price;
        target.durability = source.durability;
        target.description = source.description;
        
        // Копируем dropModelPrefab для правильного отображения модели
        target.DropModelPrefab = source.DropModelPrefab;
        
        // Копируем базовые статы (они будут перезаписаны динамическими если есть)
        target.minAttackConstantBonus = source.minAttackConstantBonus;
        target.maxAttackConstantBonus = source.maxAttackConstantBonus;
        target.maxHpConstantBonus = source.maxHpConstantBonus;
        target.maxSpConstantBonus = source.maxSpConstantBonus;
        target.crtConstantBonus = source.crtConstantBonus;
        target.mspdConstantBonus = source.mspdConstantBonus;
        target.physicalResist = source.physicalResist;
        target.armorBonus = source.armorBonus;
        target.physicalResistBonus = source.physicalResistBonus;
        target.strengthBonus = source.strengthBonus;
        target.agilityBonus = source.agilityBonus;
        target.spiritBonus = source.spiritBonus;
        target.constitutionBonus = source.constitutionBonus;
        target.accuracyBonus = source.accuracyBonus;
        target.hpRecoveryBonus = source.hpRecoveryBonus;
        target.spRecoveryBonus = source.spRecoveryBonus;
        target.dodgeBonus = source.dodgeBonus;
        
        // Копируем настройки динамических статов
        target.useDynamicStats = source.useDynamicStats;
        if (source.minDamageRange != null)
            target.minDamageRange = new Item.StatRange { minValue = source.minDamageRange.minValue, maxValue = source.minDamageRange.maxValue, chance = source.minDamageRange.chance };
        if (source.maxDamageRange != null)
            target.maxDamageRange = new Item.StatRange { minValue = source.maxDamageRange.minValue, maxValue = source.maxDamageRange.maxValue, chance = source.maxDamageRange.chance };
        if (source.strengthRange != null)
            target.strengthRange = new Item.StatRange { minValue = source.strengthRange.minValue, maxValue = source.strengthRange.maxValue, chance = source.strengthRange.chance };
        if (source.agilityRange != null)
            target.agilityRange = new Item.StatRange { minValue = source.agilityRange.minValue, maxValue = source.agilityRange.maxValue, chance = source.agilityRange.chance };
        if (source.spiritRange != null)
            target.spiritRange = new Item.StatRange { minValue = source.spiritRange.minValue, maxValue = source.spiritRange.maxValue, chance = source.spiritRange.chance };
        if (source.constitutionRange != null)
            target.constitutionRange = new Item.StatRange { minValue = source.constitutionRange.minValue, maxValue = source.constitutionRange.maxValue, chance = source.constitutionRange.chance };
        if (source.accuracyRange != null)
            target.accuracyRange = new Item.StatRange { minValue = source.accuracyRange.minValue, maxValue = source.accuracyRange.maxValue, chance = source.accuracyRange.chance };
        if (source.healthRange != null)
            target.healthRange = new Item.StatRange { minValue = source.healthRange.minValue, maxValue = source.healthRange.maxValue, chance = source.healthRange.chance };
        if (source.manaRange != null)
            target.manaRange = new Item.StatRange { minValue = source.manaRange.minValue, maxValue = source.manaRange.maxValue, chance = source.manaRange.chance };
        if (source.defenseRange != null)
            target.defenseRange = new Item.StatRange { minValue = source.defenseRange.minValue, maxValue = source.defenseRange.maxValue, chance = source.defenseRange.chance };
        if (source.armorRange != null)
            target.armorRange = new Item.StatRange { minValue = source.armorRange.minValue, maxValue = source.armorRange.maxValue, chance = source.armorRange.chance };
        if (source.physicalResistRange != null)
            target.physicalResistRange = new Item.StatRange { minValue = source.physicalResistRange.minValue, maxValue = source.physicalResistRange.maxValue, chance = source.physicalResistRange.chance };
        if (source.criticalRange != null)
            target.criticalRange = new Item.StatRange { minValue = source.criticalRange.minValue, maxValue = source.criticalRange.maxValue, chance = source.criticalRange.chance };
        if (source.movementSpeedRange != null)
            target.movementSpeedRange = new Item.FloatStatRange { minValue = source.movementSpeedRange.minValue, maxValue = source.movementSpeedRange.maxValue, chance = source.movementSpeedRange.chance };
        if (source.hpRecoveryRange != null)
            target.hpRecoveryRange = new Item.StatRange { minValue = source.hpRecoveryRange.minValue, maxValue = source.hpRecoveryRange.maxValue, chance = source.hpRecoveryRange.chance };
        if (source.spRecoveryRange != null)
            target.spRecoveryRange = new Item.StatRange { minValue = source.spRecoveryRange.minValue, maxValue = source.spRecoveryRange.maxValue, chance = source.spRecoveryRange.chance };
        if (source.dodgeRange != null)
            target.dodgeRange = new Item.StatRange { minValue = source.dodgeRange.minValue, maxValue = source.dodgeRange.maxValue, chance = source.dodgeRange.chance };
            
        Debug.Log($"[DroppedItem] Copied all properties from {source.itemName} to new instance");
    }
    
    [Client]
    private void UpdateNameText()
    {
        if (item != null && nameText != null)
        {
            nameText.text = $"{item.itemName} x{Mathf.Max(1, quantity)}"; // ����: max 1
            // Name text set
        }
    }
    [Client]
    private void SpawnModel()
    {
        if (modelInstance != null) Destroy(modelInstance);
        if (item != null && modelParent != null)
        {
            GameObject modelPrefab = item.GetDropModelPrefab() ?? defaultModelPrefab;
            if (modelPrefab != null)
            {
            modelInstance = Instantiate(modelPrefab, modelParent.position, Quaternion.identity, modelParent);
            modelInstance.transform.localScale = Vector3.one * 0.5f;
                Debug.Log($"[DroppedItem] Model spawned successfully for {item.itemName}: {modelPrefab.name}");
            }
            else
            {
                Debug.LogError($"[DroppedItem] No model prefab found for {item.itemName} (ID: {item.id}). dropModelPrefab: {item.GetDropModelPrefab()}, defaultModelPrefab: {defaultModelPrefab}");
            }
        }
        else
        {
            Debug.LogError($"[DroppedItem] Cannot spawn model: item={item != null}, modelParent={modelParent != null}");
        }
    }
    [Client]
    private void AnimateDrop()
    {
        if (modelInstance == null || modelParent == null) return;
        // �� ����-������� � ��������� �������, ���������� 1-2f (�����)
        Vector2 randomCircle = Random.insideUnitCircle; // 2D ����
        Vector3 randomDir = new Vector3(randomCircle.x, 0, randomCircle.y).normalized;
        float randomDist = Random.Range(1f, 2f);
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + randomDir * randomDist;
        Vector3 midHeight = endPos + Vector3.up * 3f; // ����. ������ ��� ��������
        modelInstance.transform.localPosition = Vector3.zero; // �������� ������������ transform
        if (nameCanvas != null) nameCanvas.transform.localPosition = Vector3.up * 1f; // ��������
        tweenSequence = DOTween.Sequence();
        // ��������: ������� ���� transform (��� collider) + Y ��� ������/������
        tweenSequence.Append(transform.DOMove(new Vector3(endPos.x, startPos.y, endPos.z), 1.5f).SetEase(Ease.InOutQuad)); // �������� �����
        tweenSequence.Join(modelInstance.transform.DOMoveY(midHeight.y - endPos.y, 0.75f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo)); // ������ ������ ������������
        // ����� ������� �� �������
        if (nameCanvas != null)
        {
            tweenSequence.Join(nameCanvas.transform.DOMoveY((midHeight.y + 1f) - endPos.y, 0.75f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo)); // ������ ������ ������������
        }
        tweenSequence.AppendCallback(() =>
        {
            if (modelInstance != null)
            {
                modelInstance.transform.DORotate(new Vector3(0f, 360f, 0f), 2f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
                modelInstance.transform.DOMoveY(modelParent.position.y + 0.2f, 1f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            if (nameCanvas != null)
            {
                nameCanvas.transform.DOMoveY(modelParent.position.y + 1.2f, 1f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        });
        // Animated parabolic drop
    }
    private void OnDestroy()
    {
        if (modelInstance != null) Destroy(modelInstance);
        if (tweenSequence != null && tweenSequence.IsActive()) tweenSequence.Kill();
        if (tweenSequence != null) tweenSequence.Kill();
    }
    [Client]
    public void OnMouseDown()
    {
        if (!isClient) return;
        Debug.Log($"[DroppedItem] OnMouseDown triggered for item: {item?.itemName} (ID: {itemID})");
        
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null)
        {
            Debug.LogWarning("[DroppedItem] No local player found for pickup");
            return;
        }
        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        Debug.Log($"[DroppedItem] Player distance: {distance:F2} (required: {pickupDistance})");
        
        if (distance <= pickupDistance)
        {
            Debug.Log($"[DroppedItem] Requesting pickup of item: {item.itemName} (ID: {itemID}, NetID: {netId})");
            localPlayer.CmdPickupDroppedItem(netId);
        }
        else
        {
            Debug.LogWarning($"[DroppedItem] Player too far to pickup item: {item.itemName} (ID: {itemID}, distance: {distance:F2}, required: {pickupDistance})");
        }
    }
    [Client] // ����: UI ����
    public void OnPointerClick(PointerEventData eventData)
    {
        OnMouseDown(); // �������� �� �� ������
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pickupCursor != null)
        {
            Cursor.SetCursor(pickupCursor, Vector2.zero, CursorMode.Auto);
            Debug.Log("[DroppedItem] Cursor changed to pickup");
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Debug.Log("[DroppedItem] Cursor reset to default");
    }
    [Server]
    public void Pickup(PlayerCore player)
    {
        Debug.Log($"[DroppedItem] Pickup called by player: {player.playerName} (NetID: {player.netId}) for item: {dynamicItemName} (ID: {itemID}, Quantity: {quantity})");
        
        if (quantity <= 0)
        {
            Debug.LogError($"[DroppedItem] Cannot pickup: quantity {quantity} <=0");
            return;
        }
        ItemDatabase db = Resources.Load<ItemDatabase>("ItemDatabase");
        if (db == null)
        {
            Debug.LogError("[DroppedItem] ItemDatabase not found on server");
            return;
        }
        // Создаем ItemInfo с динамическими статами
        ItemInfo itemInfo = new ItemInfo
        {
            id = itemID,
            quantity = quantity,
            hasDynamicStats = !string.IsNullOrEmpty(dynamicItemName),
            dynamicItemName = dynamicItemName,
            strengthBonus = strengthBonus,
            agilityBonus = agilityBonus,
            spiritBonus = spiritBonus,
            constitutionBonus = constitutionBonus,
            accuracyBonus = accuracyBonus,
            minAttackConstantBonus = minAttackConstantBonus,
            maxAttackConstantBonus = maxAttackConstantBonus,
            maxHpConstantBonus = maxHpConstantBonus,
            maxSpConstantBonus = maxSpConstantBonus,
            crtConstantBonus = crtConstantBonus,
            mspdConstantBonus = mspdConstantBonus,
            physicalResist = physicalResist,
            dynamicRarity = dynamicRarity
        };
        
        Debug.Log($"[DroppedItem] Checking ownership - OwnerNetID: {ownerNetId}, PlayerNetID: {player.netId}, Time since drop: {Time.time - dropTime:F2}s");
        
        if (ownerNetId == 0 || player.netId == ownerNetId || Time.time - dropTime >= 30f)
        {
            Debug.Log($"[DroppedItem] Ownership check passed, attempting to add to inventory");
            if (player.Inventory.AddItemInfo(itemInfo))
            {
                Debug.Log($"[DroppedItem] SUCCESS: Item picked up: {itemInfo.dynamicItemName} (ID: {itemID}, quantity: {quantity}) by player {player.playerName}");
                NetworkServer.Destroy(gameObject);
                return;
            }
            else
            {
                Debug.LogWarning($"[DroppedItem] FAILED: Could not add item to inventory (inventory full?)");
            }
        }
        else
        {
            Debug.Log($"[DroppedItem] Pickup denied: Protected for 30s. Owner: {ownerNetId}, Player: {player.netId}, Time elapsed: {Time.time - dropTime:F2}s");
            RpcShowProtectedMessage(player);
        }
    }
    [ClientRpc]
    private void RpcShowProtectedMessage(PlayerCore player)
    {
        if (player == PlayerCore.localPlayerCoreInstance)
        {
            Debug.LogWarning("[DroppedItem] ���� ������� ������� �� ������� 30 ������!");
        }
    }
    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
        Debug.Log($"[DroppedItem] Despawned item: {item?.itemName} (ID: {itemID}) after {despawnTime} seconds");
    }
}