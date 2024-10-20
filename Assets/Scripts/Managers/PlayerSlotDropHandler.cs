using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerSlotDropHandler : MonoBehaviour, IDropHandler
{
    private DraftManager draftManager;

    private void Start()
    {
        // Find DraftManager in the scene when the slot is created
        draftManager = FindObjectOfType<DraftManager>();
    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"OnDrop called for {gameObject.name}");
        // Check if we are dropping a Player Slot instead of a card
        PlayerSlotDragHandler draggedSlot = eventData.pointerDrag.GetComponent<PlayerSlotDragHandler>();
        if (draggedSlot != null && draggedSlot != this)
        {
            // Check if the dragged slot's parent matches the valid roster
            string droppedRosterName = transform.parent.name;
            if (draggedSlot.validRosterName == droppedRosterName)
            {
                Debug.Log($"Valid drop: {draggedSlot.name} and {gameObject.name} are in the same roster panel.");
                // Swap slot data as the drop is valid
                SwapSlotData(draggedSlot.gameObject);
            }
            else
            {
                Debug.LogError($"Invalid drop: '{gameObject.name}' and '{draggedSlot.name}' are not in the same roster panel.");
            }
        }
        
        // Handle the dropped card
        PlayerCardDragHandler cardDragHandler = eventData.pointerDrag.GetComponent<PlayerCardDragHandler>();
        if (cardDragHandler != null)
        {
            // Check if the drop is valid based on the current team's turn
            if (!draftManager.IsValidTeamPanel(transform.parent.name))
            {
                Debug.LogWarning($"Invalid drop: {transform.parent.name} is not a valid target for {draftManager.GetCurrentTeamTurn()}.");
                return;  // Reject the drop if it's not a valid team panel
            }
            // Debug.Log("Dropping Cards in SlotDropManager");
            PlayerCard card = cardDragHandler.GetComponent<PlayerCard>();
            // Check if the slot is already populated
            if (IsSlotPopulated())
            {
                Debug.Log($"Slot {gameObject.name} is already populated. Finding the next available slot.");
                // Find the next available slot and update that one
                PlayerSlotDropHandler nextAvailableSlot = FindNextAvailableSlot();
                if (nextAvailableSlot != null)
                {
                    nextAvailableSlot.UpdateSlot(card);  // Populate the next available slot
                    draftManager.CardAssignedToSlot(card);  // Pass the card as an argument
                    Destroy(card.gameObject);  // This removes the card after it's dropped in the slot
                }
                else
                {
                    Debug.LogWarning("No available slots left to assign the card.");
                }
            }
            else
            {
                // Update this slot if it's not populated
                UpdateSlot(card);
                // Notify DraftManager that the card has been assigned
                draftManager.CardAssignedToSlot(card);  // Pass the card as an argument
                Destroy(card.gameObject);  // This removes the card after it's dropped in the slot
            }

        }
    }

    private bool IsSlotPopulated()
    {
        // Log the slot name to confirm what's being checked
        // Debug.Log($"Checking if slot '{gameObject.name}' is populated by analyzing its name.");

        // Split the slot's name by '-' and check if it contains more than two parts (indicating it's renamed with player info)
        string[] nameParts = gameObject.name.Split('-');

        // If the slot has more than two parts, it's populated with a player's name
        return nameParts.Length > 2;  // More than 2 parts means it's named something like "Away-6-PlayerName"
    }

    private PlayerSlotDropHandler FindNextAvailableSlot()
    {
        // Get the parent roster (either HomeRoster or AwayRoster)
        Transform rosterParent = transform.parent;
        // Start searching from the next sibling (slot) in the panel
        int siblingIndex = transform.GetSiblingIndex();
        int totalSlots = rosterParent.childCount;
        // First search below the current slot
        for (int i = siblingIndex + 1; i < totalSlots; i++)
        {
            PlayerSlotDropHandler nextSlot = rosterParent.GetChild(i).GetComponent<PlayerSlotDropHandler>();
            if (nextSlot != null && !nextSlot.IsSlotPopulated())
            {
                return nextSlot;  // Return the next available slot
            }
        }
        // If no empty slots below, search from the top
        for (int i = 0; i < siblingIndex; i++)
        {
            PlayerSlotDropHandler nextSlot = rosterParent.GetChild(i).GetComponent<PlayerSlotDropHandler>();
            if (nextSlot != null && !nextSlot.IsSlotPopulated())
            {
                return nextSlot;  // Return the first available slot found from the top
            }
        }
        return null;  // No available slot found
    }

    private void SwapSlotData(GameObject draggedSlot)
    {
        // Debug.Log($"Swapping slot data between {gameObject.name} and {draggedSlot.name}");
        // Get TMP_Text components from both slots (current and dragged)
        Transform currentSlotWrapper = transform.Find("ContentWrapper");
        Transform draggedSlotWrapper = draggedSlot.transform.Find("ContentWrapper");

        if (currentSlotWrapper == null || draggedSlotWrapper == null)
        {
            Debug.LogError("ContentWrapper not found in one of the slots");
            return;
        }

        // Get all the fields to be swapped (except Jersey Number)
        TMP_Text[] currentSlotFields = currentSlotWrapper.GetComponentsInChildren<TMP_Text>();
        TMP_Text[] draggedSlotFields = draggedSlotWrapper.GetComponentsInChildren<TMP_Text>();

        // Temporary storage for the dragged slot's data
        string[] draggedData = new string[draggedSlotFields.Length];

        // Store dragged slot data (but skip Jersey Number)
        for (int i = 1; i < draggedSlotFields.Length; i++)  // Starting from index 1 to skip Jersey Number
        {
            draggedData[i] = draggedSlotFields[i].text;
            // Debug.Log($"Storing data from {draggedSlot.name}: {draggedSlotFields[i].text}");
        }

        // Swap: Move current slot data to dragged slot
        for (int i = 1; i < currentSlotFields.Length; i++)  // Starting from index 1 to skip Jersey Number
        {
            draggedSlotFields[i].text = currentSlotFields[i].text;
            // Debug.Log($"Moving data to dragged slot {draggedSlot.name}: {currentSlotFields[i].text}");
        }

        // Move dragged slot data to the current slot
        for (int i = 1; i < draggedData.Length; i++)  // Starting from index 1 to skip Jersey Number
        {
            currentSlotFields[i].text = draggedData[i];
            // Debug.Log($"Moving data to current slot {gameObject.name}: {draggedData[i]}");
        }
        // Debug.Log($"Slot data swap completed between {gameObject.name} and {draggedSlot.name}");
    }

    private void UpdateSlot(PlayerCard card)
    {
        // Navigate to the ContentWrapper before accessing the text fields
        Transform contentWrapper = transform.Find("ContentWrapper");
        
        if (contentWrapper == null)
        {
            Debug.LogError("ContentWrapper not found in PlayerSlot prefab");
            return;
        }

        // Update the text fields inside the ContentWrapper
        contentWrapper.Find("PlayerNameInSlot").GetComponent<TMP_Text>().text = card.playerNameText.text;  // Name
        contentWrapper.Find("PaceInSlot").GetComponent<TMP_Text>().text = card.paceValueText.text;  // Pace
        contentWrapper.Find("DribblingInSlot").GetComponent<TMP_Text>().text = card.dribblingValueText.text;  // Dribbling
        contentWrapper.Find("HeadingInSlot").GetComponent<TMP_Text>().text = card.headingValueText.text;  // Heading
        contentWrapper.Find("HighPassInSlot").GetComponent<TMP_Text>().text = card.highPassValueText.text;  // High Pass
        contentWrapper.Find("ResilienceInSlot").GetComponent<TMP_Text>().text = card.resilienceValueText.text;  // Resilience
        contentWrapper.Find("ShootingInSlot").GetComponent<TMP_Text>().text = card.shootingValueText.text;  // Shooting
        contentWrapper.Find("TacklingInSlot").GetComponent<TMP_Text>().text = card.tacklingValueText.text;  // Tackling
        // Rename the slot by appending the player's name
        string originalSlotName = gameObject.name;  // Keep only the "Home-#" or "Away-#"
        gameObject.name = $"{originalSlotName}-{card.playerNameText.text}";  // Append player name to the slot name
        Debug.Log($"Slot renamed to: {gameObject.name}");
    }

}