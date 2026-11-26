// Read-only authority seam; the ground-pass manager retains queue ownership.
public partial class GroundBallManager
{
    public HexCell PendingOnlineInterceptionHex => isWaitingForDiceRoll ? currentDefenderHex : null;
}
