using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineFullRoomPayload")]
public sealed class OnlineFullRoomPayloadEditorTests
{
    internal static OnlineGroundPassState State(int size,bool populated)
    {
        return new OnlineGroundPassState{matchId="payload",catalogId=OnlineFullRoomBootstrap.Catalog,publicationSequence=1,
            homeDirection="LeftToRight",awayDirection="RightToLeft",homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats(),
            legalActions=Enumerable.Range(0,1600).Select(i=>"movement.target:"+i+":-12").ToArray(),
            tokens=Enumerable.Range(0,size*2).Select(i=>
            {
                string side=i<size?"Home":"Away";int jersey=i%size+1;var stats=new MatchManager.PlayerStats();
                if(populated)foreach(var field in typeof(MatchManager.PlayerStats).GetFields()){if(field.FieldType==typeof(int))field.SetValue(stats,7);else if(field.FieldType==typeof(float))field.SetValue(stats,.125f);}
                return new OnlineGroundPassTokenState{id=side.ToLowerInvariant()+":"+jersey,visible=true,playerStats=stats,
                    runtime=new RoomTokenSnapshot{tokenKey=side+":"+jersey,teamSide=side,jerseyNumber=jersey,playerName=new string('n',64)}};
            }).ToArray()};
    }
    [Test] public void LargeRosterRoundTripsWithinWireBudget([Values(16,18)]int size,[Values(false,true)]bool populated)
    {
        var state=State(size,populated);var message=new OnlineGroundPassMessage{kind="state",matchId="payload",state=state};
        int expanded=Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(state,new JsonSerializerSettings{NullValueHandling=NullValueHandling.Ignore}));
        Assert.That(expanded,Is.GreaterThan(OnlineGroundPassWire.PayloadLimit));var packets=OnlineGroundPassWire.Encode(message).ToArray();
        int wireBytes=packets.Sum(p=>p.Length-OnlineGroundPassWire.HeaderSize);
        Assert.That(wireBytes,Is.LessThanOrEqualTo(OnlineGroundPassWire.PayloadLimit));Assert.That(wireBytes,Is.LessThan(expanded));
        Assert.That(message.state,Is.SameAs(state));Assert.That(message.compressedState,Is.Null,"Encoding must not replace the caller's logical snapshot");
        var reader=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in packets)decoded=reader.Receive(packet,packet.Length);
        Assert.That(JsonUtility.ToJson(decoded.state),Is.EqualTo(JsonUtility.ToJson(state)));Assert.That(decoded.compressedState,Is.Null);
        TestContext.WriteLine($"Expanded bytes={expanded}, wire bytes={wireBytes}, fragments={packets.Length}");
    }
    private static string Zip(byte[] bytes)
    {
        using var output=new MemoryStream();using(var gzip=new GZipStream(output,System.IO.Compression.CompressionLevel.Fastest,true))gzip.Write(bytes,0,bytes.Length);return Convert.ToBase64String(output.ToArray());
    }
    [TestCase("base64")][TestCase("truncated")][TestCase("length")][TestCase("bomb")][TestCase("kind")][TestCase("conflict")][TestCase("catalog")][TestCase("missing-data")][TestCase("size-header")]
    public void MalformedCompressionCannotPoisonNextMessage(string branch)
    {
        var state=State(16,false);state.legalActions=Array.Empty<string>();if(branch=="catalog")state.catalogId="legacy";
        var bytes=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(state));
        var message=new OnlineGroundPassMessage{kind="state",matchId="payload",compressedState=Zip(bytes),expandedStateBytes=bytes.Length};
        switch(branch)
        {
            case "base64":message.compressedState="%%%";break;
            case "truncated":var compressed=Convert.FromBase64String(message.compressedState);message.compressedState=Convert.ToBase64String(compressed.Take(compressed.Length/2).ToArray());break;
            case "length":message.expandedStateBytes++;break;
            case "bomb":message.expandedStateBytes=OnlineFullRoomStateCodec.ExpandedLimit;message.compressedState=Zip(new byte[OnlineFullRoomStateCodec.ExpandedLimit+1]);break;
            case "kind":message.kind="hello";break;
            case "conflict":message.state=state;break;
            case "missing-data":message.compressedState=null;break;
            case "size-header":message.expandedStateBytes=OnlineFullRoomStateCodec.ExpandedLimit+1;break;
        }
        var reader=new OnlineGroundPassWire();
        Assert.Throws<InvalidDataException>(()=>{foreach(var packet in OnlineGroundPassWire.Encode(message))reader.Receive(packet,packet.Length);});
        OnlineGroundPassMessage good=null;foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="payload"}))good=reader.Receive(packet,packet.Length);
        Assert.That(good.kind,Is.EqualTo("hello"));
    }
    [Test] public void ExpandedStateHasIndependentHardLimit()
    {
        var state=State(18,true);state.legalActions=Enumerable.Repeat(new string('x',128),3000).ToArray();
        Assert.Throws<InvalidDataException>(()=>OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId="payload",state=state}).ToArray());
    }
    [Test] public void PreparedProfileKeepsUncompressedState()
    {
        var state=State(16,false);state.catalogId="prepared-profile";state.legalActions=Array.Empty<string>();
        var reader=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId="payload",state=state}))decoded=reader.Receive(packet,packet.Length);
        Assert.That(decoded.state.catalogId,Is.EqualTo("prepared-profile"));Assert.That(decoded.compressedState,Is.Null);Assert.That(decoded.expandedStateBytes,Is.Zero);
    }
}
