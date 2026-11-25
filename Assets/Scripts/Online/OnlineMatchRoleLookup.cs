using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using UnityEngine;

namespace CounterAttack.Online
{
    public sealed class OnlineMatchRoleOptions
    {
        public OnlineMatchRoleOptions(bool found, bool canJoinAway, bool canSpectate)
        {
            Found = found;
            CanJoinAway = canJoinAway;
            CanSpectate = canSpectate;
        }

        public bool Found { get; }
        public bool CanJoinAway { get; }
        public bool CanSpectate { get; }
    }

    internal static class OnlineMatchRoleLookup
    {
        internal static IEnumerator Find(string code, Action<OnlineMatchRoleOptions> complete)
        {
            using var session = new EOSStandaloneSession();
            var login = session.OpenAndLogin();
            using (login as IDisposable)
                while (login.MoveNext()) yield return login.Current;

            var lobby = session.Platform.GetLobbyInterface();
            var search = new CreateLobbySearchOptions { MaxResults = 2 };
            EOSProofCheck.Require(lobby.CreateLobbySearch(ref search, out var manager) == Result.Success, "Creating the manager lookup");
            try
            {
                EOSProofCheck.Require(lobby.CreateLobbySearch(ref search, out var viewer) == Result.Success, "Creating the spectator lookup");
                try
                {
                    string lookup = OnlineSpectatorAdmission.Lookup(code);
                    Configure(manager, lookup, EOSStandalonePacketProbe.LobbyBucket);
                    Configure(viewer, lookup, EOSStandalonePacketProbe.LiveWatchLobbyBucket);
                    var find = new LobbySearchFindOptions { LocalUserId = session.UserId };
                    Result? managerResult = null, viewerResult = null;
                    manager.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => managerResult = info.ResultCode);
                    viewer.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => viewerResult = info.ResultCode);
                    double deadline = Time.realtimeSinceStartupAsDouble + 15;
                    while ((!managerResult.HasValue || !viewerResult.HasValue) && Time.realtimeSinceStartupAsDouble < deadline)
                    {
                        session.Tick();
                        yield return null;
                    }
                    EOSProofCheck.Require(managerResult is Result.Success or Result.NotFound
                        || viewerResult is Result.Success or Result.NotFound, "Match Code lookup failed. Try again.");
                    var away = managerResult == Result.Success ? Availability(manager, session.UserId, code) : (found: false, open: false);
                    var spectators = viewerResult == Result.Success ? Availability(viewer, session.UserId) : (found: false, open: false);
                    if (!spectators.found && viewerResult is Result.Success or Result.NotFound)
                    {
                        double retryUntil = Time.realtimeSinceStartupAsDouble + 5;
                        while (!spectators.found && Time.realtimeSinceStartupAsDouble < retryUntil)
                        {
                            double retryAt = Time.realtimeSinceStartupAsDouble + .75;
                            while (Time.realtimeSinceStartupAsDouble < retryAt)
                            {
                                session.Tick();
                                yield return null;
                            }
                            viewerResult = null;
                            viewer.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => viewerResult = info.ResultCode);
                            while (!viewerResult.HasValue && Time.realtimeSinceStartupAsDouble < retryUntil)
                            {
                                session.Tick();
                                yield return null;
                            }
                            if (viewerResult == Result.Success) spectators = Availability(viewer, session.UserId);
                            else if (viewerResult != Result.NotFound) break;
                        }
                    }
                    complete(new OnlineMatchRoleOptions(away.found || spectators.found, away.open, spectators.open));
                }
                finally { viewer.Release(); }
            }
            finally { manager.Release(); }
        }

        private static void Configure(LobbySearch search, string lookup, string bucket)
        {
            var code = new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = OnlineFrontendSessionPolicy.LobbyCodeAttribute, Value = lookup }
            };
            var scope = new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = LobbyInterface.SEARCH_BUCKET_ID, Value = bucket }
            };
            EOSProofCheck.Require(search.SetParameter(ref code) == Result.Success
                && search.SetParameter(ref scope) == Result.Success, "Configuring the Match Code lookup");
        }

        private static (bool found, bool open) Availability(LobbySearch search, ProductUserId local, string managerCode = null)
        {
            var count = new LobbySearchGetSearchResultCountOptions();
            uint matches = search.GetSearchResultCount(ref count);
            if (matches == 0) return (false, false);
            OnlineFrontendSessionPolicy.RequireUniqueLobbyCodeMatch(matches);
            var copy = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = 0 };
            EOSProofCheck.Require(search.CopySearchResultByIndex(ref copy, out var details) == Result.Success, "Reading the match lobby");
            try
            {
                var options = new LobbyDetailsCopyInfoOptions();
                EOSProofCheck.Require(details.CopyInfo(ref options, out var info) == Result.Success && info != null, "Reading open lobby seats");
                var owner = new LobbyDetailsGetLobbyOwnerOptions();
                if (details.GetLobbyOwner(ref owner)?.ToString() == local.ToString()) return (true, false);
                if (managerCode != null && IsAwayReservedForSomeoneElse(details, managerCode, local)) return (true, false);
                return (true, info.Value.AvailableSlots > 0);
            }
            finally { details.Release(); }
        }

        internal static bool IsAwayReservedForSomeoneElse(LobbyDetails details, string code, ProductUserId local)
        {
            var lookup = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = OnlineSpectatorAdmission.AwayReservationAttribute };
            if (details.CopyAttributeByKey(ref lookup, out var attribute) != Result.Success)
                return false;
            return !string.Equals(attribute?.Data?.Value.AsUtf8?.ToString(),
                OnlineSpectatorAdmission.AwayReservation(code, local.ToString()), StringComparison.Ordinal);
        }
    }
}
