using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PostMatchDropdownFixture
{
    public static IEnumerator Run()
    {
        var canvasRoot = new GameObject("Owned dropdown canvas", typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        GameObject panel = null;
        try
        {
            panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath), canvasRoot.transform);
            panel.SetActive(true);
            var data = new MatchManager.GameData();
            data.events.Add(new GameplayEvent { sequenceNumber = 1, currentHalf = 4, kind = "snapshot.action_boundary",
                snapshot = new GameplaySnapshotSummary { currentHalf = 4, currentState = "StandardPassCompleted",
                    tokens = new() { new GameplayTokenStateSummary { tokenKey = "Home:1", status = "playing", hex = new RoomHexCoordinates { x = -10, z = 0 } } } } });
            var report = panel.GetComponent<PostMatchReportController>();
            report.ShowReport(data, true);
            report.ShowPitch();
            yield return null;
            foreach (var dropdown in panel.GetComponentsInChildren<PostMatchDropdown>(true))
            {
                dropdown.Show();
                yield return null;
                var list = dropdown.transform.Find("Dropdown List");
                Assert.That(list, Is.Not.Null, dropdown.name);
                var menu = list.GetComponent<RectTransform>();
                Assert.That(menu.rect.height, Is.EqualTo(dropdown.options.Count * PostMatchDropdown.RowHeight + PostMatchDropdown.Padding).Within(.1f));
                var scroll = list.GetComponent<ScrollRect>();
                Assert.That(scroll.vertical || scroll.horizontal, Is.False);
                var items = list.GetComponentsInChildren<Toggle>().Where(t => t.gameObject.activeSelf).ToArray();
                Assert.That(items.Length, Is.EqualTo(dropdown.options.Count));
                foreach (var item in items)
                {
                    var corners = new Vector3[4];
                    item.GetComponent<RectTransform>().GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var local = scroll.viewport.InverseTransformPoint(corner);
                        Assert.That(local.y, Is.InRange(scroll.viewport.rect.yMin - .1f, scroll.viewport.rect.yMax + .1f), "Every option must fit without scrolling.");
                    }
                }
                items.Last().isOn = true;
                Assert.That(dropdown.value, Is.EqualTo(dropdown.options.Count - 1));
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(dropdown.transform.Find("Dropdown List"), Is.Null);
                dropdown.Show();
                yield return null;
                Assert.That(dropdown.transform.Find("Dropdown List").GetComponentsInChildren<Toggle>().Count(t => t.gameObject.activeSelf), Is.EqualTo(dropdown.options.Count), "Reopening must not duplicate options.");
                dropdown.Hide();
                yield return new WaitForSecondsRealtime(.25f);
            }
        }
        finally
        {
            if (panel != null) Object.DestroyImmediate(panel);
            Object.DestroyImmediate(canvasRoot);
        }
    }
}
