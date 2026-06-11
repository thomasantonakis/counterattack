using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;  // For JsonConvert
using System.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using TMPro;

public class MatchManager : MonoBehaviour
{
    public const int MaxSubstitutionsPerTeam = 3;
    public const int ExtraTimeMaxSubstitutionsPerTeam = MaxSubstitutionsPerTeam + 1;
    private const int StandardHalfDurationMinutes = 45;
    private const int StandardNumberOfHalfs = 2;
    private const int ExtraTimeHalfDurationMinutes = 15;
    private const int ExtraTimeHalfs = 2;
    private const string EditorRoomDirectPlayTestSaveFileName = "gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json";
    private const string EditorRoomDirectPlayWorkingCopyFolderName = "__RoomPlaytests";
    private const string EditorRoomDirectPlayWorkingCopyFileName = "__RoomDirectPlay__gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json";

    // Define the possible game states
    public enum GameState
    {
        KickOffSetup, // Free movements of Players in each own Half
        PostGoalKickOffSetup,
        KickOffTakerSelection,
        KickoffBlown, // Only a Standard Pass is available
        MovementPhase,
        EndOfMovementPhase,
        StandardPass,
        EndOfStandardPass,
        EndOfFirstTimePass,
        AnyOtherScenario,
        LongBall,
        EndOfLongBall,
        WaitingForThrowInTaker, // An attacker must be chosen to take the throw in
        WaitingForGoalKickFinalThirds, // Both Final Thirds Can Move
        LooseBallPickedUp, // Any type of Loose ball picked up by an outfielder
        SuccessfulTackle,
        BallControl,
        HighPass,
        HighPassCompleted,
        HeaderGeneric,
        HeaderAttackerSelection,
        HeaderChallengeResolved,
        HeaderCompleted,
        FirstTimePassAttackerMovement,
        FirstTimePassDefenderMovement,
        FreeKickKickerSelect,
        FreeKickAttGK,
        FreeKickDefGK1,
        FreeKickAtt1,
        FreeKickAtt2,
        FreeKickAtt3,
        FreeKickDef1,
        FreeKickDef2,
        FreeKickDef3,
        FreeKickDefGK2,
        FreeKickAttMovement3,
        FreeKickDefMovement3,
        FreeKickDefineKicker,
        FreeKickExecution,
        PenaltyKickerSelect,
        PenaltyDef1,
        PenaltyAtt,
        PenaltyDef2,
        PenaltyExecution,
        PenaltyShootoutOrderSelection,
        PenaltyShootoutSetup,
        PenaltyShootoutKickExecution,
        PenaltyShootoutTransition,
        PenaltyShootoutComplete,
        QuickThrow,
        ActivateFinalThirdsAfterSave,
        GoalKick,
        HalfTime,
        MatchEnded,
    }

    public enum MatchActionKind
    {
        None,
        MovementPhase,
        StandardPass,
        FirstTimePass,
        HighPass,
        LongBall,
        Header,
        BallControl,
        Shot,
        Snapshot,
    }
    
    [Serializable]
    public class GameData
    {
        public int saveSchemaVersion;
        public string createdUtc;
        public string lastSavedUtc;
        public GameSettings gameSettings;
        public Rosters rosters;
        public Stats stats;
        public GameLog gameLog;
        public RoomRuntimeSnapshot runtimeSnapshot;
        public int eventLogSchemaVersion;
        public List<GameplayEvent> events;

        public GameData()
        {
            saveSchemaVersion = RoomSaveService.SaveSchemaVersion;
            gameSettings = new GameSettings();
            rosters = new Rosters();
            stats = new Stats();
            gameLog = new GameLog(stats);
            runtimeSnapshot = null;
            eventLogSchemaVersion = GameplayEvent.CurrentSchemaVersion;
            events = new List<GameplayEvent>();
        }
    }

    [Serializable]
    public class GameSettings
    {
        public string gameMode;
        public string homeTeamName;
        public string awayTeamName;
        public string homeKit;
        public string awayKit;
        public string homeGKKit;
        public string awayGKKit;
        public int playerAssistance;
        public string matchType;
        public string ballColor;
        public string draft;
        public string gkDraft;
        public int halfDuration;
        public int numberOfHalfs;
        public string tiebreaker;
        public string referee;
        public string weatherConditions;
        public bool includeTabletopia;
        public bool includeNonTabletopia;
        public bool includeInternationals;
        public bool includeTabletopiaGK;
        public bool includeNonTabletopiaGK;
        public bool includeInternationalsGK;
        public int squadSize;
        // Add other game settings properties as needed
    }

    [Serializable]
    public class Rosters
    {
        // public List<RosterPlayer> home = new List<RosterPlayer>();
        // public List<RosterPlayer> away = new List<RosterPlayer>();
        public Dictionary<string, RosterPlayer> home = new Dictionary<string, RosterPlayer>();
        public Dictionary<string, RosterPlayer> away = new Dictionary<string, RosterPlayer>();
        // public void LoadFromDictionary()
        // {
        //     home = new List<RosterPlayer>(homeDict.Values);
        //     away = new List<RosterPlayer>(awayDict.Values);
        // }
        // public Dictionary<string, RosterPlayer> home = new Dictionary<string, RosterPlayer>();
        // public Dictionary<string, RosterPlayer> away = new Dictionary<string, RosterPlayer>();
    }

    [Serializable]
    public class RosterPlayer
    {
        public string name;
        public int pace;
        public int dribbling;
        public int heading; // For outfielders
        public int highPass;
        public int resilience;
        public int shooting; // For outfielders
        public int tackling; // For outfielders
        public int aerial; // For goalkeepers
        public int saving; // For goalkeepers
        public int handling; // For goalkeepers
    }

    [Serializable]
    public class Stats
    {
        public List<PlayerStatsEntry> playerStats = new List<PlayerStatsEntry>();
        public TeamStats homeTeamStats = new TeamStats(); 
        public TeamStats awayTeamStats = new TeamStats();

        public Stats() { }

        public PlayerStats GetPlayerStats(string playerName)
        {
            var entry = playerStats.Find(p => p.playerName == playerName);
            if (entry == null)
            {
                entry = new PlayerStatsEntry { playerName = playerName, stats = new PlayerStats() };
                playerStats.Add(entry);
            }
            return entry.stats;
        }

        public TeamStats GetTeamStats(bool isHomeTeam)
        {
            return isHomeTeam ? homeTeamStats : awayTeamStats;
        }

        public void UpdateTeamStats(bool isHomeTeam)
        {
            TeamStats teamStats = isHomeTeam ? homeTeamStats : awayTeamStats;
            teamStats.Reset();

            foreach (var player in playerStats)
            {
                bool playerIsHome = MatchManager.Instance.IsPlayerInTeam(player.playerName, true);
                if (playerIsHome == isHomeTeam) teamStats.AddPlayerStats(player.stats);
            }
        }
    }

    [Serializable]
    public class PlayerStatsEntry
    {
        public string playerName;
        public PlayerStats stats;
    }

    [Serializable]
    public class PlayerStats
    {
        public int goals;
        public int shotsAttempted;
        public int shotsOnTarget;
        public int shotsBlocked;
        public int shotsOffTarget;
        public int shotBlocksAttempted;
        public int shotBlocksMade;
        public int passesAttempted;
        public int passesCompleted;
        public int aerialPassesAttempted;
        public int aerialPassesTargeted;
        public int aerialPassesCompleted;
        public int pacesRan;
        public int assists;
        public int possessionWon;
        public int possessionLost;
        public int groundDuelsInvolved;
        public int groundDuelsWon;
        public int dribblesMade;
        public int tacklesMade;
        public int interceptionsAttempted;
        public int interceptionsMade;
        public int aerialChallengesInvolved;
        public int aerialChallengesWon;
        public int attemptsFaced;
        public int attemptsSaved;
        public int yellowCards;
        public int redCards;
        public int injuries;
        public float xRecoveries;
        public float xDribbles;
        public float xTackles;
        public float xGoals;
        public PlayerStats()
        {
            goals = 0;
            shotsAttempted = 0;
            shotsOnTarget = 0;
            shotsBlocked = 0;
            shotsOffTarget = 0;
            shotBlocksAttempted = 0;
            shotBlocksMade = 0;
            passesAttempted = 0;
            passesCompleted = 0;
            aerialPassesAttempted = 0;
            aerialPassesTargeted = 0;
            aerialPassesCompleted = 0;
            pacesRan = 0;
            assists = 0;
            possessionWon = 0;
            possessionLost = 0;
            groundDuelsInvolved = 0;
            groundDuelsWon = 0;
            dribblesMade = 0;
            tacklesMade = 0;
            interceptionsAttempted = 0;
            interceptionsMade = 0;
            aerialChallengesInvolved = 0;
            aerialChallengesWon = 0;
            attemptsFaced = 0;
            attemptsSaved = 0;
            yellowCards = 0;
            redCards = 0;
            injuries = 0;
            xRecoveries = 0f;
            xDribbles = 0f;
            xTackles = 0f;
            xGoals = 0f;
        }
    }

    [Serializable]
    public class TeamStats
    {
        public int totalGoals;
        public int totalShots;
        public int totalShotsOnTarget;
        public int totalShotsBlocked;
        public int totalShotsOffTarget;
        public int totalShotBlocksAttempted;
        public int totalShotBlocksMade;
        public int totalPassesAttempted;
        public int totalPassesCompleted;
        public int totalAerialPassesAttempted;
        public int totalAerialPassesTargeted;
        public int totalAerialPassesCompleted;
        public int totalPacesRan;
        public int totalGroundDuelsInvolved;
        public int totalGroundDuelsWon;
        public int totalDribblesMade;
        public int totalTacklesMade;
        public int totalInterceptionsAttempted;
        public int totalInterceptionsMade;
        public int totalAerialChallengesInvolved;
        public int totalAerialChallengesWon;
        public int totalYellowCards;
        public int totalRedCards;
        public int totalAssists;
        public int totalInjuries;
        public int totalAttemptsSaved;
        public int totalSubstiutions;
        public int totalPossessionWon;
        public int totalPossessionLost;
        public int totalCorners;
        public float totalXRecoveries;
        public float totalXDribbles;
        public float totalXTackles;
        public float totalXGoals;

        public TeamStats()
        {
            totalGoals = 0;
            totalShots = 0;
            totalShotsOnTarget = 0;
            totalShotsBlocked = 0;
            totalShotsOffTarget = 0;
            totalShotBlocksAttempted = 0;
            totalShotBlocksMade = 0;
            totalPassesAttempted = 0;
            totalPassesCompleted = 0;
            totalAerialPassesAttempted = 0;
            totalAerialPassesTargeted = 0;
            totalAerialPassesCompleted = 0;
            totalPacesRan = 0;
            totalGroundDuelsInvolved = 0;
            totalGroundDuelsWon = 0;
            totalDribblesMade = 0;
            totalTacklesMade = 0;
            totalInterceptionsAttempted = 0;
            totalInterceptionsMade = 0;
            totalAerialChallengesInvolved = 0;
            totalAerialChallengesWon = 0;
            totalYellowCards = 0;
            totalRedCards = 0;
            totalAssists = 0;
            totalInjuries = 0;
            totalAttemptsSaved = 0;
            totalSubstiutions = 0;
            totalPossessionWon = 0;
            totalPossessionLost = 0;
            totalCorners = 0;
            totalXRecoveries = 0f;
            totalXDribbles = 0f;
            totalXTackles = 0f;
            totalXGoals = 0f;
        }
        public void Reset()
        {
            totalGoals = 0;
            totalShots = 0;
            totalShotsOnTarget = 0;
            totalShotsBlocked = 0;
            totalShotsOffTarget = 0;
            totalShotBlocksAttempted = 0;
            totalShotBlocksMade = 0;
            totalPassesAttempted = 0;
            totalPassesCompleted = 0;
            totalAerialPassesAttempted = 0;
            totalAerialPassesTargeted = 0;
            totalAerialPassesCompleted = 0;
            totalPacesRan = 0;
            totalGroundDuelsInvolved = 0;
            totalGroundDuelsWon = 0;
            totalDribblesMade = 0;
            totalTacklesMade = 0;
            totalInterceptionsAttempted = 0;
            totalInterceptionsMade = 0;
            totalAerialChallengesInvolved = 0;
            totalAerialChallengesWon = 0;
            totalYellowCards = 0;
            totalRedCards = 0;
            totalAssists = 0;
            totalInjuries = 0;
            totalAttemptsSaved = 0;
            totalSubstiutions = 0;
            totalPossessionWon = 0;
            totalPossessionLost = 0;
            totalCorners = 0;
            totalXRecoveries = 0f;
            totalXDribbles = 0f;
            totalXTackles = 0f;
            totalXGoals = 0f;
        }
        public void AddPlayerStats(PlayerStats stats)
        {
            totalGoals += stats.goals;
            totalShots += stats.shotsAttempted;
            totalShotsOnTarget += stats.shotsOnTarget;
            totalShotsBlocked += stats.shotsBlocked;
            totalShotsOffTarget += stats.shotsOffTarget;
            totalShotBlocksAttempted += stats.shotBlocksAttempted;
            totalShotBlocksMade += stats.shotBlocksMade;
            totalPassesAttempted += stats.passesAttempted;
            totalPassesCompleted += stats.passesCompleted;
            totalAerialPassesAttempted += stats.aerialPassesAttempted;
            totalAerialPassesTargeted += stats.aerialPassesTargeted;
            totalAerialPassesCompleted += stats.aerialPassesCompleted;
            totalPacesRan += stats.pacesRan;
            totalGroundDuelsInvolved += stats.groundDuelsInvolved;
            totalGroundDuelsWon += stats.groundDuelsWon;
            totalDribblesMade += stats.dribblesMade;
            totalTacklesMade += stats.tacklesMade;
            totalInterceptionsAttempted += stats.interceptionsAttempted;
            totalInterceptionsMade += stats.interceptionsMade;
            totalAerialChallengesInvolved += stats.aerialChallengesInvolved;
            totalAerialChallengesWon += stats.aerialChallengesWon;
            totalAssists += stats.assists;
            totalInjuries += stats.injuries;
            totalAttemptsSaved += stats.attemptsSaved;
            totalPossessionWon += stats.possessionWon;
            totalPossessionLost += stats.possessionLost;
            totalYellowCards += stats.yellowCards;
            totalRedCards += stats.redCards;
            totalXRecoveries += stats.xRecoveries;
            totalXDribbles += stats.xDribbles;
            totalXTackles += stats.xTackles;
            totalXGoals += stats.xGoals;
        }
    }

    [Serializable]
    public class GameLog
    {
        [JsonProperty("entries")]
        private List<string> gameLog;

        [JsonIgnore]
        private Stats stats; // Add this

        public GameLog()
        {
            gameLog = new List<string>();
        }

        public GameLog(Stats statsRef)
        {
            gameLog = new List<string>();
            this.stats = statsRef ?? throw new ArgumentNullException(nameof(statsRef)); // Ensure stats is never null
        }

        public void RebindStats(Stats statsRef)
        {
            stats = statsRef ?? throw new ArgumentNullException(nameof(statsRef));
            gameLog ??= new List<string>();
        }

        public void ReplaceEntries(IEnumerable<string> entries)
        {
            gameLog = entries != null
                ? new List<string>(entries)
                : new List<string>();
        }

        public void LogEvent(
            PlayerToken token // the main actor of the action
            , ActionType actionType // on of the loggable actions Enum
            // Optional input
            , int value = 1 // value to be added to stats, defaults to 1, except Paces.
            , PlayerToken connectedToken = null // optional reference to the connected Token 
            , string tackleType = "" // tackle, tackle from behind, reckless tackle, nutmeg
            , string shotType = "" // Shot, Snapshot from outside the box
            , string recoveryType = "" // 
            , string saveType = "" // for a loose ball by - but not handled by - for a corner by - and held by
        )
        {
            if (token == null)
            {
                Debug.LogError("❌ LogEvent ERROR: token is NULL!");
                return;
            }
            if (stats == null)
            {
                Debug.LogError("❌ LogEvent ERROR: stats is NULL!");
                return;
            }
            if (MatchManager.Instance == null || MatchManager.Instance.gameData == null)
            {
                Debug.LogError("❌ LogEvent ERROR: MatchManager.Instance or gameData is NULL!");
                return;
            }
            if (MatchManager.Instance.gameData.gameSettings == null)
            {
                Debug.LogError("❌ LogEvent ERROR: gameSettings is NULL!");
                return;
            }

            string teamName = token.isHomeTeam ? MatchManager.Instance.gameData.gameSettings.homeTeamName 
                                              : MatchManager.Instance.gameData.gameSettings.awayTeamName;

            string logEntry = $"{token.name} ({teamName}) ";

            // ✅ Ensure `connectedToken` isn't NULL before using it
            if (connectedToken != null)
            {
                Debug.Log($"🔍 Connected Token Found: {connectedToken.name}");
            }

            PlayerStats playerStats = stats.GetPlayerStats(token.playerName);
            PlayerStats connectedPlayerStats = connectedToken != null ? stats.GetPlayerStats(connectedToken.playerName) : null;
            TeamStats teamStats = stats.GetTeamStats(token.isHomeTeam);
            TeamStats connectedTeamStats = connectedToken != null ? stats.GetTeamStats(connectedToken.isHomeTeam) : null;


            switch (actionType)
            {
                case ActionType.Move:
                    logEntry += $"moves {value} paces";
                    playerStats.pacesRan += value;
                    teamStats.totalPacesRan += value;
                    // TODO: convert to km :p
                    break;
                
                case ActionType.PassAttempt:
                    logEntry += "attempts a ground pass";
                    playerStats.passesAttempted += value;
                    teamStats.totalPassesAttempted += value;
                    break;

                case ActionType.PassCompleted:
                    logEntry += "completes a ground pass";
                    playerStats.passesCompleted += value;
                    teamStats.totalPassesCompleted += value;
                    break;

                case ActionType.AerialPassAttempt:
                    logEntry += "attempts an aerial pass";
                    playerStats.aerialPassesAttempted += value;
                    teamStats.totalAerialPassesAttempted += value;
                    break;

                case ActionType.AerialPassTargeted:
                    logEntry += "accurate places an aerial pass";
                    playerStats.aerialPassesTargeted += value;
                    teamStats.totalAerialPassesTargeted += value;
                    break;

                case ActionType.AerialPassCompleted:
                    logEntry += "completes an aerial pass";
                    playerStats.aerialPassesCompleted += value;
                    teamStats.totalAerialPassesCompleted += value;
                    break;

                case ActionType.InterceptionAttempt:
                    logEntry += "attempts an interception";
                    playerStats.interceptionsAttempted += value;
                    teamStats.totalInterceptionsAttempted += value;
                    break;

                case ActionType.InterceptionSuccess:
                    switch (recoveryType)
                    {
                        case "steal":
                            logEntry += $"steals the ball from {connectedToken.name}";
                            break;
                        case "standard":
                            logEntry += $"Intercepts a Standard Pass from {connectedToken.name}";
                            break;
                        case "ftp":
                            logEntry += $"Intercepts a First-Time Pass from {connectedToken.name}";
                            break;
                        case "headedpass":
                            logEntry += $"Intercepts a headed Pass from {connectedToken.name}";
                            break;
                        case "long":
                            logEntry += $"Intercepts a Long Pass from {connectedToken.name}";
                            break;
                    }
                    playerStats.interceptionsMade += value;
                    playerStats.possessionWon += value;
                    connectedPlayerStats.possessionLost += value;
                    teamStats.totalInterceptionsMade += value;
                    teamStats.totalPossessionWon += value;
                    connectedTeamStats.totalPossessionLost += value;
                    break;

                case ActionType.BallRecovery:
                    logEntry += "wins possession";
                    switch (recoveryType)
                    {
                        case "tackle":
                            logEntry += $" after tackling {connectedToken.name}";
                            break;
                        case "long":
                            logEntry += $" after a Long Pass from {connectedToken.name}";
                            break;
                        case "high":
                            logEntry += $" after an Inaccurate High Pass from {connectedToken.name}";
                            break;
                        case "freeheader":
                            logEntry += $" after an Inaccurate High Pass from {connectedToken.name}";
                            break;
                        case "header":
                            logEntry += $" in the air from {connectedToken.name}";
                            break;
                        case "shot":
                            logEntry += $" after a shot from {connectedToken.name}";
                            break;
                        case "control":
                            logEntry += $" after a missed ball control from {connectedToken.name}";
                            break;
                        default:
                            logEntry += $"Unknown recoveryType: {recoveryType}";
                            break;
                    }
                    playerStats.possessionWon += value;
                    teamStats.totalPossessionWon += value;
                    if (connectedPlayerStats != null)
                    {
                        connectedPlayerStats.possessionLost += value;
                    }
                    if (connectedTeamStats != null)
                    {
                        connectedTeamStats.totalPossessionLost += value;
                    }
                    break;

                case ActionType.ShotAttempt:
                    switch (shotType)
                    {
                        case "snap":
                            logEntry += $"takes a Snapshot!";
                            break;
                        case "snapO":
                            logEntry += $"takes a Snapshot! from outside the box";
                            break;
                        case "shot":
                            logEntry += $"takes a SHOT!";
                            break;
                        case "header":
                            logEntry += $"takes a headed shot!";
                            break;
                        case "freeKick":
                            logEntry += $"takes a Free Kick shot!";
                            break;
                        case "penalty":
                            logEntry += $"takes a Penalty Kick!";
                            break;
                        case "shot0":
                            logEntry += $"takes a SHOT! from outside the box";
                            break;
                        default:
                            logEntry += $"Unknown ShotType";
                            break;
                    }
                    playerStats.shotsAttempted += value;
                    teamStats.totalShots += value;
                    break;

                case ActionType.ShotOnTarget:
                    logEntry += "completes an attempt on target";
                    playerStats.shotsOnTarget += value;
                    teamStats.totalShotsOnTarget += value;
                    break;

                case ActionType.ShotBlocked:
                    logEntry += connectedToken != null
                        ? $"has a shot blocked by {connectedToken.name}"
                        : "has a shot blocked";
                    playerStats.shotsBlocked += value;
                    teamStats.totalShotsBlocked += value;
                    break;

                case ActionType.ShotBlockAttempt:
                    logEntry += "attempts to block a shot";
                    playerStats.shotBlocksAttempted += value;
                    teamStats.totalShotBlocksAttempted += value;
                    break;

                case ActionType.ShotBlockMade:
                    logEntry += connectedToken != null
                        ? $"blocks a shot from {connectedToken.name}"
                        : "blocks a shot";
                    playerStats.shotBlocksMade += value;
                    teamStats.totalShotBlocksMade += value;
                    break;

                case ActionType.ShotOffTarget:
                    logEntry += "sends an attempt off target";
                    playerStats.shotsOffTarget += value;
                    teamStats.totalShotsOffTarget += value;
                    break;

                case ActionType.GoalScored:
                    logEntry += "scores a goal! ⚽";
                    playerStats.goals += value;
                    teamStats.totalGoals += value;
                    bool isPenaltyGoal = MatchManager.Instance.ConsumeNextGoalIsPenalty();
                    bool suppressAssist = MatchManager.Instance.ConsumeSuppressAssistForNextGoal() || isPenaltyGoal;
                    PlayerToken assistToken = suppressAssist ? null : MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose;
                    MatchManager.Instance.AddGoal(
                        token.playerName
                        , token.isHomeTeam
                        , MatchManager.Instance.GetCurrentGoalMinute()
                        , isPenaltyGoal
                        , assistToken?.playerName
                        , MatchManager.Instance.GetCurrentGoalMinuteLabel()
                    );
                    if (
                        assistToken != null &&
                        assistToken != token &&
                        assistToken.isHomeTeam == token.isHomeTeam
                    )
                    {
                        LogEvent(
                            assistToken,
                            ActionType.AssistProvided,
                            connectedToken: token
                        );
                    }
                    break;

                case ActionType.AssistProvided:
                    logEntry += "provides the assist for a goal! 🅰️";
                    playerStats.assists += value;
                    teamStats.totalAssists += value;
                    break;

                case ActionType.GroundDuelAttempt:
                    logEntry += "engages in a ground duel";
                    playerStats.groundDuelsInvolved += value;
                    teamStats.totalGroundDuelsInvolved += value;
                    connectedPlayerStats.groundDuelsInvolved += value;
                    connectedTeamStats.totalGroundDuelsInvolved += value;
                    break;

                case ActionType.GroundDuelWon:
                    switch (tackleType)
                    {
                        case "successful":
                            logEntry += $"successful tackle on {connectedToken.name}";
                            break;
                        case "nutmeg":
                            logEntry += $"successful nutmeg on {connectedToken.name}";
                            break;
                        case "keep":
                            logEntry += $"skips past {connectedToken.name}";
                            break;
                    }
                    playerStats.groundDuelsWon += value;
                    teamStats.totalGroundDuelsWon += value;
                    if (tackleType == "successful")
                    {
                        playerStats.tacklesMade += value;
                        teamStats.totalTacklesMade += value;
                    }
                    else if (tackleType == "nutmeg" || tackleType == "keep")
                    {
                        playerStats.dribblesMade += value;
                        teamStats.totalDribblesMade += value;
                    }
                    break;

                case ActionType.AerialChallengeAttempt:
                    logEntry += $"engages in an aerial challenge with {connectedToken.name}";
                    playerStats.aerialChallengesInvolved += value;
                    teamStats.totalAerialChallengesInvolved += value;
                    connectedPlayerStats.aerialChallengesInvolved += value;
                    connectedTeamStats.totalAerialChallengesInvolved += value;
                    break;

                case ActionType.AerialChallengeWon:
                    logEntry += "wins an aerial challenge" + ((connectedToken!= null) ? $" from {connectedToken.name}" : "");
                    playerStats.aerialChallengesWon += value;
                    teamStats.totalAerialChallengesWon += value;
                    break;

                case ActionType.SaveAttempt:
                    logEntry += "faces a shot";
                    playerStats.attemptsFaced += value;
                    break;

                case ActionType.SaveMade:
                    logEntry += "makes a save! 🧤";
                    switch (saveType)
                    {
                        case "held":
                            logEntry += "Saved and held!";
                            playerStats.attemptsSaved += value;
                            break;
                        case "loose":
                            logEntry += "Saved for a loose ball";
                            playerStats.attemptsSaved += value;
                            break;
                        case "corner":
                            logEntry += "Saved for corner Kick";
                            playerStats.attemptsSaved += value;
                            break;
                        default:
                            logEntry = "UNKNOWN SAVETYPE";
                            break;
                    }
                    teamStats.totalAttemptsSaved += value;
                    break;

                case ActionType.CornerWon:
                    logEntry += "win a corner";
                    teamStats.totalCorners += value;
                    break;

                case ActionType.YellowCardShown:
                    logEntry += $"is shown a Yellow Card 🟨 for something on {connectedToken.name}";
                    playerStats.yellowCards += value;
                    teamStats.totalYellowCards += value;
                    break;

                case ActionType.RedCardShown:
                    logEntry += $"is Sent Off for something on {connectedToken.name}";
                    playerStats.redCards += value;
                    teamStats.totalRedCards += value;
                    break;

                case ActionType.Injured:
                    logEntry += $"is Injured 🚑 by {connectedToken.name}";
                    playerStats.injuries += value;
                    teamStats.totalInjuries += value;
                    break;

                case ActionType.Substituted:
                    logEntry += $"⬇️ Subbed off for ⬆️ {connectedToken.name}";
                    teamStats.totalSubstiutions += value;
                    MatchManager.Instance.RecordSubstitutionEvent(token.playerName, connectedToken != null ? connectedToken.playerName : null, value);
                    break;

                default:
                    logEntry += "performs an unknown action";
                    break;
            }

            gameLog.Add(logEntry);
            MatchManager.Instance?.RecordStructuredGameLogAction(
                token,
                actionType,
                value,
                connectedToken,
                tackleType,
                shotType,
                recoveryType,
                saveType);
            MatchManager.Instance?.MarkLiveLogDirty();
            Debug.Log("[Game Log] " + logEntry);
        }

