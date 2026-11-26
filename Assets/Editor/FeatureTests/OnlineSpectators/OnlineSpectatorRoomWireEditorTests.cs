using System.Text;
using CounterAttack.Online;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OnlineSpectatorRoomWireEditorTests
{
    private static MatchManager.GameSettings Settings() => new() { homeTeamName = "Home club", awayTeamName = "Away club" };

    private static OnlineGroundPassState State() => new()
    {
        matchId = "match-spectator",
        catalogId = OnlineFullRoomBootstrap.Catalog
    };

    [Test]
    public void PublishesPublicMatchStateAndVisibleRoster()
    {
        var state = State();
        state.half = 2;
        state.regulationSeconds = 3600;
        state.isSharedPaused = true;
        state.matchComplete = true;
        state.ballOnBoard = true;
        state.ballX = 3;
        state.ballZ = -5;
        state.homeStats = new MatchManager.TeamStats { totalGoals = 1, totalShots = 3 };
        state.homeScorers = new[] { new MatchManager.GoalEvent { scorer = "Striker", minute = 10 } };
        state.tokens = new[]
        {
            new OnlineGroundPassTokenState
            {
                id = "home:9", onBoard = true, visible = true, x = 4, z = 5,
                runtime = new RoomTokenSnapshot { teamSide = "Home", jerseyNumber = 9, playerName = "Striker", shooting = 5 },
                playerStats = new MatchManager.PlayerStats { goals = 1 }
            }
        };

        var json = Encode(state);
        Assert.That((int)json["version"], Is.EqualTo(OnlineSpectatorRoomWire.Version));
        Assert.That((long)json["sequence"], Is.EqualTo(7));
        Assert.That((string)json["matchId"], Is.EqualTo(state.matchId));
        Assert.That((bool)json["paused"], Is.True);
        Assert.That((bool)json["matchComplete"], Is.True);
        Assert.That((int)json["clock"]["half"], Is.EqualTo(2));
        Assert.That((float)json["clock"]["regulationSeconds"], Is.EqualTo(3600));
        Assert.That((int)json["ball"]["z"], Is.EqualTo(-5));
        Assert.That((string)json["home"]["name"], Is.EqualTo("Home club"));
        Assert.That((int)json["home"]["stats"]["totalShots"], Is.EqualTo(3));
        Assert.That((string)json["home"]["scorers"][0]["scorer"], Is.EqualTo("Striker"));
        Assert.That((int)json["players"][0]["position"]["x"], Is.EqualTo(4));
        Assert.That((int)json["players"][0]["attributes"]["shooting"], Is.EqualTo(5));
        Assert.That((int)json["players"][0]["stats"]["goals"], Is.EqualTo(1));
    }

    [Test]
    public void EncodedSnapshotOmitsManagerChoicesPrivateKeysAndUnrevealedRolls()
    {
        const string privateValue = "manager-private";
        var state = State();
        state.decisionId = privateValue;
        state.legalActions = new[] { privateValue };
        state.selectedPlayer = privateValue;
        state.roomSettings = new[] { privateValue };
        state.homeScorers = new[] { new MatchManager.GoalEvent { scorer = "Public", scorerKey = privateValue } };
        state.tokens = new[]
        {
            new OnlineGroundPassTokenState
            {
                id = "home:12", onBoard = true, visible = false, x = 789,
                runtime = new RoomTokenSnapshot { tokenKey = privateValue, playerName = "Bench" }
            }
        };
        state.shootout = new OnlineShootoutSnapshot { selecting = true, homeOrder = new[] { privateValue } };
        state.rolls = new[] { new OnlineRollPublication { actorName = privateValue, roll = 6 } };
        state.rollPanel = new OnlineRollPanelFrame { labels = new[] { privateValue }, dice = new[] { 6, 4 }, diceAnimating = new[] { true, false } };

        var json = Encode(state);
        Assert.That(json.ToString(), Does.Not.Contain(privateValue));
        Assert.That(json["players"][0]["position"], Is.Null);
        Assert.That(json["shootout"], Is.Null);
        Assert.That((bool)json["dice"]["faces"][0]["rolling"], Is.True);
        Assert.That((int)json["dice"]["faces"][0]["value"], Is.Zero);
        Assert.That((int)json["dice"]["faces"][1]["value"], Is.EqualTo(4));
        state.rollPanel.diceAnimating = null;
        Assert.That(Encode(state)["dice"], Is.Null);
    }

    [Test]
    public void CoinTossProjectsOnlyAlreadyRevealedFaces()
    {
        var state = State();
        state.coin = new OnlineCoinTossSnapshot
        {
            purpose = nameof(CoinTossPurpose.FirstHalf), stage = "away", homeRoll = 4, rollSeconds = 3
        };
        var coin = Encode(state)["coin"];
        Assert.That((int)coin["homeRoll"], Is.EqualTo(4));
        Assert.That((int)coin["awayRoll"], Is.Zero);
        Assert.That(coin["rollSeconds"], Is.Null);

        state.coin.awayRoll = 6;
        Assert.That(Encode(state)["coin"], Is.Null, "An unrevealed face cannot appear in an away-turn snapshot");
    }

    [Test]
    public void MotionPublishesOnlyVisibleOnPitchActorsDuringCommittedMovement()
    {
        var state = State();
        state.tokens = new[]
        {
            new OnlineGroundPassTokenState { id = "home:9", onBoard = true, visible = true },
            new OnlineGroundPassTokenState { id = "home:12", onBoard = false, visible = true },
            new OnlineGroundPassTokenState { id = "away:2", onBoard = true, visible = false }
        };
        state.roomPose = new OnlineGroundPassPresentation
        {
            matchId = "private-manager-id",
            ballX = 7, ballY = 2, ballZ = 3,
            players = new[]
            {
                new OnlinePlayerPresentation { id = "home:9", x = 2, y = 1, z = 3 },
                new OnlinePlayerPresentation { id = "home:12", x = 123 },
                new OnlinePlayerPresentation { id = "away:2", x = 456 }
            }
        };
        Assert.That(Encode(state)["pose"], Is.Null);
        state.playerMoving = true;
        var pose = Encode(state)["pose"];
        Assert.That(pose["ball"], Is.Null);
        Assert.That(((JArray)pose["players"]).Count, Is.EqualTo(1));
        Assert.That((string)pose["players"][0]["id"], Is.EqualTo("home:9"));
        Assert.That(pose.ToString(), Does.Not.Contain("private-manager-id"));
        state.ballOnBoard = state.ballMoving = true;
        Assert.That((float)Encode(state)["pose"]["ball"]["y"], Is.EqualTo(2));
    }

    [Test]
    public void RejectsOtherCatalogsAndOversizedPublications()
    {
        var state = State();
        state.catalogId = "proof-only";
        Assert.That(OnlineSpectatorRoomWire.TryEncode(state, Settings(), 1, out var bytes), Is.False);
        Assert.That(bytes, Is.Null);

        state.catalogId = OnlineFullRoomBootstrap.Catalog;
        var settings = Settings();
        settings.homeTeamName = new string('x', OnlineSpectatorRoomWire.MaxBytes);
        Assert.That(OnlineSpectatorRoomWire.TryEncode(state, settings, 1, out bytes), Is.False);
        Assert.That(bytes, Is.Null);
    }

    private static JObject Encode(OnlineGroundPassState state)
    {
        Assert.That(OnlineSpectatorRoomWire.TryEncode(state, Settings(), 7, out var payload), Is.True);
        return JObject.Parse(Encoding.UTF8.GetString(payload));
    }
}
