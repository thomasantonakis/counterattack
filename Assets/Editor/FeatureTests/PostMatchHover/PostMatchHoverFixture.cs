using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunPostMatchHover(bool lineup)
    {
        Board(1);
        var camera = Child("Owned inspection camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.gameObject.SetActive(true);
        camera.transform.position = new Vector3(0, 25, -15);
        camera.transform.LookAt(attacker.transform);
        var canvasObject = new GameObject("Owned hover label", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(root.transform);
        var textObject = new GameObject("Owned hover text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform);
        Set(input, "hoverNameCanvas", canvasObject.GetComponent<Canvas>());
        Set(input, "hoverNameRect", textObject.GetComponent<RectTransform>());
        Set(input, "hoverNameLabel", textObject.GetComponent<TMPro.TextMeshProUGUI>());
        GameObject panel = null;
        try
        {
            Assert.That(EndGamePanelManager.IsVisible, Is.False);
            if (lineup) input.SetLineupHoverToken(attacker);
            else Invoke(input, "ApplyHoverTarget", attacker, attacker.currentHex, true);
            Invoke(input, "UpdateHoverNameLabel");
            Assert.That(canvasObject.activeSelf, Is.True, "Names still appear during ordinary match inspection.");
            panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath), root.transform);
            panel.SetActive(true);
            Assert.That(EndGamePanelManager.IsVisible, Is.True);
            Invoke(input, "UpdateHoverNameLabel");
            Assert.That(canvasObject.activeSelf, Is.False, "An already-visible label disappears as Full Time opens.");
            input.SetLineupHoverToken(attacker);
            Invoke(input, "ApplyHoverTarget", attacker, attacker.currentHex, true);
            Invoke(input, "ProcessInputs");
            Invoke(input, "UpdateHoverNameLabel");
            Assert.That(canvasObject.activeSelf, Is.False);
            Assert.That(input.InspectionHoverToken, Is.Null);
            Assert.That(input.hoveredToken, Is.Null);
            panel.SetActive(false);
            Assert.That(EndGamePanelManager.IsVisible, Is.False);
            if (lineup) input.SetLineupHoverToken(attacker);
            else Invoke(input, "ApplyHoverTarget", attacker, attacker.currentHex, true);
            Invoke(input, "UpdateHoverNameLabel");
            Assert.That(canvasObject.activeSelf, Is.True, "Closing the report restores ordinary inspection.");
            yield return null;
        }
        finally { if (panel != null) Object.DestroyImmediate(panel); }
    }
}
