using System;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public static class OnlineContextKeyFixture
{
    // Only actions reached by the real restart fixtures below are translated.
    public static string Pick(OnlineGroundPassState state,string action)
    {
        KeyCode key=action switch
        {
            "set-piece.skip" or "movement.forfeit"=>KeyCode.X,"final-third.forfeit"=>KeyCode.Return,
            "set-piece.ground" or "throw.feet"=>KeyCode.P,"set-piece.high" or "throw.head"=>KeyCode.C,
            "set-piece.long"=>KeyCode.L,"set-piece.shot"=>KeyCode.S,"throw.movement"=>KeyCode.M,"throw.now"=>KeyCode.T,_=>KeyCode.None
        };
        if(key==KeyCode.None)return action;string picked=OnlineBoardActionPicker.Key(state,key);
        Assert.That(picked,Is.EqualTo(action),"Published "+state.stage+" must accept its existing "+key+" key");return picked;
    }
}
