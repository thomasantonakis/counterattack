using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class TeamNameAbbreviatorEditorTests
{
    [Test]
    public void CatalogReflectsCurrentRulesAndKeepsThreeCharacterCodes()
    {
        var rows = JArray.Parse(Resources.Load<TextAsset>("UI/team-name-abbreviations").text);
        Assert.That(rows.Count, Is.EqualTo(208));
        foreach (JToken row in rows)
        {
            string name = row.Value<string>("teamName");
            string code = row.Value<string>("abbreviation");
            Assert.That(TeamNameAbbreviator.Abbreviate(name), Is.EqualTo(code), name);
            Assert.That(code, Does.Match("^[A-Z0-9]{3}$"));
        }
    }

    [TestCase("  aurora F.C. ", "AUR")]
    [TestCase("FC Grun-Weiss Galoppa", "GWG")]
    [TestCase("FC Pacos de Ferreira", "PDF")]
    [TestCase("Já Foste", "JFO")]
    [TestCase("Netherlands", "NED")]
    [TestCase("Japan", "JPN")]
    [TestCase("North Harbor FC", "NOH")]
    [TestCase("Ravenwood", "RAV")]
    [TestCase("Inverness Caledonian Thistle FC", "ICT")]
    [TestCase("Inverness United", "INV")]
    [TestCase("FC Porto", "POR")]
    [TestCase("Porto", "POR")]
    [TestCase("Portimonense", "PTM")]
    [TestCase("SC Ontario", "SCO")]
    [TestCase("FCD Vignareal", "FCD")]
    [TestCase("Riverton United FC XI", "RIV")]
    [TestCase("Kings United North Harbor FC XI", "KNH")]
    [TestCase("SC Ontario FC XI", "SCO")]
    public void RecognizesNamesAndGeneratesUnlistedCodes(string name, string expected)
    {
        Assert.That(TeamNameAbbreviator.Abbreviate(name), Is.EqualTo(expected));
    }

    [TestCase("Atletico", "Atletico Salsiccia")]
    [TestCase("North Harbor FC", "North Heath FC")]
    [TestCase("North Harbor FC", "North Harbor FC")]
    [TestCase("X", "X")]
    [TestCase("", "")]
    [TestCase("⚽", "⚽⚽")]
    [TestCase("Greece", "Green Club")]
    [TestCase("Green Club", "Greece")]
    public void PairCodesAreReadableDistinctAndStableAcrossReloads(string home, string away)
    {
        var pair = TeamNameAbbreviator.CreatePair(home, away);
        Assert.That(pair.home, Does.Match("^[A-Z0-9]{3}$"));
        Assert.That(pair.away, Does.Match("^[A-Z0-9]{3}$"));
        Assert.That(TeamNameAbbreviator.AreTooSimilar(pair.home, pair.away), Is.False);
        Assert.That(TeamNameAbbreviator.CreatePair(home, away), Is.EqualTo(pair));
        Assert.That(TeamNameAbbreviator.CreatePair(home, away, pair.home, pair.away), Is.EqualTo(pair));
        if (home == "Greece") Assert.That(pair.home, Is.EqualTo("GRE"));
        if (away == "Greece") Assert.That(pair.away, Is.EqualTo("GRE"));
    }

    [Test]
    public void OfficialInternationalCodesAreNeverRenamedToAvoidSimilarity()
    {
        var pair = TeamNameAbbreviator.CreatePair("Brazil", "France", "ZZZ", "YYY");
        Assert.That(pair.home, Is.EqualTo("BRA"));
        Assert.That(pair.away, Is.EqualTo("FRA"));
        Assert.That(TeamNameAbbreviator.AreTooSimilar("B0A", "BOA"), Is.True);
    }

    [TestCase("FC Porto", "Portugal")]
    [TestCase("Portugal", "FC Porto")]
    [TestCase("SC Ontario", "Scotland")]
    [TestCase("Scotland", "SC Ontario")]
    public void SharedClubBaseCodesAreDifferentiatedFromOfficialCountryCodes(string home, string away)
    {
        var pair = TeamNameAbbreviator.CreatePair(home, away);
        Assert.That(TeamNameAbbreviator.AreTooSimilar(pair.home, pair.away), Is.False);
        Assert.That(TeamNameAbbreviator.CreatePair(home, away, pair.home, pair.away), Is.EqualTo(pair));
        if (home == "Portugal") Assert.That(pair.home, Is.EqualTo("POR"));
        if (away == "Portugal") Assert.That(pair.away, Is.EqualTo("POR"));
        if (home == "Scotland") Assert.That(pair.home, Is.EqualTo("SCO"));
        if (away == "Scotland") Assert.That(pair.away, Is.EqualTo("SCO"));
    }

    [Test]
    public void SupersededSavedBaseCodesMigrateWhilePairSpecificAlternativesStayStable()
    {
        var pair = TeamNameAbbreviator.CreatePair("SC Ontario", "FC Porto", "ONT", "FCP");
        Assert.That(pair.home, Is.EqualTo("SCO"));
        Assert.That(pair.away, Is.EqualTo("POR"));
        var exception = TeamNameAbbreviator.CreatePair("FCD Vignareal", "Inverness United", "FVI", null);
        Assert.That(exception.home, Is.EqualTo("FCD"));
        Assert.That(exception.away, Is.EqualTo("INV"));
        var differentiated = TeamNameAbbreviator.CreatePair("FC Porto", "Portugal", "PTO", "POR");
        Assert.That(differentiated.home, Is.EqualTo("PTO"));
        Assert.That(differentiated.away, Is.EqualTo("POR"));
    }

    [TestCase("Portimonense", "PTM")]
    [TestCase("Poritiba", "PBA")]
    public void PortoPairingsRetainKnownChoicesOrDifferentiateTheNewName(string opponent, string expected)
    {
        var pair = TeamNameAbbreviator.CreatePair("Porto", opponent);
        Assert.That(pair.home, Is.EqualTo("POR"));
        Assert.That(pair.away, Is.EqualTo(expected));
        Assert.That(TeamNameAbbreviator.CreatePair("FC Porto", opponent), Is.EqualTo(pair));
        var reverse = TeamNameAbbreviator.CreatePair(opponent, "Porto");
        Assert.That(reverse.home, Is.EqualTo(expected));
        Assert.That(reverse.away, Is.EqualTo("POR"));
    }

    [Test]
    public void NewGameJsonContainsDifferentiatedCodesAndPreservesFullNames()
    {
        var root = new GameObject("Abbreviation JSON authoring");
        root.SetActive(false);
        try
        {
            var manager = root.AddComponent<CreateNewGameManager>();
            var settings = new GameSettings { homeTeamName = "North Harbor FC", awayTeamName = "North Heath FC", squadSize = "16" };
            string json = (string)typeof(CreateNewGameManager)
                .GetMethod("BuildNewGameDataJson", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { settings });
            var saved = JObject.Parse(json)["gameSettings"];
            Assert.That(saved.Value<string>("homeTeamName"), Is.EqualTo(settings.homeTeamName));
            Assert.That(saved.Value<string>("awayTeamName"), Is.EqualTo(settings.awayTeamName));
            Assert.That(TeamNameAbbreviator.AreTooSimilar(saved.Value<string>("homeTeamAbbreviation"),
                saved.Value<string>("awayTeamAbbreviation")), Is.False);
            var loaded = JsonConvert.DeserializeObject<MatchManager.GameData>(json);
            TeamNameAbbreviator.EnsureAbbreviations(loaded.gameSettings);
            Assert.That(loaded.gameSettings.homeTeamAbbreviation, Is.EqualTo(settings.homeTeamAbbreviation));
            Assert.That(loaded.gameSettings.awayTeamAbbreviation, Is.EqualTo(settings.awayTeamAbbreviation));
            // Both DTOs must retain the new fields, including the draft-settings load path.
            var draftSettings = saved.ToObject<GameSettings>();
            Assert.That(draftSettings.homeTeamAbbreviation, Is.EqualTo(settings.homeTeamAbbreviation));
            Assert.That(draftSettings.awayTeamAbbreviation, Is.EqualTo(settings.awayTeamAbbreviation));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void EditingDraftSettingsKeepsTheRestOfTheSaveAndRegeneratesCodes()
    {
        string path = Path.Combine(Path.GetTempPath(), "ScoreboardCodes_" + System.Guid.NewGuid().ToString("N") + ".json");
        const string original = "{\"gameSettings\":{\"homeTeamName\":\"Old Home\",\"awayTeamName\":\"Old Away\"},\"rosters\":{\"home\":{\"1\":{\"name\":\"Keeper\"}}},\"marker\":7}";
        var root = new GameObject("Edited draft abbreviations");
        root.SetActive(false);
        try
        {
            File.WriteAllText(path, original);
            var manager = root.AddComponent<CreateNewGameManager>();
            var settings = new GameSettings { homeTeamName = "England", awayTeamName = "Scotland" };
            string json = (string)typeof(CreateNewGameManager)
                .GetMethod("BuildUpdatedExistingGameDataJson", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { path, settings });
            var saved = JObject.Parse(json);
            Assert.That(saved["gameSettings"].Value<string>("homeTeamAbbreviation"), Is.EqualTo("ENG"));
            Assert.That(saved["gameSettings"].Value<string>("awayTeamAbbreviation"), Is.EqualTo("SCO"));
            Assert.That(saved["rosters"]["home"]["1"].Value<string>("name"), Is.EqualTo("Keeper"));
            Assert.That(saved.Value<int>("marker"), Is.EqualTo(7));
            Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void LegacySettingsAndMainScoreboardUseCodesWhileStatsKeepsNames()
    {
        var previousMatch = MatchManager.Instance;
        var root = new GameObject("Scoreboard abbreviation ownership");
        root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>();
            MatchManager.Instance = match;
            match.gameData = new MatchManager.GameData();
            var settings = match.gameData.gameSettings;
            settings.homeTeamName = "Greece";
            settings.awayTeamName = "Gremio";
            var scoreboard = root.AddComponent<ScoreboardManager>();
            scoreboard.matchManager = match;
            scoreboard.homeTeamText = MakeText(root.transform, "Home code");
            scoreboard.awayTeamText = MakeText(root.transform, "Away code");
            typeof(ScoreboardManager).GetMethod("LoadTeamNames", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(scoreboard, null);
            Assert.That(scoreboard.homeTeamText.text, Is.EqualTo("GRE"));
            Assert.That(scoreboard.awayTeamText.text, Is.EqualTo("GIO"));
            var stats = root.AddComponent<MatchStatsUI>();
            stats.homeTeamNameText = MakeText(root.transform, "Home full name");
            stats.awayTeamNameText = MakeText(root.transform, "Away full name");
            stats.UpdateScorersDisplay();
            Assert.That(stats.homeTeamNameText.text, Is.EqualTo("Greece"));
            Assert.That(stats.awayTeamNameText.text, Is.EqualTo("Gremio"));
        }
        finally { MatchManager.Instance = previousMatch; Object.DestroyImmediate(root); }
    }

    private static TMP_Text MakeText(Transform parent, string name)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        return text;
    }
}
