using NUnit.Framework;

public sealed class DuelResolutionPresentationEditorTests
{
    [TestCase(true, true, true)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    public void DiceKeepTeamColorsAndUseBlackOutlineWhenTheSurfaceConflicts(bool lightDie, bool lightSurface, bool blackBorder)
    {
        var surface = new UnityEngine.GameObject("Surface", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image));
        try
        {
            var navy = new UnityEngine.Color(.1f, .2f, .3f);
            surface.GetComponent<UnityEngine.UI.Image>().color = lightSurface ? UnityEngine.Color.white : navy;
            var root = new UnityEngine.GameObject("Die", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Outline), typeof(DuelDieView));
            root.transform.SetParent(surface.transform, false);
            var face = new UnityEngine.GameObject("Face", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image));
            face.transform.SetParent(root.transform, false);
            var die = root.GetComponent<DuelDieView>();
            die.background = root.GetComponent<UnityEngine.UI.Image>(); die.border = root.GetComponent<UnityEngine.UI.Outline>();
            die.face = face.GetComponent<UnityEngine.UI.Image>();
            var primary = lightDie ? UnityEngine.Color.white : navy;
            var secondary = UnityEngine.Color.red;
            die.SetTeamColors(primary, secondary);
            Assert.That(die.border.effectColor, Is.EqualTo(blackBorder ? UnityEngine.Color.black : secondary));
            Assert.That(die.background.color, Is.EqualTo(primary));
            Assert.That(die.face.color, Is.EqualTo(secondary));
        }
        finally { UnityEngine.Object.DestroyImmediate(surface); }
    }

    [Test]
    public void ExpectedDribblesIncludeFoulsAndKeepContinuationSeparate()
    {
        var expectation = new ExpectedStatsCalculator.GroundDuelExpectation(.4f, .3f, .1f, .2f);
        Assert.That(expectation.xDribbles, Is.EqualTo(.6166667f).Within(.00001f));
        Assert.That(expectation.xTackles, Is.EqualTo(.3833333f).Within(.00001f));
        Assert.That(expectation.xDribbles + expectation.xTackles, Is.EqualTo(1f).Within(.00001f));
        Assert.That(expectation.dribbleContinuationProbability, Is.EqualTo(.4166667f).Within(.00001f));
    }

    [TestCase(4, 5, 0, false, "Roll 4 + 5 = 9")]
    [TestCase(1, 6, 1, false, "Roll 1 + 6 + 1 = 8")]
    [TestCase(6, 5, 0, true, "Roll JACKPOT · Total JACKPOT")]
    public void RollCopyStartsWithTheActualRoll(int roll, int skill, int bonus, bool jackpot, string expected)
        => Assert.That(DuelRollGuidance.RollTotal(roll, skill, bonus, jackpot), Is.EqualTo(expected));

    [Test]
    public void ConditionalChancesUseRemainingD12FacesWithoutRecoveryWeighting()
    {
        var chances = ExpectedStatsCalculator.CalculateGroundDuelAfterDefenderRoll(3, 5, false);
        Assert.That(chances.attackerWinProbability, Is.EqualTo(2f / 3f).Within(.00001f));
        Assert.That(chances.tieProbability, Is.EqualTo(1f / 6f).Within(.00001f));
        Assert.That(chances.defenderWinProbability, Is.EqualTo(1f / 6f).Within(.00001f));
        Assert.That(DuelRollGuidance.LiveOutcomes(chances),
            Is.EqualTo("Dribble: 66.7% - Loose ball: 16.7% - Successful tackle: 16.7%"));
    }

    [Test]
    public void DefenderJackpotCanOnlyBeTiedByAttackerJackpot()
    {
        var chances = ExpectedStatsCalculator.CalculateGroundDuelAfterDefenderRoll(5, 50, false);
        Assert.That(chances.attackerWinProbability, Is.Zero);
        Assert.That(chances.tieProbability, Is.EqualTo(1f / 12f).Within(.00001f));
        Assert.That(chances.defenderWinProbability, Is.EqualTo(11f / 12f).Within(.00001f));
    }

    [Test]
    public void KnownFoulDoesNotPresentWinningChances()
    {
        var chances = ExpectedStatsCalculator.CalculateGroundDuelAfterDefenderRoll(5, 5, true);
        Assert.That(chances.defenderFoulProbability, Is.EqualTo(1f));
        Assert.That(chances.attackerWinProbability + chances.tieProbability + chances.defenderWinProbability, Is.Zero);
    }

    [TestCase(2, "Referee leniency: 2\n2+: yellow card (83.3%)\n1: no card (16.7%)")]
    [TestCase(3, "Referee leniency: 3\n3+: yellow card (66.7%)\n1–2: no card (33.3%)")]
    [TestCase(6, "Referee leniency: 6\n6+: yellow card (16.7%)\n1–5: no card (83.3%)")]
    public void CheckCopyShowsBothRangesAndActualProbabilities(int leniency, string expected)
        => Assert.That(DuelRollGuidance.ThresholdCheck("Referee leniency", leniency, "yellow card", "no card"), Is.EqualTo(expected));

    [TestCase(5, 8, false, false, "4+ to win; 3 ties")]
    [TestCase(5, 11, false, false, "JACKPOT to win; 6 ties")]
    [TestCase(5, 12, false, false, "JACKPOT to win")]
    [TestCase(5, 5, false, false, "Any roll wins")]
    [TestCase(5, 6, false, false, "2+ to win; 1 ties")]
    [TestCase(5, 50, true, false, "JACKPOT to tie")]
    [TestCase(5, 8, false, true, "Foul committed — dribbler roll still required")]
    public void GuidanceDescribesExactWinningAndTyingFaces(int dribbling, int total, bool jackpot, bool foul, string expected)
    {
        Assert.That(DuelRollGuidance.ForDribbler(dribbling, total, jackpot, foul), Is.EqualTo(expected));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AnimationRepresentsTheTwelveEligibleFaces(bool jackpots)
    {
        int[] counts = new int[7];
        int stars = 0;
        for (int step = 0; step < 12; step++)
        {
            var face = DuelDieView.AnimationFace(step, jackpots);
            if (face.isJackpot) stars++;
            else counts[face.roll]++;
        }
        for (int number = 1; number <= 5; number++) Assert.That(counts[number], Is.EqualTo(2));
        Assert.That(counts[6], Is.EqualTo(jackpots ? 1 : 2));
        Assert.That(stars, Is.EqualTo(jackpots ? 1 : 0));
    }
}