        public void LogExpectedRecovery(
            PlayerToken token,
            float expectedValue,
            PlayerToken connectedToken = null,
            string recoveryType = "")
        {
            if (token == null || stats == null)
            {
                return;
            }

            PlayerStats playerStats = stats.GetPlayerStats(token.playerName);
            TeamStats teamStats = stats.GetTeamStats(token.isHomeTeam);
            playerStats.xRecoveries += expectedValue;
            teamStats.totalXRecoveries += expectedValue;
            MatchManager.Instance?.RecordExpectedRecovery(
                token,
                expectedValue,
                connectedToken,
                recoveryType,
                playerStats.xRecoveries,
                teamStats.totalXRecoveries);

            string targetText = connectedToken != null ? $" against {connectedToken.name}" : string.Empty;
            string contextText = string.IsNullOrWhiteSpace(recoveryType) ? "recovery" : recoveryType;
            MatchManager.Instance?.MarkLiveLogDirty();
            Debug.Log($"[Expected Stats] {token.name} records xRecovery {expectedValue:0.###} on {contextText}{targetText}");
        }

        public void LogExpectedGoal(
            PlayerToken token,
            float expectedValue,
            string shotType = "")
        {
            if (token == null || stats == null)
            {
                return;
            }

            PlayerStats playerStats = stats.GetPlayerStats(token.playerName);
            TeamStats teamStats = stats.GetTeamStats(token.isHomeTeam);
            playerStats.xGoals += expectedValue;
            teamStats.totalXGoals += expectedValue;

            string contextText = string.IsNullOrWhiteSpace(shotType) ? "shot" : shotType;
            MatchManager.Instance?.MarkLiveLogDirty();
            Debug.Log($"[Expected Stats] {token.name} records xG {expectedValue:0.###} on {contextText}");
        }

        public void LogExpectedGroundDuel(
            PlayerToken attacker,
            PlayerToken defender,
            ExpectedStatsCalculator.GroundDuelExpectation expectation,
            string duelType = "")
        {
            if (attacker == null || defender == null || stats == null)
            {
                return;
            }

            PlayerStats attackerStats = stats.GetPlayerStats(attacker.playerName);
            TeamStats attackerTeamStats = stats.GetTeamStats(attacker.isHomeTeam);
            PlayerStats defenderStats = stats.GetPlayerStats(defender.playerName);
            TeamStats defenderTeamStats = stats.GetTeamStats(defender.isHomeTeam);

            attackerStats.xDribbles += expectation.xDribbles;
            attackerTeamStats.totalXDribbles += expectation.xDribbles;
            defenderStats.xTackles += expectation.xTackles;
            defenderTeamStats.totalXTackles += expectation.xTackles;

            string contextText = string.IsNullOrWhiteSpace(duelType) ? "ground duel" : duelType;
            MatchManager.Instance?.MarkLiveLogDirty();
            Debug.Log(
                $"[Expected Stats] {attacker.name} vs {defender.name} ({contextText}) " +
                $"xDribbles={expectation.xDribbles:0.###}, xTackles={expectation.xTackles:0.###}, " +
                $"tie={expectation.tieProbability:0.###}, foul={expectation.defenderFoulProbability:0.###}");
        }

        public List<string> GetGameLog()
        {
            return new List<string>(gameLog);
        }
    }

    [Serializable]
    public class GoalEvent
    {
        public string scorer;
        public int minute;
        public string minuteLabel;
        public bool isPenalty;
        public string assist;  // Optional

        public override string ToString()
        {
            string displayMinute = string.IsNullOrWhiteSpace(minuteLabel) ? $"{minute}'" : minuteLabel;
            if (isPenalty)
                return $"{scorer} {displayMinute}(p)";
            if (!string.IsNullOrEmpty(assist))
                return $"{scorer} {displayMinute} (A: {assist})";
            return $"{scorer} {displayMinute}";
        }
    }

    public enum ActionType
    {
        Move,
        PassAttempt,
        PassCompleted,
        AerialPassAttempt,
        AerialPassTargeted,
        AerialPassCompleted,
        InterceptionAttempt,
        InterceptionSuccess,
        ShotAttempt,
        ShotOnTarget,
        ShotBlocked,
        ShotBlockAttempt,
        ShotBlockMade,
        ShotOffTarget,
        GoalScored,
        BallRecovery,
        AssistProvided,
        GroundDuelAttempt,
        GroundDuelWon,
        AerialChallengeAttempt,
        AerialChallengeWon,
        SaveAttempt,
        YellowCardShown,
        RedCardShown,
        Injured,
        Substituted,
        SaveMade,
        CornerWon
    }


    public List<GoalEvent> homeScorers = new List<GoalEvent>();
    public List<GoalEvent> awayScorers = new List<GoalEvent>();
    private readonly Dictionary<string, int> playerSubOnCounts = new();
    private readonly Dictionary<string, int> playerSubOffCounts = new();

    public event Action OnGameSettingsLoaded;
    public event Action OnPlayersInstantiated;
    public enum TeamInAttack
    {
        Home,
        Away
    }
    public enum TeamAttackingDirection
    {
        LeftToRight,
        RightToLeft
    }
    public TeamAttackingDirection homeTeamDirection;
    public TeamAttackingDirection awayTeamDirection;

    public GameState currentState; // Tracks the current state of the match
    public TeamInAttack teamInAttack; // Tracks which team is in Attack
    public bool attackHasPossession; 
    // Singleton instance for easy access
    public static MatchManager Instance;
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Reference to the ball
    public GroundBallManager groundBallManager;
    public HighPassManager highPassManager;
    public LongBallManager longBallManager;
    public FirstTimePassManager firstTimePassManager;
    public MovementPhaseManager movementPhaseManager;
    public ShotManager shotManager;
    public PenaltyKickManager penaltyKickManager;
    public FreeKickManager freeKickManager;
    public PlayerTokenManager playerTokenManager;
    public FinalThirdManager finalThirdManager;
    public GoalFlowManager goalFlowManager;
    public KickoffManager kickoffManager;
    public HelperFunctions helperFunctions;
    public MatchStatsUI matchStatsUI;
    public GameData gameData;
    // public PlayerToken LastTokenToTouchTheBallOnPurpose { get; private set; }
    // public PlayerToken PreviousTokenToTouchTheBallOnPurpose { get; private set; }
    public PlayerToken LastTokenToTouchTheBallOnPurpose;
    public PlayerToken PreviousTokenToTouchTheBallOnPurpose;
    private PlayerToken pendingGoalKickRestartTaker;
    public string hangingPassType;
    public PlayerToken hangingPassExcludedCollector;
    public PlayerToken setPieceTakerExcludedFromNextTouch;
    public bool clearPreviousOnNextBallCollection;
    private bool nextGoalIsPenalty;
    private bool suppressAssistForNextGoal;
    public int difficulty_level;
    public int refereeLeniency;
    public bool isFTPAvailable = false;
    private const int StandardGroundBallDistance = 11;
    private const int ShortGroundBallDistance = 6;
    [SerializeField] private int pendingGroundBallDistance = StandardGroundBallDistance;
    [Header("Match Clock")]
    public bool useFastClock = false;
    public float fastClockMultiplier = 60f;
    public int currentHalf = 1;
    public int completedActionsThisHalf = 0;
    public int totalCompletedActions = 0;
    public bool isClockRunning = false;
    public bool isHalfExpired = false;
    public bool extraActionsDetermined = false;
    public int extraActionsTotal = 0;
    public int extraActionsRemaining = 0;
    [SerializeField] private float currentHalfRegulationSeconds = 0f;
    [SerializeField] private bool isWaitingForExtraActionsRoll = false;
    [SerializeField] private bool isMatchComplete = false;
    [SerializeField] private bool isHalfTimeFlowRunning = false;
    [SerializeField] private bool isPauseMenuOpen = false;
    [SerializeField] private bool areSubstitutionsAvailable = false;
    [SerializeField] private string substitutionsAvailabilityReason = string.Empty;
    [SerializeField] private bool goalkeeperReplacementRequired = false;
    [SerializeField] private bool goalkeeperReplacementTeamIsHome = true;
    [SerializeField] private bool emergencyGoalkeeperNominationRequired = false;
    [SerializeField] private bool emergencyGoalkeeperNominationTeamIsHome = true;
    [SerializeField] private string emergencyGoalkeeperNominationReason = string.Empty;
    [SerializeField] private int homeSubstitutionsUsed = 0;
    [SerializeField] private int awaySubstitutionsUsed = 0;
    [SerializeField] private bool extraTimeSubstitutionCreditGranted = false;
    [SerializeField] private bool isHalfEndPendingAfterGoalFlow = false;
    [SerializeField] private MatchActionKind currentCommittedActionKind = MatchActionKind.None;
    [SerializeField] private bool hasUnresolvedCommittedExtraAction = false;
    [SerializeField] private int committedExtraActionNumber = 0;
    [SerializeField] private string pendingShotGoalTimeLabel = string.Empty;
    [SerializeField] private int pendingShotGoalExtraActionNumber = 0;
    private TeamInAttack firstHalfKickoffTeam = TeamInAttack.Home;
    private Action pendingHalfGateContinuation;
    public bool IsWaitingForExtraActionsRoll => isWaitingForExtraActionsRoll;
    public bool IsPauseMenuOpen => isPauseMenuOpen;
    public bool IsGameplayInputBlocked => isPauseMenuOpen;
    public bool AreSubstitutionsAvailable => areSubstitutionsAvailable;
    public string SubstitutionsAvailabilityReason => substitutionsAvailabilityReason;
    public bool IsAnyGoalkeeperReplacementRequired => goalkeeperReplacementRequired;
    public bool IsEmergencyGoalkeeperNominationRequired => emergencyGoalkeeperNominationRequired;
    public string EmergencyGoalkeeperNominationReason => emergencyGoalkeeperNominationReason;
    public event Action OnSubstitutionStateChanged;
    private int nextGameplayEventSequence = 1;
    private int gameplayEventLoggingSuppressionDepth = 0;
    private int tokenStepMoveLoggingSuppressionDepth = 0;
    private bool initialSetupSnapshotRecorded = false;
    private const float LiveLogFlushIntervalSeconds = 1f;
    private bool liveLogDirty = false;
    private float nextLiveLogFlushTime = 0f;
    private float nextLiveLogFailureWarningTime = 0f;
    private string lastLiveLogFailureMessage = string.Empty;
    private bool hasLoggedInstructionFingerprint = false;
    private string lastInstructionLogFingerprint = string.Empty;

    public bool HasPlayingTokensRequiringSubstitution()
    {
        if (playerTokenManager == null)
        {
            return FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
                .Any(token => token != null && token.isPlaying && token.requiresSubstitution);
        }

        return playerTokenManager.GetPlayingTokens(true).Any(token => token.requiresSubstitution)
            || playerTokenManager.GetPlayingTokens(false).Any(token => token.requiresSubstitution);
    }

    public void HandleDoubleInjuredToken(PlayerToken token)
    {
        if (token == null || !token.isPlaying)
        {
            return;
        }

        if (GetSubstitutionsRemaining(token.isHomeTeam) <= 0)
        {
            Debug.LogWarning($"{token.name} cannot continue and no substitutions remain. Removing token from play.");
            playerTokenManager?.RemoveActiveToken(token);
            OnSubstitutionStateChanged?.Invoke();
            return;
        }

        SetSubstitutionsAvailable(true, "Double injury requires substitution");
        PauseMenuManager pauseMenuManager = FindAnyObjectByType<PauseMenuManager>();
        if (pauseMenuManager != null)
        {
            pauseMenuManager.OpenSubstitutionsForForcedSubstitution();
        }
        else
        {
            SetPauseMenuOpen(true);
            Debug.LogWarning("PauseMenuManager not found. Double-injured token requires substitution before play can continue.");
        }
    }

    public void HandleSentOff(PlayerToken token)
    {
        if (token == null)
        {
            return;
        }

        bool wasGoalkeeper = token.IsGoalKeeper;
        bool isHomeTeam = token.isHomeTeam;
        if (!token.isSentOff || token.isPlaying)
        {
            token.MarkSentOff();
        }
        Debug.Log($"{token.playerName} (Jersey {token.jerseyNumber}) has been sent off.");

        if (wasGoalkeeper)
        {
            HandleGoalkeeperSentOff(isHomeTeam, token.playerName);
        }

        OnSubstitutionStateChanged?.Invoke();
    }

    private void HandleGoalkeeperSentOff(bool isHomeTeam, string goalkeeperName)
    {
        PauseMatchClockForSetPiecePrep();
        bool hasSubstitutionsRemaining = GetSubstitutionsRemaining(isHomeTeam) > 0;
        bool hasBenchGoalkeeper = HasAvailableBenchGoalkeeper(isHomeTeam);

        goalkeeperReplacementRequired = hasSubstitutionsRemaining && hasBenchGoalkeeper;
        goalkeeperReplacementTeamIsHome = isHomeTeam;
        emergencyGoalkeeperNominationRequired = !goalkeeperReplacementRequired;
        emergencyGoalkeeperNominationTeamIsHome = isHomeTeam;

        if (goalkeeperReplacementRequired)
        {
            emergencyGoalkeeperNominationReason = string.Empty;
            SetSubstitutionsAvailable(true, "Goalkeeper sent off - replace an outfield player with a bench goalkeeper");
            PauseMenuManager pauseMenuManager = FindAnyObjectByType<PauseMenuManager>();
            if (pauseMenuManager != null)
            {
                pauseMenuManager.OpenSubstitutionsForForcedSubstitution();
            }
            else
            {
                SetPauseMenuOpen(true);
                Debug.LogWarning("PauseMenuManager not found. Goalkeeper replacement must be completed before play can continue.");
            }

            Debug.Log($"Goalkeeper {goalkeeperName} has been sent off. Select a playing outfield player to come off and bring on the bench goalkeeper.");
            return;
        }

        string missingReason = !hasSubstitutionsRemaining
            ? "no substitutions remain"
            : "no bench goalkeeper is available";
        emergencyGoalkeeperNominationReason = $"Goalkeeper sent off and {missingReason}. Nominate a playing outfield player as emergency goalkeeper.";
        SetSubstitutionsAvailable(false, emergencyGoalkeeperNominationReason);
        OpenEmergencyGoalkeeperNominationPause();
        Debug.LogWarning(emergencyGoalkeeperNominationReason);
    }

    public bool IsGoalkeeperReplacementRequired(bool isHomeTeam)
    {
        return goalkeeperReplacementRequired && goalkeeperReplacementTeamIsHome == isHomeTeam;
    }

    public bool IsEmergencyGoalkeeperNominationRequiredForTeam(bool isHomeTeam)
    {
        return emergencyGoalkeeperNominationRequired && emergencyGoalkeeperNominationTeamIsHome == isHomeTeam;
    }

    public bool HasAvailableBenchGoalkeeper(bool isHomeTeam)
    {
        if (playerTokenManager == null)
        {
            return false;
        }

        return playerTokenManager.GetAvailableBenchTokens(isHomeTeam)
            .Any(token => token != null && token.IsGoalKeeper && !token.isSentOff && !token.wasSubbedOff);
    }

    public bool HasActiveGoalkeeper(bool isHomeTeam)
    {
        if (playerTokenManager == null)
        {
            return FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
                .Any(token => token != null
                    && token.isHomeTeam == isHomeTeam
                    && token.isPlaying
                    && !token.isSentOff
                    && token.GetCurrentHex() != null
                    && token.IsGoalKeeper);
        }

        return playerTokenManager.GetPlayingTokens(isHomeTeam)
            .Any(token => token != null && !token.isSentOff && token.GetCurrentHex() != null && token.IsGoalKeeper);
    }

    public List<PlayerToken> GetEmergencyGoalkeeperNominees(bool isHomeTeam)
    {
        if (!IsEmergencyGoalkeeperNominationRequiredForTeam(isHomeTeam) || playerTokenManager == null)
        {
            return new List<PlayerToken>();
        }

        return playerTokenManager.GetPlayingTokens(isHomeTeam)
            .Where(token => token != null
                && !token.IsGoalKeeper
                && !token.isSentOff
                && token.GetCurrentHex() != null)
            .OrderBy(token => token.jerseyNumber)
            .ToList();
    }

    public bool NominateEmergencyGoalkeeper(PlayerToken token, out string error)
    {
        error = string.Empty;
        if (token == null)
        {
            error = "A player must be selected as emergency goalkeeper.";
            return false;
        }

        if (!IsEmergencyGoalkeeperNominationRequiredForTeam(token.isHomeTeam))
        {
            error = "This team does not need to nominate an emergency goalkeeper.";
            return false;
        }

        if (!token.isPlaying || token.GetCurrentHex() == null || token.isSentOff)
        {
            error = $"{token.playerName} is not an eligible playing outfield player.";
            return false;
        }

        if (token.IsGoalKeeper)
        {
            error = $"{token.playerName} is already a goalkeeper.";
            return false;
        }

        ConvertOutfieldToGK(token);
        CompleteGoalkeeperReplacement(token.isHomeTeam);
        Debug.Log($"{token.playerName} (Jersey {token.jerseyNumber}) has been nominated as emergency goalkeeper.");
        return true;
    }

    public void CompleteGoalkeeperReplacement(bool isHomeTeam)
    {
        bool wasEmergencyNomination = IsEmergencyGoalkeeperNominationRequiredForTeam(isHomeTeam);
        if (!IsGoalkeeperReplacementRequired(isHomeTeam) && !wasEmergencyNomination)
        {
            return;
        }

        if (!HasActiveGoalkeeper(isHomeTeam))
        {
            Debug.LogWarning($"{(isHomeTeam ? "Home" : "Away")} still has no active goalkeeper.");
            return;
        }

        goalkeeperReplacementRequired = false;
        emergencyGoalkeeperNominationRequired = false;
        emergencyGoalkeeperNominationReason = string.Empty;
        SetSubstitutionsAvailable(false, "Goalkeeper replacement completed");
        if (wasEmergencyNomination)
        {
            SetPauseMenuOpen(false);
        }
        OnSubstitutionStateChanged?.Invoke();
    }

    public void OpenEmergencyGoalkeeperNominationPause()
    {
        if (!emergencyGoalkeeperNominationRequired)
        {
            return;
        }

        SetPauseMenuOpen(true);
    }

    public bool IsBenchGoalkeeperSentOff(bool isHomeTeam)
    {
        return !HasAvailableBenchGoalkeeper(isHomeTeam);
    }

    public void ConvertOutfieldToGK(PlayerToken token)
    {
        if (token == null || token.IsGoalKeeper)
        {
            Debug.LogWarning($"Cannot convert {token?.playerName ?? "null"} to goalkeeper - invalid player.");
            return;
        }

        Debug.Log($"Converting {token.playerName} (Jersey {token.jerseyNumber}) from outfield player to goalkeeper.");
        token.ConvertToGK();
        ApplyTeamGoalkeeperKit(token);
    }

    private void ApplyTeamGoalkeeperKit(PlayerToken token)
    {
        if (token == null)
        {
            return;
        }

        TokenStyleDefinition goalkeeperStyle = ResolveTeamGoalkeeperStyle(token);
        if (goalkeeperStyle == null)
        {
            return;
        }

        PlayerTokenVisuals visuals = token.GetComponent<PlayerTokenVisuals>();
        if (visuals == null)
        {
            visuals = token.gameObject.AddComponent<PlayerTokenVisuals>();
        }

        visuals.ApplyStyle(goalkeeperStyle);

        TextMeshPro numberText = token.GetComponentInChildren<TextMeshPro>(true);
        if (numberText != null)
        {
            visuals.ApplyNumberStyle(numberText, goalkeeperStyle);
        }
    }

    private TokenStyleDefinition ResolveTeamGoalkeeperStyle(PlayerToken token)
    {
        GameSettings settings = gameData?.gameSettings;
        if (settings == null)
        {
            return null;
        }

        string goalkeeperKit = token.isHomeTeam ? settings.homeGKKit : settings.awayGKKit;
        string fallbackKit = token.isHomeTeam ? settings.homeKit : settings.awayKit;
        string kitId = string.IsNullOrWhiteSpace(goalkeeperKit) ? fallbackKit : goalkeeperKit;
        return TokenKitCatalog.ResolveStyle(kitId);
    }

    public void RecordSubstitutionEvent(string playerOffName, string playerOnName, int value = 1)
    {
        if (value <= 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerOffName))
        {
            IncrementPlayerEventCount(playerSubOffCounts, playerOffName, value);
        }

