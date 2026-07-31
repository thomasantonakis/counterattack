using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PlayerSlotDropHandler : MonoBehaviour, IDropHandler
{
    private DraftManager draftManager;

    private void Start()
    {
        // Find DraftManager in the scene when the slot is created
        draftManager = FindAnyObjectByType<DraftManager>();
    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"OnDrop called for {gameObject.name}");
        if (eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        // Dragging an already-assigned roster slot is a reordering action, not a new draft pick.
        // Allowed only inside the same roster panel:
        // - outfielders can swap with outfielders
        // - GKs can swap with GKs
        PlayerSlotDragHandler draggedSlot = eventData.pointerDrag.GetComponent<PlayerSlotDragHandler>();
        if (draggedSlot != null && draggedSlot != this)
        {
            // Check if the dragged slot's parent matches the valid roster
            string droppedRosterName = transform.parent.name;
            if (draggedSlot.validRosterName == droppedRosterName)
            {
                // Determine if the target slot is a GK slot (either 1 or 12)
                PlayerSlotDropHandler draggedDropHandler = draggedSlot.GetComponent<PlayerSlotDropHandler>();
                bool isCurrentSlotGK = IsGoalkeeperRosterSlot();
                bool isDraggedSlotGK = draggedDropHandler != null && draggedDropHandler.IsGoalkeeperRosterSlot();

                if (isCurrentSlotGK && isDraggedSlotGK)
                {
                    // Allow the swap since both slots are goalkeeper slots
                    Debug.Log($"Valid GK swap: {draggedSlot.name} and {gameObject.name}.");
                    SwapSlotData(draggedSlot.gameObject);
                }
                else if (!isCurrentSlotGK && !isDraggedSlotGK)
                {
                    // Allow normal player swapping for non-GK slots
                    Debug.Log($"Valid player swap: {draggedSlot.name} and {gameObject.name}.");
                    SwapSlotData(draggedSlot.gameObject);
                }
                else
                {
                    // Invalid drop: Goalkeeper can only be swapped with another goalkeeper
                    Debug.LogWarning($"Invalid drop: '{draggedSlot.name}' and '{gameObject.name}' are not both GK or non-GK slots.");
                }
            }
            else
            {
                Debug.LogWarning($"Invalid drop: '{gameObject.name}' and '{draggedSlot.name}' are not in the same roster panel.");
            }
        }

        FreeDraftTableRowDragHandler freeDraftRow = eventData.pointerDrag.GetComponent<FreeDraftTableRowDragHandler>();
        if (freeDraftRow != null)
        {
            if (draftManager != null && draftManager.AssignFreeDraftCandidateToSlot(freeDraftRow, this))
            {
                if (!draftManager.IsCurrentFreeDraftPhaseArcade())
                {
                    freeDraftRow.MarkConsumed();
                }
                else
                {
                    freeDraftRow.RestoreAfterReusableDrop();
                }
            }

            return;
        }
        
        // Dropping a draft card is a new selection from the current 4-card batch.
        PlayerCardDragHandler cardDragHandler = eventData.pointerDrag.GetComponent<PlayerCardDragHandler>();
        if (cardDragHandler != null)
        {
            // New picks are restricted to the active team's roster only.
            if (!draftManager.IsValidTeamPanel(transform.parent.name))
            {
                Debug.LogWarning($"Invalid drop: {transform.parent.name} is not a valid target for {draftManager.GetCurrentTeamTurn()}.");
                return;  // Reject the drop if it's not a valid team panel
            }
            // Outfield cards can never be dropped into goalkeeper slots 1 or 12.
            if (IsGoalkeeperRosterSlot())
            {
                Debug.LogWarning($"Invalid drop: Cannot place a player card in a goalkeeper slot {gameObject.name}.");
                return;  // Reject the drop if it's a GK slot
            }
            // Debug.Log("Dropping Cards in SlotDropManager");
            PlayerCard card = cardDragHandler.GetComponent<PlayerCard>();
            // Check if the slot is already populated
            if (IsSlotPopulated())
            {
                Debug.Log($"Slot {gameObject.name} is already populated. Finding the next available slot.");
                // Quality-of-life rule: dropping on a filled slot advances to the next empty slot
                // in the same roster instead of rejecting the pick outright.
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

    public bool IsSlotPopulated()
    {
        // Log the slot name to confirm what's being checked
        // Debug.Log($"Checking if slot '{gameObject.name}' is populated by analyzing its name.");

        // Split the slot's name by '-' and check if it contains more than two parts (indicating it's renamed with player info)
        string[] nameParts = gameObject.name.Split('-');

        // If the slot has more than two parts, it's populated with a player's name
        return nameParts.Length > 2;  // More than 2 parts means it's named something like "Away-6-PlayerName"
    }

    public bool IsGoalkeeperRosterSlot()
    {
        int jerseyNumber = GetJerseyNumber();
        return jerseyNumber == 1 || jerseyNumber == 12;
    }

    public int GetJerseyNumber()
    {
        string[] nameParts = gameObject.name.Split('-');
        if (nameParts.Length > 1 && int.TryParse(nameParts[1], out int jerseyNumber))
        {
            return jerseyNumber;
        }

        Transform contentWrapper = transform.Find("ContentWrapper");
        TMP_Text jerseyText = contentWrapper != null ? contentWrapper.Find("Jersey#")?.GetComponent<TMP_Text>() : null;
        if (jerseyText != null && int.TryParse(jerseyText.text.Trim(), out jerseyNumber))
        {
            return jerseyNumber;
        }

        return -1;
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
        Debug.Log($"Swapping slot data between {gameObject.name} and {draggedSlot.name}");
        
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
        }

        // Swap: Move current slot data to dragged slot
        for (int i = 1; i < currentSlotFields.Length; i++)  // Starting from index 1 to skip Jersey Number
        {
            draggedSlotFields[i].text = currentSlotFields[i].text;

            if (i > 1) // Assuming index 1 is the player name, which doesn't need color coding
            {
                if (int.TryParse(currentSlotFields[i].text, out int attributeValue))
                {
                    draggedSlotFields[i].color = GetAttributeColor(attributeValue);  // Apply correct color coding for attributes
                }
            }
            else
            {
                draggedSlotFields[i].color = Color.white;  // Set player name to black
            }
        }

        // Move dragged slot data to the current slot
        for (int i = 1; i < draggedData.Length; i++)  // Starting from index 1 to skip Jersey Number
        {
            currentSlotFields[i].text = draggedData[i];

            if (i > 1) // Assuming index 1 is the player name, which doesn't need color coding
            {
                if (int.TryParse(draggedData[i], out int attributeValue))
                {
                    currentSlotFields[i].color = GetAttributeColor(attributeValue);  // Apply correct color coding for attributes
                }
            }
            else
            {
                currentSlotFields[i].color = Color.white;  // Set player name to white
            }
        }

        // Rename both slots in the hierarchy to reflect the swapped player names
        // Ensure that the jersey number (e.g., "Home-1" or "Home-7") is preserved
        string currentSlotBaseName = gameObject.name.Split('-')[0] + "-" + gameObject.name.Split('-')[1];  // e.g., "Home-1"
        string draggedSlotBaseName = draggedSlot.name.Split('-')[0] + "-" + draggedSlot.name.Split('-')[1];  // e.g., "Home-7"
        // Handle renaming for an empty slot (no player name)
        gameObject.name = string.IsNullOrWhiteSpace(currentSlotFields[1].text) ? currentSlotBaseName : $"{currentSlotBaseName}-{currentSlotFields[1].text}";
        draggedSlot.name = string.IsNullOrWhiteSpace(draggedSlotFields[1].text) ? draggedSlotBaseName : $"{draggedSlotBaseName}-{draggedSlotFields[1].text}";
        Debug.Log($"Slot renaming completed: {gameObject.name} and {draggedSlot.name}");
        if (draftManager != null)
        {
            draftManager.RefreshRosterAverages();
        }
    }

    public void UpdateGoalkeeperSlot(Goalkeeper gk)
    {
        // Navigate to the ContentWrapper before accessing the text fields
        Transform contentWrapper = transform.Find("ContentWrapper");

        if (contentWrapper == null)
        {
            Debug.LogError("ContentWrapper not found in PlayerSlot prefab");
            return;
        }

        int jerseyNumber = GetJerseyNumber();
        if (jerseyNumber == 12)
        {
            contentWrapper.Find("TopBorder").GetComponent<Image>().color = new Color(0.9137255f, 0.9647059f, 0.9568627f, 1f);
        }
        
        // Update the text fields inside the ContentWrapper
        TMP_Text playerNameText = contentWrapper.Find("PlayerNameInSlot").GetComponent<TMP_Text>();
        playerNameText.text = gk.Name;

        TMP_Text paceText = contentWrapper.Find("PaceInSlot").GetComponent<TMP_Text>();
        TMP_Text dribblingText = contentWrapper.Find("DribblingInSlot").GetComponent<TMP_Text>();
        TMP_Text aerialText = contentWrapper.Find("HeadingInSlot").GetComponent<TMP_Text>();  // Reuse "Heading" for "Aerial"
        TMP_Text highPassText = contentWrapper.Find("HighPassInSlot").GetComponent<TMP_Text>();
        TMP_Text resilienceText = contentWrapper.Find("ResilienceInSlot").GetComponent<TMP_Text>();
        TMP_Text savingText = contentWrapper.Find("ShootingInSlot").GetComponent<TMP_Text>();  // Reuse "Shooting" for "Saving"
        TMP_Text handlingText = contentWrapper.Find("TacklingInSlot").GetComponent<TMP_Text>();  // Reuse "Tackling" for "Handling"

        // Set text
        playerNameText.text = gk.Name;
        paceText.text = gk.Pace.ToString();
        dribblingText.text = gk.Dribbling.ToString();
        aerialText.text = gk.Aerial.ToString();
        highPassText.text = gk.HighPass.ToString();
        resilienceText.text = gk.Resilience.ToString();
        savingText.text = gk.Saving.ToString();
        handlingText.text = gk.Handling.ToString();

        // Update the image fields inside the ContentWrapper
        Image paceBadge = contentWrapper.Find("PaceBadge").GetComponent<Image>();
        Image dribblingBadge = contentWrapper.Find("DribblingBadge").GetComponent<Image>();
        Image aerialBadge = contentWrapper.Find("HeadingBadge").GetComponent<Image>();  // Reuse "Heading" for "Aerial"
        Image highPassBadge = contentWrapper.Find("HighPassBadge").GetComponent<Image>();
        Image resilienceBadge = contentWrapper.Find("ResilienceBadge").GetComponent<Image>();
        Image savingBadge = contentWrapper.Find("ShootingBadge").GetComponent<Image>();  // Reuse "Shooting" for "Saving"
        Image handlingBadge = contentWrapper.Find("TacklingBadge").GetComponent<Image>();  // Reuse "Tackling" for "Handling"

        // Apply dynamic colors based on the attribute values
        paceBadge.color = GetAttributeColor(gk.Pace);
        dribblingBadge.color = GetAttributeColor(gk.Dribbling);
        aerialBadge.color = GetAttributeColor(gk.Aerial);
        highPassBadge.color = GetAttributeColor(gk.HighPass);
        resilienceBadge.color = GetAttributeColor(gk.Resilience);
        savingBadge.color = GetAttributeColor(gk.Saving);
        handlingBadge.color = GetAttributeColor(gk.Handling);

        // Rename the slot by appending the goalkeeper's name
        gameObject.name = $"{GetSlotBaseName()}-{gk.Name}";  // Append GK name to the slot name
        Debug.Log($"Slot renamed to: {gameObject.name}");
    }

    public void ClearSlot()
    {
        Transform contentWrapper = transform.Find("ContentWrapper");
        if (contentWrapper == null)
        {
            Debug.LogError("ContentWrapper not found in PlayerSlot prefab");
            return;
        }

        ClearSlotText(contentWrapper, "PlayerNameInSlot");
        ClearSlotText(contentWrapper, "PaceInSlot");
        ClearSlotText(contentWrapper, "DribblingInSlot");
        ClearSlotText(contentWrapper, "HeadingInSlot");
        ClearSlotText(contentWrapper, "HighPassInSlot");
        ClearSlotText(contentWrapper, "ResilienceInSlot");
        ClearSlotText(contentWrapper, "ShootingInSlot");
        ClearSlotText(contentWrapper, "TacklingInSlot");

        gameObject.name = GetSlotBaseName();
        Debug.Log($"Slot cleared and renamed to: {gameObject.name}");
    }

    private void ClearSlotText(Transform contentWrapper, string childName)
    {
        TMP_Text text = contentWrapper.Find(childName)?.GetComponent<TMP_Text>();
        if (text == null)
        {
            return;
        }

        text.text = string.Empty;
    }

    public void UpdatePlayerSlot(Player player)
    {
        Transform contentWrapper = transform.Find("ContentWrapper");

        if (contentWrapper == null)
        {
            Debug.LogError("ContentWrapper not found in PlayerSlot prefab");
            return;
        }

        // Update the text fields inside the ContentWrapper
        TMP_Text playerNameText = contentWrapper.Find("PlayerNameInSlot").GetComponent<TMP_Text>();
        playerNameText.text = player.Name;
        
        TMP_Text paceText = contentWrapper.Find("PaceInSlot").GetComponent<TMP_Text>();
        TMP_Text dribblingText = contentWrapper.Find("DribblingInSlot").GetComponent<TMP_Text>();
        TMP_Text headingText = contentWrapper.Find("HeadingInSlot").GetComponent<TMP_Text>();
        TMP_Text highPassText = contentWrapper.Find("HighPassInSlot").GetComponent<TMP_Text>();
        TMP_Text resilienceText = contentWrapper.Find("ResilienceInSlot").GetComponent<TMP_Text>();
        TMP_Text shootingText = contentWrapper.Find("ShootingInSlot").GetComponent<TMP_Text>();
        TMP_Text tacklingText = contentWrapper.Find("TacklingInSlot").GetComponent<TMP_Text>();

        // Set text
        paceText.text = player.Pace.ToString();
        dribblingText.text = player.Dribbling.ToString();
        headingText.text = player.Heading.ToString();
        highPassText.text = player.HighPass.ToString();
        resilienceText.text = player.Resilience.ToString();
        shootingText.text = player.Shooting.ToString();
        tacklingText.text = player.Tackling.ToString();

        // Update the image fields inside the ContentWrapper
        Image paceBadge = contentWrapper.Find("PaceBadge").GetComponent<Image>();
        Image dribblingBadge = contentWrapper.Find("DribblingBadge").GetComponent<Image>();
        Image headingBadge = contentWrapper.Find("HeadingBadge").GetComponent<Image>();
        Image highPassBadge = contentWrapper.Find("HighPassBadge").GetComponent<Image>();
        Image resilienceBadge = contentWrapper.Find("ResilienceBadge").GetComponent<Image>();
        Image shootingBadge = contentWrapper.Find("ShootingBadge").GetComponent<Image>();
        Image tacklingBadge = contentWrapper.Find("TacklingBadge").GetComponent<Image>();

        // Apply dynamic colors based on the attribute values
        paceBadge.color = GetAttributeColor(player.Pace);
        dribblingBadge.color = GetAttributeColor(player.Dribbling);
        headingBadge.color = GetAttributeColor(player.Heading);
        highPassBadge.color = GetAttributeColor(player.HighPass);
        resilienceBadge.color = GetAttributeColor(player.Resilience);
        shootingBadge.color = GetAttributeColor(player.Shooting);
        tacklingBadge.color = GetAttributeColor(player.Tackling);

        gameObject.name = $"{GetSlotBaseName()}-{player.Name}";
        Debug.Log($"Slot renamed to: {gameObject.name}");
    }

    public void UpdateSlot(PlayerCard card)
    {
        // Navigate to the ContentWrapper before accessing the text fields
        Transform contentWrapper = transform.Find("ContentWrapper");
        
        if (contentWrapper == null)
        {
            Debug.LogError("ContentWrapper not found in PlayerSlot prefab");
            return;
        }
        // Update the text fields inside the ContentWrapper
        TMP_Text playerNameText = contentWrapper.Find("PlayerNameInSlot").GetComponent<TMP_Text>();
        playerNameText.text = card.playerNameText.text;

        TMP_Text paceText = contentWrapper.Find("PaceInSlot").GetComponent<TMP_Text>();
        TMP_Text dribblingText = contentWrapper.Find("DribblingInSlot").GetComponent<TMP_Text>();
        TMP_Text headingText = contentWrapper.Find("HeadingInSlot").GetComponent<TMP_Text>();
        TMP_Text highPassText = contentWrapper.Find("HighPassInSlot").GetComponent<TMP_Text>();
        TMP_Text resilienceText = contentWrapper.Find("ResilienceInSlot").GetComponent<TMP_Text>();
        TMP_Text shootingText = contentWrapper.Find("ShootingInSlot").GetComponent<TMP_Text>();
        TMP_Text tacklingText = contentWrapper.Find("TacklingInSlot").GetComponent<TMP_Text>();

        // Set text
        playerNameText.text = card.playerNameText.text;
        paceText.text = card.paceValueText.text;
        dribblingText.text = card.dribblingValueText.text;
        headingText.text = card.headingValueText.text;
        highPassText.text = card.highPassValueText.text;
        resilienceText.text = card.resilienceValueText.text;
        shootingText.text = card.shootingValueText.text;
        tacklingText.text = card.tacklingValueText.text;

        // Update the image fields inside the ContentWrapper
        Image paceBadge = contentWrapper.Find("PaceBadge").GetComponent<Image>();
        Image dribblingBadge = contentWrapper.Find("DribblingBadge").GetComponent<Image>();
        Image headingBadge = contentWrapper.Find("HeadingBadge").GetComponent<Image>();
        Image highPassBadge = contentWrapper.Find("HighPassBadge").GetComponent<Image>();
        Image resilienceBadge = contentWrapper.Find("ResilienceBadge").GetComponent<Image>();
        Image shootingBadge = contentWrapper.Find("ShootingBadge").GetComponent<Image>();
        Image tacklingBadge = contentWrapper.Find("TacklingBadge").GetComponent<Image>();

        // Apply dynamic colors based on the attribute values
        paceBadge.color = GetAttributeTextColor(card.paceValueText.text);
        dribblingBadge.color = GetAttributeTextColor(card.dribblingValueText.text);
        headingBadge.color = GetAttributeTextColor(card.headingValueText.text);
        highPassBadge.color = GetAttributeTextColor(card.highPassValueText.text);
        resilienceBadge.color = GetAttributeTextColor(card.resilienceValueText.text);
        shootingBadge.color = GetAttributeTextColor(card.shootingValueText.text);
        tacklingBadge.color = GetAttributeTextColor(card.tacklingValueText.text);

        // Rename the slot by appending the player's name
        gameObject.name = $"{GetSlotBaseName()}-{card.playerNameText.text}";  // Append player name to the slot name
        Debug.Log($"Slot renamed to: {gameObject.name}");
    }

    private string GetSlotBaseName()
    {
        string[] nameParts = gameObject.name.Split('-');
        if (nameParts.Length >= 2)
        {
            return $"{nameParts[0]}-{nameParts[1]}";
        }

        return gameObject.name;
    }

    private Color GetAttributeTextColor(string valueText)
    {
        if (int.TryParse(valueText, out int value))
        {
            return GetAttributeColor(value);
        }
        return Color.white;  // Default color if parsing fails
    }
    private Color GetAttributeColor(int value)
    {
        if (value >= 5)
        {
            return new Color(0.1019608f, 0.7058824f, 0.654902f, 1f);  // Green
        }
        else if (value >= 3f)
        {
            return new Color(0.9686275f, 0.5647059f, 0.3411765f, 1f);  // Orange
        }
        else
        {
            return new Color(0.8823529f, 0.3843137f, 0.3333333f, 1f);  // Red
        }
    }

}

public class FreeDraftPoolDropHandler : MonoBehaviour, IDropHandler
{
    private DraftManager draftManager;

    public void Configure(DraftManager manager)
    {
        draftManager = manager;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draftManager == null || eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        PlayerSlotDragHandler slotDragHandler = eventData.pointerDrag.GetComponent<PlayerSlotDragHandler>();
        PlayerSlotDropHandler slotDropHandler = eventData.pointerDrag.GetComponent<PlayerSlotDropHandler>();
        if (slotDragHandler == null || slotDropHandler == null)
        {
            return;
        }

        if (draftManager.ReturnRosterSlotToPool(slotDropHandler))
        {
            slotDragHandler.MarkReturnedToPool();
        }
    }
}
