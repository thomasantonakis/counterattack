using CounterAttack.Online;
using UnityEngine;
public sealed partial class CoinTossPanelController
{
    private bool onlineChoice;
    private bool nativeProjectionVisible;
    private float lastOnlineTick;
    private bool FullOnlineRoom=>dicePanel?.roomMatch?.gameData?.gameSettings?.gameMode==ApplicationManager.OnlineGameMode;

    internal void BindNativeOnlineControls(OnlineGroundPassControls controls)
    {
        kickoffButton.onClick.AddListener(() => controls.TrySubmitAction("coin.kickoff"));
        leftToRightButton.onClick.AddListener(() => controls.TrySubmitAction("coin.left"));
        rightToLeftButton.onClick.AddListener(() => controls.TrySubmitAction("coin.right"));
    }

    internal void DisplayNativeOnline(OnlineGroundPassState state, OnlineTeam localTeam)
    {
        var toss = state.coin;
        if (toss == null || state.connectionHeld || state.isSharedPaused)
        {
            HidePublishedToss();
            return;
        }

        bool homeTurn = toss.stage == "choice" ? toss.choosingHome : toss.stage == "home";
        var settings = dicePanel.roomMatch.gameData.gameSettings;
        PresentPublishedToss(settings.homeTeamName, settings.awayTeamName, settings.homeKit, settings.awayKit,
            toss, homeTurn == (localTeam == OnlineTeam.Home));
    }

    internal void HidePublishedToss()
    {
        if (!nativeProjectionVisible) return;
        choices.SetActive(false);
        dicePanel.visuals.SetActive(false);
        nativeProjectionVisible = false;
    }

    internal void PresentPublishedToss(string home, string away, string homeKit, string awayKit,
        OnlineCoinTossSnapshot toss, bool ownsTurn)
    {
        if (!nativeProjectionVisible)
        {
            dicePanel.visuals.SetActive(true);
            dicePanel.visuals.transform.SetAsLastSibling();
            nativeProjectionVisible = true;
        }

        var homePalette = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(homeKit));
        var awayPalette = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(awayKit));
        dicePanel.homeDie.SetTeamColors(homePalette.Primary, homePalette.Secondary);
        dicePanel.awayDie.SetTeamColors(awayPalette.Primary, awayPalette.Secondary);
        bool penalties = toss.purpose == nameof(CoinTossPurpose.PenaltyShootout);
        bool choosing = toss.stage == "choice";
        bool homeTurn = choosing ? toss.choosingHome : toss.stage == "home";
        string team = homeTurn ? home : away;
        string round = penalties ? " · PENALTY SHOOTOUT" : toss.purpose == nameof(CoinTossPurpose.ExtraTime) ? " · EXTRA TIME" : "";
        dicePanel.title.text = "COIN TOSS FLIP" + round;
        dicePanel.homeTeam.text = home;
        dicePanel.awayTeam.text = away;
        dicePanel.homeResult.text = toss.homeRoll == 0 ? string.Empty : $"Roll {toss.homeRoll}";
        dicePanel.awayResult.text = toss.awayRoll == 0 ? string.Empty : $"Roll {toss.awayRoll}";
        if (toss.homeRoll != 0) dicePanel.homeDie.Resolve(toss.homeRoll, false);
        else dicePanel.homeDie.Clear();
        if (toss.awayRoll != 0) dicePanel.awayDie.Resolve(toss.awayRoll, false);
        else dicePanel.awayDie.Clear();
        dicePanel.outcome.text = choosing ? $"{(toss.winnerHome ? home : away)} wins the toss"
            : toss.stage == "result" && toss.homeRoll == toss.awayRoll ? "Tie · Both managers roll again"
            : "Highest roll chooses";
        dicePanel.footer.text = choosing ? $"{team} manager · {(ownsTurn ? "Click your choice" : "Choosing")}"
            : toss.stage == "result" ? "Waiting for the result"
            : $"{team} manager · {(ownsTurn ? "Press [R] to roll" : "Waiting for roll")}";
        choices.SetActive(choosing);
        kickoffButton.gameObject.SetActive(toss.canChooseKickoff);
        kickoffButton.interactable = choosing && ownsTurn && toss.canChooseKickoff;
        leftToRightButton.interactable = rightToLeftButton.interactable = choosing && ownsTurn;
        SetLabel(kickoffButton, penalties ? "Take the First Penalty" : "Take Kick Off");
        SetLabel(leftToRightButton, penalties ? "Left Side" : "Attack Left to Right");
        SetLabel(rightToLeftButton, penalties ? "Right Side" : "Attack Right to Left");
        dicePanel.panel.sizeDelta = new Vector2(1440, choosing ? 1080 : 720);
        SetTop(dicePanel.footer.rectTransform, choosing ? 995 : 625);
        dicePanel.Fit();
    }
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
