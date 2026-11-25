using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DiscPathGeometryEditorTests
{
    private static readonly Vector2[] Square = { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, 0.5f) };

    [TestCase(0.6f, true)]
    [TestCase(0.675f, true)]
    [TestCase(0.6751f, false)]
    [TestCase(-0.675f, true)]
    [TestCase(-0.6751f, false)]
    public void DiscTouchesHexAreaEvenWhenCellCenterIsOutsideRadius(float row, bool expected)
    {
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(new Vector2(-2, row), new Vector2(2, row), 0.175f, Square), Is.EqualTo(expected));
    }

    [TestCase(-0.65f, true)]
    [TestCase(-0.675f, true)]
    [TestCase(-0.6751f, false)]
    public void RoundedEndCapIsIncludedAndSegmentIsFinite(float endpoint, bool expected)
    {
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(new Vector2(-2, 0), new Vector2(endpoint, 0), 0.175f, Square), Is.EqualTo(expected));
    }

    [TestCase(0.175f, true)]
    [TestCase(0.1752f, false)]
    public void StationaryDiscTestsRoundedCornerDistance(float distance, bool expected)
    {
        Vector2 point = Vector2.one * (0.5f + distance / Mathf.Sqrt(2));
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(point, point, 0.175f, Square), Is.EqualTo(expected));
    }

    [Test]
    public void ZeroRadiusIncludesCrossingInsideAndBoundaryButNotNearbyPolygon()
    {
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(new Vector2(-2, 0), new Vector2(2, 0), 0, Square), Is.True);
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(Vector2.zero, Vector2.zero, 0, Square), Is.True);
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(new Vector2(-2, 0.5f), new Vector2(2, 0.5f), 0, Square), Is.True);
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(new Vector2(-2, 0.501f), new Vector2(2, 0.501f), 0, Square), Is.False);
    }

    [TestCase(0.675f)]
    [TestCase(0.676f)]
    public void ReversingTravelAndPolygonWindingPreservesResult(float row)
    {
        Vector2 a = new Vector2(-2, row), b = new Vector2(2, row);
        bool expected = DiscPathGeometry.CapsuleTouchesPolygon(a, b, 0.175f, Square);
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(b, a, 0.175f, Square.Reverse().ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void InvalidInputsDoNotProduceAPath()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DiscPathGeometry.CapsuleTouchesPolygon(Vector2.zero, Vector2.one, -1, Square));
        Assert.That(DiscPathGeometry.CapsuleTouchesPolygon(Vector2.zero, Vector2.one, 1, null), Is.False);
    }

    [Test]
    public void CapsuleDrawingHasPhysicalDiameterAndRoundCaps()
    {
        Vector3 start = new Vector3(2, 0.26f, 3), end = new Vector3(8, 0.26f, 3);
        var outline = ShootingPathInspector.CapsuleOutline(start, end, 0.175f);
        Assert.That(outline.Min(p => p.x), Is.EqualTo(start.x - 0.175f).Within(0.0001f));
        Assert.That(outline.Max(p => p.x), Is.EqualTo(end.x + 0.175f).Within(0.0001f));
        Assert.That(outline.Max(p => p.z) - outline.Min(p => p.z), Is.EqualTo(0.35f).Within(0.0001f));
        Assert.That(outline.All(p => Mathf.Abs(p.y - 0.26f) < 0.0001f), Is.True);
    }
}
