using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class ShootingPathInspectorEditorTests
{
    private ShootingPathInspector tool;
    private SceneSetup[] previousScenes;
    private bool openedDiagnosticScene;

    [OneTimeSetUp]
    public void OpenDiagnosticScene()
    {
        if (Object.FindAnyObjectByType<ShootingPathInspector>() != null) return;
        previousScenes = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty, Is.False, "Save loaded scenes before tests.");
        EditorSceneManager.OpenScene(ShootingPathInspectionAuthoring.ScenePath, OpenSceneMode.Single);
        openedDiagnosticScene = true;
    }

    [OneTimeTearDown]
    public void RestoreScenes()
    {
        if (!openedDiagnosticScene) return;
        // Framework-owned unnamed/temporary scenes may have no restorable SceneSetup.
        if (previousScenes != null && previousScenes.Any(s => s.isLoaded) && previousScenes.Count(s => s.isActive) == 1)
            EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
        else
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [SetUp]
    public void SetUp()
    {
        var source = Object.FindAnyObjectByType<ShootingPathInspector>();
        Assert.That(source, Is.Not.Null, "Open the authored ShootingPathInspection scene before this focused fixture.");
        tool = Object.Instantiate(source.gameObject).GetComponent<ShootingPathInspector>();
        tool.status = null; // UI belongs to the source scene; never write it from the owned clone.
        tool.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        if (tool != null) Object.DestroyImmediate(tool.gameObject);
    }

    private HexCell Cell(int x, int z) => tool.cells.Single(c => c.coordinates == new Vector3Int(x, 0, z));

    private HexGrid OwnedGrid()
    {
        var grid = new GameObject("Owned ground path grid").AddComponent<HexGrid>();
        grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
        foreach (HexCell cell in tool.cells)
            grid.cells[cell.coordinates.x + grid.GridWidth / 2, cell.coordinates.z + grid.GridHeight / 2] = cell;
        return grid;
    }

    [TestCase(1, 12, 6, 19, 1)]
    [TestCase(-1, 12, 6, 19, 1)]
    [TestCase(1, 12, 0, 18, 0)]
    [TestCase(-1, 12, 0, 18, 0)]
    public void SharedGroundPathMatchesPhysicalDiscAcrossAllHexes(int side, int fromX, int fromZ, int toX, int toZ)
    {
        HexGrid grid = OwnedGrid();
        try
        {
            HexCell from = Cell(side * fromX, fromZ), to = Cell(side * toX, toZ);
            Assert.That(tool.referenceBall.PhysicalRadius, Is.EqualTo(tool.PhysicalRadius).Within(0.0001f));
            var actual = GroundPassCommon.CalculateThickPath(grid, from, to, tool.referenceBall.PhysicalRadius);
            var expected = tool.CalculatePhysicalPath(from, to);
            Assert.That(actual.Select(c => c.coordinates), Is.EquivalentTo(expected.Select(c => c.coordinates)));
            Vector3 direction = to.GetHexCenter() - from.GetHexCenter();
            Assert.That(actual.Select(c => Vector3.Dot(c.GetHexCenter() - from.GetHexCenter(), direction)),
                Is.Ordered);
            Assert.That(actual, Has.No.Member(from));
            Assert.That(actual, Has.Member(to));
        }
        finally { Object.DestroyImmediate(grid.gameObject); }
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void TinyCornerOverlapsDoNotAddFourHexesToTargetOnePath(int side)
    {
        HexGrid grid = OwnedGrid();
        try
        {
            HexCell from = Cell(side * 9, 0), to = Cell(side * 19, 3);
            var path = GroundPassCommon.CalculateThickPath(grid, from, to, tool.referenceBall.PhysicalRadius);
            Assert.That(path, Has.Count.EqualTo(14));
            foreach (Vector3Int coordinate in new[]
            {
                new Vector3Int(side * 10, 0, 0), new Vector3Int(side * 13, 0, 2),
                new Vector3Int(side * 15, 0, 1), new Vector3Int(side * 18, 0, 4)
            })
                Assert.That(path, Has.No.Member(Cell(coordinate.x, coordinate.z)), coordinate.ToString());
            Assert.That(path, Has.Member(to));

            Vector3 a = from.GetHexCenter(), b = to.GetHexCenter();
            Vector2[] capsule = DiscPathGeometry.BuildCapsulePolygon(new Vector2(a.x, a.z), new Vector2(b.x, b.z), tool.PhysicalRadius);
            float sliver = DiscPathGeometry.ConvexIntersectionArea(
                Cell(side * 10, 0).GetHexCorners().Select(p => new Vector2(p.x, p.z)).ToArray(), capsule);
            float retained = DiscPathGeometry.ConvexIntersectionArea(
                Cell(side * 10, 1).GetHexCorners().Select(p => new Vector2(p.x, p.z)).ToArray(), capsule);
            Assert.That(sliver, Is.GreaterThan(0f).And.LessThan(DiscPathGeometry.MinimumPathOverlapArea));
            Assert.That(retained, Is.GreaterThan(DiscPathGeometry.MinimumPathOverlapArea));
        }
        finally { Object.DestroyImmediate(grid.gameObject); }
    }

    [TestCase(1, 8, 2, false)] // Outside the upper post.
    [TestCase(1, 8, 1, true)]  // Same origin, target 3 is inside.
    [TestCase(1, 7, 2, true)]  // Centerline meets the upper post.
    [TestCase(-1, 8, 2, false)]
    [TestCase(-1, 8, 1, true)]
    [TestCase(-1, 7, 2, true)]
    public void GoalMouthUsesCenterlineAndIncludesPostBoundary(int side, int fromZ, int toZ, bool expected)
    {
        HexGrid grid = OwnedGrid();
        try
        {
            Assert.That(grid.IsCenterlineWithinGoalMouth(Cell(side * 16, fromZ), Cell(side * 19, toZ)),
                Is.EqualTo(expected));
        }
        finally { Object.DestroyImmediate(grid.gameObject); }
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void PhysicalDiscUsesWholeHexPolygonsAcrossSharedEdges(int side)
    {
        var result = tool.CalculatePhysicalPath(Cell(side * 12, 0), Cell(side * 18, 0));
        Assert.That(result.Contains(Cell(side * 12, 0)), Is.False);
        var expected = Enumerable.Range(13, 6).SelectMany(x => x % 2 == 0
            ? new[] { new Vector3Int(side * x, 0, 0) }
            : new[] { new Vector3Int(side * x, 0, -1), new Vector3Int(side * x, 0, 0) }).ToArray();
        Assert.That(result.Select(c => c.coordinates), Is.EquivalentTo(expected));
    }

    [Test]
    public void ComparisonViewsShowStoredPhysicalAndDifferenceSetsWithoutChangingCache()
    {
        tool.SetInspectionEnabled(true);
        tool.ClickOrigin(Cell(12, 6));
        tool.SelectTarget(4);
        string before = tool.shootingCache.text;
        var cache = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(before);
        var stored = cache[ShootingPathInspector.Key(tool.Origin.coordinates)][ShootingPathInspector.Key(tool.SelectedTarget.coordinates)]
            .Select(key => tool.cells.Single(c => ShootingPathInspector.Key(c.coordinates) == key)).ToHashSet();
        var physical = tool.CalculatePhysicalPath(tool.Origin, tool.SelectedTarget).ToHashSet();
        tool.SetPathView(ShootingPathInspector.PathView.PhysicalDisc);
        Assert.That(tool.pathMarkers.All(m => !m.gameObject.activeSelf), Is.True);
        Assert.That(tool.physicalMarkers.Count(m => m.gameObject.activeSelf), Is.EqualTo(physical.Count));
        tool.SetPathView(ShootingPathInspector.PathView.Stored);
        Assert.That(tool.pathMarkers.Count(m => m.gameObject.activeSelf), Is.EqualTo(stored.Count));
        Assert.That(tool.physicalMarkers.All(m => !m.gameObject.activeSelf), Is.True);
        tool.SetPathView(ShootingPathInspector.PathView.Differences);
        Assert.That(tool.physicalMarkers.Count(m => m.gameObject.activeSelf), Is.EqualTo(physical.Count(c => !stored.Contains(c))));
        for (int i = 0; i < stored.Count; i++)
        {
            var cell = tool.cells.Single(c => Vector3.Distance(c.GetHexCenter() + Vector3.up * 0.06f, tool.pathMarkers[i].position) < 0.0001f);
            Assert.That(tool.pathMarkers[i].GetComponent<Renderer>().sharedMaterial,
                Is.SameAs(physical.Contains(cell) ? tool.storedPathMaterial : tool.storedOnlyMaterial));
        }
        Assert.That(tool.shootingCache.text, Is.EqualTo(before));
        tool.SetInspectionEnabled(false);
        Assert.That(tool.physicalMarkers.All(m => !m.gameObject.activeSelf), Is.True);
        Assert.That(tool.capsuleOutline.gameObject.activeSelf, Is.False);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void MouseRayScrubsLockedShotAtPointerRowAndClampsToEndpoints(int side)
    {
        tool.SetInspectionEnabled(true);
        tool.ClickOrigin(Cell(side * 12, 6));
        tool.SelectTarget(4);
        Vector3 start = tool.Origin.GetHexCenter();
        Vector3 end = tool.SelectedTarget.GetHexCenter();
        foreach (float fraction in new[] { -0.5f, 0f, 0.25f, 0.75f, 1f, 1.5f })
        {
            Vector3 pointer = Vector3.LerpUnclamped(start, end, fraction);
            pointer.x += 4f; // Same horizontal row, deliberately away from the shot.
            tool.MoveBallToScreenPoint(tool.inspectionCamera.WorldToScreenPoint(pointer));
            Vector3 expected = Vector3.Lerp(start, end, Mathf.Clamp01(fraction));
            expected.y = tool.referenceBall.playerHeightOffset;
            Assert.That(Vector3.Distance(tool.movingBall.position, expected), Is.LessThan(0.0001f));
            Assert.That(tool.dribblingReference.position, Is.EqualTo(start));
        }
    }

    [Test]
    public void UnlockedAndOffModesCannotScrubAndAllComparisonVisualsCleanUp()
    {
        tool.SetInspectionEnabled(true);
        tool.HoverOrigin(Cell(12, 6));
        Vector3 before = tool.movingBall.position;
        tool.MoveBallToPointerRow(Vector3.zero);
        Assert.That(tool.movingBall.position, Is.EqualTo(before));
        tool.SetInspectionEnabled(false);
        tool.MoveBallToPointerRow(Vector3.one);
        Assert.That(tool.movingBall.gameObject.activeSelf, Is.False);
        Assert.That(tool.dribblingReference.gameObject.activeSelf, Is.False);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void HorizontalShotUsesPointerXAndDegenerateShotStaysAtOrigin(int side)
    {
        Vector3 start = new Vector3(side * 8, 0, 2);
        Vector3 end = new Vector3(side * 14, 0, 2);
        Vector3 pointer = new Vector3(side * 11, 0, 999);
        Assert.That(ShootingPathInspector.PointOnShotAtPointerRow(start, end, pointer), Is.EqualTo((start + end) * 0.5f));
        Assert.That(ShootingPathInspector.PointOnShotAtPointerRow(start, start, pointer), Is.EqualTo(start));
    }

    [Test]
    public void ComparisonPreservesActualBallAndTokenScaleWithoutGameplayComponents()
    {
        var source = tool.referenceBall;
        var ballRenderer = tool.movingBall.GetComponent<Renderer>();
        Assert.That(tool.movingBall.lossyScale, Is.EqualTo(source.transform.lossyScale));
        Assert.That(tool.movingBall.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(source.GetComponent<MeshFilter>().sharedMesh));
        Assert.That(ballRenderer.bounds.size.x, Is.EqualTo(0.35f).Within(0.0001f));
        Assert.That(tool.visibleBallDiameter, Is.EqualTo(ballRenderer.bounds.size.x).Within(0.0001f));
        Assert.That(tool.dribblingReference.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
        Assert.That(tool.movingBall.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
        var token = tool.dribblingReference.Find("Reference Player Token (visual only)");
        Assert.That(token.localScale, Is.EqualTo(new Vector3(0.55f, 0.2f, 0.55f)));
        Assert.That(tool.dribblingReference.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(2));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void AllEightTargetsUseStoredEligibilityAndStableNumbering(int side)
    {
        tool.SetInspectionEnabled(true);
        HexCell origin = tool.cells.First(c => c.coordinates.x * side > 0 && tool.CanInspect(c));
        tool.ClickOrigin(origin);
        for (int i = 0; i < 8; i++)
        {
            tool.SelectTarget(i);
            Assert.That(tool.SelectedTarget.coordinates, Is.EqualTo(new Vector3Int(side * 19, 0, 3 - i)));
            Assert.That(tool.targetMarkers.All(m => m.gameObject.activeSelf), Is.True);
            Assert.That(tool.targetMarkers[i].GetComponent<Renderer>().sharedMaterial,
                Is.SameAs(tool.IsTargetValid(tool.SelectedTarget) ? tool.validMaterial : tool.invalidMaterial));
            Assert.That(tool.targetMarkers[i].position.y, Is.GreaterThan(tool.SelectedTarget.GetHexCenter().y));
        }
    }

    [Test]
    public void ClickingReplacesLockWhileHoverCannotMoveLockedOrigin()
    {
        tool.SetInspectionEnabled(true);
        var origins = tool.cells.Where(tool.CanInspect).Take(3).ToArray();
        tool.HoverOrigin(origins[0]);
        Assert.That(tool.Origin, Is.SameAs(origins[0]));
        tool.ClickOrigin(origins[1]);
        tool.HoverOrigin(origins[2]);
        Assert.That(tool.IsLocked, Is.True);
        Assert.That(tool.Origin, Is.SameAs(origins[1]));
        tool.ClickOrigin(origins[2]);
        Assert.That(tool.Origin, Is.SameAs(origins[2]));
        tool.ClickOrigin(Cell(0, 0));
        Assert.That(tool.Origin, Is.SameAs(origins[2]), "Non-shooting origins must not replace the selection.");
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void RejectedTargetRetainsCorridorAndClearsBluePath(int side)
    {
        tool.SetInspectionEnabled(true);
        foreach (HexCell origin in tool.cells.Where(c => c.coordinates.x * side > 0 && tool.CanInspect(c)))
        {
            tool.ClickOrigin(origin);
            for (int i = 0; i < 8; i++)
            {
                tool.SelectTarget(i);
                if (tool.IsTargetValid(tool.SelectedTarget)) continue;
                Assert.That(tool.pathMarkers.All(m => !m.gameObject.activeSelf), Is.True);
                Assert.That(tool.corridor.gameObject.activeSelf, Is.True);
                Assert.That(tool.corridor.localScale.x, Is.EqualTo(tool.visibleBallDiameter).Within(0.0001f));
                return;
            }
        }
        Assert.Fail("Expected at least one rejected connection in the shipped cache.");
    }

    [Test]
    public void TurningOffClearsEveryOverlayAndIgnoresSelectionInput()
    {
        tool.SetInspectionEnabled(true);
        tool.ClickOrigin(tool.cells.First(tool.CanInspect));
        tool.SetInspectionEnabled(false);
        tool.ClickOrigin(tool.cells.First(tool.CanInspect));
        tool.SelectTarget(7);
        Assert.That(tool.Origin, Is.Null);
        Assert.That(tool.IsLocked, Is.False);
        Assert.That(tool.targetMarkers.Concat(tool.pathMarkers).All(m => !m.gameObject.activeSelf), Is.True);
        Assert.That(tool.corridor.gameObject.activeSelf, Is.False);
        Assert.That(tool.centerLine.gameObject.activeSelf, Is.False);
        Assert.That(tool.corridorOutline.gameObject.activeSelf, Is.False);
    }

    [TestCase(12f, 3f, 15f, -2f)]
    [TestCase(-12f, -3f, -15f, 2f)]
    public void CorridorUsesVisibleBallDiameterAndCenteredEndpoints(float x1, float z1, float x2, float z2)
    {
        Vector3 start = new Vector3(x1, 0.24f, z1);
        Vector3 end = new Vector3(x2, 0.24f, z2);
        float diameter = tool.visibleBallDiameter;
        Vector3[] corners = ShootingPathInspector.CorridorCorners(start, end, diameter);
        Assert.That(Vector3.Distance(corners[0], corners[1]), Is.EqualTo(diameter).Within(0.0001f));
        Assert.That(Vector3.Distance(corners[2], corners[3]), Is.EqualTo(diameter).Within(0.0001f));
        Assert.That(Vector3.Distance((corners[0] + corners[1]) / 2, start), Is.LessThan(0.0001f));
        Assert.That(Vector3.Distance((corners[2] + corners[3]) / 2, end), Is.LessThan(0.0001f));
        Assert.That(Mathf.Abs(Vector3.Dot(corners[1] - corners[0], end - start)), Is.LessThan(0.0001f));
    }

    [Test]
    public void PitchMarkingsAndGoalOutlinesRenderAfterEveryDiagnosticOverlay()
    {
        foreach (string group in new[] { "PitchMarkings", "GoalNets" })
        {
            var root = GameObject.Find("PitchBoardVisuals/GeneratedVisuals/" + group);
            Assert.That(root, Is.Not.Null);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Diagnostics/ShootingInspection/PitchLinesOnTop"));
                Assert.That(renderer.sharedMaterial.renderQueue, Is.GreaterThan(tool.corridor.GetComponent<Renderer>().sharedMaterial.renderQueue));
                Assert.That(renderer.sharedMaterial.renderQueue, Is.GreaterThan(tool.validMaterial.renderQueue));
            }
        }
    }

    [Test]
    public void BlueMarkersExactlyMatchTheStoredPathWithoutRecalculation()
    {
        var cache = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(tool.shootingCache.text);
        tool.SetInspectionEnabled(true);
        HexCell origin = Cell(12, 6);
        tool.ClickOrigin(origin);
        tool.SelectTarget(4);
        var expected = cache[ShootingPathInspector.Key(origin.coordinates)][ShootingPathInspector.Key(tool.SelectedTarget.coordinates)];
        Assert.That(tool.pathMarkers.Count(m => m.gameObject.activeSelf), Is.EqualTo(expected.Count));
        for (int i = 0; i < expected.Count; i++)
        {
            HexCell cell = tool.cells.Single(c => ShootingPathInspector.Key(c.coordinates) == expected[i]);
            Assert.That(tool.pathMarkers[i].position, Is.EqualTo(cell.GetHexCenter() + Vector3.up * 0.06f));
        }
        Assert.That(tool.pathMarkers.Length, Is.GreaterThanOrEqualTo(cache.Values.SelectMany(v => v.Values).Max(p => p.Count)));
    }

    [Test]
    public void SceneHasNoMatchOrCameraGameplayControllersAndEnoughAuthoredPathMarkers()
    {
        Assert.That(Object.FindAnyObjectByType<MatchManager>(), Is.Null);
        Assert.That(Object.FindAnyObjectByType<GameInputManager>(), Is.Null);
        Assert.That(Object.FindAnyObjectByType<CameraController>(), Is.Null);
        Assert.That(tool.referenceBall.enabled, Is.False);
        string cacheBefore = tool.shootingCache.text;
        var cellsBefore = tool.cells.Select(c => new { c.transform.position, c.CanShootFrom, c.ShootingPaths }).ToArray();
        tool.SetInspectionEnabled(true);
        foreach (var origin in tool.cells.Where(tool.CanInspect))
        {
            tool.ClickOrigin(origin);
            for (int i = 0; i < 8; i++) tool.SelectTarget(i);
        }
        Assert.That(tool.shootingCache.text, Is.EqualTo(cacheBefore));
        for (int i = 0; i < tool.cells.Length; i++)
        {
            Assert.That(tool.cells[i].transform.position, Is.EqualTo(cellsBefore[i].position));
            Assert.That(tool.cells[i].CanShootFrom, Is.EqualTo(cellsBefore[i].CanShootFrom));
            Assert.That(tool.cells[i].ShootingPaths, Is.SameAs(cellsBefore[i].ShootingPaths));
        }
    }
}