        if (!string.IsNullOrWhiteSpace(playerOnName))
        {
            IncrementPlayerEventCount(playerSubOnCounts, playerOnName, value);
        }
    }

    public int GetPlayerSubOnCount(string playerName)
    {
        return GetPlayerEventCount(playerSubOnCounts, playerName);
    }

    public int GetPlayerSubOffCount(string playerName)
    {
        return GetPlayerEventCount(playerSubOffCounts, playerName);
    }

    public void SetSubstitutionsAvailable(bool available, string reason = "")
    {
        string normalizedReason = reason ?? string.Empty;
        if (areSubstitutionsAvailable == available && substitutionsAvailabilityReason == normalizedReason)
        {
            return;
        }

        areSubstitutionsAvailable = available;
        substitutionsAvailabilityReason = normalizedReason;
        OnSubstitutionStateChanged?.Invoke();
        Debug.Log(available
            ? $"Substitutions are now available: {substitutionsAvailabilityReason}"
            : $"Substitutions are no longer available: {substitutionsAvailabilityReason}");
        RecordGameplayOutcome(
            "substitution.availability",
            "substitution",
            available ? "available" : "unavailable",
            details: CreateDetails(
                ("reason", substitutionsAvailabilityReason),
                ("available", available)));
    }

    public int GetSubstitutionsUsed(bool isHomeTeam)
    {
        return isHomeTeam ? homeSubstitutionsUsed : awaySubstitutionsUsed;
    }

    public int GetSubstitutionsRemaining(bool isHomeTeam)
    {
        return Mathf.Max(0, GetSubstitutionLimit(isHomeTeam) - GetSubstitutionsUsed(isHomeTeam));
    }

    public int GetSubstitutionLimit(bool isHomeTeam)
    {
        return extraTimeSubstitutionCreditGranted
            ? ExtraTimeMaxSubstitutionsPerTeam
            : MaxSubstitutionsPerTeam;
    }

    public bool CanRegisterSubstitution(PlayerToken playerOff, PlayerToken playerOn, out string error)
    {
        error = string.Empty;
        if (playerOff == null || playerOn == null)
        {
            error = "Both outgoing and incoming players are required.";
            return false;
        }

        if (!areSubstitutionsAvailable)
        {
            error = "Substitutions are not available right now.";
            return false;
        }

        if (playerOff.isHomeTeam != playerOn.isHomeTeam)
        {
            error = "Both players must belong to the same team.";
            return false;
        }

        if (goalkeeperReplacementRequired && !IsGoalkeeperReplacementRequired(playerOff.isHomeTeam))
        {
            error = "Only the team whose goalkeeper was sent off can substitute right now.";
            return false;
        }

        if (!playerOff.isPlaying || playerOff.GetCurrentHex() == null)
        {
            error = $"{playerOff.name} is not currently playing.";
            return false;
        }

        if (playerOff.isSentOff)
        {
            error = $"{playerOff.name} has been sent off and cannot be substituted.";
            return false;
        }

        if (playerOn.isPlaying)
        {
            error = $"{playerOn.name} is already playing.";
            return false;
        }

        if (playerOn.wasSubbedOff)
        {
            error = $"{playerOn.name} has already been subbed off and cannot return.";
            return false;
        }

        bool isGoalkeeperReplacement = IsGoalkeeperReplacementRequired(playerOff.isHomeTeam);
        if (isGoalkeeperReplacement)
        {
            if (playerOff.IsGoalKeeper)
            {
                error = "A sent-off goalkeeper must be replaced by taking off a playing outfield player.";
                return false;
            }

            if (!playerOn.IsGoalKeeper)
            {
                error = "A bench goalkeeper must come on after a goalkeeper is sent off.";
                return false;
            }
        }
        else if (playerOff.IsGoalKeeper)
        {
            if (!playerOn.IsGoalKeeper || playerOn.jerseyNumber != 12)
            {
                error = "The goalkeeper can only be replaced by the bench goalkeeper.";
                return false;
            }
        }
        else if (playerOn.IsGoalKeeper)
        {
            error = "An outfield player cannot be replaced by a goalkeeper.";
            return false;
        }

        if (GetSubstitutionsRemaining(playerOff.isHomeTeam) <= 0)
        {
            error = $"{(playerOff.isHomeTeam ? "Home" : "Away")} has no substitutions remaining.";
            return false;
        }

        return true;
    }

    public bool RegisterSubstitution(PlayerToken playerOff, PlayerToken playerOn, out string error)
    {
        if (!CanRegisterSubstitution(playerOff, playerOn, out error))
        {
            return false;
        }

        if (playerOff.isHomeTeam)
        {
            homeSubstitutionsUsed++;
            gameData.stats.homeTeamStats.totalSubstiutions++;
        }
        else
        {
            awaySubstitutionsUsed++;
            gameData.stats.awayTeamStats.totalSubstiutions++;
        }

        RecordSubstitutionEvent(playerOff.playerName, playerOn.playerName);
        RecordGameplayOutcome(
            "substitution.committed",
            "substitution",
            "registered",
            actor: playerOff,
            relatedToken: playerOn,
            sourceHex: playerOff.GetCurrentHex(),
            details: CreateDetails(
                ("team", playerOff.isHomeTeam ? "home" : "away"),
                ("playerOff", playerOff.playerName),
                ("playerOn", playerOn.playerName),
                ("used", GetSubstitutionsUsed(playerOff.isHomeTeam)),
                ("remaining", GetSubstitutionsRemaining(playerOff.isHomeTeam)),
                ("reason", substitutionsAvailabilityReason),
                ("goalkeeperReplacement", IsGoalkeeperReplacementRequired(playerOff.isHomeTeam))));
        OnSubstitutionStateChanged?.Invoke();
        return true;
    }

    private static void IncrementPlayerEventCount(Dictionary<string, int> counts, string playerName, int value)
    {
        if (counts.TryGetValue(playerName, out int currentValue))
        {
            counts[playerName] = currentValue + value;
        }
        else
        {
            counts[playerName] = value;
        }
    }

    private static int GetPlayerEventCount(Dictionary<string, int> counts, string playerName)
    {
        return !string.IsNullOrWhiteSpace(playerName) && counts.TryGetValue(playerName, out int count) ? count : 0;
    }

    public void EnsureGameplayEventLogInitialized()
    {
        if (gameData == null)
        {
            return;
        }

        gameData.eventLogSchemaVersion = GameplayEvent.CurrentSchemaVersion;
        gameData.events ??= new List<GameplayEvent>();
        int maxSequence = gameData.events
            .Where(gameplayEvent => gameplayEvent != null)
            .Select(gameplayEvent => gameplayEvent.sequenceNumber)
            .DefaultIfEmpty(0)
            .Max();
        if (nextGameplayEventSequence <= maxSequence)
        {
            nextGameplayEventSequence = maxSequence + 1;
        }
    }

    public bool ShouldRecordGameplayEvents =>
        gameData?.events != null && gameplayEventLoggingSuppressionDepth <= 0;

    public void BeginGameplayEventLoggingSuppression()
    {
        gameplayEventLoggingSuppressionDepth++;
    }

    public void EndGameplayEventLoggingSuppression()
    {
        gameplayEventLoggingSuppressionDepth = Mathf.Max(0, gameplayEventLoggingSuppressionDepth - 1);
    }

    public void BeginTokenStepMoveLoggingSuppression()
    {
        tokenStepMoveLoggingSuppressionDepth++;
    }

    public void EndTokenStepMoveLoggingSuppression()
    {
        tokenStepMoveLoggingSuppressionDepth = Mathf.Max(0, tokenStepMoveLoggingSuppressionDepth - 1);
    }

    public void MarkLiveLogDirty()
    {
        if (gameData == null)
        {
            return;
        }

        liveLogDirty = true;
    }

    public bool FlushLiveLogNow(out string savedPath, out string message, bool force = false)
    {
        if (!force && !liveLogDirty)
        {
            ApplicationManager.EnsureInstanceExists();
            savedPath = ApplicationManager.Instance.GetLastSavedFilePath();
            message = "Live log is already up to date.";
            return true;
        }

        bool didWrite = RoomLiveLogService.PatchActiveLiveLog(this, out savedPath, out message);
        if (didWrite)
        {
            liveLogDirty = false;
            lastLiveLogFailureMessage = string.Empty;
            nextLiveLogFlushTime = Time.unscaledTime + LiveLogFlushIntervalSeconds;
        }

        return didWrite;
    }

    public bool FlushLiveLogNow()
    {
        return FlushLiveLogNow(out _, out _, force: true);
    }

    public bool CaptureLiveLogFile(
        bool openFolder,
        out string captureFolderPath,
        out string capturedFilePath,
        out string message)
    {
        bool didCapture = RoomLiveLogService.CaptureActiveLogFile(
            this,
            openFolder,
            out captureFolderPath,
            out capturedFilePath,
            out message);
        if (didCapture)
        {
            liveLogDirty = false;
            lastLiveLogFailureMessage = string.Empty;
            nextLiveLogFlushTime = Time.unscaledTime + LiveLogFlushIntervalSeconds;
        }

        return didCapture;
    }

    public void RecordInstructionSnapshotIfChanged(GameplayInstructionSnapshot snapshot)
    {
        string semanticCta = BuildInstructionSemanticCta(snapshot);
        string fingerprint = BuildInstructionFingerprint(snapshot, semanticCta);
        bool hasInstruction = HasInstructionText(snapshot);
        if (!hasInstruction && !hasLoggedInstructionFingerprint)
        {
            hasLoggedInstructionFingerprint = true;
            lastInstructionLogFingerprint = fingerprint;
            return;
        }

        if (hasLoggedInstructionFingerprint
            && string.Equals(lastInstructionLogFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return;
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent("instruction.changed");
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.instruction = hasInstruction ? snapshot : null;
        gameplayEvent.result = CreateResult(
            "instruction_cta",
            hasInstruction ? "shown" : "cleared",
            CreateDetails(
                ("manager", snapshot?.manager),
                ("instructionSide", snapshot?.instructionSide),
                ("expectedTeam", snapshot?.expectedTeam),
                ("expectedInput", snapshot?.expectedInput),
                ("expectedKeys", snapshot?.expectedKeys != null ? string.Join(",", snapshot.expectedKeys) : null),
                ("semanticCta", semanticCta),
                ("instructionText", snapshot?.instructionText)));
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
        hasLoggedInstructionFingerprint = true;
        lastInstructionLogFingerprint = fingerprint;
    }

    private GameplayInstructionSnapshot CaptureInstructionSnapshotForEvent()
    {
        GameplayInstructionSnapshot snapshot = GameDebugMonitor.Instance != null
            ? GameDebugMonitor.Instance.GetCurrentInstructionSnapshotForLog()
            : null;
        return HasInstructionText(snapshot) ? snapshot : null;
    }

    private static bool HasInstructionText(GameplayInstructionSnapshot snapshot)
    {
        return !string.IsNullOrWhiteSpace(snapshot?.instructionText);
    }

    private static string BuildInstructionFingerprint(GameplayInstructionSnapshot snapshot, string semanticCta)
    {
        if (!HasInstructionText(snapshot))
        {
            return "empty";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("manager=").Append(snapshot.manager).Append('|');
        builder.Append("side=").Append(snapshot.instructionSide).Append('|');
        builder.Append("team=").Append(snapshot.expectedTeam).Append('|');
        builder.Append("keys=").Append(BuildExpectedKeysFingerprint(snapshot.expectedKeys)).Append('|');
        builder.Append("semanticCta=").Append(semanticCta).Append('|');
        return builder.ToString();
    }

    private static string BuildInstructionSemanticCta(GameplayInstructionSnapshot snapshot)
    {
        if (!HasInstructionText(snapshot))
        {
            return "none";
        }

        string manager = snapshot.manager ?? string.Empty;
        if (InstructionManagerContains(manager, nameof(HighPassManager)))
        {
            if (GetInstructionDetailBool(snapshot, "highPassIsAwaitingTargetSelection"))
            {
                return "high_pass_target_selection";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsWaitingForAttackerSelection")
                || GetInstructionDetailBool(snapshot, "highPassIsWaitingForAttackerMove"))
            {
                return "high_pass_attacker_movement";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsWaitingForDefenderSelection")
                || GetInstructionDetailBool(snapshot, "highPassIsWaitingForDefenderMove"))
            {
                return "high_pass_defender_movement";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsWaitingForAccuracyRoll"))
            {
                return "high_pass_accuracy_roll";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsWaitingForDirectionRoll"))
            {
                return "high_pass_direction_roll";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsWaitingForDistanceRoll"))
            {
                return "high_pass_distance_roll";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsWaitingForDefGKChallengeDecision"))
            {
                return "high_pass_goalkeeper_rush";
            }

            if (GetInstructionDetailBool(snapshot, "highPassIsAvailable"))
            {
                return "high_pass_available";
            }
        }

        if (InstructionManagerContains(manager, nameof(GroundBallManager)))
        {
            if (GetInstructionDetailBool(snapshot, "isWaitingForDiceRoll"))
            {
                return "standard_pass_roll";
            }

            if (GetInstructionDetailBool(snapshot, "isAwaitingTargetSelection"))
            {
                return "standard_pass_target_selection";
            }

            if (GetInstructionDetailBool(snapshot, "isAvailable"))
            {
                return "standard_pass_available";
            }
        }

        return NormalizeInstructionInput(snapshot.expectedInput);
    }

    private static bool InstructionManagerContains(string managers, string managerName)
    {
        return !string.IsNullOrWhiteSpace(managers)
            && !string.IsNullOrWhiteSpace(managerName)
            && managers.IndexOf(managerName, StringComparison.Ordinal) >= 0;
    }

    private static bool GetInstructionDetailBool(GameplayInstructionSnapshot snapshot, string key)
    {
        return snapshot?.details != null
            && snapshot.details.TryGetValue(key, out string value)
            && bool.TryParse(value, out bool parsedValue)
            && parsedValue;
    }

    private static string NormalizeInstructionInput(string expectedInput)
    {
        return string.IsNullOrWhiteSpace(expectedInput)
            ? "none"
            : expectedInput.Trim();
    }

    private static string BuildExpectedKeysFingerprint(List<string> expectedKeys)
    {
        if (expectedKeys == null || expectedKeys.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ",",
            expectedKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
    }

    public GameplayEvent BeginInputClick(ClickInputData clickData)
    {
        if (clickData == null)
        {
            return null;
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "input.click",
            actor: clickData.token,
            targetHex: clickData.hex);
        if (gameplayEvent == null)
        {
            return null;
        }

        PopulateInputClickEvent(gameplayEvent, clickData);
        AppendGameplayEvent(gameplayEvent);
        return gameplayEvent;
    }

    public void CompleteInputClick(GameplayEvent gameplayEvent, ClickInputData clickData)
    {
        if (gameplayEvent == null || clickData == null)
        {
            return;
        }

        PopulateInputClickEvent(gameplayEvent, clickData);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        MarkLiveLogDirty();
    }

    public void RecordInputClick(PlayerToken clickedToken, HexCell clickedHex, bool isOutOfBounds)
    {
        ClickInputData clickData = new ClickInputData(clickedToken, clickedHex, isOutOfBounds);
        GameplayEvent gameplayEvent = BeginInputClick(clickData);
        CompleteInputClick(gameplayEvent, clickData);
    }

    public void RecordUiClick(
        string panel,
        string control,
        string action,
        string outcome = "clicked",
        params (string Key, object Value)[] details)
    {
        panel = string.IsNullOrWhiteSpace(panel) ? "ui" : panel;
        control = string.IsNullOrWhiteSpace(control) ? "unknown_control" : control;
        action = string.IsNullOrWhiteSpace(action) ? control : action;
        outcome = string.IsNullOrWhiteSpace(outcome) ? "clicked" : outcome;

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "input.ui_click",
            sourceHex: ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.label = $"{panel}.{control}";
        gameplayEvent.input = new GameplayInputEvent
        {
            inputType = "ui_click",
            button = control,
            consumed = true,
            consumedBy = panel
        };

        Dictionary<string, string> resultDetails = MergeDetails(
            CreateDetails(
                ("panel", panel),
                ("control", control)),
            details);
        gameplayEvent.result = CreateResult(action, outcome, resultDetails);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
        FlushLiveLogNow(out _, out _, force: true);
    }

    private void PopulateInputClickEvent(GameplayEvent gameplayEvent, ClickInputData clickData)
    {
        gameplayEvent.input = new GameplayInputEvent
        {
            inputType = "mouse",
            button = "left",
            consumed = clickData.isConsumed,
            consumedBy = clickData.consumedBy,
            consumedTeam = FormatTeamSide(clickData.consumedByHomeTeam)
        };
        gameplayEvent.actionPreview = clickData.actionPreview;
        gameplayEvent.result = CreateResult(
            clickData.action,
            clickData.outcome,
            MergeDetails(
                clickData.details,
                ("clickedTokenKey", GetStableTokenKey(clickData.token)),
                ("clickedHex", FormatHex(clickData.hex)),
                ("consumedBy", clickData.consumedBy),
                ("consumedTeam", FormatTeamSide(clickData.consumedByHomeTeam))));
        AddPreviewRelatedTokens(gameplayEvent, clickData.actionPreview);
    }

    public GameplayEvent BeginInputKey(KeyPressData keyData)
    {
        if (keyData == null)
        {
            return null;
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent("input.key");
        if (gameplayEvent == null)
        {
            return null;
        }

        PopulateInputKeyEvent(gameplayEvent, keyData);
        AppendGameplayEvent(gameplayEvent);
        return gameplayEvent;
    }

    public void CompleteInputKey(GameplayEvent gameplayEvent, KeyPressData keyData)
    {
        if (gameplayEvent == null || keyData == null)
        {
            return;
        }

        PopulateInputKeyEvent(gameplayEvent, keyData);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        MarkLiveLogDirty();
    }

    public void RecordInputKey(KeyPressData keyData)
    {
        GameplayEvent gameplayEvent = BeginInputKey(keyData);
        CompleteInputKey(gameplayEvent, keyData);
    }

    private void PopulateInputKeyEvent(GameplayEvent gameplayEvent, KeyPressData keyData)
    {
        gameplayEvent.input = new GameplayInputEvent
        {
            inputType = "key",
            key = keyData.key.ToString(),
            chord = BuildKeyChord(keyData),
            shift = keyData.shift,
            ctrl = keyData.ctrl,
            alt = keyData.alt,
            consumed = keyData.isConsumed,
            consumedBy = keyData.consumedBy,
            consumedTeam = FormatTeamSide(keyData.consumedByHomeTeam)
        };
        gameplayEvent.result = CreateResult(
            "key_press",
            keyData.isConsumed ? "consumed" : "ignored",
            CreateDetails(
                ("consumedBy", keyData.consumedBy),
                ("consumedTeam", FormatTeamSide(keyData.consumedByHomeTeam))));
    }

    public void RecordActionSelection(string action, PlayerToken actor = null, HexCell targetHex = null, Dictionary<string, string> details = null)
    {
        Dictionary<string, string> resultDetails = details != null
            ? new Dictionary<string, string>(details)
            : new Dictionary<string, string>();
        AddDetail(resultDetails, "action", action);

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "action.selected",
            actor: actor,
            sourceHex: actor != null ? actor.GetCurrentHex() : ball?.GetCurrentHex(),
            targetHex: targetHex);
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.result = CreateResult(action, "selected", resultDetails);
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordActionCommitted(
        MatchActionKind actionKind,
        string commitSource = "user",
        string commitReason = "")
    {
        if (actionKind == MatchActionKind.None)
        {
            return;
        }

        string actionName = FormatActionKind(actionKind);
        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "action.committed",
            actor: LastTokenToTouchTheBallOnPurpose,
            sourceHex: ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.result = CreateResult(
            actionName,
            "committed",
            CreateDetails(
                ("action", actionName),
                ("actionKind", actionKind.ToString()),
                ("commitSource", commitSource),
                ("commitReason", commitReason)));
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordInputChoiceOffered(
        string choiceId,
        string prompt,
        PlayerToken actor,
        bool? teamIsHome,
        params (string Key, string Action, string Label)[] options)
    {
        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "input.choice",
            actor: actor,
            sourceHex: actor != null ? actor.GetCurrentHex() : ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        List<GameplayChoiceOption> choiceOptions = BuildChoiceOptions(options);
        gameplayEvent.choice = new GameplayChoiceEvent
        {
            choiceId = choiceId,
            prompt = prompt,
            teamSide = FormatTeamSide(teamIsHome),
            actorTokenKey = GetStableTokenKey(actor),
            options = choiceOptions
        };
        gameplayEvent.result = CreateResult(
            "choice",
            "offered",
            CreateDetails(
                ("choiceId", choiceId),
                ("prompt", prompt),
                ("teamSide", FormatTeamSide(teamIsHome)),
                ("actorTokenKey", GetStableTokenKey(actor)),
                ("optionKeys", string.Join(",", choiceOptions.Select(option => option.key))),
                ("optionActions", string.Join(",", choiceOptions.Select(option => option.action)))));
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordInputChoiceSelected(
        string choiceId,
        string selectedKey,
        string selectedAction,
        PlayerToken actor,
        bool? teamIsHome)
    {
        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "input.choice",
            actor: actor,
            sourceHex: actor != null ? actor.GetCurrentHex() : ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.choice = new GameplayChoiceEvent
        {
            choiceId = choiceId,
            teamSide = FormatTeamSide(teamIsHome),
            actorTokenKey = GetStableTokenKey(actor),
            selectedKey = selectedKey,
            selectedAction = selectedAction
        };
        gameplayEvent.result = CreateResult(
            "choice",
            "selected",
            CreateDetails(
                ("choiceId", choiceId),
                ("selectedKey", selectedKey),
                ("selectedAction", selectedAction),
                ("teamSide", FormatTeamSide(teamIsHome)),
                ("actorTokenKey", GetStableTokenKey(actor))));
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    public GameplayDiceRollResult ResolveGameplayDiceRoll(
        string context,
        RollInputOverride? rollOverride = null,
        PlayerToken actor = null,
        PlayerToken relatedToken = null,
        HexCell sourceHex = null,
        HexCell targetHex = null,
        bool jackpotEnabled = true,
        Dictionary<string, string> details = null)
    {
        int randomRoll;
        bool randomJackpot;
        if (jackpotEnabled && helperFunctions != null)
        {
            (randomRoll, randomJackpot) = helperFunctions.DiceRoll();
        }
        else
        {
            randomRoll = UnityEngine.Random.Range(1, 7);
            randomJackpot = false;
        }

        bool overrideUsed = rollOverride.HasValue && rollOverride.Value.hasOverride;
        int resolvedRoll = overrideUsed
            ? Mathf.Clamp(rollOverride.Value.isJackpot ? 6 : rollOverride.Value.roll, 1, 6)
            : randomRoll;
        bool resolvedJackpot = jackpotEnabled && (overrideUsed ? rollOverride.Value.isJackpot : randomJackpot);

        GameplayDiceRollResult rollResult = new GameplayDiceRollResult
        {
            context = context,
            roll = resolvedRoll,
            isJackpot = resolvedJackpot,
            jackpotEnabled = jackpotEnabled,
            overrideUsed = overrideUsed,
            randomRoll = randomRoll,
            randomJackpot = randomJackpot
        };

        Dictionary<string, string> resultDetails = details != null
            ? new Dictionary<string, string>(details)
            : new Dictionary<string, string>();
        if (overrideUsed)
        {
            AddDetail(resultDetails, "overrideRoll", rollOverride.Value.roll);
            AddDetail(resultDetails, "overrideJackpot", rollOverride.Value.isJackpot);
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "dice.roll",
            actor: actor,
            relatedToken: relatedToken,
            sourceHex: sourceHex ?? actor?.GetCurrentHex(),
            targetHex: targetHex);
        if (gameplayEvent == null)
        {
            return rollResult;
        }

        gameplayEvent.dice = new GameplayDiceEvent
        {
            context = context,
            roll = resolvedRoll,
            isJackpot = resolvedJackpot,
            jackpotEnabled = jackpotEnabled,
            overrideUsed = overrideUsed,
            randomRoll = randomRoll,
            randomJackpot = randomJackpot,
            modifiers = resultDetails
        };
        gameplayEvent.result = CreateResult(context, "resolved", resultDetails);
        AppendGameplayEvent(gameplayEvent);
        return rollResult;
    }

    public void RecordStructuredGameLogAction(
        PlayerToken token,
        ActionType actionType,
        int value = 1,
        PlayerToken connectedToken = null,
        string tackleType = "",
        string shotType = "",
        string recoveryType = "",
        string saveType = "")
    {
        Dictionary<string, string> details = CreateDetails(
            ("actionType", actionType.ToString()),
            ("value", value),
            ("tackleType", tackleType),
            ("shotType", shotType),
            ("recoveryType", recoveryType),
            ("saveType", saveType));

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "stats.action",
            actor: token,
            relatedToken: connectedToken,
            sourceHex: token != null ? token.GetCurrentHex() : null,
            targetHex: connectedToken != null ? connectedToken.GetCurrentHex() : null);
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.result = CreateResult(
            actionType.ToString(),
            MapActionOutcome(actionType),
            details);
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordExpectedRecovery(
        PlayerToken token,
        float expectedValue,
        PlayerToken connectedToken = null,
        string recoveryType = "",
        float playerTotal = 0f,
        float teamTotal = 0f)
    {
        Dictionary<string, string> details = CreateDetails(
            ("stat", "xRecovery"),
            ("value", expectedValue),
            ("recoveryType", recoveryType),
            ("playerTotal", playerTotal),
            ("teamTotal", teamTotal),
            ("tackling", token != null ? token.tackling : 0));

        RecordGameplayOutcome(
            "stats.expected_recovery",
            "x_recovery",
            "accumulated",
            actor: token,
            relatedToken: connectedToken,
            sourceHex: token != null ? token.GetCurrentHex() : null,
            targetHex: connectedToken != null ? connectedToken.GetCurrentHex() : null,
            details: details);
    }

    public void RecordGameplayOutcome(
        string kind,
        string action,
        string outcome,
        PlayerToken actor = null,
        PlayerToken relatedToken = null,
        HexCell sourceHex = null,
        HexCell targetHex = null,
        Dictionary<string, string> details = null,
        string preStateHash = null)
    {
        GameplayEvent gameplayEvent = CreateGameplayEvent(
            kind,
            actor: actor,
            relatedToken: relatedToken,
            sourceHex: sourceHex,
            targetHex: targetHex);
        if (gameplayEvent == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(preStateHash))
        {
            gameplayEvent.preStateHash = preStateHash;
        }
        gameplayEvent.result = CreateResult(action, outcome, details);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordGameplaySnapshot(
        string kind,
        string action,
        string outcome,
        PlayerToken actor = null,
        HexCell sourceHex = null,
        Dictionary<string, string> details = null)
    {
        GameplayEvent gameplayEvent = CreateGameplayEvent(
            kind,
            actor: actor,
            sourceHex: sourceHex ?? ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.result = CreateResult(action, outcome, details);
        gameplayEvent.snapshot = BuildGameplaySnapshotSummary(gameplayEvent.sequenceNumber);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordBallMove(HexCell sourceHex, HexCell targetHex, string movementType, int? roll = null)
    {
        if (sourceHex == targetHex)
        {
            return;
        }

        Dictionary<string, string> details = CreateDetails(
            ("movementType", movementType),
            ("roll", roll));

        RecordGameplayOutcome(
            "ball.move",
            "ball_move",
            targetHex == null ? "cleared" : "moved",
            sourceHex: sourceHex,
            targetHex: targetHex,
            details: details);
    }

    public void RecordTokenMove(PlayerToken token, HexCell sourceHex, HexCell targetHex, string movementType)
    {
        if (token == null || sourceHex == targetHex || tokenStepMoveLoggingSuppressionDepth > 0)
        {
            return;
        }

        RecordGameplayOutcome(
            "token.move",
            "token_move",
            targetHex == null ? "removed" : "moved",
            actor: token,
            sourceHex: sourceHex,
            targetHex: targetHex,
            details: CreateDetails(("movementType", movementType)));
    }

    public void RecordTokenPathMove(
        PlayerToken token,
        HexCell sourceHex,
        HexCell targetHex,
        IReadOnlyList<HexCell> path,
        bool isDribble,
        bool carriedBall,
        bool countedForDistance,
        string movementType)
    {
        if (token == null || sourceHex == targetHex || path == null || path.Count == 0)
        {
            return;
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "token.move",
            actor: token,
            sourceHex: sourceHex,
            targetHex: targetHex);
        if (gameplayEvent == null)
        {
            return;
        }

        List<RoomHexCoordinates> pathHexes = new List<RoomHexCoordinates>();
        if (sourceHex != null)
        {
            pathHexes.Add(RoomHexCoordinates.FromHex(sourceHex));
        }

        foreach (HexCell step in path)
        {
            RoomHexCoordinates coordinates = RoomHexCoordinates.FromHex(step);
            if (coordinates == null)
            {
                continue;
            }

            RoomHexCoordinates previous = pathHexes.Count > 0 ? pathHexes[pathHexes.Count - 1] : null;
            if (previous == null || previous.x != coordinates.x || previous.z != coordinates.z)
            {
                pathHexes.Add(coordinates);
            }
        }

        gameplayEvent.movementPath = new GameplayMovementPath
        {
            isDribble = isDribble,
            carriedBall = carriedBall,
            countedForDistance = countedForDistance,
            stepCount = path.Count,
            hexes = pathHexes
        };
        gameplayEvent.result = CreateResult(
            "token_move",
            targetHex == null ? "removed" : "moved",
            CreateDetails(
                ("movementType", movementType),
                ("stepCount", path.Count),
                ("pathHexCount", pathHexes.Count),
                ("isDribble", isDribble),
                ("carriedBall", carriedBall),
                ("countedForDistance", countedForDistance)));
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    public void RecordPossessionChanged(TeamInAttack previousTeamInAttack, TeamInAttack nextTeamInAttack)
    {
        RecordGameplayOutcome(
            "possession.changed",
            "change_possession",
            "changed",
            details: CreateDetails(
                ("fromTeamInAttack", previousTeamInAttack.ToString()),
                ("toTeamInAttack", nextTeamInAttack.ToString())));
    }

    public void RecordPossessionStatus(HexCell ballHex, bool previousAttackHasPossession, bool nextAttackHasPossession)
    {
        if (previousAttackHasPossession == nextAttackHasPossession)
        {
            return;
        }

        RecordGameplayOutcome(
            "possession.status_changed",
            "update_possession_after_ball",
            nextAttackHasPossession ? "attack_retained" : "attack_lost",
            targetHex: ballHex,
            details: CreateDetails(
                ("previousAttackHasPossession", previousAttackHasPossession),
                ("nextAttackHasPossession", nextAttackHasPossession)));
    }

    public void RecordMatchTransition(string transition, string outcome, Dictionary<string, string> details = null, string preStateHash = null)
    {
        RecordGameplayOutcome(
            "match.transition",
            transition,
            outcome,
            sourceHex: ball?.GetCurrentHex(),
            details: details,
            preStateHash: preStateHash);
    }

    public void RecordFinalThirdPhaseStarted(string context, bool bothSides, bool isSecondPhase, string movingTeam, int eligibleTokenCount)
    {
        RecordGameplayOutcome(
            "action.phase",
            "final_third",
            "started",
            sourceHex: ball?.GetCurrentHex(),
            details: CreateDetails(
                ("context", context),
                ("bothSides", bothSides),
                ("isSecondPhase", isSecondPhase),
                ("movingTeam", movingTeam),
                ("eligibleTokenCount", eligibleTokenCount)));
    }

    public void RecordFinalThirdPhaseEnded(string context, bool bothSides, bool isSecondPhase, string movedTokenKeys, string forfeitedTokenKeys = "")
    {
        Dictionary<string, string> details = CreateDetails(
            ("context", context),
            ("bothSides", bothSides),
            ("isSecondPhase", isSecondPhase),
            ("movedTokenKeys", movedTokenKeys),
            ("forfeitedTokenKeys", forfeitedTokenKeys));

        RecordGameplayOutcome(
            "action.phase",
            "final_third",
            "ended",
            sourceHex: ball?.GetCurrentHex(),
            details: details);
        RecordGameplaySnapshot(
            "snapshot.final_third_boundary",
            "final_third",
            "resolved",
            sourceHex: ball?.GetCurrentHex(),
            details: details);
    }

    private void RecordAvailableActions(string reason)
    {
        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "action.availability",
            sourceHex: ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        GameplayAvailableActions availableActions = BuildAvailableActions(reason);
        List<string> availableActionNames = availableActions.actions
            .Where(action => action != null && action.available)
            .Select(action => action.action)
            .ToList();

        gameplayEvent.availableActions = availableActions;
        gameplayEvent.result = CreateResult(
            "available_actions",
            "recorded",
            CreateDetails(
                ("reason", reason),
                ("availableActionCount", availableActionNames.Count),
                ("availableActions", string.Join(",", availableActionNames)),
                ("groundPassMaxDistance", availableActions.groundPassMaxDistance),
                ("difficulty", availableActions.difficulty)));
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    private GameplayAvailableActions BuildAvailableActions(string reason)
    {
        return new GameplayAvailableActions
        {
            reason = reason,
            state = currentState.ToString(),
            difficulty = difficulty_level,
            groundPassMaxDistance = pendingGroundBallDistance,
            actions = new List<GameplayAvailableAction>
            {
                CreateAvailableAction(
                    MatchActionKind.MovementPhase,
                    "M",
                    movementPhaseManager != null && movementPhaseManager.isAvailable,
                    autoCommitOnSelection: false,
                    selectionMode: "movement_selection"),
                CreateAvailableAction(
                    MatchActionKind.StandardPass,
                    "P",
                    groundBallManager != null && groundBallManager.isAvailable,
                    autoCommitOnSelection: difficulty_level >= 3,
                    selectionMode: difficulty_level >= 3 ? "target_selection_commits" : "preview_then_confirm_target",
                    imposedMaxDistance: pendingGroundBallDistance),
                CreateAvailableAction(
                    MatchActionKind.FirstTimePass,
                    "F",
                    firstTimePassManager != null && firstTimePassManager.isAvailable,
                    autoCommitOnSelection: false,
                    selectionMode: difficulty_level >= 3 ? "target_selection_commits" : "preview_then_confirm_target"),
                CreateAvailableAction(
                    MatchActionKind.HighPass,
                    "C",
                    highPassManager != null && highPassManager.isAvailable,
                    autoCommitOnSelection: false,
                    selectionMode: "target_preview"),
                CreateAvailableAction(
                    MatchActionKind.LongBall,
                    "L",
                    longBallManager != null && longBallManager.isAvailable,
                    autoCommitOnSelection: false,
                    selectionMode: "target_preview"),
                CreateAvailableAction(
                    MatchActionKind.Shot,
                    "S",
                    shotManager != null && shotManager.isAvailable,
                    autoCommitOnSelection: false,
                    selectionMode: "target_selection")
            }
        };
    }

    private GameplayAvailableAction CreateAvailableAction(
        MatchActionKind actionKind,
        string key,
        bool available,
        bool autoCommitOnSelection,
        string selectionMode,
        int imposedMaxDistance = 0)
    {
        return new GameplayAvailableAction
        {
            action = FormatActionKind(actionKind),
            key = key,
            available = available,
            autoCommitOnSelection = autoCommitOnSelection,
            selectionMode = selectionMode,
            imposedMaxDistance = imposedMaxDistance
        };
    }

    public void RecordInitialSetupSnapshot()
    {
        if (initialSetupSnapshotRecorded
            || gameData?.runtimeSnapshot != null
            || gameData?.events == null
            || gameData.events.Any(gameplayEvent => gameplayEvent?.kind == "snapshot.initial_state"))
        {
            return;
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "snapshot.initial_state",
            sourceHex: ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        gameplayEvent.result = CreateResult(
            "initial_setup",
            "recorded",
            CreateDetails(("source", "initial_scene_setup")));
        gameplayEvent.snapshot = BuildGameplaySnapshotSummary(gameplayEvent.sequenceNumber);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
        initialSetupSnapshotRecorded = true;
    }

    private void AppendActionBoundarySnapshot(MatchActionKind actionKind)
    {
        if (actionKind == MatchActionKind.None)
        {
            return;
        }

        GameplayEvent gameplayEvent = CreateGameplayEvent(
            "snapshot.action_boundary",
            actor: LastTokenToTouchTheBallOnPurpose,
            sourceHex: ball?.GetCurrentHex());
        if (gameplayEvent == null)
        {
            return;
        }

        string actionName = FormatActionKind(actionKind);
        gameplayEvent.result = CreateResult(
            actionName,
            "resolved",
            CreateDetails(
                ("action", actionName),
                ("actionKind", actionKind.ToString()),
                ("completedActionsThisHalf", completedActionsThisHalf),
                ("totalCompletedActions", totalCompletedActions)));
        gameplayEvent.snapshot = BuildGameplaySnapshotSummary(gameplayEvent.sequenceNumber);
        gameplayEvent.postStateHash = ComputeGameplayStateHash();
        AppendGameplayEvent(gameplayEvent);
    }

    private GameplayEvent CreateGameplayEvent(
        string kind,
        PlayerToken actor = null,
        PlayerToken relatedToken = null,
        HexCell sourceHex = null,
        HexCell targetHex = null)
    {
        EnsureGameplayEventLogInitialized();
        if (!ShouldRecordGameplayEvents)
        {
            return null;
        }

        GameplayEvent gameplayEvent = new GameplayEvent
        {
            sequenceNumber = nextGameplayEventSequence++,
            schemaVersion = GameplayEvent.CurrentSchemaVersion,
            timestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            currentHalf = Mathf.Max(1, currentHalf),
            clockDisplay = GetClockDisplayText().Replace("\n", " | "),
            matchClockSeconds = GetDisplayRegulationSeconds(),
            currentGameState = currentState.ToString(),
            teamInAttack = teamInAttack.ToString(),
            attackHasPossession = attackHasPossession,
            kind = kind,
            actorTokenKey = GetStableTokenKey(actor),
            sourceHex = RoomHexCoordinates.FromHex(sourceHex),
            targetHex = RoomHexCoordinates.FromHex(targetHex),
            instruction = CaptureInstructionSnapshotForEvent(),
            preStateHash = ComputeGameplayStateHash()
        };

        if (relatedToken != null)
        {
            gameplayEvent.relatedTokenKeys.Add(GetStableTokenKey(relatedToken));
        }

        return gameplayEvent;
    }

    private void AppendGameplayEvent(GameplayEvent gameplayEvent)
    {
        if (gameplayEvent == null || !ShouldRecordGameplayEvents)
        {
            return;
        }

        gameData.events.Add(gameplayEvent);
        MarkLiveLogDirty();
    }

    private GameplaySnapshotSummary BuildGameplaySnapshotSummary(int sequenceNumber)
    {
        GameplaySnapshotSummary snapshot = new GameplaySnapshotSummary
        {
            sequenceNumber = sequenceNumber,
            ballHex = RoomHexCoordinates.FromHex(ball?.GetCurrentHex()),
            score = new GameplayScoreSnapshot
            {
                home = gameData?.stats?.homeTeamStats?.totalGoals ?? 0,
                away = gameData?.stats?.awayTeamStats?.totalGoals ?? 0
            },
            currentState = currentState.ToString(),
            possession = teamInAttack.ToString(),
            attackHasPossession = attackHasPossession,
            currentHalf = Mathf.Max(1, currentHalf),
            clockDisplay = GetClockDisplayText().Replace("\n", " | "),
            matchClockSeconds = GetDisplayRegulationSeconds()
        };

        snapshot.tokens = FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .Where(token => token != null)
            .OrderBy(token => token.isHomeTeam ? 0 : 1)
            .ThenBy(token => token.jerseyNumber)
            .Select(token => new GameplayTokenStateSummary
            {
                tokenKey = GetStableTokenKey(token),
                hex = RoomHexCoordinates.FromHex(token.GetCurrentHex()),
                status = BuildTokenStatus(token)
            })
            .ToList();

        return snapshot;
    }

    private string ComputeGameplayStateHash()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("state=").Append(currentState).Append('|');
        builder.Append("team=").Append(teamInAttack).Append('|');
        builder.Append("attackPoss=").Append(attackHasPossession).Append('|');
        builder.Append("half=").Append(currentHalf).Append('|');
        builder.Append("clock=").Append(GetDisplayRegulationSeconds()).Append('|');
        builder.Append("actions=").Append(completedActionsThisHalf).Append('/').Append(totalCompletedActions).Append('|');
        builder.Append("score=")
            .Append(gameData?.stats?.homeTeamStats?.totalGoals ?? 0)
            .Append('-')
            .Append(gameData?.stats?.awayTeamStats?.totalGoals ?? 0)
            .Append('|');
        builder.Append("ball=").Append(FormatHex(ball?.GetCurrentHex())).Append('|');

        foreach (PlayerToken token in FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .Where(token => token != null)
            .OrderBy(token => token.isHomeTeam ? 0 : 1)
            .ThenBy(token => token.jerseyNumber))
        {
            builder.Append(GetStableTokenKey(token))
                .Append('@')
                .Append(FormatHex(token.GetCurrentHex()))
                .Append(':')
                .Append(BuildTokenStatus(token))
                .Append('|');
        }

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return ToLowerHex(hashBytes);
        }
    }

    private static string ToLowerHex(byte[] bytes)
    {
        const string hex = "0123456789abcdef";
        char[] chars = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = hex[bytes[i] >> 4];
            chars[i * 2 + 1] = hex[bytes[i] & 0x0F];
        }

        return new string(chars);
    }

    private static GameplayEventResult CreateResult(
        string action,
        string outcome,
        Dictionary<string, string> details = null)
    {
        return new GameplayEventResult
        {
            action = action,
            outcome = outcome,
            details = details ?? new Dictionary<string, string>()
        };
    }

    private static Dictionary<string, string> CreateDetails(params (string Key, object Value)[] values)
    {
        Dictionary<string, string> details = new Dictionary<string, string>();
        if (values == null)
        {
            return details;
        }

        foreach ((string key, object value) in values)
        {
            AddDetail(details, key, value);
        }

        return details;
    }

    private static List<GameplayChoiceOption> BuildChoiceOptions(params (string Key, string Action, string Label)[] options)
    {
        List<GameplayChoiceOption> choiceOptions = new List<GameplayChoiceOption>();
        if (options == null)
        {
            return choiceOptions;
        }

        foreach ((string key, string action, string label) in options)
        {
            choiceOptions.Add(new GameplayChoiceOption
            {
                key = key,
                action = action,
                label = label
            });
        }

        return choiceOptions;
    }

    private static Dictionary<string, string> MergeDetails(
        Dictionary<string, string> existingDetails,
        params (string Key, object Value)[] values)
    {
        Dictionary<string, string> details = existingDetails != null
            ? new Dictionary<string, string>(existingDetails)
            : new Dictionary<string, string>();

        if (values == null)
        {
            return details;
        }

        foreach ((string key, object value) in values)
        {
            AddDetail(details, key, value);
        }

        return details;
    }

    private static void AddPreviewRelatedTokens(GameplayEvent gameplayEvent, GameplayActionPreview preview)
    {
        if (gameplayEvent == null || preview == null)
        {
            return;
        }

        AddRelatedTokenKey(gameplayEvent, preview.targetTokenKey);
        if (preview.pathInteractions == null)
        {
            return;
        }

        foreach (GameplayPathInteractionPreview interaction in preview.pathInteractions)
        {
            AddRelatedTokenKey(gameplayEvent, interaction?.defenderTokenKey);
        }
    }

    private static void AddRelatedTokenKey(GameplayEvent gameplayEvent, string tokenKey)
    {
        if (gameplayEvent == null
            || string.IsNullOrWhiteSpace(tokenKey)
            || tokenKey == gameplayEvent.actorTokenKey)
        {
            return;
        }

        gameplayEvent.relatedTokenKeys ??= new List<string>();
        if (!gameplayEvent.relatedTokenKeys.Contains(tokenKey))
        {
            gameplayEvent.relatedTokenKeys.Add(tokenKey);
        }
    }

    private static void AddDetail(Dictionary<string, string> details, string key, object value)
    {
        if (details == null || string.IsNullOrWhiteSpace(key) || value == null)
        {
            return;
        }

        string formattedValue = FormatInvariant(value);
        if (!string.IsNullOrWhiteSpace(formattedValue))
        {
            details[key] = formattedValue;
        }
    }

    private static string FormatInvariant(object value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static string FormatTeamSide(bool? isHomeTeam)
    {
        if (!isHomeTeam.HasValue)
        {
            return null;
        }

        return isHomeTeam.Value ? "Home" : "Away";
    }

    private static string BuildKeyChord(KeyPressData keyData)
    {
        StringBuilder builder = new StringBuilder();
        if (keyData.ctrl) builder.Append("Ctrl+");
        if (keyData.alt) builder.Append("Alt+");
        if (keyData.shift) builder.Append("Shift+");
        builder.Append(keyData.key);
        return builder.ToString();
    }

    public static string GetStableTokenKey(PlayerToken token)
    {
        return token == null ? null : BuildTokenKey(token.isHomeTeam, token.jerseyNumber);
    }

    private static string FormatHex(HexCell hex)
    {
        return hex == null
            ? null
            : string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}",
                hex.coordinates.x,
                hex.coordinates.z);
    }

    private static string FormatRoomHex(RoomHexCoordinates hex)
    {
        return hex == null
            ? null
            : string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}",
                hex.x,
                hex.z);
    }

    private string BuildTokenStatus(PlayerToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }

        List<string> statuses = new List<string>();
        statuses.Add(token.isPlaying ? "playing" : "bench");
        bool tokenTeamIsAttackingSide = token.isHomeTeam == (teamInAttack == TeamInAttack.Home);
        statuses.Add(tokenTeamIsAttackingSide ? "attacker" : "defender");
        if (ball != null && token.GetCurrentHex() != null && ball.GetCurrentHex() == token.GetCurrentHex())
        {
            statuses.Add("has_ball");
        }
        if (token.IsGoalKeeper) statuses.Add("gk");
        if (token.isBooked) statuses.Add("booked");
        if (token.isInjured) statuses.Add("injured");
        if (token.isSentOff) statuses.Add("sent_off");
        if (token.requiresSubstitution) statuses.Add("requires_substitution");
        if (token.wasSubbedOn) statuses.Add("subbed_on");
        if (token.wasSubbedOff) statuses.Add("subbed_off");
        return string.Join(",", statuses);
    }

    private static string FormatActionKind(MatchActionKind actionKind)
    {
        return actionKind switch
        {
            MatchActionKind.MovementPhase => "movement",
            MatchActionKind.StandardPass => "standard_pass",
            MatchActionKind.FirstTimePass => "first_time_pass",
            MatchActionKind.HighPass => "high_pass",
            MatchActionKind.LongBall => "long_ball",
            MatchActionKind.Header => "header",
            MatchActionKind.BallControl => "ball_control",
            MatchActionKind.Shot => "shot",
            MatchActionKind.Snapshot => "snapshot",
            _ => "none"
        };
    }

    private static string MapActionOutcome(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.PassCompleted => "completed",
            ActionType.InterceptionSuccess => "success",
            ActionType.ShotOnTarget => "on_target",
            ActionType.ShotBlocked => "blocked",
            ActionType.ShotBlockMade => "block_made",
            ActionType.ShotOffTarget => "off_target",
            ActionType.GoalScored => "goal",
            ActionType.BallRecovery => "recovered",
            ActionType.GroundDuelWon => "won",
            ActionType.AerialChallengeWon => "won",
            ActionType.SaveMade => "save_made",
            ActionType.YellowCardShown => "yellow_card",
            ActionType.RedCardShown => "red_card",
            ActionType.Injured => "injury",
            ActionType.Substituted => "substitution",
            ActionType.CornerWon => "corner_won",
            _ => "recorded"
        };
    }

    // // Define other match-specific variables here (e.g., time, score, teams)
    // private int homeScore = 0;
    // private int awayScore = 0;

    private void Awake()
    {
        Debug.Log("MatchManager Awake() - Starting Initialization");
        Debug.Log($"⚠️ MatchManager Awake() - Entity ID: {GetEntityId()}");

        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
            EnsurePenaltyKickManager();
            // DontDestroyOnLoad(gameObject); // Keep MatchManager persistent
        }
        else
        {
            Debug.LogWarning("⚠️ Duplicate MatchManager detected! Destroying new instance.");
            Destroy(gameObject);
            return;
        }
    }

    public PenaltyKickManager EnsurePenaltyKickManager()
    {
        if (penaltyKickManager == null)
        {
            penaltyKickManager = FindAnyObjectByType<PenaltyKickManager>();
            Debug.Log($"PenaltyKickManager found in scene: {penaltyKickManager != null}");
        }

        if (penaltyKickManager == null)
        {
            penaltyKickManager = gameObject.AddComponent<PenaltyKickManager>();
        }

        penaltyKickManager.Configure(this, hexGrid, ball, movementPhaseManager, shotManager);
        if (movementPhaseManager != null)
        {
            movementPhaseManager.penaltyKickManager = penaltyKickManager;
        }

        return penaltyKickManager;
    }

    public PenaltyShootoutManager EnsurePenaltyShootoutManager()
    {
        PenaltyShootoutManager manager = FindAnyObjectByType<PenaltyShootoutManager>();
        if (manager == null)
        {
            manager = gameObject.AddComponent<PenaltyShootoutManager>();
        }

        manager.Configure(this);
        return manager;
    }

    public void MarkPenaltyShootoutInProgress()
    {
        isMatchComplete = false;
    }

    public void MarkPenaltyShootoutComplete()
    {
        isMatchComplete = true;
    }

    public void MarkNextGoalAsPenalty(bool suppressAssist = true)
    {
        nextGoalIsPenalty = true;
        suppressAssistForNextGoal = suppressAssist;
    }

    private bool ConsumeNextGoalIsPenalty()
    {
        bool value = nextGoalIsPenalty;
        nextGoalIsPenalty = false;
        return value;
    }

    private bool ConsumeSuppressAssistForNextGoal()
    {
        bool value = suppressAssistForNextGoal;
        suppressAssistForNextGoal = false;
        return value;
    }

    IEnumerator Start()
    {
        Debug.Log($"⚠️ MatchManager Start() - Entity ID: {GetEntityId()}");
        LoadGameSettingsFromJson();
        yield return new WaitUntil(() => gameData != null && gameData.rosters != null && gameData.rosters.home.Count > 10 && gameData.rosters.away.Count > 10);
        if (gameData != null && gameData.gameSettings != null)
        {
            ClampLoadedMatchSettings();
            difficulty_level = gameData.gameSettings.playerAssistance;
        }
        if (gameData != null && gameData.gameSettings != null)
        {
            refereeLeniency = int.Parse(gameData.gameSettings.referee[^1].ToString());
        }
        {
            if (ball != null)
            {
                PlayerToken.SetBallReference(ball);
                Debug.Log("Ball reference assigned to PlayerToken class.");
            }
            else
            {
                Debug.LogError("Ball object not found in the scene!");
            }
        }
        // Wait until the grid is fully initialized
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());
        EnsurePenaltyKickManager();
        Debug.Log("⚠️ MatchManager Start() - Checking gameData...");
        if (gameData == null)
        {
            Debug.LogWarning("⚠️ gameData was NULL in Start! Reinitializing...");
            gameData = new GameData();
        }

        if (gameData.stats == null)
        {
            Debug.LogWarning("⚠️ gameData.stats was NULL in Start! Reinitializing...");
            gameData.stats = new Stats();
        }
        InitializeSubstitutionCountsFromStats();

        if (gameData.gameLog == null)
        {
            Debug.LogWarning("⚠️ gameData.gameLog was NULL in Start! Reinitializing...");
            gameData.gameLog = new GameLog(gameData.stats);  // Now `stats` is guaranteed to exist
        }
        else
        {
            gameData.gameLog.RebindStats(gameData.stats);
        }
        EnsureGameplayEventLogInitialized();
        Debug.Log($"🔍 Home Roster: {JsonConvert.SerializeObject(gameData.rosters.home, Formatting.Indented)}");
        Debug.Log($"🔍 Away Roster: {JsonConvert.SerializeObject(gameData.rosters.away, Formatting.Indented)}");
        Debug.Log($"🔍 GameLog: {JsonConvert.SerializeObject(gameData.gameLog, Formatting.Indented)}");
        // Debug.Log($"✅ Final gameLog: {JsonUtility.ToJson(gameData.gameLog, true)}");
        // Initialize the match in the KickOffSetup state
        currentState = GameState.KickOffSetup;
        Debug.Log("Game initialized in KickOffSetup state.");
        // Initialize the attacking team and direction
        teamInAttack = TeamInAttack.Home;  // Home team starts with the ball
        firstHalfKickoffTeam = teamInAttack;
        homeTeamDirection = TeamAttackingDirection.LeftToRight;  // Set home team attacking direction to LeftToRight
        awayTeamDirection = TeamAttackingDirection.RightToLeft;  // Away team will attack in the opposite direction
        ResetMatchClockForNewMatch();
        ResolveClockDependencies();
        if (gameData.runtimeSnapshot != null)
        {
            StartCoroutine(RestoreRuntimeSnapshotWhenReady(gameData.runtimeSnapshot));
        }
    }

    private void InitializeSubstitutionCountsFromStats()
    {
        gameData.stats.homeTeamStats ??= new TeamStats();
        gameData.stats.awayTeamStats ??= new TeamStats();
        homeSubstitutionsUsed = Mathf.Clamp(gameData.stats.homeTeamStats.totalSubstiutions, 0, GetSubstitutionLimit(true));
        awaySubstitutionsUsed = Mathf.Clamp(gameData.stats.awayTeamStats.totalSubstiutions, 0, GetSubstitutionLimit(false));
    }

    private void Update()
    {
        // // Handle state transitions or global inputs, like starting the match
        // if (currentState == GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        // {
        //     StartMatch();
        // }

        // if (currentState == GameState.StandardPassAttempt)
        // {
        //     // Here you could manage inputs like the player selecting a pass target (by clicking a hex)
        //     // You can also trigger transitions between game states based on player actions
        // }
        UpdateMatchClock();
        FlushLiveLogIfDue();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            FlushLiveLogBeforeExit();
        }
    }

    private void OnApplicationQuit()
    {
        FlushLiveLogBeforeExit();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            FlushLiveLogBeforeExit();
        }
    }

    private void FlushLiveLogIfDue()
    {
        if (!liveLogDirty || Time.unscaledTime < nextLiveLogFlushTime)
        {
            return;
        }

        nextLiveLogFlushTime = Time.unscaledTime + LiveLogFlushIntervalSeconds;
        if (!FlushLiveLogNow(out _, out string message))
        {
            LogLiveLogFailure(message);
        }
    }

    private void FlushLiveLogBeforeExit()
    {
        if (liveLogDirty && !FlushLiveLogNow(out _, out string message))
        {
            LogLiveLogFailure(message);
        }
    }

    private void LogLiveLogFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (message == lastLiveLogFailureMessage && Time.unscaledTime < nextLiveLogFailureWarningTime)
        {
            return;
        }

        lastLiveLogFailureMessage = message;
        nextLiveLogFailureWarningTime = Time.unscaledTime + 10f;
        Debug.LogWarning(message);
    }

    private void ClampLoadedMatchSettings()
    {
        if (gameData?.gameSettings == null)
        {
            return;
        }

        gameData.gameSettings.halfDuration = Mathf.Clamp(gameData.gameSettings.halfDuration, 15, 60);
        gameData.gameSettings.numberOfHalfs = Mathf.Clamp(gameData.gameSettings.numberOfHalfs, 1, 2);
        if (!IsStandardTwoHalfMatch() && TieBreakerIncludesExtraTime())
        {
            gameData.gameSettings.tiebreaker = TieBreakerIncludesPenalties() ? "Penalties" : "None";
        }
    }

    private void ResetMatchClockForNewMatch()
    {
        currentHalf = 1;
        currentHalfRegulationSeconds = 0f;
        completedActionsThisHalf = 0;
        totalCompletedActions = 0;
        isClockRunning = false;
        isHalfExpired = false;
        extraActionsDetermined = false;
        extraActionsTotal = 0;
        extraActionsRemaining = 0;
        isWaitingForExtraActionsRoll = false;
        isMatchComplete = false;
        isHalfTimeFlowRunning = false;
        isPauseMenuOpen = false;
        isHalfEndPendingAfterGoalFlow = false;
        pendingHalfGateContinuation = null;
        currentCommittedActionKind = MatchActionKind.None;
        hasUnresolvedCommittedExtraAction = false;
        committedExtraActionNumber = 0;
        pendingShotGoalTimeLabel = string.Empty;
        pendingShotGoalExtraActionNumber = 0;
        extraTimeSubstitutionCreditGranted = false;
    }

    private void ResolveClockDependencies()
    {
        if (finalThirdManager == null)
        {
            finalThirdManager = FindAnyObjectByType<FinalThirdManager>();
        }

        if (goalFlowManager == null)
        {
            goalFlowManager = FindAnyObjectByType<GoalFlowManager>();
        }

        if (kickoffManager == null)
        {
            kickoffManager = FindAnyObjectByType<KickoffManager>();
        }

        if (freeKickManager == null)
        {
            freeKickManager = FindAnyObjectByType<FreeKickManager>();
        }

        if (helperFunctions == null)
        {
            helperFunctions = FindAnyObjectByType<HelperFunctions>();
        }
    }

    private void UpdateMatchClock()
    {
        if (!CanAdvanceMatchClock())
        {
            return;
        }

        float multiplier = useFastClock ? Mathf.Max(1f, fastClockMultiplier) : 1f;
        currentHalfRegulationSeconds += Time.deltaTime * multiplier;
        float halfLimitSeconds = GetHalfDurationSeconds();
        if (currentHalfRegulationSeconds >= halfLimitSeconds)
        {
            currentHalfRegulationSeconds = halfLimitSeconds;
            isHalfExpired = true;
            isClockRunning = false;
            Debug.Log($"Half {currentHalf} regulation time expired. Current action may finish before stoppage actions are determined.");
        }
    }

    private bool CanAdvanceMatchClock()
    {
        return isClockRunning
            && !isHalfExpired
            && !isMatchComplete
            && !isHalfTimeFlowRunning
            && !isPauseMenuOpen
            && !IsClockPausedForCurrentState();
    }

    private bool IsClockPausedForCurrentState()
    {
        if (goalFlowManager != null && goalFlowManager.isActivated)
        {
            return true;
        }

        return currentState == GameState.KickOffSetup
            || currentState == GameState.PostGoalKickOffSetup
            || currentState == GameState.KickOffTakerSelection
            || currentState == GameState.WaitingForThrowInTaker
            || currentState == GameState.GoalKick
            || currentState == GameState.HalfTime
            || currentState == GameState.MatchEnded
            || IsFreeKickClockPaused(currentState)
            || currentState.ToString().StartsWith("Penalty", StringComparison.Ordinal);
    }

    private bool IsFreeKickClockPaused(GameState state)
    {
        if (!IsFreeKickPreparationState(state))
        {
            return false;
        }

        if (freeKickManager == null)
        {
            freeKickManager = FindAnyObjectByType<FreeKickManager>();
        }

        return freeKickManager == null || freeKickManager.ShouldPauseMatchClockForState(state);
    }

    private static bool IsFreeKickPreparationState(GameState state)
    {
        return state == GameState.FreeKickKickerSelect
            || state == GameState.FreeKickAttGK
            || state == GameState.FreeKickDefGK1
            || state == GameState.FreeKickAtt1
            || state == GameState.FreeKickAtt2
            || state == GameState.FreeKickAtt3
            || state == GameState.FreeKickDef1
            || state == GameState.FreeKickDef2
            || state == GameState.FreeKickDef3
            || state == GameState.FreeKickDefGK2
            || state == GameState.FreeKickAttMovement3
            || state == GameState.FreeKickDefMovement3;
    }

    public void PauseMatchClockForSetPiecePrep()
    {
        isClockRunning = false;
    }

    public void ResumeMatchClockForLivePlay()
    {
        if (!isHalfExpired && !isMatchComplete)
        {
            isClockRunning = true;
        }
    }

    public void SetPauseMenuOpen(bool isOpen)
    {
        isPauseMenuOpen = isOpen;
    }

    private float GetHalfDurationSeconds()
    {
        if (IsExtraTimeHalf(currentHalf))
        {
            return ExtraTimeHalfDurationMinutes * 60f;
        }

        int durationMinutes = gameData?.gameSettings != null
            ? Mathf.Clamp(gameData.gameSettings.halfDuration, 15, 60)
            : StandardHalfDurationMinutes;
        return durationMinutes * 60f;
    }

    private int GetConfiguredNumberOfHalfs()
    {
        return gameData?.gameSettings != null
            ? Mathf.Clamp(gameData.gameSettings.numberOfHalfs, 1, 2)
            : 2;
    }

    private int GetCurrentMatchHalfLimit()
    {
        int normalHalfs = GetConfiguredNumberOfHalfs();
        if (normalHalfs == StandardNumberOfHalfs && currentHalf > normalHalfs)
        {
            return normalHalfs + ExtraTimeHalfs;
        }

        return normalHalfs;
    }

    private bool IsStandardTwoHalfMatch()
    {
        return gameData?.gameSettings != null
            && Mathf.Clamp(gameData.gameSettings.halfDuration, 15, 60) == StandardHalfDurationMinutes
            && Mathf.Clamp(gameData.gameSettings.numberOfHalfs, 1, 2) == StandardNumberOfHalfs;
    }

    private bool TieBreakerIncludesExtraTime()
    {
        return (gameData?.gameSettings?.tiebreaker ?? string.Empty).IndexOf("Extra Time", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TieBreakerIncludesPenalties()
    {
        return (gameData?.gameSettings?.tiebreaker ?? string.Empty).IndexOf("Penalties", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsScoreTied()
    {
        return (gameData?.stats?.homeTeamStats?.totalGoals ?? 0) == (gameData?.stats?.awayTeamStats?.totalGoals ?? 0);
    }

    private bool IsExtraTimeHalf(int half)
    {
        return IsStandardTwoHalfMatch() && half > StandardNumberOfHalfs;
    }

    private int GetDisplayRegulationSeconds()
    {
        int priorHalfSeconds;
        if (IsExtraTimeHalf(currentHalf))
        {
            int extraHalfIndex = currentHalf - StandardNumberOfHalfs - 1;
            priorHalfSeconds = (StandardNumberOfHalfs * StandardHalfDurationMinutes * 60)
                + (Mathf.Max(0, extraHalfIndex) * ExtraTimeHalfDurationMinutes * 60);
        }
        else
        {
            int halfDurationSeconds = Mathf.RoundToInt(GetHalfDurationSeconds());
            priorHalfSeconds = (currentHalf - 1) * halfDurationSeconds;
        }

        return priorHalfSeconds + Mathf.FloorToInt(currentHalfRegulationSeconds);
    }

    private int GetCurrentHalfEndMinute()
    {
        if (IsExtraTimeHalf(currentHalf))
        {
            return StandardNumberOfHalfs * StandardHalfDurationMinutes
                + ((currentHalf - StandardNumberOfHalfs) * ExtraTimeHalfDurationMinutes);
        }

        return Mathf.RoundToInt((currentHalf * GetHalfDurationSeconds()) / 60f);
    }

    public string GetClockDisplayText()
    {
        string regulation = FormatClockSeconds(GetDisplayRegulationSeconds());
        if (!isHalfExpired)
        {
            return regulation;
        }

        if (!extraActionsDetermined)
        {
            return $"{regulation}\n00:00 (+?)";
        }

        int playedExtraActions = Mathf.Clamp(extraActionsTotal - extraActionsRemaining, 0, extraActionsTotal);
        return $"{regulation}\n0{playedExtraActions}:00 (+{extraActionsTotal})";
    }

    private static string FormatClockSeconds(int totalSeconds)
    {
        int minutes = Mathf.Max(0, totalSeconds) / 60;
        int seconds = Mathf.Max(0, totalSeconds) % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    public int GetCurrentGoalMinute()
    {
        if (isHalfExpired && extraActionsDetermined)
        {
            int halfBaseMinute = GetCurrentHalfEndMinute();
            if (pendingShotGoalExtraActionNumber > 0)
            {
                return halfBaseMinute + pendingShotGoalExtraActionNumber;
            }

            int extraActionNumber = ResolveCurrentGoalExtraActionNumber();
            return halfBaseMinute + Mathf.Max(0, extraActionNumber);
        }

        return Mathf.Max(1, Mathf.CeilToInt(GetDisplayRegulationSeconds() / 60f));
    }

    public string GetCurrentGoalMinuteLabel()
    {
        if (!string.IsNullOrWhiteSpace(pendingShotGoalTimeLabel))
        {
            return pendingShotGoalTimeLabel;
        }

        if (isHalfExpired && extraActionsDetermined)
        {
            int halfBaseMinute = GetCurrentHalfEndMinute();
            int extraActionNumber = ResolveCurrentGoalExtraActionNumber();
            return $"{halfBaseMinute}'+{Mathf.Max(1, extraActionNumber)}'";
        }

        return $"{GetCurrentGoalMinute()}'";
    }

    private int ResolveCurrentGoalExtraActionNumber()
    {
        if (committedExtraActionNumber > 0)
        {
            return committedExtraActionNumber;
        }

        return Mathf.Clamp(extraActionsTotal - extraActionsRemaining + 1, 1, Mathf.Max(1, extraActionsTotal));
    }

    // public void PrintGameLog()
    // {
    //     Debug.Log("📜 Game Log Contents:");
    //     foreach (string entry in gameData.gameLog)
    //     {
    //         Debug.Log(entry);
    //     }
    // }
    // Call this when players are fully instantiated
    
    public void NotifyPlayersInstantiated()
    {
        if (OnPlayersInstantiated != null)
        {
            OnPlayersInstantiated.Invoke();
        }
    }

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        bool hasRollOverride = RollInputOverride.TryParse(keyData, out RollInputOverride rollOverride);
        if (isWaitingForExtraActionsRoll && (keyData.key == KeyCode.R || hasRollOverride))
        {
            DetermineExtraActions(hasRollOverride ? (RollInputOverride?)rollOverride : null);
            keyData.Consume(nameof(MatchManager));
            return;
        }

        if (currentState == GameState.KickOffSetup && keyData.key == KeyCode.Space)
        {
            StartMatch();
            keyData.Consume(nameof(MatchManager));
        }
        else if (currentState == GameState.GoalKick
            && pendingGoalKickRestartTaker != null
            && keyData.key == KeyCode.K)
        {
            TriggerGoalkeeperKick();
            keyData.Consume(nameof(MatchManager));
        }
    }

    // Example method to start the match
    public void StartMatch()
    {
        HexCell kickoffHex = ball != null ? ball.GetCurrentHex() : null;
        PlayerToken kickoffToken = kickoffHex != null ? kickoffHex.GetOccupyingToken() : null;
        if (kickoffHex == null)
        {
            Debug.LogWarning("Cannot start match because the ball is not on a hex yet.");
            return;
        }

        if (kickoffToken == null)
        {
            Debug.LogWarning($"Cannot start match because no token is on the ball hex {kickoffHex.coordinates}.");
            return;
        }

        string preTransitionStateHash = ComputeGameplayStateHash();
        currentState = GameState.KickoffBlown;
        OfferStandardGroundBallPass();
        groundBallManager.isAvailable = true;
        highPassManager.isAvailable = true;
        longBallManager.isAvailable = true;
        LastTokenToTouchTheBallOnPurpose = kickoffToken;
        firstHalfKickoffTeam = teamInAttack;
        isClockRunning = true;
        RefreshAerialTargetPrecomputations();
        RecordMatchTransition(
            "match_started",
            "started",
            CreateDetails(
                ("kickoffTokenKey", GetStableTokenKey(kickoffToken)),
                ("kickoffHex", FormatHex(kickoffHex))),
            preStateHash: preTransitionStateHash);
        RecordAvailableActions("initial_kickoff");
        // Start the timer or wait for the next Action to be called to start it.
        Debug.Log("Match Kicked Off. Awaiting for Attacking Team Press [P] to start the Standard Pass Attempt, and the timer.");
    }

    public void SwitchSides()
    {
        // Swap the attacking directions for both teams
        if (homeTeamDirection == TeamAttackingDirection.LeftToRight)
        {
            homeTeamDirection = TeamAttackingDirection.RightToLeft;
            awayTeamDirection = TeamAttackingDirection.LeftToRight;
        }
        else
        {
            homeTeamDirection = TeamAttackingDirection.LeftToRight;
            awayTeamDirection = TeamAttackingDirection.RightToLeft;
        }

        // Log the switch for debugging
        Debug.Log("Sides switched. Home team is now attacking " + homeTeamDirection + " and away team is attacking " + awayTeamDirection);
    }

    public void ChangePossession()
    {
        TeamInAttack previousTeamInAttack = teamInAttack;
        // Switch the team in attack
        if (teamInAttack == TeamInAttack.Home)
        {
            teamInAttack = TeamInAttack.Away;
        }
        else
        {
            teamInAttack = TeamInAttack.Home;
        }
        // Loop through all hexes and swap attacker/defender status
        foreach (HexCell hex in hexGrid.cells)
        {
            // Swap isAttackOccupied and isDefenseOccupied
            bool temp = hex.isAttackOccupied;
            hex.isAttackOccupied = hex.isDefenseOccupied;
            hex.isDefenseOccupied = temp;

            // Update the highlights during development
            if (hex.isAttackOccupied)
            {
                hex.HighlightHex("isAttackOccupied");  // Use a distinct color for attackers
            }
            else if (hex.isDefenseOccupied)
            {
                hex.HighlightHex("isDefenseOccupied");  // Use a distinct color for defenders
            }
            else
            {
                hex.ResetHighlight();  // Reset to normal if neither
            }
        }
        Debug.Log($"Possession changed! {teamInAttack} now is the Attacking Team.");
        // Now update the PlayerTokens to reflect the new possession
        UpdatePlayerTokensAfterPossessionChange();
        RecordPossessionChanged(previousTeamInAttack, teamInAttack);

    }

    public void UpdatePossessionAfterPass(HexCell ballHex)
    {
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();
        bool previousAttackHasPossession = attackHasPossession;

        // Check if the ball is on an attacker's hex
        if (attackerHexes.Contains(ballHex))
        {
            attackHasPossession = true;
            // Debug.Log("Attacking team retains possession.");
        }
        else
        {
            attackHasPossession = false;
            // Debug.Log("Attacking team lost possession.");
        }
        Debug.Log($"Attacking team has possession: {attackHasPossession}.");
        RecordPossessionStatus(ballHex, previousAttackHasPossession, attackHasPossession);
    }

    public void OfferShortGroundBallPass()
    {
        pendingGroundBallDistance = ShortGroundBallDistance;
        ApplyPendingGroundBallDistance();
    }

    public void OfferStandardGroundBallPass()
    {
        pendingGroundBallDistance = StandardGroundBallDistance;
        ApplyPendingGroundBallDistance();
    }

    private void ResetPendingGroundBallOffer()
    {
        pendingGroundBallDistance = StandardGroundBallDistance;
    }

    private void ApplyPendingGroundBallDistance()
    {
        if (groundBallManager != null)
        {
            groundBallManager.imposedDistance = pendingGroundBallDistance;
        }
    }

    public void SetHangingPass(string passType, PlayerToken excludedCollector = null)
    {
        hangingPassType = passType;
        hangingPassExcludedCollector = excludedCollector;
    }

    public void ClearHangingPass()
    {
        hangingPassType = null;
        hangingPassExcludedCollector = null;
    }

    public void MarkSetPieceTakerForNextTouchExclusion(PlayerToken taker)
    {
        setPieceTakerExcludedFromNextTouch = taker;
        if (taker != null)
        {
            Debug.Log($"{taker.name} took the set piece and cannot be the next player to touch the ball.");
        }
    }

    public void ClearSetPieceTakerNextTouchExclusion(PlayerToken tokenWhoTouched = null)
    {
        if (setPieceTakerExcludedFromNextTouch == null)
        {
            return;
        }

        if (tokenWhoTouched == null || tokenWhoTouched != setPieceTakerExcludedFromNextTouch)
        {
            setPieceTakerExcludedFromNextTouch = null;
        }
    }

    public bool CanTokenCollectHangingPass(PlayerToken token)
    {
        return token != null
            && setPieceTakerExcludedFromNextTouch != token
            && (string.IsNullOrEmpty(hangingPassType) || hangingPassExcludedCollector != token);
    }

    public void MarkNextBallCollectionToClearPrevious()
    {
        // A loose ball in space breaks the purposeful-touch chain when it is next collected.
        clearPreviousOnNextBallCollection = true;
    }

    public void ClearPendingLooseBallCollectionReset()
    {
        clearPreviousOnNextBallCollection = false;
    }

    public void ClearLastTokenChain()
    {
        LastTokenToTouchTheBallOnPurpose = null;
        PreviousTokenToTouchTheBallOnPurpose = null;
        ClearSetPieceTakerNextTouchExclusion();
    }

    public void SetLastTokenFromLooseBall(PlayerToken inputToken)
    {
        // Loose-ball contact is not a purposeful pass, so the new holder starts a fresh chain.
        ClearLastTokenChain();
        SetLastToken(inputToken);
        clearPreviousOnNextBallCollection = false;
    }

    public void ApplyBallCollectionOwnership(PlayerToken inputToken)
    {
        if (clearPreviousOnNextBallCollection)
        {
            SetLastTokenFromLooseBall(inputToken);
            return;
        }

        SetLastToken(inputToken);
    }

    public void ResolveActionBeforeFinalThird(
        MatchActionKind actionKind,
        Action continuation,
        bool consumesExtraAction = true,
        bool allowContinuationAfterFinalExtraAction = false)
    {
        RecordResolvedAction(actionKind, consumesExtraAction);
        Action continuationWithSnapshot = () =>
        {
            continuation?.Invoke();
            AppendActionBoundarySnapshot(actionKind);
        };

        if (isHalfExpired && !extraActionsDetermined)
        {
            pendingHalfGateContinuation = continuationWithSnapshot;
            EnterExtraActionsRollPrompt();
            return;
        }

        if (ShouldWhistleForHalfEnd())
        {
            if (allowContinuationAfterFinalExtraAction && continuation != null)
            {
                continuationWithSnapshot.Invoke();
                return;
            }

            BeginHalfEndFlow();
            AppendActionBoundarySnapshot(actionKind);
            return;
        }

        continuationWithSnapshot.Invoke();
    }

    public void RecordResolvedAction(MatchActionKind actionKind, bool consumesExtraAction = true)
    {
        if (actionKind == MatchActionKind.None || !consumesExtraAction)
        {
            return;
        }

        completedActionsThisHalf++;
        totalCompletedActions++;

        if (isHalfExpired && extraActionsDetermined && extraActionsRemaining > 0)
        {
            extraActionsRemaining = Mathf.Max(0, extraActionsRemaining - 1);
        }

        if (actionKind == currentCommittedActionKind)
        {
            hasUnresolvedCommittedExtraAction = false;
            committedExtraActionNumber = 0;
            currentCommittedActionKind = MatchActionKind.None;
        }
    }

    public void RecordSnapshotBallStarted()
    {
        int snapshotExtraActionNumber = ResolveSnapshotExtraActionNumber();
        RecordResolvedAction(MatchActionKind.Snapshot);
        AppendActionBoundarySnapshot(MatchActionKind.Snapshot);
        if (isHalfExpired && extraActionsDetermined)
        {
            int halfBaseMinute = GetCurrentHalfEndMinute();
            pendingShotGoalTimeLabel = $"{halfBaseMinute}'+{snapshotExtraActionNumber}'";
            pendingShotGoalExtraActionNumber = snapshotExtraActionNumber;
        }
    }

    public void ClearPendingShotGoalTimeLabel()
    {
        pendingShotGoalTimeLabel = string.Empty;
        pendingShotGoalExtraActionNumber = 0;
    }

    private int ResolveSnapshotExtraActionNumber()
    {
        if (!isHalfExpired || !extraActionsDetermined)
        {
            return 0;
        }

        if (hasUnresolvedCommittedExtraAction && currentCommittedActionKind == MatchActionKind.MovementPhase)
        {
            return Mathf.Clamp(committedExtraActionNumber + 1, 1, Mathf.Max(1, extraActionsTotal));
        }

        return ResolveCurrentGoalExtraActionNumber();
    }

    private bool ShouldWhistleForHalfEnd()
    {
        return isHalfExpired && extraActionsDetermined && extraActionsRemaining <= 0;
    }

    private void EnterExtraActionsRollPrompt()
    {
        isWaitingForExtraActionsRoll = true;
        if (movementPhaseManager != null) movementPhaseManager.isAvailable = false;
        if (groundBallManager != null) groundBallManager.isAvailable = false;
        if (firstTimePassManager != null) firstTimePassManager.isAvailable = false;
        if (highPassManager != null) highPassManager.isAvailable = false;
        if (longBallManager != null) longBallManager.isAvailable = false;
        if (shotManager != null) shotManager.isAvailable = false;
        Debug.Log($"Half {currentHalf} regulation time is up. Attacking team rolls a normal D6 for stoppage actions. Press [R].");
    }

    private void DetermineExtraActions(RollInputOverride? rollOverride = null)
    {
        GameplayDiceRollResult diceRoll = ResolveGameplayDiceRoll(
            "stoppage_extra_actions",
            rollOverride,
            actor: LastTokenToTouchTheBallOnPurpose,
            sourceHex: ball?.GetCurrentHex(),
            jackpotEnabled: false,
            details: CreateDetails(("refereeLeniency", refereeLeniency)));
        int roll = diceRoll.roll;
        extraActionsTotal = Mathf.Min(refereeLeniency + roll, 7);
        extraActionsRemaining = extraActionsTotal;
        extraActionsDetermined = true;
        isWaitingForExtraActionsRoll = false;
        RecordGameplayOutcome(
            "clock.extra_actions",
            "stoppage_actions_roll",
            "determined",
            details: CreateDetails(
                ("roll", roll),
                ("refereeLeniency", refereeLeniency),
                ("extraActionsTotal", extraActionsTotal)));
        Debug.Log($"Stoppage actions determined: referee leniency {refereeLeniency} + roll {roll} = {refereeLeniency + roll}, capped to {extraActionsTotal}.");

        Action continuation = pendingHalfGateContinuation;
        pendingHalfGateContinuation = null;
        if (extraActionsRemaining <= 0)
        {
            BeginHalfEndFlow();
            return;
        }

        continuation?.Invoke();
    }

    public bool TryEnterExtraActionsRollBeforeRestart(Action restartContinuation)
    {
        if (!isHalfExpired || extraActionsDetermined || isMatchComplete)
        {
            return false;
        }

        pendingHalfGateContinuation = restartContinuation;
        EnterExtraActionsRollPrompt();
        Debug.Log("Post-regulation goal restart is waiting for stoppage actions roll before kick-off setup.");
        return true;
    }

    private void BeginHalfEndFlow()
    {
        pendingHalfGateContinuation = null;
        isWaitingForExtraActionsRoll = false;
        isClockRunning = false;
        hasUnresolvedCommittedExtraAction = false;
        committedExtraActionNumber = 0;
        currentCommittedActionKind = MatchActionKind.None;
        ClearAvailableActionsForHalfBreak();

        if (TryDeferHalfEndUntilGoalCelebration())
        {
            return;
        }

        CompleteHalfEndFlow();
    }

    private bool TryDeferHalfEndUntilGoalCelebration()
    {
        ResolveClockDependencies();
        if (goalFlowManager == null || !goalFlowManager.isActivated)
        {
            return false;
        }

        if (isHalfEndPendingAfterGoalFlow)
        {
            return true;
        }

        isHalfEndPendingAfterGoalFlow = true;
        bool matchWillEnd = currentHalf >= GetCurrentMatchHalfLimit()
            || (currentHalf == GetConfiguredNumberOfHalfs() && !ShouldStartExtraTime());
        goalFlowManager.CompleteAfterCelebrationWithoutPostGoalReset(CompleteDeferredHalfEndAfterGoal);
        Debug.Log(matchWillEnd
            ? "Full-time is pending after the goal celebration. Suppressing post-goal reset."
            : "Half-time is pending after the goal celebration. Suppressing post-goal reset and waiting for half-time reset.");
        return true;
    }

    private void CompleteDeferredHalfEndAfterGoal()
    {
        isHalfEndPendingAfterGoalFlow = false;
        CompleteHalfEndFlow();
    }

    private void CompleteHalfEndFlow()
    {
        if (currentHalf == GetConfiguredNumberOfHalfs() && ShouldStartExtraTime())
        {
            GrantExtraTimeSubstitutionCreditIfNeeded();
            StartCoroutine(RunHalfTimeFlow());
            return;
        }

        if (currentHalf >= GetCurrentMatchHalfLimit())
        {
            CompleteMatchAfterFinalHalf();
            return;
        }

        StartCoroutine(RunHalfTimeFlow());
    }

    private bool ShouldStartExtraTime()
    {
        return IsStandardTwoHalfMatch() && IsScoreTied() && TieBreakerIncludesExtraTime();
    }

    private void GrantExtraTimeSubstitutionCreditIfNeeded()
    {
        if (extraTimeSubstitutionCreditGranted)
        {
            return;
        }

        extraTimeSubstitutionCreditGranted = true;
        OnSubstitutionStateChanged?.Invoke();
        Debug.Log("Extra time reached. Each team receives one additional substitution.");
        RecordGameplayOutcome(
            "substitution.extra_time_credit",
            "substitution",
            "granted",
            details: CreateDetails(
                ("homeLimit", GetSubstitutionLimit(true)),
                ("awayLimit", GetSubstitutionLimit(false)),
                ("homeRemaining", GetSubstitutionsRemaining(true)),
                ("awayRemaining", GetSubstitutionsRemaining(false))));
    }

    private void CompleteMatchAfterFinalHalf()
    {
        PenaltyShootoutManager existingShootoutManager = FindAnyObjectByType<PenaltyShootoutManager>();
        if (existingShootoutManager != null && existingShootoutManager.IsOrderSelectionOrShootoutStarted)
        {
            Debug.Log("Ignoring duplicate final whistle because penalty shootout flow is already active.");
            return;
        }

        currentState = GameState.MatchEnded;
        isMatchComplete = true;
        isPauseMenuOpen = false;
        RecordMatchTransition(
            IsExtraTimeHalf(currentHalf) ? "extra_time_full_time" : "full_time",
            "match_ended",
            CreateDetails(
                ("completedActionsTotal", totalCompletedActions),
                ("completedHalf", currentHalf),
                ("scoreTied", IsScoreTied()),
                ("tiebreaker", gameData?.gameSettings?.tiebreaker ?? string.Empty)));

        if (IsScoreTied() && TieBreakerIncludesPenalties())
        {
            Debug.Log("Final whistle with tied score. Opening penalty shootout order selection.");
            EnsurePenaltyShootoutManager().ShowOrderSelection();
            return;
        }

        Debug.Log(IsExtraTimeHalf(currentHalf) ? "Extra-time full-time whistle. Match ended." : "Full-time whistle. Match ended.");
        EndGamePanelManager.ShowMatchEndedPanel();
    }

    private void ClearAvailableActionsForHalfBreak()
    {
        if (movementPhaseManager != null) movementPhaseManager.isAvailable = false;
        if (groundBallManager != null) groundBallManager.isAvailable = false;
        if (firstTimePassManager != null) firstTimePassManager.isAvailable = false;
        if (highPassManager != null) highPassManager.isAvailable = false;
        if (longBallManager != null) longBallManager.isAvailable = false;
        if (shotManager != null) shotManager.isAvailable = false;
        isFTPAvailable = false;
    }

    private IEnumerator RunHalfTimeFlow()
    {
        isHalfTimeFlowRunning = true;
        currentState = GameState.HalfTime;
        RecordMatchTransition(
            "half_time",
            "started",
            CreateDetails(("completedHalf", currentHalf)));
        Debug.Log("Half-time whistle. Switching sides and resetting teams for the next kick-off.");
        ResolveClockDependencies();
        SwitchSides();
        currentHalf++;
        currentHalfRegulationSeconds = 0f;
        completedActionsThisHalf = 0;
        isHalfExpired = false;
        extraActionsDetermined = false;
        extraActionsTotal = 0;
        extraActionsRemaining = 0;
        pendingShotGoalTimeLabel = string.Empty;
        pendingShotGoalExtraActionNumber = 0;

        teamInAttack = currentHalf % 2 == 0
            ? (firstHalfKickoffTeam == TeamInAttack.Home ? TeamInAttack.Away : TeamInAttack.Home)
            : firstHalfKickoffTeam;
        attackHasPossession = true;
        ClearLastTokenChain();

        if (goalFlowManager != null)
        {
            yield return StartCoroutine(goalFlowManager.MoveTeamsToHalfTimeResetFormation(teamInAttack == TeamInAttack.Home));
        }
        else
        {
            Debug.LogWarning("Half-time reset could not move teams because GoalFlowManager is missing.");
        }

        PlaceBallOnKickoffHex();
        currentState = GameState.PostGoalKickOffSetup;
        isClockRunning = true;
        kickoffManager?.StartPostGoalKickoffSetupPhase();
        isHalfTimeFlowRunning = false;
        RecordMatchTransition(
            "half_time",
            "completed",
            CreateDetails(("newHalf", currentHalf)));
    }

    private void PlaceBallOnKickoffHex()
    {
        if (hexGrid == null || ball == null)
        {
            Debug.LogError("Cannot place ball for kick-off because MatchManager is missing HexGrid or Ball.");
            return;
        }

        HexCell kickoffHex = hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0));
        if (kickoffHex == null)
        {
            Debug.LogError("Cannot place ball for kick-off because hex (0,0) was not found.");
            return;
        }

        ball.PlaceAtCell(kickoffHex);
        ClearGoalKickRestartTaker();
    }

    public void ClearGoalKickRestartTaker()
    {
        pendingGoalKickRestartTaker = null;
    }

    public bool IsFinalExtraMovementPhaseSnapshotSuppressed()
    {
        return isHalfExpired
            && extraActionsDetermined
            && extraActionsRemaining <= 1
            && currentCommittedActionKind == MatchActionKind.MovementPhase
            && hasUnresolvedCommittedExtraAction
            && movementPhaseManager != null
            && movementPhaseManager.isActivated
            && movementPhaseManager.isCommitted;
    }

    // Method to trigger the standard pass attempt mode (on key press, like "P")
    public void TriggerStandardPass(PlayerToken pendingSetPieceTaker = null)
    {
        RecordActionSelection(
            pendingSetPieceTaker != null ? "set_piece_standard_pass" : "standard_pass",
            pendingSetPieceTaker ?? LastTokenToTouchTheBallOnPurpose,
            ball?.GetCurrentHex());
        bool preserveAerialPrecompute = ShouldPreserveAerialTargetPrecomputeDuringPreview();
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass(preserveTargetPrecompute: preserveAerialPrecompute);
        longBallManager.CleanUpLongBall(preserveTargetPrecompute: preserveAerialPrecompute);
        RefreshAvailableActions();
        ApplyPendingGroundBallDistance();
        PlayerToken setPieceTaker = pendingSetPieceTaker ?? pendingGoalKickRestartTaker;
        if (setPieceTaker != null)
        {
            groundBallManager.SetPendingSetPieceTakerForCommit(setPieceTaker);
        }
        groundBallManager.ActivateGroundBall();
    }

    public void TriggerQuickThrowPass()
    {
        RecordActionSelection("quick_throw", LastTokenToTouchTheBallOnPurpose, ball?.GetCurrentHex());
        bool preserveAerialPrecompute = ShouldPreserveAerialTargetPrecomputeDuringPreview();
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass(preserveTargetPrecompute: preserveAerialPrecompute);
        longBallManager.CleanUpLongBall(preserveTargetPrecompute: preserveAerialPrecompute);
        currentState = GameState.QuickThrow;
        ApplyPendingGroundBallDistance();
        groundBallManager.ActivateGroundBall(true);
        groundBallManager.CommitToThisAction();
    }

    public void TriggerGoalkeeperKick(PlayerToken explicitGoalkeeper = null, bool commitImmediately = false)
    {
        PlayerToken gkToken = explicitGoalkeeper ?? pendingGoalKickRestartTaker ?? ball.GetCurrentHex()?.GetOccupyingToken();
        if (gkToken == null)
        {
            Debug.LogError("Cannot take a Goalkeeper Kick because no goalkeeper restart taker is available.");
            return;
        }

        RecordActionSelection(
            "goalkeeper_kick",
            gkToken,
            gkToken.GetCurrentHex(),
            CreateDetails(("commitImmediately", commitImmediately)));
        bool preserveAerialPrecompute = ShouldPreserveAerialTargetPrecomputeDuringPreview();
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass(preserveTargetPrecompute: preserveAerialPrecompute);
        longBallManager.CleanUpLongBall(preserveTargetPrecompute: preserveAerialPrecompute);
        RefreshAvailableActions();
        ClearLastTokenChain();
        ForceGoalKickRestartTakerAsLastToken(gkToken);
        if (gkToken.GetCurrentHex() != null)
        {
            ball.PlaceAtCell(gkToken.GetCurrentHex());
        }
        highPassManager.SetPendingSetPieceTakerForCommit(gkToken);
        highPassManager.ActivateGoalkeeperKick(commitImmediately);
    }

    public void TriggerMovement()
    {
        RecordActionSelection("movement", LastTokenToTouchTheBallOnPurpose, ball?.GetCurrentHex());
        // All these resets are in case it is not comitted
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass();
        longBallManager.CleanUpLongBall();
        RefreshAvailableActions();
        movementPhaseManager.ActivateMovementPhase();
        if (difficulty_level == 3)
        {
            movementPhaseManager.CommitToAction();
        }
    }

    public void TriggerHighPass(bool isCornerKick = false)
    {
        RecordActionSelection(
            isCornerKick ? "corner" : "high_pass",
            LastTokenToTouchTheBallOnPurpose,
            ball?.GetCurrentHex(),
            CreateDetails(("isCornerKick", isCornerKick)));
        bool preserveAerialPrecompute = ShouldPreserveAerialTargetPrecomputeDuringPreview();
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass(preserveTargetPrecompute: preserveAerialPrecompute);
        longBallManager.CleanUpLongBall(preserveTargetPrecompute: preserveAerialPrecompute);
        RefreshAvailableActions();
        highPassManager.isCornerKick = isCornerKick;
        highPassManager.ActivateHighPass();
    }
    
    public void TriggerLongPass()
    {
        RecordActionSelection("long_ball", LastTokenToTouchTheBallOnPurpose, ball?.GetCurrentHex());
        bool preserveAerialPrecompute = ShouldPreserveAerialTargetPrecomputeDuringPreview();
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass(preserveTargetPrecompute: preserveAerialPrecompute);
        longBallManager.CleanUpLongBall(preserveTargetPrecompute: preserveAerialPrecompute);
        RefreshAvailableActions();
        longBallManager.ActivateLongBall();
    }

    public void TriggerFTP()
    {
        RecordActionSelection("first_time_pass", LastTokenToTouchTheBallOnPurpose, ball?.GetCurrentHex());
        bool preserveAerialPrecompute = ShouldPreserveAerialTargetPrecomputeDuringPreview();
        ClearPendingActionPreviews();
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass(preserveTargetPrecompute: preserveAerialPrecompute);
        longBallManager.CleanUpLongBall(preserveTargetPrecompute: preserveAerialPrecompute);
        RefreshAvailableActions();
        firstTimePassManager.ActivateFTP();
    }

    private bool ShouldPreserveAerialTargetPrecomputeDuringPreview()
    {
        return difficulty_level == 1;
    }

    private void ClearPendingActionPreviews()
    {
        if (shotManager != null)
        {
            shotManager.CancelShotCommitPreview();
        }
    }

    public void ClearNonShotActionPreviews()
    {
        movementPhaseManager?.ResetMovementPhase();
        groundBallManager?.CleanUpPass();
        firstTimePassManager?.CleanUpFTP();
        highPassManager?.CleanUpHighPass();
        longBallManager?.CleanUpLongBall();
        RefreshAvailableActions();
    }

    public void CommitToAction(
        MatchActionKind actionKind = MatchActionKind.None,
        string commitSource = "user",
        string commitReason = "")
    {
        if (actionKind != MatchActionKind.None)
        {
            SetSubstitutionsAvailable(false, $"Committed to {actionKind}");
        }

        movementPhaseManager.isAvailable = false;
        groundBallManager.isAvailable = false;
        firstTimePassManager.isAvailable = false;
        highPassManager.isAvailable = false;
        longBallManager.isAvailable = false;
        shotManager.isAvailable = false;
        isFTPAvailable = false;
        if (currentState == GameState.StandardPass || currentState == GameState.HighPass)
        {
            pendingGoalKickRestartTaker = null;
        }
        ResetPendingGroundBallOffer();
        ApplyPendingGroundBallDistance();
        RefreshAerialTargetPrecomputations();
        RegisterCommittedAction(actionKind);
        RecordActionCommitted(actionKind, commitSource, commitReason);
        if (!isHalfExpired && !isMatchComplete)
        {
            isClockRunning = true;
        }
    }

    private void RegisterCommittedAction(MatchActionKind actionKind)
    {
        if (actionKind == MatchActionKind.None)
        {
            return;
        }

        currentCommittedActionKind = actionKind;
        hasUnresolvedCommittedExtraAction = false;
        committedExtraActionNumber = 0;
        pendingShotGoalTimeLabel = string.Empty;

        if (isHalfExpired && extraActionsDetermined && extraActionsRemaining > 0)
        {
            hasUnresolvedCommittedExtraAction = true;
            committedExtraActionNumber = Mathf.Clamp(extraActionsTotal - extraActionsRemaining + 1, 1, Mathf.Max(1, extraActionsTotal));
        }
    }

    public void BroadcastSafeEndofMovementPhase(bool countMovementAction = true, bool triggerFinalThird = true)
    {
        ResolveActionBeforeFinalThird(
            MatchActionKind.MovementPhase,
            () =>
            {
                if (triggerFinalThird && finalThirdManager != null)
                {
                    finalThirdManager.TriggerFinalThirdPhase();
                }

                currentState = GameState.EndOfMovementPhase;
                UpdatePossessionAfterPass(ball.GetCurrentHex());
                OfferStandardGroundBallPass();
                RefreshAvailableActions();
                RecordGameplayOutcome(
                    "action.phase",
                    "movement",
                    "ended",
                    sourceHex: ball?.GetCurrentHex());
            },
            countMovementAction);
    }
    public void BroadcastSuccessfulTackle()
    {
        ResolveActionBeforeFinalThird(
            MatchActionKind.MovementPhase,
            () =>
            {
                if (finalThirdManager != null)
                {
                    finalThirdManager.TriggerFinalThirdPhase();
                }

                currentState = GameState.SuccessfulTackle;
                OfferStandardGroundBallPass();
                RefreshAvailableActions();
                RecordGameplayOutcome(
                    "action.phase",
                    "movement",
                    "successful_tackle",
                    sourceHex: ball?.GetCurrentHex());
            },
            movementPhaseManager == null || movementPhaseManager.committedMovementConsumesExtraAction);
    }
    
    public void BroadcastEndofGroundBallPass()
    {
        currentState = GameState.EndOfStandardPass;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "standard_pass",
            "ended",
            sourceHex: ball?.GetCurrentHex());
    }

    public void BroadcastEndofFirstTimePass()
    {
        currentState = GameState.EndOfFirstTimePass;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "first_time_pass",
            "ended",
            sourceHex: ball?.GetCurrentHex());
    }
    
    public void BroadcastEndOfLongBall()
    {
        currentState = GameState.EndOfLongBall;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "long_ball",
            "ended",
            sourceHex: ball?.GetCurrentHex());
    }
    
    public void BroadcastAnyOtherScenario(bool offerShortGroundBall = true)
    {
        currentState = GameState.AnyOtherScenario;
        if (offerShortGroundBall) OfferShortGroundBallPass();
        else OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "any_other_scenario",
            "entered",
            sourceHex: ball?.GetCurrentHex(),
            details: CreateDetails(("offerShortGroundBall", offerShortGroundBall)));
    }

    public bool BroadcastDefensiveRecoveryOutcome(PlayerToken recoveringToken, HexCell recoveryHex, bool triggerFinalThirdsForAnyOther = true)
    {
        if (IsGoalkeeperHoldingInOwnPenaltyBox(recoveringToken, recoveryHex))
        {
            if (movementPhaseManager != null && movementPhaseManager.isActivated)
            {
                movementPhaseManager.EndMovementPhase(false);
            }

            currentState = GameState.ActivateFinalThirdsAfterSave;
            movementPhaseManager.isAvailable = false;
            groundBallManager.isAvailable = false;
            firstTimePassManager.isAvailable = false;
            highPassManager.isAvailable = false;
            longBallManager.isAvailable = false;
            shotManager.EnterSaveAndHoldDecision();
            RecordGameplayOutcome(
                "action.phase",
                "goalkeeper_save_hold",
                "entered",
                actor: recoveringToken,
                targetHex: recoveryHex);
            Debug.Log($"{recoveringToken.name} recovered the ball in their own penalty box. Save and hold scenario.");
            return true;
        }

        BroadcastAnyOtherScenario();
        FinalThirdManager resolvedFinalThirdManager = FindAnyObjectByType<FinalThirdManager>();
        if (triggerFinalThirdsForAnyOther && resolvedFinalThirdManager != null)
        {
            resolvedFinalThirdManager.TriggerFinalThirdPhase();
        }

        return false;
    }

    private bool IsGoalkeeperHoldingInOwnPenaltyBox(PlayerToken token, HexCell recoveryHex)
    {
        if (token == null || recoveryHex == null || !token.IsGoalKeeper || recoveryHex.isInPenaltyBox == 0)
        {
            return false;
        }

        TeamAttackingDirection direction = token.isHomeTeam ? homeTeamDirection : awayTeamDirection;
        int ownPenaltyBox = direction == TeamAttackingDirection.LeftToRight ? -1 : 1;
        return recoveryHex.isInPenaltyBox == ownPenaltyBox;
    }

    public void BroadcastBallControl()
    {
        currentState = GameState.BallControl;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "ball_control",
            "entered",
            sourceHex: ball?.GetCurrentHex());
    }


    public void BroadcastQuickThrow()
    {
        currentState = GameState.QuickThrow;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "throw_in",
            "quick_throw",
            sourceHex: ball?.GetCurrentHex());
    }
    public void BroadcastActivateFinalThirdsAfterSave()
    {
        currentState = GameState.ActivateFinalThirdsAfterSave;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "goalkeeper_save_hold",
            "activate_final_thirds",
            sourceHex: ball?.GetCurrentHex());
    }

    public void BroadcastGoalKickRestartOptions(PlayerToken goalkeeper)
    {
        pendingGoalKickRestartTaker = goalkeeper;
        currentState = GameState.GoalKick;
        OfferStandardGroundBallPass();
        if (goalkeeper != null)
        {
            ClearLastTokenChain();
            ForceGoalKickRestartTakerAsLastToken(goalkeeper);
            if (goalkeeper.GetCurrentHex() != null)
            {
                ball.PlaceAtCell(goalkeeper.GetCurrentHex());
            }
        }
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "goalkeeper_kick",
            "options_available",
            actor: goalkeeper,
            sourceHex: goalkeeper != null ? goalkeeper.GetCurrentHex() : ball?.GetCurrentHex());
    }

    private void ForceGoalKickRestartTakerAsLastToken(PlayerToken goalkeeper)
    {
        if (goalkeeper == null)
        {
            return;
        }

        PreviousTokenToTouchTheBallOnPurpose = null;
        LastTokenToTouchTheBallOnPurpose = goalkeeper;
    }

    public void BroadcastHeaderCompleted()
    {
        currentState = GameState.HeaderCompleted;
        OfferStandardGroundBallPass();
        RefreshAvailableActions();
        RecordGameplayOutcome(
            "action.phase",
            "header",
            "completed",
            sourceHex: ball?.GetCurrentHex());
    }

    private void RefreshAvailableActions()
    {
        bool autoCommittedAction = false;
        void AutoCommitMovement(string reason)
        {
            autoCommittedAction = true;
            movementPhaseManager.ActivateMovementPhase();
            movementPhaseManager.CommitToAction(
                commitSource: "auto",
                commitReason: reason);
        }

        if (currentState == GameState.KickoffBlown)
        {
            movementPhaseManager.isAvailable = false;
            groundBallManager.isAvailable = true;
            firstTimePassManager.isAvailable = false;
            highPassManager.isAvailable = true;
            longBallManager.isAvailable = true;
            shotManager.isAvailable = false;
        }
        else if (currentState == GameState.EndOfStandardPass)
        {
            movementPhaseManager.isAvailable = true;
            groundBallManager.isAvailable = false;
            highPassManager.isAvailable = false;
            longBallManager.isAvailable = false;
            if (attackHasPossession)
            {
                firstTimePassManager.isAvailable = true;
                isFTPAvailable = true;
                if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
                else shotManager.isAvailable = false;
            }
            else
            {
                AutoCommitMovement("end_standard_pass_attack_without_possession");
            }
        }
        else if (currentState == GameState.EndOfMovementPhase)
        {
            if (attackHasPossession)
            {
                movementPhaseManager.isAvailable = true;
                groundBallManager.isAvailable = true;
                firstTimePassManager.isAvailable = false;
                highPassManager.isAvailable = true;
                longBallManager.isAvailable = true;
                if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
                else shotManager.isAvailable = false;
            }
            else 
            {
                AutoCommitMovement("end_movement_phase_attack_without_possession");
            }
        }
        else if (currentState == GameState.EndOfLongBall)
        {
            if (attackHasPossession && ball.GetCurrentHex() != null && ball.GetCurrentHex().isAttackOccupied && ShouldShotBeAvailable())
            {
                movementPhaseManager.isAvailable = true;
                shotManager.isAvailable = true;
            }
            else
            {
                AutoCommitMovement("end_long_ball_no_available_continuation");
            }
        }
        else if (currentState == GameState.EndOfFirstTimePass)
        {
            if (!attackHasPossession)
            {
                AutoCommitMovement("end_first_time_pass_attack_without_possession");
            }
            else
            {
                if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
                else
                {
                    AutoCommitMovement("end_first_time_pass_shot_unavailable");
                }
            }
        }
        else if (currentState == GameState.AnyOtherScenario)
        {
            if (attackHasPossession)
            {
                movementPhaseManager.isAvailable = true;
                groundBallManager.isAvailable = true;
                firstTimePassManager.isAvailable = false;
                highPassManager.isAvailable = false;
                longBallManager.isAvailable = true;
                if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
                else shotManager.isAvailable = false;
            }
            else 
            {
                AutoCommitMovement("any_other_scenario_attack_without_possession");
            }
        }
        else if (currentState == GameState.HeaderCompleted)
        {
            if (attackHasPossession)
            {
                movementPhaseManager.isAvailable = true;
                groundBallManager.isAvailable = false;
                firstTimePassManager.isAvailable = true;
                highPassManager.isAvailable = false;
                longBallManager.isAvailable = true;
                if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
                else shotManager.isAvailable = false;
            }
            else 
            {
                AutoCommitMovement("header_completed_attack_without_possession");
            }
        }
        else if (currentState == GameState.SuccessfulTackle || currentState == GameState.BallControl)
        {
            movementPhaseManager.isAvailable = true;
            groundBallManager.isAvailable = true;
            firstTimePassManager.isAvailable = false;
            highPassManager.isAvailable = true;
            longBallManager.isAvailable = true;
            if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
            else shotManager.isAvailable = false;
        }
        else if (currentState == GameState.QuickThrow)
        {
            autoCommittedAction = true;
            groundBallManager.ActivateGroundBall(true);
            groundBallManager.CommitToThisAction();
        }
        else if (currentState == GameState.ActivateFinalThirdsAfterSave)
        {
            // Drop Ball and GK
            // movementPhaseManager.isAvailable = false;
            // groundBallManager.isAvailable = true;
            // firstTimePassManager.isAvailable = false;
            // highPassManager.isAvailable = false;
            // longBallManager.isAvailable = false;
            // shotManager.isAvailable = false;
        }
        else if (currentState == GameState.GoalKick)
        {
            movementPhaseManager.isAvailable = false;
            groundBallManager.isAvailable = true;
            firstTimePassManager.isAvailable = false;
            highPassManager.isAvailable = true;
            longBallManager.isAvailable = false;
            shotManager.isAvailable = false;
        }

        ApplyPendingGroundBallDistance();
        RefreshAerialTargetPrecomputations();
        if (!autoCommittedAction && (finalThirdManager == null || !finalThirdManager.isActivated))
        {
            RecordAvailableActions("refresh_available_actions");
        }
    }

    public void RefreshAvailableActionsForCurrentState()
    {
        RefreshAvailableActions();
    }

    public bool CanCreateRuntimeSnapshot(out string reason)
    {
        reason = string.Empty;
        if (gameData == null || gameData.gameSettings == null)
        {
            reason = "Cannot save before match settings are loaded.";
            return false;
        }

        if (gameData.rosters == null || gameData.rosters.home.Count <= 10 || gameData.rosters.away.Count <= 10)
        {
            reason = "Cannot save before both rosters are loaded.";
            return false;
        }

        if (hexGrid == null || !hexGrid.IsGridInitialized())
        {
            reason = "Cannot save before the pitch grid is ready.";
            return false;
        }

        if (playerTokenManager == null || playerTokenManager.allTokens.Count == 0)
        {
            reason = "Cannot save before player tokens are ready.";
            return false;
        }

        if (ball == null || ball.GetCurrentHex() == null)
        {
            reason = "Cannot save before the ball has a valid hex.";
            return false;
        }

        if (ball.isMoving)
        {
            reason = "Cannot save while the ball is moving.";
            return false;
        }

        if (movementPhaseManager != null && (movementPhaseManager.isPlayerMoving || movementPhaseManager.isDribblerRunning))
        {
            reason = "Cannot save while a token movement is in progress.";
            return false;
        }

        if (isHalfTimeFlowRunning || isHalfEndPendingAfterGoalFlow)
        {
            reason = "Cannot save while half-time or full-time reset flow is running.";
            return false;
        }

        if (HasActiveRuntimeAction())
        {
            reason = "Cannot save while an action or set-piece flow is active. Wait for the next stable choice point.";
            return false;
        }

        if (!IsStableSaveState(currentState))
        {
            reason = $"Cannot save from {currentState}. Wait for a stable action-choice point.";
            return false;
        }

        return true;
    }

    public RoomRuntimeSnapshot CreateRuntimeSnapshot()
    {
        List<PlayerToken> tokens = FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .Where(token => token != null)
            .OrderBy(token => token.isHomeTeam ? 0 : 1)
            .ThenBy(token => token.jerseyNumber)
            .ToList();

        List<string> logEntries = gameData?.gameLog != null
            ? gameData.gameLog.GetGameLog()
            : new List<string>();

        return new RoomRuntimeSnapshot
        {
            version = RoomSaveService.SaveSchemaVersion,
            currentState = currentState.ToString(),
            teamInAttack = teamInAttack.ToString(),
            attackHasPossession = attackHasPossession,
            homeTeamDirection = homeTeamDirection.ToString(),
            awayTeamDirection = awayTeamDirection.ToString(),
            clock = new RoomClockSnapshot
            {
                currentHalf = currentHalf,
                currentHalfRegulationSeconds = currentHalfRegulationSeconds,
                completedActionsThisHalf = completedActionsThisHalf,
                totalCompletedActions = totalCompletedActions,
                isClockRunning = isClockRunning,
                isHalfExpired = isHalfExpired,
                extraActionsDetermined = extraActionsDetermined,
                extraActionsTotal = extraActionsTotal,
                extraActionsRemaining = extraActionsRemaining,
                isWaitingForExtraActionsRoll = isWaitingForExtraActionsRoll,
                isMatchComplete = isMatchComplete,
                currentCommittedActionKind = currentCommittedActionKind.ToString(),
                hasUnresolvedCommittedExtraAction = hasUnresolvedCommittedExtraAction,
                committedExtraActionNumber = committedExtraActionNumber,
                pendingShotGoalTimeLabel = pendingShotGoalTimeLabel,
                pendingShotGoalExtraActionNumber = pendingShotGoalExtraActionNumber,
                pendingGroundBallDistance = pendingGroundBallDistance
            },
            ball = RoomHexCoordinates.FromHex(ball.GetCurrentHex()),
            tokens = tokens.Select(CreateTokenSnapshot).ToList(),
            touchReferences = new RoomTouchReferences
            {
                lastTokenToTouchTheBallOnPurpose = CreateTokenReference(LastTokenToTouchTheBallOnPurpose),
                previousTokenToTouchTheBallOnPurpose = CreateTokenReference(PreviousTokenToTouchTheBallOnPurpose),
                pendingGoalKickRestartTaker = CreateTokenReference(pendingGoalKickRestartTaker),
                setPieceTakerExcludedFromNextTouch = CreateTokenReference(setPieceTakerExcludedFromNextTouch),
                hangingPassType = hangingPassType,
                hangingPassExcludedCollector = CreateTokenReference(hangingPassExcludedCollector),
                clearPreviousOnNextBallCollection = clearPreviousOnNextBallCollection
            },
            substitutions = new RoomSubstitutionSnapshot
            {
                homeSubstitutionsUsed = homeSubstitutionsUsed,
                awaySubstitutionsUsed = awaySubstitutionsUsed,
                extraTimeSubstitutionCreditGranted = extraTimeSubstitutionCreditGranted,
                areSubstitutionsAvailable = areSubstitutionsAvailable,
                substitutionsAvailabilityReason = substitutionsAvailabilityReason,
                goalkeeperReplacementRequired = goalkeeperReplacementRequired,
                goalkeeperReplacementTeamIsHome = goalkeeperReplacementTeamIsHome,
                emergencyGoalkeeperNominationRequired = emergencyGoalkeeperNominationRequired,
                emergencyGoalkeeperNominationTeamIsHome = emergencyGoalkeeperNominationTeamIsHome,
                emergencyGoalkeeperNominationReason = emergencyGoalkeeperNominationReason
            },
            stats = gameData?.stats,
            homeScorers = homeScorers != null ? new List<GoalEvent>(homeScorers) : new List<GoalEvent>(),
            awayScorers = awayScorers != null ? new List<GoalEvent>(awayScorers) : new List<GoalEvent>(),
            gameLog = logEntries
        };
    }

    private bool IsStableSaveState(GameState state)
    {
        return state == GameState.KickOffSetup
            || state == GameState.KickoffBlown
            || state == GameState.EndOfMovementPhase
            || state == GameState.EndOfStandardPass
            || state == GameState.EndOfFirstTimePass
            || state == GameState.AnyOtherScenario
            || state == GameState.EndOfLongBall
            || state == GameState.SuccessfulTackle
            || state == GameState.BallControl
            || state == GameState.HeaderCompleted
            || state == GameState.GoalKick
            || state == GameState.HalfTime
            || state == GameState.MatchEnded;
    }

    private bool HasActiveRuntimeAction()
    {
        return (movementPhaseManager != null && movementPhaseManager.isActivated)
            || (groundBallManager != null && groundBallManager.isActivated)
            || (firstTimePassManager != null && firstTimePassManager.isActivated)
            || (highPassManager != null && highPassManager.isActivated)
            || (longBallManager != null && longBallManager.isActivated)
            || (shotManager != null && shotManager.isActivated)
            || (freeKickManager != null && freeKickManager.isActivated)
            || (penaltyKickManager != null && penaltyKickManager.isActivated)
            || (finalThirdManager != null && finalThirdManager.isActivated)
            || (goalFlowManager != null && goalFlowManager.isActivated)
            || (FindAnyObjectByType<ThrowInManager>()?.isActivated ?? false)
            || (FindAnyObjectByType<GoalKeeperManager>()?.isActivated ?? false);
    }

    private RoomTokenSnapshot CreateTokenSnapshot(PlayerToken token)
    {
        return new RoomTokenSnapshot
        {
            tokenKey = BuildTokenKey(token.isHomeTeam, token.jerseyNumber),
            teamSide = token.isHomeTeam ? "Home" : "Away",
            jerseyNumber = token.jerseyNumber,
            playerName = token.playerName,
            currentHex = RoomHexCoordinates.FromHex(token.GetCurrentHex()),
            isPlaying = token.isPlaying,
            wasSubbedOn = token.wasSubbedOn,
            wasSubbedOff = token.wasSubbedOff,
            isSentOff = token.isSentOff,
            isBooked = token.isBooked,
            isInjured = token.isInjured,
            requiresSubstitution = token.requiresSubstitution,
            isGoalKeeper = token.IsGoalKeeper,
            isAttacker = token.isAttacker,
            pace = token.pace,
            dribbling = token.dribbling,
            highPass = token.highPass,
            resilience = token.resilience,
            heading = token.heading,
            shooting = token.shooting,
            tackling = token.tackling,
            aerial = token.aerial,
            saving = token.saving,
            handling = token.handling
        };
    }

    private RoomTokenReference CreateTokenReference(PlayerToken token)
    {
        if (token == null)
        {
            return null;
        }

        return new RoomTokenReference
        {
            tokenKey = BuildTokenKey(token.isHomeTeam, token.jerseyNumber),
            teamSide = token.isHomeTeam ? "Home" : "Away",
            jerseyNumber = token.jerseyNumber,
            playerName = token.playerName
        };
    }

    private static string BuildTokenKey(bool isHomeTeam, int jerseyNumber)
    {
        return $"{(isHomeTeam ? "Home" : "Away")}:{jerseyNumber}";
    }

    private IEnumerator RestoreRuntimeSnapshotWhenReady(RoomRuntimeSnapshot snapshot)
    {
        if (snapshot == null)
        {
            yield break;
        }

        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());
        yield return new WaitUntil(() => playerTokenManager != null
            && FindObjectsByType<PlayerToken>(FindObjectsInactive.Include).Length > 0);
        yield return new WaitUntil(() => ball != null && ball.GetCurrentHex() != null);
        yield return null;

        ApplyRuntimeSnapshot(snapshot);
    }

    private void ApplyRuntimeSnapshot(RoomRuntimeSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        ResolveClockDependencies();
        BeginGameplayEventLoggingSuppression();
        try
        {
            RestoreSnapshotStateFields(snapshot);
            RestoreTokenSnapshots(snapshot.tokens);
            RestoreBallSnapshot(snapshot.ball);
            RestoreTouchReferences(snapshot.touchReferences);
            RestoreSubstitutionSnapshot(snapshot.substitutions);
            RebuildManagersAfterRuntimeRestore();
        }
        finally
        {
            EndGameplayEventLoggingSuppression();
        }

        EnsureGameplayEventLogInitialized();
        gameData.runtimeSnapshot = snapshot;
        Debug.Log($"Restored runtime save snapshot from {snapshot.savedAtUtc ?? "unknown time"}.");
    }

    private void RestoreSnapshotStateFields(RoomRuntimeSnapshot snapshot)
    {
        if (Enum.TryParse(snapshot.currentState, out GameState restoredState))
        {
            currentState = restoredState;
        }

        if (Enum.TryParse(snapshot.teamInAttack, out TeamInAttack restoredTeamInAttack))
        {
            teamInAttack = restoredTeamInAttack;
        }

        if (Enum.TryParse(snapshot.homeTeamDirection, out TeamAttackingDirection restoredHomeDirection))
        {
            homeTeamDirection = restoredHomeDirection;
        }

        if (Enum.TryParse(snapshot.awayTeamDirection, out TeamAttackingDirection restoredAwayDirection))
        {
            awayTeamDirection = restoredAwayDirection;
        }

        attackHasPossession = snapshot.attackHasPossession;

        if (snapshot.clock != null)
        {
            currentHalf = Mathf.Max(1, snapshot.clock.currentHalf);
            currentHalfRegulationSeconds = Mathf.Max(0f, snapshot.clock.currentHalfRegulationSeconds);
            completedActionsThisHalf = Mathf.Max(0, snapshot.clock.completedActionsThisHalf);
            totalCompletedActions = Mathf.Max(0, snapshot.clock.totalCompletedActions);
            isClockRunning = snapshot.clock.isClockRunning;
            isHalfExpired = snapshot.clock.isHalfExpired;
            extraActionsDetermined = snapshot.clock.extraActionsDetermined;
            extraActionsTotal = Mathf.Max(0, snapshot.clock.extraActionsTotal);
            extraActionsRemaining = Mathf.Max(0, snapshot.clock.extraActionsRemaining);
            isWaitingForExtraActionsRoll = snapshot.clock.isWaitingForExtraActionsRoll;
            isMatchComplete = snapshot.clock.isMatchComplete;
            hasUnresolvedCommittedExtraAction = snapshot.clock.hasUnresolvedCommittedExtraAction;
            committedExtraActionNumber = Mathf.Max(0, snapshot.clock.committedExtraActionNumber);
            pendingShotGoalTimeLabel = snapshot.clock.pendingShotGoalTimeLabel ?? string.Empty;
            pendingShotGoalExtraActionNumber = Mathf.Max(0, snapshot.clock.pendingShotGoalExtraActionNumber);
            pendingGroundBallDistance = snapshot.clock.pendingGroundBallDistance > 0
                ? snapshot.clock.pendingGroundBallDistance
                : StandardGroundBallDistance;
            if (Enum.TryParse(snapshot.clock.currentCommittedActionKind, out MatchActionKind restoredActionKind))
            {
                currentCommittedActionKind = restoredActionKind;
            }
        }

        isPauseMenuOpen = false;
        isHalfTimeFlowRunning = false;
        isHalfEndPendingAfterGoalFlow = false;
        pendingHalfGateContinuation = null;

        if (snapshot.stats != null)
        {
            gameData.stats = snapshot.stats;
        }

        gameData.stats ??= new Stats();
        gameData.stats.homeTeamStats ??= new TeamStats();
        gameData.stats.awayTeamStats ??= new TeamStats();
        gameData.gameLog ??= new GameLog(gameData.stats);
        gameData.gameLog.RebindStats(gameData.stats);
        gameData.gameLog.ReplaceEntries(snapshot.gameLog);
        homeScorers = snapshot.homeScorers != null ? new List<GoalEvent>(snapshot.homeScorers) : new List<GoalEvent>();
        awayScorers = snapshot.awayScorers != null ? new List<GoalEvent>(snapshot.awayScorers) : new List<GoalEvent>();
    }

    private void RestoreTokenSnapshots(List<RoomTokenSnapshot> tokenSnapshots)
    {
        if (tokenSnapshots == null || tokenSnapshots.Count == 0 || playerTokenManager == null)
        {
            return;
        }

        ClearAllHexOccupancy();
        Dictionary<string, PlayerToken> tokensByKey = FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .Where(token => token != null)
            .GroupBy(token => BuildTokenKey(token.isHomeTeam, token.jerseyNumber))
            .ToDictionary(group => group.Key, group => group.First());

        playerTokenManager.allTokens.Clear();
        playerTokenManager.benchTokens.Clear();

        int homeBenchIndex = 0;
        int awayBenchIndex = 0;
        foreach (RoomTokenSnapshot tokenSnapshot in tokenSnapshots)
        {
            if (tokenSnapshot == null || !tokensByKey.TryGetValue(tokenSnapshot.tokenKey, out PlayerToken token))
            {
                Debug.LogWarning($"Could not restore token {tokenSnapshot?.tokenKey ?? "<missing key>"} because no matching runtime token exists.");
                continue;
            }

            HexCell restoredHex = ResolveHex(tokenSnapshot.currentHex);
            if (restoredHex != null)
            {
                restoredHex.occupyingToken = null;
                restoredHex.isAttackOccupied = tokenSnapshot.isAttacker;
                restoredHex.isDefenseOccupied = !tokenSnapshot.isAttacker;
            }

            bool assignHomeBench = tokenSnapshot.teamSide == "Home" || token.isHomeTeam;
            int benchIndex = assignHomeBench ? homeBenchIndex : awayBenchIndex;
            Vector3 inactivePosition = playerTokenManager.GetBenchTokenPositionForRestore(assignHomeBench, benchIndex);
            if (!tokenSnapshot.isPlaying && !tokenSnapshot.isSentOff)
            {
                if (assignHomeBench) homeBenchIndex++;
                else awayBenchIndex++;
            }

            token.RestoreRuntimeState(tokenSnapshot, restoredHex, inactivePosition);
            if (token.IsGoalKeeper)
            {
                ApplyTeamGoalkeeperKit(token);
            }

            if (token.isPlaying)
            {
                playerTokenManager.allTokens.Add(token);
            }
            else if (!token.isSentOff)
            {
                playerTokenManager.benchTokens.Add(token);
            }
        }

        playerTokenManager.allTokens = playerTokenManager.allTokens
            .OrderBy(token => token.isHomeTeam ? 0 : 1)
            .ThenBy(token => token.jerseyNumber)
            .ToList();
        playerTokenManager.benchTokens = playerTokenManager.benchTokens
            .OrderBy(token => token.isHomeTeam ? 0 : 1)
            .ThenBy(token => token.jerseyNumber)
            .ToList();
        RefreshHexOccupancyHighlights();
    }

    private void ClearAllHexOccupancy()
    {
        if (hexGrid?.cells == null)
        {
            return;
        }

        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null)
            {
                continue;
            }

            hex.occupyingToken = null;
            hex.isAttackOccupied = false;
            hex.isDefenseOccupied = false;
            hex.ResetHighlight();
        }
    }

    private void RefreshHexOccupancyHighlights()
    {
        if (hexGrid?.cells == null)
        {
            return;
        }

        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null)
            {
                continue;
            }

            if (hex.isAttackOccupied)
            {
                hex.HighlightHex("isAttackOccupied");
            }
            else if (hex.isDefenseOccupied)
            {
                hex.HighlightHex("isDefenseOccupied");
            }
            else
            {
                hex.ResetHighlight();
            }
        }
    }

    private void RestoreBallSnapshot(RoomHexCoordinates ballHexCoordinates)
    {
        HexCell restoredBallHex = ResolveHex(ballHexCoordinates);
        if (restoredBallHex == null)
        {
            Debug.LogWarning("Runtime snapshot did not contain a valid ball hex. Leaving ball at its current position.");
            return;
        }

        ball.PlaceAtCell(restoredBallHex);
    }

    private void RestoreTouchReferences(RoomTouchReferences references)
    {
        LastTokenToTouchTheBallOnPurpose = ResolveTokenReference(references?.lastTokenToTouchTheBallOnPurpose);
        PreviousTokenToTouchTheBallOnPurpose = ResolveTokenReference(references?.previousTokenToTouchTheBallOnPurpose);
        pendingGoalKickRestartTaker = ResolveTokenReference(references?.pendingGoalKickRestartTaker);
        setPieceTakerExcludedFromNextTouch = ResolveTokenReference(references?.setPieceTakerExcludedFromNextTouch);
        hangingPassType = references?.hangingPassType;
        hangingPassExcludedCollector = ResolveTokenReference(references?.hangingPassExcludedCollector);
        clearPreviousOnNextBallCollection = references != null && references.clearPreviousOnNextBallCollection;
    }

    private void RestoreSubstitutionSnapshot(RoomSubstitutionSnapshot substitutions)
    {
        if (substitutions == null)
        {
            extraTimeSubstitutionCreditGranted = IsExtraTimeHalf(currentHalf);
            InitializeSubstitutionCountsFromStats();
            return;
        }

        extraTimeSubstitutionCreditGranted = substitutions.extraTimeSubstitutionCreditGranted || IsExtraTimeHalf(currentHalf);
        homeSubstitutionsUsed = Mathf.Clamp(substitutions.homeSubstitutionsUsed, 0, GetSubstitutionLimit(true));
        awaySubstitutionsUsed = Mathf.Clamp(substitutions.awaySubstitutionsUsed, 0, GetSubstitutionLimit(false));
        areSubstitutionsAvailable = substitutions.areSubstitutionsAvailable;
        substitutionsAvailabilityReason = substitutions.substitutionsAvailabilityReason ?? string.Empty;
        goalkeeperReplacementRequired = substitutions.goalkeeperReplacementRequired;
        goalkeeperReplacementTeamIsHome = substitutions.goalkeeperReplacementTeamIsHome;
        emergencyGoalkeeperNominationRequired = substitutions.emergencyGoalkeeperNominationRequired;
        emergencyGoalkeeperNominationTeamIsHome = substitutions.emergencyGoalkeeperNominationTeamIsHome;
        emergencyGoalkeeperNominationReason = substitutions.emergencyGoalkeeperNominationReason ?? string.Empty;
        OnSubstitutionStateChanged?.Invoke();
    }

    private HexCell ResolveHex(RoomHexCoordinates coordinates)
    {
        if (coordinates == null || hexGrid == null)
        {
            return null;
        }

        return hexGrid.GetHexCellAt(coordinates.ToVector3Int());
    }

    private PlayerToken ResolveTokenReference(RoomTokenReference reference)
    {
        if (reference == null)
        {
            return null;
        }

        return FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .FirstOrDefault(token => token != null
                && BuildTokenKey(token.isHomeTeam, token.jerseyNumber) == reference.tokenKey);
    }

    private void RebuildManagersAfterRuntimeRestore()
    {
        ClearPendingActionPreviews();
        movementPhaseManager?.ResetMovementPhase();
        groundBallManager?.CleanUpPass();
        firstTimePassManager?.CleanUpFTP();
        highPassManager?.CleanUpHighPass();
        longBallManager?.CleanUpLongBall();
        ApplyPendingGroundBallDistance();

        if (currentState == GameState.KickOffSetup)
        {
            kickoffManager?.StartPreKickoffPhase();
        }
        else if (currentState == GameState.MatchEnded)
        {
            ClearAvailableActionsForHalfBreak();
        }
        else if (currentState != GameState.HalfTime)
        {
            RefreshAvailableActionsForCurrentState();
        }
    }

    private void RefreshAerialTargetPrecomputations()
    {
        RefreshHighPassTargetPrecomputation();
        RefreshLongBallTargetPrecomputation();
    }

    private void RefreshHighPassTargetPrecomputation()
    {
        if (highPassManager == null)
        {
            return;
        }

        if (highPassManager.isAvailable && difficulty_level == 1)
        {
            highPassManager.BeginAvailableTargetPrecompute();
        }
        else
        {
            highPassManager.ResetAvailableTargetPrecompute();
        }
    }

    private void RefreshLongBallTargetPrecomputation()
    {
        if (longBallManager == null)
        {
            return;
        }

        if (longBallManager.isAvailable && difficulty_level == 1)
        {
            longBallManager.BeginAvailableTargetPrecompute();
        }
        else
        {
            longBallManager.ResetAvailableTargetPrecompute();
        }
    }

    private bool ShouldShotBeAvailable()
    {
        if (IsFinalExtraMovementPhaseSnapshotSuppressed())
        {
            return false;
        }

        bool shouldShotBeAvailable = false;
        HexCell ballHex = ball.GetCurrentHex();
        PlayerToken tokenOnBallHex = ballHex != null ? ballHex.GetOccupyingToken() : null;
        bool ballIsPossessedByAttacker = ballHex != null
            && ballHex.isAttackOccupied
            && tokenOnBallHex != null
            && tokenOnBallHex.isAttacker
            && attackHasPossession;
        MatchManager.TeamAttackingDirection attackingDirection;
        if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home)
        {
            attackingDirection = MatchManager.Instance.homeTeamDirection;
        }
        else
        {
            attackingDirection = MatchManager.Instance.awayTeamDirection;
        }
        if (
            (
                attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attackers shoot to the Right
                && ballHex != null
                && ballHex.CanShootFrom // Is in shooting distance
                && ballHex.coordinates.x > 0 // In Right Side of Pitch
                && ballIsPossessedByAttacker // Ball is on an attacker
            )
            ||
            (
                attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attackers shoot to the Left
                && ballHex != null
                && ballHex.CanShootFrom // Is in shooting distance
                && ballHex.coordinates.x < 0 // In Left Side of Pitch
                && ballIsPossessedByAttacker // Ball is on an attacker
            )
        )
        {
          shouldShotBeAvailable = true;
        }
        return shouldShotBeAvailable;
    }
    
    public void EnableFreeKickOptions()
    {
        OfferStandardGroundBallPass();
        movementPhaseManager.isAvailable = false;
        groundBallManager.isAvailable = true;
        firstTimePassManager.isAvailable = false;
        highPassManager.isAvailable = true;
        longBallManager.isAvailable = true;
        if (shotManager.IsFreeKickShotAvailableFromBall()) shotManager.isAvailable = true;
        else shotManager.isAvailable = false;
        RefreshAerialTargetPrecomputations();
    }
    
    public void EnableCornerKickOptions()
    {
        OfferShortGroundBallPass();
        movementPhaseManager.isAvailable = false;
        groundBallManager.isAvailable = true;
        firstTimePassManager.isAvailable = false;
        highPassManager.isAvailable = true;
        longBallManager.isAvailable = false;
        shotManager.isAvailable = false;
        RefreshAerialTargetPrecomputations();
    }
    
    public void UpdatePlayerTokensAfterPossessionChange()
    {
        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            // Determine if the token is now an attacker or a defender
            if ((teamInAttack == TeamInAttack.Home && token.isHomeTeam) ||
                (teamInAttack == TeamInAttack.Away && !token.isHomeTeam))
            {
                token.isAttacker = true;  // Set to attacker if the token's team is now in attack
            }
            else
            {
                token.isAttacker = false;  // Set to defender otherwise
            }
        }

        Debug.Log("Player tokens updated after possession change.");
    }

    public void MakeSureEveryOneIsCorrectlyAssigned()
    {
        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            // Determine if the token is now an attacker or a defender
            if ((teamInAttack == TeamInAttack.Home && token.isHomeTeam) ||
                (teamInAttack == TeamInAttack.Away && !token.isHomeTeam))
            {
                token.isAttacker = true;  // Set to attacker if the token's team is now in attack
            }
            else
            {
                token.isAttacker = false;  // Set to defender otherwise
            }
        }
        foreach (HexCell hex in hexGrid.cells)
        {
            PlayerToken token = hex.GetOccupyingToken();
            if (token == null)
            {
                hex.isAttackOccupied = false;
                hex.isDefenseOccupied = false;
                hex.ResetHighlight();
            }
            else
            {
                if (token.isAttacker)
                {
                    hex.isAttackOccupied = true;
                    hex.isDefenseOccupied = false;
                    hex.ResetHighlight();
                    hex.HighlightHex("isAttackOccupied");
                }
                else
                {
                    hex.isAttackOccupied = false;
                    hex.isDefenseOccupied = true;
                    hex.ResetHighlight();
                    hex.HighlightHex("isDefenseOccupied");
                }
            }
        }
    }
    
    public void LoadGameSettingsFromJson()
    {
        ApplicationManager.EnsureInstanceExists();
        string filePath = string.Empty;
        bool hasExplicitRuntimeSaveContext = ApplicationManager.Instance.HasExplicitSaveContext &&
                                             !string.IsNullOrEmpty(ApplicationManager.Instance.GetLastSavedFilePath());
#if UNITY_EDITOR
        bool shouldForceEditorDirectPlaySave = !hasExplicitRuntimeSaveContext;
#else
        bool shouldForceEditorDirectPlaySave = false;
#endif

        // TODO: Replace the current ApplicationManager + PlayerPrefs + newest-file fallback chain
        // with a single explicit active-save identifier once Load Game is implemented properly.

        // Room direct-play in the editor should ignore stale PlayerPrefs/newest-file discovery and always
        // use the dedicated test save unless an upstream scene explicitly handed us a save path.
        if (hasExplicitRuntimeSaveContext)
        {
            string explicitSavePath = ApplicationManager.Instance.GetLastSavedFilePath();
            if (IsProtectedEditorFixturePath(explicitSavePath))
            {
                Debug.LogWarning($"Room ignored protected direct-play fixture from explicit save context: {explicitSavePath}");
            }
            else
            {
                filePath = explicitSavePath;
                Debug.Log($"Room loading explicit active save context: {filePath}");
            }
        }
        else if (shouldForceEditorDirectPlaySave)
        {
            string editorDirectPlayPath = GetEditorDirectPlaySavePath();
            if (!string.IsNullOrEmpty(editorDirectPlayPath))
            {
                filePath = editorDirectPlayPath;
                Debug.Log($"Room loading direct-play test save: {filePath}");
            }
            else
            {
                Debug.LogError("Room direct-play test save is missing. Aborting load instead of falling back to another save.");
                return;
            }
        }

        if (string.IsNullOrEmpty(filePath) && !shouldForceEditorDirectPlaySave)
        {
            string playerPrefsPath = PlayerPrefs.GetString("currentGameSettings", string.Empty);
            if (!string.IsNullOrEmpty(playerPrefsPath))
            {
                string resolvedPlayerPrefsPath = Path.IsPathRooted(playerPrefsPath)
                    ? playerPrefsPath
                    : Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), playerPrefsPath);

                if (IsProtectedEditorFixturePath(resolvedPlayerPrefsPath))
                {
                    Debug.LogWarning($"Room ignored protected direct-play fixture from PlayerPrefs: {resolvedPlayerPrefsPath}");
                }
                else
                {
                    filePath = resolvedPlayerPrefsPath;
                    Debug.Log($"Room loading PlayerPrefs save fallback: {filePath}");
                }
            }
        }

        if ((string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) && !shouldForceEditorDirectPlaySave)
        {
            string folderPath = ApplicationManager.Instance.GetSaveFolderPath();
            // Get JSON files in the folder
            string[] files = Directory.GetFiles(folderPath, "*.json");
            if (files.Length == 0)
            {
                Debug.LogWarning("No game settings files found in the persistent data path!");
                return;
            }

            // Get the most recent file
            string newestNonProtectedFile = files
                .Where(path => !IsProtectedEditorFixturePath(path))
                .OrderByDescending(File.GetCreationTime)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(newestNonProtectedFile))
            {
                Debug.LogWarning("No non-protected game settings files found in the persistent data path!");
                return;
            }

            filePath = newestNonProtectedFile;
            Debug.LogWarning($"Room fell back to newest save discovery: {filePath}");
        }

        // Never let Room mutate the canonical direct-play fixture. Always switch to a disposable
        // working copy before storing active save context or allowing any later writes.
        filePath = SwapProtectedFixtureForWorkingCopy(filePath);

        if (!string.IsNullOrEmpty(filePath))
        {
            ApplicationManager.Instance.SetActiveSaveFilePath(filePath);
        }

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON into the GameData class
            // gameData = JsonConvert.DeserializeObject<GameData>(json);
            gameData = JsonConvert.DeserializeObject<GameData>(json) ?? new GameData();
            if (gameData == null)
            {
                Debug.LogWarning("⚠️ gameData was NULL after loading JSON! Reinitializing...");
                gameData = new GameData();
            }
            // Convert dictionary-based rosters to list-based rosters for JSON compatibility
            if (gameData.rosters == null)
            {
                Debug.LogWarning("⚠️ gameData.rosters is NULL! Creating new rosters...");
                gameData.rosters = new Rosters();
            }
            if (gameData.stats == null)
            {
                gameData.stats = new Stats();
            }
            if (gameData.gameLog == null)
            {
                gameData.gameLog = new GameLog(gameData.stats);
            }
            else
            {
                gameData.gameLog.RebindStats(gameData.stats);
            }
            EnsureGameplayEventLogInitialized();
            // Debugging after loading JSON
            Debug.Log($"🔍 Loaded Home Roster: {JsonConvert.SerializeObject(gameData.rosters.home, Formatting.Indented)}");
            Debug.Log($"🔍 Loaded Away Roster: {JsonConvert.SerializeObject(gameData.rosters.away, Formatting.Indented)}");

            if (gameData != null && gameData.gameSettings != null)
            {
                Debug.Log("Game settings loaded successfully! Invoking OnGameSettingsLoaded!");
                // Trigger event or call methods to initialize gameplay settings
                OnGameSettingsLoaded?.Invoke();
                DebugGameSettings();
                // // Debugging loaded data
                // Debug.Log($"Loaded Home Team: {gameData.gameSettings.homeTeamName}");
                // Debug.Log($"Loaded Away Team: {gameData.gameSettings.awayTeamName}");
                // Debug.Log($"Loaded Home Kit: {gameData.gameSettings.homeKit}");
                // Debug.Log($"Loaded Away Kit: {gameData.gameSettings.awayKit}");
            }
            else
            {
                Debug.LogError("Failed to load game settings from the file!");
            }
        }
        else
        {
            Debug.LogWarning("Game settings file not found.");
        }
    }

    private string GetEditorDirectPlaySavePath()
    {
#if UNITY_EDITOR
        string directPlayPath = Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), EditorRoomDirectPlayTestSaveFileName);
        if (File.Exists(directPlayPath))
        {
            return directPlayPath;
        }

        Debug.LogWarning($"Room direct-play test save not found: {directPlayPath}");
