using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FreeDraftTableRowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private const float DoubleClickThreshold = 0.25f;

    private DraftManager draftManager;
    private CanvasGroup canvasGroup;
    private RectTransform rowRectTransform;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRotation;
    private Vector3 mouseOffset;
    private GameObject dragProxy;
    private RectTransform dragProxyRectTransform;
    private float lastClickTime;
    private bool consumed;

    public string CandidateName { get; private set; }
    public bool IsGoalkeeper { get; private set; }

    public void Configure(DraftManager manager, string candidateName, bool isGoalkeeper)
    {
        draftManager = manager;
        CandidateName = candidateName;
        IsGoalkeeper = isGoalkeeper;
        consumed = false;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rowRectTransform = GetComponent<RectTransform>();
    }

    public void MarkConsumed()
    {
        consumed = true;
        DestroyDragProxy();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < DoubleClickThreshold)
        {
            if (draftManager != null && draftManager.AssignFreeDraftCandidateToNextSlot(this))
            {
                MarkConsumed();
            }
        }

        lastClickTime = Time.time;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        CacheOriginalTransformState();
        CreateDragProxy(eventData);

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragProxyRectTransform != null ? dragProxyRectTransform : rowRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePosition);
        Transform draggedTransform = dragProxyRectTransform != null ? dragProxyRectTransform : transform;
        mouseOffset = draggedTransform.position - globalMousePosition;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.25f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Transform draggedTransform = dragProxyRectTransform != null ? dragProxyRectTransform : transform;
        RectTransform draggedRectTransform = dragProxyRectTransform != null ? dragProxyRectTransform : rowRectTransform;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            draggedRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePosition);
        draggedTransform.position = globalMousePosition + mouseOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragProxy();
        if (consumed)
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (originalParent != null)
        {
            RestoreOriginalTransformState();
        }
    }

    private void CacheOriginalTransformState()
    {
        rowRectTransform = GetComponent<RectTransform>();
        if (rowRectTransform == null)
        {
            return;
        }

        originalAnchoredPosition = rowRectTransform.anchoredPosition;
        originalLocalPosition = rowRectTransform.localPosition;
        originalLocalScale = rowRectTransform.localScale;
        originalLocalRotation = rowRectTransform.localRotation;
    }

    private void RestoreOriginalTransformState()
    {
        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);

        if (rowRectTransform == null)
        {
            rowRectTransform = GetComponent<RectTransform>();
        }

        if (rowRectTransform != null)
        {
            rowRectTransform.anchoredPosition = originalAnchoredPosition;
            rowRectTransform.localPosition = originalLocalPosition;
            rowRectTransform.localScale = originalLocalScale;
            rowRectTransform.localRotation = originalLocalRotation;
        }
    }

    private void CreateDragProxy(PointerEventData eventData)
    {
        Transform dragRoot = GetDragRoot();
        dragProxy = Instantiate(gameObject, dragRoot, true);
        dragProxy.name = $"{gameObject.name}-DragProxy";
        dragProxy.transform.SetAsLastSibling();

        FreeDraftTableRowDragHandler proxyDragHandler = dragProxy.GetComponent<FreeDraftTableRowDragHandler>();
        if (proxyDragHandler != null)
        {
            proxyDragHandler.enabled = false;
            Destroy(proxyDragHandler);
        }

        CanvasGroup proxyCanvasGroup = dragProxy.GetComponent<CanvasGroup>();
        if (proxyCanvasGroup == null)
        {
            proxyCanvasGroup = dragProxy.AddComponent<CanvasGroup>();
        }
        proxyCanvasGroup.alpha = 0.85f;
        proxyCanvasGroup.blocksRaycasts = false;
        proxyCanvasGroup.interactable = false;

        dragProxyRectTransform = dragProxy.GetComponent<RectTransform>();
        if (dragProxyRectTransform != null && rowRectTransform != null)
        {
            dragProxyRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            dragProxyRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dragProxyRectTransform.pivot = rowRectTransform.pivot;
            dragProxyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rowRectTransform.rect.width);
            dragProxyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowRectTransform.rect.height);
            dragProxyRectTransform.rotation = rowRectTransform.rotation;
            dragProxyRectTransform.position = rowRectTransform.position;
            MatchProxyWorldSize(eventData);
        }
    }

    private void MatchProxyWorldSize(PointerEventData eventData)
    {
        RectTransform dragRootRect = dragProxyRectTransform != null && dragProxyRectTransform.parent != null
            ? dragProxyRectTransform.parent as RectTransform
            : null;
        if (dragRootRect == null || rowRectTransform == null)
        {
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Camera eventCamera = eventData != null ? eventData.pressEventCamera : null;
        Camera canvasCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;
        Camera conversionCamera = eventCamera != null ? eventCamera : canvasCamera;

        Vector3[] worldCorners = new Vector3[4];
        rowRectTransform.GetWorldCorners(worldCorners);
        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(conversionCamera, worldCorners[0]);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(conversionCamera, worldCorners[2]);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragRootRect, bottomLeftScreen, conversionCamera, out Vector2 bottomLeftLocal) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(dragRootRect, topRightScreen, conversionCamera, out Vector2 topRightLocal))
        {
            return;
        }

        Vector2 proxySize = new Vector2(
            Mathf.Abs(topRightLocal.x - bottomLeftLocal.x),
            Mathf.Abs(topRightLocal.y - bottomLeftLocal.y));
        Vector2 pivotOffset = new Vector2(proxySize.x * dragProxyRectTransform.pivot.x, proxySize.y * dragProxyRectTransform.pivot.y);
        Vector2 pivotLocalPosition = bottomLeftLocal + pivotOffset;

        dragProxyRectTransform.sizeDelta = proxySize;
        dragProxyRectTransform.localPosition = new Vector3(pivotLocalPosition.x, pivotLocalPosition.y, dragProxyRectTransform.localPosition.z);
    }

    private Transform GetDragRoot()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        return parentCanvas != null ? parentCanvas.transform : transform.root;
    }

    private void DestroyDragProxy()
    {
        if (dragProxy == null)
        {
            return;
        }

        Destroy(dragProxy);
        dragProxy = null;
        dragProxyRectTransform = null;
    }
}
