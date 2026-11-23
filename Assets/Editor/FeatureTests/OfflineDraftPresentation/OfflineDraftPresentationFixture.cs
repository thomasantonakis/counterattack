using System;
using System.Collections;
using System.IO;
using System.Linq;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class OfflineDraftPresentationFixture
{
    private ApplicationManager app;
    private string previousMode, previousSave, temporarySave;
    private bool createdApp, hadExplicitSave;
    private UnityEngine.Random.State random;
    private float previousScale;

    private IEnumerator OpenDraft(string mode, bool? homeWins)
    {
        random = UnityEngine.Random.state;
        previousScale = Time.timeScale;
        createdApp = ApplicationManager.Instance == null;
        ApplicationManager.EnsureInstanceExists();
        app = ApplicationManager.Instance;
        previousMode = app.SelectedRoomGameMode;
        previousSave = app.LastSavedFileName;
        hadExplicitSave = app.HasExplicitSaveContext;
        app.SetSelectedRoomGameMode(mode);
        var settings = OnlineSetupAgreementEditorTests.Valid();
        settings.gameMode = mode;
        settings.homeTeamControl = "Human";
        settings.awayTeamControl = mode == ApplicationManager.SinglePlayerGameMode ? "CPU" : "Human";
        temporarySave = Path.Combine(Path.GetTempPath(), $"counterattack-draft-{Guid.NewGuid():N}.json");
        File.WriteAllText(temporarySave, JsonConvert.SerializeObject(new { gameSettings = settings }));
        app.SetActiveSaveFilePath(temporarySave);
        UnityEngine.Random.InitState(20261123);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/Scenes/Draft.unity", new LoadSceneParameters(LoadSceneMode.Single));

        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        Assert.That(view.source.isActiveAndEnabled, Is.True);
        Assert.That(view.onlineRoots.All(root => root == null || !root.activeInHierarchy), Is.True);
        var dice = view.source.draftOrderPanel;
        float readyBy = Time.realtimeSinceStartup + 5f;
        while (!dice.IsOpen && Time.realtimeSinceStartup < readyBy) yield return null;
        Assert.That(dice.IsOpen, Is.True, "The real Draft startup opens the first-pick panel");
        if (!homeWins.HasValue) yield break;
        dice.Roll(new RollInputOverride { hasOverride = true, roll = homeWins.Value ? 6 : 1 });
        dice.Roll(new RollInputOverride { hasOverride = true, roll = homeWins.Value ? 1 : 6 });
        dice.AdvanceResult(Time.unscaledTime + 3f);
        yield return null;
        yield return null;
        Assert.That(view.source.GetCurrentTeamTurn(), Is.EqualTo(homeWins.Value ? "Home" : "Away"));
    }

    public IEnumerator RunAutomaticRoll(string mode, bool kickoff)
    {
        yield return OpenDraft(mode, kickoff ? true : null);
        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        var coin = view.sourceUi.coinTossPanel;
        var panel = kickoff ? coin.dicePanel : view.source.draftOrderPanel;
        if (kickoff) Assert.That(coin.Open(view.source.currentSettings), Is.True);
        Assert.That(panel.IsAwaitingHomeRoll, Is.True);
        Assert.That(panel.footer.text, Does.Contain("Auto-roll in 5s"));
        float deadline = Time.realtimeSinceStartup + 7f;
        while (panel.IsAwaitingHomeRoll && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(panel.IsAwaitingAwayRoll, Is.True);
        Assert.That(panel.homeDie.DisplayedRoll, Is.InRange(1, 6));
        Assert.That(panel.footer.text, Does.Contain("Auto-roll in 5s"));
        deadline = Time.realtimeSinceStartup + 7f;
        while (panel.IsAwaitingAwayRoll && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(panel.IsAwaitingAwayRoll, Is.False);
        Assert.That(panel.awayDie.DisplayedRoll, Is.InRange(1, 6));
    }

    public IEnumerator RunFooter(string mode)
    {
        yield return OpenDraft(mode, null);
        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        var panel = view.source.draftOrderPanel;
        var footer = panel.footer;
        Canvas.ForceUpdateCanvases();
        footer.ForceMeshUpdate();
        Assert.That(footer.rectTransform.rect.width, Is.GreaterThan(0f));
        Assert.That(footer.textInfo.lineCount, Is.EqualTo(1));
        Assert.That(footer.preferredWidth, Is.LessThanOrEqualTo(footer.rectTransform.rect.width));
        var kickoff = view.sourceUi.coinTossPanel.dicePanel.footer;
        Assert.That(footer.font, Is.SameAs(kickoff.font));
        Assert.That(footer.rectTransform.anchoredPosition, Is.EqualTo(kickoff.rectTransform.anchoredPosition));
        Assert.That(footer.rectTransform.pivot, Is.EqualTo(kickoff.rectTransform.pivot));
        Vector3[] promptCorners = new Vector3[4], outcomeCorners = new Vector3[4];
        footer.rectTransform.GetWorldCorners(promptCorners);
        panel.outcome.rectTransform.GetWorldCorners(outcomeCorners);
        Assert.That(promptCorners[1].y, Is.LessThan(outcomeCorners[0].y), "The footer belongs below the result");
        Assert.That(view.rollPanelButton.gameObject.activeSelf, Is.False);
        Assert.That(((RectTransform)view.rollPanelButton.transform).rect.width, Is.EqualTo(footer.rectTransform.rect.width));
    }

    public IEnumerator RunCards(string mode)
    {
        yield return OpenDraft(mode, true);
        var draft = Object.FindAnyObjectByType<DraftManager>();
        var cards = draft.draftPanel.GetComponentsInChildren<PlayerCard>();
        Assert.That(cards, Has.Length.EqualTo(4));
        foreach (var card in cards)
        {
            var values = new[] { card.paceValueText, card.dribblingValueText, card.headingValueText,
                card.highPassValueText, card.resilienceValueText, card.shootingValueText, card.tacklingValueText };
            var expected = new[] { card.assignedPlayer.Pace, card.assignedPlayer.Dribbling, card.assignedPlayer.Heading,
                card.assignedPlayer.HighPass, card.assignedPlayer.Resilience, card.assignedPlayer.Shooting, card.assignedPlayer.Tackling };
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                Assert.That(value.text, Is.EqualTo(expected[i].ToString()));
                var badge = value.transform.parent.Find(value.name + "Badge").GetComponent<Image>();
                Assert.That(badge.transform.GetSiblingIndex(), Is.LessThan(value.transform.GetSiblingIndex()),
                    $"{value.name} must render in front of its circle");
                Assert.That(badge.sprite.name, Is.EqualTo("Disc"));
                Assert.That(badge.rectTransform.sizeDelta, Is.EqualTo(new Vector2(45, 45)));
            }
        }
    }

    public IEnumerator RunDrop(string mode, bool homeWins)
    {
        yield return OpenDraft(mode, homeWins);
        var draft = Object.FindAnyObjectByType<DraftManager>();
        var card = draft.draftPanel.GetComponentsInChildren<PlayerCard>().First();
        var drag = card.GetComponent<PlayerCardDragHandler>();
        var own = homeWins ? draft.homeTeamPanel : draft.awayTeamPanel;
        var other = homeWins ? draft.awayTeamPanel : draft.homeTeamPanel;
        PlayerSlotDropHandler Slot(GameObject roster, int jersey) =>
            roster.GetComponentsInChildren<PlayerSlotDropHandler>().Single(slot => slot.GetJerseyNumber() == jersey);
        var permitted = Slot(own, 2);
        var forbidden = Slot(other, 2);
        var keeper = Slot(own, 1);
        var border = permitted.transform.Find("Online Drop Border");
        Assert.That(border, Is.Not.Null);
        string expected = card.assignedPlayer.Name;
        var input = new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left, pointerDrag = card.gameObject };
        drag.OnBeginDrag(input);
        input.dragging = true;
        void Hover(PlayerSlotDropHandler slot)
        {
            input.pointerCurrentRaycast = new RaycastResult { gameObject = slot.gameObject };
            drag.OnDrag(input);
        }
        Hover(permitted);
        Assert.That(border.gameObject.activeInHierarchy, Is.True, "The first hovered slot needs a four-sided border");
        Assert.That(border.GetComponentsInChildren<Image>(), Has.Length.EqualTo(4));
        Hover(forbidden);
        Assert.That(border.gameObject.activeSelf, Is.False);
        Assert.That(forbidden.transform.Find("Online Drop Border").gameObject.activeSelf, Is.False);
        Hover(keeper);
        Assert.That(border.gameObject.activeSelf, Is.False);
        Assert.That(forbidden.IsSlotPopulated(), Is.False);
        Hover(permitted);
        permitted.OnDrop(input);
        drag.OnEndDrag(input);
        yield return null;
        Assert.That(border.gameObject.activeSelf, Is.False);
        Assert.That(permitted.transform.Find("ContentWrapper/PlayerNameInSlot").GetComponent<TMPro.TMP_Text>().text,
            Is.EqualTo(expected));
    }

    public IEnumerator Cleanup()
    {
        if (app == null) yield break;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        app.SetSelectedRoomGameMode(previousMode);
        if (hadExplicitSave) app.SetActiveSaveFilePath(previousSave);
        else
        {
            app.ClearActiveSaveContext();
            app.LastSavedFileName = previousSave;
        }
        if (createdApp) Object.Destroy(app.gameObject);
        if (temporarySave != null) File.Delete(temporarySave);
        UnityEngine.Random.state = random;
        Time.timeScale = previousScale;
    }
}
