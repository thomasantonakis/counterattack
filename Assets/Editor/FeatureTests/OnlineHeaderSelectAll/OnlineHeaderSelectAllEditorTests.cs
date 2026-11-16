using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineHeaderSelectAll")]
public sealed class OnlineHeaderSelectAllEditorTests
{
    [Test] public void ASelectsPublishedHeaderAll()=>Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{legalActions=new[]{"header.all"}},KeyCode.A),Is.EqualTo("header.all"));
    [Test] public void AmbiguousAIsNotGuessed()=>Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{legalActions=new[]{"header.all","foul.advantage"}},KeyCode.A),Is.Null);
}
