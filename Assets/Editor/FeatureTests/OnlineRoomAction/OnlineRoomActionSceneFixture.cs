using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineRoomActionSceneFixture
{
    private OnlineRoomSession host;
    private OnlineGroundPassBoard board;
    private readonly Random.State random = Random.state;
    private readonly float scale = Time.timeScale;
    public IEnumerator Run(int side, string branch)
    {
        host = new OnlineRoomSession("room-action", false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        board = Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        Assert.That(board.controls != null && board.controls.legalTargets != null, Is.True, "Room inherits the authored legal-target controls prefab");
        Assert.That(board.authorityRoot.activeSelf, Is.False);
        if (side == -1) for (var i = 0; i < board.initialCoordinates.Length; i++) board.initialCoordinates[i].x *= -1;
        if (branch == "safe") board.initialCoordinates[1] = new Vector3Int(8 * side,0,10);
        board.InitializeAuthority();
        Assert.That(Object.FindAnyObjectByType<PauseMenuManager>().enabled, Is.False, "host Escape cannot bypass shared authority");
        Assert.That(Object.FindAnyObjectByType<RoomMatchIntro>().enabled, Is.False, "host personal intro cannot hold guest commands");
        board.match.homeTeamDirection = side == 1 ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
        board.match.awayTeamDirection = side == 1 ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
        var version = new OnlineCompatibility("room-proof", "ground-rules-v1", "prepared-board-v1");
        var protocol = new OnlineMatchProtocol("room-action", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "guest", version); protocol.BindAuthenticatedAccount("away", "host", version);
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        using var pause = new OnlineSharedPause(protocol, board.match);
        void Submit(string connection, long sequence, string action)
        {
            var state = pause.Capture(authority);
            var reply = authority.Submit(connection, new OnlineMatchCommand(protocol.MatchId, sequence, state.revision, state.decisionId, action));
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        Submit("guest",1,OnlineGroundPassAuthority.StartAction);
        var beforeClock = pause.Capture(authority);
        board.match.AdvanceOnlineAuthorityTime(.1f);
        var afterClock = pause.Capture(authority);
        Assert.That(afterClock.publicationSequence, Is.GreaterThan(beforeClock.publicationSequence));
        Assert.That(afterClock.revision, Is.EqualTo(beforeClock.revision), "remaining time cannot invalidate an otherwise legal in-transit command");
        Assert.That(authority.Submit("guest", new OnlineMatchCommand(protocol.MatchId,2,beforeClock.revision,beforeClock.decisionId,
            OnlineGroundPassAuthority.TargetAction(0,6))).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Submit("guest",new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        double now = 0;
        var recovery = new OnlinePeerRecovery(protocol,version,pause,"home",()=>now);
        Assert.That(recovery.BindAuthenticated("home","guest-return",version), Is.True);
        var heldSeconds = board.match.OnlineRegulationSeconds;
        board.match.AdvanceOnlineAuthorityTime(.5f);
        Assert.That(board.match.OnlineRegulationSeconds, Is.EqualTo(heldSeconds), "connection hold freezes the real authority clock");
        Assert.That(recovery.AcknowledgeSnapshot("guest-return",1), Is.True);
        Assert.That(pause.IsPaused && Time.timeScale == 0, Is.True);
        Assert.That(pause.Submit("host",new OnlineMatchCommand(protocol.MatchId,1,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(pause.Submit("guest-return",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Submit("guest-return",3,OnlineGroundPassAuthority.TargetAction(0,6));
        var deadline = Time.realtimeSinceStartupAsDouble + 25;
        if (branch != "safe")
        {
            while (!board.match.groundBallManager.isWaitingForDiceRoll && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(board.match.groundBallManager.isWaitingForDiceRoll, Is.True);
            var desired = branch == "intercept" ? 6 : 1;
            var seed = 0;
            for (; seed < 1000; seed++) { Random.InitState(seed); if (board.match.helperFunctions.DiceRoll()._roll == desired) break; }
            Assert.That(seed, Is.LessThan(1000)); Random.InitState(seed);
            Submit("host",1,OnlineGroundPassAuthority.RollAction);
        }
        while (board.match.groundBallManager.isActivated && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        var final = pause.Capture(authority);
        Assert.That(final.passActive || final.ballMoving, Is.False);
        Assert.That(final.rollSequence, Is.EqualTo(branch == "safe" ? 0 : 1));
        Assert.That(final.tokens.Single(token=>token.id=="home:9").passesAttempted, Is.EqualTo(1));
        Assert.That(final.tokens.Single(token=>token.id=="home:9").passesCompleted, Is.EqualTo(branch == "intercept" ? 0 : 1));
        Assert.That(final.tokens.Single(token=>token.id=="away:8").interceptionsMade, Is.EqualTo(branch == "intercept" ? 1 : 0));
        Assert.That(final.possessionTeam, Is.EqualTo(branch == "intercept" ? "Away" : "Home"));
        Assert.That(final.movementAvailable, Is.True);
        host.Dispose();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board = Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        Assert.That(OnlineRoomSession.IsGuest && !board.authorityRoot.activeSelf, Is.True);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(token=>(token.isHomeTeam?"home:":"away:")+token.jerseyNumber),version.Catalog);
        Assert.That(replica.Apply(final), Is.True); board.Display(replica.State,"Owned Room guest outcome");
        Assert.That(board.match.TimingRollSequence, Is.Zero);
        Assert.That(replica.State.tokens.Single(token=>token.id=="home:9").passesAttempted, Is.EqualTo(1));
    }
    public IEnumerator Cleanup()
    {
        if (board != null)
        {
            foreach(var behaviour in board.authorityRoot.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.StopAllCoroutines();
            Object.DestroyImmediate(board.gameObject);
        }
        host?.Dispose(); MatchManager.Instance=null; PlayerToken.SetBallReference(null);
        Random.state=random; Time.timeScale=scale; yield break;
    }
}
