[System.Serializable]
public sealed class RoomAIKickoffPlan
{
    public string team;
    public string takerTokenKey;
    public string receiverTokenKey;
    public RoomHexCoordinates landingHex;
    public RoomHexCoordinates receiverSetupHex;
    public RoomHexCoordinates takerSetupHex;
    public bool isSetupProjection;
    public bool requiresReceiverSetup;
    public bool firstPassEvaluated;
    public bool firstActionReady;
    public bool continuationEvaluated;
    public int collectionCost;
    public int remainingPace;
    public int remainingPaceThisMovement;
    public int collectionPhases;
    public int collectionMovesConsumed;
    public bool collectionFitsNextMovement;
    public int remainingAttackingMoves;
    public string secondAction;
    public RoomActionType secondActionType;
    public RoomHexCoordinates secondActionTargetHex;
    public string secondActionReceiverTokenKey;
    public string secondActionMoverTokenKey;
    public RoomActionType followingActionType;
    public RoomHexCoordinates followingActionTargetHex;
    public string followingActionReceiverTokenKey;
    public float passRetention = -1f;
    public float continuationSurvival = -1f;
    public float routeExpectedGoals = -1f;
    public float value;
    public string rationale;
}
