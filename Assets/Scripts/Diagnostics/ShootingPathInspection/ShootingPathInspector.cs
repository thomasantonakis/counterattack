using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

/// <summary>Opt-in, read-only viewer for the shipped shot cache. Only authored in its diagnostic scene.</summary>
public sealed class ShootingPathInspector : MonoBehaviour
{
    public enum PathView { Stored, PhysicalDisc, Differences }
    public const string SceneName = "ShootingPathInspection";
    public Camera inspectionCamera;
    public Ball referenceBall;
    public HexCell[] cells;
    public Transform[] targetMarkers;
    public TMP_Text[] targetLabels;
    public Transform[] pathMarkers;
    public Transform originMarker;
    public Transform corridor;
    public LineRenderer corridorOutline;
    public LineRenderer centerLine;
    public TMP_Text status;
    public Material validMaterial;
    public Material invalidMaterial;
    public TextAsset shootingCache;
    [Header("Authored scale comparison (diagnostic scene only)")]
    public Transform dribblingReference;
    public Transform movingBall;
    public float visibleBallDiameter;
    [Header("Physical disc comparison (diagnostic only)")]
    public Transform[] physicalMarkers;
    public Material storedPathMaterial;
    public Material storedOnlyMaterial;
    public Material physicalPathMaterial;
    public LineRenderer capsuleOutline;
    public PathView View { get; private set; } = PathView.Differences;
    public float PhysicalRadius => visibleBallDiameter * 0.5f;
    public IReadOnlyList<HexCell> PhysicalPath => physicalPath;
    private List<HexCell> physicalPath = new List<HexCell>();
    private Dictionary<HexCell, Vector2[]> polygons;
    private int sharedCount, storedOnlyCount, physicalOnlyCount;

    private Dictionary<string, Dictionary<string, List<string>>> paths;
    private Dictionary<string, HexCell> byCoordinate;
    private HexCell[] targets = Array.Empty<HexCell>();
    private int targetIndex;
    public bool InspectionEnabled { get; private set; }
    public bool IsLocked { get; private set; }
    public HexCell Origin { get; private set; }
    public HexCell SelectedTarget => targets.Length == 8 ? targets[targetIndex] : null;
    public float BallRadius => referenceBall.ballRadius;

    public void Initialize()
    {
        paths = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(shootingCache.text);
        byCoordinate = cells.ToDictionary(c => Key(c.coordinates));
        polygons = cells.ToDictionary(c => c, c => c.GetHexCorners().Select(p => new Vector2(p.x, p.z)).ToArray());
        SetInspectionEnabled(false);
    }

    private void Start()
    {
        // This tool deliberately has no input integration with Room or live matches.
        if (gameObject.scene.name != SceneName)
        {
            enabled = false;
            return;
        }
        Initialize();
    }

