using System;
using System.Linq;
using System.Text;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
[Category("OnlineActionPreviews")]
public sealed class OnlineActionPreviewsEditorTests
{
    private static OnlineGroundPassState State()=>new(){matchId="previews",publicationSequence=1,revision=1,tokens=Array.Empty<OnlineGroundPassTokenState>(),legalActions=Array.Empty<string>(),
        hexPreviews=new[]{new OnlineHexPreview{x=2,z=3,reason="ballPath"}}};
    [TestCase("missing",true)][TestCase("empty",true)][TestCase("one",true)][TestCase("boundary",true)]
    [TestCase("duplicate",false)][TestCase("many",false)][TestCase("null",false)][TestCase("reason",false)][TestCase("hover",false)]
    [TestCase("x-low",false)][TestCase("x-high",false)][TestCase("z-low",false)][TestCase("z-high",false)]
    [TestCase("dark-nan",false)][TestCase("dark-low",false)][TestCase("dark-high",false)]
    [TestCase("height-nan",false)][TestCase("height-infinite",false)][TestCase("height-low",false)][TestCase("height-high",false)]
    public void BoundedPreviewContract(string corruption,bool accepted)
    {
        var s=State();var p=s.hexPreviews[0];
        switch(corruption)
        {
            case "missing":s.hexPreviews=null;break;case "empty":s.hexPreviews=Array.Empty<OnlineHexPreview>();break;
            case "boundary":p.x=-25;p.z=18;p.darkness=10;p.height=1;break;
            case "duplicate":s.hexPreviews=new[]{p,p};break;case "many":s.hexPreviews=Enumerable.Repeat(p,1901).ToArray();break;case "null":s.hexPreviews=new OnlineHexPreview[]{null};break;
            case "reason":p.reason="arbitrary";break;case "hover":p.reason="hover";break;
            case "x-low":p.x=-26;break;case "x-high":p.x=25;break;case "z-low":p.z=-20;break;case "z-high":p.z=19;break;
            case "dark-nan":p.darkness=float.NaN;break;case "dark-low":p.darkness=-1;break;case "dark-high":p.darkness=11;break;
            case "height-nan":p.height=float.NaN;break;case "height-infinite":p.height=float.PositiveInfinity;break;case "height-low":p.height=-2;break;case "height-high":p.height=2;break;
        }
        Assert.That(new OnlineGroundPassReplica(s.matchId).Apply(s),Is.EqualTo(accepted));
    }
    [Test] public void LargestRosterAndFullPitchPreviewStayWithinExistingWireBudgets()
    {
        var s=OnlineFullRoomPayloadEditorTests.State(18,true);s.instruction=new OnlineInstructionObservation{text=new string('w',OnlineInstructionObservation.TextLimit),owner="Home"};
        s.hexPreviews=Enumerable.Range(0,1900).Select(i=>new OnlineHexPreview{x=i%50-25,z=i/50-19,reason=i%2==0?"dangerousPass":"CanShootFrom",darkness=1,height=.03f,committedGoal=i%2==0}).ToArray();
        Assert.That(OnlineHexPreview.Valid(s.hexPreviews),Is.True);
        int expanded=Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(s,new JsonSerializerSettings{NullValueHandling=NullValueHandling.Ignore}));
        Assert.That(expanded,Is.LessThanOrEqualTo(OnlineFullRoomStateCodec.ExpandedLimit),"Complete previews must retain the bounded full roster and instructions");
        var packets=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=s.matchId,state=s}).ToArray();
        int wireBytes=packets.Sum(p=>p.Length-OnlineGroundPassWire.HeaderSize);TestContext.WriteLine($"Preview capacity: expanded={expanded}; wire={wireBytes}; actors=36; hexes=1900");
        Assert.That(wireBytes,Is.LessThanOrEqualTo(OnlineGroundPassWire.PayloadLimit));
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage result=null;foreach(var p in packets)result=wire.Receive(p,p.Length);
        Assert.That(result.state.hexPreviews.Length,Is.EqualTo(1900));Assert.That(result.state.tokens.Length,Is.EqualTo(36));Assert.That(result.state.instruction.text,Is.EqualTo(s.instruction.text));
        Assert.That(JsonConvert.SerializeObject(result.state.hexPreviews),Is.EqualTo(JsonConvert.SerializeObject(s.hexPreviews)));
    }
    [Test] public void InvalidPreviewCannotReplaceAcceptedState()
    {
        var s=State();var r=new OnlineGroundPassReplica(s.matchId);Assert.That(r.Apply(s),Is.True);s.publicationSequence++;s.hexPreviews[0].reason="hover";
        Assert.That(r.Apply(s),Is.False);Assert.That(r.State.publicationSequence,Is.EqualTo(1));Assert.That(r.State.hexPreviews[0].reason,Is.EqualTo("ballPath"));
    }
}
