using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored UI graphic: schematic pitch and a player's normalized occupancy distribution.</summary>
[AddComponentMenu("UI/Counter Attack/Post Match Pitch")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PostMatchPitchGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private Color grass = new(.10f, .29f, .25f, 1);
    [SerializeField] private Color lines = new(.80f, .89f, .85f, .8f);
    private IReadOnlyList<Vector2> samples;
    public void ShowHeatmap(IReadOnlyList<Vector2> points) { samples = points; SetVerticesDirty(); }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Quad(rect.xMin, rect.yMin, rect.width, rect.height, grass);
        const int columns = 20, rows = 14;
        if (samples?.Count > 0)
        {
            var counts = new int[columns, rows];
            int maximum = 1;
            foreach (var p in samples)
            {
                int x = Mathf.Clamp((int)(p.x * columns), 0, columns - 1), y = Mathf.Clamp((int)(p.y * rows), 0, rows - 1);
                maximum = Mathf.Max(maximum, ++counts[x, y]);
            }
            for (int x = 0; x < columns; x++) for (int y = 0; y < rows; y++)
                if (counts[x, y] > 0) Quad(rect.xMin + x * rect.width / columns, rect.yMin + y * rect.height / rows,
                    rect.width / columns, rect.height / rows, new Color(1f, .45f, .12f, .15f + .7f * counts[x, y] / maximum));
        }
        Outline(0, 0, 1, 1); Outline(0, .23f, .16f, .54f); Outline(.84f, .23f, .16f, .54f);
        Outline(0, .38f, .06f, .24f); Outline(.94f, .38f, .06f, .24f);
        Quad(rect.xMin + rect.width / 2 - 1, rect.yMin, 2, rect.height, lines);
        for (int i = 0; i < 48; i++)
        {
            float a = i * Mathf.PI * 2 / 48;
            float b = (i + 1) * Mathf.PI * 2 / 48;
            var center = rect.center;
            var start = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rect.height * .14f;
            var end = center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * rect.height * .14f;
            var normal = new Vector2(-(end - start).y, (end - start).x).normalized;
            int index = mesh.currentVertCount;
            mesh.AddVert(start - normal, lines, Vector2.zero); mesh.AddVert(start + normal, lines, Vector2.zero);
            mesh.AddVert(end + normal, lines, Vector2.zero); mesh.AddVert(end - normal, lines, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
        }

        void Outline(float x, float y, float width, float height)
        {
            float left = rect.xMin + x * rect.width, bottom = rect.yMin + y * rect.height;
            float w = width * rect.width, h = height * rect.height;
            Quad(left, bottom, w, 2, lines); Quad(left, bottom + h - 2, w, 2, lines);
            Quad(left, bottom, 2, h, lines); Quad(left + w - 2, bottom, 2, h, lines);
        }
        void Quad(float x, float y, float width, float height, Color tint)
        {
            int index = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y), tint, Vector2.zero); mesh.AddVert(new Vector3(x, y + height), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + width, y + height), tint, Vector2.zero); mesh.AddVert(new Vector3(x + width, y), tint, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
