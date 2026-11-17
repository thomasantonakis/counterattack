using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[Category("PostMatchPitch")]
public sealed class PostMatchPitchEditorTests
{
    private static RoomHexCoordinates Hex(int x, int z = 0) => new() { x = x, z = z };
    private static GameplayTokenStateSummary Token(string key, int x, int z = 0, string status = "playing") => new() { tokenKey = key, hex = Hex(x, z), status = status };
    private static GameplayEvent Boundary(int sequence, int half, params GameplayTokenStateSummary[] tokens) => new()
    {
        sequenceNumber = sequence, currentHalf = half, kind = "snapshot.action_boundary", teamInAttack = "Home", attackHasPossession = true,
        snapshot = new GameplaySnapshotSummary { tokens = tokens.ToList(), currentHalf = half, currentState = "StandardPassCompleted" }
    };
    private static MatchManager.GameData Data(params GameplayEvent[] events) => new()
    {
        openingSetup = new MatchOpeningSetup { homeAttacksLeftToRight = true },
        events = events.ToList(),
    };

    [Test]
    public void StationaryPlayersWithoutBallTouchesAreIncludedAndProjectionDoesNotMutateEvents()
    {
        var data = Data(Boundary(1, 1, Token("Home:1", -10), Token("Away:1", 10)), Boundary(2, 1, Token("Home:1", -10), Token("Away:1", 10)));
        data.rosters.home["1"] = new MatchManager.RosterPlayer { name = "Untouched keeper" };
        string before = JsonConvert.SerializeObject(data);
        var players = PostMatchPitchData.Build(data);
        Assert.That(players.Count, Is.EqualTo(2));
        var keeper = players.Single(p => p.Key == "Home:1");
        Assert.That(keeper.Name, Is.EqualTo("Untouched keeper"));
        Assert.That(keeper.Positions.Count, Is.EqualTo(2));
        Assert.That(keeper.Movement, Is.Empty);
        Assert.That(keeper.Average.x, Is.EqualTo(8f / 36).Within(.0001));
        Assert.That(JsonConvert.SerializeObject(data), Is.EqualTo(before));
    }

