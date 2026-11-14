using System.Linq;

namespace CounterAttack.Online
{
    public static class OnlineManualSessionPolicy
    {
        public static string Catalog(bool continuous)=>continuous?"continuous-board-v1":"prepared-board-v1";
        public static bool CanFinish(OnlineGroundPassState state,bool continuous)
        {
            if(state==null || state.passActive || state.ballMoving || state.playerMoving || state.isSharedPaused || state.connectionHeld)return false;
            if(continuous)return state.gameState==MatchManager.GameState.MatchEnded.ToString();
            return (state.stage=="continuation" || state.stage=="action") && state.tokens!=null
                && state.tokens.Any(token=>token.passesAttempted==1);
        }
    }
}
