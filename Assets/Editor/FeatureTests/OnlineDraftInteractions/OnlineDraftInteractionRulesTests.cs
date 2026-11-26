using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CounterAttack.Online;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

[Category("OnlineDraftInteractions")]
public sealed class OnlineDraftInteractionRulesTests
{
    private OnlineMatchProtocol protocol;
    private OnlineDraftAuthority draft;

    private void Create(string keepers, string outfield, bool hostAway, Func<int> roll = null, Func<double> clock = null)
    {
        var compatibility = new OnlineCompatibility("draft-interactions", "test-rules", "test-catalog");
        protocol = new OnlineMatchProtocol("draft-interactions", "home", "away", hostAway ? "away" : "home", compatibility);
        protocol.BindAuthenticatedAccount("home", "h", compatibility);
        protocol.BindAuthenticatedAccount("away", "a", compatibility);
        var settings = OnlineSetupAgreementEditorTests.Valid();
        settings.squadSize = "18";
        settings.gkDraft = keepers;
        settings.draft = outfield;
        draft = new OnlineDraftAuthority(protocol, settings, OnlineDraftCatalog.Load(), _ => 0, roll, clock);
    }

    private OnlineCommandStatus Submit(OnlineTeam team, OnlineDraftIntent intent, string id = null, int slot = 0,
        long? revision = null, int otherSlot = 0)
    {
        var state = draft.Capture();
        return draft.Submit(team == OnlineTeam.Home ? "h" : "a", new OnlineDraftRequest
        {
            matchId = state.matchId, revision = revision ?? state.revision, intent = intent, cardId = id,
            slot = slot, otherSlot = otherSlot, sequence = (team == OnlineTeam.Home ? state.homeSequence : state.awaySequence) + 1
        }).Status;
    }

