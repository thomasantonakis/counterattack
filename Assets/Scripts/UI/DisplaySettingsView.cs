using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public sealed class DisplaySettingsView : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    private List<(FullScreenMode Mode, Vector2Int Size)> presets;
    private (Vector2Int Fullscreen, Vector2Int Windowed) limits;
    private FullScreenMode mode;

    private void OnEnable()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        RefreshOptions(DisplaySettings.GetLimits(), Screen.fullScreenMode);
        dropdown.onValueChanged.AddListener(SelectPreset);
    }

    private void Update()
    {
        if (dropdown.IsExpanded)
            return;
        var currentLimits = DisplaySettings.GetLimits();
        var currentMode = Screen.fullScreenMode;
        if (currentLimits != limits || currentMode != mode)
            RefreshOptions(currentLimits, currentMode);
    }

    private void RefreshOptions((Vector2Int Fullscreen, Vector2Int Windowed) currentLimits, FullScreenMode currentMode)
    {
        limits = currentLimits;
        mode = currentMode;
        presets = DisplaySettings.AvailablePresets(limits.Fullscreen, limits.Windowed);

        var labels = new List<string> { "Fullscreen (Auto)" };
        for (int i = 1; i < presets.Count; i++)
        {
            var preset = presets[i];
            string displayMode = preset.Mode == FullScreenMode.Windowed ? "Windowed" : "Fullscreen";
            labels.Add($"{displayMode} – {preset.Size.x} × {preset.Size.y}");
        }

        int selected = presets.IndexOf(DisplaySettings.SavedPreset);
        if (!Application.isEditor && (selected < 0 || presets[selected].Mode != mode))
            selected = presets.FindIndex(preset => preset.Mode == mode);
        dropdown.ClearOptions();
        dropdown.AddOptions(labels);
        dropdown.SetValueWithoutNotify(Mathf.Max(0, selected));
        dropdown.RefreshShownValue();
    }

    private void OnDisable()
    {
        dropdown.onValueChanged.RemoveListener(SelectPreset);
    }

    private void SelectPreset(int index)
    {
        DisplaySettings.Select(presets[index]);
    }
}

public static class DisplaySettings
{
    private const string WidthKey = "Display.Width";
    private const string FullscreenKey = "Display.Fullscreen";
    private const string LegacyWindowWidthKey = "Display.WindowWidth";
    private static readonly (FullScreenMode Mode, Vector2Int Size) Auto = (FullScreenMode.FullScreenWindow, Vector2Int.zero);
    private static readonly Vector2Int[] Resolutions =
    {
        new(1280, 720), new(1920, 1080), new(2560, 1440), new(3840, 2160)
    };

    public static (FullScreenMode Mode, Vector2Int Size) SavedPreset
    {
        get
        {
            int legacyWidth = PlayerPrefs.GetInt(LegacyWindowWidthKey, 0);
            int savedWidth = PlayerPrefs.GetInt(WidthKey, legacyWidth);
            bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, legacyWidth == 0 ? 1 : 0) != 0;
            foreach (Vector2Int size in Resolutions)
            {
                if (size.x == savedWidth)
                    return (fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, size);
            }
            return Auto;
        }
    }

    public static List<(FullScreenMode Mode, Vector2Int Size)> AvailablePresets(
        Vector2Int fullscreenLimit, Vector2Int windowLimit)
    {
        var available = new List<(FullScreenMode Mode, Vector2Int Size)> { Auto };
        foreach (Vector2Int size in Resolutions)
        {
            if (Fits(size, fullscreenLimit) && size != fullscreenLimit)
                available.Add((FullScreenMode.FullScreenWindow, size));
        }
        foreach (Vector2Int size in Resolutions)
        {
            if (Fits(size, windowLimit))
                available.Add((FullScreenMode.Windowed, size));
        }
        return available;
    }

    private static bool Fits(Vector2Int size, Vector2Int limit) => size.x <= limit.x && size.y <= limit.y;

    public static (Vector2Int Fullscreen, Vector2Int Windowed) GetLimits()
    {
        DisplayInfo display = Screen.mainWindowDisplayInfo;
        Resolution fallback = Screen.currentResolution;
        var fullscreen = new Vector2Int(display.width > 0 ? display.width : fallback.width,
            display.height > 0 ? display.height : fallback.height);
        RectInt workArea = display.workArea;
        int windowWidth = workArea.width > 0 ? workArea.width : fullscreen.x;
        int windowHeight = workArea.height > 0 ? workArea.height : fullscreen.y;
        // Leave space for the window title bar and resize borders.
        return (fullscreen, new Vector2Int(windowWidth - 16, windowHeight - 48));
    }

    public static void ApplySavedPreset()
    {
        var limits = GetLimits();
        var saved = SavedPreset;
        Apply(AvailablePresets(limits.Fullscreen, limits.Windowed).Contains(saved) ? saved : Auto, limits.Fullscreen);
    }

    public static void Select((FullScreenMode Mode, Vector2Int Size) preset)
    {
        var limits = GetLimits();
        if (!AvailablePresets(limits.Fullscreen, limits.Windowed).Contains(preset))
            return;
        PlayerPrefs.SetInt(WidthKey, preset.Size.x);
        PlayerPrefs.SetInt(FullscreenKey, preset.Mode == FullScreenMode.Windowed ? 0 : 1);
        PlayerPrefs.Save();
        Apply(preset, limits.Fullscreen);
    }

    private static void Apply((FullScreenMode Mode, Vector2Int Size) preset, Vector2Int nativeSize)
    {
        if (Application.isEditor)
            return;
        Vector2Int size = preset.Size == Vector2Int.zero ? nativeSize : preset.Size;
        Screen.SetResolution(size.x, size.y, preset.Mode);
    }
}
