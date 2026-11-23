using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunMovementOpeningSave(int side, bool preview, bool expired)
    {
        StartAutomaticMovementOpening(side);
        Assert.That(match.CanCreateRuntimeSnapshot(out string reason), Is.True, reason);

        var origin = attacker.currentHex;
        movement.headerManager.defenderWillJump.Add(defender);
        movement.stunnedTokens.Add(awayGK);
        HexCell previewDestination = null;
        if (preview)
        {
            Invoke(movement, "OnClickReceived", attacker, origin);
            Assert.That(movement.selectedToken, Is.SameAs(attacker));
            previewDestination = grid.highlightedHexes.First(hex => hex != ball.GetCurrentHex());
            Assert.That(match.CanCreateRuntimeSnapshot(out reason), Is.True, reason);
        }

        match.RefreshDecisionTiming();
        var originalDecision = match.DecisionTiming.Current;
        Assert.That(originalDecision.kind, Is.EqualTo(DecisionKind.AttackingMovement));
        match.AdvanceDecisionTiming(expired ? originalDecision.remaining : 4f);
        float remaining = originalDecision.remaining;
        float budget = originalDecision.budget;
        const float regulationTime = 12f;
        Set(match, "currentHalfRegulationSeconds", regulationTime);
        match.SetPauseMenuOpen(true);
        match.AdvanceDecisionTiming(5f);
        Assert.That(originalDecision.remaining, Is.EqualTo(remaining));
        int actions = match.totalCompletedActions;
        var ballHex = ball.GetCurrentHex();

        var previousApp = ApplicationManager.Instance;
        var appProperty = typeof(ApplicationManager).GetProperty(nameof(ApplicationManager.Instance));
        bool hadPreference = PlayerPrefs.HasKey("currentGameSettings");
        string preference = PlayerPrefs.GetString("currentGameSettings");
        var appHost = Child("Isolated movement save context");
        var app = appHost.AddComponent<ApplicationManager>();
        appProperty.SetValue(null, app);
        string directory = Path.GetFullPath(Path.Combine("TestResults/GameplayRegression", "MovementOpening-" + Guid.NewGuid().ToString("N")));
        try
        {
            string path = Path.Combine(directory, "match.json");
            app.SetActiveSaveFilePath(path);
            Assert.That(RoomSaveService.SaveToPath(match, path, false, out _, out reason), Is.True, reason);
            string originalSave = File.ReadAllText(path);
            Assert.That(RoomSaveService.CreateLoadedRunCopy(path, out string run, out reason), Is.True, reason);
            Assert.That(File.ReadAllText(path), Is.EqualTo(originalSave));
            var loaded = JsonConvert.DeserializeObject<MatchManager.GameData>(File.ReadAllText(run));
            match.SetPauseMenuOpen(false);
            match.AdvanceDecisionTiming(1000f);
            Set(match, "currentHalfRegulationSeconds", regulationTime + 12f);
            movement.ForfeitTeamMovementPhase();
            movement.ClearSetPieceTransientState();
            int commitments = match.gameData.events.Count(entry => entry.kind == "action.committed");

            Invoke(match, "ApplyRuntimeSnapshot", loaded.runtimeSnapshot);
            match.RefreshDecisionTiming();
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.MovementPhase));
            Assert.That(movement.isCommitted && movement.isMovementPhaseAttack, Is.True);
            Assert.That(movement.attackersMoved, Is.Zero);
            Assert.That(movement.selectedToken, Is.SameAs(preview ? attacker : null));
            if (preview)
                Assert.That(grid.highlightedHexes, Does.Contain(previewDestination));
            Assert.That(attacker.currentHex, Is.SameAs(origin));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(ballHex));
            Assert.That(match.totalCompletedActions, Is.EqualTo(actions));
            Assert.That(match.CreateRuntimeSnapshot().clock.currentHalfRegulationSeconds, Is.EqualTo(regulationTime));
            Assert.That(match.gameData.events.Count(entry => entry.kind == "action.committed"), Is.EqualTo(commitments));
            Assert.That(movement.headerManager.defenderWillJump, Does.Contain(defender));
            Assert.That(movement.stunnedTokens, Does.Contain(awayGK));
            Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(originalDecision.id));
            Assert.That(match.DecisionTiming.Current.budget, Is.EqualTo(budget));
            Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(remaining));
            Assert.That(match.DecisionTiming.Current.expired, Is.EqualTo(expired));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired));
            Assert.That(match.CanCreateRuntimeSnapshot(out reason), Is.True, reason);

            if (!preview)
                Invoke(movement, "OnClickReceived", attacker, origin);
            var destination = previewDestination ?? grid.highlightedHexes.First(hex => hex != ball.GetCurrentHex());
            Invoke(movement, "OnClickReceived", null, destination);
            Assert.That(match.CanCreateRuntimeSnapshot(out _), Is.False, "An accepted move closes the opening checkpoint immediately.");
            yield return Until(() => attacker.currentHex == destination && !movement.isPlayerMoving && movement.attackersMoved == 1,
                "restored Movement Phase accepts and completes the first move");
            Assert.That(match.CanCreateRuntimeSnapshot(out _), Is.False, "Saving is blocked after a move.");
        }
        finally
        {
            appProperty.SetValue(null, previousApp);
            if (hadPreference) PlayerPrefs.SetString("currentGameSettings", preference);
            else PlayerPrefs.DeleteKey("currentGameSettings");
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(appHost);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    public IEnumerator RunForfeitedMovementOpeningCannotSave()
    {
        StartAutomaticMovementOpening(1);
        Subscribe(movement);
        LegacyKey("X");
        Assert.That(movement.isMovementPhaseDef, Is.True);
        Assert.That(movement.movedTokens, Is.Empty);
        Assert.That(match.CanCreateRuntimeSnapshot(out _), Is.False);
        yield break;
    }

    public IEnumerator RunAcceptedNutmegClosesMovementOpening()
    {
        LoadLegacyRoster();
        Set(grid, "gridInitialized", true);
        Place(attacker, Hex(0));
        var adjacent = attacker.currentHex.GetNeighbors(grid)
            .First(hex => !hex.isAttackOccupied && !hex.isDefenseOccupied);
        Place(defender, adjacent);
        ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker);
        match.UpdatePossessionAfterPass(attacker.currentHex);
        match.BroadcastEndofFirstTimePass();

        Invoke(movement, "OnClickReceived", attacker, attacker.currentHex);
        Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.True);
        Assert.That(match.CanCreateRuntimeSnapshot(out string reason), Is.True, reason);
        Invoke(movement, "OnClickReceived", defender, adjacent);
        Assert.That(movement.isNutmegInProgress || movement.isWaitingForInterceptionDiceRoll, Is.True);
        Assert.That(movement.movedTokens, Is.Empty);
        Assert.That(match.CanCreateRuntimeSnapshot(out _), Is.False);
        yield break;
    }

    private void StartAutomaticMovementOpening(int side)
    {
        LoadLegacyRoster(side);
        Set(grid, "gridInitialized", true);
        Place(attacker, Hex(-2 * side));
        HexCell looseBallHex = grid.cells.Cast<HexCell>()
            .First(hex => hex.coordinates.x * side > 7 && Mathf.Abs(hex.coordinates.z) < 4
                && !hex.isAttackOccupied && !hex.isDefenseOccupied);
        ball.PlaceAtCell(looseBallHex);
        match.UpdatePossessionAfterPass(looseBallHex);
        match.BroadcastEndofGroundBallPass();
        Assert.That(movement.isActivated && movement.isCommitted && movement.isMovementPhaseAttack, Is.True);
        Assert.That(movement.movedTokens, Is.Empty);
    }
}
