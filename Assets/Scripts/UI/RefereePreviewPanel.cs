using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RefereePreviewPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const string YellowKitId = "gk6";
    // Light blue, pink, green, orange, black.
    private static readonly string[] AlternativeKitIds = { "gk4", "gk5", "gk2", "gk3", "gk1" };

    [SerializeField] private CanvasGroup visibility;
    [SerializeField] private TMP_Text refereeName;
    [SerializeField] private TMP_Text leniencyValue;
    [SerializeField] private Image leniencyBadge;
    [SerializeField] private RefereePreviewCard previewCard;
    [SerializeField] private MatchManager match;
    [SerializeField] private RoomInspectionCard roomInspection;
    [SerializeField] private DraftManager draft;

    private string displayedSetting;
    private (string home, string away, string homeKeeper, string awayKeeper) displayedKits;
    private Image background;
    private TokenKitInstructionPalette defaultPalette;
    private bool initialized;
    private bool hasReferee;
    private bool hovered;

    private void Awake() => Refresh();
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        // Both managers expose the settings used by the local, online and spectator views.
        var roomSettings = match != null ? match.gameData?.gameSettings : null;
        var draftSettings = draft != null ? draft.currentSettings : null;
        string setting = roomSettings?.referee ?? draftSettings?.referee;
        RefreshPalette(roomSettings != null
            ? (roomSettings.homeKit, roomSettings.awayKit, roomSettings.homeGKKit, roomSettings.awayGKKit)
            : (draftSettings?.homeKit, draftSettings?.awayKit, draftSettings?.homeGKKit, draftSettings?.awayGKKit));

        if (initialized && setting == displayedSetting) return;
        initialized = true;
        displayedSetting = setting;
        hasReferee = TryReadReferee(setting, out string label, out string country, out int leniency);
        visibility.alpha = hasReferee ? 1f : 0f;
        visibility.blocksRaycasts = hasReferee;
        if (!hasReferee)
        {
            if (roomInspection != null) roomInspection.CloseReferee();
            else previewCard.gameObject.SetActive(false);
            return;
        }

        refereeName.text = label;
        leniencyValue.text = leniency.ToString(CultureInfo.InvariantCulture);
        leniencyBadge.color = MatchStatsUI.GetInspectionAttributeColor(leniency);
        previewCard.Display(label, country, leniency);
        if (roomInspection == null) previewCard.gameObject.SetActive(hovered);
    }

    private void RefreshPalette((string home, string away, string homeKeeper, string awayKeeper) kits)
    {
        if (kits == displayedKits) return;
        displayedKits = kits;
        if (background == null)
        {
            background = GetComponent<Image>();
            defaultPalette = new TokenKitInstructionPalette(background.color, refereeName.color);
        }

        var palette = SelectPalette(kits);
        background.color = palette.Primary;
        refereeName.color = palette.Secondary;
    }

    private TokenKitInstructionPalette SelectPalette((string home, string away, string homeKeeper, string awayKeeper) kits)
    {
        var home = TokenKitCatalog.GetPresetByIdOrAlias(kits.home);
        var away = TokenKitCatalog.GetPresetByIdOrAlias(kits.away);
        string homeKeeper = TokenKitCatalog.GetPresetByIdOrAlias(kits.homeKeeper)?.Id;
        string awayKeeper = TokenKitCatalog.GetPresetByIdOrAlias(kits.awayKeeper)?.Id;
        bool IsAvailable(string id) => id != homeKeeper && id != awayKeeper;
        if (IsAvailable(YellowKitId) && !ClashesWithYellow(home) && !ClashesWithYellow(away))
            return defaultPalette;

        TokenKitPreset leastSimilar = null;
        float lowestClash = float.PositiveInfinity;
        foreach (string id in AlternativeKitIds)
        {
            if (!IsAvailable(id)) continue;
            var candidate = TokenKitCatalog.GetPresetByIdOrAlias(id);
            if (candidate == null) continue;
            var homeClash = TokenKitCatalog.GetSimilarityBreakdown(candidate, home);
            var awayClash = TokenKitCatalog.GetSimilarityBreakdown(candidate, away);
            if (!homeClash.IsClash && !awayClash.IsClash)
                return TokenKitCatalog.GetInstructionPalette(candidate.Style);

            float clash = Mathf.Max(PeakSimilarity(homeClash), PeakSimilarity(awayClash));
            if (clash >= lowestClash) continue;
            leastSimilar = candidate;
            lowestClash = clash;
        }

        // Legacy saves can leave no clash-free option. Prefer the least similar unused kit.
        return leastSimilar == null ? defaultPalette : TokenKitCatalog.GetInstructionPalette(leastSimilar.Style);
    }

    private bool ClashesWithYellow(TokenKitPreset kit)
    {
        if (kit == null) return false;
        Color primary = kit.Style.bodyColor;
        if (TokenKitCatalog.GetColorSimilarity(defaultPalette.Primary, primary) > TokenKitCatalog.KitComponentClashThreshold)
            return true;

        // Compare hue as well so saturated gold and yellow kits (such as Brazil) also conflict.
        Color.RGBToHSV(defaultPalette.Primary, out float yellowHue, out _, out _);
        Color.RGBToHSV(primary, out float hue, out float saturation, out float value);
        return saturation >= .2f && value >= .5f && Mathf.Abs(hue - yellowHue) <= .035f;
    }

    private static float PeakSimilarity(TokenKitSimilarityBreakdown similarity) =>
        Mathf.Max(similarity.OverallScore, Mathf.Max(similarity.BodyColorSimilarity, similarity.TopFaceSimilarity));

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (roomInspection == null && hasReferee) previewCard.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) => ClearHover();
    private void OnDisable() => ClearHover();

    private void ClearHover()
    {
        hovered = false;
        if (roomInspection == null && previewCard != null) previewCard.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
            TryToggleRoomPreview();
    }

    public bool TryToggleRoomPreview()
    {
        if (!isActiveAndEnabled || !hovered || !hasReferee || roomInspection == null
            || RoomInputOwnership.ReservePauseEscape || RulesViewer.IsOpen
            || GameInputManager.IsPresentationModalActive) return false;

        roomInspection.ToggleReferee();
        return true;
    }

    public static bool TryReadReferee(string setting, out string label, out string country, out int leniency)
    {
        label = country = string.Empty;
        leniency = 0;
        int separator = setting?.LastIndexOf(" - ", StringComparison.Ordinal) ?? -1;
        if (separator < 0 || !int.TryParse(setting.AsSpan(separator + 3), NumberStyles.None,
                CultureInfo.InvariantCulture, out leniency) || leniency is < 2 or > 5) return false;

        label = setting.Substring(0, separator);
        country = label switch
        {
            "Webster" => "Scotland",
            "Castolo" => "Brazil",
            "Bakker" => "Netherlands",
            "Read" => "England",
            _ => string.Empty
        };
        return country.Length > 0;
    }
}
