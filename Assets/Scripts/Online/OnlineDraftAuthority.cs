using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CounterAttack.Online
{
    public enum OnlineDraftIntent { Roll, Pick, Swap, Ready, Unready, Start, Choose }
    [Serializable] public sealed class OnlineDraftCard
    {
        public string id,name,country,type;
        public bool goalkeeper;
        public int pace,dribbling,heading,highPass,resilience,shooting,tackling,saving,handling,aerial,squadNumber;
    }
    [Serializable] public sealed class OnlineDraftRequest
    {
        public string matchId,cardId;public long sequence,revision;
        public OnlineDraftIntent intent;public int slot,otherSlot;
        public CoinTossChoice choice;
    }
    [Serializable] public sealed class OnlineDraftState
    {
        public string matchId,phase,selection;public long revision,connectionRevision,homeSequence,awaySequence;
        public OnlineTeam turn,hostTeam,firstPicker;public bool connected,homeReady,awayReady;
        public int homeRoll,awayRoll,rollRound,batch,totalBatches;
        public bool freeSelection,repeatable;public string[] available;
        public bool openingCanChooseKickoff,openingWinnerIsHome;
        public MatchOpeningSetup openingSetup;
        public string roomSessionId,roomStartedUtc;
        public GameSettings settings;
        public OnlineDraftCard[] cards,home,away;
    }
    public static class OnlineDraftCatalog
    {
        public static OnlineDraftCard[] Load()
        {
            var result=new List<OnlineDraftCard>();
            foreach(bool keeper in new[]{false,true})
            {
                string resource=keeper?"goalkeepers":"outfield_players";
                var asset=Resources.Load<TextAsset>(resource)??throw new InvalidOperationException("Missing draft catalog: "+resource);
                string[] lines=asset.text.TrimStart('\uFEFF').Split(new[]{"\r\n","\n"},StringSplitOptions.RemoveEmptyEntries);
                var headers=lines[0].Split(',').Select(h=>h.Trim()).ToArray();
                for(int row=1;row<lines.Length;row++)
                {
                    var fields=lines[row].Split(',');
                    string Text(string key){int column=Array.FindIndex(headers,h=>string.Equals(h,key,StringComparison.OrdinalIgnoreCase));return column>=0 && column<fields.Length?fields[column].Trim():"";}
                    int Number(string key)=>int.TryParse(Text(key),out int number)?number:0;
                    if(string.IsNullOrWhiteSpace(Text("Name")))continue;
                    result.Add(new OnlineDraftCard{id=(keeper?"g":"o")+row.ToString("D4"),name=Text("Name"),country=Text("Nationality"),type=Text("Type"),goalkeeper=keeper,
                        pace=Number("Pace"),dribbling=Number("Dribbling"),heading=Number("Heading"),highPass=Number("High Pass"),resilience=Number("Resilience"),
                        shooting=Number("Shooting"),tackling=Number("Tackling"),saving=Number("Saving"),handling=Number("Handling"),aerial=Number("Aerial"),squadNumber=Number("sqno")});
                }
            }
            return result.ToArray();
        }
    }
    // Host-only rules/state. Random sources are injected by the host; commands
    // contain neither random outcomes nor client-supplied player attributes.
    public sealed partial class OnlineDraftAuthority
    {
        private readonly OnlineMatchProtocol protocol;
        private readonly Func<int> die;
        private readonly Func<double> rollClock;
        private double rollResultDeadline;
        private double? automaticRollDeadline;
        private readonly GameSettings settings;
        private readonly List<OnlineDraftCard> deck=new(),visible=new();
        private readonly OnlineDraftCard[] home,away;
        private string phase="roll";
        private OnlineTeam turn=OnlineTeam.Home,first;
        private int homeRoll,awayRoll,round=1,batch,picks;
        private long revision,connections,homeSequence,awaySequence;
        private long arcadeRevisionFloor, homeLastPickRevision, awayLastPickRevision;
        private bool homeReady,awayReady;
        private readonly Dictionary<(OnlineTeam,long),(string body,long revision)> receipts=new();
        private readonly Queue<(OnlineTeam,long)> receiptOrder=new();
        public OnlineDraftAuthority(OnlineMatchProtocol protocol,GameSettings agreed,OnlineDraftCard[] catalog,Func<int,int> nextIndex=null,Func<int> roll=null,Func<double> rollClock=null)
        {
            this.protocol=protocol??throw new ArgumentNullException(nameof(protocol));
            this.rollClock=rollClock;
            settings=OnlineSetupRules.Normalize(agreed);string error=OnlineSetupRules.Validate(settings);
            if(!string.IsNullOrEmpty(error))throw new ArgumentException(error,nameof(agreed));
            var all=Copy(catalog??throw new ArgumentNullException(nameof(catalog)));
            if(all.Any(c=>c==null || string.IsNullOrEmpty(c.id)) || all.Select(c=>c.id).Distinct().Count()!=all.Length)throw new ArgumentException("Draft cards require unique IDs");
            int size=int.Parse(settings.squadSize);home=new OnlineDraftCard[size];away=new OnlineDraftCard[size];
            die=roll??(()=>UnityEngine.Random.Range(1,7));nextIndex??=max=>UnityEngine.Random.Range(0,max);
            if(settings.referee=="Random")
            {
                string[] referees={"Webster - 2","Castolo - 3","Bakker - 4","Read - 5"};int selected=nextIndex(referees.Length);
                if(selected<0 || selected>=referees.Length)throw new InvalidOperationException("Invalid host referee selection");settings.referee=referees[selected];
            }
            var players=all.Where(c=>!c.goalkeeper && Allowed(c,false)).ToList();var keepers=all.Where(c=>c.goalkeeper && Allowed(c,true)).ToList();
            if(settings.matchType=="International"){InitializeInternational(all);connections=protocol.ConnectionRevision;return;}
            if(players.Count<(settings.draft=="Arcade"?1:(size-2)*2) || keepers.Count<(settings.gkDraft=="Arcade"?1:4))throw new ArgumentException("Selected pools do not contain enough players");
            void Shuffle(List<OnlineDraftCard> cards)
            {for(int i=0;i<cards.Count;i++){int offset=nextIndex(cards.Count-i);if(offset<0 || offset>=cards.Count-i)throw new InvalidOperationException("Invalid host shuffle result");int index=i+offset;(cards[i],cards[index])=(cards[index],cards[i]);}}
            if(settings.draft=="Regular"){Shuffle(players);deck.AddRange(players.Take((size-2)*2));}
            else outfieldPool=RankOutfield(players).ToList();
            if(settings.gkDraft=="Deal")Shuffle(keepers);
            else{goalkeeperPhase=true;pool=RankKeepers(keepers).ToList();phase=settings.gkDraft=="Arcade"?"pick":"roll";}
            for(int side=0;settings.gkDraft=="Deal" && side<2;side++)
            {
                var pair=keepers.Skip(side*2).Take(2).OrderByDescending(c=>c.saving).ThenByDescending(c=>c.handling).ThenByDescending(c=>c.pace).ThenByDescending(c=>c.aerial).ThenByDescending(c=>c.name).ToArray();
                var roster=side==0?home:away;roster[0]=pair[0];roster[11]=pair[1];
            }
            if(settings.gkDraft=="Deal" && settings.draft!="Regular"){pool=outfieldPool;phase=settings.draft=="Arcade"?"pick":"roll";}
            connections=protocol.ConnectionRevision;
        }
        public static T Copy<T>(T value)=>JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        private bool Allowed(OnlineDraftCard c,bool keeper)=>c.type?.Trim() switch
        {"TableTopia"=>keeper?settings.includeTabletopiaGK:settings.includeTabletopia,"Not TableTopia"=>keeper?settings.includeNonTabletopiaGK:settings.includeNonTabletopia,"World Cup"=>keeper?settings.includeInternationalsGK:settings.includeInternationals,_=>true};
        private void Observe()
        {if(connections==protocol.ConnectionRevision)return;connections=protocol.ConnectionRevision;if(phase is "roll" or "pick" or "review")homeReady=awayReady=false;arcadeRevisionFloor=++revision;}
        public bool AdvanceRollResult()
        {
            if(phase is not ("roll-result" or "opening-result") || !protocol.BothConnected || rollClock()<rollResultDeadline)return false;
            CompleteRoll(phase=="opening-result");
            revision=checked(revision+1);
            return true;
        }

        public bool AdvanceAutomaticRoll()
        {
            if(rollClock==null || !protocol.BothConnected || phase is not ("roll" or "opening-roll"))
            {
                automaticRollDeadline=null;
                return false;
            }
            automaticRollDeadline??=rollClock()+DecisionTimingConfig.AutomaticRollSeconds;
            if(rollClock()<automaticRollDeadline.Value)return false;
            ResolveRoll(turn);
            revision=checked(revision+1);
            return true;
        }

        private void ResolveRoll(OnlineTeam actor)
        {
            int result=die();
            if(result<1 || result>6)throw new InvalidOperationException("Invalid host die result");
            automaticRollDeadline=null;
            if(actor==OnlineTeam.Home){homeRoll=result;turn=OnlineTeam.Away;return;}
            awayRoll=result;
            if(rollClock==null)CompleteRoll(phase=="opening-roll");
            else
            {
                phase=phase=="opening-roll"?"opening-result":"roll-result";
                rollResultDeadline=rollClock()+DraftOrderPanelController.RollResultSeconds;
            }
        }
        private void CompleteRoll(bool openingRoll)
        {
            if(homeRoll==awayRoll)
            {
                homeRoll=awayRoll=0;round++;turn=OnlineTeam.Home;phase=openingRoll?"opening-roll":"roll";
            }
            else if(openingRoll)
            {
                opening=new CoinTossDecision(homeRoll>awayRoll);turn=opening.WinnerIsHome?OnlineTeam.Home:OnlineTeam.Away;phase="opening-choice";
            }
            else
            {
                first=turn=homeRoll>awayRoll?OnlineTeam.Home:OnlineTeam.Away;phase="pick";if(!FreeSelection)Deal();
            }
        }
        public OnlineDraftState Capture()
        {
            Observe();return Copy(new OnlineDraftState{matchId=protocol.MatchId,revision=revision,connectionRevision=connections,homeSequence=homeSequence,awaySequence=awaySequence,
                phase=phase,turn=turn,hostTeam=protocol.HostTeam,firstPicker=first,connected=protocol.BothConnected,homeReady=homeReady,awayReady=awayReady,
                homeRoll=homeRoll,awayRoll=awayRoll,rollRound=round,batch=batch,totalBatches=(home.Length-2)/2,settings=settings,cards=visible.ToArray(),home=home,away=away,
                selection=goalkeeperPhase?"goalkeepers":"outfielders",freeSelection=FreeSelection,repeatable=Repeatable,available=FreeSelection && phase is "pick" or "roll"?pool.Select(c=>c.id).ToArray():Array.Empty<string>(),
                openingCanChooseKickoff=opening?.CanChooseKickoff==true,openingWinnerIsHome=opening?.WinnerIsHome==true,openingSetup=opening?.Result,roomSessionId=roomSessionId,roomStartedUtc=roomStartedUtc});
        }
        public OnlineCommandReply Submit(string connection,OnlineDraftRequest request)
        {
            Observe();OnlineCommandReply Reply(OnlineCommandStatus status)=>new(status,revision);
            if(request==null || request.sequence<=0 || request.revision<0 || !Enum.IsDefined(typeof(OnlineDraftIntent),request.intent))return Reply(OnlineCommandStatus.InvalidCommand);
            if(request.matchId!=protocol.MatchId)return Reply(OnlineCommandStatus.WrongMatch);
            if(!protocol.TryGetTeam(connection,out var actor))return Reply(OnlineCommandStatus.UnknownConnection);
            string body=JsonConvert.SerializeObject(request);var key=(actor,request.sequence);
            if(receipts.TryGetValue(key,out var receipt))return receipt.body==body?new(OnlineCommandStatus.Duplicate,receipt.revision):Reply(OnlineCommandStatus.SequenceConflict);
            if(request.sequence<=(actor==OnlineTeam.Home?homeSequence:awaySequence))return Reply(OnlineCommandStatus.StaleSequence);
            if(!protocol.BothConnected)return Reply(OnlineCommandStatus.Disconnected);
            bool arcadePick=phase=="pick" && Repeatable && request.intent==OnlineDraftIntent.Pick;
            bool wasGoalkeeperPhase=goalkeeperPhase;
            long ownPickRevision=actor==OnlineTeam.Home?homeLastPickRevision:awayLastPickRevision;
            // A peer filling their own Arcade slots does not invalidate our exact-slot pick.
            if(request.revision!=revision && (!arcadePick || request.revision>revision
                || request.revision<Math.Max(arcadeRevisionFloor,ownPickRevision)))return Reply(OnlineCommandStatus.StaleRevision);
            if(phase=="room-ready")return Reply(OnlineCommandStatus.NotReady);
            var roster=actor==OnlineTeam.Home?home:away;
            switch(request.intent)
            {
                case OnlineDraftIntent.Roll:
                    if(phase is not ("roll" or "opening-roll"))return Reply(OnlineCommandStatus.NotReady);
                    if(actor!=turn)return Reply(OnlineCommandStatus.WrongTeam);
                    ResolveRoll(actor);
                    break;
                case OnlineDraftIntent.Pick:
                    if(phase!="pick")return Reply(OnlineCommandStatus.NotReady);
                    if(!Repeatable && actor!=turn)return Reply(OnlineCommandStatus.WrongTeam);
                    int index=request.slot-1;
                    if(index<0 || index>=roster.Length || !goalkeeperPhase && index is 0 or 11)return Reply(OnlineCommandStatus.IllegalAction);
                    if(FreeSelection && (((index is 0 or 11)!=goalkeeperPhase) || roster[index]!=null))return Reply(OnlineCommandStatus.IllegalAction);
                    var card=(FreeSelection?pool:visible).FirstOrDefault(c=>c!=null && c.id==request.cardId);if(card==null)return Reply(OnlineCommandStatus.IllegalAction);
                    if(roster[index]!=null)
                    {int destination=-1;for(int n=1;n<=roster.Length;n++){int candidate=(index+n)%roster.Length;if((candidate is 0 or 11)==goalkeeperPhase && roster[candidate]==null){destination=candidate;break;}}index=destination;}
                    if(index<0)return Reply(OnlineCommandStatus.IllegalAction);
                    roster[index]=card;
                    if(FreeSelection)
                    {
                        if(!Repeatable)pool.Remove(card);
                        if(StageFull(home) && StageFull(away)){if(goalkeeperPhase)BeginOutfield();else phase="review";}
                        else if(!Repeatable)turn=Other(turn);
                        break;
                    }
                    visible[visible.IndexOf(card)]=null;picks++;turn=Other(turn);
                    if(visible.All(offer=>offer==null))
                    {visible.Clear();if(deck.Count==0)phase="review";else{Deal();turn=batch%2==1?first:Other(first);}}
                    break;
                case OnlineDraftIntent.Swap:
                    int from=request.slot-1,to=request.otherSlot-1;
                    if(phase is not ("pick" or "review") || from<0 || to<0 || from>=roster.Length || to>=roster.Length || from==to || ((from is 0 or 11)!=(to is 0 or 11)) || roster[from]==null)return Reply(OnlineCommandStatus.IllegalAction);
                    (roster[from],roster[to])=(roster[to],roster[from]);homeReady=awayReady=false;break;
                case OnlineDraftIntent.Ready:
                    if(phase!="review" || home.Any(c=>c==null) || away.Any(c=>c==null))return Reply(OnlineCommandStatus.NotReady);
                    if(actor==OnlineTeam.Home)homeReady=true;else awayReady=true;break;
                case OnlineDraftIntent.Unready:
                    if(phase is "opening-roll" or "opening-choice")return Reply(OnlineCommandStatus.NotReady);
                    if(actor==OnlineTeam.Home)homeReady=false;else awayReady=false;break;
                case OnlineDraftIntent.Start:
                    if(actor!=protocol.HostTeam)return Reply(OnlineCommandStatus.WrongTeam);
                    if(phase!="review" || !homeReady || !awayReady)return Reply(OnlineCommandStatus.NotReady);
                    phase="opening-roll";homeRoll=awayRoll=0;round=1;turn=OnlineTeam.Home;break;
                case OnlineDraftIntent.Choose:
                    if(phase!="opening-choice" || opening==null)return Reply(OnlineCommandStatus.NotReady);
                    if(actor!=turn)return Reply(OnlineCommandStatus.WrongTeam);
                    if(!opening.Choose(request.choice))return Reply(OnlineCommandStatus.IllegalAction);
                    turn=opening.ChoosingHome?OnlineTeam.Home:OnlineTeam.Away;
                    if(opening.IsComplete){phase="room-ready";roomSessionId=Guid.NewGuid().ToString("N");roomStartedUtc=DateTime.UtcNow.ToString("O");}break;
            }
            revision++;
            if(!arcadePick || phase!="pick" || goalkeeperPhase!=wasGoalkeeperPhase)
                arcadeRevisionFloor=revision;
            if(actor==OnlineTeam.Home){homeSequence=request.sequence;if(arcadePick)homeLastPickRevision=revision;}
            else{awaySequence=request.sequence;if(arcadePick)awayLastPickRevision=revision;}
            receipts.Add(key,(body,revision));receiptOrder.Enqueue(key);while(receiptOrder.Count>128)receipts.Remove(receiptOrder.Dequeue());return Reply(OnlineCommandStatus.Accepted);
        }
        private static OnlineTeam Other(OnlineTeam team)=>team==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home;
        private void Deal(){visible.AddRange(deck.Take(4));deck.RemoveRange(0,Math.Min(4,deck.Count));batch++;}
    }
}
