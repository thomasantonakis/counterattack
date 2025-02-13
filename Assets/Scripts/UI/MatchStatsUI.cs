using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MatchStatsUI : MonoBehaviour
{
    public TMP_Text statsText;  // Drag the TextMeshPro UI element here
    public TMP_Text homeScorersText;
    public TMP_Text awayScorersText;

    private void Start()
    {
        InvokeRepeating(nameof(UpdateStatsUI), 1f, 1f);  // Update UI every second
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
            $"{homeTeam.totalShots} ({homeTeam.totalShotsOnTarget}) Shots (On Trg) {awayTeam.totalShots} ({awayTeam.totalShotsOnTarget})\n" +
            $"{homeTeam.totalPassesAttempted} ({homeTeam.totalPassesCompleted}) Passes (Comp) {awayTeam.totalPassesAttempted} ({awayTeam.totalPassesCompleted})\n" +
            $"{homeTeam.totalPacesRan} Distance (paces) {awayTeam.totalPacesRan}\n"+
            $"{homeTeam.totalInterceptionsAttempted} ({homeTeam.totalInterceptionsMade}) Int.(Made) {awayTeam.totalInterceptionsAttempted} ({awayTeam.totalInterceptionsMade})\n"+
            $"{homeTeam.totalAerialPassesAttempted} Aerial Att {awayTeam.totalAerialPassesAttempted}\n"+
            $"{homeTeam.totalAerialPassesTargeted} Aerial Targ {awayTeam.totalAerialPassesTargeted}\n"+
            $"{homeTeam.totalAerialPassesCompleted} Aerial Completed {awayTeam.totalAerialPassesCompleted}\n"+
            $"{homeTeam.totalAssists} Assists {awayTeam.totalAssists}\n"
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