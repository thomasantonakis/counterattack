using System.Collections.Generic;
using UnityEngine;
namespace CounterAttack.Online
{
    public sealed partial class OnlineBoardInput
    {
        private readonly HashSet<HexCell> publishedHintHexes=new();
        private void ClearPublishedHighlights()
        {
            foreach(var hex in publishedHintHexes)if(hex!=null)hex.ResetHighlight();
            publishedHintHexes.Clear();
        }
        private void RefreshPublishedHighlights(OnlineGroundPassState publication)
        {
            ClearPublishedHighlights();
            if(!isActiveAndEnabled || board==null || !board.liveFullRoom || !OnlineRoomSession.IsGuest)return;
            foreach(var hint in OnlinePublishedHexHints.Build(publication))
            {
                var hex=board.grid.GetHexCellAt(new Vector3Int(hint.Key.x,0,hint.Key.y));if(hex==null)continue;
                hex.HighlightHex(hint.Value);publishedHintHexes.Add(hex);
            }
        }
        private void OnDisable()=>ClearPublishedHighlights();
    }
}
