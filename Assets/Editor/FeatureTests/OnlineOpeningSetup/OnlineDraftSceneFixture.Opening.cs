using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class OnlineDraftSceneFixture
{
    public IEnumerator RunOpening(bool localHome,bool kickoff)
    {
        yield return RunVariant("International",localHome);
        var view=Object.FindAnyObjectByType<OnlineDraftView>();var client=localHome?guest.Draft:host.Draft;
        var other=localHome?host.Draft:guest.Draft;Assert.That(other.Request(OnlineDraftIntent.Ready),Is.True);Flush();
        Assert.That(host.Draft.Request(OnlineDraftIntent.Start),Is.True);Flush();
        var coin = view.sourceUi.coinTossPanel;
        Assert.That(client.Capture().phase,Is.EqualTo("opening-roll"));Assert.That(coin.dicePanel.visuals.activeInHierarchy,Is.True);
        Assert.That(view.source.draftOrderPanel.visuals.activeSelf,Is.False);
        Assert.That(coin.dicePanel.title.text,Does.Contain("COIN TOSS"));
        yield return null;
        var footer = coin.dicePanel.footer;
        Assert.That(footer.color, Is.EqualTo(Color.white));
        Assert.That(footer.font.name, Does.Contain("Poppins-Regular"));
        Assert.That(footer.rectTransform.IsChildOf(coin.dicePanel.panel), Is.True);
        if (localHome) Assert.That(footer.text, Does.StartWith("Press [R] to roll, Autorolling in "));
        rolls.Enqueue(6);rolls.Enqueue(1);
        for(int i=0;i<2;i++)
        {
            var s=client.Capture();Assert.That(view.actionButton.interactable,Is.EqualTo(s.turn==client.Team));
            if(s.turn==client.Team)view.actionButton.onClick.Invoke();else Client(s.turn).Request(OnlineDraftIntent.Roll);Flush();
        }
        Assert.That(client.Capture().phase,Is.EqualTo("opening-choice"));Assert.That(coin.kickoffButton.gameObject.activeInHierarchy,Is.True);
        Assert.That(coin.kickoffButton.interactable,Is.EqualTo(localHome));
        long revision=client.Capture().revision;view.SelectSlot(client.Team,1);view.SelectSlot(client.Team,12);Flush();Assert.That(client.Capture().revision,Is.EqualTo(revision),"Confirmed rosters are frozen during the toss");
        var choice=kickoff?CoinTossChoice.Kickoff:CoinTossChoice.RightToLeft;
        if(localHome){if(kickoff)coin.kickoffButton.onClick.Invoke();else coin.rightToLeftButton.onClick.Invoke();}
        else Client(OnlineTeam.Home).Request(OnlineDraftIntent.Choose,choice:choice);
        Flush();
        if(kickoff)
        {
            Assert.That(coin.kickoffButton.gameObject.activeSelf,Is.False);Assert.That(coin.leftToRightButton.interactable,Is.EqualTo(!localHome));
            if(!localHome)coin.leftToRightButton.onClick.Invoke();else Client(OnlineTeam.Away).Request(OnlineDraftIntent.Choose,choice:CoinTossChoice.LeftToRight);Flush();
        }
        var final=client.Capture();Assert.That(final.phase,Is.EqualTo("room-ready"));Assert.That(final.openingSetup,Is.Not.Null);
        Assert.That(final.openingSetup.homeHasFirstHalfKickoff,Is.EqualTo(kickoff));Assert.That(final.openingSetup.homeAttacksLeftToRight,Is.False);
        Assert.That(coin.dicePanel.visuals.activeSelf,Is.False);
        Assert.That(view.actionButton.interactable,Is.False);Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));
    }
}
