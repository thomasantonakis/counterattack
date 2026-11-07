using NUnit.Framework;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PenaltyDotPresentationEditorTests
{
    [TestCase(0)] [TestCase(1)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(12)]
    public void DotsShowLatestFiveAndPadPending(int attempts)
    {
        var results = Enumerable.Range(0, attempts).Select(i => i % 3 != 0).ToArray();
        var expected = results.Skip(System.Math.Max(0, attempts - 5)).Select(x => (bool?)x).ToList();
        while (expected.Count < 5) expected.Add(null);
        Assert.That(Enumerable.Range(0, 5).Select(i => ScoreboardManager.GetPenaltyDotResult(results, i)), Is.EqualTo(expected));
    }

    [Test]
    public void RoomHasTenSceneOwnedDotsWithNoRaycasts()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var board = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ScoreboardManager>(true)).Single(x => x.name == "ScoreBoardManager");
            Assert.That(board.penaltyDots.transform.parent, Is.EqualTo(board.transform));
            typeof(ScoreboardManager).GetMethod("RefreshPenaltyDots", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(board, new object[] { false });
            Assert.That(board.penaltyDots.activeSelf, Is.False);
            Assert.That(board.timeText.transform.parent.gameObject.activeSelf, Is.True);
            Assert.That(board.homePenaltyDots.Length, Is.EqualTo(5));
            Assert.That(board.awayPenaltyDots.Length, Is.EqualTo(5));
            foreach (var dot in board.homePenaltyDots.Concat(board.awayPenaltyDots))
            {
                Assert.That(dot, Is.Not.Null);
                Assert.That(dot.sprite, Is.Not.Null);
                Assert.That(dot.raycastTarget, Is.False);
                Assert.That(dot.color, Is.EqualTo(board.pendingPenaltyColor));
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
