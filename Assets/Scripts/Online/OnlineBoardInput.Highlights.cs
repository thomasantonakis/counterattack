using System;
using System.Collections.Generic;
using UnityEngine;
namespace CounterAttack.Online
{
    public sealed partial class OnlineBoardInput
    {
        private readonly HashSet<HexCell> publishedHintHexes=new();
        private readonly Dictionary<HexCell,float> publishedPreviewHeights=new();
        private readonly HashSet<HexCell> publishedGoalMarkers=new();
        private readonly HashSet<HexCell> passHoverHexes = new();
        private HexCell passHoverOrigin, passHoverTarget;
        private long passHoverRevision = -1;
        private Vector3 publishedGoalBallPose;
        private void ClearPublishedHighlights()
        {
            foreach(var item in publishedPreviewHeights)if(item.Key!=null)
            {
                item.Key.SetCommittedGoalTarget(false);var position=item.Key.transform.position;position.y=item.Value;item.Key.transform.position=position;
            }
            publishedPreviewHeights.Clear();publishedGoalMarkers.Clear();
            foreach(var hex in publishedHintHexes)if(hex!=null)hex.ResetHighlight();
            publishedHintHexes.Clear();
        }
        private void RefreshPublishedHighlights(OnlineGroundPassState publication)
        {
            ClearPublishedHighlights();
            PresentGroundPassHover(passHoverTarget);
            if(!isActiveAndEnabled || board==null || !board.liveFullRoom)return;
            if(publication==null || publication.connectionHeld || publication.isSharedPaused || publication.stage=="blocked")return;
            if (!OnlineRoomSession.IsGuest) return;

            // Replicate the authority's gameplay previews without deriving colors from legal actions.
            foreach(var preview in publication.hexPreviews??System.Array.Empty<OnlineHexPreview>())
            {
                var hex=board.grid.GetHexCellAt(new Vector3Int(preview.x,0,preview.z));if(hex==null)continue;
                if(preview.committedGoal || Mathf.Abs(hex.transform.position.y-preview.height)>.000001f)publishedPreviewHeights[hex]=hex.transform.position.y;
                if(preview.committedGoal)publishedGoalMarkers.Add(hex);
                hex.SetCommittedGoalTarget(preview.committedGoal);var position=hex.transform.position;position.y=preview.height;hex.transform.position=position;
                hex.HighlightHex(preview.reason,preview.darkness);publishedHintHexes.Add(hex);
            }
        }

        private void PresentGroundPassHover(HexCell target)
        {
            var origin = board?.match?.ball?.GetCurrentHex();
            bool ftp = state?.stage == "ftp.target";
            if (target != null)
            {
                var coordinates = target.coordinates;
                var action = ftp ? $"ftp.target:{coordinates.x}:{coordinates.z}"
                    : OnlineGroundPassAuthority.TargetAction(coordinates.x, coordinates.z);
                if (origin == null || (!ftp && state?.stage != "target") || board.match.difficulty_level != 1
                    || board.controls?.CanSubmitAction(action) != true)
                    target = null;
            }

            long revision = state?.revision ?? -1;
            if (target == passHoverTarget && origin == passHoverOrigin && revision == passHoverRevision) return;
            foreach (var hex in passHoverHexes) if (hex != null) hex.SetLocalPassPreview(null);
            passHoverHexes.Clear();
            passHoverTarget = target;
            passHoverOrigin = origin;
            passHoverRevision = revision;
            if (target == null) return;

            var match = board.match;
            bool safeKickoff = !ftp && state.kickoffGroundPass && GroundPassCommon.IsInAttackingTeamOwnHalf(match, target);
            bool dangerous = !safeKickoff && GroundPassCommon.CountDangerousInteractions(
                GroundPassCommon.BuildOrderedBallPathInteractions(board.grid, match.ball, match.groundBallManager.goalKeeperManager, target,
                    isQuickThrow: !ftp && match.currentState == MatchManager.GameState.QuickThrow)) > 0;
            var selected = state.hasSelectedTarget
                ? board.grid.GetHexCellAt(new Vector3Int(state.selectedX, 0, state.selectedZ)) : null;

            passHoverHexes.UnionWith(GroundPassCommon.CalculateThickPath(board.grid, origin, target, match.ball.PhysicalRadius));
            passHoverHexes.Add(target);
            foreach (var hex in passHoverHexes)
                if (hex != null) hex.SetLocalPassPreview(GroundPassCommon.HoverPreviewReason(hex, target, selected, dangerous));
        }

        private bool IsAssistedTargetHover(HexCell hex)
        {
            if (hex == null || !board.liveFullRoom || board.match.difficulty_level >= 3) return false;
            var action = OnlineBoardActionPicker.Hex(state, hex.coordinates.x, hex.coordinates.z);
            if (action == null || board.controls?.CanSubmitAction(action) != true) return false;

            var actionKind = action.Substring(0, action.IndexOf(':'));
            return actionKind switch
            {
                "ground.target" or "ftp.target" or "ftp.move" => board.match.difficulty_level == 2,
                "long.target" or "high.target" or "penalty.target" => true,
                "set-piece.target" => board.match.difficulty_level == 1,
                "shot.target" => board.match.difficulty_level == 2,
                "shot.move" => hex.BasePreviewHighlight.Reason != null,
                "header.target" => hex.BasePreviewHighlight.Reason is "HeaderTargetFree" or "HeaderTargetRisk",
                "kickoff.move" => false,
                _ => hex.BasePreviewHighlight.Reason is "PaceAvailable" or "PaceRisk" or "DangerousTackle" or "reposition"
            };
        }

        private void PresentTargetHover(HexCell hex)
        {
            var goalAction = GoalHoverAction(hex);
            SetLocalGoalHover(IsAssistedTargetHover(hex)
                || (goalAction != null && board.controls?.CanSubmitAction(goalAction) == true) ? hex : null);
        }
        internal void SynchronizePreviewMarkerPose()
        {if(board!=null && publishedGoalMarkers.Count>0)publishedGoalBallPose=board.displayedBall.position;}
        internal void AdvancePreviewMarkers()
        {
            if(board==null || publishedGoalMarkers.Count==0 || (board.displayedBall.position-publishedGoalBallPose).sqrMagnitude<=.0000000001f)return;
            foreach(var hex in publishedGoalMarkers)if(hex!=null)hex.SetCommittedGoalTarget(false);
            publishedGoalMarkers.Clear();
        }
        private void OnDisable(){SetLocalGoalHover(null);PresentPeerHover(null);PresentGroundPassHover(null);ClearPublishedHighlights();}
    }
}
