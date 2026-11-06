using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator NativeFreeKickState(MatchManager.GameState state)
    {
        yield return Until(() => match.currentState == state, $"actual free-kick setup reaches {state}");
        Assert.That(match.freeKickManager.isActivated && match.freeKickManager.isWaitingForSetupPhase, Is.True);
        Assert.That(match.freeKickManager.isWaitingForFinalKickerSelection || match.freeKickManager.isWaitingForExecution, Is.False);
    }
    private void AssertNativeFreeKickOppositeIgnored(PlayerToken token)
    {
        var free = match.freeKickManager; var state = match.currentState; var selected = free.selectedToken;
        var target = free.targetHex; string instructions = free.GetInstructions();
        input.TryExecuteSyntheticHexClick(token.currentHex, "Legacy migration", out _);
        Assert.That(match.currentState, Is.EqualTo(state)); Assert.That(free.selectedToken, Is.SameAs(selected));
        Assert.That(free.targetHex, Is.SameAs(target)); Assert.That(free.GetInstructions(), Is.EqualTo(instructions));
    }
    private IEnumerator NativeFreeKickMove(PlayerToken token, HexCell destination)
    {
        input.TryExecuteSyntheticHexClick(token.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(destination, "Legacy migration", out _);
        yield return Until(() => token.currentHex == destination && !movement.isPlayerMoving && !match.freeKickManager.IsResolvingSetup, $"actual free-kick setup moves {token.playerName} to {destination.coordinates}");
    }
    public IEnumerator RunNativeFreeKickSheetPreparation()
    {
        var p = LoadLegacyRoster(); var yaneva = p["Yaneva"]; var free = match.freeKickManager;
        var cameraPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(FrameworkAuthoredCameraAsset.PrefabPath);
        Assert.That(cameraPrefab, Is.Not.Null, "Export the production-authored test camera in Edit Mode before executing the shot cinematic.");
        UnityEngine.Object.Instantiate(cameraPrefab, root.transform, false);
        var throwIn = CreateLegacyThrowInManager(); AttachAuthoredPanels();
        EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose); EnableForAuthoredPresentation(match.shotManager);
        Subscribe(free); yield return AttachLegacyGoalFlow();
        var oob = Add<OutOfBoundsManager>(); oob.hexGrid = grid; oob.ball = ball; oob.freeKickManager = free;
        oob.throwInManager = throwIn; oob.outOfBoundsPushManager = throwIn.outOfBoundsPushManager;
        oob.playerTokenManager = match.playerTokenManager; oob.finalThirdManager = match.finalThirdManager;
        loose.outOfBoundsManager = oob; match.shotManager.outOfBoundsManager = oob;
        LegacyKey("P"); ClickPass(Hex(8)); ClickPass(Hex(8));
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(8), "free-kick sheet kickoff pass reaches empty space");
        yield return ForfeitLegacyFinalThirds();
        // Passing to space already starts committed movement. Preserve the old
        // extra M input, but assert the resulting phase rather than acceptance of
        // a redundant activation key.
        input.TryExecuteSyntheticKeyPress("M", "Legacy migration", out _);
        yield return Until(() => movement.isActivated && movement.isMovementPhaseAttack, "pass to space commits attacking movement before pickup");
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(yaneva)); LegacyKey("V");
        yield return Until(() => yaneva.currentHex == Hex(8) && !movement.isPlayerMoving, "free-kick sheet dribbler picks up the kickoff pass");
        LegacyKey("X"); yield return NativeFreeKickMove(p["Kalla"], Hex(11, 3)); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "free-kick sheet preparation advances to defense");
        yield return NativeFreeKickMove(p["Paterson"], Hex(9));
        yield return Until(() => movement.isWaitingForTackleDecision, "Paterson can tackle Yaneva before the free-kick setup");
        LegacyKey("T"); yield return Until(() => movement.isWaitingForTackleRoll, "real tackle request opens rolls");
        movement.PerformTackleDiceRoll(true, 1); yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, 6);
        yield return Until(() => movement.isWaitingForYellowCardRoll, "natural-one tackle foul opens leniency");
        movement.PerformLeniencyTest(6); yield return Until(() => movement.isWaitingForInjuryRoll, "leniency opens injury");
        movement.PerformInjuryTest(2); yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForFoulDecision, "completed booking and injury checks offer the restart decision");
        LegacyKey("F"); yield return Until(() => match.finalThirdManager.isActivated, "taking the foul offers Final Thirds before free-kick selection");
        Assert.That(free.isWaitingForKickerSelection, Is.False); yield return ForfeitLegacyFinalThirds();
        yield return Until(() => free.isActivated && free.isWaitingForKickerSelection, "free-kick taker selection begins after Final Thirds");
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.FreeKickKickerSelect));
        var noruega = p.Values.Single(t => t.playerName.Contains("Noruega"));
        input.TryExecuteSyntheticHexClick(noruega.currentHex, "Legacy migration", out _);
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickAttGK);
        AssertNativeFreeKickOppositeIgnored(p["Kuzmic"]); yield return NativeFreeKickMove(p["Aziz"], Hex(-14));
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickDefGK1);
        AssertNativeFreeKickOppositeIgnored(p["Aziz"]); LegacyKey("X");
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickAtt1);
        AssertNativeFreeKickOppositeIgnored(p["Marell"]);
        yield return NativeFreeKickMove(p["Jansen"], Hex(14, 2)); yield return NativeFreeKickMove(p["Toothnail"], Hex(14, -1));
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickDef1);
        AssertNativeFreeKickOppositeIgnored(p["Murphy"]);
        yield return NativeFreeKickMove(p["Marell"], Hex(14, 3)); yield return NativeFreeKickMove(p["Vladoiu"], Hex(14, -2));
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickAtt2);
        AssertNativeFreeKickOppositeIgnored(p["Poulsen"]);
        yield return NativeFreeKickMove(p["Murphy"], Hex(14, -3)); yield return NativeFreeKickMove(p["Cafferata"], Hex(14, -6));
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickDef2);
        AssertNativeFreeKickOppositeIgnored(p["Pavlovic"]);
        yield return NativeFreeKickMove(p["Poulsen"], Hex(14, 6)); LegacyKey("X");
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickAtt3);
        AssertNativeFreeKickOppositeIgnored(p["Delgado"]);
        yield return NativeFreeKickMove(p["Pavlovic"], Hex(13, -2)); LegacyKey("X");
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickDef3);
        AssertNativeFreeKickOppositeIgnored(p["Pavlovic"]);
        yield return NativeFreeKickMove(p["Delgado"], Hex(1, -8));
        Assert.That(p["Paterson"].currentHex, Is.SameAs(Hex(9))); Assert.That(HexGridUtils.GetHexStepDistance(ball.GetCurrentHex(), p["Paterson"].currentHex), Is.LessThanOrEqualTo(2));
        LegacyKey("X"); Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.FreeKickDef3));
        Assert.That(free.isWaitingForSetupPhase, Is.True); Assert.That(p["Paterson"].currentHex, Is.SameAs(Hex(9)));
        yield return NativeFreeKickMove(p["Paterson"], Hex(-1, 7));
        yield return NativeFreeKickState(MatchManager.GameState.FreeKickDefGK2);
        AssertNativeFreeKickOppositeIgnored(p["Aziz"]); yield return NativeFreeKickMove(p["Kuzmic"], Hex(18));
        yield return Until(() => match.currentState == MatchManager.GameState.FreeKickDefineKicker && free.isWaitingForFinalKickerSelection, "free-kick setup completes before final kicker selection");
        Assert.That(free.isActivated && !free.isWaitingForSetupPhase && !free.isWaitingForExecution, Is.True);
        var selected = free.selectedKicker; string instructions = free.GetInstructions();
        input.TryExecuteSyntheticHexClick(p["Paterson"].currentHex, "Legacy migration", out _);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.FreeKickDefineKicker));
        Assert.That(free.isActivated && free.isWaitingForFinalKickerSelection && !free.isWaitingForSetupPhase && !free.isWaitingForExecution, Is.True);
        Assert.That(free.selectedKicker, Is.SameAs(selected)); Assert.That(free.GetInstructions(), Is.EqualTo(instructions));
        input.TryExecuteSyntheticHexClick(Hex(7, -1), "Legacy migration", out _);
        yield return Until(() => match.currentState == MatchManager.GameState.FreeKickExecution && free.isWaitingForExecution, "final legal kicker selection opens actual free-kick execution");
        Assert.That(free.isActivated && !free.isWaitingForSetupPhase && !free.isWaitingForFinalKickerSelection, Is.True);
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(8))); Assert.That(Hex(8).CanShootFrom, Is.True);
        Assert.That(match.shotManager.isAvailable, Is.True);
        Assert.That(free.GetInstructions(), Is.EqualTo("FK: Standard [P]ass, High Pass [C], [L]ong Ball or [S]hot"));
        Assert.That(longBall.GetInstructions(), Is.Empty);
    }
}
