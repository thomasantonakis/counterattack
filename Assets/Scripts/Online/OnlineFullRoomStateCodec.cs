using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    // Keep the existing 64 KiB transport bound. Only the negotiated full Room
    // snapshot uses compression, with an independent bounded expansion budget.
    public static class OnlineFullRoomStateCodec
    {
        public const int ExpandedLimit=262144;
        internal static OnlineGroundPassMessage Compress(OnlineGroundPassMessage message)
        {
            var bytes=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message.state,new JsonSerializerSettings{NullValueHandling=NullValueHandling.Ignore}));
            if(bytes.Length<1 || bytes.Length>ExpandedLimit)throw new InvalidDataException("Expanded Room state size");
            using var output=new MemoryStream();
            using(var gzip=new GZipStream(output,CompressionLevel.Fastest,true))gzip.Write(bytes,0,bytes.Length);
            return message.WithCompressedState(Convert.ToBase64String(output.ToArray()),bytes.Length);
        }
        internal static OnlineGroundPassMessage Expand(OnlineGroundPassMessage message)
        {
            try
            {
                using var input=new MemoryStream(Convert.FromBase64String(message.compressedState));
                using var gzip=new GZipStream(input,CompressionMode.Decompress);
                using var output=new MemoryStream();var buffer=new byte[4096];int read;
                while((read=gzip.Read(buffer,0,buffer.Length))>0)
                {
                    if(output.Length+read>message.expandedStateBytes || output.Length+read>ExpandedLimit)throw new InvalidDataException("Expanded Room state exceeded its budget");
                    output.Write(buffer,0,read);
                }
                if(output.Length!=message.expandedStateBytes)throw new InvalidDataException("Expanded Room state length mismatch");
                var state=JsonConvert.DeserializeObject<OnlineGroundPassState>(new UTF8Encoding(false,true).GetString(output.ToArray()));
                if(state==null || state.catalogId!=OnlineFullRoomBootstrap.Catalog || state.matchId!=message.matchId)throw new InvalidDataException("Compressed Room state identity");
                message.state=state;message.compressedState=null;message.expandedStateBytes=0;return message;
            }
            catch(FormatException error){throw new InvalidDataException("Compressed Room state encoding",error);}
            catch(JsonException error){throw new InvalidDataException("Compressed Room state JSON",error);}
            catch(DecoderFallbackException error){throw new InvalidDataException("Compressed Room state UTF8",error);}
        }
    }
}
