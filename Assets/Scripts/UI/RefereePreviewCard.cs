using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RefereePreviewCard : MonoBehaviour
{
    [SerializeField] private TMP_Text refereeName;
    [SerializeField] private TMP_Text nationality;
    [SerializeField] private Image flag;
    [SerializeField] private TMP_Text leniencyValue;
    [SerializeField] private Image leniencyBadge;

    public void Display(string label, string country, int leniency)
    {
        refereeName.text = label;
        nationality.text = country;
        flag.sprite = FlagSpriteProvider.GetFlagSprite(country);
        flag.enabled = flag.sprite != null;
        leniencyValue.text = leniency.ToString(CultureInfo.InvariantCulture);
        leniencyBadge.color = MatchStatsUI.GetInspectionAttributeColor(leniency);
    }

    private void OnEnable() => FitToCardArea();
    private void LateUpdate() => FitToCardArea();

    private void FitToCardArea()
    {
        // Both Room inspection and Draft author a card area spanning 60% of the canvas height.
        var card = (RectTransform)transform;
        var area = (RectTransform)card.parent;
        card.localScale = Vector3.one * (area.rect.height / card.rect.height);
    }
}
