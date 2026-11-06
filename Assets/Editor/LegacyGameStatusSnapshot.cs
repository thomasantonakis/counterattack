using System.Collections.Generic;

public class GameStatusSnapshot
{
    public bool gbmAvailable;
    public bool gbmisActivated;
    public bool gbmIsAwaitingTargetSelection;
    public bool gbmIsWaitingForDiceRoll;
    public HexCell gbmCurrentTargetHex;
    public bool ftpAvailable;
    public bool ftpIsActivated;
    public bool ftpIsAwaitingTargetSelection;
    public bool ftpIsWaitingforAttackerSelection;
    public bool ftpIsWaitingforDefenderSelection;
    public bool ftpIsWaitingforDiceRoll;
    public HexCell ftpCurrentTargetHex;
    public bool hpmAvailable;
    public bool hpmIsActivated;
    public bool hpmIsAwaitingTargetSelection;
    public bool hpmIsWaitingforAttackerSelection;
    public bool hpmIsWaitingforDefenderSelection;
    public HexCell hpmCurrentTargetHex;
    public bool hpmIsWaitingForAccuracyRoll;
    public bool hpmIsWaitingForDirectionRoll;
    public bool hpmIsWaitingForDistanceRoll;
    public bool lbmAvailable;
    public bool lbmIsActivated;
    public bool lbmIsAwaitingTargetSelection;
    public bool lbmIsWaitingForAccuracyRoll;
    public bool lbmIsWaitingForDirectionRoll;
    public bool lbmIsWaitingForDistanceRoll;
    public bool lbmIsWaitingForDefLBMove;
    public bool looseIsActivated;
    public bool looseIsWaitingForDirectionRoll;
    public bool looseIsWaitingForDistanceRoll;
    public bool looseIsWaitingForInterceptionRoll;
    public HexCell lbmCurrentTargetHex;

    public GameStatusSnapshot(
        GroundBallManager gbm, FirstTimePassManager ftp, HighPassManager hpm, LongBallManager lbm, MovementPhaseManager mpm
        , FinalThirdManager ftm, HeaderManager hdm, LooseBallManager loose, OutOfBoundsManager obob, FreeKickManager fkm
        , ShotManager shot, GoalKeeperManager gkm, GoalFlowManager gfm, KickoffManager kom, HexGrid hg, GameInputManager gim
    )
    {
        gbmAvailable = gbm.isAvailable;
        gbmisActivated = gbm.isActivated;
        gbmIsAwaitingTargetSelection = gbm.isAwaitingTargetSelection; 
        gbmIsWaitingForDiceRoll = gbm.isWaitingForDiceRoll;
        gbmCurrentTargetHex = gbm.currentTargetHex;
        ftpAvailable = ftp.isAvailable;
        ftpIsActivated = ftp.isActivated;
        ftpIsAwaitingTargetSelection = ftp.isAwaitingTargetSelection;
        ftpIsWaitingforAttackerSelection = ftp.isWaitingForAttackerSelection;
        ftpIsWaitingforDefenderSelection = ftp.isWaitingForDefenderSelection;
        ftpIsWaitingforDiceRoll = ftp.isWaitingForDiceRoll;
        ftpCurrentTargetHex = ftp.currentTargetHex;
        hpmAvailable = hpm.isAvailable;
        hpmIsActivated = hpm.isActivated;
        hpmIsAwaitingTargetSelection = hpm.isWaitingForConfirmation;
        hpmIsWaitingforAttackerSelection = hpm.isWaitingForAttackerSelection;
        hpmIsWaitingforDefenderSelection = hpm.isWaitingForDefenderSelection;
        hpmCurrentTargetHex = hpm.currentTargetHex;
        hpmIsWaitingForAccuracyRoll = hpm.isWaitingForAccuracyRoll;
        hpmIsWaitingForDirectionRoll = hpm.isWaitingForDirectionRoll;
        hpmIsWaitingForDistanceRoll = hpm.isWaitingForDistanceRoll;
        lbmAvailable = lbm.isAvailable;
        lbmIsActivated = lbm.isActivated;
        lbmIsWaitingForAccuracyRoll = lbm.isWaitingForAccuracyRoll;
        lbmIsWaitingForDirectionRoll = lbm.isWaitingForDirectionRoll;
        lbmIsWaitingForDistanceRoll = lbm.isWaitingForDistanceRoll;
        lbmIsAwaitingTargetSelection = lbm.isAwaitingTargetSelection;
        lbmCurrentTargetHex = lbm.currentTargetHex;
        lbmIsWaitingForDefLBMove = lbm.isWaitingForDefLBMove;
        looseIsActivated = loose.isActivated;
        looseIsWaitingForDirectionRoll = loose.isWaitingForDirectionRoll;
        looseIsWaitingForDistanceRoll = loose.isWaitingForDistanceRoll;
        looseIsWaitingForInterceptionRoll = loose.isWaitingForInterceptionRoll;
    }

