using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DraftPlayerCardStyler
{
    private static readonly Color FrameColor = new(0.09019608f, 0.227451f, 0.3098039f, 1f);
    private static Color HighValueColor = new(0.1019608f, 0.7058824f, 0.654902f, 1f);
    private static Color MidValueColor = new(0.9686275f, 0.5647059f, 0.3411765f, 1f);
    private static Color LowValueColor = new(0.8823529f, 0.3843137f, 0.3333333f, 1f);

    private const float ValueBadgeSize = 72f;
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

        TMP_Text country = FindDescendantComponent<TMP_Text>(cardObject.transform, "Country");

        foreach (TMP_Text text in cardObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }
            
            if (text.name.EndsWith("Value", StringComparison.Ordinal))
            {
                UpdateValueBadge(text);
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
        else if (value >= 3)
        {
            return MidValueColor;
        }
        else
        {
            return LowValueColor;
        }
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
