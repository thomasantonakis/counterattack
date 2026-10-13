using System;
using System.Collections.Generic;
using UnityEngine;

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
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        IReadOnlyList<PlayerToken> stunnedTokens = movementPhaseManager != null
            ? movementPhaseManager.stunnedTokens
            : null;
        IReadOnlyList<PlayerToken> jumpedTokens = BuildJumpedTokens();
        IReadOnlyList<PlayerToken> stunnedNextTokens = movementPhaseManager != null
            ? movementPhaseManager.stunnedforNext
            : null;

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

    private IReadOnlyList<PlayerToken> BuildJumpedTokens()
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
