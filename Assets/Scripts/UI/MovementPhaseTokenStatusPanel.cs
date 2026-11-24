using System;
using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;
using UnityEngine;

[Serializable]
public sealed class OnlineMovementTokenStatusFrame
{
    public string[] stunned, jumped, stunnedNext;

    public static bool Valid(OnlineMovementTokenStatusFrame frame, OnlineGroundPassTokenState[] tokens)
    {
        if (frame == null) return true;
        var roster = tokens.Select(token => token.id).ToHashSet(StringComparer.Ordinal);
        return ValidRow(frame.stunned) && ValidRow(frame.jumped) && ValidRow(frame.stunnedNext);

        bool ValidRow(string[] row) => row != null && row.Length <= roster.Count
            && row.Distinct(StringComparer.Ordinal).Count() == row.Length && row.All(roster.Contains);
    }
}

public class MovementPhaseTokenStatusPanel : MonoBehaviour
{
    [SerializeField] private MovementPhaseManager movementPhaseManager;
    [SerializeField] private HeaderManager headerManager;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform stunnedSection;
    [SerializeField] private RectTransform jumpedSection;
    [SerializeField] private RectTransform stunnedNextSection;
    [SerializeField] private MovementPhaseMovedTokenSlotView[] stunnedSlots;
    [SerializeField] private MovementPhaseMovedTokenSlotView[] jumpedSlots;
    [SerializeField] private MovementPhaseMovedTokenSlotView[] stunnedNextSlots;

    private int lastRenderedSignature = int.MinValue;
    private bool lastVisible;

    private void Awake()
    {
        movementPhaseManager ??= FindAnyObjectByType<MovementPhaseManager>();
        headerManager ??= FindAnyObjectByType<HeaderManager>();
        canvasGroup ??= GetComponent<CanvasGroup>();
        Refresh(force: true);
    }

    private void Update()
    {
        if (!OnlineRoomSession.IsGuest) Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        IReadOnlyList<PlayerToken> stunnedTokens = movementPhaseManager != null
            ? movementPhaseManager.stunnedTokens
            : null;
        IReadOnlyList<PlayerToken> jumpedTokens = BuildJumpedTokens(headerManager);
        IReadOnlyList<PlayerToken> stunnedNextTokens = movementPhaseManager != null
            ? movementPhaseManager.stunnedforNext
            : null;

        RenderStatus(stunnedTokens, jumpedTokens, stunnedNextTokens, force);
    }

    public static OnlineMovementTokenStatusFrame CaptureOnline(MovementPhaseManager movement, HeaderManager header)
    {
        string[] Ids(IEnumerable<PlayerToken> tokens) => tokens?.Where(token => token != null)
            .Select(OnlineRollPublication.ActorId).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
        var frame = new OnlineMovementTokenStatusFrame
        {
            stunned = Ids(movement?.stunnedTokens),
            jumped = Ids(BuildJumpedTokens(header)),
            stunnedNext = Ids(movement?.stunnedforNext)
        };
        return frame.stunned.Length + frame.jumped.Length + frame.stunnedNext.Length > 0 ? frame : null;
    }

    public void ApplyOnline(OnlineMovementTokenStatusFrame frame, PlayerToken[] roster)
    {
        PlayerToken[] Resolve(string[] ids) => ids?.Select(id => roster.FirstOrDefault(token =>
            token != null && OnlineRollPublication.ActorId(token) == id)).ToArray();
        RenderStatus(Resolve(frame?.stunned), Resolve(frame?.jumped), Resolve(frame?.stunnedNext), force: false);
    }

