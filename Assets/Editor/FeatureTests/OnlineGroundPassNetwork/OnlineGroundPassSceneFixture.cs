using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public sealed class OnlineGroundPassSceneFixture
{
    private readonly UnityEngine.Random.State previousRandom = UnityEngine.Random.state;
    private readonly float previousTimeScale = Time.timeScale;
    private OnlineGroundPassBoard board;

    public IEnumerator Run(bool resolvePass)
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineGroundPassSceneAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        board = UnityEngine.Object.FindFirstObjectByType<OnlineGroundPassBoard>();
        Assert.That(board, Is.Not.Null);
        Assert.That(board.authorityRoot.activeSelf, Is.False);
        Assert.That(board.authorityRoot.GetComponentsInChildren<MonoBehaviour>(true).All(c => !c.enabled), Is.True,
            "guest rules, input, clocks and AI have no enabled callbacks");
        Assert.That(board.tokens.Length, Is.EqualTo(board.displayedTokens.Length));
        if (!resolvePass) yield break;

        board.InitializeAuthority();
        Assert.That(board.match.gameObject.activeSelf, Is.False, "live save bootstrap stays inactive on the host too");
        var version = new OnlineCompatibility("v1", "rules", "fixture");
        var protocol = new OnlineMatchProtocol("loopback", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "guest", version); protocol.BindAuthenticatedAccount("away", "host", version);
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        void Submit(string connection, long sequence, string action)
        {
            var state = authority.Capture();
            var original = OnlineGroundPassMessage.Command(state, sequence, action);
            var reader = new OnlineGroundPassWire(); OnlineGroundPassMessage decoded = null;
            foreach (var packet in OnlineGroundPassWire.Encode(original)) decoded = reader.Receive(packet, packet.Length);
            Assert.That(authority.Submit(connection, decoded.ToCommand()).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        Submit("guest", 1, OnlineGroundPassAuthority.StartAction);
        Submit("guest", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        Submit("guest", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        var deadline = Time.realtimeSinceStartupAsDouble + 15;
        while (!board.match.groundBallManager.isWaitingForDiceRoll && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(board.match.groundBallManager.isWaitingForDiceRoll, Is.True);
        Submit("host", 1, OnlineGroundPassAuthority.RollAction);
        while (board.match.groundBallManager.isActivated && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(board.match.groundBallManager.isActivated, Is.False);
        var final = authority.Capture();
        Assert.That(final.rollSequence, Is.EqualTo(1));
        Assert.That(final.tokens.Single(t => t.id == "home:9").passesAttempted, Is.EqualTo(1));

        // Normal scene replacement creates a fresh guest. No additive loading or
        // guest-side rule execution is used to reproduce the authority outcome.
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineGroundPassSceneAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        board = UnityEngine.Object.FindFirstObjectByType<OnlineGroundPassBoard>();
        var replica = new OnlineGroundPassReplica("loopback"); var wire = new OnlineGroundPassWire();
        OnlineGroundPassMessage received = null;
        foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage { kind = "state", matchId = "loopback", state = final }))
            received = wire.Receive(packet, packet.Length);
        Assert.That(replica.Apply(received.state), Is.True);
        board.Display(replica.State, "Test guest");
        Assert.That(board.authorityRoot.activeSelf, Is.False);
        Assert.That(board.match.TimingRollSequence, Is.Zero, "guest did not reroll");
        var expected = board.grid.GetHexCenterForCoordinates(new Vector3Int(final.ballX, 0, final.ballZ)) + Vector3.up * 1.2f;
        Assert.That(Vector3.Distance(board.displayedBall.position, expected), Is.LessThan(.001f));
        Assert.That(board.statusText.text, Does.Contain("Rolls 1"));
    }

    public IEnumerator Cleanup()
    {
        if (board != null)
        {
            foreach (var behaviour in board.authorityRoot.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.StopAllCoroutines();
            UnityEngine.Object.DestroyImmediate(board.gameObject);
        }
        MatchManager.Instance = null; PlayerToken.SetBallReference(null);
        UnityEngine.Random.state = previousRandom; Time.timeScale = previousTimeScale;
        yield break;
    }
}
