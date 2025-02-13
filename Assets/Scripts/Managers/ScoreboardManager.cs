using TMPro;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    public MatchManager matchManager;  // Drag and drop the MatchManager object here
    public TMP_Text homeTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayTeamText;  // Drag and drop the TextMeshPro element here

    void Start()
    {
        // Subscribe to the event in MatchManager
        matchManager.OnGameSettingsLoaded += LoadTeamNames;
        // Debug.Log("ScoreboardManager: Subscribed to OnGameSettingsLoaded event");
    }


    void LoadTeamNames()
    {
        Debug.Log("ScoreboardManager: Running LoadTeamNames");
        // Ensure gameData and gameSettings are loaded
        if (MatchManager.Instance.gameData != null && MatchManager.Instance.gameData.gameSettings != null)
        {
            MatchManager.GameSettings settings = MatchManager.Instance.gameData.gameSettings;

            string homeTeamName = settings.homeTeamName;
            string awayTeamName = settings.awayTeamName;
            // Assign the names to separate Text objects
            homeTeamText.text = homeTeamName;
            awayTeamText.text = awayTeamName;
            // Debug.Log($"Home Team: {homeTeamName}, Away Team: {awayTeamName}");
            // Debug.Log($"Home Team: {homeTeamText.text}, Away Team: {awayTeamText.text}");
        }
        else
        {
            Debug.LogError("ScoreboardManager: Game settings are not loaded!");
        }
    }

}
