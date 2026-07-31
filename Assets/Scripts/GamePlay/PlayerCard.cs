using UnityEngine;
using TMPro;  // For TextMeshPro
using UnityEngine.UI;

public class PlayerCard : MonoBehaviour
{
    [Header("Dependencies")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI countryText;
    public TextMeshProUGUI paceValueText;
    public TextMeshProUGUI dribblingValueText;
    public TextMeshProUGUI headingValueText;
    public TextMeshProUGUI highPassValueText;
    public TextMeshProUGUI resilienceValueText;
    public TextMeshProUGUI shootingValueText;
    public TextMeshProUGUI tacklingValueText;
    public Image flagImage;
    public Player assignedPlayer;

    // Method to update the card with player data
    public void UpdatePlayerCard(Player player)
    {
        assignedPlayer = player;  // Store the player data
        playerNameText.text = player.Name;
        countryText.text = player.Country;
        paceValueText.text = player.Pace.ToString();
        dribblingValueText.text = player.Dribbling.ToString();
        headingValueText.text = player.Heading.ToString();
        highPassValueText.text = player.HighPass.ToString();
        resilienceValueText.text = player.Resilience.ToString();
        shootingValueText.text = player.Shooting.ToString();
        tacklingValueText.text = player.Tackling.ToString();
        UpdateFlag(player.Country);
    }

    public void UpdateFromToken(PlayerToken token, string secondaryText = "")
    {
        if (token == null)
        {
            return;
        }

        playerNameText.text = (token.playerName ?? string.Empty).ToUpperInvariant();
        countryText.text = (secondaryText ?? string.Empty).ToUpperInvariant();
        paceValueText.text = token.pace.ToString();
        dribblingValueText.text = token.dribbling.ToString();
        headingValueText.text = token.heading.ToString();
        highPassValueText.text = token.highPass.ToString();
        resilienceValueText.text = token.resilience.ToString();
        shootingValueText.text = token.shooting.ToString();
        tacklingValueText.text = token.tackling.ToString();
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
