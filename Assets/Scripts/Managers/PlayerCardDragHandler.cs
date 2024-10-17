using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private Transform originalParent;
    private Vector3 originalPosition;

    // Placeholder to keep the grid structure while dragging
    private GameObject placeholder;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save original parent and position
        originalParent = transform.parent;
        originalPosition = transform.position;

        // Create a placeholder in the grid to maintain the structure
        placeholder = new GameObject("Placeholder");
        var layoutElementPlaceholder = placeholder.AddComponent<LayoutElement>();
        layoutElementPlaceholder.preferredWidth = layoutElement.preferredWidth;
        layoutElementPlaceholder.preferredHeight = layoutElement.preferredHeight;

        // Insert the placeholder at the same position as the original card
        placeholder.transform.SetParent(originalParent, false);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Make the card semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;  // Disable raycasts so other objects can receive events

        // Ignore layout to prevent the remaining cards from being rearranged
        layoutElement.ignoreLayout = true;

        // Move card to the root canvas level for easier dragging
        transform.SetParent(transform.root, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update the card's position to follow the pointer
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Reset transparency
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;  // Enable raycasts again

        // Check if dropped on a valid slot
        if (transform.parent == transform.root)
        {
            // If not dropped on a valid slot, return to the original position and parent
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            transform.position = originalPosition;
        }

        // Destroy the placeholder
        Destroy(placeholder);

        // Restore layout handling after drag ends
        layoutElement.ignoreLayout = false;
    }
}
