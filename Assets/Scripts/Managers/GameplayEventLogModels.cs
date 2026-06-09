using System;
using System.Collections.Generic;

[Serializable]
public class GameplayEvent
{
    public const int CurrentSchemaVersion = 6;

    public int schemaVersion = CurrentSchemaVersion;
    public int sequenceNumber;
    public string timestampUtc;
    public int currentHalf;
    public string clockDisplay;
    public int matchClockSeconds;
    public string currentGameState;
    public string teamInAttack;
    public bool attackHasPossession;
    public string kind;
    public string actorTokenKey;
    public List<string> relatedTokenKeys = new();
    public RoomHexCoordinates sourceHex;
    public RoomHexCoordinates targetHex;
    public GameplayInputEvent input;
    public GameplayDiceEvent dice;
    public GameplayChoiceEvent choice;
    public GameplayActionPreview actionPreview;
    public GameplayAvailableActions availableActions;
    public GameplayMovementPath movementPath;
    public GameplayInstructionSnapshot instruction;
    public GameplayEventResult result;
    public string preStateHash;
    public string postStateHash;
    public GameplaySnapshotSummary snapshot;
    public string label;
}

[Serializable]
public class GameplayInstructionSnapshot
{
    public bool isAwaitingInput;
    public string manager;
    public string instructionText;
    public string expectedTeam;
    public string instructionSide;
    public string expectedInput;
    public List<string> expectedKeys = new();
    public RoomHexCoordinates currentTargetHex;
    public string actorTokenKey;
    public List<string> relatedTokenKeys = new();
    public Dictionary<string, string> details = new();
}

[Serializable]
public class GameplayInputEvent
{
    public string inputType;
    public string key;
    public string chord;
    public bool shift;
    public bool ctrl;
    public bool alt;
    public bool consumed;
    public string consumedBy;
    public string consumedTeam;
    public string button;
}

[Serializable]
public class GameplayChoiceEvent
{
    public string choiceId;
    public string prompt;
    public string teamSide;
    public string actorTokenKey;
    public string selectedKey;
    public string selectedAction;
    public List<GameplayChoiceOption> options = new();
}

[Serializable]
public class GameplayChoiceOption
{
    public string key;
    public string action;
    public string label;
}

[Serializable]
public class GameplayDiceEvent
{
    public string context;
    public string die = "d6";
    public int roll;
    public bool isJackpot;
    public bool jackpotEnabled = true;
    public bool overrideUsed;
    public int randomRoll;
    public bool randomJackpot;
    public Dictionary<string, string> modifiers = new();
}

[Serializable]
public class GameplayActionPreview
{
    public string action;
    public string phase;
    public bool isValid;
    public string failureReason;
    public bool willCommit;
    public RoomHexCoordinates targetHex;
    public string targetTokenKey;
    public int imposedMaxDistance;
    public int targetHexStepDistance;
    public bool isDangerous;
    public int pathHexCount;
    public int pathInteractionCount;
    public int interceptionAttemptCount;
    public int outfieldInterceptionCount;
    public int rollInteractionCount;
    public bool hasConditionalGoalkeeperInteraction;
    public List<GameplayPathInteractionPreview> pathInteractions = new();
}

[Serializable]
public class GameplayAvailableActions
{
    public string reason;
    public string state;
    public int difficulty;
    public int groundPassMaxDistance;
    public List<GameplayAvailableAction> actions = new();
}

[Serializable]
public class GameplayAvailableAction
{
    public string action;
    public string key;
    public bool available;
    public bool autoCommitOnSelection;
    public string selectionMode;
    public int imposedMaxDistance;
}

[Serializable]
public class GameplayPathInteractionPreview
{
    public string type;
    public string defenderTokenKey;
    public RoomHexCoordinates defenderHex;
    public RoomHexCoordinates interactionHex;
    public int pathIndex;
    public int tackling;
    public int saving;
    public int gkPenalty;
    public int requiredRoll;
    public bool requiresRoll;
    public bool isDefinite;
    public int distanceFromBall;
}

[Serializable]
public class GameplayMovementPath
{
    public bool isDribble;
    public bool carriedBall;
    public bool countedForDistance;
    public int stepCount;
    public List<RoomHexCoordinates> hexes = new();
}

[Serializable]
public class GameplayEventResult
{
    public string action;
    public string outcome;
    public Dictionary<string, string> details = new();
}

[Serializable]
public class GameplayDiceRollResult
{
    public string context;
    public int roll;
    public bool isJackpot;
    public bool jackpotEnabled;
    public bool overrideUsed;
    public int randomRoll;
    public bool randomJackpot;
}

[Serializable]
public class GameplaySnapshotSummary
{
    public int sequenceNumber;
    public RoomHexCoordinates ballHex;
    public List<GameplayTokenStateSummary> tokens = new();
    public GameplayScoreSnapshot score = new();
    public string currentState;
    public string possession;
    public bool attackHasPossession;
    public int currentHalf;
    public string clockDisplay;
    public int matchClockSeconds;
}

[Serializable]
public class GameplayTokenStateSummary
{
    public string tokenKey;
    public RoomHexCoordinates hex;
    public string status;
}

[Serializable]
public class GameplayScoreSnapshot
{
    public int home;
    public int away;
}
