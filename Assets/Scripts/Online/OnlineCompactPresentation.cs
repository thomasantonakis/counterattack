using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Epic.OnlineServices;
namespace CounterAttack.Online
{
    /// <summary>Lossless, single-packet full-Room poses; never carries commands or outcomes.</summary>
    public static class OnlineCompactPresentation
    {
        public const int PacketLimit=1170;
        private const ushort Magic=0xCA18;
        public static bool Use(bool fullRoom,string kind)=>fullRoom && kind=="presentation";
        public static bool DropTransient(bool compact,Result result)=>compact && (result==Result.LimitExceeded || result==Result.NoConnection || result==Result.TimedOut);
        public static bool IsPacket(byte[] bytes,int length)=>bytes!=null && length>=2 && length<=bytes.Length && bytes[0]==0x18 && bytes[1]==0xCA;
        private static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value)<=1000;
        private static int Actor(string id)
        {
            var parts=id?.Split(':');
            if(parts?.Length!=2 || (parts[0]!="home" && parts[0]!="away") || !int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out int number)
                || number<1 || number>18 || parts[1]!=number.ToString(CultureInfo.InvariantCulture))return -1;
            return (parts[0]=="home"?0:18)+number-1;
        }
        private static bool Valid(OnlineGroundPassMessage message)
        {
            var frame=message?.presentation;
            return message?.kind=="presentation" && OnlineProtocolIds.IsValid(message.matchId) && message.matchId.Length<=128 && message.matchId.All(c=>c<128)
                && Guid.TryParseExact(message.peerNonce,"N",out var nonce) && nonce!=Guid.Empty && message.connectionGeneration>0
                && frame!=null && frame.matchId==message.matchId && frame.publicationSequence>0 && frame.frameSequence>0
                && Finite(frame.ballX) && Finite(frame.ballY) && Finite(frame.ballZ) && frame.players!=null && frame.players.Length<=36
                && frame.players.All(p=>p!=null && Actor(p.id)>=0 && Finite(p.x) && Finite(p.y) && Finite(p.z))
                && frame.players.Select(p=>p.id).Distinct().Count()==frame.players.Length;
        }
        public static byte[] Encode(OnlineGroundPassMessage message)
        {
            if(!Valid(message))throw new InvalidDataException("Invalid compact presentation");
            using var stream=new MemoryStream();using var writer=new BinaryWriter(stream,Encoding.ASCII,true);
            writer.Write(Magic);writer.Write((byte)1);var id=Encoding.ASCII.GetBytes(message.matchId);writer.Write((byte)id.Length);writer.Write(id);
            writer.Write(Encoding.ASCII.GetBytes(message.peerNonce));writer.Write(message.connectionGeneration);
            var frame=message.presentation;writer.Write(frame.publicationSequence);writer.Write(frame.frameSequence);
            writer.Write(frame.ballX);writer.Write(frame.ballY);writer.Write(frame.ballZ);writer.Write((byte)frame.players.Length);
            foreach(var player in frame.players){writer.Write((byte)Actor(player.id));writer.Write(player.x);writer.Write(player.y);writer.Write(player.z);}
            writer.Flush();var packet=stream.ToArray();if(packet.Length>PacketLimit)throw new InvalidDataException("Compact pose exceeds one EOS packet");return packet;
        }
        public static OnlineGroundPassMessage Decode(byte[] packet,int length)
        {
            if(!IsPacket(packet,length) || length<58 || length>PacketLimit)return null;
            try
            {
                using var stream=new MemoryStream(packet,0,length,false);using var reader=new BinaryReader(stream,Encoding.ASCII,true);
                if(reader.ReadUInt16()!=Magic || reader.ReadByte()!=1)return null;int size=reader.ReadByte();if(size<1 || size>128)return null;
                var message=new OnlineGroundPassMessage{version="ground-wire-v7",kind="presentation",matchId=Encoding.ASCII.GetString(reader.ReadBytes(size)),peerNonce=Encoding.ASCII.GetString(reader.ReadBytes(32)),connectionGeneration=reader.ReadInt64()};
                var frame=new OnlineGroundPassPresentation{matchId=message.matchId,publicationSequence=reader.ReadInt64(),frameSequence=reader.ReadInt64(),ballX=reader.ReadSingle(),ballY=reader.ReadSingle(),ballZ=reader.ReadSingle()};
                int count=reader.ReadByte();if(count>36 || stream.Position+count*13!=length)return null;frame.players=new OnlinePlayerPresentation[count];
                for(int i=0;i<count;i++)
                {
                    int actor=reader.ReadByte();if(actor>=36)return null;
                    frame.players[i]=new OnlinePlayerPresentation{id=(actor<18?"home:":"away:")+(actor%18+1).ToString(CultureInfo.InvariantCulture),x=reader.ReadSingle(),y=reader.ReadSingle(),z=reader.ReadSingle()};
                }
                message.presentation=frame;return Valid(message)?message:null;
            }
            catch(IOException){return null;}catch(ArgumentException){return null;}
        }
    }
}
