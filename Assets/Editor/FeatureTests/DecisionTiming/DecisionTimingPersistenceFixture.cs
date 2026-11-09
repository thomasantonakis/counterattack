using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunTimingStoppageReview(bool expired, bool dismiss)
    {
        Board(1); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker); match.isClockRunning = true;
        movement.ActivateMovementPhase();
        match.RefreshDecisionTiming(); var parent = match.DecisionTiming.Current;
        match.AdvanceDecisionTiming(expired ? parent.remaining : 7);
        float remaining = parent.remaining;
        AttachAuthoredPanels();
        match.gameObject.SetActive(true);
        var panel = match.stoppageActionsPanel; panel.resultSeconds = .1f;
        match.isHalfExpired = true;
        int continuations = 0;
        Assert.That(match.TryEnterExtraActionsRollBeforeRestart(() => continuations++), Is.True);
        Assert.That(panel.IsAwaitingRoll, Is.True);
        match.AdvanceDecisionTiming(2);
        LegacyKey("R");
        Assert.That(panel.IsOpen && !panel.IsAwaitingRoll, Is.True);
        int allocation = match.extraActionsRemaining;
        Assert.That(allocation, Is.InRange(1, match.GetStoppageActionCap()));
        LegacyKey("Return");
        Assert.That(panel.IsResultHeld, Is.True);
        yield return new WaitForSecondsRealtime(.25f);
        float possession = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        match.AdvanceDecisionTiming(100);
        typeof(MatchManager).GetMethod("AdvanceMatchTime", Private).Invoke(match, new object[] {10f});
        Assert.That(panel.IsOpen, Is.True);
        Assert.That(continuations, Is.Zero);
        Assert.That(parent.remaining, Is.EqualTo(remaining));
        Assert.That(match.DecisionTiming.BlockingResult, Is.True);
        Assert.That(match.extraActionsRemaining, Is.EqualTo(allocation));
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.EqualTo(possession));
        LegacyKey(dismiss ? "Escape" : "Return");
        yield return Until(() => continuations == 1, "stoppage result resumes real restart callback exactly once");
        match.RefreshDecisionTiming();
        Assert.That(panel.IsOpen, Is.False);
        Assert.That(match.DecisionTiming.BlockingResult, Is.False);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired));
        Assert.That(parent.remaining, Is.EqualTo(remaining));
        Assert.That(match.extraActionsRemaining, Is.EqualTo(allocation));
    }

    public IEnumerator RunTimingFileRestore(int side, bool expired)
    {
        LoadLegacyRoster(side); Set(grid, "gridInitialized", true);
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        match.isClockRunning = true; match.RefreshAvailableActionsForCurrentState();
        match.RefreshDecisionTiming(); var original = match.DecisionTiming.Current;
        Assert.That(original.kind, Is.EqualTo(DecisionKind.SelectNextAction));
        match.AdvanceDecisionTiming(expired ? original.remaining : 4);
        float remaining = original.remaining;
        string directory = Path.Combine("TestResults/GameplayRegression", "TimingSave-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var previousApp = ApplicationManager.Instance;
        var appProperty = typeof(ApplicationManager).GetProperty(nameof(ApplicationManager.Instance));
        bool hadPreference = PlayerPrefs.HasKey("currentGameSettings");
        string preference = PlayerPrefs.GetString("currentGameSettings");
        var appHost = Child("Isolated save context");
        var app = appHost.AddComponent<ApplicationManager>();
        appProperty.SetValue(null, app);
        try
        {
            string path = Path.GetFullPath(Path.Combine(directory, "match.json"));
            app.SetActiveSaveFilePath(path);
            Assert.That(RoomSaveService.SaveToPath(match, path, false, out var saved, out var error), Is.True, error);
            Assert.That(saved, Is.EqualTo(path));
            string source = File.ReadAllText(path);
            Assert.That(RoomSaveService.CreateLoadedRunCopy(path, out var run, out error), Is.True, error);
            Assert.That(File.ReadAllText(path), Is.EqualTo(source), "loading must not mutate the seed");
            var loaded = JsonConvert.DeserializeObject<MatchManager.GameData>(File.ReadAllText(run));
            Assert.That(loaded.playSessionId, Is.Not.EqualTo(match.gameData.playSessionId));
            match.AdvanceDecisionTiming(1000);
            Invoke(match, "ApplyRuntimeSnapshot", loaded.runtimeSnapshot);
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(original.id));
            Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(remaining));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired));
            Subscribe(ground); LegacyKey("P");
            Assert.That(ground.isActivated, Is.True);
        }
        finally
        {
            appProperty.SetValue(null, previousApp);
            if (hadPreference) PlayerPrefs.SetString("currentGameSettings", preference);
            else PlayerPrefs.DeleteKey("currentGameSettings");
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(appHost);
            Directory.Delete(directory, true);
        }
        yield break;
    }

    public IEnumerator RunTimingInterruptedRestore(int side, bool expired)
    {
        LoadLegacyRoster(side); Set(grid, "gridInitialized", true);
        // Move unrelated players away, then exercise the real tackle/reposition path.
        foreach (var token in match.playerTokenManager.allTokens.Where(t => t != attacker && t != defender).ToArray())
            if (token.currentHex != null) Place(token, Hex((token.isHomeTeam ? -10 : 10) * side, token.jerseyNumber - 8));
        Place(attacker, Hex(0)); Place(defender, Hex(side));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        match.currentState = MatchManager.GameState.EndOfMovementPhase; match.isClockRunning = true;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        movement.ActivateMovementPhase(); movement.ForfeitTeamMovementPhase();
        match.RefreshDecisionTiming(); var parent = match.DecisionTiming.Current;
        match.AdvanceDecisionTiming(expired ? parent.remaining : 7);
        Invoke(movement, "OnClickReceived", defender, defender.currentHex); LegacyKey("T");
        movement.PerformTackleDiceRoll(true, new RollInputOverride {hasOverride=true,roll=2});
        yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, new RollInputOverride {hasOverride=true,roll=6});
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "real interrupted phase reaches child reposition");
        match.AdvanceDecisionTiming(2); var child = match.DecisionTiming.Current;
        Assert.That(child.kind, Is.EqualTo(DecisionKind.TackleReposition));
        Assert.That(match.CanCreateRuntimeSnapshot(out string reason), Is.False);
        Assert.That(reason, Does.Contain("action"));
        var snapshot = JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(JsonConvert.SerializeObject(match.CreateRuntimeSnapshot()));
        Invoke(match, "ApplyRuntimeSnapshot", snapshot); match.RefreshDecisionTiming();
        Assert.That(movement.isActivated || movement.isWaitingForReposition, Is.False);
        Assert.That(match.DecisionTiming.Capture().decisions.Any(d => d.id == parent.id || d.id == child.id), Is.False);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False, "unreconstructed parent/child holds must be discarded");
        Subscribe(ground); LegacyKey("P");
        Assert.That(ground.isActivated, Is.True, "rebuilt stable gameplay accepts its next action");
    }
}
