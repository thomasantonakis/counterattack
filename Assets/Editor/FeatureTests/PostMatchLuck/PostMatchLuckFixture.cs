using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLuckCapture(int side, bool home)
    {
        Board(side); if (!home) match.ChangePossession();
        var passer = home ? attacker : defender; passer.highPass = 1;
        var details = new Dictionary<string, string> { ["highPassAttribute"] = "1", ["threshold"] = "8" };
        Random.InitState(701);
        var state = Random.state;
        var expectedRoll = helper.DiceRoll(); var expectedState = Random.state;
        Random.state = state;
        var result = match.ResolveGameplayDiceRoll("high_pass_accuracy", actor: passer, details: details);
        Assert.That(result.roll, Is.EqualTo(expectedRoll._roll)); Assert.That(result.isJackpot, Is.EqualTo(expectedRoll._isJackPot));
        Assert.That(Random.state, Is.EqualTo(expectedState), "Capturing luck must consume no additional random values.");
        passer.highPass = 9; details["highPassAttribute"] = "9";
        var recorded = match.gameData.events.Last(e => e.kind == "dice.roll");
        Assert.That(recorded.dice.luck.skill, Is.EqualTo(1)); Assert.That(recorded.dice.luck.side, Is.EqualTo(home ? "Home" : "Away"));
        var copy = JsonConvert.DeserializeObject<MatchManager.GameData>(JsonConvert.SerializeObject(match.gameData));
        var report = PostMatchLuckData.Build(copy);
        Assert.That(report.Checks.Count, Is.EqualTo(1));
        Assert.That(report.ExpectedHome, Is.EqualTo(home ? 1d / 12 : 11d / 12).Within(1e-9));
        match.helperFunctions = null;
        match.ResolveGameplayDiceRoll("high_pass_accuracy", actor: passer, details: new() { ["highPassAttribute"] = "1", ["threshold"] = "8" });
        Assert.That(match.gameData.events.Last(e => e.kind == "dice.roll").dice.luckJackpotEnabled, Is.False);
        Assert.That(PostMatchLuckData.Build(match.gameData).Checks.Last().ExpectedHome, Is.EqualTo(home ? 0 : 1));
        yield return null;
    }

    public IEnumerator RunLuckAerialCapture(int side)
    {
        Board(side); Place(attacker, Hex(0)); Place(defender, Hex(2 * side)); ball.PlaceAtCell(Hex(side));
        attacker.heading = 4; defender.heading = 4;
        var header = match.shotManager.headerManager;
        header.attackerWillJump.Add(attacker); header.defenderWillJump.Add(defender);
        header.tokenRolling = attacker; header.PerformHeaderRoll((RollInputOverride?)null);
        header.tokenRolling = defender; header.PerformHeaderRoll((RollInputOverride?)null);
        var rolls = match.gameData.events.Where(e => e.kind == "dice.roll").ToArray();
        Assert.That(rolls.Length, Is.EqualTo(2)); Assert.That(rolls[0].dice.luck.contestId, Is.EqualTo(rolls[1].dice.luck.contestId));
        Assert.That(rolls[0].dice.luck.contenders.Count, Is.EqualTo(2));
        Assert.That(rolls[0].dice.luck.contenders.Select(c => c.tokenKey), Is.EquivalentTo(new[] { "Home:9", "Away:8" }));
        var report = PostMatchLuckData.Build(match.gameData);
        Assert.That(report.Checks.Count, Is.EqualTo(1)); Assert.That(report.AssessedRolls, Is.EqualTo(2));
        Assert.That(report.ExpectedHome, Is.EqualTo(.5).Within(1e-9));
        yield return null;
    }

    public IEnumerator RunLuckGroundCapture(int side)
    {
        yield return RunRepositionHoverOwnership(side);
        var rolls = match.gameData.events.Where(e => e.dice?.context is "movement_tackle_defender" or "movement_tackle_attacker").ToArray();
        Assert.That(rolls.Length, Is.EqualTo(2));
        Assert.That(rolls.All(e => e.dice.luck?.rule == "ground_duel"), Is.True);
        Assert.That(rolls[0].dice.luck.contestId, Is.EqualTo(rolls[1].dice.luck.contestId));
        Assert.That(rolls[0].dice.luck.defenderSkill, Is.EqualTo(0), "Outside-box GK foot tackles keep their actual zero skill.");
        Assert.That(PostMatchLuckData.Build(match.gameData).Checks, Is.Empty, "Rigged duel rolls must be excluded from manager luck.");
    }

    public IEnumerator RunLuckRecoveryCapture(int side, bool headerPass)
    {
        Board(side);
        if (headerPass)
        {
            var header = match.shotManager.headerManager;
            header.interceptingDefender = defender;
            yield return header.StartCoroutine((IEnumerator)Invoke(header, "PresentHeaderInterception", (object)null));
            var entry = match.gameData.events.Last(e => e.kind == "dice.roll");
            Assert.That(entry.actorTokenKey, Is.EqualTo("Away:8"), "The interceptor owns the roll, rather than the player who last passed.");
            Assert.That(entry.dice.luck.skill, Is.EqualTo(defender.tackling));
            Assert.That(PostMatchLuckData.Build(match.gameData).ExpectedHome, Is.EqualTo(5d / 6).Within(1e-9));
        }
        else
        {
            awayGK.saving = 7; awayGK.tackling = 0;
            loose.potentialInterceptor = awayGK;
            Invoke(loose, "BeginRecoveryPresentation", true, -2);
            yield return loose.StartCoroutine((IEnumerator)Invoke(loose, "PresentInterceptionRoll", (object)null));
            var entry = match.gameData.events.Last(e => e.kind == "dice.roll");
            Assert.That(entry.dice.luck.skill, Is.EqualTo(7)); Assert.That(entry.dice.luck.modifier, Is.EqualTo(-2));
            Assert.That(PostMatchLuckData.Build(match.gameData).ExpectedHome, Is.EqualTo(2d / 3).Within(1e-9));
        }
    }
}
