using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>Updates the authored Luck page. Never creates UI objects.</summary>
public sealed class PostMatchLuckView : MonoBehaviour
{
    [SerializeField] private TMP_Text homeName, awayName, homeLuck, awayLuck, homeDetail, awayDetail, coverage;
    [SerializeField] private TMP_Text[] categoryLabels, homeValues, awayValues;
    public PostMatchLuckData.Report LastReport { get; private set; }
    public void Configure(TMP_Text home, TMP_Text away, TMP_Text homeScore, TMP_Text awayScore,
        TMP_Text homeBreakdown, TMP_Text awayBreakdown, TMP_Text sample,
        TMP_Text[] labels, TMP_Text[] homeCategories, TMP_Text[] awayCategories)
    {
        homeName = home; awayName = away; homeLuck = homeScore; awayLuck = awayScore;
        homeDetail = homeBreakdown; awayDetail = awayBreakdown; coverage = sample;
        categoryLabels = labels; homeValues = homeCategories; awayValues = awayCategories;
    }
    public void Show(MatchManager.GameData data)
    {
        LastReport = PostMatchLuckData.Build(data);
        var report = LastReport;
        homeName.text = string.IsNullOrWhiteSpace(data?.gameSettings?.homeTeamName) ? "Home" : data.gameSettings.homeTeamName;
        awayName.text = string.IsNullOrWhiteSpace(data?.gameSettings?.awayTeamName) ? "Away" : data.gameSettings.awayTeamName;
        int count = report.Checks.Count;
        homeLuck.text = count == 0 ? "No assessed checks" : Signed(report.HomeLuckPercentagePoints) + " pp";
        awayLuck.text = count == 0 ? "No assessed checks" : Signed(-report.HomeLuckPercentagePoints) + " pp";
        homeDetail.text = count == 0 ? "No luck estimate available." : Detail(report.ActualHome, report.ExpectedHome, count);
        awayDetail.text = count == 0 ? "No luck estimate available." : Detail(count - report.ActualHome, count - report.ExpectedHome, count);
        coverage.text = $"{count} assessed checks from {report.AssessedRolls}/{report.TotalRolls} recorded rolls\n"
            + $"{report.ForcedRolls} forced rolls excluded · {report.UnassessedRolls} other rolls unassessed";
        for (int i = 0; i < categoryLabels.Length; i++)
        {
            var checks = report.Checks.Where(c => c.Category == PostMatchLuckData.Categories[i]).ToArray();
            double surplus = checks.Sum(c => c.ActualHome - c.ExpectedHome);
            categoryLabels[i].text = PostMatchLuckData.Categories[i] + $" ({checks.Length})";
            homeValues[i].text = checks.Length == 0 ? "—" : Signed(surplus) + " checks";
            awayValues[i].text = checks.Length == 0 ? "—" : Signed(-surplus) + " checks";
        }
    }
    private static string Detail(double actual, double expected, int count)
        => $"Favourable: {(100 * actual / count).ToString("0.0", CultureInfo.InvariantCulture)}%\n"
            + $"Expected: {(100 * expected / count).ToString("0.0", CultureInfo.InvariantCulture)}%\n"
            + $"Surplus: {Signed(actual - expected)} checks";
    private static string Signed(double value) => (System.Math.Abs(value) < .00001 ? 0 : value).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
}
