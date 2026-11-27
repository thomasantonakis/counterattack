using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RefereePreviewPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private CanvasGroup visibility;
    [SerializeField] private TMP_Text refereeName;
    [SerializeField] private TMP_Text leniencyValue;
    [SerializeField] private Image leniencyBadge;
    [SerializeField] private RefereePreviewCard previewCard;
    [SerializeField] private MatchManager match;
    [SerializeField] private RoomInspectionCard roomInspection;
    [SerializeField] private DraftManager draft;

    private string displayedSetting;
    private bool initialized;
    private bool hasReferee;
    private bool hovered;

    private void Awake() => Refresh();
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        // DraftManager receives the local save or the settings published to online managers and spectators.
        string setting = match != null ? match.gameData?.gameSettings?.referee : draft?.currentSettings?.referee;
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
