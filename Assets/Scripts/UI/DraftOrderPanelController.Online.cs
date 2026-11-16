using CounterAttack.Online;
public sealed partial class DraftOrderPanelController
{
    private bool onlineRoll,onlineProgress;
    private bool OnlineRoomControlled=>roomMatch?.gameData?.gameSettings?.gameMode==ApplicationManager.OnlineGameMode;
    public int OnlineHomeScore=>homeScore;
    public int OnlineAwayScore=>awayScore;
    public string OnlineStage=>stage==Stage.Home?"home":stage==Stage.Away?"away":stage==Stage.Result?"result":stage==Stage.Choice?"choice":"closed";
    public bool TryRollOnline()
    {
        if(!OnlineRoomControlled || OnlineRoomSession.Current?.HasInitialData!=true || OnlineRoomSession.IsGuest || !roomMatch.IsOnlineAuthorityControlled || roomMatch.IsPauseMenuOpen || stage is not (Stage.Home or Stage.Away))return false;
        onlineRoll=true;try{Roll();}finally{onlineRoll=false;}return true;
    }
    public void TickOnlineResult(float now,float elapsed,bool held)
    {
        if(!OnlineRoomControlled || OnlineRoomSession.Current?.HasInitialData!=true || OnlineRoomSession.IsGuest || !roomMatch.IsOnlineAuthorityControlled)return;
        onlineProgress=true;
        try{if(held && stage==Stage.Result)resultUntil+=elapsed;else if(!held)AdvanceResult(now);}
        finally{onlineProgress=false;}
    }
}
