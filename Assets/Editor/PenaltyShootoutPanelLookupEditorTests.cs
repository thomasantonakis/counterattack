using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class PenaltyShootoutPanelLookupEditorTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene scene;
    private GameObject root;
    private MatchManager match, previousMatch;
    private PenaltyShootoutManager shootout;
    private PenaltyShootoutOrderPanelController panel;

    [SetUp]
    public void SetUp()
    {
        previousMatch = MatchManager.Instance;
        scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        root = new GameObject("Shootout lookup fixture"); root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
        match.gameData = new MatchManager.GameData();
        match.gameData.gameSettings.halfDuration = 15;
        match.gameData.gameSettings.numberOfHalfs = 1;
        match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup { homeTakesFirstPenalty = false, useRightGoal = true };
        match.currentState = MatchManager.GameState.MatchEnded;
        match.MarkPenaltyShootoutComplete();
        match.playerTokenManager = root.AddComponent<PlayerTokenManager>();
        shootout = root.AddComponent<PenaltyShootoutManager>(); shootout.matchManager = match;
        panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<PenaltyShootoutOrderPanelController>(true)).Single();
        for (int team = 0; team < 2; team++)
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Nominee"); go.SetActive(false); go.transform.SetParent(root.transform);
            var token = go.AddComponent<PlayerToken>();
            token.isHomeTeam = team == 0; token.playerName = $"Team {team} player {i}"; token.shooting = i + 3;
            match.playerTokenManager.allTokens.Add(token);
        }
    }

    [TearDown]
    public void TearDown()
    {
        MatchManager.Instance = previousMatch;
        if (root != null) Object.DestroyImmediate(root);
        EditorSceneManager.ClosePreviewScene(scene);
    }

    [Test]
    public void FindsInactiveAuthoredPanelDespiteCompetingCanvasAndKeepsDesignerParent()
    {
        var decoy = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        SceneManager.MoveGameObjectToScene(decoy, scene);
        Transform parent = panel.transform.parent;
        Assert.That(panel.gameObject.activeSelf, Is.False);
        shootout.ShowOrderSelection();
        Assert.That(panel.transform.parent, Is.SameAs(parent));
        Assert.That(Get("orderPanel"), Is.SameAs(panel));
        Assert.That(panel.gameObject.activeSelf, Is.True);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PenaltyShootoutOrderSelection));
        Assert.That(shootout.IsOrderSelectionOrShootoutStarted, Is.True);
        Assert.That(shootout.homeOrder.Count, Is.EqualTo(2));
        Assert.That(panel.GetOrder(false).Count, Is.EqualTo(2));
        var view = panel.GetComponent<PenaltyShootoutOrderPanelView>();
        Assert.That(view.homeRows[2].gameObject.activeSelf, Is.False);
        Assert.That(view.homeRows[2].nameText.text, Is.Empty);
        Assert.That(match.gameData.penaltyShootoutSetup.homeTakesFirstPenalty, Is.False);
        Assert.That(match.gameData.penaltyShootoutSetup.useRightGoal, Is.True);
        shootout.ShowOrderSelection();
        Assert.That(panel.gameObject.activeSelf, Is.True, "Duplicate entry must retain the pending selection UI.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingOrIncompletePanelDoesNotChangeStateAndCanBeRetried(bool incomplete)
    {
        var view = panel.GetComponent<PenaltyShootoutOrderPanelView>();
        var start = view.startButton;
        if (incomplete) view.startButton = null;
        else panel.name = "Unbound panel";
        LogAssert.Expect(LogType.Error, incomplete
            ? "[Shootout] PenaltyShootoutOrderPanel scene references are incomplete. Use Tools/Counter Attack/Ensure Scene-Owned Match Panels In Room."
            : "[Shootout] Missing designer-owned PenaltyShootoutOrderPanel in the match scene.");
        Assert.DoesNotThrow(shootout.ShowOrderSelection);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.MatchEnded));
        Assert.That(shootout.IsOrderSelectionOrShootoutStarted, Is.False);
        Assert.That(shootout.homeOrder, Is.Empty);
        view.startButton = start; panel.name = "PenaltyShootoutOrderPanel";
        shootout.ShowOrderSelection();
        Assert.That(panel.gameObject.activeSelf, Is.True);
    }

    private object Get(string field) => typeof(PenaltyShootoutManager).GetField(field, Private).GetValue(shootout);
}
