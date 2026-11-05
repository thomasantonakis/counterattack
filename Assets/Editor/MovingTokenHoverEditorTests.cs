using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class MovingTokenHoverEditorTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(false)]
    [TestCase(true)]
    public void NewPossessionBallCarrierShowsBothSharedZoneHexesBeforeMoving(bool alreadyRunning)
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var match = replay.match;
        var movement = match.movementPhaseManager;
        var origin = replay.Hex(new RoomHexCoordinates { x = 0, z = 0 });
        var defenderHex = origin.GetNeighbors(match.hexGrid).First(h => h != null);
        foreach (var hex in match.hexGrid.cells.Cast<HexCell>().Where(h => h != null))
        {
            hex.occupyingToken = null;
            hex.isAttackOccupied = hex.isDefenseOccupied = false;
        }
        var carrier = replay.tokens["Away:10"];
        var defender = replay.tokens["Home:9"];
        carrier.isAttacker = true; defender.isAttacker = false;
        typeof(PlayerToken).GetProperty("currentHex").SetValue(carrier, origin);
        typeof(PlayerToken).GetProperty("currentHex").SetValue(defender, defenderHex);
        origin.occupyingToken = carrier; origin.isAttackOccupied = true;
        defenderHex.occupyingToken = defender; defenderHex.isDefenseOccupied = true;
        typeof(Ball).GetField("currentCell", Private).SetValue(match.ball, origin);
        movement.headerManager = movement.gameObject.AddComponent<HeaderManager>();
        movement.gameInputManager = movement.gameObject.AddComponent<GameInputManager>();
        movement.selectedToken = carrier;
        movement.isAwaitingHexDestination = true;
        movement.isMovementPhaseAttack = true;
        movement.isDribblerRunning = alreadyRunning;
        movement.remainingDribblerPace = 1; // Tests steal warnings independently of nutmeg availability.
        var neighbors = origin.GetNeighbors(match.hexGrid).Where(h => h != null && h != defenderHex).ToArray();
        var shared = neighbors.Intersect(defenderHex.GetNeighbors(match.hexGrid)).ToArray();
        Assert.That(shared.Length, Is.EqualTo(2));
        var material = new Material(Shader.Find("Sprites/Default"));
        try
        {
            foreach (var hex in neighbors)
            {
                hex.hexRenderer = hex.gameObject.AddComponent<MeshRenderer>();
                hex.hexRenderer.sharedMaterial = material;
            }
            movement.HighlightValidMovementHexes(carrier, 1);
            var reason = typeof(HexCell).GetField("currentHighlightReason", Private);
            foreach (var hex in shared)
                Assert.That(reason.GetValue(hex), Is.EqualTo("PaceRisk"),
                    "Both empty hexes shared with the defender's ZOI must be orange before the first step.");
            foreach (var hex in neighbors.Except(shared))
                Assert.That(reason.GetValue(hex), Is.EqualTo("PaceAvailable"));
            Assert.That(movement.isDribblerRunning, Is.EqualTo(alreadyRunning),
                "Displaying threats must not commit movement or change running state.");
        }
        finally { Object.DestroyImmediate(material); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MovementHoverPreservesDestinationWhileCollectorIsAnimating(bool tokenMovementScope)
    {
        var root = new GameObject("Collector highlights"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<MovementPhaseManager>();
            var token = root.AddComponent<PlayerToken>();
            var hovered = root.AddComponent<HexCell>();
            manager.selectedToken = token;
            manager.isActivated = true;
            manager.isAwaitingHexDestination = true;
            manager.isPlayerMoving = !tokenMovementScope;
            var hoverField = typeof(MovementPhaseManager).GetField("hoveredMovementHex", Private);
            hoverField.SetValue(manager, hovered);
            using var movement = tokenMovementScope ? token.TrackMovement() : null;
            typeof(MovementPhaseManager).GetMethod("OnHoverReceived", Private)
                .Invoke(manager, new object[] { null, null });
            Assert.That(hoverField.GetValue(manager), Is.SameAs(hovered),
                "Hover must leave the chosen destination untouched until collection movement finishes.");
            // No grid is supplied: entering the refresh path would fail, so this
            // also checks that direct refreshes cannot rebuild highlights mid-move.
            typeof(MovementPhaseManager).GetField("lastMovementHighlightRange", Private).SetValue(manager, 5);
            Assert.DoesNotThrow(() => typeof(MovementPhaseManager)
                .GetMethod("RefreshMovementDestinationHighlights", Private).Invoke(manager, null));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MovingTokenDispatchesNoHoverAndStationaryTokensStillDispatch(bool previewOnly)
    {
        var root = new GameObject("Moving token hover fixture"); root.SetActive(false);
        int previews = 0, gameplay = 0;
        Action<PlayerToken, HexCell> onPreview = (_, _) => previews++;
        Action<PlayerToken, HexCell> onHover = (_, _) => gameplay++;
        GameInputManager.OnTokenPreviewHover += onPreview;
        GameInputManager.OnHover += onHover;
        try
        {
            var input = root.AddComponent<GameInputManager>();
            var moving = root.AddComponent<PlayerToken>();
            var stationaryObject = new GameObject("Stationary"); stationaryObject.SetActive(false);
            stationaryObject.transform.SetParent(root.transform);
            var stationary = stationaryObject.AddComponent<PlayerToken>();
            var apply = typeof(GameInputManager).GetMethod("ApplyHoverTarget", Private);
            apply.Invoke(input, new object[] { moving, null, previewOnly });
            Assert.That(previews, Is.EqualTo(1));
            using (moving.TrackMovement())
            {
                apply.Invoke(input, new object[] { moving, null, previewOnly });
                Assert.That(previews, Is.EqualTo(1));
                Assert.That(input.hoveredToken, Is.Null);
                Assert.That(input.hoveredHex, Is.Null);
                apply.Invoke(input, new object[] { stationary, null, previewOnly });
                Assert.That(previews, Is.EqualTo(2), "Other stationary tokens remain hoverable.");
                apply.Invoke(input, new object[] { moving, null, previewOnly });
                Assert.That(previews, Is.EqualTo(2));
            }
            // No cursor movement is required to restore the original token's hover.
            apply.Invoke(input, new object[] { moving, null, previewOnly });
            Assert.That(input.hoveredToken, Is.SameAs(moving));
            Assert.That(previews, Is.EqualTo(3));
            Assert.That(gameplay, Is.EqualTo(previewOnly ? 0 : 3));
        }
        finally
        {
            GameInputManager.OnTokenPreviewHover -= onPreview;
            GameInputManager.OnHover -= onHover;
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OverlappingMovementScopesReleaseOnlyAfterBothAnimationsAndDisposeOnce()
    {
        var root = new GameObject("Movement scopes"); root.SetActive(false);
        try
        {
            var token = root.AddComponent<PlayerToken>();
            var first = token.TrackMovement(); var second = token.TrackMovement();
            Assert.That(token.IsMoving, Is.True);
            first.Dispose(); first.Dispose();
            Assert.That(token.IsMoving, Is.True);
            second.Dispose();
            Assert.That(token.IsMoving, Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void InterruptedKickoffAnimationReleasesMovementWhenIteratorIsDisposed()
    {
        var root = new GameObject("Interrupted movement fixture"); root.SetActive(false);
        try
        {
            var token = root.AddComponent<PlayerToken>();
            var kickoff = root.AddComponent<KickoffManager>();
            var targetObject = new GameObject("Target"); targetObject.SetActive(false);
            targetObject.transform.SetParent(root.transform);
            var target = targetObject.AddComponent<HexCell>();
            target.transform.position = new Vector3(3, 0, 0);
            var animation = (IEnumerator)typeof(KickoffManager).GetMethod("AnimateTokenToHex", Private)
                .Invoke(kickoff, new object[] { token, target });
            Assert.That(animation.MoveNext(), Is.True);
            Assert.That(token.IsMoving, Is.True);
            ((IDisposable)animation).Dispose();
            Assert.That(token.IsMoving, Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
