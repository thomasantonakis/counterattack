using System.Collections.Generic;
using NUnit.Framework;

public class DraftDecisionsEditorTests
{
    [Test]
    public void SophisticatedOpeningPickPrefersWillemsOverBratozAndDelgado()
    {
        Player bratoz = CreatePlayer("Bratoz", "Slovenia", 5, 3, 2, 6, 5, 4, 1);
        Player delgado = CreatePlayer("Delgado", "Spain", 5, 4, 4, 5, 5, 2, 3);
        Player willems = CreatePlayer("Willems", "Belgium", 5, 3, 5, 5, 5, 3, 4);
        DraftDecisions.DraftContext context = new DraftDecisions.DraftContext(
            new[] { bratoz, delgado, willems },
            null,
            "Away",
            1,
            7,
            3,
            24);

        DraftDecisions.DraftAction action = DraftDecisions.ChooseOutfielder(DraftDecisions.DraftProfile.Sophisticated, context);

        Assert.That(action.SelectedPlayer, Is.SameAs(willems));
    }

    [Test]
    public void MarginalBestXiComparison_PrefersAguayoOverKuzmina()
    {
        Player kuzmina = CreatePlayer("Kuzmina", "Slovakia", 4, 4, 3, 4, 5, 5, 3);
        Player aguayo = CreatePlayer("Aguayo", "Chile", 4, 5, 5, 5, 4, 5, 3);
        DraftDecisions.DraftContext context = new(
            new[] { kuzmina, aguayo },
            CreateRepresentativePartialRoster(),
            "Away",
            6,
            8,
            1,
            12);

        DraftDecisions.DraftAction action = DraftDecisions.ChooseOutfielder(
            DraftDecisions.DraftProfile.Sophisticated,
            context);
        List<DraftDecisions.SophisticatedCandidateScore> scores =
            DraftDecisions.GetSophisticatedCandidateScores(context);
        DraftDecisions.SophisticatedCandidateScore aguayoScore = scores.Find(score => score.Player == aguayo);
        DraftDecisions.SophisticatedCandidateScore kuzminaScore = scores.Find(score => score.Player == kuzmina);

        Assert.That(action.SelectedPlayer, Is.SameAs(aguayo));
        Assert.That(aguayoScore.StarterImpact, Is.GreaterThan(kuzminaScore.StarterImpact));
        Assert.That(aguayoScore.FinalScore, Is.GreaterThan(kuzminaScore.FinalScore));
    }

    [Test]
    public void MarginalBestXiComparison_PrefersLittlewoodOverGowman()
    {
        Player gowman = CreatePlayer("Gowman", "Wales", 4, 2, 5, 4, 4, 4, 3);
        Player littlewood = CreatePlayer("Littlewood", "England", 4, 4, 4, 6, 5, 4, 4);
        DraftDecisions.DraftContext context = new(
            new[] { gowman, littlewood },
            CreateRepresentativePartialRoster(),
            "Away",
            7,
            8,
            1,
            6);

        DraftDecisions.DraftAction action = DraftDecisions.ChooseOutfielder(
            DraftDecisions.DraftProfile.Sophisticated,
            context);
        List<DraftDecisions.SophisticatedCandidateScore> scores =
            DraftDecisions.GetSophisticatedCandidateScores(context);
        DraftDecisions.SophisticatedCandidateScore littlewoodScore = scores.Find(score => score.Player == littlewood);
        DraftDecisions.SophisticatedCandidateScore gowmanScore = scores.Find(score => score.Player == gowman);

        Assert.That(action.SelectedPlayer, Is.SameAs(littlewood));
        Assert.That(littlewoodScore.StarterImpact, Is.GreaterThan(gowmanScore.StarterImpact));
        Assert.That(littlewoodScore.FinalScore, Is.GreaterThan(gowmanScore.FinalScore));
    }

    private static List<Player> CreateRepresentativePartialRoster()
    {
        return new List<Player>
        {
            CreatePlayer("FB One", "Test", 6, 3, 3, 4, 5, 2, 5),
            CreatePlayer("FB Two", "Test", 5, 4, 3, 4, 5, 2, 5),
            CreatePlayer("CB One", "Test", 3, 2, 6, 4, 6, 2, 6),
            CreatePlayer("CB Two", "Test", 4, 2, 5, 5, 6, 2, 6),
            CreatePlayer("DM", "Test", 4, 3, 4, 6, 6, 3, 5),
            CreatePlayer("Winger", "Test", 6, 6, 2, 4, 4, 5, 2),
            CreatePlayer("Striker", "Test", 5, 4, 5, 3, 5, 6, 2),
            CreatePlayer("Ten", "Test", 5, 6, 3, 5, 4, 5, 2)
        };
    }

    private static Player CreatePlayer(
        string name,
        string nationality,
        int pace,
        int dribbling,
        int heading,
        int highPass,
        int resilience,
        int shooting,
        int tackling)
    {
        return new Player(new Dictionary<string, string>
        {
            ["Name"] = name,
            ["Nationality"] = nationality,
            ["Pace"] = pace.ToString(),
            ["Dribbling"] = dribbling.ToString(),
            ["Heading"] = heading.ToString(),
            ["HighPass"] = highPass.ToString(),
            ["Resilience"] = resilience.ToString(),
            ["Shooting"] = shooting.ToString(),
            ["Tackling"] = tackling.ToString(),
            ["Type"] = "TableTopia"
        });
    }
}
