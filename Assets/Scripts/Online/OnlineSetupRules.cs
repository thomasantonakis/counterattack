using System;
using System.Linq;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    public static class OnlineSetupRules
    {
        public static GameSettings Copy(GameSettings settings)=>settings==null?null:JsonConvert.DeserializeObject<GameSettings>(JsonConvert.SerializeObject(settings));
        public static GameSettings MergeOwnedSelection(GameSettings current,GameSettings proposed,OnlineTeam actor,bool ownsShared)
        {
            var merged=Copy(ownsShared?proposed:current);
            merged.homeTeamName=current.homeTeamName;merged.awayTeamName=current.awayTeamName;
            merged.homeKit=current.homeKit;merged.awayKit=current.awayKit;merged.homeGKKit=current.homeGKKit;merged.awayGKKit=current.awayGKKit;
            merged.homeTeamAbbreviation=current.homeTeamAbbreviation;merged.awayTeamAbbreviation=current.awayTeamAbbreviation;
            if(actor==OnlineTeam.Home){merged.homeTeamName=proposed.homeTeamName;merged.homeKit=proposed.homeKit;merged.homeGKKit=proposed.homeGKKit;}
            else{merged.awayTeamName=proposed.awayTeamName;merged.awayKit=proposed.awayKit;merged.awayGKKit=proposed.awayGKKit;}
            if(merged.homeTeamName!=current.homeTeamName)merged.homeTeamAbbreviation=null;
            if(merged.awayTeamName!=current.awayTeamName)merged.awayTeamAbbreviation=null;
            return merged;
        }
        public static GameSettings Normalize(GameSettings source)
        {
            var s=Copy(source);if(s==null)return null;
            s.gameMode="Online";s.homeTeamName=(s.homeTeamName??"").Trim();s.awayTeamName=(s.awayTeamName??"").Trim();
            s.homeTeamControl=s.awayTeamControl="Human";s.homeDraftPersona=s.awayDraftPersona=s.defaultDraftPersona="Sophisticated";
            s.homeRoomPersona=s.awayRoomPersona=s.defaultRoomPersona="AskHuman";
            s.homeKit=TokenKitCatalog.GetPresetByIdOrAlias(s.homeKit)?.Id??s.homeKit;
            s.awayKit=TokenKitCatalog.GetPresetByIdOrAlias(s.awayKit)?.Id??s.awayKit;
            s.homeGKKit=TokenKitCatalog.GetPresetByIdOrAlias(s.homeGKKit)?.Id??s.homeGKKit;
            s.awayGKKit=TokenKitCatalog.GetPresetByIdOrAlias(s.awayGKKit)?.Id??s.awayGKKit;return s;
        }
        public static string Validate(GameSettings s)
        {
            if(s==null)return "Settings are required";
            if(new[]{s.homeTeamName,s.awayTeamName}.Any(n=>string.IsNullOrWhiteSpace(n) || n.Length>20 || n.Any(char.IsControl)))return "Team names must contain 1 to 20 printable characters";
            if(string.Equals(s.homeTeamName,s.awayTeamName,StringComparison.OrdinalIgnoreCase))return "Both team names must differ";
            if(s.halfDuration<15 || s.halfDuration>60 || s.numberOfHalfs is <1 or >2 || s.playerAssistance is <1 or >3)return "Invalid duration, halves or assistance";
            if(!One(s.tiebreaker,"None","Penalties","Extra Time","Extra Time & Penalties")
                || (s.halfDuration!=45 || s.numberOfHalfs!=2) && s.tiebreaker.Contains("Extra Time"))return "Invalid tiebreaker";
            if(!One(s.matchType,"Regular","International") || !One(s.squadSize,"16","18"))return "Invalid match or squad type";
            if(s.matchType=="International")
            {
                if(s.squadSize!="18" || s.draft!="International" || s.gkDraft!="International")return "International roster settings must agree";
                var teams=CreateNewGameManager.LoadWorldCupNationalities().ToArray();
                if(!teams.Contains(s.homeTeamName) || !teams.Contains(s.awayTeamName))return "Unknown international team";
            }
            else if(!One(s.draft,"Regular","Free Regular","Arcade") || !One(s.gkDraft,"Deal","Free","Arcade"))return "Invalid draft mode";
            if(!(s.includeTabletopia || s.includeNonTabletopia || s.includeInternationals)
                || !(s.includeTabletopiaGK || s.includeNonTabletopiaGK || s.includeInternationalsGK))return "Both player pools require a source";
            if(!One(s.referee,"Random","Webster - 2","Castolo - 3","Bakker - 4","Read - 5")
                || !One(s.weatherConditions,"Clear","Rain","Snow") || !One(s.ballColor,"White","Orange","Yellow")
                || !One(s.fixtureLighting,"MidAfternoon","Sunset","LateNight"))return "Invalid referee or presentation setting";
            if(s.weatherConditions=="Snow" && s.ballColor!="Orange")return "Snow requires the orange ball";
            var h=TokenKitCatalog.GetPresetByIdOrAlias(s.homeKit);var a=TokenKitCatalog.GetPresetByIdOrAlias(s.awayKit);
            var hg=TokenKitCatalog.GetPresetByIdOrAlias(s.homeGKKit);var ag=TokenKitCatalog.GetPresetByIdOrAlias(s.awayGKKit);
            if(h==null || a==null || hg==null || ag==null || h.Id.StartsWith("gk") || a.Id.StartsWith("gk")
                || !hg.Id.StartsWith("gk") || !ag.Id.StartsWith("gk"))return "Unknown or incorrectly typed kit";
            return CreateNewGameManager.ValidateSelectedKits(h,a,hg,ag,TokenKitCatalog.GetSimilarityBreakdown(h.Id,a.Id));
        }
        private static bool One(string value,params string[] options)=>options.Contains(value);
    }
}
