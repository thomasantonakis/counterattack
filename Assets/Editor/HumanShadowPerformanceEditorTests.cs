using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class HumanShadowPerformanceEditorTests
{
    private static IEnumerator CountWork(int total, System.Action step)
    {
        for (int index = 0; index < total; index++)
        {
            step();
            yield return null;
        }
    }

    [Test]
    public void TargetAnalysis_YieldsBeforeWorkAndCompletesAll310OptionsInBatches()
    {
        int completed = 0;
        double milliseconds = 0;
        var batches = RoomAIHumanShadowCache.InFrameBatches(
            CountWork(310, () => { completed++; milliseconds++; }), 6, () => milliseconds);
        Assert.That(batches.MoveNext(), Is.True);
        Assert.That(completed, Is.Zero, "Starting analysis cannot block the input update.");
        int frames = 1;
        while (true)
        {
            int before = completed;
            bool more = batches.MoveNext();
            Assert.That(completed - before, Is.LessThanOrEqualTo(6));
            if (!more) break;
            frames++;
        }
        Assert.That(completed, Is.EqualTo(310), "Batching must not prune legal targets.");
        Assert.That(frames, Is.GreaterThan(50));
    }

    [Test]
    public void TargetAnalysis_CancellationDoesNotRunRemainingWork()
    {
        int completed = 0;
        double milliseconds = 0;
        var batches = RoomAIHumanShadowCache.InFrameBatches(
            CountWork(310, () => { completed++; milliseconds++; }), 6, () => milliseconds);
        batches.MoveNext();
        batches.MoveNext();
        Assert.That(completed, Is.EqualTo(6));
        ((System.IDisposable)batches).Dispose();
        Assert.That(batches.MoveNext(), Is.False);
        Assert.That(completed, Is.EqualTo(6));
    }

    [Test]
    public void TargetAndConfirmation_ReuseFactsButRebindActualCandidate()
    {
        var target = new RoomActionCandidate
        {
            manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget, label = "Choose target"
        };
        var confirm = new RoomActionCandidate
        {
            manager = target.manager, actionType = target.actionType,
            step = RoomDecisionStep.Confirm, label = "Confirm target", key = "Enter"
        };
        var facts = new RoomAICandidateExplanation
        {
            candidate = target, label = target.label, groundPassRouteExpectedGoals = 0.5555556f,
            passRace = new RoomAIPassRacePreview { pickupCost = 1, remainingPace = 5 }
        };
        Assert.That(RoomAIHumanShadowCache.CandidateKey(confirm),
            Is.EqualTo(RoomAIHumanShadowCache.CandidateKey(target)));
        var copy = facts.ForCandidate(confirm);
        Assert.That(copy.candidate, Is.SameAs(confirm));
        Assert.That(copy.step, Is.EqualTo(RoomDecisionStep.Confirm));
        Assert.That(copy.key, Is.EqualTo("Enter"));
        Assert.That(copy.label, Is.EqualTo("Confirm target"));
        Assert.That(copy.groundPassRouteExpectedGoals, Is.EqualTo(facts.groundPassRouteExpectedGoals));
        Assert.That(copy.passRace.pickupCost, Is.EqualTo(1));
        Assert.That(copy.passRace.remainingPace, Is.EqualTo(5));
        Assert.That(facts.candidate, Is.SameAs(target));
    }

    [Test]
    public void GroundPassAnalysis_DeferredAndSynchronousFactsRemainEquivalent()
    {
        // No live MatchManager required: this also covers cancellation/load gaps.
        var previousMatch = MatchManager.Instance;
        MatchManager.Instance = null;
        try
        {
            var action = new RoomActionCandidate
            {
                manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseTarget, label = "Ground pass", isExecutableNow = true
            };
            var instruction = new GameplayInstructionSnapshot { manager = action.manager, expectedTeam = "Home" };
            var complete = RoomAIDecisionSnapshotBuilder.Build(instruction, null, new[] { action });
            var deferred = RoomAIDecisionSnapshotBuilder.Build(instruction, null, new[] { action }, false);
            Assert.That(deferred.candidateExplanations, Is.Empty);
            var cache = new RoomAIHumanShadowCache { boardKey = "same board" };
            cache.Attach(deferred);
            var work = RoomAIDecisionSnapshotBuilder.BuildCandidateExplanationsIncrementally(deferred, cache);
            while (work.MoveNext()) { }
            Assert.That(deferred.candidateExplanations.Count, Is.EqualTo(1));
            var expected = complete.candidateExplanations.Single();
            var actual = deferred.candidateExplanations.Single();
            foreach (var field in typeof(RoomAICandidateExplanation).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(field => field.FieldType.IsValueType || field.FieldType == typeof(string)))
                Assert.That(field.GetValue(actual), Is.EqualTo(field.GetValue(expected)), field.Name);
            Assert.That(cache.explanations.Count, Is.EqualTo(1));
        }
        finally { MatchManager.Instance = previousMatch; }
    }

    [Test]
    public void BoardIdentity_IgnoresSelectionButInvalidatesOnAttributesRulesAndMatchState()
    {
        var previousMatch = MatchManager.Instance;
        var fixture = new GameObject("Human Shadow board identity fixture");
        fixture.SetActive(false);
        try
        {
            var match = fixture.AddComponent<MatchManager>();
            MatchManager.Instance = match;
            match.groundBallManager = fixture.AddComponent<GroundBallManager>();
            match.playerTokenManager = fixture.AddComponent<PlayerTokenManager>();
            var player = fixture.AddComponent<PlayerToken>();
            player.pace = 5;
            match.playerTokenManager.allTokens.Add(player);
            var manager = fixture.AddComponent<RoomDecisionManager>();
            var identity = typeof(RoomDecisionManager).GetMethod("BuildHumanShadowBoardKey",
                BindingFlags.NonPublic | BindingFlags.Instance);
            string Key() => (string)identity.Invoke(manager, new object[] { "Home" });
            string before = Key();
            Assert.That(Key(), Is.EqualTo(before));
            var target = fixture.AddComponent<HexCell>();
            target.coordinates = new Vector3Int(5, 0, -8);
            match.groundBallManager.currentTargetHex = target;
            Assert.That(Key(), Is.EqualTo(before), "Orange target selection is not a board change.");
            match.groundBallManager.currentTargetHex = null;
            player.pace = 6;
            Assert.That(Key(), Is.Not.EqualTo(before));
            player.pace = 5;
            Assert.That(Key(), Is.EqualTo(before));
            match.groundBallManager.isQuickThrow = true;
            Assert.That(Key(), Is.Not.EqualTo(before));
            match.groundBallManager.isQuickThrow = false;
            match.currentState = MatchManager.GameState.StandardPass;
            Assert.That(Key(), Is.Not.EqualTo(before));
        }
        finally
        {
            MatchManager.Instance = previousMatch;
            Object.DestroyImmediate(fixture);
        }
    }

    [Test]
    public void BoardCache_IsScopedAndPartialDecisionIsExplicit()
    {
        var first = new RoomAIHumanShadowCache { boardKey = "before move" };
        var second = new RoomAIHumanShadowCache { boardKey = "after move" };
        first.explanations["target"] = new RoomAICandidateExplanation();
        Assert.That(second.explanations, Is.Empty);
        var pending = new RoomAIProviderDecision { analysisComplete = false, legalCandidateCount = 310 };
        Assert.That(pending.rankedCandidates, Is.Empty);
        Assert.That(pending.analysisComplete, Is.False);
        Assert.That(pending.legalCandidateCount, Is.EqualTo(310));
        Assert.That(new RoomAIProviderDecision().analysisComplete, Is.True,
            "Normal automated decisions remain complete by default.");
    }
}
