using System;
using System.Collections;
using System.Linq;
using UnityEngine.TestTools;

public sealed class MenuExitConfirmationPlayModeTests
{
    [UnityTest]
    public IEnumerator QuitFooterLabelsReceivePointerClicks() => Run(nameof(QuitFooterLabelsReceivePointerClicks));

    [UnityTest]
    public IEnumerator BackArrowShowsHoverWhileItHasFocus() => Run(nameof(BackArrowShowsHoverWhileItHasFocus));

    private static IEnumerator Run(string test)
    {
        var fixture = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("MenuExitConfirmationPointerFixture", throwOnError: true);
        return (IEnumerator)fixture.GetMethod(test).Invoke(null, null);
    }
}
