using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CounterAttack.Online;

[System.Serializable]
public sealed class OnlineMovementPanelFrame
{
    public int section;
    public bool attackingForfeited,defendingForfeited,nextBlocked;
    public string selected;
    public string[] attacking,defending,twoForTwo;

    public static bool Valid(OnlineMovementPanelFrame value)=>value==null || value.section is >=1 and <=3
        && ValidRow(value.attacking) && ValidRow(value.defending) && ValidRow(value.twoForTwo)
        && (value.selected==null || OnlineProtocolIds.IsValid(value.selected));
    private static bool ValidRow(string[] row)=>row!=null && row.Length<=36 && row.All(OnlineProtocolIds.IsValid);
}

public class MovementPhaseMovedTokensPanel : MonoBehaviour
{
    [SerializeField] private MovementPhaseManager movementPhaseManager;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private MovementPhaseMovedTokenSlotView[] attackingMovementSlots;
    [SerializeField] private MovementPhaseMovedTokenSlotView[] defensiveMovementSlots;
    [SerializeField] private MovementPhaseMovedTokenSlotView[] attackingTwoForTwoSlots;

    private int lastRenderedSignature = int.MinValue;
    private bool lastVisible;

    private void Awake()
    {
        movementPhaseManager ??= FindAnyObjectByType<MovementPhaseManager>();
        canvasGroup ??= GetComponent<CanvasGroup>();
        Refresh(force: true);
    }

    private void Update()
    {
        if(OnlineRoomSession.IsGuest)return;
        Refresh(force: false);
    }

    public static OnlineMovementPanelFrame CaptureOnline(MovementPhaseManager movement)
    {
        if(movement==null || !movement.isActivated || movement.finalThirdManager?.isActivated==true)return null;
        string[] Row(MovementPhaseMovedTokenSection section)=>movement.MovedTokenEntries
            .Where(entry=>entry.section==section && entry.token!=null)
            .Select(entry=>OnlineRollPublication.ActorId(entry.token)).ToArray();
        return new OnlineMovementPanelFrame
        {
            section=movement.isMovementPhaseAttack?1:movement.isMovementPhaseDef?2:3,
            attackingForfeited=movement.AttackingMovementForfeited,defendingForfeited=movement.DefensiveMovementForfeited,
            selected=movement.selectedToken==null?null:OnlineRollPublication.ActorId(movement.selectedToken),
            nextBlocked=movement.shotManager?.isActivated==true || movement.shotManager?.isWaitingForSnapshotDecisionFromLoose==true
                || movement.selectedToken!=null && movement.MovedTokenEntries.Any(entry=>entry.token==movement.selectedToken),
            attacking=Row(MovementPhaseMovedTokenSection.AttMP),defending=Row(MovementPhaseMovedTokenSection.DefMP),
            twoForTwo=Row(MovementPhaseMovedTokenSection.Att2f2)
        };
    }

    public void ApplyOnline(OnlineMovementPanelFrame frame,PlayerToken[] roster,bool held = false)
    {
        SetVisible(frame!=null);
        if(frame==null){ClearAllSlots();return;}
        PlayerToken Resolve(string id)=>id==null?null:roster.FirstOrDefault(token=>OnlineRollPublication.ActorId(token)==id);
        var settings=MatchManager.Instance?.gameData?.gameSettings;
        bool attackHome=MatchManager.Instance?.teamInAttack==MatchManager.TeamInAttack.Home;
        void Draw(MovementPhaseMovedTokenSlotView[] slots,string[] moved,int section,bool forfeited)
        {
            string kit=(section==2?!attackHome:attackHome)?settings?.homeKit:settings?.awayKit;
            var palette=TokenKitCatalog.ResolveInstructionPalette(kit,Color.white,Color.black);
            for(int i=0;i<slots.Length;i++)
            {
                var slot=slots[i];
                if(slot==null)continue;
                slot.SetReplicaHold(held);
                var token=i<moved.Length?Resolve(moved[i]):null;
                if(token!=null)slot.Render(ResolveStyle(token),token.jerseyNumber);
                else if(forfeited)slot.RenderForfeit(palette.Primary,palette.Secondary);
                else if(frame.section==section && !frame.nextBlocked && i==moved.Length)
                {
                    var selection=Resolve(frame.selected);
                    slot.RenderActive(palette.Primary,ResolveStyle(selection),selection?.jerseyNumber??0,TokenKitCatalog.ResolveStyle(kit));
                }
                else slot.Clear();
            }
        }
        Draw(attackingMovementSlots,frame.attacking,1,frame.attackingForfeited);
        Draw(defensiveMovementSlots,frame.defending,2,frame.defendingForfeited);
        Draw(attackingTwoForTwoSlots,frame.twoForTwo,3,false);
    }

