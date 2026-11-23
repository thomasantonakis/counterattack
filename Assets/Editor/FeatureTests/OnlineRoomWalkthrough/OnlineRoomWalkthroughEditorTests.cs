using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[Category("OnlineRoomWalkthrough")]
public sealed class OnlineRoomWalkthroughEditorTests
{
    [TestCase(16)]
    [TestCase(18)]
    public void DraftFooterAndAveragesUseAuthoredPanels(int squadSize)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Draft.unity");
        try
        {
            var view = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OnlineDraftView>(true)).Single();
            var version = new OnlineCompatibility("draft-test", "rules", "catalog");
            var protocol = new OnlineMatchProtocol("draft-test", "home", "away", "home", version);
            protocol.BindAuthenticatedAccount("home", "h", version);
            protocol.BindAuthenticatedAccount("away", "a", version);
            var settings = OnlineSetupAgreementEditorTests.Valid();
            settings.squadSize = squadSize.ToString();
            var scores = new System.Collections.Generic.Queue<int>(new[] { 6, 2 });
            double time = 0;
            var authority = new OnlineDraftAuthority(protocol, settings, OnlineDraftCatalog.Load(), _ => 0, scores.Dequeue, () => time);
            var client = new OnlineDraftClient(protocol.MatchId, OnlineTeam.Home);
            client.Requested += _ => { };
            Assert.That(client.Apply(authority.Capture()), Is.True);
            view.Configure(client);
            typeof(OnlineDraftView).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
            var panel = view.source.draftOrderPanel;
            Assert.That(panel.footer.text, Does.Contain("Press [R] to roll").And.Contain("Auto-roll in"));
            Assert.That(panel.footer.color, Is.EqualTo(Color.white));
            Assert.That(view.rollPanelButton.GetComponent<Animator>(), Is.Null);
            Assert.That(view.rollPanelButton.targetGraphic.color.a, Is.Zero);
            Assert.That(view.rollPanelButton.interactable, Is.True);
            Assert.That(panel.homeDie.IsAnimating && !panel.awayDie.IsAnimating, Is.True);

