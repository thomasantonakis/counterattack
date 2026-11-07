using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOpeningFromSeedSource(bool roomFixture, int side)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        var fileName = roomFixture
            ? "gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json"
            : "new-match.json";
        // Exercise the same source configuration as loading, with no file reads/writes.
        Invoke(match, "ConfigureLoadedGameSource", System.IO.Path.Combine("/fixture-input", fileName));
        Assert.That(match.gameData.runtimeSnapshot, Is.Null, "the Room seed has no snapshot");
        Subscribe(ground);
        match.StartMatch();
        Assert.That(ground.isAvailable, Is.True);
        Assert.That(high.isAvailable, Is.EqualTo(roomFixture));
        Assert.That(longBall.isAvailable, Is.EqualTo(roomFixture));
        match.RefreshAvailableActionsForCurrentState();
        Assert.That(high.isAvailable && longBall.isAvailable, Is.EqualTo(roomFixture), "refresh preserves source-specific choices");
        LegacyKey("P");
        Assert.That(ground.isAwaitingTargetSelection, Is.True, "P opens target previews");
        var target = Hex(-5 * side);
        Assert.That(ground.GetLegalGroundPassTargetsForDecisionPreview(), Does.Contain(target));
        Invoke(ground, "OnHoverReceived", null, target);
        Assert.That(grid.highlightedHexes.Count, Is.GreaterThan(0), "legal hover produces a path preview after P");
        yield break;
    }

    public IEnumerator RunOpeningGroundPassOnly(int side, string mode)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.gameData.gameSettings.gameMode = mode;
        Subscribe(match); Subscribe(ground);
        match.StartMatch();
        Assert.That(ground.isAvailable, Is.True);
        Assert.That(high.isAvailable || longBall.isAvailable || movement.isAvailable || ftp.isAvailable, Is.False);
        Assert.That(input.TryExecuteSyntheticKeyPress("C", "Kickoff test", out _), Is.False);
        Assert.That(input.TryExecuteSyntheticKeyPress("L", "Kickoff test", out _), Is.False);
        Assert.That(high.isActivated || longBall.isActivated, Is.False);
        LegacyKey("P");
        Assert.That(ground.isActivated && ground.isKickoffPass, Is.True);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.StandardPass));
        yield break;
    }

    public IEnumerator RunKickoffOwnHalfPass(int side, int difficulty, int restartHalf)
    {
        Board(side);
        match.currentHalf = System.Math.Max(1, restartHalf);
        if (restartHalf == 2 || restartHalf == 4)
        {
            match.ChangePossession();
            side = -side;
        }
        var taker = match.teamInAttack == MatchManager.TeamInAttack.Home ? attacker : defender;
        var opponent = taker == attacker ? defender : attacker;
        Place(taker, Hex(0)); ball.PlaceAtCell(taker.currentHex);
        Place(opponent, Hex(-6 * side, 1));
        match.difficulty_level = difficulty;
        Subscribe(match); Subscribe(ground); Subscribe(match.finalThirdManager);
        if (restartHalf != 0)
        {
            var kickoff = Add<KickoffManager>(); match.kickoffManager = kickoff; input.kickoffManager = kickoff;
            kickoff.ball = ball; kickoff.hexGrid = grid; kickoff.playerTokenManager = match.playerTokenManager;
            kickoff.freeKickManager = match.freeKickManager; kickoff.groundBallManager = ground;
            kickoff.movementPhaseManager = movement; kickoff.headerManager = match.shotManager.headerManager;
            Subscribe(kickoff);
            match.currentState = MatchManager.GameState.PostGoalKickOffSetup;
            // Same public taker-selection input used after goals and at half starts.
            kickoff.StartPostGoalKickoffSetupPhase();
            LegacyKey("Return"); LegacyKey("Return");
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickOffTakerSelection));
            input.TryExecuteSyntheticTokenClick(taker, "Kickoff test", out _);
            yield return Until(() => ground.isActivated, "restart taker starts committed ground pass");
        }
        else
        {
            match.StartMatch(); LegacyKey("P");
        }
        var target = Hex(-14 * side);
        Assert.That(ground.GetInstructions(), Does.Contain("no distance limit"));
        Assert.That(ground.GetLegalGroundPassTargetsForDecisionPreview(), Does.Contain(target), "AI and human previews include distant own-half targets");
        ClickPass(target);
        if (difficulty != 3) ClickPass(target);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == target, "uncapped own-half kickoff finishes without interception rolls");
        Assert.That(ground.isKickoffPass, Is.False, "kickoff exemption ends with the pass");
        ground.ActivateGroundBall();
        Assert.That(ground.GetLegalGroundPassTargetsForDecisionPreview(), Has.No.Member(Hex(0)), "ordinary passes retain the distance cap");
    }

    public IEnumerator RunKickoffNormalTarget(int side, int difficulty, bool halfway)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.difficulty_level = difficulty;
        Subscribe(ground); Subscribe(match.finalThirdManager);
        match.StartMatch(); LegacyKey("P");
        var target = halfway ? Hex(0, 5) : Hex(5 * side);
        var farTarget = halfway ? Hex(0, 13) : Hex(14 * side);
        Assert.That(ground.IsGroundPassTargetLegalForDecision(target), Is.True, "normal-range opponent-half and x=0 targets are legal");
        Assert.That(ground.GetLegalGroundPassTargetsForDecisionPreview(), Does.Contain(target));
        Assert.That(ground.ValidateGroundPassPath(farTarget, 11).FailureReason, Is.EqualTo(PassValidationFailureReason.OutOfRange));
        Assert.That(ground.IsGroundPassTargetLegalForDecision(farTarget), Is.False, "ordinary 11-hex limit applies outside own half");

        Place(defender, target);
        Assert.That(ground.ValidateGroundPassPath(target, 11).FailureReason, Is.EqualTo(PassValidationFailureReason.BlockedByDefender));
        Assert.That(ground.IsGroundPassTargetLegalForDecision(target), Is.False);
        Place(defender, halfway ? Hex(1, 3) : Hex(3 * side, 1));
        var normal = GroundPassCommon.ValidateStandardPassPath(grid, ball, target, 11, logFailures: false);
        Assert.That(normal.IsValid && normal.IsDangerous, Is.True, "fixture places an adjacent normal interception threat");
        var kickoff = ground.ValidateGroundPassPath(target, 11);
        Assert.That(kickoff.IsValid, Is.EqualTo(normal.IsValid));
        Assert.That(kickoff.IsDangerous, Is.EqualTo(normal.IsDangerous), "opponent-half and halfway targets retain normal interceptions");
        Place(defender, Hex(0, 10));
        ClickPass(target);
        if (difficulty != 3) ClickPass(target);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == target, "normal kickoff target completes via real pass input");
        Assert.That(ground.isKickoffPass, Is.False);
    }

    public IEnumerator RunKickoffSnapshotOptions(bool groundOnly)
    {
        Board(1); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        // Deserialize old shape without the new field to preserve existing Room data.
        var snapshot = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(
            "{\"currentState\":\"KickoffBlown\",\"teamInAttack\":\"Home\"}");
        if (groundOnly)
        {
            match.StartMatch();
            snapshot = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(
                Newtonsoft.Json.JsonConvert.SerializeObject(match.CreateRuntimeSnapshot()));
        }
        Invoke(match, "RestoreSnapshotStateFields", snapshot);
        match.RefreshAvailableActionsForCurrentState();
        Assert.That(ground.isAvailable, Is.True);
        Assert.That(high.isAvailable, Is.EqualTo(!groundOnly));
        Assert.That(longBall.isAvailable, Is.EqualTo(!groundOnly));
        yield break;
    }
}
