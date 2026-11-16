using System;
using System.Linq;
using UnityEngine;
namespace CounterAttack.Online
{
    [DefaultExecutionOrder(-31000)]
    public sealed class OnlineFullRoomBootstrap:MonoBehaviour
    {
        public const string SceneName="OnlineRoom",Catalog="full-room-v8";
        public OnlineGroundPassBoard board;
        public Renderer replicaBallRenderer;
        public bool Ready {get;private set;}
        public void Awake()
        {if(OnlineRoomSession.Current?.HasInitialData==true)Initialize(OnlineRoomSession.Current.CaptureInitialData());}
        public void Initialize(MatchManager.GameData initial)
        {
            if(Ready)throw new InvalidOperationException("Full Room already initialized");
            bool guest=OnlineRoomSession.IsGuest;board.liveFullRoom=true;board.continuousManualSession=true;board.localTeam=guest?OnlineTeam.Home:OnlineTeam.Away;
            board.match.gameData=OnlineRoomLaunchData.Copy(initial);
            if(!guest)
            {
                foreach(Transform root in board.authorityRoot.transform)root.gameObject.SetActive(true);
                board.authorityRoot.SetActive(true);
            }
            MatchManager.Instance=board.match;board.match.InitializeAuthoredOnlineMatch(initial);board.grid.BindAuthoredOnlineCells(board.cells);
            board.match.BeginGameplayEventLoggingSuppression();
            try
            {
                board.tokens=board.match.playerTokenManager.BindAuthoredOnlineRoster(board.tokens,board.match.gameData);
                board.match.ball.PlaceAtCell(board.grid.GetHexCellAt(Vector3Int.zero));
            }
            finally{board.match.EndGameplayEventLoggingSuppression();}
            board.displayedTokens=board.tokens.Select(t=>t.transform).ToArray();
            board.displayedBall.gameObject.SetActive(guest);
            if(replicaBallRenderer!=null)
            {
                var color=initial.gameSettings.ballColor=="Orange"?new Color32(230,121,36,255):initial.gameSettings.ballColor=="Yellow"?new Color32(235,214,58,255):new Color32(244,246,250,255);
                var properties=new MaterialPropertyBlock();properties.SetColor("_Color",color);properties.SetColor("_BaseColor",color);replicaBallRenderer.SetPropertyBlock(properties);
            }
            if(!guest)board.match.kickoffManager.StartPreKickoffPhase();
            Ready=true;
        }
    }
}
