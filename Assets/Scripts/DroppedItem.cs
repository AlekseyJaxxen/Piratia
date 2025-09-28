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
        quantity = Mathf.Max(1, quantity); // ����: min 1
        SpawnModel();
        UpdateNameText();
        AnimateDrop();
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
            modelInstance = Instantiate(modelPrefab, modelParent.position, Quaternion.identity, modelParent);
            modelInstance.transform.localScale = Vector3.one * 0.5f;
            // Model spawned for item
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
        PlayerCore localPlayer = PlayerCore.localPlayerCoreInstance;
        if (localPlayer == null)
        {
            Debug.LogWarning("[DroppedItem] No local player found for pickup");
            return;
        }
        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        if (distance <= pickupDistance)
        {
            // Pickup requested
            localPlayer.CmdPickupDroppedItem(netId);
        }
        else
        {
            Debug.LogWarning($"[DroppedItem] Player too far to pickup item: {item.itemName} (ID: {itemID}, distance: {distance}, required: {pickupDistance})");
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
        Item item = db.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[DroppedItem] Item with ID {itemID} not found on server");
            return;
        }
        if (ownerNetId == 0 || player.netId == ownerNetId || Time.time - dropTime >= 30f)
        {
            if (player.Inventory.AddItem(item, quantity))
            {
                Debug.Log($"[DroppedItem] Item picked up: {item.itemName} (ID: {itemID}, quantity: {quantity}) by player {player.playerName}");
                NetworkServer.Destroy(gameObject);
                return;
            }
        }
        else
        {
            Debug.Log($"[DroppedItem] Pickup denied: Protected for 30s. Owner: {ownerNetId}, Player: {player.netId}, Time elapsed: {Time.time - dropTime}");
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