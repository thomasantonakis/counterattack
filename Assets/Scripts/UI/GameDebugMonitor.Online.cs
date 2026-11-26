using CounterAttack.Online;
using UnityEngine.UI;
public partial class GameDebugMonitor
{
    private InstructionSide onlineInstructionSide;
    private bool onlineInstructionFlash;
    public void DisplayOnlineInstruction(OnlineInstructionObservation observation,bool blocked)
    {
        if(instructionText==null)return;
        instructionText.text=blocked?string.Empty:observation?.text??string.Empty;
        onlineInstructionSide=blocked?InstructionSide.Neutral:observation?.owner=="Home"?InstructionSide.Home:observation?.owner=="Away"?InstructionSide.Away:InstructionSide.Neutral;
        onlineInstructionFlash=!blocked && observation?.flash==true;
        if(instructionPanelImage==null)instructionPanelImage=instructionText.GetComponentInParent<Image>();
        RefreshOnlineInstructionPalette();
    }
    public void RefreshOnlineInstructionPalette()=>ApplyInstructionPalette(onlineInstructionSide,onlineInstructionFlash);
}
