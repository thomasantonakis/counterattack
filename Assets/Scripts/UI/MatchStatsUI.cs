using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class MatchStatsUI : MonoBehaviour
{
    public TMP_Text statsText;  // Drag the TextMeshPro UI element here
    public TMP_Text homeScorersText;
    public TMP_Text awayScorersText;
    public RectTransform panel;     // Assign the MatchStatsUI panel here
    public Button toggleButton;     // Assign a small edge button (like "◀"/"▶")
    public float collapsedX = 370f; // Distance off-screen to slide
    public float animationSpeed = 5f;

    private bool isExpanded = true;
    private Vector2 onScreenPos;
    private Vector2 offScreenPos;

    private void Start()
    {
        InvokeRepeating(nameof(UpdateStatsUI), 1f, 1f);  // Update UI every second
        onScreenPos = panel.anchoredPosition;
        offScreenPos = new Vector2(collapsedX, onScreenPos.y);

        toggleButton.onClick.AddListener(TogglePanel);
    }

    private void TogglePanel()
    {
        isExpanded = !isExpanded;
        StopAllCoroutines();
        StartCoroutine(SlidePanel(isExpanded ? onScreenPos : offScreenPos));

        // Optional: flip button text/icon
        // toggleButton.GetComponentInChildren<TextMeshProUGUI>().text = isExpanded ? "▶" : "◀";
        toggleButton.GetComponentInChildren<TextMeshProUGUI>().text = isExpanded ? "C" : "E";
    }

    private IEnumerator SlidePanel(Vector2 targetPos)
    {
        while (Vector2.Distance(panel.anchoredPosition, targetPos) > 0.1f)
        {
            panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, targetPos, Time.deltaTime * animationSpeed);
            yield return null;
        }
        panel.anchoredPosition = targetPos;
    }

    void UpdateStatsUI()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.gameData == null)
        {
            statsText.text = "Match data not available";
            return;
        }

        MatchManager.TeamStats homeTeam = MatchManager.Instance.gameData.stats.homeTeamStats;
        MatchManager.TeamStats awayTeam = MatchManager.Instance.gameData.stats.awayTeamStats;

        string homeTeamName = MatchManager.Instance.gameData.gameSettings.homeTeamName;
        string awayTeamName = MatchManager.Instance.gameData.gameSettings.awayTeamName;

        statsText.text =
            $"{homeTeamName} - {awayTeamName} \n\n" +
            $"{homeTeam.totalGoals} Goals {awayTeam.totalGoals}\n" +
            $"{homeTeam.totalAssists} Assists {awayTeam.totalAssists}\n"+
            $"{homeTeam.totalShots} ({homeTeam.totalShotsOnTarget}) Shots (On Trg) {awayTeam.totalShots} ({awayTeam.totalShotsOnTarget})\n" +
            $"{homeTeam.totalPassesAttempted} ({homeTeam.totalPassesCompleted}) Passes (Comp) {awayTeam.totalPassesAttempted} ({awayTeam.totalPassesCompleted})\n" +
            $"{homeTeam.totalAerialPassesAttempted}-{homeTeam.totalAerialPassesTargeted}-{homeTeam.totalAerialPassesCompleted} Aerial Att-Trg-Comp {awayTeam.totalAerialPassesAttempted}-{awayTeam.totalAerialPassesTargeted}-{awayTeam.totalAerialPassesCompleted}\n"+
            $"{homeTeam.totalPacesRan} Distance (paces) {awayTeam.totalPacesRan}\n"+
            $"{homeTeam.totalInterceptionsAttempted} ({homeTeam.totalInterceptionsMade}) Int.(Made) {awayTeam.totalInterceptionsAttempted} ({awayTeam.totalInterceptionsMade})\n"+
            $"{homeTeam.totalAerialChallengesInvolved} ({homeTeam.totalAerialChallengesWon}) Aerial Duels(Won) {awayTeam.totalAerialChallengesInvolved} ({awayTeam.totalAerialChallengesWon})\n"
            ;
    }

    public void UpdateScorersDisplay()
    {
        homeScorersText.text = FormatScorerList(MatchManager.Instance.homeScorers);
        awayScorersText.text = FormatScorerList(MatchManager.Instance.awayScorers);
    }

    private string FormatScorerList(List<MatchManager.GoalEvent> scorers)
    {
        return string.Join(", ", scorers.Select(g => g.ToString()));
    }

}