    public bool IsEqualTo(GameStatusSnapshot other, out string reason, HashSet<string> excludeFields = null)
    {
        List<string> mismatches = new List<string>();

        if (excludeFields?.Contains("gbmAvailable") != true && gbmAvailable != other.gbmAvailable)
        {
            mismatches.Add($"GroundBallManager.isAvailable mismatch: {gbmAvailable} vs {other.gbmAvailable}");
        }

        if (excludeFields?.Contains("gbmisActivated") != true && gbmisActivated != other.gbmisActivated)
        {
            mismatches.Add($"GroundBallManager.isActivated mismatch: {gbmisActivated} vs {other.gbmisActivated}");
        }

        if (excludeFields?.Contains("gbmIsAwaitingTargetSelection") != true && gbmIsAwaitingTargetSelection != other.gbmIsAwaitingTargetSelection)
        {
            mismatches.Add($"GroundBallManager.isAwaitingTargetSelection mismatch: {gbmIsAwaitingTargetSelection} vs {other.gbmIsAwaitingTargetSelection}");
        }

        if (excludeFields?.Contains("gbmIsWaitingForDiceRoll") != true && gbmIsWaitingForDiceRoll != other.gbmIsWaitingForDiceRoll)
        {
            mismatches.Add($"GroundBallManager.isWaitingForDiceRoll mismatch: {gbmIsWaitingForDiceRoll} vs {other.gbmIsWaitingForDiceRoll}");
        }

        if (excludeFields?.Contains("gbmCurrentTargetHex") != true && gbmCurrentTargetHex != other.gbmCurrentTargetHex)
        {
            mismatches.Add($"GroundBallManager.currentTargetHex mismatch: {gbmCurrentTargetHex?.name} vs {other.gbmCurrentTargetHex?.name}");
        }

        if (excludeFields?.Contains("ftpAvailable") != true && ftpAvailable != other.ftpAvailable)
        {
            mismatches.Add($"FirstTimePassManager.isAvailable mismatch: {ftpAvailable} vs {other.ftpAvailable}");
        }

        if (excludeFields?.Contains("ftpIsActivated") != true && ftpIsActivated != other.ftpIsActivated)
        {
            mismatches.Add($"FirstTimePassManager.isActivated mismatch: {ftpIsActivated} vs {other.ftpIsActivated}");
        }

        if (excludeFields?.Contains("ftpIsAwaitingTargetSelection") != true && ftpIsAwaitingTargetSelection != other.ftpIsAwaitingTargetSelection)
        {
            mismatches.Add($"FirstTimePassManager.isAwaitingTargetSelection mismatch: {ftpIsAwaitingTargetSelection} vs {other.ftpIsAwaitingTargetSelection}");
        }

        if (excludeFields?.Contains("ftpIsWaitingforAttackerSelection") != true && ftpIsWaitingforAttackerSelection != other.ftpIsWaitingforAttackerSelection)
        {
            mismatches.Add($"FirstTimePassManager.isWaitingforAttackerSelection mismatch: {ftpIsWaitingforAttackerSelection} vs {other.ftpIsWaitingforAttackerSelection}");
        }

        if (excludeFields?.Contains("ftpIsWaitingforDefenderSelection") != true && ftpIsWaitingforDefenderSelection != other.ftpIsWaitingforDefenderSelection)
        {
            mismatches.Add($"FirstTimePassManager.isWaitingforDefenderSelection mismatch: {ftpIsWaitingforDefenderSelection} vs {other.ftpIsWaitingforDefenderSelection}");
        }

        if (excludeFields?.Contains("ftpIsWaitingforDiceRoll") != true && ftpIsWaitingforDiceRoll != other.ftpIsWaitingforDiceRoll)
        {
            mismatches.Add($"FirstTimePassManager.isWaitingforDiceRoll mismatch: {ftpIsWaitingforDiceRoll} vs {other.ftpIsWaitingforDiceRoll}");
        }

        if (excludeFields?.Contains("ftpCurrentTargetHex") != true && ftpCurrentTargetHex != other.ftpCurrentTargetHex)
        {
            mismatches.Add($"FirstTimePassManager.currentTargetHex mismatch: {ftpCurrentTargetHex?.name} vs {other.ftpCurrentTargetHex?.name}");
        }
        
        if (excludeFields?.Contains("hpmAvailable") != true && hpmAvailable != other.hpmAvailable)
        {
            mismatches.Add($"HighPassManager.isAvailable mismatch: {hpmAvailable} vs {other.hpmAvailable}");
        }
        
        if (excludeFields?.Contains("hpmIsActivated") != true && hpmIsActivated != other.hpmIsActivated)
        {
            mismatches.Add($"HighPassManager.isActivated mismatch: {hpmIsActivated} vs {other.hpmIsActivated}");
        }
        
        if (excludeFields?.Contains("hpmIsAwaitingTargetSelection") != true && hpmIsAwaitingTargetSelection != other.hpmIsAwaitingTargetSelection)
        {
            mismatches.Add($"HighPassManager.IsAwaitingTargetSelection mismatch: {hpmIsAwaitingTargetSelection} vs {other.hpmIsAwaitingTargetSelection}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingforAttackerSelection") != true && hpmIsWaitingforAttackerSelection != other.hpmIsWaitingforAttackerSelection)
        {
            mismatches.Add($"HighPassManager.IsWaitingforAttackerSelection mismatch: {hpmIsWaitingforAttackerSelection} vs {other.hpmIsWaitingforAttackerSelection}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingforDefenderSelection") != true && hpmIsWaitingforDefenderSelection != other.hpmIsWaitingforDefenderSelection)
        {
            mismatches.Add($"HighPassManager.IsWaitingforDefenderSelection mismatch: {hpmIsWaitingforDefenderSelection} vs {other.hpmIsWaitingforDefenderSelection}");
        }

        if (excludeFields?.Contains("hpmCurrentTargetHex") != true && hpmCurrentTargetHex != other.hpmCurrentTargetHex)
        {
            mismatches.Add($"HighPassManager.currentTargetHex mismatch: {hpmCurrentTargetHex?.name} vs {other.hpmCurrentTargetHex?.name}");
        }

        if (excludeFields?.Contains("hpmIsWaitingForAccuracyRoll") != true && hpmIsWaitingForAccuracyRoll != other.hpmIsWaitingForAccuracyRoll)
        {
            mismatches.Add($"HighPassManager.isWaitingForAccuracyRoll mismatch: {hpmIsWaitingForAccuracyRoll} vs {other.hpmIsWaitingForAccuracyRoll}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingForDirectionRoll") != true && hpmIsWaitingForDirectionRoll != other.hpmIsWaitingForDirectionRoll)
        {
            mismatches.Add($"HighPassManager.IsWaitingForDirectionRoll mismatch: {hpmIsWaitingForDirectionRoll} vs {other.hpmIsWaitingForDirectionRoll}");
        }
        
        if (excludeFields?.Contains("hpmIsWaitingForDistanceRoll") != true && hpmIsWaitingForDistanceRoll != other.hpmIsWaitingForDistanceRoll)
        {
            mismatches.Add($"HighPassManager.IsWaitingForDistanceRoll mismatch: {hpmIsWaitingForDistanceRoll} vs {other.hpmIsWaitingForDistanceRoll}");
        }
        
        if (excludeFields?.Contains("lbmAvailable") != true && lbmAvailable != other.lbmAvailable)
        {
            mismatches.Add($"LongBallManager.isAvailable mismatch: {lbmAvailable} vs {other.lbmAvailable}");
        }
        
        if (excludeFields?.Contains("lbmIsActivated") != true && lbmIsActivated != other.lbmIsActivated)
        {
            mismatches.Add($"LongBallManager.isActivated mismatch: {lbmIsActivated} vs {other.lbmIsActivated}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForAccuracyRoll") != true && lbmIsWaitingForAccuracyRoll != other.lbmIsWaitingForAccuracyRoll)
        {
            mismatches.Add($"LongBallManager.isWaitingForAccuracyRoll mismatch: {lbmIsWaitingForAccuracyRoll} vs {other.lbmIsWaitingForAccuracyRoll}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForDirectionRoll") != true && lbmIsWaitingForDirectionRoll != other.lbmIsWaitingForDirectionRoll)
        {
            mismatches.Add($"LongBallManager.IsWaitingForDirectionRoll mismatch: {lbmIsWaitingForDirectionRoll} vs {other.lbmIsWaitingForDirectionRoll}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForDistanceRoll") != true && lbmIsWaitingForDistanceRoll != other.lbmIsWaitingForDistanceRoll)
        {
            mismatches.Add($"LongBallManager.IsWaitingForDistanceRoll mismatch: {lbmIsWaitingForDistanceRoll} vs {other.lbmIsWaitingForDistanceRoll}");
        }
        
        if (excludeFields?.Contains("lbmIsAwaitingTargetSelection") != true && lbmIsAwaitingTargetSelection != other.lbmIsAwaitingTargetSelection)
        {
            mismatches.Add($"LongBallManager.isAwaitingTargetSelection mismatch: {lbmIsAwaitingTargetSelection} vs {other.lbmIsAwaitingTargetSelection}");
        }
        
        if (excludeFields?.Contains("lbmIsWaitingForDefLBMove") != true && lbmIsWaitingForDefLBMove != other.lbmIsWaitingForDefLBMove)
        {
            mismatches.Add($"LongBallManager.IsWaitingForDefLBMove mismatch: {lbmIsWaitingForDefLBMove} vs {other.lbmIsWaitingForDefLBMove}");
        }
        
        if (excludeFields?.Contains("lbmCurrentTargetHex") != true && lbmCurrentTargetHex != other.lbmCurrentTargetHex)
        {
            mismatches.Add($"LongBallManager.currentTargetHex mismatch: {lbmCurrentTargetHex?.name} vs {other.lbmCurrentTargetHex?.name}");
        }
        if (excludeFields?.Contains("looseIsActivated") != true && looseIsActivated != other.looseIsActivated)
        {
            mismatches.Add($"LooseBallManager.IsActivated mismatch: {looseIsActivated} vs {other.looseIsActivated}");
        }
        if (excludeFields?.Contains("looseIsWaitingForDirectionRoll") != true && looseIsWaitingForDirectionRoll != other.looseIsWaitingForDirectionRoll)
        {
            mismatches.Add($"LooseBallManager.IsWaitingForDirectionRoll mismatch: {looseIsWaitingForDirectionRoll} vs {other.looseIsWaitingForDirectionRoll}");
        }
        if (excludeFields?.Contains("looseIsWaitingForDistanceRoll") != true && looseIsWaitingForDistanceRoll != other.looseIsWaitingForDistanceRoll)
        {
            mismatches.Add($"LooseBallManager.IsWaitingForDistanceRoll mismatch: {looseIsWaitingForDistanceRoll} vs {other.looseIsWaitingForDistanceRoll}");
        }
        if (excludeFields?.Contains("looseIsWaitingForInterceptionRoll") != true && looseIsWaitingForInterceptionRoll != other.looseIsWaitingForInterceptionRoll)
        {
            mismatches.Add($"LooseBallManager.IsWaitingForInterceptionRoll mismatch: {looseIsWaitingForInterceptionRoll} vs {other.looseIsWaitingForInterceptionRoll}");
        }
        
        if (mismatches.Count > 0)
        {
            reason = string.Join("\n", mismatches);
            return false;
        }

        reason = null;
        return true;
    }
}

