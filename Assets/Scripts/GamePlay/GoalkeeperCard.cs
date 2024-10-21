using UnityEngine;
using TMPro;  // For TextMeshPro

public class GoalkeeperCard : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI countryText;
    public TextMeshProUGUI aerialValueText;
    public TextMeshProUGUI dribblingValueText;
    public TextMeshProUGUI paceValueText;
    public TextMeshProUGUI resilienceValueText;
    public TextMeshProUGUI savingValueText;
    public TextMeshProUGUI handlingValueText;
    public TextMeshProUGUI highPassValueText;
    // public TextMeshProUGUI specialAbilityText;
    
    // Optionally add Image for country flags if you plan to use them
    // public Image flagImage;

    public Goalkeeper assignedgoalkeeper;

    // Method to update the card with player data
    public void UpdatePlayerCard(Goalkeeper goalkeeper)
    {
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
        // Set default black color for the player name and country
        playerNameText.color = Color.black;
        countryText.color = Color.black;

        // Dynamically color the attribute texts based on their values
        aerialValueText.color = GetAttributeColor(goalkeeper.Aerial);
        dribblingValueText.color = GetAttributeColor(goalkeeper.Dribbling);
        paceValueText.color = GetAttributeColor(goalkeeper.Pace);
        resilienceValueText.color = GetAttributeColor(goalkeeper.Resilience);
        savingValueText.color = GetAttributeColor(goalkeeper.Saving);
        handlingValueText.color = GetAttributeColor(goalkeeper.Handling);
        highPassValueText.color = GetAttributeColor(goalkeeper.HighPass);
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