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

    [UnityTest, Category("TokenResetMovement")]
    public IEnumerator GuestAcceptsGoalResetWithOccupiedDestination([Values(1, -1)] int goalSide)
        => Run("OnlineFullRoomSceneFixture", "RunGoalResetWithOccupiedDestination", goalSide, true);

    [UnityTest, Category("TokenResetMovement")]
    public IEnumerator OfflineGoalResetHandlesOccupiedDestination()
        => Run("OnlineFullRoomSceneFixture", "RunGoalResetWithOccupiedDestination", 1, false);

    [UnityTest, Category("TokenResetMovement")]
    public IEnumerator HalfTimeResetHandlesOccupiedDestination([Values(false, true)] bool online)
        => Run("OnlineFullRoomSceneFixture", "RunHalfTimeResetWithOccupiedDestination", online);

    [UnityTest, Category("TokenResetMovement")]
    public IEnumerator ShootoutStagingHandlesSwappedTokens([Values(false, true)] bool online)
        => Run("OnlineFullRoomSceneFixture", "RunShootoutStagingWithSwappedTokens", online);

    [UnityTest]
    public IEnumerator PenaltyTossCanResumeAndRollAfterTheFinalWhistle()
        => Run("OnlineFullRoomSceneFixture", "RunPenaltyTossPauseFromFinalWhistle");

    [UnityTest]
    public IEnumerator GuestAcceptsEachPenaltyTossCountdownPublication()
        => Run("OnlineFullRoomSceneFixture", "RunPenaltyTossCountdownPublication");

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
