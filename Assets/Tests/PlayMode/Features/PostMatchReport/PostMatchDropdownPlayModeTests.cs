using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("PostMatchPitch")]
public sealed class PostMatchDropdownPlayModeTests
{
    [UnityTest]
    public IEnumerator EachMenuOpensAllOptionsWithoutScrollingAndSupportsSelectionAndReopening()
    {
        var fixture = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("PostMatchDropdownFixture", true);
        return (IEnumerator)fixture.GetMethod("Run").Invoke(null, null);
    }
}
