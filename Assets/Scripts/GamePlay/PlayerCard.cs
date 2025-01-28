using UnityEngine;
using TMPro;  // For TextMeshPro

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
    // Optionally add Image for country flags if you plan to use them
    // public Image flagImage;
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
        // Set default black color for the player name and country
        playerNameText.color = Color.black;
        countryText.color = Color.black;

        // Dynamically color the attribute texts based on their values
        paceValueText.color = GetAttributeColor(player.Pace);
        dribblingValueText.color = GetAttributeColor(player.Dribbling);
        headingValueText.color = GetAttributeColor(player.Heading);
        highPassValueText.color = GetAttributeColor(player.HighPass);
        resilienceValueText.color = GetAttributeColor(player.Resilience);
        shootingValueText.color = GetAttributeColor(player.Shooting);
        tacklingValueText.color = GetAttributeColor(player.Tackling);
        // Set flag based on country (if using flag sprites)
        // flagImage.sprite = Resources.Load<Sprite>($"Flags/{player.Country}");
    }

    private Color GetAttributeColor(int value)
    {
        if (value >= 5)
        {
            return new Color(0f, 0.5f, 0f);  // Dark Green
        }
        else if (value >= 3)
        {
            return new Color(0.8f, 0.4f, 0f);  // Dark Orange
        }
        else
        {
            return new Color(0.5f, 0f, 0f);  // Dark Red
        }
    }
}