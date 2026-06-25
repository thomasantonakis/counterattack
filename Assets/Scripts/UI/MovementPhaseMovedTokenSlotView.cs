using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovementPhaseMovedTokenSlotView : MonoBehaviour
{
    private const float DefaultPanelPlainNumberFontSize = 24f;
    private const float DefaultPanelVerticalNumberFontSize = 21f;

    [SerializeField] private RawImage faceImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private float plainNumberFontSize = DefaultPanelPlainNumberFontSize;
    [SerializeField] private float verticalNumberFontSize = DefaultPanelVerticalNumberFontSize;

    public void Render(TokenStyleDefinition style, int jerseyNumber)
    {
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
        TokenFaceUiRenderer.Clear(faceImage, numberText);
    }
}
