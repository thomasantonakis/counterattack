using TMPro;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    public MatchManager matchManager;  // Drag and drop the MatchManager object here
    public TMP_Text homeTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayTeamText;  // Drag and drop the TextMeshPro element here

    private void OnEnable()
    {
        if (matchManager == null)
        {
            matchManager = MatchManager.Instance ?? FindObjectOfType<MatchManager>();
        }

        if (matchManager == null)
        {
            Debug.LogError("ScoreboardManager: MatchManager reference is missing.");
            return;
        }

        matchManager.OnGameSettingsLoaded += LoadTeamNames;

        // Room direct-play can load settings before this component's Start/OnEnable ordering settles.
        // Apply the already-loaded names immediately when data is present.
        if (matchManager.gameData?.gameSettings != null)
        {
            LoadTeamNames();
        }
    }

    private void OnDisable()
    {
        if (matchManager != null)
        {
            matchManager.OnGameSettingsLoaded -= LoadTeamNames;
        }
    }

    void LoadTeamNames()
    {
        Debug.Log("ScoreboardManager: Running LoadTeamNames");
        if (matchManager == null)
        {
            matchManager = MatchManager.Instance ?? FindObjectOfType<MatchManager>();
        }

        if (homeTeamText == null || awayTeamText == null)
        {
            Debug.LogError("ScoreboardManager: Team text references are missing.");
            return;
        }

        // Use the resolved MatchManager reference instead of reaching back through the singleton
        // during early scene initialization, where Instance may not be ready yet.
        if (matchManager != null && matchManager.gameData?.gameSettings != null)
        {
            MatchManager.GameSettings settings = matchManager.gameData.gameSettings;

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
