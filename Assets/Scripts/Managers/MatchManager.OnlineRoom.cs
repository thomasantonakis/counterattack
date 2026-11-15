using System;
using System.Linq;
using CounterAttack.Online;
using UnityEngine;
public partial class MatchManager
{
    public bool OnlineMatchComplete=>isMatchComplete && !isHalfTimeFlowRunning && !isTieBreakerTossRunning;
    private PenaltyShootoutManager onlineShootoutManager;
    public PenaltyShootoutManager OnlineShootoutManager=>onlineShootoutManager!=null?onlineShootoutManager:onlineShootoutManager=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PenaltyShootoutManager>(true)).FirstOrDefault();
    public RoomTokenSnapshot CaptureOnlineRosterToken(PlayerToken token)=>CreateTokenSnapshot(token);
    public bool OnlineExtraTimeSubstitutionCredit=>extraTimeSubstitutionCreditGranted;
    public void InitializeAuthoredOnlineMatch(GameData initial)
    {
        if(initial?.openingSetup==null || initial.gameSettings?.gameMode!=ApplicationManager.OnlineGameMode)throw new ArgumentException("An agreed online opening is required",nameof(initial));
        gameData=OnlineRoomLaunchData.Copy(initial);ClampLoadedMatchSettings();difficulty_level=gameData.gameSettings.playerAssistance;
        refereeLeniency=int.Parse(gameData.gameSettings.referee[^1].ToString());
        teamInAttack=gameData.openingSetup.KickoffTeam;firstHalfKickoffTeam=teamInAttack;attackHasPossession=true;
        homeTeamDirection=gameData.openingSetup.DirectionFor(true);awayTeamDirection=gameData.openingSetup.DirectionFor(false);
        currentState=GameState.KickOffSetup;InitializeSubstitutionCountsFromStats();EnsureGameplayEventLogInitialized();
        if(!OnlineRoomSession.IsGuest)ResetMatchClockForNewMatch();
        else{currentHalf=1;currentHalfRegulationSeconds=0;isClockRunning=false;TimingRollSequence=0;}
        ResolveClockDependencies();
        PlayerToken.SetBallReference(ball);OnGameSettingsLoaded?.Invoke();
    }
    public void ApplyOnlineReplicaMatchState(OnlineGroundPassState state)
    {
        if(!OnlineRoomSession.IsGuest)throw new InvalidOperationException("Only replicas apply published match state");
        if(Enum.TryParse(state.gameState,out GameState parsed))currentState=parsed;
        if(Enum.TryParse(state.possessionTeam,out TeamInAttack attack))teamInAttack=attack;
        isMatchComplete=state.matchComplete;
        attackHasPossession=state.attackHasPossession;currentHalf=state.half;currentHalfRegulationSeconds=state.regulationSeconds;isClockRunning=state.clockRunning;
        gameData.stats.homeTeamStats=JsonUtility.FromJson<TeamStats>(JsonUtility.ToJson(state.homeStats));
        gameData.stats.awayTeamStats=JsonUtility.FromJson<TeamStats>(JsonUtility.ToJson(state.awayStats));
        if(state.catalogId==OnlineFullRoomBootstrap.Catalog)
        {
            homeTeamDirection=Enum.Parse<TeamAttackingDirection>(state.homeDirection);awayTeamDirection=Enum.Parse<TeamAttackingDirection>(state.awayDirection);
            areSubstitutionsAvailable=state.substitutionsAvailable;substitutionsAvailabilityReason=state.substitutionsReason;
            extraTimeSubstitutionCreditGranted=state.extraTimeSubstitutionCredit;
            goalkeeperReplacementRequired=state.homeKeeperReplacement || state.awayKeeperReplacement;goalkeeperReplacementTeamIsHome=state.homeKeeperReplacement;
            emergencyGoalkeeperNominationRequired=state.homeEmergencyKeeper || state.awayEmergencyKeeper;emergencyGoalkeeperNominationTeamIsHome=state.homeEmergencyKeeper;
            emergencyGoalkeeperNominationReason=state.keeperActionReason;
            InitializeSubstitutionCountsFromStats();
        }
        BeginGameplayEventLoggingSuppression();
        try
        {
            foreach(var cell in hexGrid.cells){cell.occupyingToken=null;cell.isAttackOccupied=cell.isDefenseOccupied=false;}
            var tokens=playerTokenManager.AuthoredOnlineRoster??playerTokenManager.allTokens.Concat(playerTokenManager.benchTokens).ToArray();
            foreach(var token in tokens)token.SetCurrentHex(null);
            foreach(var item in state.tokens)
            {
                var token=tokens.FirstOrDefault(t=>OnlineRollPublication.ActorId(t)==item.id);if(token==null)continue;
                if(item.runtime!=null)
                {
                    var current=item.onBoard?hexGrid.GetHexCellAt(new Vector3Int(item.x,0,item.z)):null;
                    if(current!=null){current.isAttackOccupied=item.runtime.isAttacker;current.isDefenseOccupied=!item.runtime.isAttacker;}
                    bool wasKeeper=token.IsGoalKeeper;
                    token.RestoreRuntimeState(item.runtime,current,new Vector3(item.inactiveX,item.inactiveY,item.inactiveZ));
                    if(item.runtime.isGoalKeeper && !wasKeeper)ApplyTeamGoalkeeperKit(token);
                    token.gameObject.SetActive(item.visible);gameData.stats.GetPlayerStats(token);
                    gameData.stats.playerStats.First(entry=>entry.playerKey==GetStableTokenKey(token)).stats=JsonUtility.FromJson<PlayerStats>(JsonUtility.ToJson(item.playerStats));
                    continue;
                }
                if(!item.onBoard)continue;
                var hex=hexGrid.GetHexCellAt(new Vector3Int(item.x,0,item.z));if(hex==null)continue;
                token.isAttacker=token.isHomeTeam==(teamInAttack==TeamInAttack.Home);hex.isAttackOccupied=token.isAttacker;hex.isDefenseOccupied=!token.isAttacker;token.SetCurrentHex(hex);
            }
            OnlineShootoutManager?.ApplyOnlineShootout(state.shootout,tokens);
            if(state.ballOnBoard)ball.PlaceAtCell(hexGrid.GetHexCellAt(new Vector3Int(state.ballX,0,state.ballZ)));
            LastTokenToTouchTheBallOnPurpose=tokens.FirstOrDefault(t=>OnlineRollPublication.ActorId(t)==state.lastTouch);
            if(playerTokenManager.AuthoredOnlineRoster!=null)
            {
                playerTokenManager.allTokens=tokens.Where(t=>t.isPlaying && !t.isSentOff).ToList();
                playerTokenManager.benchTokens=tokens.Where(t=>!t.isPlaying && !t.isSentOff).ToList();
            }
        }
        finally{EndGameplayEventLoggingSuppression();}
        foreach(var item in state.tokens)
        {
            if(item.runtime!=null)continue;
            var token=playerTokenManager.allTokens.Concat(playerTokenManager.benchTokens).FirstOrDefault(t=>OnlineRollPublication.ActorId(t)==item.id);
            if(token==null)continue;
            var stats=gameData.stats.GetPlayerStats(token);stats.passesAttempted=item.passesAttempted;stats.passesCompleted=item.passesCompleted;
            stats.interceptionsAttempted=item.interceptionsAttempted;stats.interceptionsMade=item.interceptionsMade;stats.possessionWon=item.possessionWon;stats.possessionLost=item.possessionLost;
            stats.aerialPassesAttempted=item.aerialPassesAttempted;stats.aerialPassesTargeted=item.aerialPassesTargeted;stats.aerialPassesCompleted=item.aerialPassesCompleted;
        }
    }
}
