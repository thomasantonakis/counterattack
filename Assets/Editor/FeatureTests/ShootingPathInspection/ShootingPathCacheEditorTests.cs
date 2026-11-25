using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ShootingPathCacheEditorTests
{
    [Test]
    public void ReassigningCachesClearsStaleOriginFlagsAndPaths()
    {
        var root = new GameObject("Owned cache assignment fixture");
        root.SetActive(false);
        try
        {
            var grid = root.AddComponent<HexGrid>();
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            HexCell shootingOrigin = Cell(root.transform, "shooting origin", 12, 0);
            HexCell headingOrigin = Cell(root.transform, "heading origin", 16, 0);
            HexCell staleOrigin = Cell(root.transform, "stale origin", 13, 0);
            HexCell target = Cell(root.transform, "target", 19, 0);
            foreach (HexCell cell in new[] { shootingOrigin, headingOrigin, staleOrigin, target })
                grid.cells[cell.coordinates.x + grid.GridWidth / 2, cell.coordinates.z + grid.GridHeight / 2] = cell;

            staleOrigin.CanShootFrom = staleOrigin.CanHeadFrom = true;
            staleOrigin.ShootingPaths = new Dictionary<HexCell, List<HexCell>> { [target] = new List<HexCell> { target } };
            staleOrigin.HeadingPaths = new Dictionary<HexCell, List<HexCell>> { [target] = new List<HexCell> { target } };
            SetCache(grid, "shootingPaths", shootingOrigin, target);
            SetCache(grid, "headingPaths", headingOrigin, target);
            Assign(grid);

            Assert.That(shootingOrigin.CanShootFrom, Is.True);
            Assert.That(shootingOrigin.CanHeadFrom, Is.False);
            Assert.That(shootingOrigin.ShootingPaths[target], Is.EqualTo(new[] { target }));
            Assert.That(headingOrigin.CanHeadFrom, Is.True);
            Assert.That(headingOrigin.CanShootFrom, Is.False);
            Assert.That(headingOrigin.HeadingPaths[target], Is.EqualTo(new[] { target }));
            Assert.That(staleOrigin.CanShootFrom || staleOrigin.CanHeadFrom, Is.False);
            Assert.That(staleOrigin.ShootingPaths, Is.Null);
            Assert.That(staleOrigin.HeadingPaths, Is.Null);

            SetCache(grid, "shootingPaths", null, null);
            SetCache(grid, "headingPaths", null, null);
            Assign(grid);
            Assert.That(shootingOrigin.CanShootFrom || headingOrigin.CanHeadFrom, Is.False);
            Assert.That(shootingOrigin.ShootingPaths, Is.Null);
            Assert.That(headingOrigin.HeadingPaths, Is.Null);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static HexCell Cell(Transform parent, string name, int x, int z)
    {
        var cell = new GameObject(name, typeof(MeshRenderer), typeof(HexCell)).GetComponent<HexCell>();
        cell.transform.SetParent(parent);
        cell.coordinates = new Vector3Int(x, 0, z);
        return cell;
    }

    private static void SetCache(HexGrid grid, string field, HexCell origin, HexCell target)
    {
        var paths = new Dictionary<HexCell, Dictionary<HexCell, List<HexCell>>>();
        if (origin != null) paths[origin] = new Dictionary<HexCell, List<HexCell>> { [target] = new List<HexCell> { target } };
        typeof(HexGrid).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(grid, paths);
    }

    private static void Assign(HexGrid grid) =>
        typeof(HexGrid).GetMethod("AssignShootingPathsToHexes", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, null);
}
