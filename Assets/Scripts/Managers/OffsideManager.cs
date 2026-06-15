using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OffsideManager : MonoBehaviour
{
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private Ball ball;
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private PlayerTokenManager playerTokenManager;

    private readonly List<PlayerToken> offsideTokens = new();
    private bool hasStoredAssessment;
    private bool assessedTeamIsHome;
    private int offsideLineX;
    private string assessedContext = string.Empty;
    private bool isResolvingOffside;

    public IReadOnlyList<PlayerToken> OffsideTokens => offsideTokens;
    public bool HasStoredAssessment => hasStoredAssessment;
    public int OffsideLineX => offsideLineX;
    public string AssessedContext => assessedContext;

    public void Configure(MatchManager manager)
    {
        matchManager = manager;
        ball = manager != null ? manager.ball : ball;
        hexGrid = manager != null ? manager.hexGrid : hexGrid;
        playerTokenManager = manager != null ? manager.playerTokenManager : playerTokenManager;
    }

    public IReadOnlyList<PlayerToken> EvaluateAndStore(string context, bool forceReassessment = false)
    {
        ResolveDependencies();
        if (!forceReassessment && ShouldPreserveStoredAssessment())
        {
            Debug.Log($"Offside assessment preserved ({assessedContext}) while ball remains uncollected. Requested context: {context}.");
            return offsideTokens;
        }

        offsideTokens.Clear();
        assessedContext = context ?? string.Empty;
        hasStoredAssessment = false;
        offsideLineX = 0;

        if (matchManager == null || ball == null)
        {
            return offsideTokens;
        }

        HexCell ballHex = ball.GetCurrentHex();
        if (ballHex == null)
        {
            return offsideTokens;
        }

        assessedTeamIsHome = matchManager.teamInAttack == MatchManager.TeamInAttack.Home;
        MatchManager.TeamAttackingDirection attackingDirection = assessedTeamIsHome
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;

        List<PlayerToken> attackers = GetPlayingTokens(assessedTeamIsHome);
        List<PlayerToken> defenders = GetPlayingTokens(!assessedTeamIsHome);
        PlayerToken secondLastDefender = GetSecondLastDefender(defenders);
        if (secondLastDefender == null || secondLastDefender.GetCurrentHex() == null)
        {
            hasStoredAssessment = true;
            Debug.Log($"Offside assessed ({assessedContext}): no second-last defender available.");
            return offsideTokens;
        }

        int ballX = ballHex.coordinates.x;
        int secondDefenderX = secondLastDefender.GetCurrentHex().coordinates.x;
        offsideLineX = attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight
            ? Mathf.Max(0, Mathf.Max(ballX, secondDefenderX))
            : Mathf.Min(0, Mathf.Min(ballX, secondDefenderX));

        foreach (PlayerToken token in attackers)
        {
            HexCell tokenHex = token != null ? token.GetCurrentHex() : null;
            if (tokenHex == null)
            {
                continue;
            }

            int tokenX = tokenHex.coordinates.x;
            bool isOffside = attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight
                ? tokenX > offsideLineX
                : tokenX < offsideLineX;
            if (isOffside)
            {
                offsideTokens.Add(token);
            }
        }

        hasStoredAssessment = true;
        Debug.Log($"Offside assessed ({assessedContext}): line x={offsideLineX}, tokens={FormatTokenNames(offsideTokens)}.");
        return offsideTokens;
    }

    private bool ShouldPreserveStoredAssessment()
    {
        return hasStoredAssessment
            && matchManager != null
            && (!matchManager.attackHasPossession || !string.IsNullOrEmpty(matchManager.hangingPassType));
    }

    public bool IsTokenOffside(PlayerToken token)
    {
        return hasStoredAssessment
            && token != null
            && token.isHomeTeam == assessedTeamIsHome
            && offsideTokens.Contains(token);
    }

    public bool ShouldBlockCollection(PlayerToken token)
    {
        return matchManager != null
            && matchManager.difficulty_level == 1
            && IsTokenOffside(token);
    }

    public bool ShouldWarnForToken(PlayerToken token)
    {
        return matchManager != null
            && matchManager.difficulty_level == 2
            && IsTokenOffside(token);
    }

    public string GetOffsideWarningForTarget(HexCell targetHex)
    {
        PlayerToken token = targetHex != null ? targetHex.GetOccupyingToken() : null;
        if (!ShouldWarnForToken(token))
        {
            return string.Empty;
        }

        return $"{token.name} is in an offside position. If they collect or challenge for this ball, offside will be called.";
    }

    public string GetFtpOffsideWarning(HexCell targetHex)
    {
        ResolveDependencies();
        if (matchManager == null || matchManager.difficulty_level > 2)
        {
            return string.Empty;
        }

        PlayerToken targetToken = targetHex != null ? targetHex.GetOccupyingToken() : null;
        if (IsTokenOffside(targetToken))
        {
            return $"{targetToken.name} is in an offside position and must move away from the First-Time Pass target.";
        }

        PlayerToken nearbyOffsideToken = offsideTokens
            .FirstOrDefault(token => token != null
                && targetHex != null
                && hexGrid != null
                && token.GetCurrentHex() != null
                && token.GetCurrentHex().GetNeighbors(hexGrid).Contains(targetHex));
        if (IsTokenOffside(nearbyOffsideToken))
        {
            return $"{nearbyOffsideToken.name} is in an offside position and must not move onto this First-Time Pass target.";
        }

        return string.Empty;
    }

    public bool WouldFtpDifficultyOneCreateOffside(HexCell targetHex)
    {
        ResolveDependencies();
        if (matchManager == null || matchManager.difficulty_level != 1)
        {
            return false;
        }

        PlayerToken targetToken = targetHex != null ? targetHex.GetOccupyingToken() : null;
        if (IsTokenOffside(targetToken))
        {
            return true;
        }

        return offsideTokens.Any(token => token != null
            && targetHex != null
            && hexGrid != null
            && token.GetCurrentHex() != null
            && token.GetCurrentHex().GetNeighbors(hexGrid).Contains(targetHex));
    }

    public bool TryHandleOffsideCollection(PlayerToken token, string source, HexCell offenceHex = null)
    {
        if (!IsTokenOffside(token) || isResolvingOffside)
        {
            return false;
        }

        isResolvingOffside = true;
        try
        {
            HexCell resolvedHex = offenceHex ?? token.GetCurrentHex() ?? ball?.GetCurrentHex();
            if (resolvedHex != null && ball != null)
            {
                ball.PlaceAtCell(resolvedHex);
            }

            Debug.Log($"Offside offence called on {token.name} from {source} at {FormatHex(resolvedHex)}.");
            matchManager?.RecordGameplayOutcome(
                "offside.offence",
                "offside",
                "called",
                actor: token,
                sourceHex: resolvedHex,
                targetHex: resolvedHex,
                details: MatchManager.CreateDetails(
                    ("source", source),
                    ("assessmentContext", assessedContext),
                    ("offsideLineX", offsideLineX.ToString())));

            ClearStoredOffside("offence_called");
            if (matchManager != null)
            {
                matchManager.ClearHangingPass();
                matchManager.ClearPendingLooseBallCollectionReset();
                matchManager.CleanupLiveActionForOffside();
                matchManager.ChangePossession();
                matchManager.freeKickManager?.StartOffsideIndirectFreeKick(token);
            }
        }
        finally
        {
            isResolvingOffside = false;
        }

        return true;
    }

    public void ClearStoredOffside(string reason)
    {
        if (!hasStoredAssessment && offsideTokens.Count == 0)
        {
            return;
        }

        Debug.Log($"Offside assessment cleared: {reason}.");
        hasStoredAssessment = false;
        assessedContext = string.Empty;
        offsideLineX = 0;
        offsideTokens.Clear();
    }

    public void ClearIfLegalCollector(PlayerToken token, string reason)
    {
        if (token == null || IsTokenOffside(token))
        {
            return;
        }

        ClearStoredOffside(reason);
    }

    public RoomOffsideSnapshot CreateSnapshot()
    {
        return new RoomOffsideSnapshot
        {
            hasStoredAssessment = hasStoredAssessment,
            assessedTeamIsHome = assessedTeamIsHome,
            offsideLineX = offsideLineX,
            assessedContext = assessedContext,
            offsideTokens = offsideTokens
                .Where(token => token != null)
                .Select(token => new RoomTokenReference
                {
                    tokenKey = MatchManager.GetStableTokenKey(token),
                    teamSide = token.isHomeTeam ? "Home" : "Away",
                    jerseyNumber = token.jerseyNumber
                })
                .ToList()
        };
    }

    public void RestoreSnapshot(RoomOffsideSnapshot snapshot, Func<RoomTokenReference, PlayerToken> resolveToken)
    {
        offsideTokens.Clear();
        if (snapshot == null || !snapshot.hasStoredAssessment)
        {
            hasStoredAssessment = false;
            return;
        }

        hasStoredAssessment = true;
        assessedTeamIsHome = snapshot.assessedTeamIsHome;
        offsideLineX = snapshot.offsideLineX;
        assessedContext = snapshot.assessedContext ?? string.Empty;

        if (snapshot.offsideTokens == null || resolveToken == null)
        {
            return;
        }

        foreach (RoomTokenReference reference in snapshot.offsideTokens)
        {
            PlayerToken token = resolveToken(reference);
            if (token != null && !offsideTokens.Contains(token))
            {
                offsideTokens.Add(token);
            }
        }
    }

    private void ResolveDependencies()
    {
        if (matchManager == null)
        {
            matchManager = MatchManager.Instance;
        }

        if (matchManager == null)
        {
            return;
        }

        ball ??= matchManager.ball;
        hexGrid ??= matchManager.hexGrid;
        playerTokenManager ??= matchManager.playerTokenManager;
    }

    private List<PlayerToken> GetPlayingTokens(bool isHomeTeam)
    {
        if (playerTokenManager != null)
        {
            return playerTokenManager.GetPlayingTokens(isHomeTeam)
                .Where(IsTokenOnPitch)
                .ToList();
        }

        return FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .Where(token => token != null
                && token.isHomeTeam == isHomeTeam
                && token.isPlaying
                && !token.isSentOff
                && IsTokenOnPitch(token))
            .ToList();
    }

    private static bool IsTokenOnPitch(PlayerToken token)
    {
        return token != null
            && token.isPlaying
            && !token.isSentOff
            && token.GetCurrentHex() != null;
    }

    private PlayerToken GetSecondLastDefender(List<PlayerToken> defenders)
    {
        if (matchManager == null || defenders == null || defenders.Count < 2)
        {
            return null;
        }

        bool defendersOwnGoalIsLeft = (!assessedTeamIsHome ? matchManager.homeTeamDirection : matchManager.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight;

        IOrderedEnumerable<PlayerToken> ordered = defendersOwnGoalIsLeft
            ? defenders.OrderBy(token => token.GetCurrentHex().coordinates.x)
                .ThenBy(token => token.GetCurrentHex().coordinates.z)
                .ThenBy(token => token.jerseyNumber)
            : defenders.OrderByDescending(token => token.GetCurrentHex().coordinates.x)
                .ThenBy(token => token.GetCurrentHex().coordinates.z)
                .ThenBy(token => token.jerseyNumber);

        return ordered.Skip(1).FirstOrDefault();
    }

    private static string FormatTokenNames(IEnumerable<PlayerToken> tokens)
    {
        List<string> names = tokens?
            .Where(token => token != null)
            .Select(token => token.name)
            .ToList() ?? new List<string>();
        return names.Count == 0 ? "none" : string.Join(", ", names);
    }

    private static string FormatHex(HexCell hex)
    {
        return hex == null ? "unknown" : hex.coordinates.ToString();
    }
}