#endif
        return string.Empty;
    }

    private string SwapProtectedFixtureForWorkingCopy(string filePath)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(filePath) || !IsProtectedEditorFixturePath(filePath))
        {
            return filePath;
        }

        string sourcePath = GetEditorDirectPlaySourceSavePath();
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            Debug.LogError($"Protected Room fixture is missing and cannot be cloned: {sourcePath}");
            return filePath;
        }

        string workingCopyFolderPath = Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), EditorRoomDirectPlayWorkingCopyFolderName);
        Directory.CreateDirectory(workingCopyFolderPath);

        string workingCopyPath = Path.Combine(workingCopyFolderPath, EditorRoomDirectPlayWorkingCopyFileName);
        File.Copy(sourcePath, workingCopyPath, true);
        Debug.Log($"Room cloned protected fixture into disposable working copy: {workingCopyPath}");
        return workingCopyPath;
#else
        return filePath;
#endif
    }

    private bool IsProtectedEditorFixturePath(string filePath)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        return string.Equals(
            Path.GetFileName(filePath),
            EditorRoomDirectPlayTestSaveFileName,
            StringComparison.OrdinalIgnoreCase);
#else
        return false;
#endif
    }

    private string GetEditorDirectPlaySourceSavePath()
    {
#if UNITY_EDITOR
        return Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), EditorRoomDirectPlayTestSaveFileName);
