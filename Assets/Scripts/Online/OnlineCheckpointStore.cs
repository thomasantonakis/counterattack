using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    public sealed class OnlineCheckpoint
    {
        public const int SchemaVersion = 1;
        public int schemaVersion = SchemaVersion;
        public Guid matchId;
        public Guid checkpointId;
        public DateTime savedUtc;
        public string displayName;
        public string protocol, rules, catalog;
        public string hostAccount, guestAccount;
        public MatchManager.GameData match;

        public bool IsHost(string authenticatedAccount) =>
            !string.IsNullOrEmpty(authenticatedAccount) && authenticatedAccount == hostAccount;

        public bool IsGuest(string authenticatedAccount) =>
            !string.IsNullOrEmpty(authenticatedAccount) && authenticatedAccount == guestAccount;
    }

    // This file format belongs to the host. It is never a network bootstrap.
    public sealed class OnlineCheckpointSummary
    {
        public Guid Slot { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Teams { get; internal set; }
        public string Score { get; internal set; }
        public string Clock { get; internal set; }
        public DateTime SavedUtc { get; internal set; }
    }

    public sealed class OnlineCheckpointStore
    {
        private static readonly object WriteGate = new();
        private readonly string directory;
        internal string FolderPath => directory;
        private readonly OnlineCompatibility compatibility;
        private readonly Action<string, string, bool> publish;

        public OnlineCheckpointStore() : this(
            Path.Combine(Application.persistentDataPath, "OnlineSaves"), OnlineFrontendSessionPolicy.Compatibility())
        {
        }

        public OnlineCheckpointStore(string directory, OnlineCompatibility compatibility)
            : this(directory, compatibility, Publish)
        {
        }

        internal OnlineCheckpointStore(string directory, OnlineCompatibility compatibility, Action<string, string, bool> publish)
        {
            this.directory = Path.GetFullPath(directory);
            this.compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            this.publish = publish ?? throw new ArgumentNullException(nameof(publish));
        }

        public OnlineCheckpoint Load(Guid slotId)
        {
            var document = JObject.Parse(File.ReadAllText(SlotPath(slotId)));
            if (document["schemaVersion"]?.Type != JTokenType.Integer
                || document["schemaVersion"].Value<long>() != OnlineCheckpoint.SchemaVersion)
                throw new InvalidDataException("This online save uses an unsupported format.");

            var snapshot = document["match"]?["runtimeSnapshot"] as JObject;
            if (snapshot == null || new[] { "version", "currentState", "teamInAttack", "attackHasPossession",
                    "homeTeamDirection", "awayTeamDirection", "clock", "ball", "tokens", "touchReferences",
                    "offside", "substitutions", "stats", "homeScorers", "awayScorers", "gameLog", "decisionTiming" }
                .Any(field => snapshot[field] == null || snapshot[field].Type == JTokenType.Null))
                throw new InvalidDataException("This online save is missing match state.");

            var checkpoint = document.ToObject<OnlineCheckpoint>();
            Validate(checkpoint);
            return checkpoint;
        }

        public IReadOnlyList<OnlineCheckpointSummary> Browse(out int unreadable)
        {
            unreadable = 0;
            var saves = new List<OnlineCheckpointSummary>();
            if (!Directory.Exists(directory)) return saves;
            foreach (string path in Directory.EnumerateFiles(directory, "*.json"))
            {
                try
                {
                    if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var slot))
                    {
                        unreadable++;
                        continue;
                    }
                    var checkpoint = Load(slot);
                    var data = checkpoint.match;
                    var clock = data.runtimeSnapshot.clock;
                    int time = Math.Max(0, (int)clock.currentHalfRegulationSeconds);
                    saves.Add(new OnlineCheckpointSummary
                    {
                        Slot = slot,
                        SavedUtc = checkpoint.savedUtc,
                        DisplayName = checkpoint.displayName,
                        Teams = $"{data.gameSettings.homeTeamName} vs {data.gameSettings.awayTeamName}",
                        Score = $"{data.stats.homeTeamStats?.totalGoals ?? 0}–{data.stats.awayTeamStats?.totalGoals ?? 0}",
                        Clock = $"H{clock.currentHalf} {time / 60:00}:{time % 60:00}"
                    });
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
                    or ArgumentException or FormatException or OverflowException)
                {
                    unreadable++;
                }
            }
            return saves.OrderByDescending(save => save.SavedUtc).ToArray();
        }

        public Guid Save(OnlineCheckpoint checkpoint, Guid? selectedSlot = null)
        {
            lock (WriteGate)
            {
                Validate(checkpoint);
                var slot = selectedSlot ?? Guid.NewGuid();
                string destination = SlotPath(slot);
                if (selectedSlot.HasValue)
                {
                    var previous = Load(slot);
                    if (previous.matchId != checkpoint.matchId || previous.hostAccount != checkpoint.hostAccount
                        || previous.guestAccount != checkpoint.guestAccount)
                        throw new InvalidDataException("The selected slot belongs to another match.");
                }

                Directory.CreateDirectory(directory);
                string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 4096, leaveOpen: true))
                            writer.Write(JsonConvert.SerializeObject(checkpoint, Formatting.Indented));
                        stream.Flush(flushToDisk: true);
                    }
                    publish(temporary, destination, selectedSlot.HasValue);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
                return slot;
            }
        }

        private static void Publish(string temporary, string destination, bool replace)
        {
            if (replace) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }

        public MatchManager.GameData CreateRun(OnlineCheckpoint checkpoint)
        {
            Validate(checkpoint);
            var data = OnlineRoomLaunchData.Copy(checkpoint.match);
            data.playSessionId = Guid.NewGuid().ToString("N");
            data.playSessionStartedUtc = DateTime.UtcNow.ToString("O");
            data.sourceSavePath = null;
            data.sourceSaveSavedAtUtc = checkpoint.savedUtc.ToString("O");
            data.inheritedEventSequence = data.events.LastOrDefault()?.sequenceNumber ?? 0;
            return data;
        }

        // These ordinary choice points are rebuilt by RefreshAvailableActionsForCurrentState.
        // The Room guard separately requires possession and all action managers to be settled.
        public static bool SupportedState(string state) => state is
            nameof(MatchManager.GameState.EndOfMovementPhase) or
            nameof(MatchManager.GameState.EndOfStandardPass) or
            nameof(MatchManager.GameState.EndOfFirstTimePass) or
            nameof(MatchManager.GameState.EndOfLongBall) or
            nameof(MatchManager.GameState.SuccessfulTackle) or
            nameof(MatchManager.GameState.BallControl) or
            nameof(MatchManager.GameState.HeaderCompleted) or
            nameof(MatchManager.GameState.AnyOtherScenario);

        private string SlotPath(Guid slot)
        {
            if (slot == Guid.Empty) throw new ArgumentException("An online save slot is required.", nameof(slot));
            return Path.Combine(directory, slot.ToString("N") + ".json");
        }

        private void Validate(OnlineCheckpoint checkpoint)
        {
            if (checkpoint == null || checkpoint.schemaVersion != OnlineCheckpoint.SchemaVersion
                || checkpoint.matchId == Guid.Empty || checkpoint.checkpointId == Guid.Empty
                || checkpoint.savedUtc.Kind != DateTimeKind.Utc || checkpoint.savedUtc == default
                || string.IsNullOrWhiteSpace(checkpoint.displayName) || checkpoint.displayName.Length > 100
                || !OnlineProtocolIds.IsValid(checkpoint.hostAccount) || !OnlineProtocolIds.IsValid(checkpoint.guestAccount)
                || checkpoint.hostAccount == checkpoint.guestAccount)
                throw new InvalidDataException("This online save has invalid checkpoint metadata.");

            if (!OnlineProtocolIds.IsValid(checkpoint.protocol) || checkpoint.rules != compatibility.Rules
                || checkpoint.catalog != compatibility.Catalog)
                throw new InvalidDataException("This online save requires a different game version or player catalog.");

            var match = checkpoint.match;
            var snapshot = match?.runtimeSnapshot;
            if (match?.gameSettings?.gameMode != ApplicationManager.OnlineGameMode
                || string.IsNullOrWhiteSpace(match.gameSettings.homeTeamName)
                || string.IsNullOrWhiteSpace(match.gameSettings.awayTeamName)
                || match.rosters?.home?.Count is not (16 or 18) || match.rosters.away?.Count != match.rosters.home.Count
                || match.stats == null || match.openingSetup == null || match.events == null
                || snapshot == null || snapshot.version != RoomSaveService.SaveSchemaVersion
                || !SupportedState(snapshot.currentState) || !snapshot.attackHasPossession
                || snapshot.teamInAttack is not ("Home" or "Away")
                || !Enum.TryParse<MatchManager.TeamAttackingDirection>(snapshot.homeTeamDirection, out var homeDirection)
                || !Enum.IsDefined(typeof(MatchManager.TeamAttackingDirection), homeDirection)
                || !Enum.TryParse<MatchManager.TeamAttackingDirection>(snapshot.awayTeamDirection, out var awayDirection)
                || !Enum.IsDefined(typeof(MatchManager.TeamAttackingDirection), awayDirection) || homeDirection == awayDirection
                || snapshot.clock == null || snapshot.clock.currentHalf < 1 || snapshot.clock.totalCompletedActions < 1
                || float.IsNaN(snapshot.clock.currentHalfRegulationSeconds) || float.IsInfinity(snapshot.clock.currentHalfRegulationSeconds)
                || snapshot.clock.currentHalfRegulationSeconds < 0
                || snapshot.clock.isMatchComplete || snapshot.clock.isWaitingForExtraActionsRoll
                || snapshot.ball == null || snapshot.tokens == null || snapshot.tokens.Count != 2 * match.rosters.home.Count
                || snapshot.tokens.Any(t => t == null || string.IsNullOrEmpty(t.tokenKey) || t.isPlaying && t.currentHex == null)
                || snapshot.tokens.Select(t => t.tokenKey).Distinct().Count() != snapshot.tokens.Count
                || snapshot.touchReferences == null || snapshot.offside == null || snapshot.substitutions == null
                || snapshot.stats == null || snapshot.homeScorers == null || snapshot.awayScorers == null || snapshot.gameLog == null
                || snapshot.decisionTiming?.decisions == null || snapshot.decisionTiming.rollId != null
                || snapshot.decisionTiming.rollKey != null)
                throw new InvalidDataException("This online save does not contain a supported match checkpoint.");

            int previousSequence = 0;
            foreach (var entry in match.events)
            {
                if (entry == null || entry.sequenceNumber <= previousSequence)
                    throw new InvalidDataException("This online save has invalid event history.");
                previousSequence = entry.sequenceNumber;
            }
        }
    }
}
