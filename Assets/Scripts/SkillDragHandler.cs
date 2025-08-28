using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private GameObject dragObject;
    private RectTransform dragRectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvas = FindFirstObjectByType<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragObject = new GameObject("DragIcon");
        dragObject.transform.SetParent(canvas.transform, false);
        dragObject.transform.SetAsLastSibling();

        Image dragImage = dragObject.AddComponent<Image>();
        dragImage.sprite = GetComponent<Image>().sprite;
        dragImage.SetNativeSize();

        CanvasGroup dragCanvasGroup = dragObject.AddComponent<CanvasGroup>();
        dragCanvasGroup.blocksRaycasts = false;

        dragRectTransform = dragObject.GetComponent<RectTransform>();
        dragRectTransform.sizeDelta = new Vector2(50, 50); // Adjust size as needed

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        dragRectTransform.localPosition = localPoint;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragRectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Destroy(dragObject);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}