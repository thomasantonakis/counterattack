using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class MovingTokenHoverEditorTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

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
