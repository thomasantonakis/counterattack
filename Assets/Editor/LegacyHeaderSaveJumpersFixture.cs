using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunNativeHeaderSaveJumpers(bool spill, int occupied)
    {
        var p = LoadLegacyRoster(); var header = match.shotManager.headerManager; var shot = match.shotManager;
        var origin = Hex(13, 1); var keeperHex = Hex(16); var target = Hex(19, -2);
        var path = Enumerable.Range(1, 5).Select(n => loose.CalculateDirectionalTarget(keeperHex, 1, n)).ToList();
        var adjacent = path[2].GetNeighbors(grid).First(h => h != null && !h.isOutOfBounds && !path.Contains(h) && h != origin);
        var reserved = new HashSet<HexCell>(path.Concat(new[] { origin, keeperHex, target, adjacent, Hex(10, 5), Hex(10, -5) }));
        foreach (var h in reserved.ToArray()) foreach (var n in h.GetNeighbors(grid)) if (n != null) reserved.Add(n);
        foreach (var token in p.Values.Where(t => t.isPlaying))
            if (reserved.Contains(token.currentHex)) Place(token, grid.cells.Cast<HexCell>().First(h => h != null && !h.isOutOfBounds && h.isInGoal == 0
                && !reserved.Contains(h) && !h.isAttackOccupied && !h.isDefenseOccupied && h.GetOccupyingToken() == null));
        Place(p["Yaneva"], origin); Place(p["Kalla"], occupied == 2 ? path[3] : Hex(10, 5));
        Place(p["Soares"], occupied > 0 ? path[2] : adjacent); Place(p["Gilbert"], Hex(10, -5)); Place(p["Kuzmic"], keeperHex);
        p["Kuzmic"].saving = spill ? 6 : 4; p["Kuzmic"].handling = 1;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header); EnableForAuthoredPresentation(shot); EnableForAuthoredPresentation(loose);
        Subscribe(movement); Subscribe(match.finalThirdManager);
        header.attackerWillJump.AddRange(new[] { p["Yaneva"], p["Kalla"] }); header.defenderWillJump.AddRange(new[] { p["Soares"], p["Gilbert"] });
        ball.PlaceAtCell(origin); match.ClearLastTokenChain(); match.SetLastToken(p["Yaneva"]); match.SetHangingPass("shot");
        // This is the original handoff audit: power 10 is the input supplied by
        // the resolved aerial challenge, not a flag forcing the save outcome.
        shot.ProcessHeaderAtGoal(p["Yaneva"], 10, target, attackerHeaderRoll: 6); header.CleanUpHeader();
        Assert.That(header.attackerWillJump.Count + header.defenderWillJump.Count, Is.EqualTo(4));
        yield return ResolveNativeHeaderSave(6);
        if (spill) yield return ResolveNativeHeaderHandling(6);
        Assert.That(p["Kuzmic"].currentHex, Is.SameAs(keeperHex));
        yield return RollNativeHeaderScatter(2, 3);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (loose.isActivated)
        {
            Assert.That(loose.isWaitingForInterceptionRoll, Is.False, "Every aerial jumper remains excluded from recovery throughout the real save/scatter continuation.");
            Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(10)); yield return null;
        }
        Assert.That(ball.GetCurrentHex(), Is.SameAs(path[2 + occupied]), "The scatter extends past each jumped occupant, without extending past an adjacent jumper.");
        Assert.That(ball.GetCurrentHex().GetOccupyingToken(), Is.Null);
    }
}
