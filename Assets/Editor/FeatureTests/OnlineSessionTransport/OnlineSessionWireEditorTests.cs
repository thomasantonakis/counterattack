using System;
using System.IO;
using System.Linq;
using System.Text;
using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineSessionTransport")]
public sealed class OnlineSessionWireEditorTests
{
    private static OnlineSessionMessage Message(string kind)=>new(){matchId="frontend-session",kind=kind,status="Ready",protocol="session-wire-v1",rules="rules-1",catalog="catalog-1",
        state=new OnlineSetupState{matchId="frontend-session",phase="setup",connected=true,settings=OnlineSetupAgreementEditorTests.Valid()}};
    [Test] public void ControlRoundTrip([Values("hello","setup-request","setup-state","setup-reply","heartbeat","leave","phase-ready","phase-ack")]string kind)
    {
        var sender=Message(kind);var wire=new OnlineSessionWire();OnlineSessionMessage received=null;
        foreach(var packet in OnlineSessionWire.Encode(sender))received=wire.Receive(packet,packet.Length);
        Assert.That(received.kind,Is.EqualTo(kind));Assert.That(received.state.settings.homeTeamName,Is.EqualTo("Home club"));
        Assert.That(received.version,Is.EqualTo(OnlineSessionWire.Version));Assert.That(received.protocol,Is.EqualTo(sender.protocol));
    }
    [Test] public void FragmentedControlPreservesUnicode()
    {
        var sender=Message("setup-state");sender.status=string.Concat(Enumerable.Repeat("????????-?-",1600));
        var packets=OnlineSessionWire.Encode(sender).ToArray();Assert.That(packets.Length,Is.GreaterThan(1));
        var wire=new OnlineSessionWire();OnlineSessionMessage received=null;
        foreach(var packet in packets){Assert.That(packet.Length,Is.LessThanOrEqualTo(OnlineSessionWire.HeaderSize+OnlineSessionWire.FragmentPayload));received=wire.Receive(packet,packet.Length);}
        Assert.That(received.status,Is.EqualTo(sender.status));Assert.That(received.state.settings.awayKit,Is.EqualTo(sender.state.settings.awayKit));
    }
    [Test] public void FrontendAndGameplayFramesCannotCrossDecode()
    {
        var frontend=OnlineSessionWire.Encode(Message("hello")).First();
        Assert.Throws<InvalidDataException>(()=>new OnlineGroundPassWire().Receive(frontend,frontend.Length));
        var gameplay=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="frontend-session"}).First();
        Assert.Throws<InvalidDataException>(()=>new OnlineSessionWire().Receive(gameplay,gameplay.Length));
    }
    private static byte[] Raw(string json)
    {
        var data=Encoding.UTF8.GetBytes(json);using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write((ushort)0xCA11);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write(data.Length);writer.Write(Guid.NewGuid().ToByteArray());writer.Write(data);return stream.ToArray();
    }
    [Test] public void RejectsHeader([Values("size","count","index","empty-id","magic")]string mutation)
    {
        var packet=Raw("{\"version\":\"session-wire-v1\",\"kind\":\"hello\",\"matchId\":\"frontend-session\"}");
        switch(mutation){case "size":packet[6]=0;packet[7]=0;packet[8]=0;packet[9]=0;break;
            case "count":packet[4]=2;break;case "index":packet[2]=1;break;case "empty-id":Array.Clear(packet,10,16);break;case "magic":packet[0]=0;break;}
        Assert.Throws<InvalidDataException>(()=>new OnlineSessionWire().Receive(packet,packet.Length));
    }
    [Test] public void RejectsContract([Values("json","version","kind","match")]string mutation)
    {
        var packet=Raw(mutation=="json"?"{bad":"{\"version\":\""+(mutation=="version"?"old":"session-wire-v1")+"\",\"kind\":\""+(mutation=="kind"?"execute-method":"hello")+"\",\"matchId\":\""+(mutation=="match"?"":"frontend-session")+"\"}");
        Assert.Throws<InvalidDataException>(()=>new OnlineSessionWire().Receive(packet,packet.Length));
    }
    [Test] public void OversizeIsRejectedBeforeSending()
    {var message=Message("setup-state");message.status=new string('x',OnlineSessionWire.PayloadLimit);Assert.Throws<InvalidDataException>(()=>OnlineSessionWire.Encode(message).ToArray());}
    [Test] public void OutOfOrderControlFragmentsAreRejected()
    {var message=Message("setup-state");message.status=new string('x',2000);var packet=OnlineSessionWire.Encode(message).Skip(1).First();Assert.Throws<InvalidDataException>(()=>new OnlineSessionWire().Receive(packet,packet.Length));}
    [Test] public void InterruptedConnectionRetiresOldPartialControl()
    {
        var message=Message("setup-state");message.status=new string('x',2000);var old=OnlineSessionWire.Encode(message).ToArray();
        var wire=new OnlineSessionWire();Assert.That(wire.Receive(old[0],old[0].Length),Is.Null);
        OnlineSessionMessage received=null;foreach(var packet in OnlineSessionWire.Encode(Message("heartbeat")))received=wire.Receive(packet,packet.Length,true);
        Assert.That(received.kind,Is.EqualTo("heartbeat"));Assert.That(wire.Receive(old[1],old[1].Length,true),Is.Null);
    }
}
