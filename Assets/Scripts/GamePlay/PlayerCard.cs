using UnityEngine;
using TMPro;  // For TextMeshPro

public class PlayerCard : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI countryText;
    public TextMeshProUGUI paceValueText;
    public TextMeshProUGUI dribblingValueText;
    public TextMeshProUGUI headingValueText;
    public TextMeshProUGUI highPassValueText;
    public TextMeshProUGUI resilienceValueText;
    public TextMeshProUGUI shootingValueText;
    public TextMeshProUGUI tacklingValueText;
    // public TextMeshProUGUI specialAbilityText;
    
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

        // Set flag based on country (if using flag sprites)
        // flagImage.sprite = Resources.Load<Sprite>($"Flags/{player.Country}");
    }
}