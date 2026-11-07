using UnityEngine;
using TMPro;  // For TextMeshPro
using UnityEngine.UI;

public class GoalkeeperCard : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public PlayerToken DisplayedToken { get; private set; }
    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left && eventData.clickCount == 2)
            RoomInspectionCard.Instance?.InspectPreview(DisplayedToken);
    }
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI countryText;
    public TextMeshProUGUI aerialValueText;
    public TextMeshProUGUI dribblingValueText;
    public TextMeshProUGUI paceValueText;
    public TextMeshProUGUI resilienceValueText;
    public TextMeshProUGUI savingValueText;
    public TextMeshProUGUI handlingValueText;
    public TextMeshProUGUI highPassValueText;
    public Image flagImage;
    // public TextMeshProUGUI specialAbilityText;
    
    // Optionally add Image for country flags if you plan to use them
    // public Image flagImage;

    [System.NonSerialized] public Goalkeeper assignedgoalkeeper;

    // Method to update the card with player data
    public void UpdatePlayerCard(Goalkeeper goalkeeper)
    {
        DisplayedToken = null;
        assignedgoalkeeper = goalkeeper;  // Store the player data
        playerNameText.text = goalkeeper.Name;
        countryText.text = goalkeeper.Country;
        aerialValueText.text = goalkeeper.Aerial.ToString();
        dribblingValueText.text = goalkeeper.Dribbling.ToString();
        paceValueText.text = goalkeeper.Pace.ToString();
        resilienceValueText.text = goalkeeper.Resilience.ToString();
        savingValueText.text = goalkeeper.Saving.ToString();
        handlingValueText.text = goalkeeper.Handling.ToString();
        highPassValueText.text = goalkeeper.HighPass.ToString();
        UpdateFlag(goalkeeper.Country);
    }

    public void UpdateFromToken(PlayerToken token, string secondaryText = "")
    {
        DisplayedToken = token;
        if (token == null)
        {
            return;
        }

        playerNameText.text = (token.playerName ?? string.Empty).ToUpperInvariant();
        countryText.text = (secondaryText ?? string.Empty).ToUpperInvariant();
        aerialValueText.text = token.aerial.ToString();
        dribblingValueText.text = token.dribbling.ToString();
        paceValueText.text = token.pace.ToString();
        resilienceValueText.text = token.resilience.ToString();
        savingValueText.text = token.saving.ToString();
        handlingValueText.text = token.handling.ToString();
        highPassValueText.text = token.highPass.ToString();
        UpdateFlag(secondaryText);
    }

    private void UpdateFlag(string country)
    {
        Image image = ResolveFlagImage();
        if (image == null)
        {
            return;
        }

        Sprite sprite = FlagSpriteProvider.GetFlagSprite(country);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        image.gameObject.SetActive(sprite != null);
    }

    private Image ResolveFlagImage()
    {
        if (flagImage != null)
        {
            return flagImage;
        }

        Transform flagTransform = transform.Find("WhiteBackground/Flag");
        if (flagTransform == null)
        {
            flagTransform = transform.Find("Flag");
        }

        if (flagTransform != null)
        {
            flagImage = flagTransform.GetComponent<Image>();
        }

        return flagImage;
    }
}