    private void OnDisable()
    {
        if (pathMarkers != null && targetMarkers != null) SetInspectionEnabled(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8)) SetInspectionEnabled(!InspectionEnabled);
        MoveCamera();
        if (!InspectionEnabled) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { IsLocked = false; RefreshStatus(); }
        if (Input.GetKeyDown(KeyCode.V)) SetPathView((PathView)(((int)View + 1) % 3));
        HexCell hovered = PickCell(Input.mousePosition);
        if (Input.GetMouseButtonDown(0)) ClickOrigin(hovered);
        else HoverOrigin(hovered);
        for (int i = 0; i < 8; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))
                || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i))) SelectTarget(i);
        MoveBallToScreenPoint(Input.mousePosition);
    }

    public void SetInspectionEnabled(bool active)
    {
        InspectionEnabled = active;
        IsLocked = false;
        Origin = null;
        targets = Array.Empty<HexCell>();
        physicalPath.Clear();
        sharedCount = storedOnlyCount = physicalOnlyCount = 0;
        ClearVisuals();
        RefreshStatus();
    }

    public bool CanInspect(HexCell cell) => cell != null && paths != null && paths.ContainsKey(Key(cell.coordinates));

    public void HoverOrigin(HexCell cell)
    {
        if (!InspectionEnabled || IsLocked || !CanInspect(cell) || cell == Origin) return;
        SetOrigin(cell);
    }

    public void ClickOrigin(HexCell cell)
    {
        if (!InspectionEnabled || !CanInspect(cell)) return;
        SetOrigin(cell);
        IsLocked = true;
        RefreshStatus();
    }

    private void SetOrigin(HexCell cell)
    {
        Origin = cell;
        int side = Math.Sign(cell.coordinates.x);
        // Same goal-line set as HexGrid.CalculateShootingPaths; stable north-to-south numbering.
        targets = cells.Where(c => c.coordinates.x == side * 19 && c.coordinates.z >= -4 && c.coordinates.z <= 3)
            .OrderByDescending(c => c.coordinates.z).ToArray();
        RenderSelection();
    }

    public void SelectTarget(int index)
    {
        if (!InspectionEnabled || index < 0 || index >= 8) return;
        targetIndex = index;
        RenderSelection();
    }

    public bool IsTargetValid(HexCell target) => Origin != null && target != null
        && paths[Key(Origin.coordinates)].ContainsKey(Key(target.coordinates));

    public void SetPathView(PathView view)
    {
        if (!Enum.IsDefined(typeof(PathView), view)) return;
        View = view;
        RenderSelection();
    }

    public List<HexCell> CalculatePhysicalPath(HexCell from, HexCell to)
    {
        if (from == null || to == null || polygons == null) return new List<HexCell>();
        Vector3 a = from.GetHexCenter(), b = to.GetHexCenter();
        Vector2 start = new Vector2(a.x, a.z), end = new Vector2(b.x, b.z);
        Vector2[] capsule = DiscPathGeometry.BuildCapsulePolygon(start, end, PhysicalRadius);
        return cells.Where(c => c != from && DiscPathGeometry.CapsuleCoversPathHex(start, end, PhysicalRadius, polygons[c], capsule))
            .OrderBy(c => Vector3.Dot(c.GetHexCenter() - a, b - a))
            .ThenBy(c => c.coordinates.x).ThenBy(c => c.coordinates.z).ToList();
    }

    private void RenderSelection()
    {
        ClearVisuals();
        if (Origin == null || targets.Length != 8) { RefreshStatus(); return; }
        originMarker.gameObject.SetActive(true);
        originMarker.position = Origin.GetHexCenter() + Vector3.up * 0.08f;
        for (int i = 0; i < targets.Length; i++)
        {
            var marker = targetMarkers[i];
            marker.gameObject.SetActive(true);
            marker.position = targets[i].GetHexCenter() + Vector3.up * 0.16f;
            marker.GetComponent<Renderer>().sharedMaterial = IsTargetValid(targets[i]) ? validMaterial : invalidMaterial;
            targetLabels[i].text = (i + 1).ToString();
        }
        HexCell target = SelectedTarget;
        physicalPath = CalculatePhysicalPath(Origin, target);
        paths[Key(Origin.coordinates)].TryGetValue(Key(target.coordinates), out var path);
        var physicalSet = new HashSet<HexCell>(physicalPath);
        var storedSet = path == null ? new HashSet<HexCell>() : new HashSet<HexCell>(path.Select(key => byCoordinate[key]));
        sharedCount = physicalSet.Count(storedSet.Contains);
        storedOnlyCount = storedSet.Count - sharedCount;
        physicalOnlyCount = physicalSet.Count - sharedCount;
        if (path != null && View != PathView.PhysicalDisc)
        {
            for (int i = 0; i < path.Count && i < pathMarkers.Length; i++)
            {
                pathMarkers[i].position = byCoordinate[path[i]].GetHexCenter() + Vector3.up * 0.06f;
                pathMarkers[i].gameObject.SetActive(true);
                if (storedPathMaterial != null)
                    pathMarkers[i].GetComponent<Renderer>().sharedMaterial = View == PathView.Differences && !physicalSet.Contains(byCoordinate[path[i]])
                        ? storedOnlyMaterial : storedPathMaterial;
            }
        }
        if (physicalMarkers != null && View != PathView.Stored)
        {
            var displayed = View == PathView.PhysicalDisc ? physicalPath : physicalPath.Where(c => !storedSet.Contains(c)).ToList();
            if (displayed.Count > physicalMarkers.Length) throw new InvalidOperationException("Author more physical path markers.");
            for (int i = 0; i < displayed.Count; i++)
            {
                physicalMarkers[i].position = displayed[i].GetHexCenter() + Vector3.up * 0.065f;
                physicalMarkers[i].gameObject.SetActive(true);
            }
        }
        Vector3 start = Origin.GetHexCenter();
        Vector3 end = target.GetHexCenter();
        start.y = end.y = 0.24f;
        Vector3[] corners = CorridorCorners(start, end, visibleBallDiameter);
        corridor.gameObject.SetActive(true);
        corridor.position = (start + end) * 0.5f;
        corridor.rotation = Quaternion.LookRotation(end - start, Vector3.up);
        corridor.localScale = new Vector3(visibleBallDiameter, 1f, Vector3.Distance(start, end));
        corridorOutline.gameObject.SetActive(true);
        corridorOutline.positionCount = 4;
        corridorOutline.SetPositions(corners);
        centerLine.gameObject.SetActive(true);
        centerLine.positionCount = 2;
        centerLine.SetPositions(new[] { start + Vector3.up * 0.01f, end + Vector3.up * 0.01f });
        if (capsuleOutline != null)
        {
            capsuleOutline.gameObject.SetActive(true);
            Vector3[] capsule = CapsuleOutline(start + Vector3.up * 0.02f, end + Vector3.up * 0.02f, PhysicalRadius);
            capsuleOutline.positionCount = capsule.Length;
            capsuleOutline.SetPositions(capsule);
        }
        if (dribblingReference != null)
        {
            dribblingReference.gameObject.SetActive(true);
            dribblingReference.position = Origin.GetHexCenter();
        }
        if (movingBall != null)
        {
            movingBall.gameObject.SetActive(true);
            movingBall.position = Origin.GetHexCenter() + Vector3.up * referenceBall.playerHeightOffset;
        }
        RefreshStatus();
    }

    public void MoveBallToPointerRow(Vector3 pitchPoint)
    {
        if (!InspectionEnabled || !IsLocked || Origin == null || SelectedTarget == null || movingBall == null) return;
        Vector3 position = PointOnShotAtPointerRow(Origin.GetHexCenter(), SelectedTarget.GetHexCenter(), pitchPoint);
        position.y = referenceBall.playerHeightOffset;
        movingBall.position = position;
    }

    public void MoveBallToScreenPoint(Vector3 screenPoint)
    {
        if (TryPitchPoint(screenPoint, out Vector3 point)) MoveBallToPointerRow(point);
    }

    /// <summary>Intersect the shot with the pointer's world-Z row, matching the overhead view's horizontal row.</summary>
    public static Vector3 PointOnShotAtPointerRow(Vector3 start, Vector3 end, Vector3 pointer)
    {
        float dz = end.z - start.z;
        float dx = end.x - start.x;
        // A horizontal shot has no unique row intersection; use the pointer's X instead.
        float t = Mathf.Abs(dz) > 0.0001f ? (pointer.z - start.z) / dz
            : Mathf.Abs(dx) > 0.0001f ? (pointer.x - start.x) / dx : 0f;
        return Vector3.Lerp(start, end, Mathf.Clamp01(t));
    }

    public static Vector3[] CorridorCorners(Vector3 start, Vector3 end, float width)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        Vector3 offset = Vector3.Cross(Vector3.up, direction.normalized) * (width * 0.5f);
        return new[] { start - offset, start + offset, end + offset, end - offset };
    }

    public static Vector3[] CapsuleOutline(Vector3 start, Vector3 end, float radius)
    {
        Vector3 forward = end - start; forward.y = 0;
        forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        const int arcSteps = 24;
        var points = new Vector3[(arcSteps + 1) * 2];
        for (int i = 0; i <= arcSteps; i++)
        {
            float angle = -Mathf.PI * 0.5f + Mathf.PI * i / arcSteps;
            points[i] = start + radius * (-forward * Mathf.Cos(angle) + side * Mathf.Sin(angle));
            points[arcSteps + 1 + i] = end + radius * (forward * Mathf.Cos(-angle) + side * Mathf.Sin(-angle));
        }
        return points;
    }

    private void ClearVisuals()
    {
        foreach (var marker in targetMarkers) if (marker != null) marker.gameObject.SetActive(false);
        foreach (var marker in pathMarkers) if (marker != null) marker.gameObject.SetActive(false);
        if (originMarker != null) originMarker.gameObject.SetActive(false);
        if (corridor != null) corridor.gameObject.SetActive(false);
        if (corridorOutline != null) corridorOutline.gameObject.SetActive(false);
        if (centerLine != null) centerLine.gameObject.SetActive(false);
        if (dribblingReference != null) dribblingReference.gameObject.SetActive(false);
        if (movingBall != null) movingBall.gameObject.SetActive(false);
        if (physicalMarkers != null) foreach (var marker in physicalMarkers) if (marker != null) marker.gameObject.SetActive(false);
        if (capsuleOutline != null) capsuleOutline.gameObject.SetActive(false);
    }

    private void RefreshStatus()
    {
        if (status == null) return;
        string selection = Origin == null ? "Hover a shooting origin, then click to lock."
            : $"{(IsLocked ? "LOCKED" : "HOVER")} {Origin.coordinates}   ->   {targetIndex + 1}: {SelectedTarget?.coordinates}   "
                + (IsTargetValid(SelectedTarget) ? "VALID" : "REJECTED");
        status.text = $"SHOT INSPECTOR   [{(InspectionEnabled ? "ON" : "OFF")}]   F8 toggle\n"
            + "1-8 target (top to bottom) | Click selects and locks | Esc unlocks\n"
            + $"WASD / arrows pan | Scroll zoom | R reset | V path view: {View}\n"
            + (InspectionEnabled ? selection : "Press F8 to inspect shooting paths.")
            + $"\nBlue: stored/shared | Magenta: stored only | Cyan: physical (min area {DiscPathGeometry.MinimumPathOverlapArea:0.####})\n"
            + $"Capsule width: {visibleBallDiameter:0.####} | Physical radius: {PhysicalRadius:0.####} | Old cutoff: {BallRadius:0.####}\n"
            + $"Shared: {sharedCount} | Stored only: {storedOnlyCount} | Physical only: {physicalOnlyCount}\n"
            + "Locked: mouse row scrubs ball along shot | Token + ball at origin show dribbling scale.";
    }

    private HexCell PickCell(Vector3 screen)
    {
        if (!TryPitchPoint(screen, out Vector3 point)) return null;
        // Use actual authored centers; raised diagnostic markers cannot intercept picking.
        HexCell nearest = null;
        float nearestDistance = 0.26f;
        foreach (HexCell cell in cells)
        {
            float squaredDistance = (cell.GetHexCenter() - point).sqrMagnitude;
            if (squaredDistance >= nearestDistance) continue;
            nearestDistance = squaredDistance;
            nearest = cell;
        }
        return nearest;
    }

    private bool TryPitchPoint(Vector3 screen, out Vector3 point)
    {
        Ray ray = inspectionCamera.ScreenPointToRay(screen);
        if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }
        point = default;
        return false;
    }

    private void MoveCamera()
    {
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
            - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
            - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        inspectionCamera.transform.position += new Vector3(x, 0, z) * (12f * Time.unscaledDeltaTime);
        inspectionCamera.orthographicSize = Mathf.Clamp(inspectionCamera.orthographicSize - Input.mouseScrollDelta.y, 3f, 25f);
        if (Input.GetKeyDown(KeyCode.R))
        {
            inspectionCamera.transform.position = new Vector3(0, 40, 0);
            inspectionCamera.orthographicSize = 15f;
        }
    }
    public static string Key(Vector3Int coordinate) => $"({coordinate.x}, {coordinate.y}, {coordinate.z})";
}
