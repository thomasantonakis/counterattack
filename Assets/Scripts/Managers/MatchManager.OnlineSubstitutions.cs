using CounterAttack.Online;
using UnityEngine;
public partial class MatchManager
{
    public bool TryGetOnlineRequiredKeeperTeam(out OnlineTeam team)
    {
        foreach(bool home in new[]{true,false})
            if(IsGoalkeeperReplacementRequired(home) || IsForcedGoalkeeperSubstitutionRequired(home) || IsEmergencyGoalkeeperNominationRequiredForTeam(home))
            {team=home?OnlineTeam.Home:OnlineTeam.Away;return true;}
        team=default;return false;
    }
    public bool TryNominateOnlineEmergencyKeeper(PlayerToken token,out string reason)
    {
        reason="An active paused online authority is required.";
        if(OnlineRoomSession.IsGuest || OnlineRoomSession.Current?.HasInitialData!=true || !IsOnlineAuthorityControlled || !IsPauseMenuOpen)return false;
        return NominateEmergencyGoalkeeper(token,out reason);
    }
    public bool TryApplyOnlineSubstitution(PlayerToken outgoing,PlayerToken incoming,out string reason)
    {
        reason="An active paused online authority is required.";
        if(OnlineRoomSession.IsGuest || OnlineRoomSession.Current?.HasInitialData!=true || !IsOnlineAuthorityControlled || !IsPauseMenuOpen || playerTokenManager==null)return false;
        if(!CanRegisterSubstitution(outgoing,incoming,out reason))return false;
        bool replacement=IsGoalkeeperReplacementRequired(outgoing.isHomeTeam) && !outgoing.IsGoalKeeper && incoming.IsGoalKeeper;
        var destination=replacement?GetGoalkeeperReplacementDestinationHex(outgoing.isHomeTeam)??outgoing.GetCurrentHex():outgoing.GetCurrentHex();
        var bench=incoming.transform.position;float height=outgoing.transform.position.y;bool attacker=outgoing.isAttacker;
        bool ballAtDestination=ball!=null && ball.GetCurrentHex()==destination;
        if(!RegisterSubstitution(outgoing,incoming,out reason))return false;
        playerTokenManager.MoveActiveTokenToBenchSlot(outgoing,bench);playerTokenManager.MoveBenchTokenToActive(incoming);
        incoming.isAttacker=attacker;
        if(destination!=null)
        {
            var center=destination.GetHexCenter();incoming.transform.position=new Vector3(center.x,height,center.z);
            destination.occupyingToken=null;destination.isAttackOccupied=attacker;destination.isDefenseOccupied=!attacker;
            incoming.SetCurrentHex(destination);destination.ResetHighlight();destination.HighlightHex(attacker?"isAttackOccupied":"isDefenseOccupied");
            if(ballAtDestination)ball.PlaceAtCell(destination);
        }
        if(replacement)CompleteGoalkeeperReplacement(outgoing.isHomeTeam);
        return true;
    }
}
