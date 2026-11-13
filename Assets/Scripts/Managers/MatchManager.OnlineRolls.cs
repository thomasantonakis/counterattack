using System;
using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;

public partial class MatchManager
{
    private const int OnlineRollHistoryLimit = 64;
    private readonly Queue<OnlineRollPublication> onlineRollHistory = new();
    private GameData onlineRollData;

    private void PublishOnlineRoll(GameplayDiceRollResult result, PlayerToken actor)
    {
        if (!ReferenceEquals(onlineRollData, gameData)
            || onlineRollHistory.Count > 0 && onlineRollHistory.Last().sequence >= TimingRollSequence)
            ClearOnlineRollHistory();
        onlineRollData = gameData;
        onlineRollHistory.Enqueue(new OnlineRollPublication
        {
            sequence=TimingRollSequence, context=result.context ?? "unspecified",
            actorId=OnlineRollPublication.ActorId(actor), actorName=actor != null ? actor.playerName : null,
            team=actor == null ? null : actor.isHomeTeam ? "Home" : "Away",
            roll=result.roll, isJackpot=result.isJackpot, jackpotEnabled=result.jackpotEnabled
        });
        if (onlineRollHistory.Count > OnlineRollHistoryLimit) onlineRollHistory.Dequeue();
    }

    public OnlineRollPublication[] CaptureOnlineRolls() => !ReferenceEquals(onlineRollData, gameData)
        ? Array.Empty<OnlineRollPublication>() : onlineRollHistory.Select(result => result.Copy()).ToArray();

    private void ClearOnlineRollHistory()
    { onlineRollHistory.Clear(); onlineRollData = gameData; }
}
