using CounterAttack.Online;
using UnityEngine;
public sealed partial class CoinTossPanelController
{
    private bool onlineChoice;
    private bool nativeProjectionVisible;
    private float lastOnlineTick;
    private string timedRollStage;
    private float onlineRollRemaining;
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
        OnlineCoinTossSnapshot toss, bool ownsTurn, bool viewOnly = false)
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
        PresentDie(dicePanel.homeDie, toss.homeRoll, toss.stage == "home");
        PresentDie(dicePanel.awayDie, toss.awayRoll, toss.stage == "away");
        dicePanel.outcome.text = choosing ? $"{(toss.winnerHome ? home : away)} wins the toss"
            : toss.stage == "result" && toss.homeRoll == toss.awayRoll ? "Tie · Both managers roll again"
            : "Highest roll chooses";
        dicePanel.footer.text = choosing ? $"{team} manager · {(ownsTurn ? "Click your choice" : "Choosing")}"
            : toss.stage == "result" ? "Waiting for the result"
            : $"{team} manager · {(ownsTurn ? "Press [R] to roll" : "Waiting for roll")}";
        if (toss.rollSeconds.HasValue && toss.stage is "home" or "away")
            dicePanel.footer.text += $" · Auto-roll in {toss.rollSeconds.Value}s";
        choices.SetActive(choosing && !viewOnly);
        kickoffButton.gameObject.SetActive(toss.canChooseKickoff);
        kickoffButton.interactable = choosing && ownsTurn && toss.canChooseKickoff;
        leftToRightButton.interactable = rightToLeftButton.interactable = choosing && ownsTurn;
        SetLabel(kickoffButton, penalties ? "Take the First Penalty" : "Take Kick Off");
        SetLabel(leftToRightButton, penalties ? "Left Side" : "Attack Left to Right");
        SetLabel(rightToLeftButton, penalties ? "Right Side" : "Attack Right to Left");
        dicePanel.panel.sizeDelta = new Vector2(1440, choosing && !viewOnly ? 1080 : 720);
        SetTop(dicePanel.footer.rectTransform, choosing && !viewOnly ? 995 : 625);
        dicePanel.Fit();
    }
    internal static void PresentDie(DuelDieView die, int roll, bool shouldAnimate)
    {
        if (shouldAnimate)
        {
            if (!die.IsAnimating) die.Begin(false);
        }
        else if (roll > 0) die.Resolve(roll, false);
        else die.Clear();
    }

    public void TickOnline(bool connectionHeld)
    {
        if(!FullOnlineRoom || OnlineRoomSession.Current?.HasInitialData!=true || OnlineRoomSession.IsGuest || !dicePanel.roomMatch.IsOnlineAuthorityControlled || !IsOpen)
        {
            timedRollStage = null;
            lastOnlineTick = Time.unscaledTime;
            return;
        }
        float now=Time.unscaledTime,elapsed=Mathf.Max(0,now-lastOnlineTick);lastOnlineTick=now;
        bool held=connectionHeld || dicePanel.roomMatch.IsPauseMenuOpen;
        dicePanel.TickOnlineResult(now,elapsed,held);
        if ((Purpose is CoinTossPurpose.FirstHalf or CoinTossPurpose.PenaltyShootout) && dicePanel.OnlineStage is "home" or "away")
        {
            string stage = dicePanel.OnlineStage;
            if (stage != timedRollStage)
            {
                timedRollStage = stage;
                onlineRollRemaining = DecisionTimingConfig.AutomaticRollSeconds;
            }
            else if (!held)
            {
                onlineRollRemaining = Mathf.Max(0, onlineRollRemaining - elapsed);
                if (onlineRollRemaining == 0)
                    dicePanel.TryRollOnline();
            }
        }
        else timedRollStage = null;
        if(held)return;
        onlineChoice=true;try{AdvanceChoices();choices.SetActive(false);}finally{onlineChoice=false;}
    }
    public OnlineCoinTossSnapshot CaptureOnline()
    {
        if(!IsOpen)return null;
        return new OnlineCoinTossSnapshot{purpose=Purpose.ToString(),stage=dicePanel.OnlineStage,homeRoll=dicePanel.OnlineHomeScore,awayRoll=dicePanel.OnlineAwayScore,
            rollSeconds = timedRollStage == dicePanel.OnlineStage ? Mathf.CeilToInt(onlineRollRemaining) : (int?)null,
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
