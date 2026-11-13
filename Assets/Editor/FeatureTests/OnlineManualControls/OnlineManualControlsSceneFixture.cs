using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class OnlineManualControlsSceneFixture
{
    private readonly Random.State random = Random.state;
    private readonly float scale = Time.timeScale;
    private OnlineGroundPassBoard board;
    private OnlineGroundPassBoard guest;
    private IEnumerator hostFlow, guestFlow;

    public IEnumerator RunTransport(int side, bool intercept)
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineGroundPassSceneAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        board = Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        if (side == -1) for (var i = 0; i < board.initialCoordinates.Length; i++) board.initialCoordinates[i].x *= -1;
        board.InitializeAuthority();
        if (side == -1) { board.match.homeTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft; board.match.awayTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight; }
        guest = new GameObject("Owned detached transport guest").AddComponent<OnlineGroundPassBoard>();
        guest.authorityRoot = new GameObject("Inert guest authority"); guest.authorityRoot.transform.SetParent(guest.transform); guest.authorityRoot.SetActive(false);
        guest.grid = board.grid; guest.tokens = board.tokens;
        guest.controls = Object.Instantiate(board.controls.gameObject, guest.transform).GetComponent<OnlineGroundPassControls>();
        guest.displayedBall = new GameObject("Guest ball value").transform; guest.displayedBall.SetParent(guest.transform);
        guest.displayedTokens = board.tokens.Select(token => { var value = new GameObject("Guest token value").transform; value.SetParent(guest.transform); return value; }).ToArray();
        var version = new OnlineCompatibility("manual", "rules", "fixture");
        var protocol = new OnlineMatchProtocol("transport", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "guest-home", version); protocol.BindAuthenticatedAccount("away", "host-away", version);
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        using var pause = new OnlineSharedPause(protocol, board.match);
        var type = typeof(OnlineGroundPassBoard).Assembly.GetType("CounterAttack.Online.EOSGroundPassExchange", true);
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(c => c.GetParameters().Length == 7);
        var hostInbox = new Queue<OnlineGroundPassMessage>(); var guestInbox = new Queue<OnlineGroundPassMessage>();
        var output = Path.GetFullPath("TestResults/GameplayRegression/Runs/" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-ManualTransport-" + side);
        Directory.CreateDirectory(output + "/host"); Directory.CreateDirectory(output + "/guest");
        double now = 0; bool dropped = false; long generation = 0; OnlineGroundPassMessage stale = null;
        OnlineGroundPassMessage Copy(OnlineGroundPassMessage message)
        {
            var wire = new OnlineGroundPassWire(); OnlineGroundPassMessage decoded = null;
            foreach (var fragment in OnlineGroundPassWire.Encode(message)) decoded = wire.Receive(fragment, fragment.Length);
            return decoded;
        }
        object hostExchange = null, guestExchange = null;
        Queue<OnlineGroundPassMessage> Inbox(object exchange) => (Queue<OnlineGroundPassMessage>)type.GetField("messages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(exchange);
        hostExchange = constructor.Invoke(new object[] { board, output + "/host", (Action<OnlineGroundPassMessage>)(message =>
            { generation = Math.Max(generation, message.connectionGeneration); if (!dropped) guestInbox.Enqueue(Copy(message)); }),
            (Action)(() => { while (hostInbox.Count > 0) Inbox(hostExchange).Enqueue(hostInbox.Dequeue()); }), "home", "transport", (Func<double>)(() => now) });
        guestExchange = constructor.Invoke(new object[] { guest, output + "/guest", (Action<OnlineGroundPassMessage>)(message =>
            { var copied = Copy(message); if (message.kind == "command" && message.sequence == 3) stale = copied; if (!dropped) hostInbox.Enqueue(copied); }),
            (Action)(() => { while (guestInbox.Count > 0) Inbox(guestExchange).Enqueue(guestInbox.Dequeue()); }), "away", "transport", (Func<double>)(() => now) });
        hostFlow = (IEnumerator)type.GetMethod("ManualHost", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hostExchange, new object[] { authority, protocol, pause });
        guestFlow = (IEnumerator)type.GetMethod("ManualGuest", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guestExchange, new object[] { new OnlineGroundPassReplica("transport") });
        bool hostRunning = true, guestRunning = true, started = false, previewed = false, confirmed = false, interrupted = false, restored = false, rolled = false;
        void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        var deadline = Time.realtimeSinceStartupAsDouble + 30;
        while ((hostRunning || guestRunning) && Time.realtimeSinceStartupAsDouble < deadline)
        {
            now += .1;
            if (hostRunning) hostRunning = hostFlow.MoveNext();
            if (guestRunning) guestRunning = guestFlow.MoveNext();
            if (!started && guest.controls.startButton.interactable) { Click(guest.controls.startButton); started = true; }
            else if (!previewed && guest.controls.targetButton.interactable)
            { guest.controls.targetX.text = "0"; guest.controls.targetZ.text = "6"; Click(guest.controls.targetButton); previewed = true; }
            else if (!confirmed && previewed && guest.controls.targetButton.interactable)
            { Click(guest.controls.targetButton); confirmed = true; }
            if (confirmed && board.match.groundBallManager.isWaitingForDiceRoll && !interrupted)
            {
                dropped = true; interrupted = true; now += 6;
                type.GetMethod("MarkTransportUnavailable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guestExchange, null);
                type.GetMethod("MarkTransportUnavailable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hostExchange, null);
                Assert.That(guest.controls.pauseButton.interactable, Is.False, "reported local transport loss stops submission immediately");
                hostInbox.Clear(); guestInbox.Clear();
            }
            else if (interrupted && !restored && pause.ConnectionHeld)
            {
                Assert.That(board.match.TimingRollSequence, Is.Zero);
                Assert.That(guest.controls.targetButton.interactable, Is.False);
                dropped = false; restored = true; now += 6;
            }
            if (restored && generation >= 2 && board.controls.rollButton.interactable && !rolled)
            {
                Assert.That(stale, Is.Not.Null); hostInbox.Enqueue(stale);
                var desired = intercept ? 6 : 1; var seed = 0;
                for (; seed < 1000; seed++) { Random.InitState(seed); if (board.match.helperFunctions.DiceRoll()._roll == desired) break; }
                Assert.That(seed, Is.LessThan(1000)); Random.InitState(seed);
                Click(board.controls.rollButton); rolled = true;
            }
            yield return null;
        }
        Assert.That(hostRunning || guestRunning, Is.False, "both real exchange iterators must finish");
        Assert.That(interrupted && restored && rolled, Is.True);
        Assert.That(board.match.TimingRollSequence, Is.EqualTo(1));
        Assert.That(board.match.gameData.stats.playerStats.Sum(entry => entry.stats.passesAttempted), Is.EqualTo(1));
        Assert.That(board.match.gameData.stats.playerStats.Sum(entry => entry.stats.interceptionsMade), Is.EqualTo(intercept ? 1 : 0));
        Assert.That(File.Exists(output + "/host/gameplay-evidence.json") && File.Exists(output + "/guest/gameplay-evidence.json"), Is.True);
        Assert.That(guest.authorityRoot.activeSelf, Is.False);
    }
    public IEnumerator RunPending()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineGroundPassSceneAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        board = Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        var controls = board.controls;
        controls.Configure(OnlineTeam.Home);
        var publication = new OnlineGroundPassState { matchId = "pending-test", decisionId = "preview", owner = "Home",
            stage = "target", legalActions = new[] { OnlineGroundPassAuthority.TargetAction(0,6) }, revision = 4, homeAcceptedSequence = 7 };
        controls.Display(publication);
        Assert.That(controls.legalTargets, Is.Not.Null);
        Assert.That(controls.legalTargets.options.Select(option => option.text), Does.Contain("Target (0,6)"));
        publication.legalActions[0] = "wrong";
        int sent = 0; OnlineGroundPassMessage accepted = null;
        controls.Requested += message => { sent++; accepted = message; };
        controls.targetX.text = "0"; controls.targetZ.text = "6";
        controls.Target(); controls.Target(); controls.Escape();
        Assert.That(sent, Is.EqualTo(1), "pending confirmation cannot submit twice or race pause");
        Assert.That(accepted.actionId, Is.EqualTo(OnlineGroundPassAuthority.TargetAction(0,6)));
        Assert.That(accepted.revision, Is.EqualTo(4), "displayed decision was detached from its publisher");
        Assert.That(accepted.sequence, Is.EqualTo(8), "a fresh view resumes after authoritative accepted receipts");
        controls.SetConnected(false); controls.Target(); Assert.That(sent, Is.EqualTo(1));
        controls.Reply(new OnlineGroundPassMessage { status = "Disconnected" });
        Assert.That(controls.commandStatus.text, Is.EqualTo("Disconnected"));
        Assert.That(controls.targetButton.interactable, Is.False);
    }
    public IEnumerator Run(int side)
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineGroundPassSceneAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        board = Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        Assert.That(board.controls, Is.Not.Null);
        if (side == -1) for (var i = 0; i < board.initialCoordinates.Length; i++) board.initialCoordinates[i].x *= -1;
        board.InitializeAuthority();
        if (side == -1)
        { board.match.homeTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft; board.match.awayTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight; }
        var controls = board.controls;
        var version = new OnlineCompatibility("manual", "rules", "fixture");
        var protocol = new OnlineMatchProtocol("manual-test", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "guest", version); protocol.BindAuthenticatedAccount("away", "host", version);
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        using var pause = new OnlineSharedPause(protocol, board.match);
        var local = "guest"; int requests = 0;
        controls.Requested += command =>
        {
            requests++;
            var reply = command.kind == "control" ? pause.Submit(local, command.ToCommand()) : authority.Submit(local, command.ToCommand());
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            controls.Reply(new OnlineGroundPassMessage { status = reply.Status.ToString() });
            controls.Display(pause.Capture(authority));
        };
        controls.Configure(OnlineTeam.Home); controls.Display(pause.Capture(authority));
        void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        Click(controls.startButton);
        Assert.That(board.match.groundBallManager.isAwaitingTargetSelection, Is.True);
        controls.targetX.text = "0"; controls.targetZ.text = "6";
        Click(controls.targetButton);
        Assert.That(board.match.groundBallManager.currentTargetHex, Is.Not.Null);
        Click(controls.pauseButton);
        Assert.That(pause.Requester, Is.EqualTo(OnlineTeam.Home));
        controls.Configure(OnlineTeam.Away); local = "host";
        Click(controls.resumeButton);
        Assert.That(pause.IsPaused, Is.True);
        controls.Configure(OnlineTeam.Home); local = "guest";
        Click(controls.resumeButton); Assert.That(pause.IsPaused, Is.False);
        controls.targetX.text = "999";
        var beforeInvalid = requests; Click(controls.targetButton); Assert.That(requests, Is.EqualTo(beforeInvalid));
        controls.targetX.text = "0"; Click(controls.targetButton);
        var deadline = Time.realtimeSinceStartupAsDouble + 20;
        while (!board.match.groundBallManager.isWaitingForDiceRoll && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(board.match.groundBallManager.isWaitingForDiceRoll, Is.True);
        controls.Display(pause.Capture(authority));
        var beforeWrong = requests; Click(controls.rollButton); Assert.That(requests, Is.EqualTo(beforeWrong));
        controls.Configure(OnlineTeam.Away); local = "host";
        Random.InitState(1234); Click(controls.rollButton); Click(controls.rollButton);
        while (board.match.groundBallManager.isActivated && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        var final = pause.Capture(authority);
        Assert.That(final.passActive, Is.False); Assert.That(final.rollSequence, Is.EqualTo(1));
        Assert.That(final.tokens.Single(t => t.id == "home:9").passesAttempted, Is.EqualTo(1));
        Assert.That(final.tokens.Single(t => t.id == "away:8").interceptionsAttempted, Is.EqualTo(1));
        controls.SetConnected(false); var disconnected = requests; controls.Pause(); Assert.That(requests, Is.EqualTo(disconnected));
    }
    public IEnumerator Cleanup()
    {
        (hostFlow as IDisposable)?.Dispose(); (guestFlow as IDisposable)?.Dispose();
        if (guest != null) Object.DestroyImmediate(guest.gameObject);
        if (board != null)
        {
            foreach (var behaviour in board.authorityRoot.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.StopAllCoroutines();
            Object.DestroyImmediate(board.gameObject);
        }
        MatchManager.Instance = null; PlayerToken.SetBallReference(null); Random.state = random; Time.timeScale = scale;
        yield break;
    }
}
