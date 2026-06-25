using System.Collections.Generic;
using UnityEngine;

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
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        bool shouldShow = movementPhaseManager != null && movementPhaseManager.isActivated;
        int signature = shouldShow ? BuildSignature(movementPhaseManager.MovedTokenEntries) : 0;
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

        for (; slotIndex < slots.Length; slotIndex++)
        {
            slots[slotIndex]?.Clear();
        }
    }

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