    [TestCase(false, "Regular")]
    [TestCase(true, "Free Regular")]
    public void FreeKeeperRollMatchesNextOutfieldRoll(bool awayWins, string outfield)
    {
        double now = 0;
        var dice = new Queue<int>();
        Create("Free", outfield, awayWins, dice.Dequeue, () => now);
        foreach (string selection in new[] { "goalkeepers", "outfielders" })
        {
            var starting = draft.Capture();
            Assert.That((starting.selection, starting.phase, starting.turn, starting.rollRound, starting.homeRoll, starting.awayRoll),
                Is.EqualTo((selection, "roll", OnlineTeam.Home, 1, 0, 0)));
            Assert.That(Submit(OnlineTeam.Away, OnlineDraftIntent.Roll), Is.EqualTo(OnlineCommandStatus.WrongTeam));

            dice.Enqueue(3);
            dice.Enqueue(3);
            Assert.That(draft.AdvanceAutomaticRoll(), Is.False);
            now += 4.5;
            Assert.That(draft.AdvanceAutomaticRoll(), Is.False);
            now += .5;
            Assert.That(draft.AdvanceAutomaticRoll(), Is.True);
            Assert.That(draft.Capture().turn, Is.EqualTo(OnlineTeam.Away));
            Assert.That(draft.AdvanceAutomaticRoll(), Is.False);
            now += 5;
            Assert.That(draft.AdvanceAutomaticRoll(), Is.True);
            Assert.That(draft.Capture().phase, Is.EqualTo("roll-result"));
            now += 4.5;
            Assert.That(draft.AdvanceRollResult(), Is.False);
            now += .5;
            Assert.That(draft.AdvanceRollResult(), Is.True);
            var tied = draft.Capture();
            Assert.That((tied.phase, tied.rollRound, tied.turn, tied.homeRoll, tied.awayRoll),
                Is.EqualTo(("roll", 2, OnlineTeam.Home, 0, 0)));

            dice.Enqueue(awayWins ? 1 : 6);
            dice.Enqueue(awayWins ? 6 : 1);
            Assert.That(Submit(OnlineTeam.Home, OnlineDraftIntent.Roll), Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(Submit(OnlineTeam.Away, OnlineDraftIntent.Roll), Is.EqualTo(OnlineCommandStatus.Accepted));
            now += 4.5;
            Assert.That(draft.AdvanceRollResult(), Is.False);
            now += .5;
            Assert.That(draft.AdvanceRollResult(), Is.True);
            var decided = draft.Capture();
            var winner = awayWins ? OnlineTeam.Away : OnlineTeam.Home;
            Assert.That((decided.phase, decided.turn, decided.firstPicker), Is.EqualTo(("pick", winner, winner)));

            if (selection == "goalkeepers")
                for (int pick = 0; pick < 4; pick++)
                {
                    var state = draft.Capture();
                    var roster = state.turn == OnlineTeam.Home ? state.home : state.away;
                    Assert.That(Submit(state.turn, OnlineDraftIntent.Pick, state.available[0], roster[0] == null ? 1 : 12),
                        Is.EqualTo(OnlineCommandStatus.Accepted));
                }
        }
        Assert.That(dice, Is.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FreePicksRejectWrongTeamAndRequireExactEmptySlot(bool keeper)
    {
        var rolls = new Queue<int>(new[] { 2, 6 });
        Create(keeper ? "Free" : "Deal", "Free Regular", false, rolls.Dequeue);
        Assert.That(Submit(OnlineTeam.Home, OnlineDraftIntent.Roll), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(OnlineTeam.Away, OnlineDraftIntent.Roll), Is.EqualTo(OnlineCommandStatus.Accepted));
        var initial = draft.Capture();
        int destination = keeper ? 12 : 18;
        int wrongSlot = keeper ? 2 : 1;
        string id = initial.available[0];
        Assert.That(Submit(OnlineTeam.Home, OnlineDraftIntent.Pick, id, destination), Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Submit(OnlineTeam.Away, OnlineDraftIntent.Pick, id, wrongSlot), Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(draft.Capture().revision, Is.EqualTo(initial.revision));
        Assert.That(Submit(OnlineTeam.Away, OnlineDraftIntent.Pick, id, destination), Is.EqualTo(OnlineCommandStatus.Accepted));
        var next = draft.Capture();
        Assert.That(next.away[destination - 1].id, Is.EqualTo(id));
        Assert.That(next.turn, Is.EqualTo(OnlineTeam.Home));
        Assert.That(next.available, Does.Not.Contain(id));
        Assert.That(Submit(OnlineTeam.Home, OnlineDraftIntent.Pick, id, destination), Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(Submit(OnlineTeam.Home, OnlineDraftIntent.Pick, next.available[0], destination), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(OnlineTeam.Away, OnlineDraftIntent.Pick, next.available[1], destination), Is.EqualTo(OnlineCommandStatus.IllegalAction));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ArcadeConcurrencyKeepsDuplicateCardsAndRejectsInvalidatedRevisions(bool awayFirst)
    {
        Create("Arcade", "Arcade", awayFirst, () => throw new InvalidOperationException("Arcade must enter picks directly"));
        var first = awayFirst ? OnlineTeam.Away : OnlineTeam.Home;
        var second = awayFirst ? OnlineTeam.Home : OnlineTeam.Away;
        var initial = draft.Capture();
        string keeper = initial.available[0];
        Assert.That((initial.phase, initial.selection, initial.repeatable), Is.EqualTo(("pick", "goalkeepers", true)));
        Assert.That(Submit(first, OnlineDraftIntent.Pick, keeper, 1), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(first, OnlineDraftIntent.Pick, keeper, 12, initial.revision), Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(Submit(first, OnlineDraftIntent.Pick, keeper, 12), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(second, OnlineDraftIntent.Pick, keeper, 1, initial.revision), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(draft.Capture().selection, Is.EqualTo("goalkeepers"));
        Assert.That(Submit(second, OnlineDraftIntent.Pick, keeper, 12), Is.EqualTo(OnlineCommandStatus.Accepted));
        var outfield = draft.Capture();
        Assert.That((outfield.phase, outfield.selection, outfield.repeatable), Is.EqualTo(("pick", "outfielders", true)));
        Assert.That(outfield.home.Concat(outfield.away).Where(card => card != null).Select(card => card.id), Is.EqualTo(Enumerable.Repeat(keeper, 4)));
        string player = outfield.available[0];
        Assert.That(Submit(first, OnlineDraftIntent.Pick, player, 18, initial.revision), Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(Submit(first, OnlineDraftIntent.Pick, player, 18), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(second, OnlineDraftIntent.Pick, player, 18, outfield.revision), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(first, OnlineDraftIntent.Pick, player, 18), Is.EqualTo(OnlineCommandStatus.IllegalAction));
        var beforeSwap = draft.Capture();
        Assert.That(Submit(first, OnlineDraftIntent.Swap, slot: 18, otherSlot: 17), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(second, OnlineDraftIntent.Pick, player, 17, beforeSwap.revision), Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(draft.Capture().available, Does.Contain(player));

        Assert.That(OnlineSpectatorPregameWire.TryEncode(outfield.matchId, 1, outfield.settings, null, draft.Capture(), out var payload), Is.True);
        var publicDraft = JObject.Parse(Encoding.UTF8.GetString(payload))["draft"];
        Assert.That((string)publicDraft["selection"], Is.EqualTo("outfielders"));
        Assert.That((bool)publicDraft["repeatable"], Is.True);
        Assert.That(publicDraft["available"], Is.Null);
        Assert.That(publicDraft["cards"], Is.Null);
        Assert.That(publicDraft["home"].OfType<JObject>().Any(card => (string)card["id"] == player), Is.True);
    }
}
