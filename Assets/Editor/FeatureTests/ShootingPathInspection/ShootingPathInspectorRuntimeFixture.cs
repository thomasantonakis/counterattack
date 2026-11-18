using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ShootingPathInspectorRuntimeFixture
{
    public IEnumerator Run()
    {
        if (Object.FindAnyObjectByType<ShootingPathInspector>() == null)
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ShootingPathInspectionAuthoring.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var tool = Object.FindAnyObjectByType<ShootingPathInspector>();
        Assert.That(tool, Is.Not.Null);
        Assert.That(tool.enabled, Is.True);
        Assert.That(tool.InspectionEnabled, Is.False);
        Assert.That(Object.FindAnyObjectByType<MatchManager>(), Is.Null);
        Assert.That(Object.FindAnyObjectByType<GameInputManager>(), Is.Null);
        int objects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Length;
        try
        {
            tool.SetInspectionEnabled(true);
            tool.HoverOrigin(tool.cells.Single(c => c.coordinates == new Vector3Int(12, 0, 6)));
            tool.ClickOrigin(tool.Origin);
            tool.SelectTarget(4);
            yield return null;
            Assert.That(tool.IsLocked, Is.True);
            Assert.That(tool.pathMarkers.Any(m => m.gameObject.activeSelf), Is.True);
            Assert.That(tool.corridor.gameObject.activeSelf, Is.True);
            Assert.That(tool.status.text, Does.Contain("LOCKED"));
            Assert.That(tool.status.text, Does.Contain("0.6474"));
            Assert.That(tool.dribblingReference.gameObject.activeSelf, Is.True);
            tool.SetPathView(ShootingPathInspector.PathView.PhysicalDisc);
            Assert.That(tool.physicalMarkers.Any(m => m.gameObject.activeSelf), Is.True);
            Assert.That(tool.corridor.localScale.x, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(tool.capsuleOutline.gameObject.activeSelf, Is.True);
            tool.SetPathView(ShootingPathInspector.PathView.Differences);
            Vector3 midpoint = (tool.Origin.GetHexCenter() + tool.SelectedTarget.GetHexCenter()) * 0.5f;
            tool.MoveBallToScreenPoint(tool.inspectionCamera.WorldToScreenPoint(midpoint));
            midpoint.y = tool.referenceBall.playerHeightOffset;
            Assert.That(Vector3.Distance(tool.movingBall.position, midpoint), Is.LessThan(0.0001f));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Length, Is.EqualTo(objects),
                "Every visual must be authored before Play Mode.");
            ScreenCapture.CaptureScreenshot("TestResults/GameplayRegression/Runs/shooting-inspection-runtime.png");
            yield return null;
            tool.enabled = false;
            yield return null;
            Assert.That(tool.InspectionEnabled, Is.False);
            Assert.That(tool.corridor.gameObject.activeSelf, Is.False);
            Assert.That(tool.targetMarkers.All(m => !m.gameObject.activeSelf), Is.True);
            Assert.That(tool.movingBall.gameObject.activeSelf, Is.False);
            Assert.That(tool.dribblingReference.gameObject.activeSelf, Is.False);
            Assert.That(tool.physicalMarkers.All(m => !m.gameObject.activeSelf), Is.True);
            Assert.That(tool.capsuleOutline.gameObject.activeSelf, Is.False);
        }
        finally
        {
            if (tool != null) { tool.SetInspectionEnabled(false); tool.enabled = true; }
        }
    }
}
