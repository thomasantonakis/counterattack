using System.Collections;
using System.IO;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class OnlineDraftSceneFixture
{
    private IEnumerator VerifyPoolLayout(OnlineDraftView view)
    {
        CheckAverageBadges(view, empty: true);
        foreach (var side in new[] { OnlineTeam.Home, OnlineTeam.Away })
        {
            var picker = Client(side);
            Assert.That(picker.Request(OnlineDraftIntent.Pick, picker.Capture().available[0], 2), Is.True);
            Flush();
            Assert.That(picker.LastReply, Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        yield return null;
        Canvas.ForceUpdateCanvases();
        string capture = System.Environment.GetEnvironmentVariable("COUNTERATTACK_DRAFT_CAPTURE");
        if (!string.IsNullOrEmpty(capture) && !File.Exists(capture))
        {
            ScreenCapture.CaptureScreenshot(capture);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!File.Exists(capture) && Time.realtimeSinceStartup < deadline) yield return null;
        }

        var table = (RectTransform)view.poolTable.transform;
        var canvas = (RectTransform)table.GetComponentInParent<Canvas>().rootCanvas.transform;
        Rect tableRect = LayoutRect(table, canvas);
        Rect home = LayoutRect((RectTransform)view.homeSlots[0].transform, canvas);
        Rect away = LayoutRect((RectTransform)view.awaySlots[0].transform, canvas);
        Rect previous = LayoutRect((RectTransform)view.previousPageButton.transform, canvas);
        Rect next = LayoutRect((RectTransform)view.nextPageButton.transform, canvas);
        Rect viewport = LayoutRect(view.poolScroll.viewport, canvas);
        var rows = view.poolRows;
        string layout = $"Home {home}, table {tableRect}, away {away}, canvas {canvas.rect}, viewport {viewport}, pagination {previous} / {next}";
        for (int i = 0; i < rows.Length; i++)
        {
            Assert.That(rows[i].gameObject.activeInHierarchy, Is.True, $"Row {i + 1}");
            Rect row = LayoutRect((RectTransform)rows[i].transform, canvas);
            Assert.That(row.width, Is.EqualTo(viewport.width).Within(1), $"Row {i + 1}");
            Assert.That(row.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1), $"Row {i + 1} is fully visible");
            if (i > 0)
            {
                Rect above = LayoutRect((RectTransform)rows[i - 1].transform, canvas);
                Assert.That(row.yMax, Is.LessThan(above.yMin), $"Rows {i} and {i + 1}. {layout}");
            }
        }
        Assert.That(tableRect.xMin, Is.GreaterThanOrEqualTo(home.xMax - 1), layout);
        Assert.That(tableRect.xMax, Is.LessThanOrEqualTo(away.xMin + 1), layout);
        Assert.That(home.xMin, Is.GreaterThanOrEqualTo(canvas.rect.xMin - 1), layout);
        Assert.That(away.xMax, Is.LessThanOrEqualTo(canvas.rect.xMax + 1), layout);
        Assert.That(previous.xMax, Is.LessThan(next.xMin), layout);
        Assert.That(previous.yMax, Is.LessThanOrEqualTo(viewport.yMin + 1), layout);
        Assert.That(previous.yMin, Is.EqualTo(tableRect.yMin).Within(1), layout);
        Assert.That(viewport.width, Is.EqualTo(tableRect.width).Within(1), layout);
        CheckAverageBadges(view, empty: false);
    }

    private static void CheckAverageBadges(OnlineDraftView view, bool empty)
    {
        foreach (var side in new[] { "Home", "Away" })
        {
            var panel = side == "Home" ? view.source.homeAveragePanel : view.source.awayAveragePanel;
            foreach (var row in new[] { "XI", "TeamAverage" })
            {
                var badge = panel.transform.Find($"{side}-{row}/ContentWrapper/PaceBadge").GetComponent<Image>();
                Assert.That(badge.isActiveAndEnabled && badge.sprite != null, Is.True, $"{side} {row} badge image");
                Assert.That(badge.color.a, Is.EqualTo(1), $"{side} {row} badge tint");
                Assert.That(badge.canvasRenderer.cull, Is.False, $"{side} {row} badge renderer");
                bool neutral = badge.color == (Color)new Color32(67, 86, 100, 255);
                Assert.That(neutral, Is.EqualTo(empty), $"{side} {row} badge color");
            }
        }
    }

    private static Rect LayoutRect(RectTransform rect, Transform reference)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 min = reference.InverseTransformPoint(corners[0]);
        Vector3 max = reference.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
