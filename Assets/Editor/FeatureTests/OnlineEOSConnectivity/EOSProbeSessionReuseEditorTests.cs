using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineEOSConnectivity")]
public sealed class EOSProbeSessionReuseEditorTests
{
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator TwoSequentialSessionsLogInWithoutRestartingEditor()
    {
        const string marker = "Temp/OnlineEOSReuse.run";
        if (!File.Exists(marker))
            Assert.Ignore("The one-use EOS session-reuse marker is absent.");
        File.Delete(marker);

        string firstUserId;
        var first = new EOSProbeSession();
        try
        {
            var login = first.OpenAndLogin();
            while (login.MoveNext())
                yield return login.Current;
            firstUserId = first.UserId.ToString();
        }
        finally
        {
            first.Dispose();
        }

        var second = new EOSProbeSession();
        try
        {
            var login = second.OpenAndLogin();
            while (login.MoveNext())
                yield return login.Current;
            Assert.That(second.UserId.ToString(), Is.EqualTo(firstUserId),
                "A second focused probe can use the same local device identity without an Editor restart");
        }
        finally
        {
            second.Dispose();
        }
    }
}
