using System;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlinePublishedHighlights")]
public sealed class OnlinePublishedHighlightsEditorTests
{
    private static OnlineGroundPassState State()=>new(){legalActions=new[]{"movement.target:2:3"},tokens=Array.Empty<OnlineGroundPassTokenState>()};
    [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)]
    public void HoldAndPauseClearHints(bool held,bool paused)
    {var s=State();s.connectionHeld=held;s.isSharedPaused=paused;Assert.That(OnlinePublishedHexHints.Build(s).Count,Is.EqualTo(held||paused?0:1));}
    [TestCase("unique",1)][TestCase("duplicate",1)][TestCase("ambiguous",0)][TestCase("noncanonical",0)][TestCase("plus",0)][TestCase("invalid",0)]
    public void DestinationsMatchClickablePublishedChoices(string kind,int count)
    {
        var s=State();if(kind=="duplicate")s.legalActions=new[]{s.legalActions[0],s.legalActions[0]};
        if(kind=="ambiguous")s.legalActions=new[]{s.legalActions[0],"ground.target:2:3"};
        if(kind=="plus")s.legalActions=new[]{"movement.target:+2:3"};if(kind=="noncanonical")s.legalActions=new[]{"movement.target:02:3"};if(kind=="invalid")s.legalActions=new[]{"movement.target:bad:3"};
        var hints=OnlinePublishedHexHints.Build(s);Assert.That(hints.Count,Is.EqualTo(count));if(count==1)Assert.That(hints[new Vector2Int(2,3)],Is.EqualTo("ReachOverlayAttacker"));
    }
    [TestCase("eligible","PaceAvailable")][TestCase("selected","MovementDestinationHover")][TestCase("target","passTargetCommitted")]
    public void SelectionPrecedence(string kind,string expected)
    {
        var s=State();s.tokens=new[]{new OnlineGroundPassTokenState{id="home:2",onBoard=true,visible=true,x=2,z=3}};s.legalActions=new[]{"kickoff.player:home:2"};
        if(kind!="eligible")s.selectedPlayer="home:2";if(kind=="target"){s.hasSelectedTarget=true;s.selectedX=2;s.selectedZ=3;}
        Assert.That(OnlinePublishedHexHints.Build(s)[new Vector2Int(2,3)],Is.EqualTo(expected));
    }
    [TestCase(false,true)][TestCase(true,false)]
    public void HiddenAndOffboardPlayersNeverSupplyPitchHints(bool visible,bool onBoard)
    {
        var s=State();s.tokens=new[]{new OnlineGroundPassTokenState{id="home:2",onBoard=onBoard,visible=visible,x=7,z=8}};s.legalActions=new[]{"kickoff.player:home:2"};s.selectedPlayer="home:2";
        Assert.That(OnlinePublishedHexHints.Build(s),Is.Empty);
    }
}
