using System.Collections.Generic;
using UnityEngine;
namespace CounterAttack.Online
{
    public sealed partial class OnlineBoardInput
    {
        private readonly HashSet<HexCell> publishedHintHexes=new();
        private readonly Dictionary<HexCell,float> publishedPreviewHeights=new();
        private readonly HashSet<HexCell> publishedGoalMarkers=new();
        private readonly HashSet<HexCell> hostHintHexes=new();
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
            ClearHostHints();
            if(!isActiveAndEnabled || board==null || !board.liveFullRoom)return;
            if(publication==null || publication.connectionHeld || publication.isSharedPaused || publication.stage=="blocked")return;
            if(!OnlineRoomSession.IsGuest)
            {
                foreach(var hint in OnlinePublishedHexHints.Build(publication))
                {
                    var hex=board.grid.GetHexCellAt(new Vector3Int(hint.Key.x,0,hint.Key.y));
                    if(hex==null || hex.isInGoal!=0)continue;
                    hex.SetOnlineHostHint(hint.Value switch{
                        "PaceAvailable"=>Color.yellow,
                        "ReachOverlayAttacker"=>new Color(.18f,.83f,1,1),
                        _=>new Color(1,.48f,.05f,1)});
                    hostHintHexes.Add(hex);
                }
                return;
            }
            bool MarkerPermitted(HexCell hex)=>hex.isInGoal==0 || publication.hexPreviews==null
                || System.Array.Exists(publication.hexPreviews,p=>p.x==hex.coordinates.x && p.z==hex.coordinates.z && p.committedGoal);
            foreach(var hint in OnlinePublishedHexHints.Build(publication))
            {
                var hex=board.grid.GetHexCellAt(new Vector3Int(hint.Key.x,0,hint.Key.y));if(hex==null)continue;
                if(hint.Value=="passTargetCommitted" && !MarkerPermitted(hex))continue;
                hex.HighlightHex(hint.Value);publishedHintHexes.Add(hex);
            }
            foreach(var preview in publication.hexPreviews??System.Array.Empty<OnlineHexPreview>())
            {
                var hex=board.grid.GetHexCellAt(new Vector3Int(preview.x,0,preview.z));if(hex==null)continue;
                if(preview.committedGoal || Mathf.Abs(hex.transform.position.y-preview.height)>.000001f)publishedPreviewHeights[hex]=hex.transform.position.y;
                if(preview.committedGoal)publishedGoalMarkers.Add(hex);
                hex.SetCommittedGoalTarget(preview.committedGoal);var position=hex.transform.position;position.y=preview.height;hex.transform.position=position;
                hex.HighlightHex(preview.reason,preview.darkness);publishedHintHexes.Add(hex);
            }
            if(publication.hasSelectedTarget)
            {
                var hex=board.grid.GetHexCellAt(new Vector3Int(publication.selectedX,0,publication.selectedZ));
                if(hex!=null && MarkerPermitted(hex)){hex.HighlightHex("passTargetCommitted");publishedHintHexes.Add(hex);}
            }
        }
        internal void SynchronizePreviewMarkerPose()
        {if(board!=null && publishedGoalMarkers.Count>0)publishedGoalBallPose=board.displayedBall.position;}
        internal void AdvancePreviewMarkers()
        {
            if(board==null || publishedGoalMarkers.Count==0 || (board.displayedBall.position-publishedGoalBallPose).sqrMagnitude<=.0000000001f)return;
            foreach(var hex in publishedGoalMarkers)if(hex!=null)hex.SetCommittedGoalTarget(false);
            publishedGoalMarkers.Clear();
        }
        private void ClearHostHints()
        {
            foreach(var hex in hostHintHexes)if(hex!=null)hex.SetOnlineHostHint(null);
            hostHintHexes.Clear();
        }
        private void OnDisable(){SetLocalGoalHover(null);ClearHostHints();ClearPublishedHighlights();}
    }
}
