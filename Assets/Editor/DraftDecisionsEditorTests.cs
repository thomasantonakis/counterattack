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
