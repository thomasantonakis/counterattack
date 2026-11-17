using System;
namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineInstructionObservation
    {
        public const int TextLimit=8192;
        public string text,owner;
        public bool flash;
        public static bool Valid(OnlineInstructionObservation value)=>value==null || !string.IsNullOrWhiteSpace(value.text)
            && value.text.Length<=TextLimit && value.owner is "Home" or "Away" or "Neutral";
        public static OnlineInstructionObservation Capture(GameDebugMonitor monitor,string owner)
        {
            if(monitor==null)return null;var snapshot=monitor.GetCurrentInstructionSnapshotForLog();if(string.IsNullOrWhiteSpace(snapshot?.instructionText))return null;
            var result=new OnlineInstructionObservation{text=snapshot.instructionText,owner=owner is "Home" or "Away"?owner:snapshot.instructionSide,
                flash=snapshot.details!=null && snapshot.details.TryGetValue("shouldFlash",out string flash) && bool.TryParse(flash,out bool enabled) && enabled};
            if(!Valid(result))throw new InvalidOperationException("Online instruction observation exceeds its bounded contract");return result;
        }
    }
}
