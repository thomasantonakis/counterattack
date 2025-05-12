using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;  // For JsonConvert
using System.Linq;
using System.Text;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using UnityEngine.Android;

public class MatchManager : MonoBehaviour
{
    // Define the possible game states
    public enum GameState
    {
        KickOffSetup, // Free movements of Players in each own Half
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
        QuickThrow,
        ActivateFinalThirdsAfterSave,
        GoalKick,
    }
    
    [Serializable]
    public class GameData
    {
        public GameSettings gameSettings;
        public Rosters rosters;
        public Stats stats;
        public GameLog gameLog;

        public GameData()
        {
            gameSettings = new GameSettings();
            rosters = new Rosters();
            stats = new Stats();
            gameLog = new GameLog(stats);
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
        public int interceptionsAttempted;
        public int interceptionsMade;
        public int aerialChallengesInvolved;
        public int aerialChallengesWon;
        public int attemptsFaced;
        public int attemptsSaved;
        public int yellowCards;
        public int redCards;
        public int injuries;
        public PlayerStats()
        {
            goals = 0;
            shotsAttempted = 0;
            shotsOnTarget = 0;
            shotsBlocked = 0;
            shotsOffTarget = 0;
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
            interceptionsAttempted = 0;
            interceptionsMade = 0;
            aerialChallengesInvolved = 0;
            aerialChallengesWon = 0;
            attemptsFaced = 0;
            attemptsSaved = 0;
            yellowCards = 0;
            redCards = 0;
            injuries = 0;
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
        public int totalPassesAttempted;
        public int totalPassesCompleted;
        public int totalAerialPassesAttempted;
        public int totalAerialPassesTargeted;
        public int totalAerialPassesCompleted;
        public int totalPacesRan;
        public int totalGroundDuelsInvolved;
        public int totalGroundDuelsWon;
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

        public TeamStats()
        {
            totalGoals = 0;
            totalShots = 0;
            totalShotsOnTarget = 0;
            totalShotsBlocked = 0;
            totalShotsOffTarget = 0;
            totalPassesAttempted = 0;
            totalPassesCompleted = 0;
            totalAerialPassesAttempted = 0;
            totalAerialPassesTargeted = 0;
            totalAerialPassesCompleted = 0;
            totalPacesRan = 0;
            totalGroundDuelsInvolved = 0;
            totalGroundDuelsWon = 0;
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
        }
        public void Reset()
        {
            totalGoals = 0;
            totalShots = 0;
            totalShotsOnTarget = 0;
            totalShotsBlocked = 0;
            totalShotsOffTarget = 0;
            totalPassesAttempted = 0;
            totalPassesCompleted = 0;
            totalAerialPassesAttempted = 0;
            totalAerialPassesTargeted = 0;
            totalAerialPassesCompleted = 0;
            totalPacesRan = 0;
            totalGroundDuelsInvolved = 0;
            totalGroundDuelsWon = 0;
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
        }
        public void AddPlayerStats(PlayerStats stats)
        {
            totalGoals += stats.goals;
            totalShots += stats.shotsAttempted;
            totalShotsOnTarget += stats.shotsOnTarget;
            totalShotsBlocked += stats.shotsBlocked;
            totalShotsOffTarget += stats.shotsOffTarget;
            totalPassesAttempted += stats.passesAttempted;
            totalPassesCompleted += stats.passesCompleted;
            totalAerialPassesAttempted += stats.aerialPassesAttempted;
            totalAerialPassesTargeted += stats.aerialPassesTargeted;
            totalAerialPassesCompleted += stats.aerialPassesCompleted;
            totalPacesRan += stats.pacesRan;
            totalGroundDuelsInvolved += stats.groundDuelsInvolved;
            totalGroundDuelsWon += stats.groundDuelsWon;
            totalInterceptionsAttempted += stats.interceptionsAttempted;
            totalInterceptionsMade += stats.interceptionsMade;
            totalAerialChallengesInvolved += stats.aerialChallengesInvolved;
            totalAerialChallengesWon += stats.aerialChallengesWon;
        }
    }

    [Serializable]
    public class GameLog
    {
        private List<string> gameLog;
        private Stats stats; // Add this
        public GameLog(Stats statsRef)
        {
            gameLog = new List<string>();
            this.stats = statsRef ?? throw new ArgumentNullException(nameof(statsRef)); // Ensure stats is never null
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
                    logEntry += "has a shot blocked";
                    playerStats.shotsBlocked += value;
                    teamStats.totalShotsBlocked += value;
                    connectedPlayerStats.interceptionsMade += value;
                    connectedTeamStats.totalInterceptionsMade += value;
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
                    MatchManager.Instance.AddGoal(
                        token.playerName
                        , token.isHomeTeam
                        // , GetCurrentMinute()
                        , 10
                        , false
                        , MatchManager.Instance.PreviousTokenToTouchTheBallOnPurpose?.playerName
                    );
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
                    teamStats.totalGroundDuelsInvolved += value;
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
                    break;

                case ActionType.AerialChallengeAttempt:
                    logEntry += $"engages in an aerial challenge with {connectedToken.name}";
                    playerStats.aerialChallengesInvolved += value;
                    teamStats.totalAerialChallengesInvolved += value;
                    connectedPlayerStats.aerialChallengesInvolved += value;
                    connectedTeamStats.totalAerialChallengesInvolved += value;
                    break;

                case ActionType.AerialChallengeWon:
                    logEntry += "wins an aerial challenge" + ((connectedToken!= null) ? $"from {connectedToken.name}" : "");
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
                            playerStats.possessionWon += value;
                            teamStats.totalPossessionWon += value;
                            break;
                        case "loose":
                            logEntry += "Saved for a loose ball";
                            playerStats.attemptsSaved += value;
                            break;
                        case "corner":
                            logEntry += "Saved for corner Kick";
                            playerStats.attemptsSaved += value;
                            connectedTeamStats.totalCorners += value;
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
                    break;

                default:
                    logEntry += "performs an unknown action";
                    break;
            }

            gameLog.Add(logEntry);
            Debug.Log("[Game Log] " + logEntry);
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
        public bool isPenalty;
        public string assist;  // Optional

        public override string ToString()
        {
            if (isPenalty)
                return $"{scorer} {minute}'(p)";
            if (!string.IsNullOrEmpty(assist))
                return $"{scorer} {minute}' (A: {assist})";
            return $"{scorer} {minute}'";
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
    public PlayerTokenManager playerTokenManager;
    public MatchStatsUI matchStatsUI;
    public GameData gameData;
    // public PlayerToken LastTokenToTouchTheBallOnPurpose { get; private set; }
    // public PlayerToken PreviousTokenToTouchTheBallOnPurpose { get; private set; }
    public PlayerToken LastTokenToTouchTheBallOnPurpose;
    public PlayerToken PreviousTokenToTouchTheBallOnPurpose;
    public string hangingPassType;
    public int difficulty_level;
    public int refereeLeniency;
    public bool isFTPAvailable = false;

    // // Define other match-specific variables here (e.g., time, score, teams)
    // private int homeScore = 0;
    // private int awayScore = 0;

    private void Awake()
    {
        Debug.Log("MatchManager Awake() - Starting Initialization");
        Debug.Log($"⚠️ MatchManager Awake() - Instance ID: {GetInstanceID()}");
        
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Keep MatchManager persistent
        }
        else
        {
            Debug.LogWarning("⚠️ Duplicate MatchManager detected! Destroying new instance.");
            Destroy(gameObject);
            return;
        }
    }

    IEnumerator Start()
    {
        Debug.Log($"⚠️ MatchManager Start() - Instance ID: {GetInstanceID()}");
        LoadGameSettingsFromJson();
        yield return new WaitUntil(() => gameData != null && gameData.rosters != null && gameData.rosters.home.Count > 10 && gameData.rosters.away.Count > 10);
        if (gameData != null && gameData.gameSettings != null)
        {
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

        if (gameData.gameLog == null)
        {
            Debug.LogWarning("⚠️ gameData.gameLog was NULL in Start! Reinitializing...");
            gameData.gameLog = new GameLog(gameData.stats);  // Now `stats` is guaranteed to exist
        }
        Debug.Log($"🔍 Home Roster: {JsonConvert.SerializeObject(gameData.rosters.home, Formatting.Indented)}");
        Debug.Log($"🔍 Away Roster: {JsonConvert.SerializeObject(gameData.rosters.away, Formatting.Indented)}");
        Debug.Log($"🔍 GameLog: {JsonConvert.SerializeObject(gameData.gameLog, Formatting.Indented)}");
        // Debug.Log($"✅ Final gameLog: {JsonUtility.ToJson(gameData.gameLog, true)}");
        // Initialize the match in the KickOffSetup state
        currentState = GameState.KickOffSetup;
        Debug.Log("Game initialized in KickOffSetup state.");
        // Initialize the attacking team and direction
        teamInAttack = TeamInAttack.Home;  // Home team starts with the ball
        homeTeamDirection = TeamAttackingDirection.LeftToRight;  // Set home team attacking direction to LeftToRight
        awayTeamDirection = TeamAttackingDirection.RightToLeft;  // Away team will attack in the opposite direction
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
        if (currentState == GameState.KickOffSetup && keyData.key == KeyCode.Space)
        {
            StartMatch();
        }
    }

    // Example method to start the match
    public void StartMatch()
    {
        currentState = GameState.KickoffBlown;
        groundBallManager.isAvailable = true;
        highPassManager.isAvailable = true;
        longBallManager.isAvailable = true;
        LastTokenToTouchTheBallOnPurpose = ball.GetCurrentHex().GetOccupyingToken();
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

    }

    public void UpdatePossessionAfterPass(HexCell ballHex)
    {
        List<HexCell> attackerHexes = hexGrid.GetAttackerHexes();

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
    }

    // Method to trigger the standard pass attempt mode (on key press, like "P")
    public void TriggerStandardPass()
    {  
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass();
        longBallManager.CleanUpLongBall();
        RefreshAvailableActions();
        groundBallManager.ActivateGroundBall();
    }

    public void TriggerMovement()
    {
        // All these resets are in case it is not comitted
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass();
        longBallManager.CleanUpLongBall();
        RefreshAvailableActions();
        movementPhaseManager.ActivateMovementPhase();
    }

    public void TriggerHighPass()
    {
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass();
        longBallManager.CleanUpLongBall();
        RefreshAvailableActions();
        highPassManager.ActivateHighPass();
    }
    
    public void TriggerLongPass()
    {
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass();
        longBallManager.CleanUpLongBall();
        RefreshAvailableActions();
        longBallManager.ActivateLongBall();
    }

    public void TriggerFTP()
    {
        movementPhaseManager.ResetMovementPhase();
        groundBallManager.CleanUpPass();
        firstTimePassManager.CleanUpFTP();
        highPassManager.CleanUpHighPass();
        longBallManager.CleanUpLongBall();
        RefreshAvailableActions();
        firstTimePassManager.ActivateFTP();
    }

    public void CommitToAction()
    {
        movementPhaseManager.isAvailable = false;
        groundBallManager.isAvailable = false;
        firstTimePassManager.isAvailable = false;
        highPassManager.isAvailable = false;
        longBallManager.isAvailable = false;
        shotManager.isAvailable = false;
        isFTPAvailable = false;
    }

    public void BroadcastSafeEndofMovementPhase()
    {
        currentState = GameState.EndOfMovementPhase;
        RefreshAvailableActions();
    }
    public void BroadcastSuccessfulTackle()
    {
        currentState = GameState.SuccessfulTackle;
        RefreshAvailableActions();
    }
    
    public void BroadcastEndofGroundBallPass()
    {
        currentState = GameState.EndOfStandardPass;
        RefreshAvailableActions();
    }

    public void BroadcastEndofFirstTimePass()
    {
        currentState = GameState.EndOfFirstTimePass;
        RefreshAvailableActions();
    }
    
    public void BroadcastEndOfLongBall()
    {
        currentState = GameState.EndOfLongBall;
        RefreshAvailableActions();
    }
    
    public void BroadcastAnyOtherScenario()
    {
        currentState = GameState.AnyOtherScenario;
        RefreshAvailableActions();
    }

    public void BroadcastBallControl()
    {
        currentState = GameState.BallControl;
        RefreshAvailableActions();
    }


    public void BroadcastQuickThrow()
    {
        currentState = GameState.QuickThrow;
        RefreshAvailableActions();
    }
    public void BroadcastActivateFinalThirdsAfterSave()
    {
        currentState = GameState.ActivateFinalThirdsAfterSave;
        RefreshAvailableActions();
    }

    public void BroadcastHeaderCompleted()
    {
        currentState = GameState.HeaderCompleted;
        RefreshAvailableActions();
    }

    private void RefreshAvailableActions()
    {
        if (currentState == GameState.EndOfStandardPass)
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
                movementPhaseManager.ActivateMovementPhase();
                movementPhaseManager.CommitToAction();
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
                movementPhaseManager.ActivateMovementPhase();
                movementPhaseManager.CommitToAction();
            }
        }
        else if (currentState == GameState.EndOfLongBall)
        {
            movementPhaseManager.ActivateMovementPhase();
            movementPhaseManager.CommitToAction();
        }
        else if (currentState == GameState.EndOfFirstTimePass)
        {
            if (!attackHasPossession)
            {
                movementPhaseManager.ActivateMovementPhase();
                movementPhaseManager.CommitToAction();
            }
            else
            {
                if (ShouldShotBeAvailable()) shotManager.isAvailable = true;
                else
                {
                    movementPhaseManager.ActivateMovementPhase();
                    movementPhaseManager.CommitToAction();
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
                movementPhaseManager.ActivateMovementPhase();
                movementPhaseManager.CommitToAction();
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
                movementPhaseManager.ActivateMovementPhase();
                movementPhaseManager.CommitToAction();
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
            movementPhaseManager.isAvailable = false;
            groundBallManager.isAvailable = true;
            firstTimePassManager.isAvailable = false;
            highPassManager.isAvailable = false;
            longBallManager.isAvailable = false;
            shotManager.isAvailable = false;
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
    }

    private bool ShouldShotBeAvailable()
    {
        bool shoulShotBeAvailable = false;
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
                && ball.GetCurrentHex().CanShootFrom // Is in shooting distance
                && ball.GetCurrentHex().coordinates.x > 0 // In Right Side of Pitch
                && attackHasPossession // Ball is on an attacker
            )
            ||
            (
                attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attackers shoot to the Left
                && ball.GetCurrentHex().CanShootFrom // Is in shooting distance
                && ball.GetCurrentHex().coordinates.x < 0 // In Left Side of Pitch
                && attackHasPossession // Ball is on an attacker
            )
        )
        {
          shoulShotBeAvailable = true;
        }
        return shoulShotBeAvailable;
    }
    
    public void EnableFreeKickOptions()
    {
        movementPhaseManager.isAvailable = false;
        groundBallManager.isAvailable = true;
        firstTimePassManager.isAvailable = false;
        highPassManager.isAvailable = true;
        longBallManager.isAvailable = true;
        // TODO: Check if the ball is in CanShootFrom Hex
        shotManager.isAvailable = true;
    }
    
    public void EnableCornerKickOptions()
    {
        movementPhaseManager.isAvailable = false;
        groundBallManager.isAvailable = true;
        firstTimePassManager.isAvailable = false;
        highPassManager.isAvailable = true;
        longBallManager.isAvailable = false;
        shotManager.isAvailable = false;
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
        string filePath;
        // Check ApplicationManager for the most recent file
        if (ApplicationManager.Instance != null && !string.IsNullOrEmpty(ApplicationManager.Instance.LastSavedFileName))
        {
            filePath = Path.Combine(Path.Combine(Application.persistentDataPath, "SavedGames"), ApplicationManager.Instance.LastSavedFileName);
        }
        else
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "SavedGames");
            // Get JSON files in the folder
            string[] files = Directory.GetFiles(folderPath, "*.json");
            if (files.Length == 0)
            {
                Debug.LogWarning("No game settings files found in the persistent data path!");
                return;
            }

            // Get the most recent file
            var sortedFiles = files.OrderByDescending(File.GetCreationTime).ToArray();
            filePath = sortedFiles[0];
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

        // If the input token is already the last token, do nothing
        if (LastTokenToTouchTheBallOnPurpose == inputToken) return;

        // If there's no last token, simply set it
        if (LastTokenToTouchTheBallOnPurpose == null)
        {
            LastTokenToTouchTheBallOnPurpose = inputToken;
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
        }
        else // New token is from the opposite team
        {
            PreviousTokenToTouchTheBallOnPurpose = null; // Reset previous token
            LastTokenToTouchTheBallOnPurpose = inputToken;
        }
    }

    public void AddGoal(string scorer, bool isHomeTeam, int minute, bool isPenalty, string assist = null)
    {
        GoalEvent goal = new GoalEvent { scorer = scorer, minute = minute, isPenalty = isPenalty, assist = assist };
        
        if (isHomeTeam)
            homeScorers.Add(goal);
        else
            awayScorers.Add(goal);

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
        if (attackHasPossession) sb.Append($"attackHasPossession, ");
        if (LastTokenToTouchTheBallOnPurpose != null) sb.Append($"LastTokenToTouchTheBallOnPurpose: {LastTokenToTouchTheBallOnPurpose.name}, ");
        if (PreviousTokenToTouchTheBallOnPurpose != null) sb.Append($"PreviousTokenToTouchTheBallOnPurpose: {PreviousTokenToTouchTheBallOnPurpose.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

  
}
