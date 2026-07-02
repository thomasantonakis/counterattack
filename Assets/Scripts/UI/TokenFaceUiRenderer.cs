using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TokenFaceUiRenderer
{
    public const float DefaultPlainNumberFontSize = 34f;
    public const float DefaultVerticalNumberFontSize = 30f;

    public static void Render(
        RawImage faceImage,
        TMP_Text numberText,
        TokenStyleDefinition style,
        int jerseyNumber,
        float plainNumberFontSize = DefaultPlainNumberFontSize,
        float verticalNumberFontSize = DefaultVerticalNumberFontSize)
    {
        if (style == null)
        {
            Clear(faceImage, numberText);
            return;
        }

        if (faceImage != null)
        {
            faceImage.texture = TokenFacePreviewUtility.GetOrCreateFaceTexture(style);
            faceImage.color = Color.white;
        }

        if (numberText != null)
        {
            numberText.text = jerseyNumber > 0 ? jerseyNumber.ToString() : string.Empty;
            TokenFacePreviewUtility.ApplyNumberStyle(numberText, style, plainNumberFontSize, verticalNumberFontSize);
        }
    }

    public static void Clear(RawImage faceImage, TMP_Text numberText)
    {
        if (faceImage != null)
        {
            faceImage.texture = null;
            faceImage.color = Color.clear;
        }

        if (numberText != null)
        {
            numberText.text = string.Empty;
        }
    }
}
