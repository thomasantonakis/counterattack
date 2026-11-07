using NUnit.Framework;
using System.Linq;

public sealed class AttributeScoreFloorEditorTests
{
    [TestCase(0, -1)]
    [TestCase(0, -2)]
    [TestCase(1, -2)]
    public void SavingProbabilitiesFloorAttributeBeforeDice(int saving, int modifier)
        => Assert.That(ExpectedStatsCalculator.CalculateKeeperConcedeProbability(3, saving, modifier),
            Is.EqualTo(1f / 3f).Within(.00001f));

    [TestCase(false)]
    [TestCase(true)]
    public void ShotProbabilityUsesCurrentInjuredAttributeBeforeFloor(bool injured)
    {
        using var board = new GoalkeeperBoxEntryFixture(); board.Board(1);
        board.attacker.shooting = injured ? 2 : 1;
        if (injured) board.attacker.ReceiveInjury();
        board.awayGK.saving = 0;
        Assert.That(ExpectedStatsCalculator.CalculateShotGoalProbability(board.attacker, 2, null,
            board.awayGK, knownShooterRoll: 4), Is.EqualTo(.5f).Within(.00001f));
        Assert.That(ExpectedStatsCalculator.CalculateShotGoalProbability(board.attacker, 2, null,
            board.awayGK, knownShooterRoll: 1), Is.Zero, "Natural one remains off target.");
    }

    [TestCase(0, -1)]
    [TestCase(1, -2)]
    public void AttributeCheckProbabilityIncludesNumericSixAfterFloor(int attribute, int modifier)
        => Assert.That(ExpectedStatsCalculator.CalculateAttributeCheckSuccessProbability(attribute, modifier, 6),
            Is.EqualTo(1f / 6f).Within(.00001f));

    [Test]
    public void AerialProbabilityFloorsInitialAttribute()
        => Assert.That(ExpectedStatsCalculator.CalculateHeadToHeadAerialControlProbability(-1, 0),
            Is.EqualTo(.5f).Within(.00001f));

    [TestCase(0, -2)]
    [TestCase(1, -2)]
    public void GroundDuelDisplayFloorsBeforeAddingRoll(int skill, int modifier)
        => Assert.That(DuelRollGuidance.RollTotal(4, skill, modifier, false), Does.EndWith("= 4"));

    [Test]
    public void JackpotDisplayIsIndependentOfNegativeModifiers()
        => Assert.That(DuelRollGuidance.RollTotal(6, 0, -5, true), Is.EqualTo("Roll JACKPOT · Total JACKPOT"));

    [TestCase(0, -1)]
    [TestCase(1, -2)]
    public void SaveGuidanceUsesFlooredSaving(int saving, int modifier)
        => Assert.That(SingleRollResolutionPresentation.HeaderSaveGuidance(saving, modifier, 3), Does.Contain("Roll 3: loose ball"));

    [TestCase(0, -3, 4, false, 4)]
    [TestCase(1, -2, 4, false, 4)]
    [TestCase(2, -2, 4, false, 4)]
    [TestCase(3, -2, 4, false, 5)]
    [TestCase(0, 1, 4, false, 5)]
    [TestCase(0, -3, 6, true, 50)]
    public void FloorContractPreservesPositiveAttributesBonusesAndJackpots(int current, int modifier, int roll, bool jackpot, int expected)
        => Assert.That(GameplayScore.Total(current, roll, modifier, jackpot), Is.EqualTo(expected));

    [Test]
    public void DiveExpectationFloorsModifiedSavingBeforeDuel()
    {
        using var board = new GoalkeeperBoxEntryFixture(); board.Board(1);
        board.attacker.dribbling = 0;
        var expectation = ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(
            board.attacker, board.awayGK, 0, -2, defenderFoulThreshold: 0);
        Assert.That(expectation.attackerWinProbability, Is.EqualTo(61f / 144f).Within(.00001f));
        Assert.That(expectation.defenderWinProbability, Is.EqualTo(61f / 144f).Within(.00001f));
        Assert.That(expectation.tieProbability, Is.EqualTo(11f / 72f).Within(.00001f));
    }

    [Test]
    public void InjuredHeadingZeroKeepsZeroAtDistanceTwo()
    {
        using var board = new GoalkeeperBoxEntryFixture(); board.Board(1);
        board.defender.heading = 1; board.defender.ReceiveInjury();
        board.Place(board.defender, board.Hex(8)); board.ball.PlaceAtCell(board.Hex(10));
        Assert.That(board.match.shotManager.headerManager.GetHeaderAttribute(board.defender), Is.Zero);
        Assert.That(board.match.shotManager.headerManager.GetHeaderPenalty(board.defender), Is.Zero);
    }

    [Test]
    public void InjuryReducesGoalkeeperAttributesOnceWithZeroFloor()
    {
        using var board = new GoalkeeperBoxEntryFixture(); board.Board(1);
        board.awayGK.aerial = 2; board.awayGK.saving = 1; board.awayGK.handling = 0;
        board.awayGK.ReceiveInjury();
        Assert.That(board.awayGK.aerial, Is.EqualTo(1));
        Assert.That(board.awayGK.saving, Is.Zero);
        Assert.That(board.awayGK.handling, Is.Zero);
        Assert.That(GameplayScore.Total(board.awayGK.saving, 4, -2), Is.EqualTo(4));
        Assert.That(board.awayGK.saving, Is.Zero, "scoring does not mutate the current injured attribute");
    }

    [Test]
    public void BundledRulebookExplainsTheSharedPreRollFloor()
    {
        var viewer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RulesViewerEditorTools.PrefabPath)
            .GetComponent<RulesViewer>();
        Assert.That(viewer.entries.Any(e => e.text.text.Contains("max(current attribute + all signed modifiers, 0) + dice")), Is.True);
    }
}
