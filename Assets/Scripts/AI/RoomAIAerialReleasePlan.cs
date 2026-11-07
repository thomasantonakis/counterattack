using System;
using System.Linq;

/// <summary>A high pass, headed release and runner continuation are one sequence.</summary>
[Serializable]
public sealed class RoomAIAerialReleasePlan
{
    public string headerTokenKey;
    public string runnerTokenKey;
    public string distributorTokenKey;
    public RoomHexCoordinates contestHex;
    public RoomHexCoordinates runnerHex;
    public int headerHeading;
    public int runnerPace;
    public int headerMovesRequired;
    public int runnerMovesRequired;
    public float highPassAccuracy;
    public float headerWinProbability;
    public float sequenceRetention;
    public float suitability;
    public bool requiresOffsideReassessment = true;
    public string nextAction;

    public static RoomAIAerialReleasePlan Build(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot?.ball == null || snapshot.expectedTeam != snapshot.teamInAttack) return null;
        var own = RoomAIRosterAttackPlan.EligiblePlayers(snapshot)
            .OrderBy(token => token.tokenKey, StringComparer.Ordinal).ToList();
        // The actual carrier must distribute: a high passer elsewhere is not a
        // free substitute for getting the ball to them first.
        var carrier = snapshot.tokens.FirstOrDefault(token => token != null && token.isPlaying
            && !token.isSentOff && token.teamSide == snapshot.expectedTeam
            && RoomAITacticalSafety.Distance(token.currentHex, snapshot.ball) == 0);
        if (own.Count < 2 || carrier == null || carrier.highPass < 4) return null;
        bool right = (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight";
        int sign = right ? 1 : -1;
        var defense = snapshot.tokens.Where(token => token != null && token.isPlaying && !token.isSentOff
                && token.currentHex != null && token.teamSide != snapshot.expectedTeam)
            .OrderByDescending(token => sign * token.currentHex.x).ToList();
        if (defense.Count < 2) return null;
        int line = sign * Math.Max(0, Math.Max(sign * defense[1].currentHex.x, sign * snapshot.ball.x));
        float accuracy = HighPassManager.AccuracyProbabilityForDecisionPreview(carrier.highPass);
        RoomAIAerialReleasePlan best = null;
        foreach (var header in own.Where(token => token != carrier && token.heading >= 4))
        foreach (var runner in own.Where(token => token != carrier && token != header && token.pace >= 4 && token.shooting > 0))
        {
            var contest = new RoomHexCoordinates { x = line, z = Math.Max(-8, Math.Min(8, runner.currentHex.z)) };
            var release = new RoomHexCoordinates { x = line + sign * 2, z = contest.z };
            if (Math.Abs(release.x) > 17 || RoomAITacticalSafety.Distance(snapshot.ball, contest) > 15) continue;
            if (snapshot.tokens.Any(token => token != null && token.isPlaying && token.currentHex != null
                && (token != header && RoomAITacticalSafety.Distance(token.currentHex, contest) == 0
                    || token != runner && RoomAITacticalSafety.Distance(token.currentHex, release) == 0))) continue;
            float headerWin = 1f;
            foreach (var defender in defense.Where(token => !token.isGoalKeeper
                && RoomAITacticalSafety.Distance(token.currentHex, contest) <= 3))
                headerWin *= ContestWin(header.heading, defender.heading);
            int headerDistance = RoomAITacticalSafety.Distance(header.currentHex, contest);
            int runnerDistance = RoomAITacticalSafety.Distance(runner.currentHex, release);
            int headerMoves = (headerDistance + Math.Max(1, header.pace) - 1) / Math.Max(1, header.pace);
            int runnerMoves = (runnerDistance + Math.Max(1, runner.pace) - 1) / Math.Max(1, runner.pace);
            float value = header.heading / 6f * .25f + runner.pace / 6f * .2f + runner.shooting / 6f * .15f
                + accuracy * .2f + headerWin * .2f - (headerMoves + runnerMoves) * .04f;
            if (best != null && value <= best.suitability) continue;
            best = new RoomAIAerialReleasePlan
            {
                distributorTokenKey = carrier.tokenKey, headerTokenKey = header.tokenKey, runnerTokenKey = runner.tokenKey,
                contestHex = contest, runnerHex = release, headerHeading = header.heading, runnerPace = runner.pace,
                headerMovesRequired = headerMoves, runnerMovesRequired = runnerMoves,
                highPassAccuracy = accuracy, headerWinProbability = headerWin,
                sequenceRetention = accuracy * headerWin, suitability = value,
                nextAction = runnerDistance > 0 ? "Prepare a running target, then a reachable aerial contest"
                    : headerDistance > 0 ? "Position the selected header at the contest" : "High pass, reassess offside, headed release into the running path"
            };
        }
        return best;
    }

    private static float ContestWin(int heading, int defense)
    {
        // Jackpot outcomes match the game's dice; strict wins give a conservative
        // continuation estimate because a tied contest remains unresolved.
        int[] rolls = { 1, 2, 3, 4, 5, 6, 50 };
        float probability = 0f;
        foreach (int a in rolls)
            foreach (int d in rolls)
                if ((GameplayScore.Total(heading, a)) > (GameplayScore.Total(defense, d)))
                    probability += (a >= 6 ? 1f / 12f : 1f / 6f) * (d >= 6 ? 1f / 12f : 1f / 6f);
        return probability;
    }
}
