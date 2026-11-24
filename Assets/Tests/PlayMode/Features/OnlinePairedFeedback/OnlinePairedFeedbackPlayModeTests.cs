using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlinePairedFeedback")]
public sealed class OnlinePairedFeedbackPlayModeTests
{
    private object fixture;

    private IEnumerator Run(string type, string method, params object[] arguments)
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType(type, true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod(method).Invoke(fixture, arguments);
    }

    [UnityTest]
    public IEnumerator DraftDisplaysBothSquadAndStartingAverages()
        => Run("OnlineDraftSceneFixture", "Run", false, false, "team-average");

    [UnityTest]
    public IEnumerator KickoffHoverRespectsAssistance([Values(1, 3)] int assistance)
        => Run("OnlineFullRoomSceneFixture", "RunOnlineKickoffAssistance", assistance);

    [UnityTest]
    public IEnumerator GuestRendersRaisedHeaderTargetsAndAllTokenStatuses()
        => Run("OnlineFullRoomSceneFixture", "RunGuestGoalTargetsAndStatus");

    [UnityTest]
    public IEnumerator PenaltyTossCanResumeAndRollAfterTheFinalWhistle()
        => Run("OnlineFullRoomSceneFixture", "RunPenaltyTossPauseFromFinalWhistle");

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
