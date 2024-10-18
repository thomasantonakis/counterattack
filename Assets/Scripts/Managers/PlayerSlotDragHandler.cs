using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector3 originalPosition;
    private GameObject placeholder;

    // Add this for valid roster panel name
    public string validRosterName;  // To check against HomeRoster or AwayRoster

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save original parent, position, and sibling index
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Set the valid roster name to the name of the original parent (HomeRoster or AwayRoster)
        validRosterName = originalParent.name;
        Debug.Log($"OnBeginDrag: Slot '{gameObject.name}' starting drag from '{validRosterName}'");

        // Create a placeholder with the same dimensions as the original slot
        placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(originalParent);

        // Copy the RectTransform settings from the original slot to the placeholder
        RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
        RectTransform slotRect = GetComponent<RectTransform>();
        placeholderRect.sizeDelta = slotRect.sizeDelta;
        placeholderRect.pivot = slotRect.pivot;
        placeholderRect.anchorMin = slotRect.anchorMin;
        placeholderRect.anchorMax = slotRect.anchorMax;

        // Add a LayoutElement to the placeholder to match layout behavior
        LayoutElement layoutElement = placeholder.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = slotRect.sizeDelta.x;
        layoutElement.preferredHeight = slotRect.sizeDelta.y;
        placeholder.transform.SetSiblingIndex(originalSiblingIndex);

        // Make the slot semi-transparent while dragging
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        // Move the slot to the root canvas level for easier dragging
        transform.SetParent(transform.root, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update the slot's position to follow the pointer
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Reset transparency and raycast settings
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Check if dropped in a valid roster panel
         if (!IsDroppedInValidRosterPanel(transform))
        {
            // If dropped outside the valid roster, return to the original position and parent
            Debug.Log($"OnEndDrag: Returning slot '{gameObject.name}' to original position in '{originalParent.name}'");
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.position = originalPosition;
        }
        else
        {
            // If valid drop, insert in the new place within the roster
            Debug.Log($"OnEndDrag: Slot '{gameObject.name}' dropped successfully in valid panel '{transform.parent.name}'");
            transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        }

        // Destroy the placeholder
        Destroy(placeholder);
    }

    // Method to check if the slot is dropped in the valid roster panel
    private bool IsDroppedInValidRosterPanel(Transform droppedTransform)
    {
        // Check if the parent of the slot is a valid roster panel
        Transform parent = droppedTransform.parent;

        // Traverse up the hierarchy to find the first valid parent that matches "HomeRoster" or "AwayRoster"
        while (parent != null)
        {
            // Check if the parent is HomeRoster or AwayRoster (valid panel)
            if (parent.name == "HomeRoster" || parent.name == "AwayRoster")
            {
                Debug.Log($"IsDroppedInValidRosterPanel: Slot '{droppedTransform.name}' is dropped in '{parent.name}'. Result: True");
                return parent.name == validRosterName;  // Return true if it matches the original valid roster name
            }
            parent = parent.parent;  // Keep moving up the hierarchy
        }
        Debug.Log($"IsDroppedInValidRosterPanel: Slot '{gameObject.name}' is dropped in 'Canvas' (valid roster: {validRosterName}). Result: False");
        return false;
    }
}
