using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DraftPlayerCardStyler
{
    private static readonly Color FrameColor = new(0.22f, 0.30f, 0.40f, 1f);
    private static readonly Color NameColor = new(0.20f, 0.27f, 0.37f, 1f);
    private static readonly Color SecondaryColor = new(0.18f, 0.63f, 0.24f, 1f);
    private static readonly Color LabelColor = new(0.11f, 0.12f, 0.16f, 1f);
    private static readonly Color HighValueColor = new(0.46f, 0.82f, 0.77f, 1f);
    private static readonly Color MidValueColor = new(0.90f, 0.67f, 0.16f, 1f);
    private static readonly Color LowValueColor = new(0.88f, 0.37f, 0.34f, 1f);

    private const float ValueBadgeSize = 40f;
    private const float FlagWidth = 32f;
    private const float FlagHeight = 22f;
    private static Sprite valueBadgeSprite;

    public static void ApplyOutfield(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return;
        }

        Image frameImage = cardObject.GetComponent<Image>();
        if (frameImage != null)
        {
            frameImage.color = FrameColor;
        }

        RectTransform flag = FindDescendantComponent<RectTransform>(cardObject.transform, "Flag");
        if (flag != null)
        {
            ConfigureFlag(flag);
        }

        TMP_Text playerName = FindDescendantComponent<TMP_Text>(cardObject.transform, "PlayerName");
        if (playerName != null)
        {
            playerName.color = NameColor;
            playerName.fontStyle = FontStyles.Bold;
            playerName.enableAutoSizing = true;
            playerName.fontSizeMin = 16f;
            playerName.fontSizeMax = 29f;
            playerName.alignment = TextAlignmentOptions.Left;
            playerName.textWrappingMode = TextWrappingModes.NoWrap;
            playerName.overflowMode = TextOverflowModes.Ellipsis;
        }

        TMP_Text country = FindDescendantComponent<TMP_Text>(cardObject.transform, "Country");
        if (country != null)
        {
            country.color = SecondaryColor;
            country.fontStyle = FontStyles.Bold;
            country.enableAutoSizing = true;
            country.fontSizeMin = 10f;
            country.fontSizeMax = 18f;
            country.alignment = TextAlignmentOptions.Center;
            country.textWrappingMode = TextWrappingModes.NoWrap;
            country.overflowMode = TextOverflowModes.Ellipsis;
        }

        foreach (TMP_Text text in cardObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }

            if (text.name.EndsWith("Label", StringComparison.Ordinal))
            {
                text.color = LabelColor;
                text.enableAutoSizing = true;
                text.fontSizeMin = 11f;
                text.fontSizeMax = 18f;
                text.alignment = TextAlignmentOptions.Left;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
            else if (text.name.EndsWith("Value", StringComparison.Ordinal))
            {
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = false;
                text.fontSize = 20f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                UpdateValueBadge(text);
            }
            else if (text.text != null && text.text.IndexOf("counter attack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text.color = new Color(0.64f, 0.86f, 1f, 1f);
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 18f;
                text.characterSpacing = 1.2f;
                text.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private static void ConfigureFlag(RectTransform flag)
    {
        Image flagImage = flag.GetComponent<Image>();
        bool hasFlag = flagImage != null && flagImage.sprite != null;
        flag.gameObject.SetActive(hasFlag);
        if (!hasFlag)
        {
            return;
        }

        flag.anchorMin = new Vector2(0.5f, 0.5f);
        flag.anchorMax = new Vector2(0.5f, 0.5f);
        flag.pivot = new Vector2(0.5f, 0.5f);
        flag.anchoredPosition = new Vector2(-82f, 112f);
        flag.localRotation = Quaternion.identity;
        flag.localScale = Vector3.one;
        flag.sizeDelta = new Vector2(FlagWidth, FlagHeight);

        flagImage.color = Color.white;
        flagImage.preserveAspect = true;
        flagImage.raycastTarget = false;
    }

    private static void UpdateValueBadge(TMP_Text valueText)
    {
        if (valueText == null
            || !int.TryParse(valueText.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
        {
            return;
        }

        valueBadgeSprite ??= CreateBadgeSprite();
        Transform parent = valueText.transform.parent;
        if (parent == null || valueBadgeSprite == null)
        {
            return;
        }

        string badgeName = $"{valueText.name}Badge";
        Image badge = FindDescendantComponent<Image>(parent, badgeName);
        if (badge == null)
        {
            GameObject badgeObject = new(badgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            badgeObject.transform.SetParent(parent, false);
            badge = badgeObject.GetComponent<Image>();
        }

        RectTransform badgeRect = badge.rectTransform;
        ConfigureBadgeTransform(badgeRect, valueText.rectTransform);

        badge.sprite = valueBadgeSprite;
        badge.preserveAspect = true;
        badge.color = GetBadgeColor(parsedValue);
        badge.raycastTarget = false;
        valueText.raycastTarget = false;

        LayoutElement layoutElement = badge.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        DraftValueBadgeFollower follower = badge.GetComponent<DraftValueBadgeFollower>();
        if (follower == null)
        {
            follower = badge.gameObject.AddComponent<DraftValueBadgeFollower>();
        }

        follower.Configure(valueText.rectTransform, ValueBadgeSize);
        badge.transform.SetSiblingIndex(valueText.transform.GetSiblingIndex());
    }

    private static void ConfigureBadgeTransform(RectTransform badgeRect, RectTransform valueRect)
    {
        if (badgeRect == null || valueRect == null)
        {
            return;
        }

        badgeRect.anchorMin = valueRect.anchorMin;
        badgeRect.anchorMax = valueRect.anchorMax;
        badgeRect.pivot = valueRect.pivot;
        badgeRect.anchoredPosition = valueRect.anchoredPosition;
        badgeRect.localRotation = Quaternion.identity;
        badgeRect.localScale = Vector3.one;
        badgeRect.sizeDelta = new Vector2(ValueBadgeSize, ValueBadgeSize);
    }

    private static Color GetBadgeColor(int value)
    {
        if (value >= 5)
        {
            return HighValueColor;
        }

        return value >= 3 ? MidValueColor : LowValueColor;
    }

    private static Sprite CreateBadgeSprite()
    {
        const int textureSize = 64;
        const float edgeSoftness = 2f;
        Texture2D texture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "DraftValueBadgeRuntime",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        Color32[] pixels = new Color32[textureSize * textureSize];
        Vector2 center = new((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = ((textureSize * 0.5f) - 2f) * 0.72f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / edgeSoftness);
                pixels[(y * textureSize) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
    }

    private static T FindDescendantComponent<T>(Transform root, string name) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, StringComparison.Ordinal)
                && child.TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }
}

public sealed class DraftValueBadgeFollower : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float badgeSize = 18f;
    private RectTransform rectTransform;

    public void Configure(RectTransform targetRectTransform, float size)
    {
        target = targetRectTransform;
        badgeSize = size;
        UpdatePosition();
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        rectTransform ??= transform as RectTransform;
        if (rectTransform == null || target == null)
        {
            return;
        }

        rectTransform.anchorMin = target.anchorMin;
        rectTransform.anchorMax = target.anchorMax;
        rectTransform.pivot = target.pivot;
        rectTransform.anchoredPosition = target.anchoredPosition;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = new Vector2(badgeSize, badgeSize);
    }
}
