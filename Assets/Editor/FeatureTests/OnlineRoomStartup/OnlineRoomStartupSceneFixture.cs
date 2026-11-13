using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OnlineRoomStartupSceneFixture
{
    private OnlineRoomSession session;
    private readonly Random.State random = Random.state;
    private readonly float scale = Time.timeScale;
    public IEnumerator Run()
    {
        session = new OnlineRoomSession("guest-room-test", true);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Room.unity", new LoadSceneParameters(LoadSceneMode.Single));
        for (var frame = 0; frame < 5; frame++) yield return null;
        var scene = SceneManager.GetActiveScene();
        var gate = Object.FindAnyObjectByType<OnlineRoomStartup>();
        Assert.That(gate != null && gate.GuestConfigured, Is.True);
        Assert.That(gate.authorityRoots.Length, Is.EqualTo(28));
        Assert.That(gate.authorityRoots.All(root => !root.activeSelf), Is.True);
        var match = gate.authorityRoots.Single(root => root.name == "MatchManager").GetComponent<MatchManager>();
        Assert.That(MatchManager.Instance, Is.Null);
        Assert.That(match.TimingRollSequence, Is.Zero);
        var state = match.currentState;
        match.LoadGameSettingsFromJson(); match.TriggerStandardPass();
        match.AdvanceOnlineAuthorityTime(3f);
        Assert.That(match.CaptureOnlineDecisionTiming(), Is.Null, "guest cannot instantiate or advance an authority clock");
        Assert.That(match.currentState, Is.EqualTo(state));
        var input = gate.authorityRoots.Single(root => root.name == "GameInputManager").GetComponent<GameInputManager>();
        Assert.That(input.TryExecuteSyntheticKeyPress("P", "owned guest test", out _), Is.False);
        var camera = Object.FindAnyObjectByType<CameraController>();
        Assert.That(camera != null && camera.enabled && camera.IsReady, Is.True, "personal camera stays usable");
        Assert.That(scene.GetRootGameObjects().Single(root => root.name == "MainCanvas").activeSelf, Is.True);
        Assert.That(Object.FindAnyObjectByType<RoomInspectionCard>(), Is.Not.Null, "authored inspection remains present");
        Assert.That(ApplicationManager.Instance, Is.Null, "guest startup must not select or create local saves");
    }
    public IEnumerator Cleanup()
    {
        // Keep the guest role until all scene destruction callbacks have run.
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/OnlineGroundPassProof.unity", new LoadSceneParameters(LoadSceneMode.Single));
        session?.Dispose(); session = null;
        MatchManager.Instance = null; PlayerToken.SetBallReference(null);
        Random.state = random; Time.timeScale = scale;
    }
}