    private void Refresh(bool force)
    {
        bool shouldShow = movementPhaseManager != null
            && movementPhaseManager.isActivated
            && (movementPhaseManager.finalThirdManager == null || !movementPhaseManager.finalThirdManager.isActivated);
        int signature = shouldShow ? BuildSignature(movementPhaseManager.MovedTokenEntries) : 0;
        if (shouldShow)
        {
            unchecked
            {
                signature = signature * 31 + (movementPhaseManager.isMovementPhaseAttack ? 1 : movementPhaseManager.isMovementPhaseDef ? 2 : 3);
                signature = signature * 31 + GetTokenSignature(movementPhaseManager.selectedToken);
                signature = signature * 31 + (IsNextMoverBlocked ? 1 : 0);
                signature = signature * 31 + (movementPhaseManager.AttackingMovementForfeited ? 1 : 0);
                signature = signature * 31 + (movementPhaseManager.DefensiveMovementForfeited ? 1 : 0);
            }
        }
        if (!force && shouldShow == lastVisible && signature == lastRenderedSignature)
        {
            return;
        }

        lastVisible = shouldShow;
        lastRenderedSignature = signature;
        SetVisible(shouldShow);

        if (!shouldShow || movementPhaseManager == null)
        {
            ClearAllSlots();
            return;
        }

        FillSlots(attackingMovementSlots, MovementPhaseMovedTokenSection.AttMP);
        FillSlots(defensiveMovementSlots, MovementPhaseMovedTokenSection.DefMP);
        FillSlots(attackingTwoForTwoSlots, MovementPhaseMovedTokenSection.Att2f2);
    }