#else
        return string.Empty;
#endif
    }

    public bool IsPlayerInTeam(string playerName, bool isHomeTeam)
    {
        return isHomeTeam 
            ? gameData.rosters.home.ContainsKey(playerName) 
            : gameData.rosters.away.ContainsKey(playerName);
    }
    
    public void SetLastToken(PlayerToken inputToken)
    {
        if (inputToken == null)
        {
            Debug.LogError("SetLastToken called with null token!");
            return;
        }

        if (setPieceTakerExcludedFromNextTouch == inputToken)
        {
            Debug.LogWarning($"{inputToken.name} cannot be the next player to touch the ball after taking the set piece.");
            return;
        }

        // If the input token is already the last token, do nothing
        if (LastTokenToTouchTheBallOnPurpose == inputToken)
        {
            ClearSetPieceTakerNextTouchExclusion(inputToken);
            return;
        }

        // If there's no last token, simply set it
        if (LastTokenToTouchTheBallOnPurpose == null)
        {
            LastTokenToTouchTheBallOnPurpose = inputToken;
            ClearSetPieceTakerNextTouchExclusion(inputToken);
            return;
        }

        // If the new token is a teammate of the last token
        if (
            // TODO: Why are we checking the home team instead of the team in attack?
            LastTokenToTouchTheBallOnPurpose.isHomeTeam == inputToken.isHomeTeam
        )
        {
            PreviousTokenToTouchTheBallOnPurpose = LastTokenToTouchTheBallOnPurpose;
            LastTokenToTouchTheBallOnPurpose = inputToken;
            Debug.Log($"Set LastTokenToTouchTheBallOnPurpose to {LastTokenToTouchTheBallOnPurpose.playerName}");
            ClearSetPieceTakerNextTouchExclusion(inputToken);
        }
        else // New token is from the opposite team
        {
            PreviousTokenToTouchTheBallOnPurpose = null; // Reset previous token
            LastTokenToTouchTheBallOnPurpose = inputToken;
            ClearSetPieceTakerNextTouchExclusion(inputToken);
        }
    }

    public void AddGoal(string scorer, bool isHomeTeam, int minute, bool isPenalty, string assist = null, string minuteLabel = null)
    {
        GoalEvent goal = new GoalEvent { scorer = scorer, minute = minute, minuteLabel = minuteLabel, isPenalty = isPenalty, assist = assist };
        
        if (isHomeTeam)
            homeScorers.Add(goal);
        else
            awayScorers.Add(goal);

        RecordGameplayOutcome(
            "score.goal",
            "goal",
            "scored",
            details: CreateDetails(
                ("scorer", scorer),
                ("team", isHomeTeam ? "Home" : "Away"),
                ("minute", minute),
                ("minuteLabel", minuteLabel),
                ("isPenalty", isPenalty),
                ("assist", assist),
                ("homeScore", gameData?.stats?.homeTeamStats?.totalGoals ?? 0),
                ("awayScore", gameData?.stats?.awayTeamStats?.totalGoals ?? 0)));
        matchStatsUI.UpdateScorersDisplay();
    }
    
    private void DebugGameSettings()
    {
        // Debug Game Settings
        if (gameData.gameSettings != null)
        {
            Debug.Log("Game Settings:");
            // Debug.Log(JsonConvert.SerializeObject(gameData.gameSettings, Formatting.Indented));
        }
        else
        {
            Debug.LogError("Game settings are missing in the JSON file!");
        }

        // Debug Rosters
        if (gameData.rosters != null)
        {
            Debug.Log("Rosters:");
            Debug.Log("Home Team Roster:");
            // foreach (var player in gameData.rosters.home)
            // {
            //     Debug.Log($"Jersey {player.Key}. {player.Value.name}");
            //     Debug.Log($"Attributes: Pace: {player.Value.pace}, Dribbling: {player.Value.dribbling}, HighPass: {player.Value.highPass}, Resilience: {player.Value.resilience}");

            //     if (player.Value.aerial > 0 || player.Value.saving > 0 || player.Value.handling > 0)
            //     {
            //         Debug.Log($"(Goalkeeper) Aerial: {player.Value.aerial}, Saving: {player.Value.saving}, Handling: {player.Value.handling}");
            //     }
            // }

            Debug.Log("Away Team Roster:");
            // foreach (var player in gameData.rosters.away)
            // {
            //     Debug.Log($"Jersey {player.Key}: {player.Value.name}");
            //     Debug.Log($"Attributes: Pace: {player.Value.pace}, Dribbling: {player.Value.dribbling}, HighPass: {player.Value.highPass}, Resilience: {player.Value.resilience}");
            //     if (player.Value.aerial > 0 || player.Value.saving > 0 || player.Value.handling > 0)
            //     {
            //         Debug.Log($"(Goalkeeper) Aerial: {player.Value.aerial}, Saving: {player.Value.saving}, Handling: {player.Value.handling}");
            //     }
            // }
        }
        else
        {
            Debug.LogError("Rosters are missing in the JSON file!");
        }
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("MM: ");

        sb.Append($"currentState: {currentState}, ");
        sb.Append($"teamInAttack: {teamInAttack}, ");
        sb.Append($"clock: H{currentHalf} {GetClockDisplayText().Replace("\n", " ")}, ");
        sb.Append($"actionsHalf/total: {completedActionsThisHalf}/{totalCompletedActions}, ");
        if (isClockRunning) sb.Append("clockRunning, ");
        if (isPauseMenuOpen) sb.Append("pauseMenuOpen, ");
        if (isHalfExpired) sb.Append("halfExpired, ");
        if (isWaitingForExtraActionsRoll) sb.Append("waitingExtraActionsRoll, ");
        if (extraActionsDetermined) sb.Append($"extras: {extraActionsRemaining}/{extraActionsTotal}, ");
        if (attackHasPossession) sb.Append($"attackHasPossession, ");
        if (LastTokenToTouchTheBallOnPurpose != null) sb.Append($"LastTokenToTouchTheBallOnPurpose: {LastTokenToTouchTheBallOnPurpose.name}, ");
        if (PreviousTokenToTouchTheBallOnPurpose != null) sb.Append($"PreviousTokenToTouchTheBallOnPurpose: {PreviousTokenToTouchTheBallOnPurpose.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        if (goalkeeperReplacementRequired)
        {
            string teamName = goalkeeperReplacementTeamIsHome
                ? gameData?.gameSettings?.homeTeamName
                : gameData?.gameSettings?.awayTeamName;
            if (string.IsNullOrWhiteSpace(teamName))
            {
                teamName = goalkeeperReplacementTeamIsHome ? "Home" : "Away";
            }

            return $"{teamName}: goalkeeper sent off. Open substitutions and take off a playing outfielder to bring on the bench goalkeeper.";
        }

        if (emergencyGoalkeeperNominationRequired)
        {
            string teamName = emergencyGoalkeeperNominationTeamIsHome
                ? gameData?.gameSettings?.homeTeamName
                : gameData?.gameSettings?.awayTeamName;
            if (string.IsNullOrWhiteSpace(teamName))
            {
                teamName = emergencyGoalkeeperNominationTeamIsHome ? "Home" : "Away";
            }

            return $"{teamName}: goalkeeper sent off. No bench goalkeeper substitution is available; nominate a playing outfielder as emergency goalkeeper.";
        }

        if (!isWaitingForExtraActionsRoll)
        {
            return string.Empty;
        }

        string attackingTeamName = teamInAttack == TeamInAttack.Home
            ? gameData?.gameSettings?.homeTeamName
            : gameData?.gameSettings?.awayTeamName;
        if (string.IsNullOrWhiteSpace(attackingTeamName))
        {
            attackingTeamName = "Attacking team";
        }

        return $"Half {currentHalf} regulation time is up. {attackingTeamName}: press [R] to roll a normal D6 for stoppage actions. Referee leniency: {refereeLeniency}.";
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (goalkeeperReplacementRequired)
        {
            return goalkeeperReplacementTeamIsHome;
        }

        if (emergencyGoalkeeperNominationRequired)
        {
            return emergencyGoalkeeperNominationTeamIsHome;
        }

        if (!isWaitingForExtraActionsRoll)
        {
            return null;
        }

        return teamInAttack == TeamInAttack.Home;
    }

  
}