    private void RenderStatus(IReadOnlyList<PlayerToken> stunnedTokens, IReadOnlyList<PlayerToken> jumpedTokens,
        IReadOnlyList<PlayerToken> stunnedNextTokens, bool force)
    {
        bool hasStunned = HasTokens(stunnedTokens);
        bool hasJumped = HasTokens(jumpedTokens);
        bool hasStunnedNext = HasTokens(stunnedNextTokens);
        bool shouldShow = hasStunned || hasJumped || hasStunnedNext;
        int signature = shouldShow
            ? BuildSignature(stunnedTokens, jumpedTokens, stunnedNextTokens)
            : 0;

        if (!force && shouldShow == lastVisible && signature == lastRenderedSignature)
        {
            return;
        }

        lastVisible = shouldShow;
        lastRenderedSignature = signature;
        SetVisible(shouldShow);

        SetSectionVisible(stunnedSection, hasStunned);
        SetSectionVisible(jumpedSection, hasJumped);
        SetSectionVisible(stunnedNextSection, hasStunnedNext);

        SetCanvasGroupSize(hasStunned, hasJumped, hasStunnedNext);

        if (!shouldShow)
        {
            ClearAllSlots();
            return;
        }

        FillSlots(stunnedSlots, stunnedTokens);
        FillSlots(jumpedSlots, jumpedTokens);
        FillSlots(stunnedNextSlots, stunnedNextTokens);
    }

    private void SetCanvasGroupSize(bool hasStunned, bool hasJumped, bool hasStunnedNext)
    {
        if (canvasGroup == null)
        {
            return;
        }
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, (hasStunned ? stunnedSection.rect.height : 0f) + (hasJumped ? jumpedSection.rect.height : 0f) + (hasStunnedNext ? stunnedNextSection.rect.height : 0f));
    }

    private static IReadOnlyList<PlayerToken> BuildJumpedTokens(HeaderManager headerManager)
    {
        List<PlayerToken> tokens = new();
        if (headerManager == null)
        {
            return tokens;
        }

        AddUniqueTokens(tokens, headerManager.attackerWillJump);
        AddUniqueTokens(tokens, headerManager.defenderWillJump);
        return tokens;
    }

    private static void AddUniqueTokens(List<PlayerToken> target, IReadOnlyList<PlayerToken> source)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            PlayerToken token = source[i];
            if (token != null && !target.Contains(token))
            {
                target.Add(token);
            }
        }
    }

    private void FillSlots(MovementPhaseMovedTokenSlotView[] slots, IReadOnlyList<PlayerToken> tokens)
    {
        if (slots == null)
        {
            return;
        }

        int slotIndex = 0;
        if (tokens != null)
        {
            for (int i = 0; i < tokens.Count && slotIndex < slots.Length; i++)
            {
                PlayerToken token = tokens[i];
                if (token == null)
                {
                    continue;
                }

                MovementPhaseMovedTokenSlotView slot = slots[slotIndex++];
                if (slot != null)
                {
                    slot.Render(ResolveStyle(token), token.jerseyNumber);
                }
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

    private static void SetSectionVisible(RectTransform section, bool visible)
    {
        if (section != null && section.gameObject.activeSelf != visible)
        {
            section.gameObject.SetActive(visible);
        }
    }

    private void ClearAllSlots()
    {
        ClearSlots(stunnedSlots);
        ClearSlots(jumpedSlots);
        ClearSlots(stunnedNextSlots);
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

    private static bool HasTokens(IReadOnlyList<PlayerToken> tokens)
    {
        if (tokens == null)
        {
            return false;
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static int BuildSignature(params IReadOnlyList<PlayerToken>[] tokenLists)
    {
        unchecked
        {
            int signature = 17;
            if (tokenLists == null)
            {
                return signature;
            }

            for (int listIndex = 0; listIndex < tokenLists.Length; listIndex++)
            {
                IReadOnlyList<PlayerToken> tokens = tokenLists[listIndex];
                signature = (signature * 31) + listIndex;
                if (tokens == null)
                {
                    continue;
                }

                for (int i = 0; i < tokens.Count; i++)
                {
                    PlayerToken token = tokens[i];
                    signature = (signature * 31) + GetTokenSignature(token);
                    signature = (signature * 31) + (token != null ? token.jerseyNumber : 0);
                }
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