    private void FillSlots(MovementPhaseMovedTokenSlotView[] slots, MovementPhaseMovedTokenSection section)
    {
        if (slots == null)
        {
            return;
        }

        int slotIndex = 0;
        IReadOnlyList<MovementPhaseMovedTokenEntry> entries = movementPhaseManager.MovedTokenEntries;
        for (int i = 0; i < entries.Count && slotIndex < slots.Length; i++)
        {
            MovementPhaseMovedTokenEntry entry = entries[i];
            if (entry.section != section || entry.token == null)
            {
                continue;
            }

            MovementPhaseMovedTokenSlotView slot = slots[slotIndex++];
            if (slot != null)
            {
                slot.Render(ResolveStyle(entry.token), entry.token.jerseyNumber);
            }
        }

        bool forfeited = section == MovementPhaseMovedTokenSection.AttMP ? movementPhaseManager.AttackingMovementForfeited
            : section == MovementPhaseMovedTokenSection.DefMP && movementPhaseManager.DefensiveMovementForfeited;
        bool active = section == MovementPhaseMovedTokenSection.AttMP ? movementPhaseManager.isMovementPhaseAttack
            : section == MovementPhaseMovedTokenSection.DefMP ? movementPhaseManager.isMovementPhaseDef : movementPhaseManager.isMovementPhase2f2;
        var settings = MatchManager.Instance?.gameData?.gameSettings;
        bool home = MatchManager.Instance?.teamInAttack == MatchManager.TeamInAttack.Home;
        if (section == MovementPhaseMovedTokenSection.DefMP) home = !home;
        TokenKitInstructionPalette palette = TokenKitCatalog.ResolveInstructionPalette(settings == null ? null : home ? settings.homeKit : settings.awayKit, Color.white, Color.black);
        int next = slotIndex;
        for (; slotIndex < slots.Length; slotIndex++)
        {
            if (forfeited) slots[slotIndex]?.RenderForfeit(palette.Primary, palette.Secondary);
            else if (active && !IsNextMoverBlocked && slotIndex == next)
            {
                PlayerToken selected = movementPhaseManager.selectedToken;
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].section == section && entries[i].token == selected) { selected = null; break; }
                slots[slotIndex]?.RenderActive(palette.Primary, selected != null ? ResolveStyle(selected) : null, selected != null ? selected.jerseyNumber : 0,
                    TokenKitCatalog.ResolveStyle(settings == null ? null : home ? settings.homeKit : settings.awayKit));
            }
            else slots[slotIndex]?.Clear();
        }
    }

    // A declared snapshot completes the dribbler, but its shot still owns input.
    private bool IsShotResolving => movementPhaseManager?.shotManager?.isActivated == true
        || movementPhaseManager?.shotManager?.isWaitingForSnapshotDecisionFromLoose == true;

    // Registering a moved token does not finish its tackle/roll/reposition input.
    // AdvanceMovementPhase releases selectedToken when the next mover may start.
    private bool IsNextMoverBlocked => IsShotResolving || movementPhaseManager?.selectedToken != null
        && movementPhaseManager.MovedTokenEntries.Any(e => e.token == movementPhaseManager.selectedToken);

    private TokenStyleDefinition ResolveStyle(PlayerToken token)
    {
        MatchManager.GameSettings settings = MatchManager.Instance?.gameData?.gameSettings;
        if (token == null || settings == null)
        {
            return null;
        }

        string outfieldKit = token.isHomeTeam ? settings.homeKit : settings.awayKit;
        string goalkeeperKit = token.isHomeTeam ? settings.homeGKKit : settings.awayGKKit;
        string kit = token.IsGoalKeeper && !string.IsNullOrWhiteSpace(goalkeeperKit)
            ? goalkeeperKit
            : outfieldKit;
        return TokenKitCatalog.ResolveStyle(kit);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        gameObject.SetActive(visible);
    }

    private void ClearAllSlots()
    {
        ClearSlots(attackingMovementSlots);
        ClearSlots(defensiveMovementSlots);
        ClearSlots(attackingTwoForTwoSlots);
    }

    private static void ClearSlots(MovementPhaseMovedTokenSlotView[] slots)
    {
        if (slots == null)
        {
            return;
        }

        foreach (MovementPhaseMovedTokenSlotView slot in slots)
        {
            slot?.Clear();
        }
    }

    private static int BuildSignature(IReadOnlyList<MovementPhaseMovedTokenEntry> entries)
    {
        unchecked
        {
            int signature = 17;
            if (entries == null)
            {
                return signature;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                MovementPhaseMovedTokenEntry entry = entries[i];
                signature = (signature * 31) + (int)entry.section;
                signature = (signature * 31) + GetTokenSignature(entry.token);
                signature = (signature * 31) + (entry.token != null ? entry.token.jerseyNumber : 0);
            }

            return signature;
        }
    }

    private static int GetTokenSignature(PlayerToken token)
    {
        if (token == null)
        {
            return 0;
        }

        unchecked
        {
            int signature = 17;
            signature = (signature * 31) + token.jerseyNumber;
            signature = (signature * 31) + (token.isHomeTeam ? 1 : 0);
            signature = (signature * 31) + (token.playerName != null ? token.playerName.GetHashCode() : 0);
            signature = (signature * 31) + (token.name != null ? token.name.GetHashCode() : 0);
            return signature;
        }
    }
}
