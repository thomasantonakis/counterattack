using System.Collections.Generic;

public enum RoomActionType
{
    Unknown,
    StartMovement,
    GroundPass,
    HighPass,
    Header,
    LongBall,
    FirstTimePass,
    Shot,
    ContinueWithoutShot,
    GoalkeeperKick,
    Roll,
    Confirm,
    SelectToken,
    SelectHex,
    Decline,
    SetupMove,
    ThrowIn,
    Tackle,
    Nutmeg,
    FoulAdvantage,
    Foul,
    GoalkeeperSave,
    Block,
    QuickThrow,
    FinalThird,
    FreeKick,
    Kickoff,
    PenaltyKick,
    PenaltyShootout
}

public enum RoomDecisionStep
{
    Unknown,
    ChooseActionType,
    ChooseActor,
    ChooseTarget,
    Confirm,
    Roll,
    Setup,
    InterruptionChoice
}

public sealed class RoomActionCandidate
{
    public string id;
    public string manager;
    public RoomActionType actionType;
    public RoomDecisionStep step;
    public string label;
    public string key;
    public PlayerToken actor;
    public PlayerToken targetToken;
    public HexCell targetHex;
    public bool isExecutableNow;
    public bool isForfeit;
    public string executionCommand;
    public string reason;
}

public sealed class RoomDecisionContext
{
    public string manager;
    public string expectedTeam;
    public string expectedInput;
    public readonly List<RoomActionCandidate> actions = new();
    public readonly List<string> validKeys = new();
    public readonly List<string> validActionSummaries = new();
    public readonly List<PlayerToken> validTokens = new();
    public readonly List<HexCell> validHexes = new();

    public void AddAction(RoomActionCandidate action)
    {
        if (action == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(action.manager))
        {
            action.manager = manager;
        }

        if (string.IsNullOrWhiteSpace(action.id))
        {
            action.id = BuildActionId(action);
        }

        if (!ContainsAction(action.id))
        {
            actions.Add(action);
        }

        if (!string.IsNullOrWhiteSpace(action.key))
        {
            AddKey(action.key);
        }

        AddToken(action.actor);
        AddToken(action.targetToken);
        AddHex(action.targetHex);
        AddActionSummary(action.label);
    }

    public void AddKeyActionCandidate(
        string managerName,
        RoomActionType actionType,
        RoomDecisionStep step,
        string key,
        string label,
        bool isExecutableNow = true,
        string executionCommand = "",
        bool isForfeit = false)
    {
        AddAction(new RoomActionCandidate
        {
            manager = managerName,
            actionType = actionType,
            step = step,
            key = key,
            label = label,
            isExecutableNow = isExecutableNow,
            executionCommand = executionCommand,
            isForfeit = isForfeit
        });
    }

    public void AddTokenActionCandidate(
        string managerName,
        RoomActionType actionType,
        RoomDecisionStep step,
        PlayerToken token,
        string label,
        bool isExecutableNow = true,
        string executionCommand = "")
    {
        AddAction(new RoomActionCandidate
        {
            manager = managerName,
            actionType = actionType,
            step = step,
            actor = token,
            label = label,
            isExecutableNow = isExecutableNow,
            executionCommand = executionCommand
        });
    }

    public void AddTargetTokenActionCandidate(
        string managerName,
        RoomActionType actionType,
        RoomDecisionStep step,
        PlayerToken token,
        string label,
        bool isExecutableNow = true,
        string executionCommand = "")
    {
        AddAction(new RoomActionCandidate
        {
            manager = managerName,
            actionType = actionType,
            step = step,
            targetToken = token,
            label = label,
            isExecutableNow = isExecutableNow,
            executionCommand = executionCommand
        });
    }

    public void AddHexActionCandidate(
        string managerName,
        RoomActionType actionType,
        RoomDecisionStep step,
        HexCell hex,
        string label,
        bool isExecutableNow = true,
        string executionCommand = "")
    {
        AddAction(new RoomActionCandidate
        {
            manager = managerName,
            actionType = actionType,
            step = step,
            targetHex = hex,
            label = label,
            isExecutableNow = isExecutableNow,
            executionCommand = executionCommand
        });
    }

    public void AddKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && !validKeys.Contains(key))
        {
            validKeys.Add(key);
        }
    }

    public void AddKeyAction(string key, string summary)
    {
        AddKey(key);
        AddActionSummary(summary);
    }

    public void AddToken(PlayerToken token)
    {
        if (token != null && !validTokens.Contains(token))
        {
            validTokens.Add(token);
        }
    }

    public void AddActionSummary(string summary)
    {
        if (!string.IsNullOrWhiteSpace(summary) && !validActionSummaries.Contains(summary))
        {
            validActionSummaries.Add(summary);
        }
    }

    public void AddHex(HexCell hex)
    {
        if (hex != null && !validHexes.Contains(hex))
        {
            validHexes.Add(hex);
        }
    }

    private bool ContainsAction(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        foreach (RoomActionCandidate action in actions)
        {
            if (action != null && action.id == id)
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildActionId(RoomActionCandidate action)
    {
        string managerKey = NormalizeIdPart(action.manager);
        string keyPart = NormalizeIdPart(action.key);
        string actorKey = BuildTokenKey(action.actor);
        string targetTokenKey = BuildTokenKey(action.targetToken);
        string targetHexKey = BuildHexKey(action.targetHex);
        string executionKey = NormalizeIdPart(action.executionCommand);
        return string.Join(
            "|",
            managerKey,
            action.actionType,
            action.step,
            $"key={keyPart}",
            $"actor={actorKey}",
            $"targetToken={targetTokenKey}",
            $"targetHex={targetHexKey}",
            $"exec={executionKey}");
    }

    private static string BuildTokenKey(PlayerToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }

        string team = token.isHomeTeam ? "home" : "away";
        return $"{team}-{token.jerseyNumber}";
    }

    private static string BuildHexKey(HexCell hex)
    {
        return hex == null ? string.Empty : $"{hex.coordinates.x},{hex.coordinates.z}";
    }

    private static string NormalizeIdPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Replace("|", "/");
    }
}
