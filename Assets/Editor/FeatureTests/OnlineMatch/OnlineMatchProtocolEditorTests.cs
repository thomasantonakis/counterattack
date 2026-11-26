using System;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;

[Category("OnlineMatchProtocol")]
public sealed class OnlineMatchProtocolEditorTests
{
    private static OnlineCompatibility Version(string protocol = "1", string rules = "rules-1", string catalog = "cards-1")
        => new(protocol, rules, catalog);

    private static OnlineMatchProtocol Match(string host = "home-account")
    {
        var match = new OnlineMatchProtocol("match-1", "home-account", "away-account", host, Version());
        Assert.That(match.BindAuthenticatedAccount("home-account", "home-connection", Version()), Is.True);
        Assert.That(match.BindAuthenticatedAccount("away-account", "away-connection", Version()), Is.True);
        match.PublishDecision("pass-choice", OnlineTeam.Home, new[] { "pass:token-7:hex-3-2", "cancel-pass" });
        return match;
    }

    private static OnlineMatchCommand Command(OnlineMatchProtocol match, long sequence = 1,
        string action = "pass:token-7:hex-3-2", string decision = "pass-choice", long? revision = null,
        string matchId = "match-1") => new(matchId, sequence, revision ?? match.Revision, decision, action);

    [TestCase("home-account")]
    [TestCase("away-account")]
    public void HostRoleDoesNotGrantControlOfTheOtherTeam(string host)
    {
        var match = Match(host);
        long before = match.Revision;
        Assert.That(match.Admit("away-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(match.Revision, Is.EqualTo(before));
        Assert.That(match.Admit("home-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [Test]
    public void OwnershipTransfersToTheDefenderForAnInterruption()
    {
        var match = Match();
        Assert.That(match.Admit("home-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        match.PublishDecision("interception-roll", OnlineTeam.Away, new[] { "roll:interception-1" });
        var roll = Command(match, 2, "roll:interception-1", "interception-roll");
        Assert.That(match.Admit("home-connection", roll).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(match.Admit("away-connection", roll).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [Test]
    public void DuplicateConfirmationAcknowledgesTheOriginalAdmissionWithoutReopeningTheDecision()
    {
        var match = Match();
        var command = Command(match);
        var original = match.Admit("home-connection", command);
        var duplicate = match.Admit("home-connection", command);
        Assert.That(duplicate.Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(duplicate.Revision, Is.EqualTo(original.Revision));
        Assert.That(match.Revision, Is.EqualTo(original.Revision));
        Assert.That(match.HasPendingDecision, Is.False);
        Assert.That(match.Admit("home-connection", Command(match, 2)).Status, Is.EqualTo(OnlineCommandStatus.NotReady));
    }

    [Test]
    public void ReusingASequenceForADifferentActionIsRejected()
    {
        var match = Match();
        var first = Command(match);
        match.Admit("home-connection", first);
        Assert.That(match.Admit("home-connection", Command(match, action: "cancel-pass", revision: first.Revision)).Status,
            Is.EqualTo(OnlineCommandStatus.SequenceConflict));
    }

    [TestCase("protocol")]
    [TestCase("rules")]
    [TestCase("catalog")]
    public void IncompatibleReconnectCannotEvictTheWorkingConnection(string component)
    {
        var match = Match();
        var incompatible = Version(component == "protocol" ? "2" : "1",
            component == "rules" ? "rules-2" : "rules-1", component == "catalog" ? "cards-2" : "cards-1");
        Assert.That(match.BindAuthenticatedAccount("home-account", "replacement", incompatible), Is.False);
        Assert.That(match.TryGetTeam("home-connection", out var team), Is.True);
        Assert.That(team, Is.EqualTo(OnlineTeam.Home));
        Assert.That(match.BothConnected, Is.True);
    }

    [Test]
    public void UnknownAccountsAndBorrowedConnectionIdsCannotClaimASeat()
    {
        var match = Match();
        Assert.That(match.BindAuthenticatedAccount("intruder", "third-connection", Version()), Is.False);
        Assert.That(match.BindAuthenticatedAccount("away-account", "home-connection", Version()), Is.False);
        Assert.That(match.BindAuthenticatedAccount("home-account", "home-connection", Version()), Is.True);
        Assert.That(match.Admit("third-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
    }

    [Test]
    public void ReconnectKeepsTeamAndReceiptsButInvalidatesOldTraffic()
    {
        var match = Match();
        var command = Command(match);
        var accepted = match.Admit("home-connection", command);
        match.Disconnect("home-connection");
        Assert.That(match.BothConnected, Is.False);
        Assert.That(match.BindAuthenticatedAccount("home-account", "home-new-connection", Version()), Is.True);
        Assert.That(match.TryGetTeam("home-new-connection", out var team), Is.True);
        Assert.That(team, Is.EqualTo(OnlineTeam.Home));
        Assert.That(match.Admit("home-new-connection", command).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(match.Revision, Is.EqualTo(accepted.Revision));
        Assert.That(match.Admit("home-connection", command).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(match.BindAuthenticatedAccount("away-account", "home-connection", Version()), Is.False);
        match.Disconnect("home-connection"); // Delayed old disconnect cannot evict the new connection.
        Assert.That(match.BothConnected, Is.True);
    }

    [Test]
    public void LiveRebindAlsoRetiresThePreviousConnection()
    {
        var match = Match();
        Assert.That(match.BindAuthenticatedAccount("home-account", "new-connection", Version()), Is.True);
        Assert.That(match.Admit("home-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(match.Admit("new-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [Test]
    public void DisconnectedOpponentBlocksNewCommandsWithoutLosingThePendingDecision()
    {
        var match = Match();
        var command = Command(match);
        long revision = match.Revision;
        match.Disconnect("away-connection");
        Assert.That(match.Admit("home-connection", command).Status, Is.EqualTo(OnlineCommandStatus.Disconnected));
        Assert.That(match.Revision, Is.EqualTo(revision));
        Assert.That(match.HasPendingDecision, Is.True);
        match.BindAuthenticatedAccount("away-account", "away-new", Version());
        Assert.That(match.Admit("home-connection", command).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [TestCase("wrong-match", "pass-choice", "pass:token-7:hex-3-2", OnlineCommandStatus.WrongMatch)]
    [TestCase("match-1", "old-choice", "pass:token-7:hex-3-2", OnlineCommandStatus.WrongDecision)]
    [TestCase("match-1", "pass-choice", "debug:rig-roll-6", OnlineCommandStatus.IllegalAction)]
    public void InvalidRequestsDoNotMutateOrConsumeTheDecision(string matchId, string decision, string action, OnlineCommandStatus status)
    {
        var match = Match();
        long revision = match.Revision;
        Assert.That(match.Admit("home-connection", Command(match, action: action, decision: decision, matchId: matchId)).Status,
            Is.EqualTo(status));
        Assert.That(match.Revision, Is.EqualTo(revision));
        Assert.That(match.Admit("home-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [Test]
    public void AStaleTargetCannotBeAppliedToANewRevisionEvenWithTheSameDecisionLabel()
    {
        var match = Match();
        var old = Command(match);
        match.PublishDecision("pass-choice", OnlineTeam.Home, new[] { "pass:token-7:hex-3-2" });
        Assert.That(match.Admit("home-connection", old).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(match.Admit("home-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("bad\nid")]
    public void MalformedActionIdsAreRejected(string action)
    {
        var match = Match();
        Assert.That(match.Admit("home-connection", Command(match, action: action)).Status, Is.EqualTo(OnlineCommandStatus.InvalidCommand));
        Assert.That(match.HasPendingDecision, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonpositiveSequencesAreRejected(long sequence)
    {
        var match = Match();
        Assert.That(match.Admit("home-connection", Command(match, sequence)).Status, Is.EqualTo(OnlineCommandStatus.InvalidCommand));
    }

    [Test]
    public void OversizedIdsAndNegativeRevisionsAreRejected()
    {
        var match = Match();
        Assert.That(match.Admit("home-connection", Command(match, action: new string('x', 129))).Status,
            Is.EqualTo(OnlineCommandStatus.InvalidCommand));
        Assert.That(match.Admit("home-connection", Command(match, revision: -1)).Status,
            Is.EqualTo(OnlineCommandStatus.InvalidCommand));
        Assert.That(match.Admit("home-connection", null).Status, Is.EqualTo(OnlineCommandStatus.InvalidCommand));
    }

    [Test]
    public void AnEvictedReceiptStillCannotBeReexecuted()
    {
        var match = Match();
        OnlineMatchCommand first = null;
        for (long sequence = 1; sequence <= 70; sequence++)
        {
            match.PublishDecision("pass-choice", OnlineTeam.Home, new[] { "pass:token-7:hex-3-2" });
            var command = Command(match, sequence);
            first ??= command;
            Assert.That(match.Admit("home-connection", command).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        long revision = match.Revision;
        Assert.That(match.Admit("home-connection", first).Status, Is.EqualTo(OnlineCommandStatus.StaleSequence));
        Assert.That(match.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void InvalidDecisionPublicationLeavesThePreviousDecisionIntact()
    {
        var match = Match();
        long revision = match.Revision;
        Assert.Throws<ArgumentException>(() => match.PublishDecision("next", OnlineTeam.Away, new[] { "valid", "bad id" }));
        Assert.That(match.Revision, Is.EqualTo(revision));
        Assert.That(match.Admit("home-connection", Command(match)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [Test]
    public void JsonRoundTripPreservesSemanticCommandAndStillChecksTheTransportSender()
    {
        var match = Match();
        string json = JsonConvert.SerializeObject(Command(match));
        var decoded = JsonConvert.DeserializeObject<OnlineMatchCommand>(json);
        Assert.That(match.Admit("away-connection", decoded).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(match.Admit("home-connection", decoded).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    [Test]
    public void MatchCreationRequiresDistinctParticipantsAndAParticipatingHost()
    {
        Assert.Throws<ArgumentException>(() => new OnlineMatchProtocol("match", "same", "same", "same", Version()));
        Assert.Throws<ArgumentException>(() => new OnlineMatchProtocol("match", "home", "away", "third", Version()));
        Assert.Throws<ArgumentException>(() => new OnlineCompatibility("", "rules", "cards"));
    }
}
