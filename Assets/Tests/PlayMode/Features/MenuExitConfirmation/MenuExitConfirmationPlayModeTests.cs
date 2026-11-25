using System;
using System.Collections;
using System.Linq;
using UnityEngine.TestTools;

public sealed class MenuExitConfirmationPlayModeTests
{
    [UnityTest]
    public IEnumerator QuitFooterLabelsReceivePointerClicks()
    {
        var fixture = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("MenuExitConfirmationPointerFixture", throwOnError: true);
        return (IEnumerator)fixture.GetMethod(nameof(QuitFooterLabelsReceivePointerClicks)).Invoke(null, null);
    }
}