            Assert.That(authority.AdvanceAutomaticRoll(), Is.False);
            time = 6;
            Assert.That(authority.AdvanceAutomaticRoll(), Is.True);
            Assert.That(client.Apply(authority.Capture()), Is.True);
            Assert.That(panel.homeDie.DisplayedRoll, Is.EqualTo(6));
            Assert.That(panel.awayDie.IsAnimating, Is.True);
            Assert.That(view.rollPanelButton.gameObject.activeSelf, Is.False);
            Assert.That(authority.AdvanceAutomaticRoll(), Is.False);
            time = 12;
            Assert.That(authority.AdvanceAutomaticRoll(), Is.True);
            time = 18;
            Assert.That(authority.AdvanceRollResult(), Is.True);
            var draft = authority.Capture();
            var card = draft.cards.First();
            var pick = new OnlineDraftRequest
            {
                matchId = draft.matchId, revision = draft.revision, sequence = draft.homeSequence + 1,
                intent = OnlineDraftIntent.Pick, cardId = card.id, slot = 2
            };
            Assert.That(authority.Submit("h", pick).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(client.Apply(authority.Capture()), Is.True);
            foreach (var averages in new[] { view.source.homeAveragePanel, view.source.awayAveragePanel })
            {
                Assert.That(averages.activeInHierarchy, Is.True);
                var labels = averages.GetComponentsInChildren<TMP_Text>().Select(label => label.text);
                Assert.That(labels, Does.Contain("Starting XI").And.Contain("Team Average"));
            }
            string pace = view.source.homeAveragePanel.transform.Find("Home-XI/ContentWrapper/PaceInSlot").GetComponent<TMP_Text>().text;
            Assert.That(pace, Is.EqualTo(card.pace.ToString("F1")));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase("UI/PlayerCardPrefab", false)]
    [TestCase("UI/GoalKeeperCardPrefab", true)]
    public void SmallCardKeepsAuthoredAttributeCircles(string asset, bool goalkeeper)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var stats = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MatchStatsUI>(true)).Single();
            var card = Object.Instantiate(Resources.Load<GameObject>(asset));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(card, scene);
            var badges = card.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.EndsWith("ValueBadge")).ToArray();
            Assert.That(badges.Length, Is.EqualTo(7));
            var authored = badges.Select(badge => (badge.sprite, badge.rectTransform.sizeDelta, badge.rectTransform.anchoredPosition)).ToArray();
            foreach (var value in card.GetComponentsInChildren<TMP_Text>(true).Where(value => value.name.EndsWith("Value")))
                value.text = "5";
            typeof(MatchStatsUI).GetMethod("ApplyPreviewCardTheme", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(stats, new object[] { card, goalkeeper, string.Empty });
            Assert.That(badges.Select(badge => (badge.sprite, badge.rectTransform.sizeDelta, badge.rectTransform.anchoredPosition)),
                Is.EqualTo(authored));
            Assert.That(badges.Select(badge => badge.color), Is.All.EqualTo(MatchStatsUI.GetInspectionAttributeColor(5)));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AuthoredSmallGoalkeeperBadgesMatchOutfield(bool home)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var stats = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MatchStatsUI>(true)).Single();
            var bindings = new UnityEditor.SerializedObject(stats);
            string side = home ? "home" : "away";
            var outfield = (PlayerCard)bindings.FindProperty(side + "HoverCard").objectReferenceValue;
            var keeper = (GoalkeeperCard)bindings.FindProperty(side + "GoalkeeperHoverCard").objectReferenceValue;
            var reference = outfield.paceValueText.transform.parent.Find(outfield.paceValueText.name + "Badge").GetComponent<Image>();
            var ratings = new[] { keeper.aerialValueText, keeper.dribblingValueText, keeper.paceValueText,
                keeper.resilienceValueText, keeper.savingValueText, keeper.handlingValueText, keeper.highPassValueText };
            var theme = typeof(MatchStatsUI).GetMethod("ApplyPreviewCardTheme", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (int score in new[] { 2, 3, 5 })
            {
                outfield.paceValueText.text = score.ToString();
                foreach (var rating in ratings) rating.text = score.ToString();
                theme.Invoke(stats, new object[] { outfield.gameObject, false, string.Empty });
                theme.Invoke(stats, new object[] { keeper.gameObject, true, string.Empty });
                foreach (var rating in ratings)
                {
                    var badge = rating.transform.parent.Find(rating.name + "Badge")?.GetComponent<Image>();
                    Assert.That(badge, Is.Not.Null, rating.name);
                    Assert.That(badge.sprite, Is.SameAs(reference.sprite));
                    Assert.That(badge.rectTransform.sizeDelta, Is.EqualTo(reference.rectTransform.sizeDelta));
                    Assert.That(badge.color, Is.EqualTo(reference.color));
                    Assert.That(badge.transform.GetSiblingIndex(), Is.LessThan(rating.transform.GetSiblingIndex()));
                }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void MovementTokenNumberKeepsTheAuthoredCentre()
    {
        var prefab = Resources.Load<MovementPhaseMovedTokenSlotView>("UI/HUD/PlayerSlot");
        var slot = Object.Instantiate(prefab);
        try
        {
            var number = slot.transform.Find("Number").GetComponent<TMP_Text>();
            var face = slot.transform.Find("Face").GetComponent<RectTransform>();
            var style = new TokenStyleDefinition { numberFaceOffset = -0.1f, numberColor = Color.white };
            slot.Render(style, 10);
            Assert.That(number.text, Is.EqualTo("10"));
            Assert.That(number.rectTransform.TransformPoint(number.rectTransform.rect.center),
                Is.EqualTo(face.TransformPoint(face.rect.center)));
        }
        finally { Object.DestroyImmediate(slot.gameObject); }
    }
}
