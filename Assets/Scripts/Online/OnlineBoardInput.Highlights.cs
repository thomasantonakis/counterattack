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
            if (publication?.stage != "target" || publication.connectionHeld || publication.isSharedPaused)
                PresentGroundPassHover(null);
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
            if (target != null && (origin == null || state?.stage != "target" || board.match.difficulty_level == 3
                || board.controls?.CanSubmitAction(OnlineGroundPassAuthority.TargetAction(target.coordinates.x, target.coordinates.z)) != true))
                target = null;
            if (target == passHoverTarget && origin == passHoverOrigin) return;
            foreach (var hex in passHoverHexes) if (hex != null) hex.SetOnlinePassHover(false);
            passHoverHexes.Clear();
            passHoverTarget = target;
            passHoverOrigin = origin;
            if (target == null) return;

            foreach (var hex in GroundPassCommon.CalculateThickPath(board.grid, origin, target, board.match.ball.ballRadius))
                passHoverHexes.Add(hex);
            passHoverHexes.Add(target);
            foreach (var hex in passHoverHexes) hex.SetOnlinePassHover(true);
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
