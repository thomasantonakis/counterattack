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
        // Handle the dropped card
        PlayerCardDragHandler cardDragHandler = eventData.pointerDrag.GetComponent<PlayerCardDragHandler>();

        if (cardDragHandler != null)
        {
            PlayerCard card = cardDragHandler.GetComponent<PlayerCard>();
            UpdateSlot(card);

            // After assigning the card, notify DraftManager
            draftManager.CardAssignedToSlot();  // Call to DraftManager
            Destroy(card.gameObject);  // This removes the card after it's dropped in the slot
        }
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
    }

}