using CounterAttack.Online;
using UnityEngine;
public sealed partial class CoinTossPanelController
{
    private bool onlineChoice;
    private float lastOnlineTick;
    private bool FullOnlineRoom=>dicePanel?.roomMatch?.gameData?.gameSettings?.gameMode==ApplicationManager.OnlineGameMode;
    public void TickOnline(bool connectionHeld)
    {
        if(!FullOnlineRoom || OnlineRoomSession.Current?.HasInitialData!=true || OnlineRoomSession.IsGuest || !dicePanel.roomMatch.IsOnlineAuthorityControlled || !IsOpen)return;
        float now=Time.unscaledTime,elapsed=Mathf.Max(0,now-lastOnlineTick);lastOnlineTick=now;
        bool held=connectionHeld || dicePanel.roomMatch.IsPauseMenuOpen;
        dicePanel.TickOnlineResult(now,elapsed,held);
        if(held)return;
        onlineChoice=true;try{AdvanceChoices();choices.SetActive(false);}finally{onlineChoice=false;}
    }
    public OnlineCoinTossSnapshot CaptureOnline()
    {
        if(!IsOpen)return null;
        return new OnlineCoinTossSnapshot{purpose=Purpose.ToString(),stage=dicePanel.OnlineStage,homeRoll=dicePanel.OnlineHomeScore,awayRoll=dicePanel.OnlineAwayScore,
            hasWinner=Decision!=null,winnerHome=Decision?.WinnerIsHome==true,choosingHome=Decision?.ChoosingHome==true,canChooseKickoff=Decision?.CanChooseKickoff==true};
    }
    public bool TryExecuteOnline(string action)
    {
        if(!FullOnlineRoom || OnlineRoomSession.Current?.HasInitialData!=true || OnlineRoomSession.IsGuest || !dicePanel.roomMatch.IsOnlineAuthorityControlled || dicePanel.roomMatch.IsPauseMenuOpen || !IsOpen)return false;
        if(action=="coin.roll")return dicePanel.TryRollOnline();
        if(Decision==null || Decision.IsComplete)return false;
        CoinTossChoice? choice=action switch{"coin.kickoff"=>CoinTossChoice.Kickoff,"coin.left"=>Purpose==CoinTossPurpose.PenaltyShootout?CoinTossChoice.LeftSide:CoinTossChoice.LeftToRight,
            "coin.right"=>Purpose==CoinTossPurpose.PenaltyShootout?CoinTossChoice.RightSide:CoinTossChoice.RightToLeft,_=>null};
        if(choice==null || choice==CoinTossChoice.Kickoff && !Decision.CanChooseKickoff)return false;
        onlineChoice=true;try{Choose(choice.Value);choices.SetActive(false);}finally{onlineChoice=false;}return true;
    }
}
