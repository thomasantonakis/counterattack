using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public partial class PlayerTokenManager
{
    public PlayerToken[] AuthoredOnlineRoster {get;private set;}
    public PlayerToken[] BindAuthoredOnlineRoster(PlayerToken[] authored,MatchManager.GameData data)
    {
        var active=new List<PlayerToken>();allTokens.Clear();benchTokens.Clear();
        foreach(bool home in new[]{true,false})
        {
            var roster=home?data.rosters.home:data.rosters.away;
            var formation=GoalFlowManager.GetResetFormationCoordinatesForDirection(data.openingSetup.DirectionFor(home));
            for(int jersey=1;jersey<=18;jersey++)
            {
                var token=authored.Single(t=>t.isHomeTeam==home && t.jerseyNumber==jersey);
                if(!roster.TryGetValue(jersey.ToString(),out var player)){token.gameObject.SetActive(false);continue;}
                token.enabled=false;token.InitializeAttributesFromRoster(player,jersey);token.name=jersey+". "+player.name;
                token.isAttacker=home==(MatchManager.Instance.teamInAttack==MatchManager.TeamInAttack.Home);
                string kit=home?(jersey is 1 or 12?data.gameSettings.homeGKKit:data.gameSettings.homeKit):(jersey is 1 or 12?data.gameSettings.awayGKKit:data.gameSettings.awayKit);
                var style=TokenKitCatalog.ResolveStyle(kit);var visuals=token.GetComponent<PlayerTokenVisuals>();visuals.ApplyStyle(style);
                foreach(var number in token.GetComponentsInChildren<TextMeshPro>(true))
                {
                    number.text=jersey is 6 or 9?"<u>"+jersey+"</u>":jersey.ToString();
                    number.fontSize=3;
                    number.alignment=TextAlignmentOptions.Center;
                    visuals.ApplyNumberStyle(number,style);
                    number.ForceMeshUpdate(true);
                }
                if(jersey<=11)
                {
                    var hex=hexgrid.GetHexCellAt(formation[jersey-1])??throw new InvalidOperationException("Online formation is outside the authored pitch");
                    hex.isAttackOccupied=token.isAttacker;hex.isDefenseOccupied=!token.isAttacker;
                    token.MarkAsStarter();token.SetCurrentHex(hex);token.transform.position=hex.GetHexCenter()+Vector3.up*.2f;allTokens.Add(token);
                }
                else{token.MarkAsBench();token.transform.position=GetBenchTokenPositionForRestore(home,jersey-12);benchTokens.Add(token);}
                active.Add(token);
            }
        }
        AuthoredOnlineRoster=active.ToArray();
        return AuthoredOnlineRoster;
    }
}
