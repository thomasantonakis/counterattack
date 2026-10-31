using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class RoomSaveBrowserEditorTests
{
    private string folder;
    private static readonly MethodInfo ReadSummary = typeof(RoomSaveService).GetMethod(
        "TryReadSummary", BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp]
    public void SetUp()
    {
        folder = Path.Combine(Path.GetTempPath(), "CounterattackSaveBrowser_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(folder, true);
    }

    [TestCase("match__AI Decisions.json")]
    [TestCase("match__AI Decisions Index.json")]
    [TestCase("match__AI Decisions__H1_00-09.json")]
    [TestCase("match__AI Decisions__H2_100-109.json")]
    [TestCase("match__Tactical Notes.json")]
    [TestCase("match__Live Events Index.json")]
    [TestCase("match__Live Events__H1_00-09.jsonl")]
    [TestCase("match__ai decisions.JSON")]
    public void DiagnosticCompanionsAreRejectedEvenIfTheyContainSaveFields(string name)
    {
        string path = Write(name, Save());
        Assert.That(Summary(path), Is.Null);
    }

    [TestCase("match.json")]
    [TestCase("match__Run_20261028T203846807Z_293f3744.json")]
    [TestCase("My AI Decisions review.json")]
    public void ManualSaveFilenamesRemainEligible(string name)
    {
        JObject save = Save();
        save["saveKind"] = "Manual";
        Assert.That(Summary(Write(name, save)), Is.Not.Null);
    }

    [Test]
    public void SummaryUsesResumableScoreClockAndExistingColumnsDespiteLargeHistory()
    {
        JObject save = Save();
        save["stats"]["homeTeamStats"]["totalGoals"] = 9;
        save["events"] = new JArray(new JObject { ["details"] = new string('x', 1024 * 1024) });
        save["runtimeSnapshot"]["tokens"] = new JArray(new JObject { ["irrelevant"] = "token history" });
        RoomSaveSummary summary = Summary(Write("match.json", save));
        Assert.That(summary.Teams, Is.EqualTo("Home - Away"));
        Assert.That(summary.Score, Is.EqualTo("2-1"));
        Assert.That(summary.Clock, Is.EqualTo("47:03"));
        Assert.That(summary.MatchDuration, Is.EqualTo("2x45"));
        Assert.That(summary.Tie, Is.EqualTo("EP"));
        Assert.That(summary.HasRuntimeSnapshot, Is.True);
    }

    [Test]
    public void CachedMetadataDoesNotMixGameModeFilters()
    {
        string path = Write("match.json", Save());
        Assert.That(Summary(path, "Hot Seat"), Is.Null);
        Assert.That(Summary(path, "Single Player"), Is.Not.Null);
        Assert.That(Summary(path, "Hot Seat"), Is.Null);
    }

    [Test]
    public void CacheRefreshesAfterSameLengthExternalRewrite()
    {
        JObject save = Save();
        string path = Write("match.json", save);
        Assert.That(Summary(path).Score, Is.EqualTo("2-1"));
        long originalLength = new FileInfo(path).Length;
        DateTime originalWrite = File.GetLastWriteTimeUtc(path);
        save["runtimeSnapshot"]["stats"]["homeTeamStats"]["totalGoals"] = 3;
        Write("match.json", save);
        File.SetLastWriteTimeUtc(path, originalWrite.AddSeconds(2));
        Assert.That(new FileInfo(path).Length, Is.EqualTo(originalLength));
        Assert.That(Summary(path).Score, Is.EqualTo("3-1"));
    }

    [Test]
    public void CacheRefreshesWhenLengthChangesEvenIfTimestampIsPreserved()
    {
        JObject save = Save();
        string path = Write("match.json", save);
        Assert.That(Summary(path).Teams, Is.EqualTo("Home - Away"));
        DateTime originalWrite = File.GetLastWriteTimeUtc(path);
        save["gameSettings"]["homeTeamName"] = "Renamed Home";
        Write("match.json", save);
        File.SetLastWriteTimeUtc(path, originalWrite);
        Assert.That(Summary(path).Teams, Is.EqualTo("Renamed Home - Away"));
    }

    [Test]
    public void MissingSnapshotOrIncompleteRosterRemainsIneligible()
    {
        JObject save = Save();
        save["runtimeSnapshot"] = null;
        Assert.That(Summary(Write("no-snapshot.json", save)), Is.Null);
        save = Save();
        ((JObject)save["rosters"]["away"]).Remove("11");
        Assert.That(Summary(Write("incomplete.json", save)), Is.Null);
    }

    [Test]
    public void LegacyRootStatsFallbackAndCompletedStatusArePreserved()
    {
        JObject save = Save();
        ((JObject)save["runtimeSnapshot"]).Remove("stats");
        save["runtimeSnapshot"]["clock"]["isMatchComplete"] = true;
        RoomSaveSummary summary = Summary(Write("legacy.json", save));
        Assert.That(summary.Score, Is.EqualTo("2-1"));
        Assert.That(summary.Status, Is.EqualTo("Match Complete"));
    }

    [Test]
    public void LoadingCreatesHiddenSessionBesideItsLogsWithoutChangingManualSlot()
    {
        string source = Write("match.json", Save());
        string original = File.ReadAllText(source);
        Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string run, out string message), Is.True, message);
        Assert.That(Path.GetDirectoryName(run), Is.EqualTo(Path.Combine(folder, "PlaySessions")));
        Assert.That(Summary(source), Is.Not.Null);
        Assert.That(Summary(run), Is.Null);
        Assert.That(ManualPath(run), Is.EqualTo(source));
        Assert.That(File.ReadAllText(source), Is.EqualTo(original));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LegacyRunAppearsOnlyIfTheUserExplicitlySavedIt(bool savedByUser)
    {
        JObject save = Save();
        save["playSessionId"] = "293f3744f7e34eca831bbc6bb9ad3f2e";
        save["playSessionStartedUtc"] = "2026-10-28T20:38:46Z";
        save["lastSavedUtc"] = savedByUser ? "2026-10-28T20:40:00Z" : "2026-10-26T20:43:57Z";
        save["lastLogWrittenUtc"] = "2026-10-28T21:00:00Z";
        RoomSaveSummary summary = Summary(Write("match__Run_20261028T203846807Z_293f3744.json", save));
        Assert.That(summary != null, Is.EqualTo(savedByUser), "Live logging must not make a run a manual save.");
    }

    [Test]
    public void ManualSaveOverwritesOriginalSlotAndKeepsSessionIdentityAndLogPaths()
    {
        string source = Write("match.json", Save());
        Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string run, out string message), Is.True, message);
        JObject checkpoint = JObject.Parse(File.ReadAllText(run));
        string id = checkpoint.Value<string>("playSessionId");
        string log = Path.ChangeExtension(run, null) + "__AI Decisions.json";
        File.WriteAllText(log, "existing run evidence");
        checkpoint["lastSavedUtc"] = DateTime.UtcNow.ToString("o");
        checkpoint["runtimeSnapshot"]["stats"]["homeTeamStats"]["totalGoals"] = 4;
        Assert.That(WriteCheckpoint(checkpoint, ManualPath(run), run), Is.EqualTo(run));
        Assert.That(Summary(source).Score, Is.EqualTo("4-1"));
        Assert.That(Summary(run), Is.Null, "A checkpoint must not publish the session as another save slot.");
        Assert.That(JObject.Parse(File.ReadAllText(run)).Value<string>("playSessionId"), Is.EqualTo(id));
        Assert.That(File.ReadAllText(log), Is.EqualTo("existing run evidence"));
    }

    [Test]
    public void SaveAsPublishesNamedSlotAndSubsequentSaveTargetsItWithoutSwitchingSession()
    {
        string source = Write("match.json", Save());
        string original = File.ReadAllText(source);
        Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string run, out string message), Is.True, message);
        JObject checkpoint = JObject.Parse(File.ReadAllText(run));
        string namedSlot = Path.Combine(folder, "My checkpoint.json");
        Assert.That(WriteCheckpoint(checkpoint, namedSlot, run), Is.EqualTo(run));
        Assert.That(ManualPath(run), Is.EqualTo(namedSlot));
        checkpoint["runtimeSnapshot"]["stats"]["awayTeamStats"]["totalGoals"] = 3;
        Assert.That(WriteCheckpoint(checkpoint, ManualPath(run), run), Is.EqualTo(run));
        Assert.That(Summary(namedSlot).Score, Is.EqualTo("2-3"));
        Assert.That(File.ReadAllText(source), Is.EqualTo(original));
        Assert.That(Summary(run), Is.Null);
    }

    [Test]
    public void LoadingAnExistingSessionDoesNotNestFoldersOrLoseTheManualSlot()
    {
        string source = Write("match.json", Save());
        Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string first, out string message), Is.True, message);
        Assert.That(RoomSaveService.CreateLoadedRunCopy(first, out string second, out message), Is.True, message);
        Assert.That(second, Is.Not.EqualTo(first));
        Assert.That(Path.GetDirectoryName(second), Is.EqualTo(Path.Combine(folder, "PlaySessions")));
        Assert.That(ManualPath(second), Is.EqualTo(source));
        Assert.That(JObject.Parse(File.ReadAllText(second)).Value<string>("sourceSavePath"), Is.EqualTo(first));
    }

    [Test]
    public void LegacyRunReferencesResolveBackToTheOriginalManualSlot()
    {
        string source = Write("match.json", Save());
        JObject legacy = Save();
        legacy["playSessionId"] = "293f3744f7e34eca831bbc6bb9ad3f2e";
        legacy["playSessionStartedUtc"] = "2026-10-28T20:38:46Z";
        legacy["lastSavedUtc"] = "2026-10-26T20:43:57Z";
        legacy["sourceSavePath"] = source;
        string first = Write("match__Run_20261028T203846807Z_293f3744.json", legacy);
        legacy["sourceSavePath"] = first;
        string second = Write("match__Run_20261029T083846807Z_12345678.json", legacy);
        Assert.That(ManualPath(second), Is.EqualTo(source));
    }

    [TestCase("Single Player")]
    [TestCase("Hot Seat")]
    public void DeleteRemovesOnlyTheSelectedManualJsonAndPreservesSessionsAndDiagnostics(string mode)
    {
        JObject save = Save();
        save["gameSettings"]["gameMode"] = mode;
        string source = Write("match.json", save);
        string other = Write("other.json", save);
        string diagnostic = Write("match__AI Decisions.json", save);
        Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string run, out string message), Is.True, message);
        Assert.That(DeleteSlot(source, folder, mode), Is.True);
        Assert.That(File.Exists(source), Is.False);
        Assert.That(File.Exists(other), Is.True);
        Assert.That(File.Exists(diagnostic), Is.True);
        Assert.That(File.Exists(run), Is.True);
    }

    [Test]
    public void DeleteRejectsAFileFromAnotherMode()
    {
        JObject save = Save();
        save["gameSettings"]["gameMode"] = "Hot Seat";
        string source = Write("hotseat.json", save);
        Assert.That(DeleteSlot(source, folder, "Single Player"), Is.False);
        Assert.That(File.Exists(source), Is.True);
    }

    [Test]
    public void DeleteRejectsAFileOutsideTheListedFolder()
    {
        string source = Write("match.json", Save());
        Assert.That(DeleteSlot(source, Path.Combine(folder, "OtherFolder"), "Single Player"), Is.False);
        Assert.That(File.Exists(source), Is.True);
    }

    [Test]
    public void DeleteRejectsProtectedFixture()
    {
        string source = Write("gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json", Save());
        Assert.That(DeleteSlot(source, folder, "Single Player"), Is.False);
        Assert.That(File.Exists(source), Is.True);
    }

    [TestCase("match__AI Decisions.json")]
    [TestCase("match__Tactical Notes.json")]
    public void DeleteRejectsDiagnosticFilesEvenIfTheyContainSaveFields(string name)
    {
        string path = Write(name, Save());
        Assert.That(DeleteSlot(path, folder, "Single Player"), Is.False);
        Assert.That(File.Exists(path), Is.True);
    }

    private static bool DeleteSlot(string path, string saveFolder, string mode)
    {
        object[] args = { path, saveFolder, mode, null };
        return (bool)typeof(RoomSaveService).GetMethod("TryDeleteManualSave", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, args);
    }

    private static string ManualPath(string path) => (string)typeof(RoomSaveService)
        .GetMethod("ResolveManualSavePath", BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { path });

    private static string WriteCheckpoint(JObject root, string destination, string activePath) => (string)typeof(RoomSaveService)
        .GetMethod("WriteManualSave", BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { root, destination, activePath });

    private RoomSaveSummary Summary(string path, string mode = "Single Player")
        => (RoomSaveSummary)ReadSummary.Invoke(null, new object[] { path, mode });

    private string Write(string name, JObject save)
    {
        string path = Path.Combine(folder, name);
        File.WriteAllText(path, save.ToString(Formatting.None));
        return path;
    }

    private static JObject Save()
    {
        var home = new JObject();
        var away = new JObject();
        for (int number = 1; number <= 11; number++)
        {
            home[number.ToString()] = new JObject();
            away[number.ToString()] = new JObject();
        }
        var stats = new JObject
        {
            ["homeTeamStats"] = new JObject { ["totalGoals"] = 2 },
            ["awayTeamStats"] = new JObject { ["totalGoals"] = 1 }
        };
        return new JObject
        {
            ["gameSettings"] = new JObject
            {
                ["gameMode"] = "Single Player", ["homeTeamName"] = "Home", ["awayTeamName"] = "Away",
                ["halfDuration"] = 45, ["numberOfHalfs"] = 2, ["tiebreaker"] = "Extra Time & Penalties"
            },
            ["rosters"] = new JObject { ["home"] = home, ["away"] = away },
            ["stats"] = stats,
            ["runtimeSnapshot"] = new JObject
            {
                ["stats"] = stats.DeepClone(),
                ["clock"] = new JObject { ["currentHalf"] = 2, ["currentHalfRegulationSeconds"] = 123f }
            }
        };
    }
}