    [Test]
    public void HalfChangesNormalizeCartesianPositionsIncludingStaggeredRows()
    {
        var data = Data(Boundary(1, 1, Token("Home:9", -9, 0)), Boundary(2, 2, Token("Home:9", 9, -1)));
        var player = PostMatchPitchData.Build(data).Single();
        Assert.That(player.Positions[0], Is.EqualTo(player.Positions[1]).Using(Vector2Comparer.Instance));
        Assert.That(player.Average.x, Is.EqualTo(.25f).Within(.0001));
        Assert.That(PostMatchPitchData.Build(data, half: PostMatchPitchData.Half.First).Single().Positions.Count, Is.EqualTo(1));
        Assert.That(PostMatchPitchData.Build(data, half: PostMatchPitchData.Half.Second).Single().Positions.Count, Is.EqualTo(1));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ExtraTimeUsesItsOwnOpeningDirection(bool homeRight)
    {
        var data = Data(Boundary(1, 3, Token("Home:1", -10)), Boundary(2, 4, Token("Home:1", 10)));
        data.extraTimeOpeningSetup = new MatchOpeningSetup { homeAttacksLeftToRight = homeRight };
        var player = PostMatchPitchData.Build(data, half: PostMatchPitchData.Half.ExtraTime).Single();
        Assert.That(player.Positions[0].x, Is.EqualTo(player.Positions[1].x).Within(.0001));
        Assert.That(player.Average.x, Is.EqualTo(homeRight ? 8f / 36 : 28f / 36).Within(.0001));
    }

    [Test]
    public void TeamAndPossessionFiltersExcludeUncontrolledBallStates()
    {
        var first = Boundary(1, 1, Token("Home:1", -10), Token("Away:1", 10));
        var loose = Boundary(2, 1, Token("Home:1", -8), Token("Away:1", 8)); loose.attackHasPossession = false;
        var away = Boundary(3, 1, Token("Home:1", -6), Token("Away:1", 6)); away.teamInAttack = "Away";
        var data = Data(first, loose, away);
        Assert.That(PostMatchPitchData.Build(data, PostMatchLeaderboard.Team.Home).Single().Positions.Count, Is.EqualTo(3));
        Assert.That(PostMatchPitchData.Build(data, PostMatchLeaderboard.Team.Home, possession: PostMatchPitchData.Possession.InPossession).Single().Average.x, Is.EqualTo(8f / 36).Within(.0001));
        Assert.That(PostMatchPitchData.Build(data, PostMatchLeaderboard.Team.Home, possession: PostMatchPitchData.Possession.OutOfPossession).Single().Average.x, Is.EqualTo(12f / 36).Within(.0001));
    }

    [Test]
    public void SetupBenchSentOffAndOffPitchTokensDoNotPolluteTheMap()
    {
        var setup = Boundary(1, 1, Token("Home:1", 0)); setup.snapshot.currentState = "PostGoalKickOffSetup";
        var initial = Boundary(2, 1, Token("Home:1", 0)); initial.kind = "snapshot.initial_state";
        var data = Data(setup, initial, Boundary(3, 1, Token("Home:1", -10), Token("Home:12", 0, status: "bench"),
            Token("Home:2", 0, status: "playing,sent_off"), Token("Home:3", 30), Token("Home:4", 0, status: "playing,subbed_off")));
        Assert.That(PostMatchPitchData.Build(data).Single().Key, Is.EqualTo("Home:1"));
        Assert.That(PostMatchPitchData.Build(data).Single().Positions.Count, Is.EqualTo(1));
    }

    [Test]
    public void MovementUsesCommittedDistancePathsAndDeduplicatesSequenceIds()
    {
        var move = new GameplayEvent { sequenceNumber = 2, currentHalf = 1, kind = "token.move", actorTokenKey = "Home:1",
            movementPath = new GameplayMovementPath { countedForDistance = true, hexes = new List<RoomHexCoordinates> { Hex(-10), Hex(-9), Hex(-8) } } };
        var reset = new GameplayEvent { sequenceNumber = 3, currentHalf = 1, kind = "token.move", actorTokenKey = "Home:1",
            movementPath = new GameplayMovementPath { countedForDistance = false, hexes = new List<RoomHexCoordinates> { Hex(-8), Hex(0) } } };
        var player = PostMatchPitchData.Build(Data(Boundary(1, 1, Token("Home:1", -10)), move, move, reset)).Single();
        Assert.That(player.Movement.Count, Is.EqualTo(2));
        Assert.That(player.Movement[0].x, Is.EqualTo(.25f).Within(.0001));
    }

    [Test]
    public void MissingLegacyDataNeverCreatesAnAveragePosition()
    {
        Assert.That(PostMatchPitchData.Build(null), Is.Empty);
        Assert.That(PostMatchPitchData.Build(new MatchManager.GameData()), Is.Empty);
        var move = new GameplayEvent { kind = "token.move", actorTokenKey = "Home:1", movementPath = new GameplayMovementPath { countedForDistance = true, hexes = new List<RoomHexCoordinates> { Hex(0), Hex(1) } } };
        Assert.That(PostMatchPitchData.Build(Data(move)), Is.Empty);
    }

    [Test]
    public void AuthoredPitchSupportsHoverPinFiltersAndEmptyStateWithoutCreatingObjects()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath);
        var root = Object.Instantiate(prefab); root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            root.SetActive(true);
            var report = root.GetComponent<PostMatchReportController>();
            var data = Data(Boundary(1, 1, Token("Home:1", -10), Token("Away:1", 10)));
            data.rosters.home["1"] = new MatchManager.RosterPlayer { name = "Home keeper" };
            int count = root.GetComponentsInChildren<Transform>(true).Length;
            report.ShowReport(data, true);
            root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "PitchTab").onClick.Invoke();
            Assert.That(report.ShowingPitch, Is.True);
            Assert.That(report.ShowingPlayers, Is.False);
            var markers = root.GetComponentsInChildren<PostMatchPitchMarker>(true);
            Assert.That(markers.Length, Is.EqualTo(36));
            Assert.That(markers.Count(m => m.gameObject.activeSelf), Is.EqualTo(2));
            var detail = root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "PitchPlayerDetail");
            var marker = markers.First(m => m.gameObject.activeSelf && m.number.text == "1");
            marker.OnPointerEnter(new PointerEventData(null));
            Assert.That(detail.text, Does.Contain("position samples"));
            marker.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            marker.OnPointerExit(new PointerEventData(null));
            Assert.That(detail.text, Does.Contain("Pinned"));
            root.GetComponentsInChildren<PostMatchDropdown>(true).Single(d => d.name == "HeatmapDropdown").value = 1;
            Assert.That(detail.text, Does.Contain("0 recorded movement steps"));
            var periods = root.GetComponentsInChildren<PostMatchDropdown>(true).Single(d => d.name == "PeriodDropdown");
            // The fixture has only a first half: a different, applicable possession view is empty.
            Assert.That(periods.options.Select(o => o.text), Is.EqualTo(new[] { "Full game", "1st half" }));
            root.GetComponentsInChildren<PostMatchDropdown>(true).Single(d => d.name == "PossessionDropdown").value = 2;
            Assert.That(markers.Count(m => m.gameObject.activeSelf), Is.EqualTo(1));
            report.SelectTeam(PostMatchLeaderboard.Team.Home);
            Assert.That(markers.All(m => !m.gameObject.activeSelf), Is.True);
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "PitchStatus").text, Does.Contain("No recorded"));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(1, new[] { "Full game", "1st half" })]
    [TestCase(2, new[] { "Full game", "1st half", "2nd half" })]
    [TestCase(3, new[] { "Full game", "1st half", "2nd half", "1st half of extra time", "Regulation", "Extra time" })]
    [TestCase(4, new[] { "Full game", "1st half", "2nd half", "1st half of extra time", "2nd half of extra time", "Regulation", "Extra time" })]
    public void PeriodOptionsOnlyIncludeApplicableMatchPeriods(int latestHalf, string[] expected)
    {
        var data = Data(Boundary(1, latestHalf, Token("Home:1", -10)));
        Assert.That(PostMatchPitchData.AvailablePeriods(data).Select(PostMatchPitchData.PeriodLabel), Is.EqualTo(expected));
    }

    [TestCase(PostMatchPitchData.Half.FirstExtraTime, 1)]
    [TestCase(PostMatchPitchData.Half.SecondExtraTime, 1)]
    [TestCase(PostMatchPitchData.Half.Regulation, 2)]
    [TestCase(PostMatchPitchData.Half.ExtraTime, 2)]
    public void RegulationAndSeparateExtraTimeHalfFiltersSelectTheCorrectSamples(PostMatchPitchData.Half selected, int samples)
    {
        var data = Data(Enumerable.Range(1, 4).Select(i => Boundary(i, i, Token("Home:1", -10))).ToArray());
        Assert.That(PostMatchPitchData.Build(data, half: selected).Single().Positions.Count, Is.EqualTo(samples));
    }

    [Test]
    public void FiltersShareOneRowAndEachMenuFitsItsOwnOptionsWithoutScrolling()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath));
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var report = root.GetComponent<PostMatchReportController>();
            report.ShowReport(Data(Boundary(1, 4, Token("Home:1", -10))), true);
            report.ShowPitch();
            var dropdowns = root.GetComponentsInChildren<PostMatchDropdown>(true);
            Assert.That(dropdowns.Length, Is.EqualTo(3));
            Assert.That(dropdowns.Select(d => d.transform.parent.parent).Distinct().Count(), Is.EqualTo(1));
            foreach (var dropdown in dropdowns)
            {
                Assert.That(dropdown.template.rect.height, Is.EqualTo(dropdown.options.Count * PostMatchDropdown.RowHeight + PostMatchDropdown.Padding));
                Assert.That(dropdown.template.GetComponent<UnityEngine.UI.ScrollRect>().vertical, Is.False);
            }
            Assert.That(dropdowns.Single(d => d.name == "PossessionDropdown").options.Select(o => o.text), Is.EqualTo(new[] { "All game", "Attacking", "Defending" }));
            Assert.That(dropdowns.Single(d => d.name == "HeatmapDropdown").options.Select(o => o.text), Is.EqualTo(new[] { "Position", "Movement" }));
            var content = root.transform.Find(PostMatchReportEditorTools.ContentPath).GetComponent<RectTransform>();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var map = root.GetComponentInChildren<PostMatchPitchGraphic>(true).rectTransform;
            Assert.That(map.rect.width / map.rect.height, Is.EqualTo(PostMatchPitchData.PitchAspectRatio).Within(.005));
        }
        finally { Object.DestroyImmediate(root); }
    }

    private sealed class Vector2Comparer : IEqualityComparer<Vector2>
    {
        public static readonly Vector2Comparer Instance = new();
        public bool Equals(Vector2 x, Vector2 y) => Vector2.Distance(x, y) < .0001f;
        public int GetHashCode(Vector2 obj) => obj.GetHashCode();
    }
}
