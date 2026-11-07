using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovementPhaseMovedTokenSlotView : MonoBehaviour
{
    public enum PresentationState { Empty, Active, Completed, Forfeited }
    public PresentationState State { get; private set; }
    [SerializeField] private Image activeDisc;
    [SerializeField] private TMP_Text forfeitGraphic;
    [SerializeField] private TMP_Text forfeitOutline;
    [SerializeField, Min(0.1f)] private float pulseSeconds = 1.5f;
    private float pulseTime;
    private bool ghost;
    private Color ghostNumberColor;

    private void Update()
    {
        if (State != PresentationState.Active) return;
        if (MatchManager.Instance?.IsGameplayInputBlocked != true) pulseTime += Time.deltaTime;
        float alpha = Mathf.Lerp(0.2f, 0.65f, (1f - Mathf.Cos(pulseTime * Mathf.PI * 2f / pulseSeconds)) * 0.5f);
        if (ghost)
        {
            if (faceImage != null) faceImage.color = new Color(1f, 1f, 1f, alpha);
            if (numberText != null) numberText.color = new Color(ghostNumberColor.r, ghostNumberColor.g, ghostNumberColor.b, ghostNumberColor.a * alpha);
        }
        if (!ghost && activeDisc != null)
        {
            Color color = activeDisc.color; color.a = alpha; activeDisc.color = color;
        }
    }

    public void RenderActive(Color primaryColor, TokenStyleDefinition selectedStyle, int jerseyNumber, TokenStyleDefinition teamStyle = null)
    {
        Clear();
        State = PresentationState.Active;
        selectedStyle ??= teamStyle;
        ghost = selectedStyle != null;
        if (ghost)
        {
            TokenFaceUiRenderer.Render(faceImage, numberText, selectedStyle, jerseyNumber, plainNumberFontSize, verticalNumberFontSize);
            ghostNumberColor = numberText != null ? numberText.color : Color.white;
        }
        else if (activeDisc != null) { activeDisc.color = primaryColor; activeDisc.enabled = true; }
    }

    public void RenderForfeit(Color primaryColor, Color secondaryColor)
    {
        Clear(); State = PresentationState.Forfeited;
        if (forfeitGraphic == null) return;
        forfeitGraphic.text = "X";
        forfeitGraphic.color = primaryColor;
        forfeitGraphic.outlineColor = secondaryColor;
        forfeitGraphic.outlineWidth = 0.25f;
        if (forfeitOutline != null) { forfeitOutline.text = "X"; forfeitOutline.color = secondaryColor; }
    }
    private const float DefaultPanelPlainNumberFontSize = 24f;
    private const float DefaultPanelVerticalNumberFontSize = 21f;

    [SerializeField] private RawImage faceImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private float plainNumberFontSize = DefaultPanelPlainNumberFontSize;
    [SerializeField] private float verticalNumberFontSize = DefaultPanelVerticalNumberFontSize;

    public void Render(TokenStyleDefinition style, int jerseyNumber)
    {
        Clear();
        State = PresentationState.Completed;
        TokenFaceUiRenderer.Render(
            faceImage,
            numberText,
            style,
            jerseyNumber,
            plainNumberFontSize,
            verticalNumberFontSize);
    }

    public void Clear()
    {
        State = PresentationState.Empty;
        ghost = false;
        pulseTime = 0f;
        if (activeDisc != null) activeDisc.enabled = false;
        if (forfeitGraphic != null) forfeitGraphic.text = string.Empty;
        if (forfeitOutline != null) forfeitOutline.text = string.Empty;
        TokenFaceUiRenderer.Clear(faceImage, numberText);
    }
}
