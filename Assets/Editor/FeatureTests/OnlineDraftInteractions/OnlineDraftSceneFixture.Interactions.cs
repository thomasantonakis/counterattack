using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed partial class OnlineDraftSceneFixture
{
    private OnlineFrontendSession draftSpectator;

    private IEnumerator OpenSpectatorSession()
    {
        Assert.That(OnlineFrontendSession.Current, Is.Null);
        app = ApplicationManager.Instance;
        created = app == null;
        if (created) { ApplicationManager.EnsureInstanceExists(); app = ApplicationManager.Instance; }
        previousMode = app.SelectedRoomGameMode;
        previousSave = app.LastSavedFileName;
        explicitSave = app.HasExplicitSaveContext;
        app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);

        yield return SceneManager.LoadSceneAsync(OnlineModeMenu.SceneName);
        var menu = Object.FindAnyObjectByType<OnlineModeMenu>();
        draftSpectator = Object.Instantiate(menu.sessionPrefab);
        var type = typeof(OnlineFrontendSession);
        type.GetProperty("IsSpectator", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(draftSpectator, true);
        type.GetProperty(nameof(OnlineFrontendSession.LobbyCode)).SetValue(draftSpectator, "AB12CD34");
    }

    public IEnumerator RunSpectatorExit(string mode)
    {
        yield return OpenSpectatorSession();
        var settings = OnlineSetupAgreementEditorTests.Valid();
        settings.draft = mode == "Setup" ? "Regular" : mode;
        const string matchId = "spectator-pregame-exit";
        bool setupScene = mode == "Setup";
        var draft = setupScene ? null : new OnlineDraftState
        {
            settings = settings, phase = "pick", selection = "outfielders", turn = OnlineTeam.Home,
            freeSelection = mode != "Regular", repeatable = mode == "Arcade",
            home = new OnlineDraftCard[16], away = new OnlineDraftCard[16],
            cards = new[] { OnlineDraftCatalog.Load().First(card => !card.goalkeeper) }
        };
        Assert.That(OnlineSpectatorPregameWire.TryEncode(matchId, 1, settings,
            setupScene ? new OnlineSetupState { settings = settings } : null, draft, out var payload), Is.True);
        var stream = new OnlineSpectatorStream(matchId);
        foreach (var packet in stream.Encode(payload)) stream.Receive(packet, packet.Length);
        var type = typeof(OnlineFrontendSession);
        type.GetProperty("SpectatorPublication").SetValue(draftSpectator, stream.Latest);
        type.GetMethod("FollowSpectatorStage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(draftSpectator, null);
        yield return WaitForSpectatorScene(setupScene ? "CreateNewGameScene" : "Draft");

        Button back;
        TMP_InputField code;
        if (setupScene)
        {
            var view = Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();
            back = view.backButton;
            code = view.sessionLobbyCode;
            Assert.That(view.setup.homeTeamInputField.interactable, Is.False);
        }
        else
        {
            var view = Object.FindAnyObjectByType<OnlineDraftView>();
            back = view.leaveButton;
            code = view.sessionLobbyCode;
            Assert.That(view.backToMatchSetupButton.gameObject.activeSelf, Is.False);
            Assert.That(view.actionButton.interactable, Is.False);
        }
        Assert.That(code.gameObject.activeInHierarchy && code.interactable && code.readOnly, Is.True);
        Assert.That(code.text, Is.EqualTo("AB12CD34"));
        Assert.That(code.textComponent.fontSize, Is.EqualTo(24f));
        var title = code.GetComponent<OnlineLobbyCodeTitle>().title;
        var rect = (RectTransform)code.transform;
        Assert.That(rect.parent, Is.SameAs(title.rectTransform));
        if (!setupScene)
            Assert.That(title.transform.IsChildOf(GameObject.Find("MainCanvas/SceneTitle").transform), Is.True);
        Assert.That(rect.localPosition.x - title.textBounds.max.x, Is.EqualTo(16f).Within(.1f));
        var glyphSize = code.textComponent.GetPreferredValues(code.text);
        Assert.That(rect.rect.width - glyphSize.x, Is.InRange(19f, 22f));
        Assert.That(rect.rect.height - glyphSize.y, Is.InRange(11f, 14f));
        string originalClipboard = GUIUtility.systemCopyBuffer;
        try
        {
            code.selectionStringAnchorPosition = 0;
            code.selectionStringFocusPosition = code.text.Length;
            var modifier = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX
                ? EventModifiers.Command : EventModifiers.Control;
            typeof(TMP_InputField).GetMethod("KeyPressed", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(code, new object[] { new Event { type = EventType.KeyDown, keyCode = KeyCode.C, modifiers = modifier } });
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(code.text));
        }
        finally { GUIUtility.systemCopyBuffer = originalClipboard; }
        Assert.That(back.gameObject.activeInHierarchy && back.interactable, Is.True);
        var label = back.GetComponentInChildren<TMP_Text>(true);
        label.ForceMeshUpdate();
        Assert.That(label.text, Is.EqualTo("Back to Online Menu"));
        Assert.That(label.textInfo.lineCount, Is.EqualTo(1));
        Assert.That(label.textBounds.size.x, Is.LessThanOrEqualTo(label.rectTransform.rect.width + 1));
        ExecuteEvents.Execute(back.gameObject, new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left
        }, ExecuteEvents.pointerClickHandler);
        yield return WaitForSpectatorScene(OnlineModeMenu.SceneName);
        Assert.That(OnlineFrontendSession.Current, Is.Null);
    }

    private static IEnumerator WaitForSpectatorScene(string scene)
    {
        float deadline = Time.realtimeSinceStartup + 8;
        while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
        yield return null;
        yield return null;
    }

    public IEnumerator RunSpectatorPool()
    {
        yield return OpenSpectatorSession();
        var type = typeof(OnlineFrontendSession);
        var follow = type.GetMethod("FollowSpectatorStage", BindingFlags.NonPublic | BindingFlags.Instance);
        const string matchId = "spectator-draft-pool";
        var stream = new OnlineSpectatorStream(matchId);
        var version = new OnlineCompatibility("spectator-draft", "test-rules", "test-catalog");
        var protocol = new OnlineMatchProtocol(matchId, "home", "away", "home", version);
        protocol.BindAuthenticatedAccount("home", "h", version);
        protocol.BindAuthenticatedAccount("away", "a", version);
        var settings = OnlineSetupAgreementEditorTests.Valid();
        settings.gkDraft = "Free";
        settings.draft = "Arcade";
        double time = 0;
        rolls.Enqueue(6);
        rolls.Enqueue(2);
        var draft = new OnlineDraftAuthority(protocol, settings, OnlineDraftCatalog.Load(), _ => 0, rolls.Dequeue, () => time);
        void Submit(OnlineTeam actor, OnlineDraftIntent intent, string card = null, int slot = 0)
        {
            var current = draft.Capture();
            var reply = draft.Submit(actor == OnlineTeam.Home ? "h" : "a", new OnlineDraftRequest
            {
                matchId = matchId, revision = current.revision, intent = intent, cardId = card, slot = slot,
                sequence = (actor == OnlineTeam.Home ? current.homeSequence : current.awaySequence) + 1
            });
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        void Publish(long sequence)
        {
            Assert.That(OnlineSpectatorPregameWire.TryEncode(matchId, sequence, settings, null, draft.Capture(), out var bytes), Is.True);
            foreach (var packet in stream.Encode(bytes)) stream.Receive(packet, packet.Length);
            type.GetProperty("SpectatorPublication").SetValue(draftSpectator, stream.Latest);
            follow.Invoke(draftSpectator, null);
        }

        Submit(OnlineTeam.Home, OnlineDraftIntent.Roll);
        Submit(OnlineTeam.Away, OnlineDraftIntent.Roll);
        Publish(1);
        float deadline = Time.realtimeSinceStartup + 8;
        while (SceneManager.GetActiveScene().name != "Draft" && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Draft"));
        yield return null;
        yield return null;
        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        Assert.That(view.Client, Is.Null);
        Assert.That(view.poolTable.activeSelf, Is.False);
        Assert.That(view.source.draftOrderPanel.visuals.activeSelf, Is.True);
        Assert.That(view.source.draftOrderPanel.homeResult.text, Is.EqualTo("Roll 6"));
        Assert.That(view.source.draftOrderPanel.awayResult.text, Is.EqualTo("Roll 2"));
        Assert.That(view.rollPanelButton.gameObject.activeSelf, Is.False);

        time = 2;
        Assert.That(draft.AdvanceRollResult(), Is.True);
        for (int i = 0; i < 4; i++)
        {
            var state = draft.Capture();
            Submit(state.turn, OnlineDraftIntent.Pick, state.available[0], i < 2 ? 1 : 12);
        }
        var keeper = draft.Capture().home[0];
        Publish(2);
        yield return null;
        yield return null;
        Assert.That(view.sourceUi.draftTurnText.text, Is.EqualTo("Managers choose outfielders"));
        Assert.That(view.sourceUi.draftBatchText.text, Is.EqualTo("outfielders"));
        Assert.That(view.poolTable.activeSelf || view.localPoolDragPreview.gameObject.activeSelf, Is.False);
        Assert.That(view.poolFilters.All(input => !input.interactable), Is.True);
        Assert.That(view.homeSlots[0].transform.Find("ContentWrapper/PlayerNameInSlot").GetComponent<TMP_Text>().text,
            Is.EqualTo(keeper.name));
    }

    private IEnumerator RunPoolInteractions(OnlineDraftView view, OnlineDraftClient client, bool arcade)
    {
        // Clone the authored scene roots together so Unity remaps their cross references.
        // Both presentations stay in the same normally loaded test scene.
        var roots = view.gameObject.scene.GetRootGameObjects();
        var group = new GameObject("Draft presentation fixture");
        foreach (var root in roots)
            if (root.GetComponent<EventSystem>() == null) root.transform.SetParent(group.transform, true);
        var staging = new GameObject("Inactive replica fixture");
        staging.SetActive(false);
        var cloned = Object.Instantiate(group, staging.transform);
        // The online view doesn't use AI. The duplicate AI singleton would destroy the cloned view's GameObject.
        foreach (var ai in cloned.GetComponentsInChildren<AIManager>(true)) Object.DestroyImmediate(ai);
        foreach (var system in cloned.GetComponentsInChildren<EventSystem>(true)) system.enabled = false;
        foreach (var listener in cloned.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        var replica = cloned.GetComponentInChildren<OnlineDraftView>(true);
        staging.SetActive(true);
        Assert.That(replica.rollPanelButton, Is.Not.SameAs(view.rollPanelButton));
        Assert.That(replica.sourceUi, Is.Not.SameAs(view.sourceUi));
        Assert.That(replica.homeSlots[0].GetComponent<OnlineDraftClickTarget>().view, Is.SameAs(replica));
        Assert.That(replica.poolRows[0].view, Is.SameAs(replica));
        var peer = Client(client.Team == OnlineTeam.Home ? OnlineTeam.Away : OnlineTeam.Home);
        view.Configure(client);
        replica.Configure(peer);
        yield return null;
        if (!arcade)
        {
            rolls.Enqueue(client.Team == OnlineTeam.Home ? 6 : 1);
            rolls.Enqueue(client.Team == OnlineTeam.Away ? 6 : 1);
            foreach (var side in new[] { OnlineTeam.Home, OnlineTeam.Away })
            {
                var rolling = side == client.Team ? view : replica;
                var passive = side == client.Team ? replica : view;
                Assert.That(rolling.source.draftOrderPanel.visuals.activeInHierarchy, Is.True);
                Assert.That(passive.source.draftOrderPanel.visuals.activeInHierarchy, Is.True);
                Assert.That(rolling.Client.Team, Is.EqualTo(side));
                Assert.That(rolling.Client.Capture().turn, Is.EqualTo(side));
                Assert.That(rolling.rollPanelButton.gameObject.activeSelf, Is.True, $"{side} roll control should be active");
                Assert.That(passive.rollPanelButton.gameObject.activeSelf, Is.False,
                    $"{passive.Client.Team} is waiting on {side}, action button {passive.actionButton.interactable}");
                rolling.rollPanelButton.onClick.Invoke();
                Flush();
                Assert.That(rolling.Client.LastReply, Is.EqualTo(OnlineCommandStatus.Accepted), $"{side} roll was submitted");
            }
            Assert.That(view.source.draftOrderPanel.visuals.activeSelf, Is.False);
        }
        yield return null;
        Canvas.ForceUpdateCanvases();
        Assert.That(view.poolTable.activeInHierarchy && replica.poolTable.activeInHierarchy, Is.True);
        Assert.That(view.cards.All(c => !c.gameObject.activeInHierarchy), Is.True);
        bool keeper = client.Capture().selection == "goalkeepers";
        if (!arcade)
        {
            int first = keeper ? 12 : 18;
            int next = keeper ? 1 : 2;
            Assert.That(view.poolFilters[0].interactable && view.poolHeaders[0].interactable, Is.True);
            Assert.That(replica.poolFilters[0].interactable || replica.poolHeaders[0].interactable, Is.False);
            var before = client.Capture();
            var waitingRows = RowIds(replica);
            replica.poolFilters[0].text = "impossible filter";
            replica.poolHeaders[0].onClick.Invoke();
            ClickPool(replica.poolRows[0], 2);
            var ignoredDrag = BeginPoolDrag(replica.poolRows[0]);
            Assert.That(replica.localPoolDragPreview.gameObject.activeSelf, Is.False);
            HoverPoolDrop(replica.poolRows[0], ignoredDrag, Slots(replica, peer.Team)[first - 1], false);
            Flush();
            Assert.That(client.Capture().revision, Is.EqualTo(before.revision));
            Assert.That(RowIds(replica), Is.EqualTo(waitingRows));
            Assert.That(replica.poolFilters[0].text, Is.Empty);

            view.poolHeaders[0].onClick.Invoke();
            Assert.That(RowIds(replica), Is.EqualTo(waitingRows));
            var selected = view.poolRows[0].PoolCard;
            view.poolFilters[0].text = selected.name;
            Assert.That(view.poolRows.Where(r => r.PoolCard != null).All(r => r.PoolCard.name.Contains(selected.name)), Is.True);
            var row = view.poolRows[0];
            var drag = BeginPoolDrag(row);
            Assert.That(view.localPoolDragPreview.gameObject.activeSelf, Is.True);
            HoverPoolDrop(row, drag, Slots(view, peer.Team)[first - 1], false);
            HoverPoolDrop(row, drag, Slots(view, client.Team)[keeper ? 1 : 0], false);
            HoverPoolDrop(row, drag, Slots(view, client.Team)[first - 1], true);
            Flush();
            Assert.That(view.localPoolDragPreview.assignedPlayer.Name, Is.EqualTo(selected.name));
            Assert.That(replica.freeDraftDragPreview.assignedPlayer.Name, Is.EqualTo(selected.name));
            Assert.That(view.localPoolDragPreview.playerNameText.text, Is.EqualTo(replica.freeDraftDragPreview.playerNameText.text));
            Assert.That(((RectTransform)view.localPoolDragPreview.transform).rect.size,
                Is.EqualTo(((RectTransform)replica.freeDraftDragPreview.transform).rect.size));
            FinishPoolDrag(row, drag, Slots(view, client.Team)[first - 1]);
            Flush();
            Assert.That(Roster(client)[first - 1].id, Is.EqualTo(selected.id));
            Assert.That(client.Capture().available, Does.Not.Contain(selected.id));
            Assert.That(replica.poolFilters[0].interactable && replica.poolHeaders[0].interactable, Is.True);
            Assert.That(view.poolFilters[0].interactable || view.poolHeaders[0].interactable, Is.False);
            Assert.That(view.localPoolDragPreview.gameObject.activeSelf, Is.False);

            string peerPick = replica.poolRows[0].PoolCard.id;
            ClickPool(replica.poolRows[0], 2);
            Flush();
            Assert.That(Roster(peer)[next - 1].id, Is.EqualTo(peerPick));
            Assert.That(view.poolFilters[0].text, Is.EqualTo(selected.name));
            view.poolFilters[0].text = string.Empty;
            row = view.poolRows[0];
            drag = BeginPoolDrag(row);
            HoverPoolDrop(row, drag, Slots(view, client.Team)[first - 1], false);
            FinishPoolDrag(row, drag, Slots(view, client.Team)[first - 1]);
            Flush();
            Assert.That(Roster(client)[next - 1], Is.Null);
            yield break;
        }

        foreach (bool keepers in new[] { true, false })
        {
            Assert.That(client.Capture().selection, Is.EqualTo(keepers ? "goalkeepers" : "outfielders"));
            Assert.That(view.poolFilters[0].interactable && replica.poolFilters[0].interactable, Is.True);
            var candidates = RowIds(replica);
            view.poolHeaders[0].onClick.Invoke();
            Assert.That(RowIds(replica), Is.EqualTo(candidates));
            var shared = view.poolRows[0].PoolCard;
            string other = replica.poolRows.First(r => r.PoolCard.name != shared.name).PoolCard.name;
            view.poolFilters[0].text = shared.name;
            replica.poolFilters[0].text = other;
            Assert.That(RowIds(replica), Does.Not.Contain(shared.id));
            Assert.That(view.poolFilters[0].text, Is.EqualTo(shared.name));
            Assert.That(replica.poolFilters[0].text, Is.EqualTo(other));
            Assert.That(view.poolHeaders[0].GetComponentInChildren<TMP_Text>().text, Does.EndWith("^"));
            Assert.That(replica.poolHeaders[0].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Name"));

            var left = view.poolRows.First(row => row.PoolCard?.id == shared.id);
            var right = replica.poolRows.First(row => row.PoolCard != null);
            var leftDrag = BeginPoolDrag(left);
            var rightDrag = BeginPoolDrag(right);
            Assert.That(left.view, Is.SameAs(view));
            Assert.That(right.view, Is.SameAs(replica));
            Assert.That(view.localPoolDragPreview.gameObject.activeSelf, Is.True, $"{client.Team} pool drag starts");
            Assert.That(replica.localPoolDragPreview.gameObject.activeSelf, Is.True, $"{peer.Team} pool drag starts");
            int first = keepers ? 1 : 2;
            HoverPoolDrop(left, leftDrag, Slots(view, client.Team)[first - 1], true);
            HoverPoolDrop(right, rightDrag, Slots(replica, peer.Team)[first - 1], true);
            Flush();
            Assert.That(replica.freeDraftDragPreview.gameObject.activeSelf, Is.True);
            Assert.That(replica.localPoolDragPreview.gameObject.activeSelf && view.localPoolDragPreview.gameObject.activeSelf, Is.True);
            Assert.That(RowIds(replica), Does.Not.Contain(shared.id));
            ExecuteEvents.Execute(left.gameObject, leftDrag, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(right.gameObject, rightDrag, ExecuteEvents.endDragHandler);
            Flush();

            replica.poolFilters[0].text = shared.name;
            ClickPool(replica.poolRows.First(row => row.PoolCard?.id == shared.id), 2);
            ClickPool(left, 2);
            Flush();
            Assert.That(Roster(client)[first - 1].id, Is.EqualTo(shared.id));
            Assert.That(Roster(peer)[first - 1].id, Is.EqualTo(shared.id));
            Assert.That(RowIds(view), Does.Contain(shared.id));

            int second = keepers ? 12 : 18;
            leftDrag = BeginPoolDrag(left);
            HoverPoolDrop(left, leftDrag, Slots(view, client.Team)[first - 1], false);
            HoverPoolDrop(left, leftDrag, Slots(view, client.Team)[second - 1], true);
            Flush();
            FinishPoolDrag(left, leftDrag, Slots(view, client.Team)[second - 1]);
            Flush();
            Assert.That(Roster(client)[second - 1].id, Is.EqualTo(shared.id));
            Assert.That(client.Capture().selection, Is.EqualTo(keepers ? "goalkeepers" : "outfielders"));
            if (keepers)
            {
                Assert.That(view.poolFilters[0].interactable, Is.True);
                Assert.That(replica.poolFilters[0].interactable, Is.True);
                ClickPool(replica.poolRows.First(row => row.PoolCard?.id == shared.id), 2);
                Flush();
                Assert.That(client.Phase, Is.EqualTo("review"));
                Assert.That(view.poolTable.activeInHierarchy && replica.poolTable.activeInHierarchy, Is.True);
                replica.actionButton.onClick.Invoke();
                Flush();
                Assert.That(view.poolTable.activeInHierarchy && replica.poolTable.activeInHierarchy, Is.True);
                var rosterSlot = Slots(view, client.Team)[second - 1].GetComponent<OnlineDraftClickTarget>();
                var rosterDrag = BeginPoolDrag(rosterSlot);
                Assert.That(ExecuteEvents.Execute(view.poolTable, rosterDrag, ExecuteEvents.dropHandler), Is.True);
                ExecuteEvents.Execute(rosterSlot.gameObject, rosterDrag, ExecuteEvents.endDragHandler);
                Flush();
                Assert.That(Roster(client)[second - 1], Is.Null);
                Assert.That(Roster(peer)[second - 1].id, Is.EqualTo(shared.id));
                Assert.That(client.Capture().homeReady || client.Capture().awayReady, Is.False);
                leftDrag = BeginPoolDrag(left);
                FinishPoolDrag(left, leftDrag, Slots(view, client.Team)[second - 1]);
                Flush();
                view.actionButton.onClick.Invoke();
                Flush();
                replica.actionButton.onClick.Invoke();
                Flush();
                Assert.That(client.Capture().selection, Is.EqualTo("outfielders"));
                Assert.That(view.poolFilters[0].text, Is.Empty);
                Assert.That(replica.poolFilters[0].text, Is.Empty);
            }
            else
            {
                var rosterSlot = Slots(replica, peer.Team)[first - 1].GetComponent<OnlineDraftClickTarget>();
                var rosterDrag = BeginPoolDrag(rosterSlot);
                Assert.That(ExecuteEvents.Execute(replica.poolRows[0].gameObject, rosterDrag, ExecuteEvents.dropHandler), Is.True);
                ExecuteEvents.Execute(rosterSlot.gameObject, rosterDrag, ExecuteEvents.endDragHandler);
                Flush();
                Assert.That(Roster(peer)[first - 1], Is.Null);
                Assert.That(Roster(client)[first - 1].id, Is.EqualTo(shared.id));
            }
        }
    }

    private static string[] RowIds(OnlineDraftView view) => view.poolRows.Select(row => row.PoolCard?.id).ToArray();

    private static OnlineDraftCard[] Roster(OnlineDraftClient client)
    {
        var state = client.Capture();
        return client.Team == OnlineTeam.Home ? state.home : state.away;
    }

    private static PlayerSlotDropHandler[] Slots(OnlineDraftView view, OnlineTeam team) => team == OnlineTeam.Home ? view.homeSlots : view.awaySlots;

    private static PointerEventData BeginPoolDrag(OnlineDraftClickTarget row)
    {
        var canvas = row.GetComponentInParent<Canvas>();
        var input = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left, pointerDrag = row.gameObject, dragging = true,
            position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, row.transform.position),
            pointerPressRaycast = new RaycastResult { gameObject = row.gameObject, module = canvas.GetComponent<GraphicRaycaster>() }
        };
        Assert.That(ExecuteEvents.Execute(row.gameObject, input, ExecuteEvents.beginDragHandler), Is.True);
        return input;
    }

    private static void HoverPoolDrop(OnlineDraftClickTarget row, PointerEventData input, PlayerSlotDropHandler target, bool highlighted)
    {
        input.pointerCurrentRaycast = new RaycastResult { gameObject = target.gameObject };
        ExecuteEvents.Execute(row.gameObject, input, ExecuteEvents.dragHandler);
        Assert.That(target.transform.Find("Online Drop Border").gameObject.activeSelf, Is.EqualTo(highlighted),
            $"{row.view.Client.Team} dragging {row.PoolCard?.id} over {target.name}");
    }

    private static void FinishPoolDrag(OnlineDraftClickTarget row, PointerEventData input, PlayerSlotDropHandler target)
    {
        ExecuteEvents.Execute(target.gameObject, input, ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(row.gameObject, input, ExecuteEvents.endDragHandler);
        Assert.That(target.transform.Find("Online Drop Border").gameObject.activeSelf, Is.False);
    }
}
