using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunRulesTiming(bool paused)
    {
        Board(1); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker); match.isClockRunning = true;
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        SceneManager.SetActiveScene(scene);
        var rulesRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RulesViewerEditorTools.PrefabPath), root.transform);
        var viewer = rulesRoot.GetComponent<RulesViewer>();
        match.RefreshDecisionTiming();
        var decision = match.DecisionTiming.Current;
        float before = decision.remaining;
        if (paused) match.SetPauseMenuOpen(true);
        float scale = Time.timeScale;
        try
        {
            rulesRoot.AddComponent<RulesViewerOpener>().OpenRules();
            Assert.That(RulesViewer.IsOpen, Is.True);
            Assert.That(match.IsPauseMenuOpen, Is.EqualTo(paused));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            match.AdvanceDecisionTiming(2);
            Assert.That(decision.remaining, Is.EqualTo(before - (paused ? 0 : 2)));
            var physical = new KeyPressData(KeyCode.R, false, false, false);
            Assert.That(RoomInputOwnership.ConsumeKey(physical, true, attacker), Is.True);
            Subscribe(match); Invoke(match, "EnterExtraActionsRollPrompt");
            match.AdvanceDecisionTiming(2);
            Assert.That(match.IsWaitingForExtraActionsRoll, Is.True);
            viewer.search.ActivateInputField();
            yield return null;
            match.AdvanceDecisionTiming(3);
            Assert.That(match.IsWaitingForExtraActionsRoll, Is.EqualTo(paused));
            Assert.That(match.TimingRollSequence, Is.EqualTo(paused ? 0 : 1));
            if (!paused)
            {
                Invoke(match, "EnterExtraActionsRollPrompt");
                Assert.That(input.TryExecuteSyntheticKeyPress("R", "Room AI", out var error), Is.True, error);
                Assert.That(match.TimingRollSequence, Is.EqualTo(2), "Rules focus must not consume CPU input");
            }
            viewer.Close();
            Assert.That(match.IsPauseMenuOpen, Is.EqualTo(paused), "closing Rules must preserve the caller's pause");
            if (paused)
            {
                match.SetPauseMenuOpen(false); match.AdvanceDecisionTiming(5);
                Assert.That(match.TimingRollSequence, Is.EqualTo(1));
            }
        }
        finally { viewer.Close(); match.SetPauseMenuOpen(false); }
    }

    public IEnumerator RunCpuRollTiming(bool cpuFirst)
    {
        Board(1); Subscribe(match);
        var cpu = Add<RoomDecisionManager>();
        string Key() => (string)Invoke(cpu, "BuildDecisionStateKeyCore", new GameplayInstructionSnapshot(),
            new RoomDecisionContext(), AIManager.RoomPersona.CodexTactician, AIManager.CodexTacticianDifficulty.Medium, true);
        Invoke(match, "EnterExtraActionsRollPrompt");
        match.RefreshDecisionTiming();
        string initialKey = Key();
        string oldRoll = match.DecisionTiming.RollId;
        match.AdvanceDecisionTiming(2);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.True, "fallback must leave the CPU its first two seconds");
        if (cpuFirst)
        {
            Assert.That(input.TryExecuteSyntheticKeyPress("R", "Room AI", out var error), Is.True, error);
            match.AdvanceDecisionTiming(3);
        }
        else
        {
            match.AdvanceDecisionTiming(3);
            input.TryExecuteSyntheticKeyPress("R", "Room AI", out _);
        }
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.False);
        Assert.That(match.DecisionTiming.SubmitRoll(oldRoll, "Both teams"), Is.False);
        // Isolate the sequence discriminator from other gameplay changes in the CPU cache key.
        var keyAfter = Key();
        typeof(MatchManager).GetProperty(nameof(MatchManager.TimingRollSequence)).SetValue(match, 0);
        Assert.That(Key(), Is.Not.EqualTo(keyAfter), "a completed automatic roll invalidates cached CPU analysis");
        typeof(MatchManager).GetProperty(nameof(MatchManager.TimingRollSequence)).SetValue(match, 1);
        Assert.That(keyAfter, Is.Not.EqualTo(initialKey));
        yield break;
    }
}
