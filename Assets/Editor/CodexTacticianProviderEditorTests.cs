using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CodexTacticianProviderEditorTests
{
    [Test]
    public void ConfirmedPassCanBeVetoedAgainstASaferReopenedTarget()
    {
        RoomAITacticalEvaluation Choice(RoomDecisionStep step, float retention)
        {
            var candidate = new RoomActionCandidate { actionType = RoomActionType.GroundPass, step = step };
            return new RoomAITacticalEvaluation { candidate = candidate, explanation = new RoomAICandidateExplanation
            {
                candidate = candidate, hasPossessionLevelPlan = true,
                possessionPlanEffectiveRetentionProbability = retention, possessionPlanThreatExpectedGoals = 0f
            } };
        }
        var confirmation = Choice(RoomDecisionStep.Confirm, .1f);
        var alternate = Choice(RoomDecisionStep.ChooseTarget, .7f);
        RoomAITacticalSafety.ProtectPossession(new[] { confirmation, alternate });
        Assert.That(confirmation.safetyPriority, Is.GreaterThan(alternate.safetyPriority));
        Assert.That(confirmation.reasons, Has.Some.Contains("safer alternative"));
    }
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void SpacePassRequiresACollectionRaceNotJustNearbySupport(bool proven, bool rejected)
    {
        var candidate = new RoomActionCandidate { actionType = RoomActionType.GroundPass, step = RoomDecisionStep.ChooseTarget };
        var facts = new RoomAICandidateExplanation { candidate = candidate, targetHex = new RoomHexCoordinates { x = -12, z = -7 },
            friendlySupportWithinRange = 2, groundPassCollectorTokenKey = "Away:11",
            passRace = proven ? new RoomAIPassRacePreview { collectorTokenKey = "Away:11", opponentCollectsFirst = false } : null };
        var evaluation = new RoomAITacticalEvaluation { candidate = candidate, explanation = facts };
        var choices = new List<RoomAITacticalEvaluation> { evaluation };
        typeof(CodexTacticianProvider).GetMethod("ApplyUnsupportedPassSafety", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { new RoomAIDecisionSnapshot(), choices, new RoomAITacticalWeights() });
        Assert.That(evaluation.reasons.Exists(reason => reason.Contains("unsupported pass")), Is.EqualTo(rejected));
    }
    [TestCase(RoomDecisionStep.ChooseTarget)]
    [TestCase(RoomDecisionStep.Confirm)]
    public void UnsupportedPassEvidenceIsRetainedAtConfirmation(RoomDecisionStep step)
    {
        var candidate = new RoomActionCandidate { actionType = RoomActionType.GroundPass, step = step };
        var evaluation = new RoomAITacticalEvaluation { candidate = candidate, explanation = new RoomAICandidateExplanation
            { candidate = candidate, targetHex = new RoomHexCoordinates { x = -15, z = -6 } } };
        var evaluations = new List<RoomAITacticalEvaluation> { evaluation };
        typeof(CodexTacticianProvider).GetMethod("ApplyUnsupportedPassSafety", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { new RoomAIDecisionSnapshot(), evaluations, new RoomAITacticalWeights() });
        Assert.That(evaluation.tier, Is.EqualTo(RoomAITacticalTier.Risky));
        Assert.That(evaluation.reasons, Has.Some.Contains("unsupported pass"));
    }
    [Test]
    public void TacticalNoteDescribesTheCorrectDribbleStopWithoutDroppingDecisionData()
    {
        var entry = new RoomAIDecisionTraceEntry { decisionNumber = 123, selectedAction = "Press [X]",
            dribbleStopTokenKey = "Away:8", dribbleStopHex = new RoomHexCoordinates { x = -4, z = -11 },
            dribblePaceForfeited = 2, dribbleSnapshotDeclined = true };
        var method = typeof(PauseMenuManager).GetMethod("FormatTacticalNoteAction", BindingFlags.NonPublic | BindingFlags.Static);
        string summary = (string)method.Invoke(null, new object[] { entry });
        Assert.That(summary, Does.Contain("(-4, -11)").And.Contain("2 Pace forfeited").And.Contain("Snapshot declined"));
        Assert.That(entry.decisionNumber, Is.EqualTo(123));
        Assert.That(entry.selectedAction, Is.EqualTo("Press [X]"));
    }
    [Test]
    public void DetailedDirectPassReplacesEarlierPossessionEstimate()
    {
        RoomAITacticalEvaluation Choice(float retained)
        {
            var candidate = new RoomActionCandidate { actionType = RoomActionType.GroundPass, step = RoomDecisionStep.ChooseTarget };
            return new RoomAITacticalEvaluation { candidate = candidate, explanation = new RoomAICandidateExplanation
            {
                candidate = candidate, hasPossessionLevelPlan = true, possessionPlanEffectiveRetentionProbability = 1f,
                actionSequence = new RoomAIActionSequencePreview { outcomes = new RoomAIPossessionChainSummary
                    { attackRetainsPossessionProbability = retained, defenseWinsPossessionProbability = 1f - retained } }
            } };
        }
        var unsafePass = Choice(.2f); var saferPass = Choice(.8f);
        var choices = new List<RoomAITacticalEvaluation> { unsafePass, saferPass };
        var method = typeof(CodexTacticianProvider).GetMethod("ApplyHardLookaheadIncrementally", BindingFlags.NonPublic | BindingFlags.Static);
        var work = (System.Collections.IEnumerator)method.Invoke(null, new object[]
            { new RoomAIDecisionSnapshot(), choices, new RoomAITacticalWeights() });
        while (work.MoveNext()) { }
        Assert.That(saferPass.score - unsafePass.score, Is.GreaterThan(250));
    }
    [Test]
    public void NearbySupportUsesOffsetHexDistance()
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away" };
        snapshot.tokens.Add(new RoomAITokenSnapshot { tokenKey = "Away:8", teamSide = "Away", isPlaying = true,
            currentHex = new RoomHexCoordinates { x = 1, z = -1 } });
        var method = typeof(CodexTacticianProvider).GetMethod("CountNearbyTeammates", BindingFlags.NonPublic | BindingFlags.Static);
        int count = (int)method.Invoke(null, new object[] { snapshot, new RoomHexCoordinates { x = 0, z = 0 }, "Away:9", 1 });
        Assert.That(count, Is.Zero, "The offset-grid route to (1, -1) takes two hexes.");
    }
    [Test]
    public void DecisionTrace_UsesTenMinuteMatchSegments()
    {
        MethodInfo buildPath = typeof(RoomAIDecisionTraceService).GetMethod(
            "BuildSegmentTracePath",
            BindingFlags.Static | BindingFlags.NonPublic);
        RoomAIDecisionSnapshot snapshot = new()
        {
            clock = new RoomClockSnapshot
            {
                currentHalf = 2,
                currentHalfRegulationSeconds = 1250f
            }
        };

        Assert.That(buildPath, Is.Not.Null);
        string path = (string)buildPath.Invoke(null, new object[] { "/tmp/match.json", snapshot });

        Assert.That(path, Does.EndWith("match__AI Decisions__H2_20-29.json"));
    }

    [Test]
    public void DecisionTrace_ResetSessionReleasesRetainedDocument()
    {
        FieldInfo activeDocument = typeof(RoomAIDecisionTraceService).GetField(
            "activeDocument",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(activeDocument, Is.Not.Null);
        activeDocument.SetValue(null, new RoomAIDecisionTraceDocument());

        RoomAIDecisionTraceService.ResetSession();

        Assert.That(activeDocument.GetValue(null), Is.Null);
    }

    [Test]
    public void LiveEvents_UseTenMinuteJsonLineSegments()
    {
        MethodInfo buildPath = typeof(RoomLiveLogService).GetMethod(
            "BuildLiveEventSegmentPath",
            BindingFlags.Static | BindingFlags.NonPublic);
        GameplayEvent gameplayEvent = new()
        {
            currentHalf = 2,
            matchClockSeconds = 53 * 60
        };

        Assert.That(buildPath, Is.Not.Null);
        string path = (string)buildPath.Invoke(null, new object[] { "/tmp/match.json", gameplayEvent });

        Assert.That(path, Does.EndWith("match__Live Events__H2_50-59.jsonl"));
    }

    [Test]
    public void HumanShadowPollFingerprintChangesWithInputStage()
    {
        MethodInfo fingerprint = typeof(GameDebugMonitor).GetMethod(
            "BuildHumanShadowPollFingerprint",
            BindingFlags.Static | BindingFlags.NonPublic);
        GameplayInstructionSnapshot first = new()
        {
            manager = nameof(MovementPhaseManager),
            expectedTeam = "Home",
            expectedInput = "token",
            instructionText = "Select a token",
            isAwaitingInput = true
        };
        GameplayInstructionSnapshot second = new()
        {
            manager = nameof(MovementPhaseManager),
            expectedTeam = "Home",
            expectedInput = "hex",
            instructionText = "Select a destination",
            isAwaitingInput = true
        };

        Assert.That(fingerprint, Is.Not.Null);
        int firstHash = (int)fingerprint.Invoke(null, new object[] { first });
        int repeatedHash = (int)fingerprint.Invoke(null, new object[] { first });
        int secondHash = (int)fingerprint.Invoke(null, new object[] { second });

        Assert.That(repeatedHash, Is.EqualTo(firstHash));
        Assert.That(secondHash, Is.Not.EqualTo(firstHash));
    }

    [Test]
    public void KickoffSetup_ClickDuringMoveQueuesNewSelectionWithoutSwap()
    {
        GameObject managerObject = new("Kickoff manager");
        GameObject nextTokenObject = new("Next setup token");
        try
        {
            KickoffManager manager = managerObject.AddComponent<KickoffManager>();
            PlayerToken nextToken = nextTokenObject.AddComponent<PlayerToken>();
            MethodInfo register = typeof(KickoffManager).GetMethod(
                "RegisterNextSetupSelection",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(register, Is.Not.Null);
            register.Invoke(manager, new object[] { nextToken });

            Assert.That(manager.selectedToken, Is.SameAs(nextToken));
        }
        finally
        {
            Object.DestroyImmediate(nextTokenObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void KickoffSetup_FreshMatchActivationEnablesInput()
    {
        GameObject managerObject = new("Kickoff manager");
        try
        {
            KickoffManager manager = managerObject.AddComponent<KickoffManager>();
            FieldInfo activated = typeof(KickoffManager).GetField(
                "isActivated",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(activated, Is.Not.Null);
            Assert.That(activated.GetValue(manager), Is.False);

            manager.StartPreKickoffPhase();

            Assert.That(activated.GetValue(manager), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void NeutralKickoffSetup_DoesNotApplyDefensivePressureAndCanConfirm()
    {
        RoomActionCandidate confirm = new()
        {
            actionType = RoomActionType.Confirm,
            step = RoomDecisionStep.InterruptionChoice,
            key = "Space",
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            currentState = "KickOffSetup",
            expectedTeam = "Neutral",
            teamInAttack = "Home",
            attackHasPossession = false
        };
        snapshot.legalActions.Add(confirm);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = confirm });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(confirm));
        Assert.That(confirm.setupTerminalReason, Is.EqualTo("formation acceptable"));
        Assert.That(confirm.setupFormationPenaltyAfter, Is.Zero);
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("confirmation"));
        Assert.That(decision.selectedEvaluation.reasons, Has.None.Contains("defensive pressure"));
    }

    [Test]
    public void KickoffSetup_UsesSavedHumanLedHighShape()
    {
        MethodInfo resolveTarget = typeof(CodexTacticianProvider).GetMethod(
            "TryGetHumanLedKickoffTarget",
            BindingFlags.Static | BindingFlags.NonPublic);
        RoomAIDecisionSnapshot snapshot = new()
        {
            teamInAttack = "Home",
            homeTeamDirection = "LeftToRight",
            awayTeamDirection = "RightToLeft"
        };
        RoomAITokenSnapshot homeForward = new()
        {
            tokenKey = "Home:9",
            teamSide = "Home",
            jerseyNumber = 9,
            isPlaying = true
        };
        RoomAITokenSnapshot awayForward = new()
        {
            tokenKey = "Away:9",
            teamSide = "Away",
            jerseyNumber = 9,
            isPlaying = true
        };
        object[] homeArguments = { snapshot, homeForward, null };
        object[] awayArguments = { snapshot, awayForward, null };

        Assert.That(resolveTarget, Is.Not.Null);
        Assert.That(resolveTarget.Invoke(null, homeArguments), Is.True);
        Assert.That(resolveTarget.Invoke(null, awayArguments), Is.True);
        RoomHexCoordinates homeTarget = (RoomHexCoordinates)homeArguments[2];
        RoomHexCoordinates awayTarget = (RoomHexCoordinates)awayArguments[2];

        Assert.That((homeTarget.x, homeTarget.z), Is.EqualTo((0, 4)));
        Assert.That((awayTarget.x, awayTarget.z), Is.EqualTo((0, 5)));
    }

    [Test]
    public void KickoffSetup_SeparatesKickerFromSafeCentralReceiverAfterDefensivePress()
    {
        MethodInfo resolveTarget = typeof(CodexTacticianProvider).GetMethod(
            "TryGetHumanLedKickoffTarget",
            BindingFlags.Static | BindingFlags.NonPublic);
        RoomAIDecisionSnapshot snapshot = new()
        {
            teamInAttack = "Home",
            homeTeamDirection = "LeftToRight",
            awayTeamDirection = "RightToLeft"
        };
        RoomAITokenSnapshot weakerSix = new()
        {
            tokenKey = "Home:6", teamSide = "Home", jerseyNumber = 6, isPlaying = true,
            pace = 3, dribbling = 2, highPass = 2, shooting = 1
        };
        RoomAITokenSnapshot passingEight = new()
        {
            tokenKey = "Home:8", teamSide = "Home", jerseyNumber = 8, isPlaying = true,
            pace = 5, dribbling = 4, highPass = 6, shooting = 4
        };
        RoomAITokenSnapshot opposingPresser = new()
        {
            tokenKey = "Away:9", teamSide = "Away", jerseyNumber = 9, isPlaying = true, pace = 6
        };
        snapshot.tokens.AddRange(new[] { weakerSix, passingEight, opposingPresser });
        weakerSix.currentHex = new RoomHexCoordinates { x = 0, z = 0 };
        passingEight.currentHex = new RoomHexCoordinates { x = -7, z = 1 };
        opposingPresser.currentHex = new RoomHexCoordinates { x = 0, z = 5 };
        object[] kickerArguments = { snapshot, weakerSix, null };
        object[] receiverArguments = { snapshot, passingEight, null };

        Assert.That(resolveTarget.Invoke(null, kickerArguments), Is.True);
        Assert.That(resolveTarget.Invoke(null, receiverArguments), Is.True);
        RoomHexCoordinates kickerTarget = (RoomHexCoordinates)kickerArguments[2];
        RoomHexCoordinates receiverTarget = (RoomHexCoordinates)receiverArguments[2];

        Assert.That((kickerTarget.x, kickerTarget.z), Is.EqualTo((0, 0)));
        Assert.That(receiverTarget.x, Is.EqualTo(-7));
        Assert.That(Mathf.Abs(receiverTarget.z), Is.LessThanOrEqualTo(3));
        Assert.That(receiverTarget.x, Is.LessThan(-opposingPresser.pace));
    }

    [Test]
    public void MultiGoalDeficit_PushesKickoffShapeOnlyWithMoreThanTenMinutesRemaining()
    {
        MethodInfo resolveTarget = typeof(CodexTacticianProvider).GetMethod(
            "TryGetHumanLedKickoffTarget",
            BindingFlags.Static | BindingFlags.NonPublic);
        RoomAITokenSnapshot kicker = new()
        {
            tokenKey = "Home:6", teamSide = "Home", jerseyNumber = 6, isPlaying = true,
            pace = 3, dribbling = 2, highPass = 2, shooting = 1
        };
        RoomAITokenSnapshot receiver = new()
        {
            tokenKey = "Home:8", teamSide = "Home", jerseyNumber = 8, isPlaying = true,
            pace = 5, dribbling = 4, highPass = 6, shooting = 4
        };
        RoomAITokenSnapshot wingBack = new()
        {
            tokenKey = "Home:2", teamSide = "Home", jerseyNumber = 2, isPlaying = true,
            pace = 5
        };
        RoomAITokenSnapshot presser = new()
        {
            tokenKey = "Away:9", teamSide = "Away", jerseyNumber = 9, isPlaying = true,
            pace = 6
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            teamInAttack = "Home",
            homeTeamDirection = "LeftToRight",
            awayTeamDirection = "RightToLeft",
            homeScore = 0,
            awayScore = 2,
            halfDurationMinutes = 45,
            regulationHalfCount = 2,
            clock = new RoomClockSnapshot { currentHalf = 2, currentHalfRegulationSeconds = 2000f }
        };
        snapshot.tokens.AddRange(new[] { kicker, receiver, wingBack, presser });
        object[] receiverArguments = { snapshot, receiver, null };
        object[] earlyWingBackArguments = { snapshot, wingBack, null };

        Assert.That(resolveTarget.Invoke(null, receiverArguments), Is.True);
        Assert.That(resolveTarget.Invoke(null, earlyWingBackArguments), Is.True);
        RoomHexCoordinates receiverTarget = (RoomHexCoordinates)receiverArguments[2];
        RoomHexCoordinates earlyWingBackTarget = (RoomHexCoordinates)earlyWingBackArguments[2];

        snapshot.clock.currentHalfRegulationSeconds = 2200f;
        object[] lateWingBackArguments = { snapshot, wingBack, null };
        Assert.That(resolveTarget.Invoke(null, lateWingBackArguments), Is.True);
        RoomHexCoordinates lateWingBackTarget = (RoomHexCoordinates)lateWingBackArguments[2];

        Assert.That(receiverTarget.x, Is.EqualTo(-7), "The receiver must remain outside the projected press.");
        Assert.That(earlyWingBackTarget.x, Is.EqualTo(lateWingBackTarget.x + 2));
    }

    [Test]
    public void KickoffTakerSelection_PreservesDesignatedReceiver()
    {
        RoomActionCandidate sixCandidate = new()
        {
            actionType = RoomActionType.Kickoff,
            step = RoomDecisionStep.ChooseActor,
            isExecutableNow = true
        };
        RoomActionCandidate eightCandidate = new()
        {
            actionType = RoomActionType.Kickoff,
            step = RoomDecisionStep.ChooseActor,
            isExecutableNow = true
        };
        RoomAITokenSnapshot six = new()
        {
            tokenKey = "Home:6", teamSide = "Home", jerseyNumber = 6, isPlaying = true,
            pace = 3, dribbling = 2, highPass = 2, shooting = 1
        };
        RoomAITokenSnapshot eight = new()
        {
            tokenKey = "Home:8", teamSide = "Home", jerseyNumber = 8, isPlaying = true,
            pace = 5, dribbling = 4, highPass = 6, shooting = 4
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            currentState = "KickOffTakerSelection",
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true
        };
        snapshot.tokens.AddRange(new[] { six, eight });
        six.currentHex = new RoomHexCoordinates { x = 0, z = 0 };
        eight.currentHex = new RoomHexCoordinates { x = -7, z = 1 };
        snapshot.legalActions.AddRange(new[] { sixCandidate, eightCandidate });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = sixCandidate,
            actorToken = six
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = eightCandidate,
            actorToken = eight
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(sixCandidate));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("less attack-useful"));
    }

    [TestCase(2, false)]
    [TestCase(4, true)]
    public void ExtremeLateDeficit_UsesKickoffRushOnlyWithEnoughSequenceRunway(
        int actionsRemaining,
        bool expectsRush)
    {
        RoomActionCandidate safePass = CreatePass("safe-kickoff-pass");
        RoomActionCandidate rushPass = CreatePass("rush-number-ten");
        RoomAITokenSnapshot safeReceiver = new()
        {
            tokenKey = "Home:8", teamSide = "Home", jerseyNumber = 8, isPlaying = true,
            dribbling = 3
        };
        RoomAITokenSnapshot numberTen = new()
        {
            tokenKey = "Home:10", teamSide = "Home", jerseyNumber = 10, isPlaying = true,
            dribbling = 6
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            currentState = "KickOff",
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true,
            homeScore = 0,
            awayScore = 1,
            clock = new RoomClockSnapshot
            {
                currentHalf = 2,
                extraActionsDetermined = true,
                extraActionsRemaining = actionsRemaining
            }
        };
        snapshot.tokens.AddRange(new[] { safeReceiver, numberTen });
        snapshot.legalActions.AddRange(new[] { safePass, rushPass });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = safePass,
            targetToken = safeReceiver,
            targetTokenKey = safeReceiver.tokenKey,
            isKickoffDecision = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = rushPass,
            targetToken = numberTen,
            targetTokenKey = numberTen.tokenKey,
            groundPassInterceptionCandidates = 1,
            isKickoffDecision = true
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(
            decision.selectedCandidate,
            Is.SameAs(expectsRush ? rushPass : safePass));
        if (expectsRush)
        {
            Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("emergency kickoff"));
            Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("turnover exposure"));
        }
        else
        {
            Assert.That(decision.rankedCandidates.Find(item => item.candidate == rushPass).reasons,
                Has.None.Contains("emergency kickoff"), "Two actions do not justify the emergency sequence bonus.");
        }
    }

    [Test]
    public void KickoffConfirmation_IsManualOnlyWhenNoAutomatedTeamOwnsIt()
    {
        GameObject managerObject = new("Room decision manager");
        RoomDecisionManager manager = managerObject.AddComponent<RoomDecisionManager>();
        MethodInfo shouldWait = typeof(RoomDecisionManager).GetMethod(
            "ShouldAwaitManualKickoffConfirmation",
            BindingFlags.Instance | BindingFlags.NonPublic);
        GameplayInstructionSnapshot snapshot = new()
        {
            manager = nameof(KickoffManager),
            expectedTeam = "Neutral",
            isAwaitingInput = true
        };
        RoomActionCandidate confirm = new()
        {
            manager = nameof(KickoffManager),
            actionType = RoomActionType.Kickoff,
            step = RoomDecisionStep.Confirm,
            key = "Space",
            isExecutableNow = true
        };

        try
        {
            Assert.That(shouldWait, Is.Not.Null);
            Assert.That(shouldWait.Invoke(manager, new object[] { snapshot, confirm }), Is.True);

            FieldInfo automatedTeam = typeof(RoomDecisionManager).GetField(
                "activeKickoffAutomationTeam",
                BindingFlags.Instance | BindingFlags.NonPublic);
            automatedTeam.SetValue(manager, "Away");
            Assert.That(shouldWait.Invoke(manager, new object[] { snapshot, confirm }), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [TestCase("Ask Human", "Ask Human", false, false)]
    [TestCase("Ask Human", "Codex Tactician Ultra Hard", false, true)]
    [TestCase("Codex Tactician Ultra Hard", "Ask Human", true, false)]
    [TestCase("Codex Tactician Ultra Hard", "Codex Tactician Ultra Hard", true, true)]
    public void KickoffSetupOwnership_IsIndependentPerTeam(
        string homePersona,
        string awayPersona,
        bool expectsHomeAutomated,
        bool expectsAwayAutomated)
    {
        MatchManager.GameSettings settings = new()
        {
            homeRoomPersona = homePersona,
            awayRoomPersona = awayPersona,
            defaultRoomPersona = "Random"
        };

        Assert.That(
            AIManager.ResolveRoomPersona(settings, "Home") != AIManager.RoomPersona.AskHuman,
            Is.EqualTo(expectsHomeAutomated));
        Assert.That(
            AIManager.ResolveRoomPersona(settings, "Away") != AIManager.RoomPersona.AskHuman,
            Is.EqualTo(expectsAwayAutomated));
    }

    [Test]
    public void AtomicKickoffMove_ScoresTheCommittedActorDestinationPair()
    {
        RoomActionCandidate move = new()
        {
            manager = nameof(KickoffManager),
            actionType = RoomActionType.SetupMove,
            step = RoomDecisionStep.ChooseTarget,
            setupTeam = "Home",
            isExecutableNow = true
        };
        RoomAITokenSnapshot actor = new()
        {
            tokenKey = "Home:9",
            teamSide = "Home",
            jerseyNumber = 9,
            isPlaying = true,
            currentHex = new RoomHexCoordinates { x = -5, z = 4 }
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            currentState = "KickOffSetup",
            expectedTeam = "Home",
            teamInAttack = "Home",
            homeTeamDirection = "LeftToRight",
            awayTeamDirection = "RightToLeft"
        };
        snapshot.tokens.Add(actor);
        snapshot.legalActions.Add(move);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = move,
            actorToken = actor,
            targetHex = new RoomHexCoordinates { x = 0, z = 4 }
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(move));
        Assert.That(move.setupFormationPenaltyBefore, Is.GreaterThan(move.setupFormationPenaltyAfter));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("formation penalty"));
    }

    [Test]
    public void DelayedCollectorBeatenToLooseGroundPass_IsRisky()
    {
        RoomActionCandidate pass = new()
        {
            id = "delayed-contested-pass",
            actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget,
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "KickOff"
        };
        snapshot.legalActions.Add(pass);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = pass,
            isGroundPassContinuation = true,
            groundPassCollectorTokenKey = "Home:11",
            groundPassCollectorPickupDistance = 12,
            groundPassCollectorNeedsSecondMovement = true,
            groundPassPrePickupChallengeCount = 1,
            groundPassRetentionProbability = 0f,
            groundPassRouteExpectedGoals = -1f,
            groundPassCreatesReachableShot = true
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedEvaluation.tier, Is.EqualTo(RoomAITacticalTier.Risky));
        Assert.That(
            decision.selectedEvaluation.reasons,
            Has.Some.Contains("intervening defensive movement"));
    }

    [Test]
    public void RepeatedNoProgressMovementState_PrefersPassOverAnotherMovementPhase()
    {
        RoomActionCandidate movement = new()
        {
            actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseActionType,
            key = "M",
            isExecutableNow = true
        };
        RoomActionCandidate pass = new()
        {
            actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseActionType,
            key = "P",
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            currentState = "EndOfMovementPhase",
            expectedTeam = "Away",
            teamInAttack = "Away",
            attackHasPossession = true,
            repeatsNoProgressMovementState = true
        };
        snapshot.legalActions.Add(movement);
        snapshot.legalActions.Add(pass);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = movement });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = pass });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(pass));
        Assert.That(
            decision.rankedCandidates.Find(evaluation => evaluation.candidate == movement).reasons,
            Has.Some.Contains("no positional progress"));
    }

    [Test]
    public void AutomatedExecution_WaitsForTheSameDecisionStateOnALaterFrame()
    {
        MethodInfo method = typeof(RoomDecisionManager).GetMethod(
            "HasObservedDecisionStateForFullFrame",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        Assert.That(method.Invoke(null, new object[] { "state-a", "state-a", 10, 10 }), Is.False);
        Assert.That(method.Invoke(null, new object[] { "state-a", "state-b", 10, 11 }), Is.False);
        Assert.That(method.Invoke(null, new object[] { "state-a", "state-a", 10, 11 }), Is.True);
    }

    [Test]
    public void GoalkeeperInterruption_IsReadyWhileMovementCoroutineWaits()
    {
        GameplayInstructionSnapshot snapshot = new()
        {
            manager = nameof(GoalKeeperManager),
            isAwaitingInput = true,
            expectedInput = "key_or_click"
        };
        RoomDecisionContext context = new() { manager = snapshot.manager };
        context.AddAction(new RoomActionCandidate
        {
            manager = nameof(GoalKeeperManager),
            actionType = RoomActionType.GoalkeeperSave,
            step = RoomDecisionStep.ChooseTarget,
            isExecutableNow = true
        });
        MethodInfo method = typeof(GameDebugMonitor).GetMethod(
            "HasReadyNonMovementInstruction",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        Assert.That(method.Invoke(null, new object[] { snapshot, context }), Is.True);

        snapshot.manager = nameof(MovementPhaseManager);
        Assert.That(method.Invoke(null, new object[] { snapshot, context }), Is.False);

        snapshot.manager = nameof(ShotManager);
        context.actions[0].manager = nameof(ShotManager);
        Assert.That(
            method.Invoke(null, new object[] { snapshot, context }),
            Is.False,
            "Snapshot input must not interrupt an in-flight Snapshot defender movement.");
    }

    [Test]
    public void TacticalCandidateRouting_IncludesEveryOfferedForfeit()
    {
        GameObject managerObject = new("Room Decision Manager");
        GameObject actorObject = new("Movement actor");
        try
        {
            RoomDecisionManager manager = managerObject.AddComponent<RoomDecisionManager>();
            RoomActionCandidate movement = new()
            {
                manager = nameof(MovementPhaseManager),
                actionType = RoomActionType.StartMovement,
                step = RoomDecisionStep.ChooseActor,
                actor = actorObject.AddComponent<PlayerToken>(),
                isExecutableNow = true
            };
            RoomActionCandidate forfeit = new()
            {
                manager = nameof(MovementPhaseManager),
                actionType = RoomActionType.Decline,
                step = RoomDecisionStep.InterruptionChoice,
                key = "X",
                isForfeit = true,
                isExecutableNow = true
            };
            RoomDecisionContext context = new() { manager = nameof(MovementPhaseManager) };
            context.AddAction(movement);
            context.AddAction(forfeit);
            GameplayInstructionSnapshot snapshot = new() { manager = nameof(MovementPhaseManager) };
            MethodInfo method = typeof(RoomDecisionManager).GetMethod(
                "BuildTacticalExecutableCandidates",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(method, Is.Not.Null);
            List<RoomActionCandidate> candidates = (List<RoomActionCandidate>)method.Invoke(
                manager,
                new object[] { snapshot, context, false });

            Assert.That(candidates, Does.Contain(movement));
            Assert.That(candidates, Does.Contain(forfeit));
        }
        finally
        {
            Object.DestroyImmediate(actorObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void CompletedMovementLock_DoesNotForceMovementAtNextRootActionChoice()
    {
        GameObject managerObject = new("Room Decision Manager");
        try
        {
            RoomDecisionManager manager = managerObject.AddComponent<RoomDecisionManager>();
            FieldInfo lockField = typeof(RoomDecisionManager).GetField(
                "executionLockActionType",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo buildCandidates = typeof(RoomDecisionManager).GetMethod(
                "BuildTacticalExecutableCandidates",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(lockField, Is.Not.Null);
            Assert.That(buildCandidates, Is.Not.Null);
            lockField.SetValue(manager, RoomActionType.StartMovement);

            RoomDecisionContext context = new() { manager = nameof(MovementPhaseManager) };
            context.AddKeyActionCandidate(
                nameof(MovementPhaseManager),
                RoomActionType.StartMovement,
                RoomDecisionStep.ChooseActionType,
                "M",
                "Movement");
            context.AddKeyActionCandidate(
                nameof(GroundBallManager),
                RoomActionType.GroundPass,
                RoomDecisionStep.ChooseActionType,
                "P",
                "Pass");
            GameplayInstructionSnapshot snapshot = new()
            {
                manager = $"{nameof(MovementPhaseManager)},{nameof(GroundBallManager)}",
                expectedKeys = new List<string> { "M", "P" }
            };

            List<RoomActionCandidate> candidates = (List<RoomActionCandidate>)buildCandidates.Invoke(
                manager,
                new object[] { snapshot, context, true });

            Assert.That(candidates.Exists(candidate => candidate.key == "M"), Is.True);
            Assert.That(candidates.Exists(candidate => candidate.key == "P"), Is.True);
            Assert.That(lockField.GetValue(manager), Is.EqualTo(RoomActionType.Unknown));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void TargetStep_WithActorAndHex_ExecutesDestinationBeforeActor()
    {
        GameObject actorObject = new("Actor");
        GameObject hexObject = new("Target Hex", typeof(MeshRenderer));
        hexObject.SetActive(false); // Execution metadata only: avoid unrelated Edit Mode rendering callbacks.

        try
        {
            RoomActionCandidate candidate = new()
            {
                actor = actorObject.AddComponent<PlayerToken>(),
                targetHex = hexObject.AddComponent<HexCell>(),
                step = RoomDecisionStep.ChooseTarget
            };
            MethodInfo method = typeof(RoomDecisionManager).GetMethod(
                "ShouldExecuteHexCandidateFirst",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { candidate }), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(actorObject);
            Object.DestroyImmediate(hexObject);
        }
    }

    [Test]
    public void ResolveRoomPersona_RecognizesCodexTacticianAndDifficulty()
    {
        MatchManager.GameSettings settings = new()
        {
            awayRoomPersona = "Codex Tactician Hard"
        };

        Assert.That(
            AIManager.ResolveRoomPersona(settings, "Away"),
            Is.EqualTo(AIManager.RoomPersona.CodexTactician));
        Assert.That(
            AIManager.ResolveCodexTacticianDifficulty(settings, "Away"),
            Is.EqualTo(AIManager.CodexTacticianDifficulty.Hard));
    }

    [TestCase("Codex Tactician Easy", AIManager.CodexTacticianDifficulty.Easy)]
    [TestCase("CPU-Codex Tactician Medium", AIManager.CodexTacticianDifficulty.Medium)]
    [TestCase("Codex Tactician Hard", AIManager.CodexTacticianDifficulty.Hard)]
    [TestCase("CPU-Codex Tactician Ultra Hard", AIManager.CodexTacticianDifficulty.UltraHard)]
    public void ResolveCodexTacticianDifficulty_MapsAllSupportedLevels(
        string persona,
        AIManager.CodexTacticianDifficulty expectedDifficulty)
    {
        MatchManager.GameSettings settings = new() { homeRoomPersona = persona };

        Assert.That(
            AIManager.ResolveCodexTacticianDifficulty(settings, "Home"),
            Is.EqualTo(expectedDifficulty));
    }

    [TestCase("Codex Tactician Easy")]
    [TestCase("Codex Tactician Medium")]
    [TestCase("Codex Tactician Hard")]
    [TestCase("Codex Tactician Ultra Hard")]
    public void ResolveDraftPersona_CodexRoomTeamUsesSophisticatedDraft(string roomPersona)
    {
        GameObject managerObject = new GameObject("Codex draft persona test");
        try
        {
            AIManager manager = managerObject.AddComponent<AIManager>();
            GameSettings settings = new()
            {
                awayRoomPersona = roomPersona,
                awayDraftPersona = "Greedy"
            };

            Assert.That(
                manager.ResolveDraftPersona(settings, "Away"),
                Is.EqualTo(AIManager.Persona.Sophisticated));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void HardDifficulty_SelectsHighestRankedLegalAction()
    {
        RoomActionCandidate movement = new()
        {
            id = "movement",
            actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseActionType,
            label = "Start Movement",
            isExecutableNow = true
        };
        RoomActionCandidate shot = new()
        {
            id = "shot",
            actionType = RoomActionType.Shot,
            step = RoomDecisionStep.ChooseActionType,
            label = "Shoot",
            isExecutableNow = true
        };
        List<RoomActionCandidate> candidates = new() { movement, shot };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Away",
            attackHasPossession = true,
            currentState = "AnyOtherScenario"
        };
        snapshot.legalActions.AddRange(candidates);

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.providerName, Is.EqualTo(CodexTacticianProvider.DisplayName));
        Assert.That(decision.selectedCandidate, Is.SameAs(shot));
        Assert.That(decision.candidateCount, Is.EqualTo(2));
        Assert.That(decision.durationMilliseconds, Is.GreaterThanOrEqualTo(0d));
    }

    [Test]
    public void HardDifficulty_UsesPossessionChainLookaheadToBreakEqualBaseScores()
    {
        RoomActionCandidate riskyPass = CreatePass("risky");
        RoomActionCandidate safePass = CreatePass("safe");
        List<RoomActionCandidate> candidates = new() { riskyPass, safePass };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "StandardPass"
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = riskyPass,
            estimatedBallRetentionProbability = 0.25f,
            opponentsWithinTackleReach = 2,
            possessionChain = new RoomAIPossessionChainSummary
            {
                defenseWinsPossessionProbability = 0.7f,
                attackRetainsPossessionProbability = 0.2f,
                unresolvedProbability = 0.1f
            }
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = safePass,
            estimatedBallRetentionProbability = 0.9f,
            friendlySupportWithinRange = 2,
            possessionChain = new RoomAIPossessionChainSummary
            {
                goalProbability = 0.1f,
                attackRetainsPossessionProbability = 0.8f,
                defenseWinsPossessionProbability = 0.05f,
                unresolvedProbability = 0.05f
            }
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(safePass));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("hard lookahead"));
    }

    [Test]
    public void LooseBallMovement_PrioritizesNearestLegalRecoveryTarget()
    {
        RoomActionCandidate attackingPosition = new()
        {
            id = "attacking-position",
            actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseTarget,
            label = "Move into a shooting position",
            isExecutableNow = true
        };
        RoomActionCandidate recoverBall = new()
        {
            id = "recover-ball",
            actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseTarget,
            label = "Move to the loose ball",
            isExecutableNow = true
        };
        List<RoomActionCandidate> candidates = new() { attackingPosition, recoverBall };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = false,
            currentState = "MovementPhase",
            manager = nameof(MovementPhaseManager)
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = attackingPosition,
            targetDistanceToBall = 7,
            forwardProgressDelta = 12,
            targetCanShootFrom = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = recoverBall,
            targetDistanceToBall = 0,
            forwardProgressDelta = -4
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(recoverBall));
        Assert.That(
            decision.selectedEvaluation.reasons,
            Has.Some.Contains("loose-ball recovery"));
    }

    [Test]
    public void Kickoff_RejectsUnsupportedPassIntoEmptySpace()
    {
        RoomActionCandidate giveaway = CreatePass("giveaway");
        giveaway.actionType = RoomActionType.HighPass;
        RoomActionCandidate supported = CreatePass("supported");
        supported.actionType = RoomActionType.HighPass;
        List<RoomActionCandidate> candidates = new() { giveaway, supported };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "KickoffBlown"
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = giveaway,
            targetHex = new RoomHexCoordinates { x = 15, z = 5 },
            friendlySupportWithinRange = 0
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = supported,
            targetHex = new RoomHexCoordinates { x = 2, z = 5 },
            friendlySupportWithinRange = 1
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(supported));
        Assert.That(
            decision.rankedCandidates.Find(item => item.candidate == giveaway).tier,
            Is.EqualTo(RoomAITacticalTier.Risky));
    }

    [Test]
    public void DefensiveMovement_PrefersPressingAndMarkingNearTheBall()
    {
        RoomActionCandidate distantLine = CreateMovementTarget("distant-line");
        RoomActionCandidate press = CreateMovementTarget("press");
        List<RoomActionCandidate> candidates = new() { distantLine, press };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "MovementPhase"
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = distantLine,
            targetHex = new RoomHexCoordinates { x = 14, z = 5 },
            targetDistanceToBall = 9,
            nearestOpponentDistance = 7,
            targetCanShootFrom = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = press,
            targetHex = new RoomHexCoordinates { x = 3, z = 5 },
            targetDistanceToBall = 1,
            nearestOpponentDistance = 1
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(press));
        Assert.That(
            decision.selectedEvaluation.reasons,
            Has.Some.Contains("defensive pressure/marking"));
    }

    [Test]
    public void DefensiveActorSelection_KeepsDistanceSignalBeyondOldZeroCutoff()
    {
        RoomActionCandidate distant = CreateMovementActor("distant-defender");
        RoomActionCandidate nearer = CreateMovementActor("nearer-defender");
        List<RoomActionCandidate> candidates = new() { distant, nearer };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "MovementPhase"
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = distant,
            actorDistanceToBall = 20
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = nearer,
            actorDistanceToBall = 12
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(nearer));
    }

    [Test]
    public void WorthwhileRiskyMovement_BeatsNeutralForfeit()
    {
        RoomActionCandidate movement = CreateMovementTarget("useful-press");
        RoomActionCandidate forfeit = new()
        {
            id = "end-movement",
            manager = nameof(MovementPhaseManager),
            actionType = RoomActionType.Decline,
            step = RoomDecisionStep.InterruptionChoice,
            key = "X",
            isForfeit = true,
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "MovementPhase"
        };
        snapshot.legalActions.AddRange(new[] { movement, forfeit });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = movement,
            targetHex = new RoomHexCoordinates { x = 4, z = 0 },
            opponentsWithinTackleReach = 3
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = forfeit });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(movement));
        Assert.That(
            decision.rankedCandidates.Find(item => item.candidate == forfeit).reasons,
            Has.Some.Contains("forfeit yields"));
    }

    [Test]
    public void DefensiveGoalkeeperActorWithoutEmergency_LosesToOutfieldMovement()
    {
        RoomActionCandidate goalkeeper = CreateMovementActor("goalkeeper");
        RoomActionCandidate outfielder = CreateMovementActor("outfielder");
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Away",
            attackHasPossession = true,
            currentState = "MovementPhase"
        };
        snapshot.legalActions.AddRange(new[] { goalkeeper, outfielder });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = goalkeeper,
            isGoalkeeperPositioning = true,
            actorToken = new RoomAITokenSnapshot
            {
                tokenKey = "Home:1",
                teamSide = "Home",
                jerseyNumber = 1,
                isPlaying = true,
                isGoalKeeper = true
            },
            goalkeeperPositioningCurrentMaximumExpectedGoals = 0.30f,
            goalkeeperPositioningMaximumExpectedGoals = 0.30f,
            goalkeeperPositioningCurrentOutfieldBlockerCount = 2
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = outfielder,
            actorDistanceToBall = 20
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(outfielder));
        Assert.That(
            decision.rankedCandidates.Find(item => item.candidate == goalkeeper).reasons,
            Has.Some.Contains("does not materially reduce"));
    }

    [Test]
    public void AdequateKickoffShape_PrefersConfirmationOverMarginalMove()
    {
        RoomActionCandidate confirm = new()
        {
            id = "confirm-kickoff",
            actionType = RoomActionType.Confirm,
            step = RoomDecisionStep.InterruptionChoice,
            key = "Space",
            isExecutableNow = true
        };
        RoomActionCandidate marginalMove = new()
        {
            id = "marginal-kickoff-move",
            actionType = RoomActionType.SetupMove,
            step = RoomDecisionStep.ChooseTarget,
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            currentState = "KickOffSetup",
            expectedTeam = "Neutral",
            teamInAttack = "Home",
            attackHasPossession = false
        };
        snapshot.legalActions.AddRange(new[] { confirm, marginalMove });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = confirm });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = marginalMove,
            actorToken = new RoomAITokenSnapshot
            {
                tokenKey = "Home:8",
                teamSide = "Home",
                jerseyNumber = 8,
                isPlaying = true,
                currentHex = new RoomHexCoordinates { x = -2, z = 0 }
            },
            targetHex = new RoomHexCoordinates { x = -1, z = 0 }
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(confirm));
    }

    [Test]
    public void HighPassHistory_DoesNotCreateAnArtificialAttemptLimit()
    {
        RoomActionCandidate highPass = CreatePass("another-high-pass");
        highPass.actionType = RoomActionType.HighPass;
        highPass.step = RoomDecisionStep.ChooseActionType;
        RoomActionCandidate groundPass = CreatePass("ground-pass");
        groundPass.step = RoomDecisionStep.ChooseActionType;
        List<RoomActionCandidate> candidates = new() { highPass, groundPass };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Away",
            attackHasPossession = true,
            currentState = "EndOfMovementPhase",
            runtime = new RoomRuntimeSnapshot
            {
                stats = new MatchManager.Stats
                {
                    awayTeamStats = new MatchManager.TeamStats
                    {
                        totalAerialPassesAttempted = 8,
                        totalShots = 0
                    }
                }
            }
        };
        snapshot.legalActions.AddRange(candidates);

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(highPass));
        Assert.That(
            decision.rankedCandidates.Find(item => item.candidate == highPass).reasons,
            Has.None.Contains("repeated aerial attacks"));
    }

    [Test]
    public void ShotCreatingStandardPass_BeatsAnotherAccurateHighPass()
    {
        RoomActionCandidate highPass = CreatePass("high-pass");
        highPass.actionType = RoomActionType.HighPass;
        highPass.step = RoomDecisionStep.ChooseActionType;
        RoomActionCandidate groundPass = CreatePass("ground-pass-to-shooter");
        groundPass.step = RoomDecisionStep.ChooseActionType;
        RoomActionCandidate movement = CreateMovementTarget("dribble-into-shot");
        movement.step = RoomDecisionStep.ChooseActionType;
        List<RoomActionCandidate> candidates = new() { highPass, groundPass, movement };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "EndOfMovementPhase"
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = highPass,
            actionType = RoomActionType.HighPass,
            highPassAccuracyProbability = 5f / 6f
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = groundPass,
            actionType = RoomActionType.GroundPass,
            targetCanShootFrom = true,
            forwardProgressDelta = 2,
            groundPassInterceptionCandidates = 0
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = movement,
            actionType = RoomActionType.StartMovement,
            receiverCanReachShootingHexNextMovement = true,
            minimumStepsToShootingHex = 1
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.WideAerialAttack, team = "Home" },
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.plan.planType, Is.EqualTo(RoomAIAttackPlanType.CreateShot));
        Assert.That(decision.selectedCandidate, Is.SameAs(groundPass));
    }

    [Test]
    public void LooseBallDefensiveFinalThird_CanForfeitACluelessMove()
    {
        RoomActionCandidate badMove = CreateMovementTarget("far-from-loose-ball");
        badMove.manager = nameof(FinalThirdManager);
        badMove.actionType = RoomActionType.FinalThird;
        RoomActionCandidate forfeit = new()
        {
            id = "forfeit-f3",
            manager = nameof(FinalThirdManager),
            actionType = RoomActionType.Decline,
            step = RoomDecisionStep.InterruptionChoice,
            key = "Enter",
            isForfeit = true,
            isExecutableNow = true
        };
        List<RoomActionCandidate> candidates = new() { badMove, forfeit };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Home",
            attackHasPossession = false,
            currentState = "HighPass",
            manager = nameof(FinalThirdManager)
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = badMove,
            targetDistanceToBall = 12,
            nearestOpponentDistance = 8
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = forfeit });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(forfeit));
    }

    [Test]
    public void DefensiveFinalThird_LineReorganization_BeatsForfeit()
    {
        RoomActionCandidate lineMove = CreateMovementTarget("raise-offside-line");
        lineMove.manager = nameof(FinalThirdManager);
        lineMove.actionType = RoomActionType.FinalThird;
        RoomActionCandidate forfeit = new()
        {
            id = "forfeit-f3-shape",
            manager = nameof(FinalThirdManager),
            actionType = RoomActionType.Decline,
            step = RoomDecisionStep.InterruptionChoice,
            key = "Enter",
            isForfeit = true,
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "EndOfMovementPhase",
            manager = nameof(FinalThirdManager)
        };
        snapshot.legalActions.AddRange(new[] { lineMove, forfeit });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = lineMove,
            isRegularFinalThirdReorganization = true,
            currentSecondDefenderLineX = 10,
            proposedSecondDefenderLineX = 8,
            offsideLineRaise = 2,
            reachableOnsideOpponents = 2,
            actorDefinesSecondDefenderLine = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = forfeit });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(lineMove));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("offside line"));
    }

    [Test]
    public void GoalkeeperPositioning_PrefersTheHexCoveringMoreGkWallShotPaths()
    {
        RoomActionCandidate poorCoverage = new()
        {
            id = "poor-gk-position",
            manager = nameof(GoalKeeperManager),
            actionType = RoomActionType.GoalkeeperSave,
            step = RoomDecisionStep.ChooseTarget,
            isExecutableNow = true
        };
        RoomActionCandidate strongCoverage = new()
        {
            id = "strong-gk-position",
            manager = nameof(GoalKeeperManager),
            actionType = RoomActionType.GoalkeeperSave,
            step = RoomDecisionStep.ChooseTarget,
            isExecutableNow = true
        };
        List<RoomActionCandidate> candidates = new() { poorCoverage, strongCoverage };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Home",
            attackHasPossession = true,
            currentState = "MovementPhase"
        };
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = poorCoverage,
            isGoalkeeperPositioning = true,
            goalkeeperWallCoveredShotTargets = 1,
            goalkeeperWallShotTargetCount = 3,
            goalkeeperWallCoverageQuality = 2
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = strongCoverage,
            isGoalkeeperPositioning = true,
            goalkeeperWallCoveredShotTargets = 3,
            goalkeeperWallShotTargetCount = 3,
            goalkeeperWallCoverageQuality = 9
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            candidates,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.Hard);

        Assert.That(decision.selectedCandidate, Is.SameAs(strongCoverage));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("GK wall covers 3/3"));
    }

    [Test]
    public void ShootingAndHeadingPositions_AreRelativeToAttackingDirection()
    {
        GameObject targetObject = new("Directional shooting position");
        targetObject.SetActive(false);
        try
        {
            targetObject.AddComponent<MeshRenderer>();
            HexCell target = targetObject.AddComponent<HexCell>();
            target.CanShootFrom = true;
            target.CanHeadFrom = true;
            RoomAIDecisionSnapshot awaySnapshot = new()
            {
                expectedTeam = "Away",
                awayTeamDirection = "RightToLeft"
            };

            target.coordinates = new Vector3Int(14, 0, 0);
            Assert.That(InvokeDirectionalHexCheck("IsOpponentShootingHex", awaySnapshot, target), Is.False);
            Assert.That(InvokeDirectionalHexCheck("IsOpponentHeadingHex", awaySnapshot, target), Is.False);

            target.coordinates = new Vector3Int(-14, 0, 0);
            Assert.That(InvokeDirectionalHexCheck("IsOpponentShootingHex", awaySnapshot, target), Is.True);
            Assert.That(InvokeDirectionalHexCheck("IsOpponentHeadingHex", awaySnapshot, target), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void FinalThirdCandidateRouting_ExcludesStaleMovementPhaseCandidates()
    {
        GameObject targetObject = new("Final Third target");
        targetObject.SetActive(false);
        try
        {
            targetObject.AddComponent<MeshRenderer>();
            HexCell target = targetObject.AddComponent<HexCell>();
            RoomActionCandidate finalThird = new()
            {
                manager = nameof(FinalThirdManager),
                actionType = RoomActionType.FinalThird,
                step = RoomDecisionStep.ChooseTarget,
                targetHex = target,
                isExecutableNow = true
            };
            RoomActionCandidate staleMovement = new()
            {
                manager = nameof(MovementPhaseManager),
                actionType = RoomActionType.StartMovement,
                step = RoomDecisionStep.ChooseTarget,
                targetHex = target,
                isExecutableNow = true
            };
            RoomDecisionContext context = new() { manager = nameof(FinalThirdManager) };
            context.AddAction(staleMovement);
            context.AddAction(finalThird);
            GameplayInstructionSnapshot instruction = new() { manager = nameof(FinalThirdManager) };

            MethodInfo method = typeof(RoomDecisionManager).GetMethod(
                "BuildFinalThirdExecutableCandidates",
                BindingFlags.NonPublic | BindingFlags.Static);
            List<RoomActionCandidate> candidates = (List<RoomActionCandidate>)method.Invoke(
                null,
                new object[] { instruction, context });

            Assert.That(candidates, Is.EqualTo(new[] { finalThird }));
        }
        finally
        {
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void FinalThirdCandidateRouting_IncludesTheLegalTeamForfeit()
    {
        RoomActionCandidate finalThird = new()
        {
            manager = nameof(FinalThirdManager),
            actionType = RoomActionType.FinalThird,
            step = RoomDecisionStep.ChooseActor,
            actor = new GameObject("F3 actor").AddComponent<PlayerToken>(),
            isExecutableNow = true
        };
        try
        {
            RoomActionCandidate forfeit = new()
            {
                manager = nameof(FinalThirdManager),
                actionType = RoomActionType.Decline,
                step = RoomDecisionStep.InterruptionChoice,
                key = "Enter",
                isForfeit = true,
                isExecutableNow = true
            };
            RoomDecisionContext context = new() { manager = nameof(FinalThirdManager) };
            context.AddAction(finalThird);
            context.AddAction(forfeit);
            GameplayInstructionSnapshot instruction = new() { manager = nameof(FinalThirdManager) };
            MethodInfo method = typeof(RoomDecisionManager).GetMethod(
                "BuildFinalThirdExecutableCandidates",
                BindingFlags.NonPublic | BindingFlags.Static);

            List<RoomActionCandidate> candidates = (List<RoomActionCandidate>)method.Invoke(
                null,
                new object[] { instruction, context });

            Assert.That(candidates, Is.EqualTo(new[] { forfeit, finalThird }));
        }
        finally
        {
            Object.DestroyImmediate(finalThird.actor.gameObject);
        }
    }

    [Test]
    public void PenaltyFoulDecision_RanksTakingThePenaltyAboveGenericAdvantage()
    {
        RoomActionCandidate advantage = new()
        {
            actionType = RoomActionType.FoulAdvantage,
            step = RoomDecisionStep.InterruptionChoice,
            label = "Play advantage",
            isExecutableNow = true
        };
        RoomActionCandidate penalty = new()
        {
            actionType = RoomActionType.Foul,
            step = RoomDecisionStep.InterruptionChoice,
            label = "Take penalty",
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Away",
            teamInAttack = "Away",
            attackHasPossession = true
        };
        snapshot.legalActions.AddRange(new[] { advantage, penalty });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = advantage
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = penalty,
            isPenaltyFoulDecision = true,
            penaltyExpectedGoals = 0.75f,
            expectedGoals = 0.75f,
            goalkeeperPenalty = -2
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(penalty));
        Assert.That(decision.selectedEvaluation.tier, Is.EqualTo(RoomAITacticalTier.ImmediateFinish));
    }

    [Test]
    public void EndOfHalfOwnHalfLead_RanksLongBallAboveShortContinuation()
    {
        RoomActionCandidate shortPass = new()
        {
            actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseActionType,
            label = "Short pass",
            isExecutableNow = true
        };
        RoomActionCandidate longBall = new()
        {
            actionType = RoomActionType.LongBall,
            step = RoomDecisionStep.ChooseActionType,
            label = "Long ball",
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true
        };
        snapshot.legalActions.AddRange(new[] { shortPass, longBall });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = shortPass,
            isLateResultManagementDecision = true,
            lateResultScoreDifference = 1,
            lateResultActionsRemaining = 1
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = longBall,
            isLateResultManagementDecision = true,
            lateResultScoreDifference = 1,
            lateResultActionsRemaining = 1
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(longBall));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("protects a 1-goal lead"));
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void EndOfHalfOwnHalfWhileNotLeading_DoesNotApplyClearanceBias(int scoreDifference)
    {
        RoomActionCandidate shortPass = new()
        {
            actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseActionType,
            label = "Attacking short pass",
            isExecutableNow = true
        };
        RoomActionCandidate longBall = new()
        {
            actionType = RoomActionType.LongBall,
            step = RoomDecisionStep.ChooseActionType,
            label = "Safety clearance",
            isExecutableNow = true
        };
        RoomAIDecisionSnapshot snapshot = new()
        {
            expectedTeam = "Home",
            teamInAttack = "Home",
            attackHasPossession = true
        };
        snapshot.legalActions.AddRange(new[] { shortPass, longBall });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = shortPass,
            isLateResultManagementDecision = true,
            lateResultScoreDifference = scoreDifference,
            lateResultActionsRemaining = 1,
            receiverCanReachShootingHexNextMovement = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = longBall,
            isLateResultManagementDecision = true,
            lateResultScoreDifference = scoreDifference,
            lateResultActionsRemaining = 1
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(shortPass));
        if (scoreDifference < 0)
        {
            Assert.That(decision.rankedCandidates[0].reasons, Has.Some.Contains("do not apply result-protection"));
        }
    }

    private static bool InvokeDirectionalHexCheck(
        string methodName,
        RoomAIDecisionSnapshot snapshot,
        HexCell hex)
    {
        MethodInfo method = typeof(RoomAIDecisionSnapshotBuilder).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);
        return (bool)method.Invoke(null, new object[] { snapshot, hex });
    }

    private static RoomActionCandidate CreatePass(string id)
    {
        return new RoomActionCandidate
        {
            id = id,
            actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget,
            label = id,
            isExecutableNow = true
        };
    }

    private static RoomActionCandidate CreateMovementTarget(string id)
    {
        return new RoomActionCandidate
        {
            id = id,
            manager = nameof(MovementPhaseManager),
            actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseTarget,
            label = id,
            isExecutableNow = true
        };
    }

    private static RoomActionCandidate CreateMovementActor(string id)
    {
        return new RoomActionCandidate
        {
            id = id,
            manager = nameof(MovementPhaseManager),
            actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseActor,
            label = id,
            isExecutableNow = true
        };
    }
}
