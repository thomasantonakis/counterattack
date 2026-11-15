using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlinePlayerPresentation
    {
        public string id;
        public float x,y,z;
    }
    [Serializable]
    public sealed class OnlineGroundPassPresentation
    {
        public string matchId;
        public long publicationSequence, frameSequence;
        public float ballX, ballY, ballZ;
        public OnlinePlayerPresentation[] players;
    }
    [Serializable]
    public sealed class OnlineGroundPassMessage
    {
        public string version;
        public string kind, matchId, decisionId, actionId, status, digest;
        public long sequence, revision;
        // Used only by the explicitly negotiated manual recovery handoff.
        public string peerNonce;
        public long connectionGeneration;
        public OnlineGroundPassState state;
        public OnlineGroundPassPresentation presentation;
        public string compressedState;
        public int expandedStateBytes;
        internal OnlineGroundPassMessage WithCompressedState(string payload,int size)
        {
            var copy=(OnlineGroundPassMessage)MemberwiseClone();copy.state=null;copy.compressedState=payload;copy.expandedStateBytes=size;return copy;
        }

        public OnlineMatchCommand ToCommand() => new(matchId, sequence, revision, decisionId, actionId);
        public static OnlineGroundPassMessage Command(OnlineGroundPassState state, long sequence, string action)
            => new() { kind = "command", matchId = state.matchId, sequence = sequence,
                revision = state.revision, decisionId = state.decisionId, actionId = action };
    }

    /// <summary>Existing v7 gameplay framing, shared with the separate frontend codec.</summary>
    public sealed class OnlineGroundPassWire
    {
        public const int PayloadLimit=OnlineReliableJsonWire<OnlineGroundPassMessage>.PayloadLimit;
        public const int FragmentPayload=OnlineReliableJsonWire<OnlineGroundPassMessage>.FragmentPayload;
        public const int HeaderSize=OnlineReliableJsonWire<OnlineGroundPassMessage>.HeaderSize;
        private readonly OnlineReliableJsonWire<OnlineGroundPassMessage> framing=new(0xCA07,Valid);
        private static bool Valid(OnlineGroundPassMessage message)=>message.version=="ground-wire-v7" && OnlineProtocolIds.IsValid(message.matchId)
            && new[]{"hello","command","state","presentation","reply","control","control-reply","done","finish"}.Contains(message.kind)
            && (message.compressedState==null?message.expandedStateBytes==0:message.kind=="state" && message.state==null && message.compressedState.Length>0
                && message.expandedStateBytes>0 && message.expandedStateBytes<=OnlineFullRoomStateCodec.ExpandedLimit);
        public static IEnumerable<byte[]> Encode(OnlineGroundPassMessage message)
        {
            message.version="ground-wire-v7";
            if(message.state!=null && message.compressedState!=null)throw new InvalidDataException("Conflicting state encodings");
            var envelope=message.state?.catalogId==OnlineFullRoomBootstrap.Catalog?OnlineFullRoomStateCodec.Compress(message):message;
            foreach(var fragment in new OnlineReliableJsonWire<OnlineGroundPassMessage>(0xCA07,Valid).Encode(envelope))yield return fragment;
        }
        public OnlineGroundPassMessage Receive(byte[] packet,int length,bool allowConnectionRestart=false)
        {
            var message=framing.Receive(packet,length,allowConnectionRestart);
            return message?.compressedState==null?message:OnlineFullRoomStateCodec.Expand(message);
        }
    }

    /// <summary>Guest value replica. It has no gameplay manager, dice or clock references.</summary>
    public sealed class OnlineGroundPassReplica
    {
        private readonly string matchId;
        private readonly HashSet<string> roster;
        private readonly string catalog;
        private string currentJson;
        private long sequence = -1, revision = -1, rollSequence = -1;
        private string presentationJson;
        private long presentationFrame = -1;
        public OnlineGroundPassReplica(string matchId, IEnumerable<string> expectedRoster = null, string expectedCatalog = null)
        {
            OnlineProtocolIds.Require(matchId, nameof(matchId)); this.matchId = matchId;
            if (expectedCatalog != null) OnlineProtocolIds.Require(expectedCatalog, nameof(expectedCatalog));
            catalog = expectedCatalog;
            if (expectedRoster != null)
            {
                var ids = expectedRoster.ToArray();
                if (ids.Length == 0 || ids.Length > 64 || ids.Any(id => !OnlineProtocolIds.IsValid(id)) || ids.Distinct().Count() != ids.Length)
                    throw new ArgumentException("A distinct compatible roster is required", nameof(expectedRoster));
                roster = new HashSet<string>(ids, StringComparer.Ordinal);
            }
        }
        public OnlineGroundPassState State => currentJson == null ? null : JsonUtility.FromJson<OnlineGroundPassState>(currentJson);

        public bool ApplyPresentation(OnlineGroundPassPresentation frame)
        {
            bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100;
            if (frame == null || frame.matchId != matchId || frame.publicationSequence != sequence || frame.frameSequence < 1
                || frame.frameSequence < presentationFrame || !Finite(frame.ballX) || !Finite(frame.ballY) || !Finite(frame.ballZ)) return false;
            if(frame.players!=null)
            {
                var state=State;
                if(frame.players.Length>64 || state==null || frame.players.Any(p=>p==null || !state.tokens.Any(t=>t.id==p.id)
                    || !Finite(p.x) || !Finite(p.y) || !Finite(p.z)) || frame.players.Select(p=>p.id).Distinct().Count()!=frame.players.Length) return false;
            }
            var json = JsonUtility.ToJson(frame);
            if (frame.frameSequence == presentationFrame) return json == presentationJson;
            presentationFrame = frame.frameSequence; presentationJson = json; return true;
        }

        public bool Apply(OnlineGroundPassState state)
        {
            if (state == null || state.matchId != matchId || state.publicationSequence < 1 || state.revision < 0
                || state.tokens == null || state.tokens.Length > 64 || state.tokens.Any(t => t == null || !OnlineProtocolIds.IsValid(t.id))
                || state.tokens.Select(t => t.id).Distinct().Count() != state.tokens.Length
                || state.legalActions == null || state.legalActions.Length > 4096
                || state.legalActions.Any(a => !OnlineProtocolIds.IsValid(a))) return false;
            if (catalog != null && state.catalogId != catalog || roster != null && !roster.SetEquals(state.tokens.Select(token => token.id))) return false;
            if(!OnlineRosterPublication.Valid(state))return false;
            if(!string.IsNullOrEmpty(state.selectedPlayer) && !state.tokens.Any(token=>token.id==state.selectedPlayer)) return false;
            if (state.rollSequence < rollSequence || state.rollSequence < 0 || !ValidRolls(state) || !ValidHeader(state)) return false;
            var prior = State;
            if (prior?.rolls != null && state.rolls != null)
                foreach (var result in state.rolls)
                {
                    var observed = prior.rolls.FirstOrDefault(item => item.sequence == result.sequence);
                    if (observed != null && JsonUtility.ToJson(observed) != JsonUtility.ToJson(result)) return false;
                }
            var json = JsonUtility.ToJson(state);
            if (state.publicationSequence < sequence || state.revision < revision) return false;
            if (state.publicationSequence == sequence) return json == currentJson;
            currentJson = json; sequence = state.publicationSequence; revision = state.revision; rollSequence = state.rollSequence;
            return true;
        }

        private static bool ValidHeader(OnlineGroundPassState state)
        {
            var header=state.header;
            if(header==null) return true;
            bool Known(string id)=>!string.IsNullOrEmpty(id) && state.tokens.Any(t=>t.id==id);
            if(header.attackers==null || header.defenders==null || header.scores==null
                || header.attackers.Length>2 || header.defenders.Length>3 || header.scores.Length>5) return false;
            var nominations=header.attackers.Concat(header.defenders).ToArray();
            if(nominations.Any(id=>!Known(id)) || nominations.Distinct().Count()!=nominations.Length) return false;
            if(!string.IsNullOrEmpty(header.roller) && !nominations.Contains(header.roller)
                || !string.IsNullOrEmpty(header.winner) && !Known(header.winner)) return false;
            return header.scores.All(score=>score!=null && nominations.Contains(score.actorId) && score.roll>=1 && score.roll<=6
                && score.total>=-64 && score.total<=64)
                && header.scores.Select(score=>score.actorId).Distinct().Count()==header.scores.Length;
        }

        private static bool ValidRolls(OnlineGroundPassState state)
        {
            if (state.rolls == null) return true;
            if (state.rolls.Length > 64) return false;
            long previous = 0;
            foreach (var result in state.rolls)
            {
                if (result == null || result.sequence <= previous || result.sequence > state.rollSequence
                    || result.roll < 1 || result.roll > 6 || string.IsNullOrEmpty(result.context)
                    || result.isJackpot && (!result.jackpotEnabled || result.roll != 6)) return false;
                if (result.actorId != null && (!state.tokens.Any(token => token.id == result.actorId)
                    || result.team != (result.actorId.StartsWith("home:", StringComparison.Ordinal) ? "Home" : "Away"))) return false;
                previous = result.sequence;
            }
            return true;
        }
    }
}
