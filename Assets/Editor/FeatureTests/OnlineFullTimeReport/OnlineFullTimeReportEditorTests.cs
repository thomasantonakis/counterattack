using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineFullTimeReport")]
public sealed class OnlineFullTimeReportEditorTests
{
    [Test]
    public void LongMatchPreservesPitchFiltersAndForcedRollClassification()
    {
        var source = Data();
        var random = new Random(12);
        for (int sequence = 1; sequence <= 600; sequence++)
        {
            int half = (sequence - 1) / 150 + 1;
            bool homeAttacks = sequence % 2 == 0;
            source.events.Add(new GameplayEvent
            {
                sequenceNumber = sequence, kind = "snapshot.action_boundary", currentHalf = half,
                teamInAttack = homeAttacks ? "Home" : "Away", attackHasPossession = true,
                snapshot = new GameplaySnapshotSummary
                {
                    currentState = "EndOfMovementPhase",
                    tokens = Enumerable.Range(1, 22).Select(slot => new GameplayTokenStateSummary
                    {
                        tokenKey = slot <= 11 ? $"Home:{slot}" : $"Away:{slot - 11}", status = "playing",
                        hex = new RoomHexCoordinates { x = random.Next(-18, 19), z = random.Next(-12, 13) }
                    }).ToList()
                },
                timestampUtc = "host-only-time", label = "host-only-diagnostic"
            });
        }
        source.events.Add(new GameplayEvent
        {
            sequenceNumber = 601, kind = "token.move", currentHalf = 4, actorTokenKey = "Home:9",
            teamInAttack = "Home", attackHasPossession = true,
            movementPath = new GameplayMovementPath
            {
                countedForDistance = true,
                hexes = new List<RoomHexCoordinates> { new() {x = 1}, new() {x = 2}, new() {x = 3} }
            }
        });
        source.events.Add(Roll(602, false));
        source.events.Add(Roll(603, true));
        source.events.Add(new GameplayEvent { sequenceNumber = 604, kind = "input.key", label = "private selection" });

        var parts = OnlineFullTimeReport.Encode("match", 57, source.events);
        Assert.That(parts.Length, Is.GreaterThan(1));
        var receiver = new OnlineFullTimeReport.Receiver("match", 57);
        var wire = new OnlineGroundPassWire();
        foreach (var part in parts.Reverse())
        {
            var decoded = Receive(wire, part);
            Assert.That(receiver.Accept(decoded), Is.True);
            Assert.That(receiver.Accept(decoded), Is.True, "Duplicate parts must not duplicate history");
        }
        var report = Data();
        report.events = receiver.Events.ToList();
        Assert.That(report.events.Count, Is.EqualTo(603));
        Assert.That(report.events.All(e => e.timestampUtc == null && e.label == null));
        foreach (PostMatchPitchData.Half half in Enum.GetValues(typeof(PostMatchPitchData.Half)))
        foreach (PostMatchPitchData.Possession possession in Enum.GetValues(typeof(PostMatchPitchData.Possession)))
        {
            var expected = PostMatchPitchData.Build(source, half: half, possession: possession);
            var actual = PostMatchPitchData.Build(report, half: half, possession: possession);
            Assert.That(actual.Select(p => p.Key), Is.EqualTo(expected.Select(p => p.Key)));
            for (int i = 0; i < actual.Count; i++)
            {
                Assert.That(actual[i].Positions, Is.EqualTo(expected[i].Positions));
                Assert.That(actual[i].Movement, Is.EqualTo(expected[i].Movement));
            }
        }
        var luck = PostMatchLuckData.Build(report);
        Assert.That(luck.TotalRolls, Is.EqualTo(2));
        Assert.That(luck.ForcedRolls, Is.EqualTo(1));
        Assert.That(luck.AssessedRolls, Is.EqualTo(1));
        Assert.That(luck.ExpectedHome, Is.EqualTo(PostMatchLuckData.Build(source).ExpectedHome));
    }

    [Test]
    public void OlderDiceEventsPreserveLuckWithoutSendingDiagnosticModifiers()
    {
        var source = Data();
        source.events.Add(new GameplayEvent
        {
            kind = "dice.roll", sequenceNumber = 1, currentHalf = 2, actorTokenKey = "Away:9",
            dice = new GameplayDiceEvent
            {
                context = "high_pass_accuracy", roll = 5,
                modifiers = new Dictionary<string, string>
                {
                    ["highPassAttribute"] = "4", ["threshold"] = "8", ["diagnostic"] = "host-only"
                }
            }
        });
        var receiver = new OnlineFullTimeReport.Receiver("match", 57);
        Assert.That(receiver.Accept(OnlineFullTimeReport.Encode("match", 57, source.events).Single()), Is.True);
        var projected = Data();
        projected.events = receiver.Events.ToList();
        Assert.That(projected.events.Single().dice.modifiers, Is.Empty);
        var luck = PostMatchLuckData.Build(projected);
        Assert.That(luck.AssessedRolls, Is.EqualTo(1));
        Assert.That(luck.ExpectedHome, Is.EqualTo(PostMatchLuckData.Build(source).ExpectedHome));
        Assert.That(luck.ActualHome, Is.EqualTo(PostMatchLuckData.Build(source).ActualHome));
    }

    [Test]
    public void StaleOrCorruptedTransferCannotSupplyPartialHistory()
    {
        var message = OnlineFullTimeReport.Encode("old-match", 4, new[] { Roll(1, false) }).Single();
        var receiver = new OnlineFullTimeReport.Receiver("new-match", 5);
        Assert.That(receiver.Accept(message), Is.False);
        message.matchId = "new-match";
        Assert.That(receiver.Accept(message), Is.False);
        message.revision = 5;
        message.reportPartCount = 97;
        Assert.That(receiver.Accept(message), Is.False);
        message.reportPartCount = 1;
        message.reportChunk = "AAAA";
        Assert.That(receiver.Accept(message), Is.False);
        Assert.That(receiver.Failed, Is.True);
        Assert.That(receiver.Events, Is.Null);
    }

    [Test]
    public void InvalidHistoryCannotBePublished()
    {
        Assert.Throws<InvalidDataException>(() => OnlineFullTimeReport.Encode("match", 1,
            new[] { Roll(1, false), Roll(1, true) }));
    }

    private static OnlineGroundPassMessage Receive(OnlineGroundPassWire wire, OnlineGroundPassMessage message)
    {
        OnlineGroundPassMessage decoded = null;
        foreach (byte[] bytes in OnlineGroundPassWire.Encode(message))
            decoded = wire.Receive(bytes, bytes.Length);
        return decoded;
    }

    private static MatchManager.GameData Data() => new()
    {
        openingSetup = new MatchOpeningSetup { homeAttacksLeftToRight = true },
        extraTimeOpeningSetup = new MatchOpeningSetup { homeAttacksLeftToRight = false }
    };

    private static GameplayEvent Roll(int sequence, bool forced) => new()
    {
        kind = "dice.roll", sequenceNumber = sequence, currentHalf = 4, actorTokenKey = "Home:9",
        dice = new GameplayDiceEvent
        {
            context = "high_pass", roll = 5, overrideUsed = forced, jackpotEnabled = false,
            luck = new GameplayLuckModel { rule = "threshold", side = "Home", category = "Passing", label = "High Pass", skill = 4, threshold = 8 }
        }
    };
}
