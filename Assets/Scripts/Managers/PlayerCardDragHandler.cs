using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class PlayerCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Vector3 mouseOffset;  // To store the offset between the mouse and the slot's position
    // Placeholder to keep the grid structure while dragging
    private GameObject placeholder;
    private DraftManager draftManager;
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.25f;  // Max time between clicks for a double click

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
        // Find DraftManager in the scene when the slot is created
        draftManager = FindObjectOfType<DraftManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save original parent and position
        originalParent = transform.parent;
        originalPosition = transform.position;
        // Calculate the offset between the mouse position and the slot's position
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePos
        );
        mouseOffset = transform.position - globalMousePos;

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

    public void OnPointerClick(PointerEventData eventData)
    {
        // Detect double-click
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            PlayerCard playerCard = GetComponent<PlayerCard>();
            HandleDoubleClick(playerCard);
        }

        lastClickTime = Time.time;
    }

    private void HandleDoubleClick(PlayerCard card)
    {
        Debug.Log($"Double-click detected on {gameObject.name}");

        // Find the next available slot based on the current team's turn
        string validRosterPanel = draftManager.GetCurrentTeamTurn() == "Home" ? "HomeRoster" : "AwayRoster";
        // Get the valid panel transform
        Transform validPanel = GameObject.Find(validRosterPanel).transform;
        // Find the next available slot in the valid roster
        PlayerSlotDropHandler nextAvailableSlot = draftManager.FindNextAvailableSlot(validPanel.name);

        if (nextAvailableSlot != null)
        {
            // Create a placeholder in the Draft Panel (where the card is currently located)
            Transform draftPanelParent = card.transform.parent;
            GameObject placeholder = new GameObject("Placeholder");
            LayoutElement layoutElementPlaceholder = placeholder.AddComponent<LayoutElement>();

            // Assuming the layoutElement of the card is already set
            LayoutElement cardLayoutElement = card.GetComponent<LayoutElement>();
            layoutElementPlaceholder.preferredWidth = cardLayoutElement.preferredWidth;
            layoutElementPlaceholder.preferredHeight = cardLayoutElement.preferredHeight;

            // Set the placeholder in the Draft Panel at the same index as the card
            placeholder.transform.SetParent(draftPanelParent, false);
            placeholder.transform.SetSiblingIndex(card.transform.GetSiblingIndex());

            // Assign the card to the found slot in the valid roster
            nextAvailableSlot.UpdateSlot(card);
            draftManager.CardAssignedToSlot(card);

            // Destroy the card from the draft panel after assigning to the slot
            Destroy(card.gameObject);

            // // Update the slot with the card info
            // PlayerCard playerCard = GetComponent<PlayerCard>();
            // nextAvailableSlot.UpdateSlot(playerCard);

            // // Notify the DraftManager that a card has been assigned
            // draftManager.CardAssignedToSlot(playerCard);

            // // Destroy the card since it's been assigned
            // Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("No available slots found for double-clicked card.");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update the slot's position to follow the pointer, maintaining the offset
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePos
        );
        transform.position = globalMousePos + mouseOffset;
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
