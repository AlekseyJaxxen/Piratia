using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using DG.Tweening;

public class DroppedItem : NetworkBehaviour
{
    [Header("Settings")]
    [SyncVar] public int itemID;
    [SyncVar] public int quantity;
    [SerializeField] private Transform modelParent;
    [SerializeField] private GameObject defaultModelPrefab; // По умолчанию, если модели нет
    [SerializeField] private Canvas nameCanvas; // World Space Canvas для названия
    [SerializeField] private TextMeshProUGUI nameText; // TextMeshPro для названия
    [SerializeField] public float pickupDistance = 2f; // Радиус подбора
    [SerializeField] private float despawnTime = 300f; // 5 минут до уничтожения
    private Item item;
    private GameObject modelInstance;

    private void Awake()
    {
        // Автоматическая привязка modelParent
        if (modelParent == null)
        {
            modelParent = transform.Find("Empty");
            if (modelParent == null)
            {
                modelParent = transform;
                Debug.LogWarning($"[DroppedItem] modelParent not assigned, using transform of {gameObject.name}");
            }
        }
    }

    private void Start()
    {
        if (isServer)
        {
            Invoke(nameof(DestroySelf), despawnTime); // Уничтожить через 5 минут
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        item = ItemDatabase.Instance.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[DroppedItem] Item with ID {itemID} not found");
            return;
        }
        SpawnModel();
        UpdateNameText();
        AnimateDrop();
    }

    [Client]
    private void UpdateNameText()
    {
        if (item != null && nameText != null)
        {
            nameText.text = $"{item.itemName} x{quantity}";
            Debug.Log($"[DroppedItem] Name text set to: {item.itemName} x{quantity}");
        }
    }

    [Client]
    private void SpawnModel()
    {
        if (modelInstance != null) Destroy(modelInstance);
        if (item != null && modelParent != null)
        {
            GameObject modelPrefab = item.GetDropModelPrefab() ?? defaultModelPrefab;
            modelInstance = Instantiate(modelPrefab, modelParent.position, Quaternion.identity, modelParent);
            modelInstance.transform.localScale = Vector3.one * 0.5f;
            Debug.Log($"[DroppedItem] Spawned model for item: {item.itemName} (ID: {itemID})");
        }
    }

    [Client]
    private void AnimateDrop()
    {
        if (modelInstance == null || modelParent == null) return;
        Vector3 startPos = modelParent.position + Vector3.up * 2f;
        Vector3 endPos = modelParent.position;
        modelInstance.transform.position = startPos;

        // Создаём последовательность анимаций
        Sequence sequence = DOTween.Sequence();
        sequence.Append(modelInstance.transform.DOMove(endPos, 0.7f).SetEase(Ease.InQuad)); // Падение
        sequence.AppendCallback(() =>
        {
            // Циклическое вращение
            modelInstance.transform.DORotate(new Vector3(0f, 360f, 0f), 2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
            // Циклическая пульсация вверх-вниз
            modelInstance.transform.DOMoveY(modelParent.position.y + 0.2f, 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        });

        Debug.Log($"[DroppedItem] Animated drop for item: {item.itemName} (ID: {itemID}) with rotation and pulsation");
    }

    [Client]
    public void OnMouseDown()
    {
        if (!isClient) return;
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null)
        {
            Debug.LogWarning("[DroppedItem] No local player found for pickup");
            return;
        }
        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        if (distance <= pickupDistance)
        {
            Debug.Log($"[DroppedItem] Pickup requested for item: {item.itemName} (ID: {itemID}, distance: {distance})");
            localPlayer.CmdPickupDroppedItem(netId);
        }
        else
        {
            Debug.LogWarning($"[DroppedItem] Player too far to pickup item: {item.itemName} (ID: {itemID}, distance: {distance}, required: {pickupDistance})");
        }
    }

    [Server]
    public void Pickup(PlayerCore player)
    {
        if (player.Inventory.AddItem(item, quantity))
        {
            Debug.Log($"[DroppedItem] Item picked up: {item.itemName} (ID: {itemID}, quantity: {quantity}) by player {player.playerName}");
            NetworkServer.Destroy(gameObject);
        }
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
        Debug.Log($"[DroppedItem] Despawned item: {item?.itemName} (ID: {itemID}) after {despawnTime} seconds");
    }
}