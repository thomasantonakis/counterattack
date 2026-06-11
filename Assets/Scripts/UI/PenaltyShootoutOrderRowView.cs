using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PenaltyShootoutOrderRowView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TMP_Text labelText;
    public TMP_Text nameText;
    public TMP_Text shootingText;
    public PlayerToken Token { get; private set; }

    private PenaltyShootoutOrderPanelController owner;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private GameObject dragProxy;
    private RectTransform dragProxyRectTransform;
    private Vector3 mouseOffset;

    public void Configure(PenaltyShootoutOrderPanelController configuredOwner, PlayerToken token, int order)
    {
        owner = configuredOwner;
        Token = token;
        rectTransform ??= GetComponent<RectTransform>();
        canvasGroup ??= GetComponent<CanvasGroup>();

        if (labelText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName;
            labelText.text = $"{order}. #{token.jerseyNumber} {displayName}";
        }

        if (nameText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName;
            nameText.text = $"{order}. #{token.jerseyNumber} {displayName}";
        }

        if (shootingText != null)
        {
            shootingText.text = $"Shooting: {token.shooting}";
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        CreateDragProxy(eventData);
        CacheMouseOffset(eventData);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.25f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragProxyRectTransform == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragProxyRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePosition);
        dragProxyRectTransform.position = globalMousePosition + mouseOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragProxy();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (owner != null)
        {
            owner.PlaceDraggedRow(this, eventData, originalParent, originalSiblingIndex);
        }
        else if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }

    private void CreateDragProxy(PointerEventData eventData)
    {
        Transform dragRoot = GetDragRoot();
        dragProxy = Instantiate(gameObject, dragRoot, true);
        dragProxy.name = $"{gameObject.name}-DragProxy";
        dragProxy.transform.SetAsLastSibling();

        PenaltyShootoutOrderRowView proxyRow = dragProxy.GetComponent<PenaltyShootoutOrderRowView>();
        if (proxyRow != null)
        {
            proxyRow.enabled = false;
            Destroy(proxyRow);
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
        if (dragProxyRectTransform != null && rectTransform != null)
        {
            dragProxyRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            dragProxyRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dragProxyRectTransform.pivot = rectTransform.pivot;
            dragProxyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.width);
            dragProxyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.height);
            dragProxyRectTransform.rotation = rectTransform.rotation;
            dragProxyRectTransform.position = rectTransform.position;
            MatchProxyWorldSize(eventData);
        }
    }

    private void CacheMouseOffset(PointerEventData eventData)
    {
        if (dragProxyRectTransform == null)
        {
            mouseOffset = Vector3.zero;
            return;
        }

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragProxyRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePosition);
        mouseOffset = dragProxyRectTransform.position - globalMousePosition;
    }

    private void MatchProxyWorldSize(PointerEventData eventData)
    {
        RectTransform dragRootRect = dragProxyRectTransform != null && dragProxyRectTransform.parent != null
            ? dragProxyRectTransform.parent as RectTransform
            : null;
        if (dragRootRect == null || rectTransform == null)
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
        rectTransform.GetWorldCorners(worldCorners);
        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(conversionCamera, worldCorners[0]);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(conversionCamera, worldCorners[2]);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragRootRect, bottomLeftScreen, conversionCamera, out Vector2 bottomLeftLocal)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(dragRootRect, topRightScreen, conversionCamera, out Vector2 topRightLocal))
        {
            return;
        }

        Vector2 proxySize = new(Mathf.Abs(topRightLocal.x - bottomLeftLocal.x), Mathf.Abs(topRightLocal.y - bottomLeftLocal.y));
        Vector2 pivotOffset = new(proxySize.x * dragProxyRectTransform.pivot.x, proxySize.y * dragProxyRectTransform.pivot.y);
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
