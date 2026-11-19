using System;
using System.IO;
using NUnit.Framework;
[Category("OnlineAuthRenewal")]
public sealed class OnlineAuthSoakArgumentsEditorTests
{
    [TestCase("login","1",true)][TestCase("login","3900",true)][TestCase("login","7500",true)]
    [TestCase("host","1",false)][TestCase("local","1",false)][TestCase("guest","1",false)]
    [TestCase("login","0",false)][TestCase("login","7501",false)][TestCase("login","-1",false)][TestCase("login","invalid",false)]
    public void SoakIsBoundedAndLoginOnly(string role,string seconds,bool valid)
    {
        string output=Path.Combine(Path.GetTempPath(),"CA-auth-args-"+Guid.NewGuid().ToString("N"));
        string[] args={"player","-ca-eos-role",role,"-ca-eos-output",output,"-ca-eos-auth-soak-seconds",seconds};
        if(valid)Assert.That(EOSProofArguments.Parse(args).AuthenticationSoakSeconds,Is.EqualTo(int.Parse(seconds)));
        else Assert.Throws<EOSProofFailure>(()=>EOSProofArguments.Parse(args));
        Assert.That(Directory.Exists(output),Is.False,"Argument checking must not initialize a session or output fixtures");
    }
    [Test] public void DuplicateSoakArgumentIsRejected()
    {
        var output=Path.Combine(Path.GetTempPath(),"CA-auth-args-"+Guid.NewGuid().ToString("N"));
        Assert.Throws<EOSProofFailure>(()=>EOSProofArguments.Parse(new[]{"-ca-eos-role","login","-ca-eos-output",output,"-ca-eos-auth-soak-seconds","3900","-ca-eos-auth-soak-seconds","3900"}));
    }
}
